// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Scoring
{
    [TestFixture]
    public class ManiaScoreProcessorTest
    {
        private class ExposedScoreProcessor : ManiaScoreProcessor
        {
            public IEnumerable<HitObject> Enumerate(IBeatmap beatmap) => EnumerateHitObjects(beatmap);
        }

        // CF: ManiaScoreProcessor.GetBaseScoreForResult | case Perfect | case Perfect
        [Test]
        public void TestPerfectBaseScore()
        {
            Assert.That(new ManiaScoreProcessor().GetBaseScoreForResult(HitResult.Perfect), Is.EqualTo(305));
        }

        // CF: ManiaScoreProcessor.GetBaseScoreForResult | switch(result) no case matches | default
        [TestCase(HitResult.Great, 300)]
        [TestCase(HitResult.Good, 200)]
        [TestCase(HitResult.Ok, 100)]
        [TestCase(HitResult.Meh, 50)]
        [TestCase(HitResult.Miss, 0)]
        public void TestNonPerfectBaseScoreDefersToBase(HitResult result, int expected)
        {
            Assert.That(new ManiaScoreProcessor().GetBaseScoreForResult(result), Is.EqualTo(expected));
        }

        // CF: ManiaScoreProcessor.RankFromScore | rank != ScoreRank.S | TRUE
        // CF: ManiaScoreProcessor.RankFromScore | rank != ScoreRank.S | FALSE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Good) > 0 | TRUE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Good) > 0 | FALSE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Ok) > 0 | TRUE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Ok) > 0 | FALSE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Meh) > 0 | TRUE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Meh) > 0 | FALSE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Miss) > 0 | TRUE
        // CF: ManiaScoreProcessor.RankFromScore | results.GetValueOrDefault(Miss) > 0 | FALSE
        // CF: ManiaScoreProcessor.RankFromScore | anyImperfect ? rank : X | TRUE
        // CF: ManiaScoreProcessor.RankFromScore | anyImperfect ? rank : X | FALSE
        [TestCase(ScoreRank.X, 1, HitResult.Perfect)]
        [TestCase(ScoreRank.X, 0.99, HitResult.Great)]
        [TestCase(ScoreRank.D, 0.1, HitResult.Great)]
        [TestCase(ScoreRank.X, 0.99, HitResult.Perfect, HitResult.Great)]
        [TestCase(ScoreRank.S, 0.99, HitResult.Perfect, HitResult.Good)]
        [TestCase(ScoreRank.S, 0.99, HitResult.Perfect, HitResult.Ok)]
        [TestCase(ScoreRank.S, 0.99, HitResult.Perfect, HitResult.Meh)]
        [TestCase(ScoreRank.S, 0.99, HitResult.Perfect, HitResult.Miss)]
        public void TestRanks(ScoreRank expected, double accuracy, params HitResult[] results)
        {
            var dict = new Dictionary<HitResult, int>();
            foreach (var result in results)
                dict[result] = dict.GetValueOrDefault(result) + 1;

            Assert.That(new ManiaScoreProcessor().RankFromScore(accuracy, dict), Is.EqualTo(expected));
        }

        private static ManiaScoreProcessor processorFor(params ManiaHitObject[] objects)
        {
            var processor = new ManiaScoreProcessor();
            processor.ApplyBeatmap(new ManiaBeatmap(new StageDefinition(4)) { HitObjects = objects.ToList() });
            return processor;
        }

        private static void apply(ManiaScoreProcessor p, ManiaHitObject o, HitResult type)
            => p.ApplyResult(new JudgementResult(o, o.CreateJudgement()) { Type = type });

        // CF: ManiaScoreProcessor.ComputeTotalScore | 150000 * comboProgress + 850000 * acc^(2+2acc) * accuracyProgress + bonus | non-null
        // CF: ManiaScoreProcessor.GetComboScoreChange | Math.Max(0.5, Math.Log(combo, 4)) | non-null
        // CF: ManiaScoreProcessor.getBaseComboScoreForResult | case Perfect | case Perfect
        [Test]
        public void TestAllPerfectScoresOneMillion()
        {
            var note = new Note { StartTime = 0 };
            var p = processorFor(note);
            apply(p, note, HitResult.Perfect);

            Assert.That(p.TotalScore.Value, Is.EqualTo(1_000_000));
        }

        // CF: ManiaScoreProcessor.getBaseComboScoreForResult | switch(result) no case matches | default
        [Test]
        public void TestAllGreatScoreFollowsFormula()
        {
            var note = new Note { StartTime = 0 };
            var p = processorFor(note);
            apply(p, note, HitResult.Great);

            // combo portion is the full 150000 (Great and Perfect give the same combo score); accuracy is 300/305.
            double accuracy = 300.0 / 305;
            double expected = 150000 + 850000 * Math.Pow(accuracy, 2 + 2 * accuracy);

            Assert.That(p.TotalScore.Value, Is.EqualTo((long)Math.Round(expected)).Within(1));
        }

        // CF: ManiaScoreProcessor.GetComboScoreChange | base score of Miss is 0 | non-null
        [Test]
        public void TestMissScoresZero()
        {
            var note = new Note { StartTime = 0 };
            var p = processorFor(note);
            apply(p, note, HitResult.Miss);

            Assert.That(p.TotalScore.Value, Is.EqualTo(0));
        }

        // CF: ManiaScoreProcessor.EnumerateHitObjects | Order(JudgementOrderComparer.DEFAULT) | non-null
        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | result = x.GetEndTime().CompareTo(y.GetEndTime()); result != 0 | TRUE
        [Test]
        public void TestOrderedByEndTime()
        {
            var late = new Note { StartTime = 200, Column = 0 };
            var early = new Note { StartTime = 100, Column = 3 };

            Assert.That(enumerate(late, early), Is.EqualTo(new[] { early, late }));
        }

        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | result != 0 | FALSE
        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | x is Note && y is not Note | TRUE
        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | x is not Note && y is Note | TRUE
        [Test]
        public void TestNotesBeforeHoldNotesAtSameEndTime()
        {
            var hold = new HoldNote { StartTime = 0, Duration = 100, Column = 0 };
            var note = new Note { StartTime = 100, Column = 1 };

            // comparer is evaluated in both argument orders depending on the sort algorithm, so feed both input orders.
            Assert.That(enumerate(hold, note), Is.EqualTo(new ManiaHitObject[] { note, hold }));
            Assert.That(enumerate(note, hold), Is.EqualTo(new ManiaHitObject[] { note, hold }));
        }

        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | x is Note && y is not Note | FALSE
        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | x is not Note && y is Note | FALSE
        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | x is ManiaHitObject && y is ManiaHitObject | TRUE
        [Test]
        public void TestSameEndTimeAndKindOrderedByColumn()
        {
            var right = new Note { StartTime = 100, Column = 3 };
            var left = new Note { StartTime = 100, Column = 1 };

            Assert.That(enumerate(right, left), Is.EqualTo(new[] { left, right }));
        }

        private static ManiaHitObject[] enumerate(params ManiaHitObject[] objects)
        {
            var beatmap = new ManiaBeatmap(new StageDefinition(4)) { HitObjects = objects.ToList() };
            return new ExposedScoreProcessor().Enumerate(beatmap).Cast<ManiaHitObject>().ToArray();
        }

        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | ReferenceEquals(x, y) | infeasible: Enumerable.Order never compares an element to itself
        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | x == null / y == null | infeasible: hit objects in a beatmap are never null
        // CF: ManiaScoreProcessor.JudgementOrderComparer.Compare | x is ManiaHitObject && y is ManiaHitObject | infeasible: beatmap hit objects are all ManiaHitObject, FALSE branch (returns 0) unreachable
    }
}
