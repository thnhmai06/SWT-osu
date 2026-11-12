// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class LegacyScoreUtilsTest
    {
        private static OsuBeatmap beatmap(params OsuHitObject[] objects)
        {
            var b = new OsuBeatmap();
            foreach (var o in objects) b.HitObjects.Add(o);
            return b;
        }

        private static Slider slider(int repeats)
        {
            var s = new Slider
            {
                StartTime = 0,
                RepeatCount = repeats,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(400, 0) })
            };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { SliderMultiplier = 2, SliderTickRate = 1 });
            return s;
        }

        private static Spinner spinner(double duration) => new Spinner { StartTime = 0, Duration = duration };

        // ---- CalculateNestedScorePerObject: switch over object type ----

        [Test]
        public void TestNestedScore_CircleOnly_IsZero()
        {
            // switch: neither Slider nor Spinner
            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmap(new HitCircle(), new HitCircle()), 2), Is.EqualTo(0));
        }

        [Test]
        public void TestNestedScore_NoObjects_IsZero()
        {
            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmap(), 1), Is.EqualTo(0));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void TestNestedScore_Slider_CountsHeadTailRepeatsAndTicks(int repeats)
        {
            // Slider case: (2 + repeats) big ticks worth 30, plus every SliderTick worth 10
            var s = slider(repeats);
            int ticks = s.NestedHitObjects.Count(h => h is SliderTick);

            double expected = ((2 + repeats) * 30 + ticks * 10) / 4.0;

            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmap(s), 4), Is.EqualTo(expected).Within(1e-9));
            Assert.That(ticks, Is.GreaterThan(0));
        }

        // Hand-derived (max 7.95 rotations/s, 3 required rotations/s, +3 half spins before bonus):
        //  1000ms => 15 half spins, 7 full (700), 4 bonus - 3 averaged away = 1 (1000) => 1700
        //  2000ms => 31 half spins, 15 full (1500), 11 bonus - 7 = 4 (4000) => 5500
        [TestCase(1000, 1700)]
        [TestCase(2000, 5500)]
        public void TestNestedScore_Spinner_CountsSpinAndBonusScore(double duration, double expected)
        {
            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmap(spinner(duration)), 1), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void TestNestedScore_ShortSpinner_BonusClampedToZero()
        {
            // 100ms => 1 half spin possible; (1 - 3) / 2 is negative => Math.Max(0, ...) picks 0; no full spins either
            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmap(spinner(100)), 1), Is.EqualTo(0));
        }

        [Test]
        public void TestNestedScore_MixedObjects_AreAveragedOverObjectCount()
        {
            var s = slider(0);
            int ticks = s.NestedHitObjects.Count(h => h is SliderTick);

            double total = (2 * 30 + ticks * 10) + 1700;

            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmap(new HitCircle(), s, spinner(1000)), 3), Is.EqualTo(total / 3).Within(1e-9));
        }

        // ---- CalculateDifficultyPeppyStars ----

        [Test]
        public void TestPeppyStars_EmptyBeatmap_DoesNotUseObjects()
        {
            // objectCount > 0 FALSE => drainLength stays 0
            Assert.That(LegacyScoreUtils.CalculateDifficultyPeppyStars(beatmap()), Is.InRange(0, 5));
        }

        [Test]
        public void TestPeppyStars_BreaksShortenDrainTimeAndRaiseStars()
        {
            // 100 objects over 120s; a 100s break leaves 20s of drain => much denser, so the star rating cannot be lower
            OsuBeatmap build(bool withBreak)
            {
                var b = new OsuBeatmap();
                for (int i = 0; i < 100; i++)
                    b.HitObjects.Add(new HitCircle { StartTime = i * 1212.12 });
                if (withBreak)
                    b.Breaks.Add(new BreakPeriod(10000, 110000));
                return b;
            }

            int noBreak = LegacyScoreUtils.CalculateDifficultyPeppyStars(build(false));
            int withBreak = LegacyScoreUtils.CalculateDifficultyPeppyStars(build(true));

            Assert.That(withBreak, Is.GreaterThan(noBreak));
        }
    }
}
