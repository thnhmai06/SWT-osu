// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Graphics;

namespace osu.Game.Tests.Graphics
{
    [TestFixture]
    public class OsuFontTest
    {
        // CF: OsuFont.GetFamilyString | typeface | case Venera
        // CF: OsuFont.GetFamilyString | typeface | case Torus
        // CF: OsuFont.GetFamilyString | typeface | case TorusAlternate
        // CF: OsuFont.GetFamilyString | typeface | case Inter
        // CF: OsuFont.GetFamilyString | typeface | default
        [TestCase(Typeface.Venera, "Venera")]
        [TestCase(Typeface.Torus, "Torus")]
        [TestCase(Typeface.TorusAlternate, "Torus-Alternate")]
        [TestCase(Typeface.Inter, "Inter")]
        [TestCase((Typeface)99, null)]
        public void TestFamilyString(Typeface typeface, string? expected) => Assert.That(OsuFont.GetFamilyString(typeface), Is.EqualTo(expected));

        // CF: OsuFont.GetWeightString | family is Torus && weight == Medium | TRUE
        // CF: OsuFont.GetWeightString | family == Torus | TRUE
        [Test]
        public void TestTorusMediumFallsBackToRegular()
        {
            Assert.That(OsuFont.GetWeightString("Torus", FontWeight.Medium), Is.EqualTo("Regular"));
        }

        // CF: OsuFont.GetWeightString | family == Torus-Alternate | TRUE
        [Test]
        public void TestTorusAlternateMediumFallsBackToRegular()
        {
            Assert.That(OsuFont.GetWeightString("Torus-Alternate", FontWeight.Medium), Is.EqualTo("Regular"));
        }

        // CF: OsuFont.GetWeightString | family is Torus | FALSE
        // CF: OsuFont.GetWeightString | weight == Medium | FALSE
        [Test]
        public void TestOtherCombinationsKeepWeight()
        {
            Assert.That(OsuFont.GetWeightString("Torus", FontWeight.Bold), Is.EqualTo("Bold"));
            Assert.That(OsuFont.GetWeightString("Inter", FontWeight.Medium), Is.EqualTo("Medium"));
        }

        [Test]
        public void TestGetFontNeverItalic()
        {
            var font = OsuFont.GetFont(Typeface.Inter, 20, FontWeight.Bold, italics: true, fixedWidth: true);
            Assert.That(font.Family, Is.EqualTo("Inter"));
            Assert.That(font.Size, Is.EqualTo(20));
            Assert.That(font.Weight, Is.EqualTo("Bold"));
            Assert.That(font.Italics, Is.False);
            Assert.That(font.FixedWidth, Is.True);
        }

        [Test]
        public void TestPresets()
        {
            Assert.That(OsuFont.Default.Weight, Is.EqualTo("Regular"));
            Assert.That(OsuFont.Numeric.Family, Is.EqualTo("Venera"));
            Assert.That(OsuFont.Style.Title.Size, Is.EqualTo(32));
            Assert.That(OsuFont.Style.Body.Size, Is.EqualTo(OsuFont.DEFAULT_FONT_SIZE));
        }

        // CF: OsuFontExtensions.With | typeface != null | TRUE
        // CF: OsuFontExtensions.With | weight != null | TRUE
        [Test]
        public void TestWithTypefaceAndWeight()
        {
            var font = OsuFont.Inter.With(Typeface.Torus, 30, FontWeight.Medium);
            Assert.That(font.Family, Is.EqualTo("Torus"));
            Assert.That(font.Weight, Is.EqualTo("Regular"));
            Assert.That(font.Size, Is.EqualTo(30));
        }

        // CF: OsuFontExtensions.With | typeface != null | FALSE
        // CF: OsuFontExtensions.With | weight != null | FALSE
        [Test]
        public void TestWithNothingKeepsUsage()
        {
            var original = OsuFont.GetFont(Typeface.Inter, 12, FontWeight.Bold);
            var font = original.With();
            Assert.That(font.Family, Is.EqualTo(original.Family));
            Assert.That(font.Weight, Is.EqualTo(original.Weight));
            Assert.That(font.Size, Is.EqualTo(12));
        }
    }
}
