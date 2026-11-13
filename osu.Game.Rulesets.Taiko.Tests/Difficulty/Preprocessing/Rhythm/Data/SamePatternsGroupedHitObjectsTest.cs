// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Rhythm.Data
{
    [TestFixture]
    public class SamePatternsGroupedHitObjectsTest
    {
        // Groups of 1 object each, at 100ms spacing; the nth group starts 100ms * n after the first.
        private static List<SameRhythmHitObjectGrouping> groups(int count)
        {
            var o = DifficultyObjectFactory.Create(Enumerable.Repeat((100d, HitType.Centre), count).ToArray());
            var result = new List<SameRhythmHitObjectGrouping>();
            foreach (var obj in o)
                result.Add(new SameRhythmHitObjectGrouping(result.LastOrDefault(), new List<osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.TaikoDifficultyHitObject> { obj }));
            return result;
        }

        // CF: SamePatternsGroupedHitObjects.GroupInterval | Groups.Count > 1 | FALSE
        // CF: SamePatternsGroupedHitObjects.IntervalRatio | Previous?.GroupInterval ?? 1.0 | null
        [Test]
        public void TestSingleGroupUsesFirstIntervalAndNoPreviousRatio()
        {
            var g = groups(1);
            var pattern = new SamePatternsGroupedHitObjects(null, g);
            Assert.That(pattern.GroupInterval, Is.EqualTo(double.PositiveInfinity));
            Assert.That(pattern.IntervalRatio, Is.EqualTo(1.0));
            Assert.That(pattern.FirstHitObject, Is.SameAs(g[0].FirstHitObject));
        }

        // CF: SamePatternsGroupedHitObjects.GroupInterval | Groups.Count > 1 | TRUE
        // CF: SamePatternsGroupedHitObjects.IntervalRatio | Previous?.GroupInterval ?? 1.0 | non-null
        [Test]
        public void TestMultipleGroupsUseSecondIntervalAndRatioToPrevious()
        {
            var g = groups(5);
            var previous = new SamePatternsGroupedHitObjects(null, g.Take(2).ToList());
            var pattern = new SamePatternsGroupedHitObjects(previous, g.Skip(2).Take(3).ToList());

            Assert.That(pattern.GroupInterval, Is.EqualTo(100));
            Assert.That(previous.GroupInterval, Is.EqualTo(100));
            Assert.That(pattern.IntervalRatio, Is.EqualTo(1.0));
            Assert.That(pattern.AllHitObjects.Count(), Is.EqualTo(3));
        }
    }
}
