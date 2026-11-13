// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class ScoreProcessorTest
    {
        private ScoreProcessor processor = null!;

        [SetUp]
        public void SetUp()
        {
            processor = new ScoreProcessor(new OsuRuleset());
        }

        private class TestJudgement : Judgement
        {
            private readonly HitResult max;

            public TestJudgement(HitResult max) => this.max = max;

            public override HitResult MaxResult => max;
        }

        private class TestHitObject : HitObject
        {
            private readonly HitResult max;

            public TestHitObject(HitResult max = HitResult.Great) => this.max = max;

            public override Judgement CreateJudgement() => new TestJudgement(max);
        }

        private static IBeatmap beatmapOf(int count, HitResult max = HitResult.Great)
        {
            var beatmap = new Beatmap();
            for (int i = 0; i < count; i++)
                beatmap.HitObjects.Add(new TestHitObject(max) { StartTime = i * 100 });
            return beatmap;
        }

        private JudgementResult apply(HitObject obj, HitResult type)
        {
            var result = new JudgementResult(obj, obj.Judgement) { Type = type };
            processor.ApplyResult(result);
            return result;
        }

        // CF: ScoreProcessor.RankFromScore | accuracy == accuracy_cutoff_x | TRUE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_s | TRUE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_a | TRUE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_b | TRUE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_c | TRUE
        // CF: ScoreProcessor.RankFromScore | accuracy == accuracy_cutoff_x | FALSE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_s | FALSE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_a | FALSE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_b | FALSE
        // CF: ScoreProcessor.RankFromScore | accuracy >= accuracy_cutoff_c | FALSE
        [TestCase(1.0, ScoreRank.X)]
        [TestCase(0.9999, ScoreRank.S)]
        [TestCase(0.95, ScoreRank.S)]
        [TestCase(0.9499, ScoreRank.A)]
        [TestCase(0.9, ScoreRank.A)]
        [TestCase(0.8999, ScoreRank.B)]
        [TestCase(0.8, ScoreRank.B)]
        [TestCase(0.7999, ScoreRank.C)]
        [TestCase(0.7, ScoreRank.C)]
        [TestCase(0.6999, ScoreRank.D)]
        [TestCase(0.0, ScoreRank.D)]
        public void TestRankFromScore(double accuracy, ScoreRank expected)
        {
            Assert.That(processor.RankFromScore(accuracy, new Dictionary<HitResult, int>()), Is.EqualTo(expected));
        }

        // CF: ScoreProcessor.AccuracyCutoffFromRank | case X | case X
        // CF: ScoreProcessor.AccuracyCutoffFromRank | case S | case S
        // CF: ScoreProcessor.AccuracyCutoffFromRank | case A | case A
        // CF: ScoreProcessor.AccuracyCutoffFromRank | case B | case B
        // CF: ScoreProcessor.AccuracyCutoffFromRank | case C | case C
        // CF: ScoreProcessor.AccuracyCutoffFromRank | case D | case D
        [TestCase(ScoreRank.X, 1.0)]
        [TestCase(ScoreRank.XH, 1.0)]
        [TestCase(ScoreRank.S, 0.95)]
        [TestCase(ScoreRank.SH, 0.95)]
        [TestCase(ScoreRank.A, 0.9)]
        [TestCase(ScoreRank.B, 0.8)]
        [TestCase(ScoreRank.C, 0.7)]
        [TestCase(ScoreRank.D, 0.0)]
        public void TestAccuracyCutoffFromRank(ScoreRank rank, double expected)
        {
            Assert.That(processor.AccuracyCutoffFromRank(rank), Is.EqualTo(expected));
        }

        // CF: ScoreProcessor.AccuracyCutoffFromRank | default | throws ArgumentOutOfRangeException
        [Test]
        public void TestAccuracyCutoffFromFailRankThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => processor.AccuracyCutoffFromRank(ScoreRank.F));
        }

        // CF: ScoreProcessor.GetBaseScoreForResult | case SmallTickHit | case SmallTickHit
        // CF: ScoreProcessor.GetBaseScoreForResult | case LargeTickHit | case LargeTickHit
        // CF: ScoreProcessor.GetBaseScoreForResult | case SliderTailHit | case SliderTailHit
        // CF: ScoreProcessor.GetBaseScoreForResult | case Meh | case Meh
        // CF: ScoreProcessor.GetBaseScoreForResult | case Ok | case Ok
        // CF: ScoreProcessor.GetBaseScoreForResult | case Good | case Good
        // CF: ScoreProcessor.GetBaseScoreForResult | case Great | case Great
        // CF: ScoreProcessor.GetBaseScoreForResult | case Perfect | case Perfect
        // CF: ScoreProcessor.GetBaseScoreForResult | case SmallBonus | case SmallBonus
        // CF: ScoreProcessor.GetBaseScoreForResult | case LargeBonus | case LargeBonus
        // CF: ScoreProcessor.GetBaseScoreForResult | default | default
        [TestCase(HitResult.SmallTickHit, 10)]
        [TestCase(HitResult.LargeTickHit, 30)]
        [TestCase(HitResult.SliderTailHit, 150)]
        [TestCase(HitResult.Meh, 50)]
        [TestCase(HitResult.Ok, 100)]
        [TestCase(HitResult.Good, 200)]
        [TestCase(HitResult.Great, 300)]
        [TestCase(HitResult.Perfect, 300)]
        [TestCase(HitResult.SmallBonus, 10)]
        [TestCase(HitResult.LargeBonus, 50)]
        [TestCase(HitResult.Miss, 0)]
        [TestCase(HitResult.None, 0)]
        public void TestBaseScoreForResult(HitResult result, int expected)
        {
            Assert.That(processor.GetBaseScoreForResult(result), Is.EqualTo(expected));
        }

        // CF: ScoreProcessor.MaximumStatistics | !beatmapApplied | throws InvalidOperationException
        [Test]
        public void TestMaximumStatisticsBeforeBeatmapThrows()
        {
            Assert.Throws<InvalidOperationException>(() => _ = processor.MaximumStatistics);
        }

        // CF: ScoreProcessor.MaximumStatistics | !beatmapApplied | FALSE
        // CF: ScoreProcessor.Reset | storeResults | TRUE
        // CF: ScoreProcessor.ApplyBeatmap | Beatmap.Value = beatmap | no throw
        // CF: ScoreProcessor.ApplyResultInternal | !IsSimulating | FALSE
        [Test]
        public void TestApplyBeatmapStoresMaximums()
        {
            processor.ApplyBeatmap(beatmapOf(3));

            Assert.That(processor.MaximumStatistics[HitResult.Great], Is.EqualTo(3));
            Assert.That(processor.MaximumCombo, Is.EqualTo(3));
            Assert.That(processor.MaximumTotalScore, Is.EqualTo(1_000_000));
            Assert.That(processor.HitEvents, Is.Empty, "simulated judgements do not produce hit events");
            Assert.That(processor.TotalScore.Value, Is.Zero);
            Assert.That(processor.Accuracy.Value, Is.EqualTo(1));
            Assert.That(processor.Combo.Value, Is.Zero);
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.X));
        }

        // CF: ScoreProcessor.ApplyResultInternal | result.FailedAtJudgement && !ApplyNewJudgementsWhenFailed | FALSE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.IncreasesCombo() | TRUE
        // CF: ScoreProcessor.ApplyResultInternal | result.Judgement.MaxResult.AffectsAccuracy() | TRUE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.AffectsAccuracy() | TRUE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.IsBonus() | FALSE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.IsScorable() | TRUE
        // CF: ScoreProcessor.ApplyResultInternal | !IsSimulating | TRUE
        // CF: ScoreProcessor.ApplyResultInternal | TrackHitEvents | TRUE
        [Test]
        public void TestPerfectPlayScoresMaximum()
        {
            var beatmap = beatmapOf(2);
            processor.ApplyBeatmap(beatmap);

            foreach (var obj in beatmap.HitObjects)
                apply(obj, HitResult.Great);

            Assert.That(processor.TotalScore.Value, Is.EqualTo(1_000_000));
            Assert.That(processor.Accuracy.Value, Is.EqualTo(1));
            Assert.That(processor.Combo.Value, Is.EqualTo(2));
            Assert.That(processor.HighestCombo.Value, Is.EqualTo(2));
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.X));
            Assert.That(processor.HitEvents.Count, Is.EqualTo(2));
            Assert.That(processor.HitEvents[1].LastHitObject, Is.SameAs(beatmap.HitObjects[0]));
            Assert.That(processor.Statistics[HitResult.Great], Is.EqualTo(2));
        }

        // CF: ScoreProcessor.ApplyResultInternal | result.Type.IncreasesCombo() | FALSE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.BreaksCombo() | TRUE
        // CF: ScoreProcessor.updateRank | rank.Value == ScoreRank.F | FALSE
        // CF: ScoreProcessor.updateScore | currentMaximumBaseScore > 0 | TRUE
        [Test]
        public void TestMissBreaksComboAndLowersAccuracyAndRank()
        {
            var beatmap = beatmapOf(2);
            processor.ApplyBeatmap(beatmap);

            apply(beatmap.HitObjects[0], HitResult.Great);
            apply(beatmap.HitObjects[1], HitResult.Miss);

            Assert.That(processor.Combo.Value, Is.Zero);
            Assert.That(processor.HighestCombo.Value, Is.EqualTo(1));
            Assert.That(processor.Accuracy.Value, Is.EqualTo(0.5));
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.D));
            Assert.That(processor.TotalScore.Value, Is.GreaterThan(0).And.LessThan(1_000_000));
            Assert.That(processor.MinimumAccuracy.Value, Is.EqualTo(0.5));
            Assert.That(processor.MaximumAccuracy.Value, Is.EqualTo(0.5));
        }

        // CF: ScoreProcessor.ApplyResultInternal | result.Type.IncreasesCombo() | FALSE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.BreaksCombo() | FALSE
        // CF: ScoreProcessor.ApplyResultInternal | result.Judgement.MaxResult.AffectsAccuracy() | FALSE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.AffectsAccuracy() | FALSE
        // CF: ScoreProcessor.ApplyResultInternal | result.Type.IsBonus() | TRUE
        // CF: ScoreProcessor.updateScore | currentMaximumBaseScore > 0 | FALSE
        // CF: ScoreProcessor.updateScore | maximumBaseScore > 0 | FALSE
        // CF: ScoreProcessor.updateScore | maximumComboPortion > 0 | FALSE
        // CF: ScoreProcessor.updateScore | maximumAccuracyJudgementCount > 0 | FALSE
        [Test]
        public void TestBonusOnlyAddsToScore()
        {
            var obj = new TestHitObject(HitResult.SmallBonus);
            apply(obj, HitResult.SmallBonus);

            Assert.That(processor.Combo.Value, Is.Zero);
            Assert.That(processor.Accuracy.Value, Is.EqualTo(1));
            Assert.That(processor.MinimumAccuracy.Value, Is.Zero);
            Assert.That(processor.MaximumAccuracy.Value, Is.EqualTo(1));
            Assert.That(processor.TotalScore.Value, Is.EqualTo(1_000_000 + 10), "no maxima yet, so progress is 1; plus the bonus base score");
        }

        // CF: ScoreProcessor.ApplyResultInternal | result.Type.IsScorable() | FALSE
        [Test]
        public void TestNonScorableResultOnlyCounted()
        {
            var obj = new TestHitObject(HitResult.IgnoreHit);
            apply(obj, HitResult.IgnoreHit);

            Assert.That(processor.Statistics[HitResult.IgnoreHit], Is.EqualTo(1));
            Assert.That(processor.TotalScore.Value, Is.EqualTo(1_000_000));
            Assert.That(processor.GetScoreProcessorStatistics().ComboPortion, Is.Zero);
        }

        // CF: ScoreProcessor.ApplyResultInternal | result.FailedAtJudgement && !ApplyNewJudgementsWhenFailed | TRUE
        // CF: ScoreProcessor.ApplyResultInternal | result.FailedAtJudgement | TRUE
        [Test]
        public void TestResultsAfterFailAreIgnoredUnlessConfigured()
        {
            var obj = new TestHitObject();
            var result = new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great, FailedAtJudgement = true };

            processor.ApplyResult(result);
            Assert.That(processor.Statistics, Is.Empty);
            Assert.That(processor.Combo.Value, Is.Zero);

            processor.ApplyNewJudgementsWhenFailed = true;
            processor.ApplyResult(result);
            Assert.That(processor.Statistics[HitResult.Great], Is.EqualTo(1));
        }

        // CF: ScoreProcessor.ApplyResultInternal | TrackHitEvents | FALSE
        [Test]
        public void TestHitEventsNotTrackedWhenDisabled()
        {
            processor.TrackHitEvents = false;
            apply(new TestHitObject(), HitResult.Great);

            Assert.That(processor.HitEvents, Is.Empty);
            Assert.That(processor.TotalScore.Value, Is.GreaterThan(0));
        }

        // CF: ScoreProcessor.RevertResultInternal | !TrackHitEvents | FALSE
        // CF: ScoreProcessor.RevertResultInternal | result.FailedAtJudgement && !ApplyNewJudgementsWhenFailed | FALSE
        // CF: ScoreProcessor.RevertResultInternal | result.Judgement.MaxResult.AffectsAccuracy() | TRUE
        // CF: ScoreProcessor.RevertResultInternal | result.Type.AffectsAccuracy() | TRUE
        // CF: ScoreProcessor.RevertResultInternal | result.Type.IsBonus() | FALSE
        // CF: ScoreProcessor.RevertResultInternal | result.Type.IsScorable() | TRUE
        [Test]
        public void TestRevertRestoresInitialState()
        {
            var beatmap = beatmapOf(2);
            processor.ApplyBeatmap(beatmap);

            var first = apply(beatmap.HitObjects[0], HitResult.Great);
            var second = apply(beatmap.HitObjects[1], HitResult.Great);

            processor.RevertResult(second);
            Assert.That(processor.Combo.Value, Is.EqualTo(1));
            Assert.That(processor.HitEvents.Count, Is.EqualTo(1));

            processor.RevertResult(first);

            Assert.That(processor.Combo.Value, Is.Zero);
            Assert.That(processor.HighestCombo.Value, Is.Zero);
            Assert.That(processor.TotalScore.Value, Is.Zero);
            Assert.That(processor.Accuracy.Value, Is.EqualTo(1));
            Assert.That(processor.Statistics[HitResult.Great], Is.Zero);
            Assert.That(processor.HitEvents, Is.Empty);
            Assert.That(processor.JudgedHits, Is.Zero);
        }

        // CF: ScoreProcessor.RevertResultInternal | result.Type.IsBonus() | TRUE
        // CF: ScoreProcessor.RevertResultInternal | result.Judgement.MaxResult.AffectsAccuracy() | FALSE
        // CF: ScoreProcessor.RevertResultInternal | result.Type.AffectsAccuracy() | FALSE
        [Test]
        public void TestRevertBonus()
        {
            var result = apply(new TestHitObject(HitResult.LargeBonus), HitResult.LargeBonus);
            processor.RevertResult(result);

            Assert.That(processor.GetScoreProcessorStatistics().BonusPortion, Is.Zero);
        }

        // CF: ScoreProcessor.RevertResultInternal | result.FailedAtJudgement && !ApplyNewJudgementsWhenFailed | TRUE
        [Test]
        public void TestRevertOfIgnoredFailedResultOnlyRestoresCombo()
        {
            var obj = new TestHitObject();
            var result = new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great, FailedAtJudgement = true };
            processor.ApplyResult(result);

            Assert.DoesNotThrow(() => processor.RevertResult(result));
            Assert.That(processor.Statistics, Is.Empty);
        }

        // CF: ScoreProcessor.RevertResultInternal | !TrackHitEvents | throws InvalidOperationException
        [Test]
        public void TestRevertWithoutHitEventTrackingThrows()
        {
            processor.TrackHitEvents = false;
            var result = apply(new TestHitObject(), HitResult.Great);

            Assert.Throws<InvalidOperationException>(() => processor.RevertResult(result));
        }

        // CF: ScoreProcessor.updateRank | rank.Value == ScoreRank.F | TRUE
        // CF: ScoreProcessor.FailScore | Rank.Value == ScoreRank.F | FALSE
        // CF: ScoreProcessor.FailScore | Rank.Value == ScoreRank.F | TRUE
        [Test]
        public void TestFailScoreMarksRankFAndOnlyOnce()
        {
            var score = new ScoreInfo { Passed = true };

            processor.FailScore(score);
            Assert.That(score.Passed, Is.False);
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.F));
            Assert.That(score.Rank, Is.EqualTo(ScoreRank.F));

            // rank no longer follows accuracy.
            apply(new TestHitObject(), HitResult.Great);
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.F));

            var second = new ScoreInfo { Passed = true };
            processor.FailScore(second);
            Assert.That(second.Passed, Is.True, "already failed: score is left untouched");
        }

        // CF: ScoreProcessor.PopulateScore | foreach ALL_TYPES | loop n
        [Test]
        public void TestPopulateScore()
        {
            var beatmap = beatmapOf(1);
            processor.ApplyBeatmap(beatmap);
            apply(beatmap.HitObjects[0], HitResult.Great);

            var score = new ScoreInfo();
            score.Statistics[HitResult.Miss] = 99;
            processor.PopulateScore(score);

            Assert.That(score.Combo, Is.EqualTo(1));
            Assert.That(score.MaxCombo, Is.EqualTo(1));
            Assert.That(score.Accuracy, Is.EqualTo(1));
            Assert.That(score.TotalScore, Is.EqualTo(1_000_000));
            Assert.That(score.Statistics[HitResult.Great], Is.EqualTo(1));
            Assert.That(score.Statistics[HitResult.Miss], Is.Zero, "statistics are rebuilt from scratch");
            Assert.That(score.MaximumStatistics[HitResult.Great], Is.EqualTo(1));
            Assert.That(score.HitEvents.Count, Is.EqualTo(1));
        }

        // CF: ScoreProcessor.updateRank | foreach IApplicableToScoreProcessor mod | loop 0
        // CF: ScoreProcessor.updateRank | foreach IApplicableToScoreProcessor mod | loop 1
        [Test]
        public void TestModsAdjustRank()
        {
            processor.Mods.Value = new[] { new OsuModHidden() };
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.XH));

            processor.Mods.Value = Array.Empty<osu.Game.Rulesets.Mods.Mod>();
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.X));
        }

        // CF: ScoreProcessor.updateScoreMultiplier | Beatmap.Value == null | TRUE
        // CF: ScoreProcessor.updateScoreMultiplier | Beatmap.Value == null | FALSE
        [Test]
        public void TestScoreMultiplierFromMods()
        {
            // mods changed while no beatmap is present: multiplier is not computed, score unaffected.
            processor.Mods.Value = new[] { new OsuModEasy() };
            apply(new TestHitObject(), HitResult.Great);
            Assert.That(processor.TotalScore.Value, Is.EqualTo(processor.TotalScoreWithoutMods.Value));

            var withBeatmap = new ScoreProcessor(new OsuRuleset());
            withBeatmap.ApplyBeatmap(beatmapOf(1));
            withBeatmap.Mods.Value = new[] { new OsuModEasy() };
            var obj = new TestHitObject();
            withBeatmap.ApplyResult(new JudgementResult(obj, obj.Judgement) { Type = HitResult.Great });

            Assert.That(withBeatmap.TotalScore.Value, Is.LessThan(withBeatmap.TotalScoreWithoutMods.Value), "Easy reduces the score multiplier");
        }

        // CF: ScoreProcessor.ResetFromReplayFrame | frame.Header == null | TRUE
        // CF: ScoreProcessor.ResetFromReplayFrame | frame.Header == null | FALSE
        // CF: ScoreProcessor.ResetFromReplayFrame | OnResetFromReplayFrame?.Invoke | non-null
        [Test]
        public void TestResetFromReplayFrame()
        {
            int resets = 0;
            processor.OnResetFromReplayFrame += () => resets++;

            processor.ResetFromReplayFrame(new ReplayFrame());
            Assert.That(resets, Is.Zero);
            Assert.That(processor.Combo.Value, Is.Zero);

            processor.ResetFromReplayFrame(new ReplayFrame
            {
                Header = new FrameHeader(
                    1234, 1, 5, 7,
                    new Dictionary<HitResult, int> { [HitResult.Great] = 5 },
                    new ScoreProcessorStatistics { BaseScore = 1500, MaximumBaseScore = 1500, AccuracyJudgementCount = 5 },
                    DateTimeOffset.UnixEpoch, Array.Empty<osu.Game.Online.API.APIMod>(), null, null)
            });

            Assert.That(processor.Combo.Value, Is.EqualTo(5));
            Assert.That(processor.HighestCombo.Value, Is.EqualTo(7));
            Assert.That(processor.Statistics[HitResult.Great], Is.EqualTo(5));
            Assert.That(processor.JudgedHits, Is.EqualTo(5));
            Assert.That(processor.GetScoreProcessorStatistics().BaseScore, Is.EqualTo(1500));
            Assert.That(resets, Is.EqualTo(1));
        }

        // CF: ScoreProcessor.SetScoreProcessorStatistics | assigns all fields | no throw
        [Test]
        public void TestScoreProcessorStatisticsRoundTrip()
        {
            var stats = new ScoreProcessorStatistics { BaseScore = 1, MaximumBaseScore = 2, AccuracyJudgementCount = 3, ComboPortion = 4, BonusPortion = 5 };
            processor.SetScoreProcessorStatistics(stats);

            var read = processor.GetScoreProcessorStatistics();
            Assert.That((read.BaseScore, read.MaximumBaseScore, read.AccuracyJudgementCount, read.ComboPortion, read.BonusPortion), Is.EqualTo((1.0, 2.0, 3, 4.0, 5.0)));
        }

        // CF: ScoreProcessor.Reset | storeResults | FALSE
        // CF: ScoreProcessor.Dispose | hitEvents.Clear | no throw
        [Test]
        public void TestReapplyingBeatmapResetsState()
        {
            var beatmap = beatmapOf(1);
            processor.ApplyBeatmap(beatmap);
            apply(beatmap.HitObjects[0], HitResult.Miss);

            processor.ApplyBeatmap(beatmap);

            Assert.That(processor.Statistics, Is.Empty);
            Assert.That(processor.HitEvents, Is.Empty);
            Assert.That(processor.Rank.Value, Is.EqualTo(ScoreRank.X));
            Assert.That(processor.MaximumTotalScore, Is.EqualTo(1_000_000));
        }
    }
}
