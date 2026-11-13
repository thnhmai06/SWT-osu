// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Moq;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class RateAdjustModHelperTest
    {
        // CF: RateAdjustModHelper.ApplyToTrack | adjustPitch == null | TRUE
        [Test]
        public void TestApplyToTrackBeforeHandlingAdjustmentsThrows()
        {
            var helper = new RateAdjustModHelper(new BindableDouble(1.5));

            Assert.That(() => helper.ApplyToTrack(new Mock<IAdjustableAudioComponent>().Object), Throws.InstanceOf<InvalidOperationException>());
        }

        // CF: RateAdjustModHelper.ApplyToTrack | adjustPitch == null | FALSE
        // CF: RateAdjustModHelper.HandleAudioAdjustments | adjustPitchSettingValue | TRUE
        [Test]
        public void TestPitchAdjustUsesFrequency()
        {
            var speed = new BindableDouble(1.5);
            var helper = new RateAdjustModHelper(speed);
            var track = new Mock<IAdjustableAudioComponent>();

            helper.HandleAudioAdjustments(new BindableBool(true));
            helper.ApplyToTrack(track.Object);

            track.Verify(t => t.AddAdjustment(AdjustableProperty.Frequency, speed), Times.Once);
            track.Verify(t => t.AddAdjustment(AdjustableProperty.Tempo, It.IsAny<IBindable<double>>()), Times.Never);
        }

        // CF: RateAdjustModHelper.HandleAudioAdjustments | adjustPitchSettingValue | FALSE
        [Test]
        public void TestNoPitchAdjustUsesTempo()
        {
            var speed = new BindableDouble(1.5);
            var helper = new RateAdjustModHelper(speed);
            var track = new Mock<IAdjustableAudioComponent>();

            helper.HandleAudioAdjustments(new BindableBool(false));
            helper.ApplyToTrack(track.Object);

            track.Verify(t => t.AddAdjustment(AdjustableProperty.Tempo, speed), Times.Once);
        }

        // CF: RateAdjustModHelper.HandleAudioAdjustments | track?.RemoveAdjustment | null
        [Test]
        public void TestTogglingBeforeTrackIsAppliedDoesNothing()
        {
            var helper = new RateAdjustModHelper(new BindableDouble(1.5));
            var pitch = new BindableBool(true);
            helper.HandleAudioAdjustments(pitch);

            Assert.That(() => pitch.Value = false, Throws.Nothing);
        }

        [Test]
        public void TestSpeedChangeExposed()
        {
            var speed = new BindableDouble(1.5);

            Assert.That(new RateAdjustModHelper(speed).SpeedChange, Is.SameAs(speed));
        }
    }
}
