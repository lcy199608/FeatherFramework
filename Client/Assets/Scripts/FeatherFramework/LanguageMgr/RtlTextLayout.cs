using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Topten.RichTextKit;
using Topten.RichTextKit.Utils;

/// <summary>Display-only adapter for legacy uGUI Text. Input and table data remain in logical order.</summary>
internal sealed class RtlTextLayout
{
    private readonly Bidi bidi = new Bidi();
    private readonly BidiData bidiData = new BidiData();
    private static readonly Regex Tag = new Regex(@"\G<(/?)(b|i|size|color)(=[^<>]+)?>", RegexOptions.IgnoreCase);

    private sealed class Style
    {
        internal string Open;
        internal string Close;
    }

    private sealed class Paragraph
    {
        internal string Text;
        internal Style[] Styles;
        internal sbyte[] Levels;
        internal int[] Boundaries;
        internal int BaseLevel;
    }

    internal static bool ContainsRtl(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        for (int i = 0; i < text.Length; i++)
        {
            int code = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                ? char.ConvertToUtf32(text[i], text[++i]) : text[i];
            var direction = UnicodeClasses.Directionality(code);
            if (direction == Directionality.R || direction == Directionality.AL) return true;
        }
        return false;
    }

    internal static bool StartsRightToLeft(string source, bool richText)
    {
        Parse(source ?? string.Empty, richText, out string plain, out _);
        foreach (char c in plain)
        {
            var direction = UnicodeClasses.Directionality(c);
            if (direction == Directionality.L) return false;
            if (direction == Directionality.R || direction == Directionality.AL) return true;
        }
        return false;
    }

    internal string Build(string source, bool richText, bool rightToLeft, float width, Func<string, float> measure)
    {
        Parse(source.Replace("\r\n", "\n").Replace('\r', '\n'), richText, out string plain, out Style[] styles);
        var output = new StringBuilder();
        int start = 0;
        while (start <= plain.Length)
        {
            int end = plain.IndexOf('\n', start);
            if (end < 0) end = plain.Length;
            var paragraph = Prepare(plain.Substring(start, end - start), styles, start, rightToLeft);
            AppendParagraph(output, paragraph, width, measure);
            if (end == plain.Length) break;
            output.Append('\n');
            start = end + 1;
        }
        return output.ToString();
    }

    private Paragraph Prepare(string text, Style[] styles, int offset, bool rtl)
    {
        var points = new List<int>();
        var offsets = new List<int>();
        for (int i = 0; i < text.Length; i++)
        {
            offsets.Add(i);
            points.Add(char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                ? char.ConvertToUtf32(text[i], text[++i]) : text[i]);
        }
        bidiData.Init(new Slice<int>(points.ToArray()), (sbyte)(rtl ? 1 : 0));
        var levels = new sbyte[text.Length];
        if (points.Count > 0)
        {
            bidi.Process(bidiData);
            for (int i = 0; i < points.Count; i++)
                for (int j = offsets[i]; j < (i + 1 < points.Count ? offsets[i + 1] : text.Length); j++)
                    levels[j] = bidi.ResolvedLevels[i];
        }
        var localStyles = new Style[text.Length];
        Array.Copy(styles, offset, localStyles, 0, text.Length);
        // Keep combining marks and surrogate pairs with their base; never split a lam-alef ligature.
        var boundaries = new List<int>(StringInfo.ParseCombiningCharacters(text));
        for (int i = boundaries.Count - 1; i > 0; i--)
        {
            int index = boundaries[i];
            if (text[boundaries[i - 1]] == '\u0644' && "\u0622\u0623\u0625\u0627".IndexOf(text[index]) >= 0)
                boundaries.RemoveAt(i);
        }
        boundaries.Add(text.Length);
        return new Paragraph { Text = text, Styles = localStyles, Levels = levels, Boundaries = boundaries.ToArray(), BaseLevel = rtl ? 1 : 0 };
    }

    private void AppendParagraph(StringBuilder output, Paragraph paragraph, float width, Func<string, float> measure)
    {
        int count = paragraph.Boundaries.Length - 1;
        if (count <= 0) return;
        if (float.IsPositiveInfinity(width)) { output.Append(RenderLine(paragraph, 0, count)); return; }
        int start = 0;
        while (start < count)
        {
            int fit = start;
            int wordBreak = -1;
            for (int end = start + 1; end <= count; end++)
            {
                if (measure(RenderLine(paragraph, start, end)) > width + 0.01f && end > start + 1) break;
                fit = end;
                int last = paragraph.Boundaries[end - 1];
                if (paragraph.Text[last] == ' ' || paragraph.Text[last] == '\t' || paragraph.Text[last] == '\u200B') wordBreak = end;
            }
            if (fit < count && wordBreak > start) fit = wordBreak;
            if (fit <= start) fit = start + 1;
            output.Append(RenderLine(paragraph, start, fit));
            start = fit;
            if (start < count) output.Append('\n');
        }
    }

