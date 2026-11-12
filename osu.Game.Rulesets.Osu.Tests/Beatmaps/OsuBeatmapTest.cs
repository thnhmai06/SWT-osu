// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Tests.Beatmaps
{
    [TestFixture]
    public class OsuBeatmapTest
    {
        private static OsuBeatmap create(int circles, int sliders, int spinners)
        {
            var beatmap = new OsuBeatmap();

            for (int i = 0; i < circles; i++) beatmap.HitObjects.Add(new HitCircle());
            for (int i = 0; i < sliders; i++) beatmap.HitObjects.Add(new Slider());
            for (int i = 0; i < spinners; i++) beatmap.HitObjects.Add(new Spinner());

            return beatmap;
        }

        [Test]
        public void TestStatistics_CountsEachObjectKind()
        {
            var stats = create(3, 1, 2).GetStatistics().ToList();

            Assert.That(stats.Select(s => s.Content), Is.EqualTo(new[] { "3", "1", "2" }));
        }

        [Test]
        public void TestStatistics_BarLengthsRelativeToCirclesPlusSliders()
        {
            // sum = Max(1, 3 + 1) = 4; spinner bar = Min(2 / 10, 1)
            var stats = create(3, 1, 2).GetStatistics().ToList();

            Assert.That(stats[0].BarDisplayLength, Is.EqualTo(0.75f).Within(1e-6));
            Assert.That(stats[1].BarDisplayLength, Is.EqualTo(0.25f).Within(1e-6));
            Assert.That(stats[2].BarDisplayLength, Is.EqualTo(0.2f).Within(1e-6));
        }

        [Test]
        public void TestStatistics_EmptyBeatmap_AvoidsDivisionByZero()
        {
            // Math.Max(1, 0) picks the constant 1
            var stats = create(0, 0, 0).GetStatistics().ToList();

            Assert.That(stats.Select(s => s.BarDisplayLength), Is.All.EqualTo(0f));
        }

        [Test]
        public void TestStatistics_ManySpinners_BarCappedAtOne()
        {
            // Math.Min(spinners / 10, 1): constant 1 wins at 20 spinners
            var stats = create(0, 0, 20).GetStatistics().ToList();

            Assert.That(stats[2].BarDisplayLength, Is.EqualTo(1f));
        }
    }
}
