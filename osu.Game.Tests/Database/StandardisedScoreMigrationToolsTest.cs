// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Database
{
    [TestFixture]
    public class StandardisedScoreMigrationToolsTest
    {
        private static readonly OsuRuleset ruleset = new OsuRuleset();

        private static ScoreInfo createScore(Dictionary<HitResult, int> statistics, Dictionary<HitResult, int> maximum, params Mod[] mods) => new ScoreInfo
        {
            Ruleset = ruleset.RulesetInfo,
            Statistics = statistics,
            MaximumStatistics = maximum,
            Mods = mods,
        };

        // CF: StandardisedScoreMigrationTools.ComputeAccuracy | maxBaseScore == 0 | TRUE
        [Test]
        public void TestAccuracyWithNoMaximumIsOne()
            => Assert.That(StandardisedScoreMigrationTools.ComputeAccuracy(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>(), ruleset.CreateScoreProcessor()), Is.EqualTo(1));

        // CF: StandardisedScoreMigrationTools.ComputeAccuracy | maxBaseScore == 0 | FALSE
        // CF: StandardisedScoreMigrationTools.ComputeAccuracy | kvp.Key.AffectsAccuracy() | TRUE
        // CF: StandardisedScoreMigrationTools.ComputeAccuracy | kvp.Key.AffectsAccuracy() | FALSE
        [Test]
        public void TestAccuracyIgnoresNonAccuracyResults()
        {
            // great = 300, ok = 100 base score each; combo-only results (e.g. bonus) do not affect accuracy.
            var statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 3, [HitResult.Ok] = 1, [HitResult.LargeBonus] = 5 };
            var maximum = new Dictionary<HitResult, int> { [HitResult.Great] = 4, [HitResult.LargeBonus] = 5 };

            double accuracy = StandardisedScoreMigrationTools.ComputeAccuracy(statistics, maximum, ruleset.CreateScoreProcessor());

            Assert.That(accuracy, Is.EqualTo(1000.0 / 1200.0).Within(1e-9));
        }

        // CF: StandardisedScoreMigrationTools.ComputeAccuracy | ScoreInfo overload | no throw
        [Test]
        public void TestAccuracyFromScoreInfo()
        {
            var score = createScore(new Dictionary<HitResult, int> { [HitResult.Great] = 1 }, new Dictionary<HitResult, int> { [HitResult.Great] = 2 });

            Assert.That(StandardisedScoreMigrationTools.ComputeAccuracy(score, ruleset.CreateScoreProcessor()), Is.EqualTo(0.5).Within(1e-9));
        }

        // CF: StandardisedScoreMigrationTools.ComputeRank | foreach mods | loop 0
        [Test]
        public void TestRankWithoutMods()
        {
            var processor = ruleset.CreateScoreProcessor();
            var stats = new Dictionary<HitResult, int>();

            Assert.That(StandardisedScoreMigrationTools.ComputeRank(1, stats, Array.Empty<Mod>(), processor), Is.EqualTo(ScoreRank.X));
            Assert.That(StandardisedScoreMigrationTools.ComputeRank(0.5, stats, Array.Empty<Mod>(), processor), Is.EqualTo(ScoreRank.D));
        }

        // CF: StandardisedScoreMigrationTools.ComputeRank | foreach mods | loop 1
        [Test]
        public void TestRankAdjustedByMod()
            => Assert.That(StandardisedScoreMigrationTools.ComputeRank(1, new Dictionary<HitResult, int>(), new Mod[] { new OsuModHidden() }, ruleset.CreateScoreProcessor()), Is.EqualTo(ScoreRank.XH));

        // CF: StandardisedScoreMigrationTools.ComputeRank | foreach mods | loop n
        [Test]
        public void TestRankIgnoresNonScoreProcessorMods()
            => Assert.That(StandardisedScoreMigrationTools.ComputeRank(1, new Dictionary<HitResult, int>(), new Mod[] { new OsuModHardRock(), new OsuModHidden() }, ruleset.CreateScoreProcessor()), Is.EqualTo(ScoreRank.XH));

        // CF: StandardisedScoreMigrationTools.ComputeRank | ScoreInfo overloads | no throw
        [Test]
        public void TestRankFromScoreInfoOverloads()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.Accuracy = 1;

            Assert.That(StandardisedScoreMigrationTools.ComputeRank(score), Is.EqualTo(ScoreRank.X));
            Assert.That(StandardisedScoreMigrationTools.ComputeRank(score, ruleset.CreateScoreProcessor()), Is.EqualTo(ScoreRank.X));
        }

        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | scoreInfo.TotalScoreVersion >= 30000017 | TRUE
        [Test]
        public void TestMultipliersUpToDateIsNoOp()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.TotalScoreVersion = 30000017;
            score.TotalScore = 5;

            StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers(score, new BeatmapDifficulty());

            Assert.That(score.TotalScore, Is.EqualTo(5));
        }

        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | scoreInfo.TotalScoreVersion >= 30000017 | FALSE
        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | TotalScoreWithoutMods == 0 | TRUE
        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | TotalScore > 0 | TRUE
        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | missing TotalScoreWithoutMods | throws InvalidOperationException
        [Test]
        public void TestMultipliersMissingScoreWithoutModsThrows()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.TotalScoreVersion = 1;
            score.TotalScore = 10;

            Assert.Throws<InvalidOperationException>(() => StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers(score, new BeatmapDifficulty()));
        }

        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | TotalScore > 0 | FALSE
        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | TotalScoreWithoutMods == 0 | FALSE
        [Test]
        public void TestMultipliersZeroScoreStaysZero()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.TotalScoreVersion = 1;

            Assert.DoesNotThrow(() => StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers(score, new BeatmapDifficulty()));
            Assert.That(score.TotalScore, Is.EqualTo(0));
        }

        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers | recalculates total score | no throw
        [Test]
        public void TestMultipliersAppliedToScoreWithoutMods()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.TotalScoreVersion = 1;
            score.TotalScoreWithoutMods = 1000;
            score.TotalScore = 1;

            StandardisedScoreMigrationTools.UpdateToLatestScoreMultipliers(score, new BeatmapDifficulty());

            Assert.That(score.TotalScore, Is.EqualTo(1000));
        }

        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoring | score.IsLegacyScore | FALSE
        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoring | score.TotalScoreWithoutMods > 0 | FALSE
        [Test]
        public void TestUpdateToLatestScoringNonLegacyPopulatesAccuracyAndRank()
        {
            var score = createScore(new Dictionary<HitResult, int> { [HitResult.Great] = 2 }, new Dictionary<HitResult, int> { [HitResult.Great] = 2 });
            score.TotalScoreVersion = 1;

            StandardisedScoreMigrationTools.UpdateToLatestScoring(score, new TestWorkingBeatmap(new TestBeatmap(ruleset.RulesetInfo)));

            Assert.That(score.Accuracy, Is.EqualTo(1));
            Assert.That(score.Rank, Is.EqualTo(ScoreRank.X));
            Assert.That(score.TotalScoreVersion, Is.EqualTo(LegacyScoreEncoder.LATEST_VERSION));
        }

        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoring | score.TotalScoreWithoutMods > 0 | TRUE
        [Test]
        public void TestUpdateToLatestScoringNonLegacyUpdatesMultipliers()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.TotalScoreVersion = 1;
            score.TotalScoreWithoutMods = 500;

            StandardisedScoreMigrationTools.UpdateToLatestScoring(score, new TestWorkingBeatmap(new TestBeatmap(ruleset.RulesetInfo)));

            Assert.That(score.TotalScore, Is.EqualTo(500));
        }

        // CF: StandardisedScoreMigrationTools.UpdateToLatestScoring | score.IsLegacyScore | TRUE
        // CF: StandardisedScoreMigrationTools.convertFromLegacyTotalScore | mods.Any(mod is ModScoreV2) | TRUE
        [Test]
        public void TestUpdateToLatestScoringLegacyScoreV2()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>(), new ModScoreV2());
            score.IsLegacyScore = true;
            score.LegacyTotalScore = 123;
            score.TotalScore = 400000;

            StandardisedScoreMigrationTools.UpdateFromLegacy(score, new TestWorkingBeatmap(new TestBeatmap(ruleset.RulesetInfo)));

            // ScoreV2 scores are already standardised-scale; osu!'s legacy multiplier for ScoreV2 alone is 1.
            Assert.That(score.TotalScore, Is.EqualTo(400000));
            Assert.That(score.TotalScoreWithoutMods, Is.EqualTo(400000));
        }

        // CF: StandardisedScoreMigrationTools.convertFromLegacyTotalScore | !score.IsLegacyScore | TRUE
        [Test]
        public void TestUpdateFromLegacyOnNonLegacyScoreKeepsTotals()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.TotalScore = 77;
            score.TotalScoreWithoutMods = 70;

            StandardisedScoreMigrationTools.UpdateFromLegacy(score, new TestWorkingBeatmap(new TestBeatmap(ruleset.RulesetInfo)));

            Assert.That(score.TotalScore, Is.EqualTo(77));
            Assert.That(score.TotalScoreWithoutMods, Is.EqualTo(70));
        }

        // CF: StandardisedScoreMigrationTools.convertFromLegacyTotalScore | playableBeatmap.HitObjects.Count == 0 | TRUE
        // CF: StandardisedScoreMigrationTools.convertFromLegacyTotalScore | empty beatmap | throws InvalidOperationException
        [Test]
        public void TestLegacyScoreOnEmptyBeatmapThrows()
        {
            var score = createScore(new Dictionary<HitResult, int>(), new Dictionary<HitResult, int>());
            score.IsLegacyScore = true;
            score.LegacyTotalScore = 100;

            var beatmap = new TestWorkingBeatmap(new TestBeatmap(ruleset.RulesetInfo, false));

            Assert.Throws<InvalidOperationException>(() => StandardisedScoreMigrationTools.UpdateFromLegacy(score, beatmap));
        }
    }
}
