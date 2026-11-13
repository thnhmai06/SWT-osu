// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Utils;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Utils
{
    [TestFixture]
    public class IntervalGroupingUtilsTest
    {
        private record Item(int Id, double Interval) : IHasInterval;

        private static int[][] group(params double[] intervals)
        {
            var items = intervals.Select((interval, i) => new Item(i, interval)).ToList();
            return IntervalGroupingUtils.GroupByInterval(items).Select(g => g.Select(x => x.Id).ToArray()).ToArray();
        }

        // CF: IntervalGroupingUtils.GroupByInterval | while (i < objects.Count) | zero iterations (empty input)
        [Test]
        public void TestEmptyInputYieldsNoGroups()
        {
            Assert.That(IntervalGroupingUtils.GroupByInterval(new List<Item>()), Is.Empty);
        }

        // CF: createNextGroup | for (i < Count - 1) FALSE immediately; Count > 2 FALSE -> singleton group
        [Test]
        public void TestSingleObjectFormsOneGroup()
        {
            Assert.That(group(100), Is.EqualTo(new[] { new[] { 0 } }));
        }

        // CF: createNextGroup | Count > 2 FALSE with two objects -> each object is its own group
        [Test]
        public void TestTwoObjectsFormSeparateGroups()
        {
            Assert.That(group(100, 100), Is.EqualTo(new[] { new[] { 0 }, new[] { 1 } }));
        }

        // CF: createNextGroup | AlmostEquals TRUE throughout (no change branch); final Count > 2 / i < Count / AlmostEquals all TRUE
        [Test]
        public void TestFlatIntervalsFormSingleGroup()
        {
            Assert.That(group(100, 100, 100, 100), Is.EqualTo(new[] { new[] { 0, 1, 2, 3 } }));
        }

        // CF: createNextGroup | !AlmostEquals TRUE, next interval > current + margin FALSE (decrease) -> group ends before changed object
        [Test]
        public void TestIntervalDecreaseEndsGroupWithoutIncludingChangedObject()
        {
            Assert.That(group(100, 100, 100, 50, 50), Is.EqualTo(new[] { new[] { 0, 1 }, new[] { 2, 3, 4 } }));
        }

        // CF: createNextGroup | !AlmostEquals TRUE, next interval > current + margin TRUE (increase) -> group includes current object
        [Test]
        public void TestIntervalIncreaseIncludesObjectBeforeChange()
        {
            Assert.That(group(100, 100, 100, 200, 200), Is.EqualTo(new[] { new[] { 0, 1, 2 }, new[] { 3, 4 } }));
        }

        // CF: createNextGroup | final check: Count > 2 TRUE, i < Count FALSE (loop already consumed all) -> trailing singleton
        [Test]
        public void TestTrailingObjectAfterIncreaseIsSingletonGroup()
        {
            Assert.That(group(100, 100, 100, 200), Is.EqualTo(new[] { new[] { 0, 1, 2 }, new[] { 3 } }));
        }

        // CF: createNextGroup | final check: Count > 2 TRUE, i < Count TRUE, AlmostEquals(last two) FALSE
        [Test]
        public void TestNonFlatTailDoesNotAbsorbLastObject()
        {
            Assert.That(group(100, 100, 100, 200, 300), Is.EqualTo(new[] { new[] { 0, 1, 2 }, new[] { 3 }, new[] { 4 } }));
        }

        // CF: createNextGroup | AlmostEquals boundary: difference exactly MARGIN_OF_ERROR counts as equal (no group split)
        [Test]
        public void TestIntervalDifferenceAtMarginIsStillSameGroup()
        {
            Assert.That(group(100, 105, 100, 105), Is.EqualTo(new[] { new[] { 0, 1, 2, 3 } }));
        }
    }
}
