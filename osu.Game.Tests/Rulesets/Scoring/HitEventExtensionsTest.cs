// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class HitEventExtensionsTest
    {
        private static HitObject objectWithWindows() => new HitObject { HitWindows = new DefaultHitWindows() };

        private static HitEvent hit(double offset, HitResult result = HitResult.Great, bool windows = true, double rate = 1)
            => new HitEvent(offset, rate, result, windows ? objectWithWindows() : new HitObject { HitWindows = HitWindows.Empty }, null, null);

        // CF: HitEventExtensions.AffectsUnstableRate | hitObject.HitWindows != Empty && result.IsHit() | TRUE
        // CF: HitEventExtensions.AffectsUnstableRate | hitObject.HitWindows != Empty | FALSE
        // CF: HitEventExtensions.AffectsUnstableRate | result.IsHit() | FALSE
        [Test]
        public void TestAffectsUnstableRate()
        {
            Assert.That(HitEventExtensions.AffectsUnstableRate(hit(5)), Is.True);
            Assert.That(HitEventExtensions.AffectsUnstableRate(hit(5, windows: false)), Is.False);
            Assert.That(HitEventExtensions.AffectsUnstableRate(hit(5, HitResult.Miss)), Is.False);
        }

        // CF: HitEventExtensions.CalculateUnstableRate | result ??= new | non-null
        // CF: HitEventExtensions.CalculateUnstableRate | EventCount == 0 | TRUE
        // CF: HitEventExtensions.CalculateUnstableRate | !AffectsUnstableRate(e) | TRUE
        // CF: HitEventExtensions.CalculateUnstableRate | for loop | loop 0
        [Test]
        public void TestUnstableRateWithNoRelevantEventsIsNull()
        {
            Assert.That(new List<HitEvent>().CalculateUnstableRate(), Is.Null);
            Assert.That(new List<HitEvent> { hit(5, HitResult.Miss), hit(5, windows: false) }.CalculateUnstableRate(), Is.Null);
        }

        // CF: HitEventExtensions.CalculateUnstableRate | EventCount == 0 | FALSE
        // CF: HitEventExtensions.CalculateUnstableRate | !AffectsUnstableRate(e) | FALSE
        // CF: HitEventExtensions.CalculateUnstableRate | for loop | loop n
        [Test]
        public void TestUnstableRateIsTenTimesStandardDeviation()
        {
            // offsets -10, 10 => mean 0, population standard deviation 10 => UR 100.
            var result = new List<HitEvent> { hit(-10), hit(10), hit(0, HitResult.Miss) }.CalculateUnstableRate();

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.EventCount, Is.EqualTo(2));
            Assert.That(result.Mean, Is.EqualTo(0).Within(1e-9));
            Assert.That(result.Result, Is.EqualTo(100).Within(1e-9));
        }

        [Test]
        public void TestUnstableRateOffsetsAreNormalisedByGameplayRate()
        {
            // at 1.5x rate raw offsets are scaled by 1.5; dividing makes them equal to the 1x case.
            var result = new List<HitEvent> { hit(-15, rate: 1.5), hit(15, rate: 1.5) }.CalculateUnstableRate();

            Assert.That(result!.Result, Is.EqualTo(100).Within(1e-9));
        }

        // CF: HitEventExtensions.CalculateUnstableRate | hitEvents.Count < LastProcessedIndex + 1 | FALSE
        // CF: HitEventExtensions.CalculateUnstableRate | hitEvents.Count < LastProcessedIndex + 1 | TRUE
        [Test]
        public void TestUnstableRateIncrementalAndRewind()
        {
            var events = new List<HitEvent> { hit(-10), hit(10) };
            var first = events.CalculateUnstableRate();

            events.Add(hit(-10));
            events.Add(hit(10));
            var incremental = events.CalculateUnstableRate(first);

            Assert.That(incremental, Is.SameAs(first));
            Assert.That(incremental!.EventCount, Is.EqualTo(4));
            Assert.That(incremental.Result, Is.EqualTo(100).Within(1e-9));

            // list got shorter than what was processed: state is discarded and recomputed.
            var rewound = events.Take(2).ToList().CalculateUnstableRate(incremental);
            Assert.That(rewound, Is.Not.SameAs(incremental));
            Assert.That(rewound!.EventCount, Is.EqualTo(2));
        }

        // CF: HitEventExtensions.UnstableRateCalculationResult.Result | EventCount == 0 | TRUE
        [Test]
        public void TestEmptyResultIsZero()
        {
            Assert.That(new HitEventExtensions.UnstableRateCalculationResult().Result, Is.Zero);
        }

        // CF: HitEventExtensions.CalculateAverageHitError | timeOffsets.Length == 0 | TRUE
        // CF: HitEventExtensions.CalculateAverageHitError | timeOffsets.Length == 0 | FALSE
        [Test]
        public void TestAverageHitError()
        {
            Assert.That(new List<HitEvent>().CalculateAverageHitError(), Is.Null);
            Assert.That(new[] { hit(10), hit(20), hit(99, HitResult.Miss) }.CalculateAverageHitError(), Is.EqualTo(15));
        }

        // CF: HitEventExtensions.CalculateMedianHitError | timeOffsets.Length == 0 | TRUE
        // CF: HitEventExtensions.CalculateMedianHitError | Length % 2 == 0 | TRUE
        // CF: HitEventExtensions.CalculateMedianHitError | Length % 2 == 0 | FALSE
        // CF: HitEventExtensions.CalculateMedianHitError | timeOffsets.Length == 0 | FALSE
        [Test]
        public void TestMedianHitError()
        {
            Assert.That(new List<HitEvent>().CalculateMedianHitError(), Is.Null);
            Assert.That(new[] { hit(30), hit(10), hit(20) }.CalculateMedianHitError(), Is.EqualTo(20), "odd count: middle value after sorting");
            Assert.That(new[] { hit(40), hit(10), hit(20), hit(30) }.CalculateMedianHitError(), Is.EqualTo(25), "even count: mean of the two central values");
        }
    }
}
