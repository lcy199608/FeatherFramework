using System;
using System.Collections.Generic;
using UnityEngine;

public class TextOfEnhance : UnityEngine.UI.Text
{
    public int languageId;
    public bool enableRtl = true;
    [Tooltip("镜像左右对齐；居中不变。关闭后保留 Inspector 中的对齐。")]
    public bool mirrorRtlAlignment = true;
    [Tooltip("可选 RTL 字体；须为包含阿拉伯连接字形/希伯来字符的 Dynamic 字体。")]
    public Font rtlFont;
    public LanguageFont[] languageFonts = Array.Empty<LanguageFont>();

    [Serializable]
    public sealed class LanguageFont
    {
        public SystemLanguage language = SystemLanguage.English;
        public Font font;
    }

    private IDisposable languageSubscription;
    private SystemLanguage textLanguage = SystemLanguage.Unknown;
    private readonly RtlTextLayout rtlLayout = new RtlTextLayout();
    private TextGenerator measureGenerator;
    private TextGenerationSettings cachedSettings;
    private TextGenerationSettings displaySettings;
    private string cachedSource;
    private string displayText;
    private bool cachedRtl;
    private bool layoutValid;
    private readonly UIVertex[] quad = new UIVertex[4];

    /// <summary>Raw logical-order text. Use the resolved language when formatting a translated template.</summary>
    public void SetRawText(string value, SystemLanguage language = SystemLanguage.Unknown)
    {
        textLanguage = language;
        base.text = value ?? string.Empty;
        RefreshTextLayout();
    }

    // Keep direct assignments compatible; never store shaped text in m_Text or prefab data.
    public override string text
    {
        get => base.text;
        set => SetRawText(value);
    }

    public void RefreshTextLayout()
    {
        layoutValid = false;
        SetAllDirty();
    }

