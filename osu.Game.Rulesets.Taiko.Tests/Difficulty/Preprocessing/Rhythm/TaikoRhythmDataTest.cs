// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Rhythm
{
    [TestFixture]
    public class TaikoRhythmDataTest
    {
        private static double ratioOfSecond(double first, double second)
        {
            var objects = DifficultyObjectFactory.Create((first, HitType.Centre), (second, HitType.Centre));
            return objects[1].RhythmData.Ratio;
        }

        // CF: TaikoRhythmData.ctor | previous == null | TRUE
        [Test]
        public void TestFirstObjectHasRatioOne()
            => Assert.That(DifficultyObjectFactory.Create((100, HitType.Centre))[0].RhythmData.Ratio, Is.EqualTo(1));

        // CF: TaikoRhythmData.ctor | previous == null | FALSE
        // CF: TaikoRhythmData.ctor | common_ratios.MinBy | non-null
        [TestCase(100, 100, 1.0)]
        [TestCase(100, 200, 2.0)]
        [TestCase(200, 100, 0.5)]
        [TestCase(100, 310, 3.0)]
        [TestCase(300, 100, 1.0 / 3)]
        [TestCase(100, 140, 1.5)]
        [TestCase(300, 200, 2.0 / 3)]
        [TestCase(100, 125, 1.25)]
        [TestCase(100, 80, 0.8)]
        public void TestRatioSnapsToClosestCommonRatio(double first, double second, double expected)
            => Assert.That(ratioOfSecond(first, second), Is.EqualTo(expected).Within(1e-9));
    }
}
