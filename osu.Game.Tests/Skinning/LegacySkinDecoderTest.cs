// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;
using osu.Game.Skinning;
using osuTK.Graphics;

namespace osu.Game.Tests.Skinning
{
    [TestFixture]
    public class LegacySkinDecoderTest
    {
        private static SkinConfiguration decode(string text)
        {
            using var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));
            return new LegacySkinDecoder().Decode(reader);
        }

        // CF: LegacySkinDecoder.CreateTemplateObject | default version | no throw
        [Test]
        public void TestDefaultsToVersionOne()
        {
            var c = decode("[General]\n");
            Assert.That(c.LegacyVersion, Is.EqualTo(1.0m));
            Assert.That(c.IsLatestVersion, Is.False);
        }

        // CF: LegacySkinDecoder.ParseLine | section != Section.Colours | TRUE
        // CF: LegacySkinDecoder.ParseLine | section | case General
        // CF: LegacySkinDecoder.ParseLine | pair.Key | case Name
        // CF: LegacySkinDecoder.ParseLine | pair.Key | case Author
        [Test]
        public void TestNameAndAuthor()
        {
            var c = decode("[General]\nName: My Skin\nAuthor: me\n");
            Assert.That(c.SkinInfo.Name, Is.EqualTo("My Skin"));
            Assert.That(c.SkinInfo.Creator, Is.EqualTo("me"));
        }

        // CF: LegacySkinDecoder.ParseLine | pair.Key | case Version
        // CF: LegacySkinDecoder.ParseLine | pair.Value == "latest" | TRUE
        [Test]
        public void TestLatestVersion()
        {
            var c = decode("[General]\nVersion: latest\n");
            Assert.That(c.IsLatestVersion, Is.True);
            Assert.That(c.LegacyVersion, Is.EqualTo(SkinConfiguration.LATEST_VERSION));
        }

        // CF: LegacySkinDecoder.ParseLine | pair.Value == "latest" | FALSE
        // CF: LegacySkinDecoder.ParseLine | decimal.TryParse(pair.Value) | TRUE
        [Test]
        public void TestNumericVersion()
        {
            var c = decode("[General]\nVersion: 2.5\n");
            Assert.That(c.LegacyVersion, Is.EqualTo(2.5m));
            Assert.That(c.IsLatestVersion, Is.False);
        }

        // CF: LegacySkinDecoder.ParseLine | decimal.TryParse(pair.Value) | FALSE
        [Test]
        public void TestInvalidVersionKeepsDefault()
        {
            Assert.That(decode("[General]\nVersion: abc\n").LegacyVersion, Is.EqualTo(1.0m));
        }

        // CF: LegacySkinDecoder.ParseLine | section | case CatchTheBeat
        // CF: LegacySkinDecoder.ParseLine | section != Section.Colours | FALSE
        [Test]
        public void TestCatchColoursAllowAlpha()
        {
            var c = decode("[CatchTheBeat]\nHyperDash: 255,0,0,100\n");
            Assert.That(c.CustomColours["HyperDash"], Is.EqualTo(new Color4(255, 0, 0, 100)));
        }

        // CF: LegacySkinDecoder.ParseLine | !string.IsNullOrEmpty(pair.Key) | TRUE
        [Test]
        public void TestUnknownKeysGoToConfigDictionary()
        {
            var c = decode("[General]\nSliderBallFlip: 1\n");
            Assert.That(c.ConfigDictionary["SliderBallFlip"], Is.EqualTo("1"));
        }

        // CF: LegacySkinDecoder.ParseLine | !string.IsNullOrEmpty(pair.Key) | FALSE
        // CF: LegacySkinDecoder.ParseLine | section (other) | default
        [Test]
        public void TestUnknownSectionKeyIsStoredAndEmptyKeyIsNot()
        {
            var c = decode("[Fonts]\nHitCirclePrefix: default\n: value\n");
            Assert.That(c.ConfigDictionary["HitCirclePrefix"], Is.EqualTo("default"));
            Assert.That(c.ConfigDictionary.ContainsKey(string.Empty), Is.False);
        }

        // CF: LegacySkinDecoder.ParseLine | section == Colours (base handles colours) | TRUE
        [Test]
        public void TestComboColours()
        {
            var c = decode("[Colours]\nCombo1: 1,2,3\nCombo2: 4,5,6\nSliderBorder: 7,8,9\n");
            Assert.That(c.CustomComboColours, Has.Count.EqualTo(2));
            Assert.That(c.CustomColours["SliderBorder"], Is.EqualTo(new Color4(7, 8, 9, 255)));
        }
    }
}
