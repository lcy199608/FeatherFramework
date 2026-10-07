using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class RtlTextTests
    {
        [TestCase("سلام", "\uFEE1\uFEFC\uFEB3")]
        [TestCase("مرحبا", "\uFE8E\uFE92\uFEA3\uFEAE\uFEE3")]
        [TestCase("שלום (ABC 123)", "(ABC 123) םולש")]
        [TestCase("שלום 😀 ABC", "ABC 😀 םולש")]
        [TestCase("שָׁלוֹם", "םוֹלשָׁ")]
        [TestCase("שלום\nעולם", "םולש\nםלוע")]
        public void LogicalTextIsShapedAndReorderedWithoutCorruptingClusters(string raw, string expected)
        {
            Assert.That(Build(raw), Is.EqualTo(expected));
        }

        [Test]
        public void RichTextStaysAttachedToItsOriginalCharacters()
        {
            Assert.That(Build("<color=red><b>שלום</b></color> 123"), Is.EqualTo("123 <color=red><b>םולש</b></color>"));
            Assert.That(Build("<size=24>שלום</size> ABC"), Is.EqualTo("ABC <size=24>םולש</size>"));
        }

        [Test]
        public void WrappedLinesKeepLogicalTopToBottomOrder()
        {
            var result = new RtlTextLayout().Build("שלום עולם בדיקה", false, true, 5, value => value.Length);
            Assert.That(result.Split('\n').Select(s => s.Trim()).ToArray(), Is.EqualTo(new[] { "םולש", "םלוע", "הקידב" }));
            Assert.That(Build("سلام").IndexOf('\uFFFF'), Is.EqualTo(-1));
        }

        [Test]
        public void NarrowWrapKeepsSurrogatesMarksAndLamAlefTogether()
        {
            var layout = new RtlTextLayout();
            string result = layout.Build("لا שָׁ 😀", false, true, 0, value => value.Length);
            Assert.That(result, Does.Contain("\uFEFB"));
            Assert.That(result, Does.Contain("שָׁ"));
            Assert.That(result, Does.Contain("😀"));
        }

        [Test]
        public void BidiIsolatesDoNotLeakIntoDisplay()
        {
            Assert.That(Build("שלום \u2066ABC 123\u2069"), Is.EqualTo("ABC 123 םולש"));
        }

        [Test]
        public void LegacyTextKeepsRawDataAndRestoresAlignmentAfterSwitch()
        {
            var go = new GameObject("RtlText", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var label = go.AddComponent<TextOfEnhance>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.alignment = TextAnchor.UpperLeft;
                label.rectTransform.sizeDelta = new Vector2(500, 100);
                label.SetRawText("שלום (ABC 123)", SystemLanguage.Hebrew);
                Assert.That(label.text, Is.EqualTo("שלום (ABC 123)"));
                Assert.That(label.DisplayText, Is.EqualTo("(ABC 123) םולש"));
                Assert.That(label.DisplayText, Is.EqualTo("(ABC 123) םולש"), "No double shaping");
                Assert.That(label.DisplayAlignment, Is.EqualTo(TextAnchor.UpperRight));
                Assert.That(label.alignment, Is.EqualTo(TextAnchor.UpperLeft), "Do not mutate authored alignment");
                label.SetRawText("Start Game", SystemLanguage.English);
                Assert.That(label.DisplayText, Is.EqualTo("Start Game"));
                Assert.That(label.DisplayAlignment, Is.EqualTo(TextAnchor.UpperLeft));
                label.text = "שלום";
                Assert.That(label.DisplayText, Is.EqualTo("םולש"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void WidthAndBestFitAreAppliedToVisualText()
        {
            var go = new GameObject("RtlLayout", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var label = go.AddComponent<TextOfEnhance>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 30;
                label.rectTransform.sizeDelta = new Vector2(90, 200);
                label.SetRawText("שלום ABC שלום ABC שלום ABC", SystemLanguage.Hebrew);
                string narrow = label.DisplayText;
                Assert.That(narrow, Does.Contain("\n"));
                label.rectTransform.sizeDelta = new Vector2(900, 200);
                Assert.That(label.DisplayText, Does.Not.Contain("\n"));
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 40;
                label.rectTransform.sizeDelta = new Vector2(130, 45);
                Assert.That(label.DisplayFontSize, Is.LessThan(40).And.GreaterThanOrEqualTo(10));
                Assert.That(label.text, Is.EqualTo("שלום ABC שלום ABC שלום ABC"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void FontOverridesAndDisabledRtlKeepAuthoredFontIntact()
        {
            var go = new GameObject("RtlFont", typeof(RectTransform), typeof(CanvasRenderer));
            var otherFont = Font.CreateDynamicFontFromOSFont("Arial", 24);
            try
            {
                var label = go.AddComponent<TextOfEnhance>();
                var original = label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.rtlFont = otherFont;
                label.SetRawText("שלום", SystemLanguage.Hebrew);
                Assert.That(label.DisplayText, Is.EqualTo("םולש"));
                Assert.That(label.font, Is.SameAs(original));
                Assert.That(label.mainTexture, Is.SameAs(otherFont.material.mainTexture));
                label.enableRtl = false;
                Assert.That(label.DisplayText, Is.EqualTo("שלום"));
                Assert.That(label.mainTexture, Is.SameAs(original.material.mainTexture));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(otherFont); }
        }

        private static string Build(string raw) => new RtlTextLayout().Build(raw, true, true, float.PositiveInfinity,
            value => Regex.Replace(value, "<[^>]*>", "").Length);
    }
}