    private static string RenderLine(Paragraph p, int first, int last)
    {
        int from = p.Boundaries[first], to = p.Boundaries[last];
        string logical = p.Text.Substring(from, to - from);
        // The shaper uses reusable static buffers. Lock the dependency, not a service/global manager.
        string shaped;
        lock (typeof(ArabicFixerTool)) shaped = ArabicFixerTool.ShapeLogical(logical);
        var indices = new int[last - first];
        var levels = new int[indices.Length];
        int maximum = p.BaseLevel;
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = first + i;
            levels[i] = p.Levels[p.Boundaries[first + i]];
        }
        // UAX #9 L1: reset trailing whitespace at each actual line break.
        for (int i = levels.Length - 1; i >= 0; i--)
        {
            char c = p.Text[p.Boundaries[first + i]];
            if (!char.IsWhiteSpace(c) && !IsBidiControl(c)) break;
            levels[i] = p.BaseLevel;
        }
        foreach (int level in levels) maximum = Math.Max(maximum, level);
        // L2 applies to grapheme clusters; marks and surrogate pairs retain their internal order (L3).
        for (int level = maximum; level >= 1; level--)
        {
            int i = 0;
            while (i < indices.Length)
            {
                if (levels[i] < level) { i++; continue; }
                int end = i + 1;
                while (end < indices.Length && levels[end] >= level) end++;
                Array.Reverse(indices, i, end - i);
                Array.Reverse(levels, i, end - i);
                i = end;
            }
        }
        var result = new StringBuilder();
        Style active = null;
        for (int i = 0; i < indices.Length; i++)
        {
            int begin = p.Boundaries[indices[i]], end = p.Boundaries[indices[i] + 1];
            for (int j = begin; j < end; j++)
            {
                char c = shaped[j - from];
                if (c == '\uFFFF' || IsBidiControl(c) || c == '\u200B') continue;
                var style = p.Styles[j];
                if (!ReferenceEquals(active, style))
                {
                    result.Append(active?.Close);
                    result.Append(style?.Open);
                    active = style;
                }
                if ((levels[i] & 1) != 0) c = Mirror(c);
                result.Append(c);
            }
        }
        result.Append(active?.Close);
        return result.ToString();
    }

    private static char Mirror(char c)
    {
        uint data = UnicodeClasses.BidiData(c);
        if (((data >> 16) & 0xff) != 0) return (char)(data & 0xffff);
        return c == '<' ? '>' : c == '>' ? '<' : c;
    }

    private static bool IsBidiControl(char c) => c == '\u061C' || c == '\u200E' || c == '\u200F' ||
        (c >= '\u202A' && c <= '\u202E') || (c >= '\u2066' && c <= '\u2069');

    private static void Parse(string source, bool richText, out string plain, out Style[] styles)
    {
        var text = new StringBuilder();
        var map = new List<Style>();
        var tags = new List<(string Name, string Open)>();
        Style current = null;
        for (int i = 0; i < source.Length;)
        {
            var match = richText && source[i] == '<' ? Tag.Match(source, i) : Match.Empty;
            if (match.Success)
            {
                string name = match.Groups[2].Value.ToLowerInvariant();
                bool close = match.Groups[1].Length > 0;
                bool valid = close ? tags.Count > 0 && tags[tags.Count - 1].Name == name
                    : (name == "b" || name == "i") ? match.Groups[3].Length == 0 : match.Groups[3].Length > 1;
                if (valid)
                {
                    if (close) tags.RemoveAt(tags.Count - 1);
                    else tags.Add((name, match.Value));
                    var open = new StringBuilder();
                    var shut = new StringBuilder();
                    foreach (var tag in tags) open.Append(tag.Open);
                    for (int j = tags.Count - 1; j >= 0; j--) shut.Append("</").Append(tags[j].Name).Append('>');
                    current = tags.Count == 0 ? null : new Style { Open = open.ToString(), Close = shut.ToString() };
                    i += match.Length;
                    continue;
                }
            }
            text.Append(source[i++]);
            map.Add(current);
        }
        plain = text.ToString();
        styles = map.ToArray();
    }
}
