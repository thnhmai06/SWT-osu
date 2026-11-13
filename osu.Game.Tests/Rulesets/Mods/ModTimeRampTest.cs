// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Moq;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModTimeRampTest
    {
        private static Beatmap beatmap(params double[] times)
        {
            var b = new Beatmap();
            foreach (double t in times) b.HitObjects.Add(new HitCircle { StartTime = t });
            return b;
        }

        [Test]
        public void TestPreviewSpeedStartsAtFinalRate()
        {
            Assert.That(new ModWindUp().SpeedChange.Value, Is.EqualTo(1.5));
            Assert.That(new ModWindDown().SpeedChange.Value, Is.EqualTo(0.75));
        }

        // CF: ModTimeRamp.ApplyToBeatmap | beatmap.HitObjects.Any() | TRUE
        [Test]
        public void TestRateRampsLinearlyUntilThreeQuartersOfMappedLength()
        {
            var mod = new ModWindUp();
            mod.ApplyToBeatmap(beatmap(0, 1000));

            Assert.That(mod.ApplyToRate(-100), Is.EqualTo(1.0), "before the first object");
            Assert.That(mod.ApplyToRate(0), Is.EqualTo(1.0));
            Assert.That(mod.ApplyToRate(375), Is.EqualTo(1.25).Within(1e-9));
            Assert.That(mod.ApplyToRate(100), Is.EqualTo(1.07).Within(1e-9), "rounded to 2 decimals");
            Assert.That(mod.ApplyToRate(750), Is.EqualTo(1.5).Within(1e-9));
            Assert.That(mod.ApplyToRate(5000), Is.EqualTo(1.5), "clamped at the final rate");
            Assert.That(mod.ApplyToRate(375, 2), Is.EqualTo(2.5).Within(1e-9), "incoming rate is multiplied");
        }

        [Test]
        public void TestWindDownRampsDown()
        {
            var mod = new ModWindDown();
            mod.ApplyToBeatmap(beatmap(0, 1000));

            Assert.That(mod.ApplyToRate(0), Is.EqualTo(1.0));
            Assert.That(mod.ApplyToRate(750), Is.EqualTo(0.75).Within(1e-9));
        }

        [Test]
        public void TestRampStartsAtFirstObject()
        {
            var mod = new ModWindUp();
            mod.ApplyToBeatmap(beatmap(1000, 5000));

            Assert.That(mod.ApplyToRate(1000), Is.EqualTo(1.0));
            Assert.That(mod.ApplyToRate(1000 + 0.75 * 4000), Is.EqualTo(1.5).Within(1e-9));
        }

        // CF: ModTimeRamp.ApplyToBeatmap | beatmap.HitObjects.Any() | FALSE
        [Test]
        public void TestEmptyBeatmapRampsAlmostInstantly()
        {
            var mod = new ModWindUp();
            mod.ApplyToBeatmap(beatmap());

            Assert.That(mod.ApplyToRate(0), Is.EqualTo(1.0));
            Assert.That(mod.ApplyToRate(10), Is.EqualTo(1.5));
        }

        [Test]
        public void TestApplyToBeatmapResetsSpeedChange()
        {
            var mod = new ModWindUp();

            mod.ApplyToBeatmap(beatmap(0, 1000));

            Assert.That(mod.SpeedChange.Value, Is.EqualTo(mod.SpeedChange.Default));
        }

        // CF: ModWindUp.ctor | val.NewValue >= FinalRate.Value | TRUE
        [Test]
        public void TestWindUpRaisesFinalRateWhenInitialCatchesUp()
        {
            var mod = new ModWindUp();
            mod.InitialRate.Value = 1.6;

            Assert.That(mod.FinalRate.Value, Is.EqualTo(1.61).Within(1e-9));
        }

        // CF: ModWindUp.ctor | val.NewValue >= FinalRate.Value | FALSE
        // CF: ModWindUp.ctor | val.NewValue <= InitialRate.Value | FALSE
        [Test]
        public void TestWindUpLeavesRatesAloneWhenOrdered()
        {
            var mod = new ModWindUp();
            mod.InitialRate.Value = 1.2;
            mod.FinalRate.Value = 1.8;

            Assert.That((mod.InitialRate.Value, mod.FinalRate.Value), Is.EqualTo((1.2, 1.8)));
        }

        // CF: ModWindUp.ctor | val.NewValue <= InitialRate.Value | TRUE
        [Test]
        public void TestWindUpLowersInitialRateWhenFinalDropsBelow()
        {
            var mod = new ModWindUp();
            mod.FinalRate.Value = 0.9;

            Assert.That(mod.InitialRate.Value, Is.EqualTo(0.89).Within(1e-9));
        }

        // CF: ModWindDown.ctor | val.NewValue <= FinalRate.Value | TRUE
        [Test]
        public void TestWindDownLowersFinalRateWhenInitialDropsBelow()
        {
            var mod = new ModWindDown();
            mod.InitialRate.Value = 0.7;

            Assert.That(mod.FinalRate.Value, Is.EqualTo(0.69).Within(1e-9));
        }

        // CF: ModWindDown.ctor | val.NewValue <= FinalRate.Value | FALSE
        // CF: ModWindDown.ctor | val.NewValue >= InitialRate.Value | FALSE
        [Test]
        public void TestWindDownLeavesRatesAloneWhenOrdered()
        {
            var mod = new ModWindDown();
            mod.InitialRate.Value = 1.5;
            mod.FinalRate.Value = 0.6;

            Assert.That((mod.InitialRate.Value, mod.FinalRate.Value), Is.EqualTo((1.5, 0.6)));
        }

        // CF: ModWindDown.ctor | val.NewValue >= InitialRate.Value | TRUE
        [Test]
        public void TestWindDownRaisesInitialRateWhenFinalRisesAbove()
        {
            var mod = new ModWindDown();
            mod.FinalRate.Value = 1.2;

            Assert.That(mod.InitialRate.Value, Is.EqualTo(1.21).Within(1e-9));
        }

        // CF: ModTimeRamp.SettingDescription | !InitialRate.IsDefault || !FinalRate.IsDefault | FALSE
        // CF: ModTimeRamp.SettingDescription | !AdjustPitch.IsDefault | FALSE
        [Test]
        public void TestNoSettingDescriptionByDefault()
        {
            Assert.That(new ModWindUp().SettingDescription, Is.Empty);
        }

        // CF: ModTimeRamp.SettingDescription | !InitialRate.IsDefault || !FinalRate.IsDefault | TRUE
        // CF: ModTimeRamp.SettingDescription | !AdjustPitch.IsDefault | TRUE
        [Test]
        public void TestSettingDescriptionWhenChanged()
        {
            var mod = new ModWindUp();
            mod.InitialRate.Value = 1.2;
            mod.AdjustPitch.Value = false;

            var description = mod.SettingDescription.ToArray();

            Assert.That(description, Has.Length.EqualTo(2));
            Assert.That(description[0].setting.ToString(), Is.EqualTo("Speed change"));
            Assert.That(description[1].setting.ToString(), Is.EqualTo("Adjust pitch"));
            Assert.That(description[1].value.ToString(), Is.EqualTo("Off"));
        }

        [Test]
        public void TestIncompatibleMods()
        {
            Assert.That(new ModWindUp().IncompatibleMods, Is.SupersetOf(new[] { typeof(ModRateAdjust), typeof(ModAdaptiveSpeed), typeof(ModWindDown) }));
            Assert.That(new ModWindDown().IncompatibleMods, Contains.Item(typeof(ModWindUp)));
        }

        [Test]
        public void TestApplyToTrackAppliesFrequencyAdjustmentWithPitchAdjust()
        {
            var mod = new ModWindUp();
            var track = new Mock<IAdjustableAudioComponent>();

            mod.ApplyToTrack(track.Object);

            track.Verify(t => t.AddAdjustment(AdjustableProperty.Frequency, mod.SpeedChange), Times.Once);
            track.Verify(t => t.AddAdjustment(AdjustableProperty.Tempo, It.IsAny<osu.Framework.Bindables.IBindable<double>>()), Times.Never);
        }

        [Test]
        public void TestTogglingPitchAdjustSwitchesToTempo()
        {
            var mod = new ModWindUp();
            var track = new Mock<IAdjustableAudioComponent>();
            mod.ApplyToTrack(track.Object);

            mod.AdjustPitch.Value = false;

            track.Verify(t => t.RemoveAdjustment(AdjustableProperty.Frequency, mod.SpeedChange), Times.AtLeastOnce);
            track.Verify(t => t.AddAdjustment(AdjustableProperty.Tempo, mod.SpeedChange), Times.Once);
        }

        [Test]
        public void TestApplyToSampleAddsFrequencyAdjustment()
        {
            var mod = new ModWindUp();
            var sample = new Mock<IAdjustableAudioComponent>();

            mod.ApplyToSample(sample.Object);

            sample.Verify(s => s.AddAdjustment(AdjustableProperty.Frequency, mod.SpeedChange), Times.Once);
        }
    }
}
