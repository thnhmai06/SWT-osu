// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Skinning;
using osuTK.Graphics;

namespace osu.Game.Tests.Skinning
{
    [TestFixture]
    public class SkinConfigurationTest
    {
        // CF: SkinConfiguration.ComboColours | CustomComboColours.Count > 0 | TRUE
        [Test]
        public void TestCustomColoursTakePrecedence()
        {
            var config = new SkinConfiguration();
            config.CustomComboColours.Add(Color4.Red);
            Assert.That(config.ComboColours, Is.EqualTo(new[] { Color4.Red }));
        }

        // CF: SkinConfiguration.ComboColours | CustomComboColours.Count > 0 | FALSE
        // CF: SkinConfiguration.ComboColours | AllowDefaultComboColoursFallback | TRUE
        [Test]
        public void TestFallsBackToDefaultColours()
        {
            Assert.That(new SkinConfiguration().ComboColours, Is.EqualTo(SkinConfiguration.DefaultComboColours));
        }

        // CF: SkinConfiguration.ComboColours | AllowDefaultComboColoursFallback | FALSE
        [Test]
        public void TestNoFallbackWhenDisallowed()
        {
            var config = new SkinConfiguration { AllowDefaultComboColoursFallback = false };
            Assert.That(config.ComboColours, Is.Null);
        }
    }
}
