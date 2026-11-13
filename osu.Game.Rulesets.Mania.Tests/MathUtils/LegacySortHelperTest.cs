// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mania.MathUtils;

namespace osu.Game.Rulesets.Mania.Tests.MathUtils
{
    [TestFixture]
    public class LegacySortHelperTest
    {
        private sealed class DescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y) => y.CompareTo(x);
        }

        // CF: LegacySortHelper.Sort | keys == null (ThrowIfNull) | throws ArgumentNullException
        [Test]
        public void TestNullKeysThrows()
        {
            Assert.Throws<ArgumentNullException>(() => LegacySortHelper<int>.Sort(null!, null!));
        }

        // CF: LegacySortHelper.Sort | keys.Length == 0 | TRUE
        [Test]
        public void TestEmptyReturnsUnchanged()
        {
            int[] keys = [];
            LegacySortHelper<int>.Sort(keys, null!);
            Assert.That(keys, Is.Empty);
        }

        // CF: LegacySortHelper.Sort | keys.Length == 0 | FALSE
        // CF: LegacySortHelper.Sort | comparer ??= (null) | TRUE
        // CF: LegacySortHelper.depthLimitedQuickSort | swapIfGreater a != b | FALSE
        // CF: LegacySortHelper.depthLimitedQuickSort | outer while left < right | FALSE
        [Test]
        public void TestSingleElementWithDefaultComparer()
        {
            int[] keys = [5];
            LegacySortHelper<int>.Sort(keys, null!);
            Assert.That(keys, Is.EqualTo(new[] { 5 }));
        }

        // CF: LegacySortHelper.Sort | comparer ??= (null) | FALSE
        // CF: LegacySortHelper.swapIfGreater | Compare(keys[a], keys[b]) > 0 | TRUE
        // CF: LegacySortHelper.depthLimitedQuickSort | i < j | TRUE
        [Test]
        public void TestTwoElementsReversedWithCustomComparer()
        {
            int[] keys = [1, 2];
            LegacySortHelper<int>.Sort(keys, new DescendingComparer());
            Assert.That(keys, Is.EqualTo(new[] { 2, 1 }));
        }

        // CF: LegacySortHelper.swapIfGreater | Compare(keys[a], keys[b]) > 0 | FALSE
        // CF: LegacySortHelper.depthLimitedQuickSort | while Compare(keys[i], x) < 0 | loop n
        // CF: LegacySortHelper.depthLimitedQuickSort | while Compare(x, keys[j]) < 0 | loop n
        // CF: LegacySortHelper.depthLimitedQuickSort | i > j | TRUE
        // CF: LegacySortHelper.depthLimitedQuickSort | j - left <= right - i | TRUE
        // CF: LegacySortHelper.depthLimitedQuickSort | j - left <= right - i | FALSE
        // CF: LegacySortHelper.depthLimitedQuickSort | left < j | TRUE
        // CF: LegacySortHelper.depthLimitedQuickSort | i < right | TRUE
        // CF: LegacySortHelper.depthLimitedQuickSort | outer while left < right | TRUE
        // CF: LegacySortHelper.depthLimitedQuickSort | depthLimit == 0 | FALSE
        [TestCase(new[] { 3, 1, 2 })]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        [TestCase(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 })]
        [TestCase(new[] { 5, 3, 5, 1, 3, 5, 1, 1, 9, 0, 5, 5, 2, 2 })]
        [TestCase(new[] { 4, 4, 4, 4, 4, 4 })]
        [TestCase(new[] { 9, 1, 8, 2, 7, 3, 6, 4, 5, 0, 12, 11, 15, 13, 14, 10 })]
        public void TestSortsAscending(int[] input)
        {
            int[] keys = (int[])input.Clone();
            LegacySortHelper<int>.Sort(keys, Comparer<int>.Default);
            Assert.That(keys, Is.EqualTo(input.OrderBy(k => k).ToArray()));
        }

        // CF: LegacySortHelper.depthLimitedQuickSort | depthLimit == 0 | TRUE
        // CF: LegacySortHelper.heapsort | for i = n/2; i >= 1 | loop n
        // CF: LegacySortHelper.heapsort | for i = n; i > 1 | loop n
        // CF: LegacySortHelper.downHeap | while i <= n/2 | loop n
        // CF: LegacySortHelper.downHeap | child < n && Compare(keys[child-1], keys[child]) < 0 | TRUE
        // CF: LegacySortHelper.downHeap | child < n && Compare(keys[child-1], keys[child]) < 0 | FALSE
        // CF: LegacySortHelper.downHeap | !(Compare(d, keys[child-1]) < 0) | TRUE
        // CF: LegacySortHelper.downHeap | !(Compare(d, keys[child-1]) < 0) | FALSE
        // CF: LegacySortHelper.downHeap | while i <= n/2 | loop 0
        [Test]
        public void TestAdversarialInputFallsBackToHeapsortAndStillSorts()
        {
            // McIlroy's "antiqsort" adversary: builds an input that forces quicksort pivots to be as bad as possible,
            // exhausting the depth limit so that the heapsort fallback is exercised.
            const int n = 2000;
            int[] input = createQuicksortKiller(n);

            int comparisons = 0;
            var counting = Comparer<int>.Create((a, b) =>
            {
                comparisons++;
                return a.CompareTo(b);
            });

            int[] keys = (int[])input.Clone();
            LegacySortHelper<int>.Sort(keys, counting);

            Assert.That(keys, Is.EqualTo(Enumerable.Range(0, n)));
            // pure quicksort on this input would need on the order of n^2 / 4 comparisons; the heapsort fallback bounds it.
            Assert.That(comparisons, Is.LessThan(n * n / 8));
        }

        private static int[] createQuicksortKiller(int n)
        {
            int gas = n;
            int nsolid = 0;
            int[] val = new int[n];
            Array.Fill(val, gas);
            int[] ptr = Enumerable.Range(0, n).ToArray();
            int candidate = 0;

            // sorts indices using the same quicksort via an adaptive comparer
            var adversary = Comparer<int>.Create((x, y) =>
            {
                if (val[x] == gas && val[y] == gas)
                {
                    if (x == candidate) val[x] = nsolid++;
                    else val[y] = nsolid++;
                }

                if (val[x] == gas) candidate = x;
                else if (val[y] == gas) candidate = y;

                return val[x].CompareTo(val[y]);
            });

            LegacySortHelper<int>.Sort(ptr, adversary);

            for (int i = 0; i < n; i++)
            {
                if (val[i] == gas) val[i] = nsolid++;
            }

            return val;
        }
    }
}