    protected override void Start()
    {
        base.Start();
        if (Application.isPlaying && Framework.IsReady && Framework.Services.HasLocalization && languageId > 0)
        {
            SwitchLanguage();
            languageSubscription = Framework.Services.Events.Subscribe(FrameworkEvents.LanguageChangedId, SwitchLanguage);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Font.textureRebuilt += OnFontTextureRebuilt;
        layoutValid = false;
    }

    protected override void OnDisable()
    {
        Font.textureRebuilt -= OnFontTextureRebuilt;
        base.OnDisable();
    }

    private void OnFontTextureRebuilt(Font changed)
    {
        if (!m_DisableFontTextureRebuiltCallback && changed == EffectiveFont)
        {
            layoutValid = false;
            cachedTextGenerator.Invalidate();
            measureGenerator?.Invalidate();
            // Match uGUI's font callback: do not enqueue a graphic while Canvas is rebuilding it.
            if (UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingGraphics() || UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingLayout())
                UpdateGeometry();
            else SetAllDirty();
        }
    }

    protected override void OnDestroy()
    {
        languageSubscription?.Dispose();
        languageSubscription = null;
        ((IDisposable)measureGenerator)?.Dispose();
        base.OnDestroy();
    }

    private void SwitchLanguage()
    {
        string raw = Framework.Services.Localization.GetLanguageById(languageId, out var resolvedLanguage);
        SetRawText(raw, resolvedLanguage);
    }

    private bool NeedsRtl => enableRtl && RtlTextLayout.ContainsRtl(base.text);
    private bool BaseRtl => textLanguage == SystemLanguage.Arabic || textLanguage == SystemLanguage.Hebrew ||
        (textLanguage == SystemLanguage.Unknown && RtlTextLayout.StartsRightToLeft(base.text, supportRichText));

    private Font EffectiveFont
    {
        get
        {
            if (languageFonts != null)
                foreach (var entry in languageFonts)
                    if (entry != null && entry.font != null && textLanguage != SystemLanguage.Unknown &&
                        LanguageMgr.Normalize(entry.language) == LanguageMgr.Normalize(textLanguage)) return entry.font;
            return NeedsRtl && rtlFont != null ? rtlFont : font;
        }
    }

    public override Texture mainTexture => EffectiveFont != null && EffectiveFont.material != null
        ? EffectiveFont.material.mainTexture : base.mainTexture;

    internal string DisplayText { get { PrepareLayout(rectTransform.rect.size, true); return displayText; } }
    internal int DisplayFontSize { get { PrepareLayout(rectTransform.rect.size, true); return displaySettings.fontSize; } }
    internal TextAnchor DisplayAlignment { get { PrepareLayout(rectTransform.rect.size, true); return displaySettings.textAnchor; } }

    private void PrepareLayout(Vector2 extents, bool allowBestFit)
    {
        var settings = GetGenerationSettings(extents);
        settings.font = EffectiveFont;
        bool rtl = NeedsRtl;
        if (rtl && BaseRtl && mirrorRtlAlignment) settings.textAnchor = MirrorAlignment(settings.textAnchor);
        settings.resizeTextForBestFit = allowBestFit && resizeTextForBestFit;
        if (layoutValid && cachedSource == base.text && cachedSettings.Equals(settings) && cachedRtl == rtl) return;
        cachedSource = base.text;
        cachedSettings = settings;
        cachedRtl = rtl;
        layoutValid = true;
        measureGenerator ??= new TextGenerator();
        bool fit = settings.resizeTextForBestFit && settings.font != null && settings.font.dynamic;
        settings.resizeTextForBestFit = false;
        int minimum = fit ? Math.Max(1, resizeTextMinSize) : settings.fontSize;
        int maximum = fit ? Math.Max(minimum, resizeTextMaxSize) : settings.fontSize;
        for (int size = maximum; size >= minimum; size--)
        {
            settings.fontSize = size;
            displayText = base.text;
            if (rtl && settings.font != null)
            {
                var measureSettings = settings;
                measureSettings.horizontalOverflow = HorizontalWrapMode.Overflow;
                measureSettings.verticalOverflow = VerticalWrapMode.Overflow;
                measureSettings.generationExtents = Vector2.zero;
                float width = horizontalOverflow == HorizontalWrapMode.Wrap ? Math.Max(0, extents.x) : float.PositiveInfinity;
                displayText = rtlLayout.Build(base.text, supportRichText, BaseRtl, width,
                    line => measureGenerator.GetPreferredWidth(line, measureSettings) / pixelsPerUnit);
                // Lines are already in visual order; Unity must not wrap them a second time.
                settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            if (!fit || Fits(displayText, settings, extents)) break;
        }
        displaySettings = settings;
    }

    private bool Fits(string value, TextGenerationSettings settings, Vector2 extents)
    {
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        if (cachedRtl)
        {
            var unconstrained = settings;
            unconstrained.generationExtents = Vector2.zero;
            if (measureGenerator.GetPreferredWidth(value, unconstrained) / pixelsPerUnit > extents.x + 0.01f) return false;
        }
        return measureGenerator.GetPreferredHeight(value, settings) / pixelsPerUnit <= extents.y + 0.01f;
    }

    public override float preferredWidth
    {
        get
        {
            if (!NeedsRtl && EffectiveFont == font) return base.preferredWidth;
            PrepareLayout(new Vector2(float.PositiveInfinity, 0), false);
            var settings = displaySettings;
            settings.generationExtents = Vector2.zero;
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            return measureGenerator.GetPreferredWidth(displayText, settings) / pixelsPerUnit;
        }
    }

    public override float preferredHeight
    {
        get
        {
            if (!NeedsRtl && EffectiveFont == font) return base.preferredHeight;
            PrepareLayout(new Vector2(rectTransform.rect.width, 0), false);
            return measureGenerator.GetPreferredHeight(displayText, displaySettings) / pixelsPerUnit;
        }
    }

    private static TextAnchor MirrorAlignment(TextAnchor anchor)
    {
        int column = (int)anchor % 3;
        return (TextAnchor)((int)anchor - column + 2 - column);
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper toFill)
    {
        toFill.Clear();
        if (EffectiveFont == null) return;
        m_DisableFontTextureRebuiltCallback = true;
        try
        {
            PrepareLayout(rectTransform.rect.size, true);
            cachedTextGenerator.PopulateWithErrors(displayText, displaySettings, gameObject);
            IList<UIVertex> verts = cachedTextGenerator.verts;
            float unitsPerPixel = 1 / pixelsPerUnit;
            int count = verts.Count;
            if (count == 0) return;
            Vector2 offset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
            offset = PixelAdjustPoint(offset) - offset;
            for (int i = 0; i < count; i++)
            {
                int corner = i & 3;
                quad[corner] = verts[i];
                quad[corner].position *= unitsPerPixel;
                quad[corner].position.x += offset.x;
                quad[corner].position.y += offset.y;
                if (corner == 3) toFill.AddUIVertexQuad(quad);
            }
        }
        finally { m_DisableFontTextureRebuiltCallback = false; }
    }
}
