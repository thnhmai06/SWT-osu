// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty.Utils
{
    [TestFixture]
    public class LegacyScoreUtilsTest
    {
        private static Beatmap<OsuHitObject> beatmapOf(params OsuHitObject[] objects)
        {
            var beatmap = new Beatmap<OsuHitObject>();
            beatmap.HitObjects.AddRange(objects);
            return beatmap;
        }

        private static Slider slider(int repeats)
        {
            var s = new Slider
            {
                StartTime = 0,
                RepeatCount = repeats,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(300, 0) }),
            };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return s;
        }

        // CF: CalculateNestedScorePerObject | foreach over empty/other objects -> switch matches neither case
        [Test]
        public void TestCirclesContributeNoNestedScore()
        {
            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmapOf(new HitCircle()), 1), Is.EqualTo(0));
        }

        // CF: CalculateNestedScorePerObject | case Slider -> 2 + repeats big ticks (30 each) plus small ticks (10 each)
        [Test]
        public void TestSliderContributesBigAndSmallTicks()
        {
            var s = slider(repeats: 2);
            int smallTicks = s.NestedHitObjects.OfType<SliderTick>().Count();
            Assert.That(smallTicks, Is.GreaterThan(0), "test slider must produce ticks");

            double expected = (4 * 30 + smallTicks * 10) / 3.0;
            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmapOf(s), 3), Is.EqualTo(expected).Within(1e-9));
        }

        // CF: CalculateNestedScorePerObject | case Spinner (long) -> spin score plus positive bonus spins
        [Test]
        public void TestLongSpinnerContributesSpinAndBonusScore()
        {
            var spinner = new Spinner { StartTime = 0, Duration = 1000 };

            // 15 possible half spins -> 7 full spins (700) + (15 - 6) / 2 = 4 bonus spins reduced by 7 / 2 = 3 -> 1 bonus spin (1000).
            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmapOf(spinner), 1), Is.EqualTo(1700));
        }

        // CF: calculateSpinnerScore | Math.Max(0, bonusSpins - fullSpins / 2) clamps negative bonus to zero
        [Test]
        public void TestShortSpinnerHasNoBonusScore()
        {
            var spinner = new Spinner { StartTime = 0, Duration = 100 };

            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmapOf(spinner), 1), Is.EqualTo(0));
        }

        // CF: CalculateNestedScorePerObject | result is averaged by objectCount
        [Test]
        public void TestScoreIsDividedByObjectCount()
        {
            var spinner = new Spinner { StartTime = 0, Duration = 1000 };

            Assert.That(LegacyScoreUtils.CalculateNestedScorePerObject(beatmapOf(spinner), 4), Is.EqualTo(425));
        }

        // CF: CalculateDifficultyPeppyStars | objectCount > 0 FALSE -> drain length stays 0
        [Test]
        public void TestPeppyStarsForEmptyBeatmapUsesZeroDrain()
        {
            var beatmap = beatmapOf();

            Assert.That(LegacyScoreUtils.CalculateDifficultyPeppyStars(beatmap),
                Is.EqualTo(LegacyRulesetExtensions.CalculateDifficultyPeppyStars(beatmap.Difficulty, 0, 0)));
        }

        // CF: CalculateDifficultyPeppyStars | objectCount > 0 TRUE -> drain = (last - first - breaks) / 1000, breaks subtracted
        [Test]
        public void TestPeppyStarsSubtractsBreaksFromDrainLength()
        {
            var beatmap = beatmapOf(new HitCircle { StartTime = 0 }, new HitCircle { StartTime = 120000 });
            beatmap.Breaks.Add(new BreakPeriod(10000, 20000));

            Assert.That(LegacyScoreUtils.CalculateDifficultyPeppyStars(beatmap),
                Is.EqualTo(LegacyRulesetExtensions.CalculateDifficultyPeppyStars(beatmap.Difficulty, 2, 110)));
        }

        // CF: CalculateDifficultyPeppyStars | objectCount > 0 TRUE, no breaks -> whole span is drain
        [Test]
        public void TestPeppyStarsWithoutBreaksUsesWholeSpan()
        {
            var beatmap = beatmapOf(new HitCircle { StartTime = 0 }, new HitCircle { StartTime = 120000 });

            Assert.That(LegacyScoreUtils.CalculateDifficultyPeppyStars(beatmap),
                Is.EqualTo(LegacyRulesetExtensions.CalculateDifficultyPeppyStars(beatmap.Difficulty, 2, 120)));
        }
    }
}
