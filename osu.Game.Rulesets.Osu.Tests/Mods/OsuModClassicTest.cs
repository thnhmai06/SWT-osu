// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModClassicTest
    {
        // CF: OsuModClassic.ApplyToHitObject | switch (hitObject) | case Slider
        [Test]
        public void TestSliderBehaviourFollowsSetting()
        {
            var slider = new Slider();

            new OsuModClassic { NoSliderHeadAccuracy = { Value = false } }.ApplyToHitObject(slider);
            Assert.That(slider.ClassicSliderBehaviour, Is.False);

            new OsuModClassic { NoSliderHeadAccuracy = { Value = true } }.ApplyToHitObject(slider);
            Assert.That(slider.ClassicSliderBehaviour, Is.True);
        }

        // CF: OsuModClassic.ApplyToHitObject | switch (hitObject) | default
        [Test]
        public void TestNonSliderIsIgnored()
        {
            Assert.DoesNotThrow(() => new OsuModClassic().ApplyToHitObject(new HitCircle()));
        }

        // CF: OsuModClassic.CreateHealthProcessor | ClassicHealth.Value | TRUE
        [Test]
        public void TestClassicHealthCreatesLegacyProcessor()
        {
            Assert.That(new OsuModClassic().CreateHealthProcessor(1000), Is.TypeOf<OsuLegacyHealthProcessor>());
        }

        // CF: OsuModClassic.CreateHealthProcessor | ClassicHealth.Value | FALSE
        [Test]
        public void TestNonClassicHealthCreatesNothing()
        {
            Assert.That(new OsuModClassic { ClassicHealth = { Value = false } }.CreateHealthProcessor(1000), Is.Null);
        }

        [Test]
        public void TestIncompatibleWithStrictTracking()
        {
            Assert.That(new OsuModClassic().IncompatibleMods, Does.Contain(typeof(OsuModStrictTracking)));
        }
    }
}
