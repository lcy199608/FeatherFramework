using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextOfEnhance : Text
{
    public int languageId = 0;
    protected override void Start()
    {
        base.Start();
        //Text的OnEnable会在编辑器模式下运行,所以需要判断是否在运行模式
        if (Application.isPlaying)
        {
            if (languageId > 0)
            {
                SwitchLanguage();
                EventCenter.Instance.AddEventListener("LanguageSwitch", SwitchLanguage);
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Application.isPlaying)
        {
            if (languageId > 0)
            {
                EventCenter.Instance.RemoveEventListener("LanguageSwitch", SwitchLanguage);
            }
        }
    }

    void SwitchLanguage()
    {
        text = LanguageMgr.Instance.GetLanguageById(languageId);
    }

    #region 实现超框时再缩小字体，适配多语言（针对中文）
    /// <summary>
    /// 当前可见的文字行数
    /// </summary>
    int visibleLines;

    private void _UseFitSettings()
    {
        TextGenerationSettings settings = GetGenerationSettings(rectTransform.rect.size);
        settings.resizeTextForBestFit = false;

        if (!resizeTextForBestFit)
        {
            cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
            return;
        }

        int minSize = resizeTextMinSize;
        int txtLen = text.Length;
        for (int i = resizeTextMaxSize; i >= minSize; --i)
        {
            settings.fontSize = i;
            cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
            if (cachedTextGenerator.characterCountVisible == txtLen) break;
        }
    }

    private readonly UIVertex[] _tmpVerts = new UIVertex[4];
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        if (null == font) return;

        m_DisableFontTextureRebuiltCallback = true;
        _UseFitSettings();

        // Apply the offset to the vertices
        IList<UIVertex> verts = cachedTextGenerator.verts;
        float unitsPerPixel = 1 / pixelsPerUnit;
        int vertCount = verts.Count;

        // We have no verts to process just return (case 1037923)
        if (vertCount <= 0)
        {
            toFill.Clear();
            return;
        }

        Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
        roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
        toFill.Clear();
        if (roundingOffset != Vector2.zero)
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                _tmpVerts[tempVertsIndex] = verts[i];
                _tmpVerts[tempVertsIndex].position *= unitsPerPixel;
                _tmpVerts[tempVertsIndex].position.x += roundingOffset.x;
                _tmpVerts[tempVertsIndex].position.y += roundingOffset.y;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(_tmpVerts);
            }
        }
        else
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                _tmpVerts[tempVertsIndex] = verts[i];
                _tmpVerts[tempVertsIndex].position *= unitsPerPixel;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(_tmpVerts);
            }
        }

        m_DisableFontTextureRebuiltCallback = false;
        visibleLines = cachedTextGenerator.lineCount;
    }
        #endregion
}
