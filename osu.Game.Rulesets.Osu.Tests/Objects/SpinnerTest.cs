// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Tests.Objects
{
    [TestFixture]
    public class SpinnerTest
    {
        private static Spinner spinner(double duration, CancellationToken token = default)
        {
            var s = new Spinner { StartTime = 1000, Duration = duration };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { OverallDifficulty = 5 }, token);
            return s;
        }

        [Test]
        public void TestEndTimeAccessors()
        {
            var s = new Spinner { StartTime = 1000, Duration = 500 };
            Assert.That(s.EndTime, Is.EqualTo(1500));

            s.EndTime = 2000;
            Assert.That(s.Duration, Is.EqualTo(1000));
        }

        // CF: Spinner.CreateNestedHitObjects | for (i < totalSpins) | loop n
        // CF: Spinner.CreateNestedHitObjects | i < SpinsRequiredForBonus | TRUE
        // CF: Spinner.CreateNestedHitObjects | i < SpinsRequiredForBonus | FALSE
        [Test]
        public void TestSpinsAndNestedObjectsForOneSecond()
        {
            // OD5: clear 150rpm = 2.5rps -> 2 spins; complete 380rpm ~ 6.33rps -> 6 total -> 6 - 2 - 2 = 2 bonus spins
            var s = spinner(1000);

            Assert.That(s.SpinsRequired, Is.EqualTo(2));
            Assert.That(s.SpinsRequiredForBonus, Is.EqualTo(4));
            Assert.That(s.MaximumBonusSpins, Is.EqualTo(2));
            Assert.That(s.NestedHitObjects.OfType<SpinnerTick>().Count(t => t is not SpinnerBonusTick), Is.EqualTo(4));
            Assert.That(s.NestedHitObjects.OfType<SpinnerBonusTick>().Count(), Is.EqualTo(2));
            Assert.That(s.NestedHitObjects.Last().StartTime, Is.EqualTo(s.EndTime).Within(1e-6));
        }

        // CF: Spinner.CreateNestedHitObjects | for (i < totalSpins) | loop 1
        // CF: Spinner.CreateNestedHitObjects | i < SpinsRequiredForBonus | TRUE
        [Test]
        public void TestZeroDurationStillHasMinimumTicks()
        {
            var s = spinner(0);

            Assert.That(s.SpinsRequired, Is.EqualTo(0));
            Assert.That(s.MaximumBonusSpins, Is.EqualTo(0));
            Assert.That(s.NestedHitObjects, Has.Count.EqualTo(2));
            Assert.That(s.NestedHitObjects, Has.All.TypeOf<SpinnerTick>());
        }

        // CF: Spinner.CreateNestedHitObjects | cancellationToken.ThrowIfCancellationRequested | throws OperationCanceledException
        [Test]
        public void TestCancelledTokenThrows()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => spinner(1000, cts.Token));
        }

        // CF: Spinner.CreateSpinningSamples | referenceSample == null | TRUE
        [Test]
        public void TestNoSamplesYieldsNoSpinningSamples()
        {
            Assert.That(new Spinner().CreateSpinningSamples(), Is.Empty);
        }

        // CF: Spinner.CreateSpinningSamples | referenceSample == null | FALSE
        [Test]
        public void TestSpinningSampleDerivedFromFirstSample()
        {
            var s = new Spinner { Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } };

            var samples = s.CreateSpinningSamples();

            Assert.That(samples, Has.Length.EqualTo(1));
            Assert.That(samples[0].Name, Is.EqualTo("spinnerspin"));
        }
    }
}
