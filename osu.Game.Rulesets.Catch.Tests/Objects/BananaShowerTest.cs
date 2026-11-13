// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Scoring;
using System;

namespace osu.Game.Rulesets.Catch.Tests.Objects
{
    [TestFixture]
    public class BananaShowerTest
    {
        private static BananaShower create(double startTime, double duration, CancellationToken token = default)
        {
            var shower = new BananaShower { StartTime = startTime, Duration = duration };
            shower.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty(), token);
            return shower;
        }

        [Test]
        public void TestEndTimeAndDurationAreLinked()
        {
            var shower = new BananaShower { StartTime = 100, Duration = 50 };
            Assert.That(shower.EndTime, Is.EqualTo(150));

            shower.EndTime = 400;
            Assert.That(shower.Duration, Is.EqualTo(300));
        }

        [Test]
        public void TestIsLastInComboAndIgnoredByJudgement()
        {
            var shower = new BananaShower();
            Assert.That(shower.LastInCombo, Is.True);
            Assert.That(shower.CreateJudgement().MaxResult, Is.EqualTo(HitResult.IgnoreHit));
        }

        // CF: BananaShower.createBananas | spacing <= 0 | TRUE
        [Test]
        public void TestZeroDurationCreatesNoBananas()
        {
            Assert.That(create(1000, 0).NestedHitObjects, Is.Empty);
        }

        // CF: BananaShower.createBananas | spacing <= 0 | FALSE
        // CF: BananaShower.createBananas | spacing > 100 | loop 0
        // CF: BananaShower.createBananas | for time <= endTime | loop n
        [Test]
        public void TestShortDurationKeepsSpacing()
        {
            var shower = create(1000, 100);

            Assert.That(shower.NestedHitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 1000.0, 1100.0 }));
            Assert.That(shower.NestedHitObjects.Cast<Banana>().Select(b => b.BananaIndex), Is.EqualTo(new[] { 0, 1 }));
        }

        // CF: BananaShower.createBananas | spacing > 100 | loop 1
        // CF: BananaShower.createBananas | spacing > 100 | loop n
        [Test]
        public void TestLongDurationHalvesSpacingUntilAtMostOneHundred()
        {
            var shower = create(0, 1000);

            // 1000 -> 500 -> 250 -> 125 -> 62.5
            Assert.That(shower.NestedHitObjects, Has.Count.EqualTo(17));
            Assert.That(shower.NestedHitObjects[1].StartTime, Is.EqualTo(62.5));
            Assert.That(shower.NestedHitObjects[16].StartTime, Is.EqualTo(1000));
        }

        // CF: BananaShower.createBananas | for time <= endTime | loop 1
        [Test]
        public void TestSubMillisecondDurationCreatesSingleBanana()
        {
            // start and end are truncated to the same integer millisecond, as in osu!stable.
            Assert.That(create(0.9, 0.05).NestedHitObjects, Has.Count.EqualTo(1));
        }

        // CF: BananaShower.createBananas | cancellationToken.ThrowIfCancellationRequested | throws OperationCanceledException
        [Test]
        public void TestCancellationThrows()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() => create(0, 1000, source.Token));
        }
    }
}
