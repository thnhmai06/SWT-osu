// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Tests.Scoring.Legacy
{
    [TestFixture]
    public class ScoreInfoExtensionsTest
    {
        private static ScoreInfo score(int rulesetId, long totalScore = 1_000_000, int maxBasic = 100)
            => new ScoreInfo
            {
                Ruleset = new RulesetInfo("r" + rulesetId, "r", "r", rulesetId),
                TotalScore = totalScore,
                MaximumStatistics = { [HitResult.Great] = maxBasic },
            };

        // CF: ScoreInfoExtensions.getDisplayScore | mode == Standardised | TRUE
        [Test]
        public void TestStandardisedDisplayScoreIsUnchanged()
        {
            Assert.That(score(0).GetDisplayScore(ScoringMode.Standardised), Is.EqualTo(1_000_000));
        }

        // CF: ScoreInfoExtensions.getDisplayScore | mode == Standardised | FALSE
        // CF: ScoreInfoExtensions.convertStandardisedToClassic | case 0 | case 0
        // CF: ScoreInfoExtensions.convertStandardisedToClassic | case 1 | case 1
        // CF: ScoreInfoExtensions.convertStandardisedToClassic | case 2 | case 2
        // CF: ScoreInfoExtensions.convertStandardisedToClassic | case 3 | case 3
        // CF: ScoreInfoExtensions.convertStandardisedToClassic | default | default
        [TestCase(0, 425_700L)] // (100^2 * 32.57 + 100000) * 1
        [TestCase(1, 210_900L)] // (100 * 1109 + 100000) * 1
        [TestCase(2, 316_200L)] // (1 * 100)^2 * 21.62 + 1_000_000 / 10
        [TestCase(3, 1_000_000L)]
        [TestCase(42, 1_000_000L)]
        public void TestClassicDisplayScoreByRuleset(int rulesetId, long expected)
        {
            Assert.That(score(rulesetId).GetDisplayScore(ScoringMode.Classic), Is.EqualTo(expected));
        }

        // CF: ScoreInfoExtensions.getDisplayScore | maximumStatistics filtered by IsBasic | loop 0
        // CF: ScoreInfoExtensions.getDisplayScore | maximumStatistics.Where(IsBasic) | loop n
        [Test]
        public void TestClassicDisplayScoreOnlyCountsBasicJudgements()
        {
            var noObjects = new ScoreInfo { Ruleset = new RulesetInfo("o", "o", "o", 0), TotalScore = 500_000 };
            Assert.That(noObjects.GetDisplayScore(ScoringMode.Classic), Is.EqualTo(50_000), "0 objects: (0 + 100000) * 0.5");

            var withBonus = score(1, 1_000_000, 10);
            withBonus.MaximumStatistics[HitResult.LargeBonus] = 1000;
            Assert.That(withBonus.GetDisplayScore(ScoringMode.Classic), Is.EqualTo(10 * 1109 + 100_000));
        }

        [Test]
        public void TestDisplayScoreOverloads()
        {
            var solo = new SoloScoreInfo { RulesetID = 0, TotalScore = 1_000_000, MaximumStatistics = { [HitResult.Great] = 100 } };
            var multi = new MultiplayerScore { RulesetId = 0, TotalScore = 1_000_000, MaximumStatistics = { [HitResult.Great] = 100 } };

            Assert.That(solo.GetDisplayScore(ScoringMode.Classic), Is.EqualTo(425_700));
            Assert.That(multi.GetDisplayScore(ScoringMode.Classic), Is.EqualTo(425_700));
            Assert.That(solo.GetDisplayScore(ScoringMode.Standardised), Is.EqualTo(1_000_000));
        }

        [Test]
        public void TestScoreProcessorOverload()
        {
            var processor = new ScoreProcessor(new OsuRuleset());
            var beatmap = new Beatmap();
            for (int i = 0; i < 10; i++)
                beatmap.HitObjects.Add(new HitObject { StartTime = i * 100 });
            processor.ApplyBeatmap(beatmap);

            processor.TotalScore.Value = 1_000_000;

            Assert.That(processor.GetDisplayScore(ScoringMode.Standardised), Is.EqualTo(1_000_000));
            Assert.That(processor.GetDisplayScore(ScoringMode.Classic), Is.EqualTo((long)System.Math.Round(100 * 32.57 + 100_000)));
        }

        // CF: ScoreInfoExtensions.GetCountGeki | case 1 | case 1
        // CF: ScoreInfoExtensions.GetCountGeki | case 3 | case 3
        // CF: ScoreInfoExtensions.GetCountGeki | no case | null
        // CF: ScoreInfoExtensions.SetCountGeki | case 1 | case 1
        // CF: ScoreInfoExtensions.SetCountGeki | case 3 | case 3
        // CF: ScoreInfoExtensions.SetCountGeki | no case | default
        [Test]
        public void TestGeki()
        {
            var taiko = score(1);
            taiko.SetCountGeki(3);
            taiko.SetCountGeki(4);
            taiko.SetCountKatu(5);
            Assert.That(taiko.Statistics[HitResult.LargeBonus], Is.EqualTo(12), "geki and katu both accumulate into the strong hit result");
            Assert.That(taiko.GetCountGeki(), Is.EqualTo(12));

            var mania = score(3);
            mania.SetCountGeki(7);
            mania.SetCountGeki(8);
            Assert.That(mania.Statistics[HitResult.Perfect], Is.EqualTo(8));
            Assert.That(mania.GetCountGeki(), Is.EqualTo(8));

            var osu = score(0);
            osu.SetCountGeki(5);
            Assert.That(osu.GetCountGeki(), Is.Null);
            Assert.That(osu.Statistics.ContainsKey(HitResult.Perfect), Is.False);
        }

        // CF: ScoreInfoExtensions.GetCountKatu | case 1 | case 1
        // CF: ScoreInfoExtensions.GetCountKatu | case 2 | case 2
        // CF: ScoreInfoExtensions.GetCountKatu | case 3 | case 3
        // CF: ScoreInfoExtensions.SetCountKatu | case 2 | case 2
        // CF: ScoreInfoExtensions.SetCountKatu | case 3 | case 3
        // CF: ScoreInfoExtensions.SetCountKatu | no case | default
        [Test]
        public void TestKatu()
        {
            var catchScore = score(2);
            catchScore.SetCountKatu(6);
            Assert.That(catchScore.Statistics[HitResult.SmallTickMiss], Is.EqualTo(6));
            Assert.That(catchScore.GetCountKatu(), Is.EqualTo(6));

            var mania = score(3);
            mania.SetCountKatu(9);
            Assert.That(mania.Statistics[HitResult.Good], Is.EqualTo(9));
            Assert.That(mania.GetCountKatu(), Is.EqualTo(9));

            Assert.That(score(1).GetCountKatu(), Is.Null, "taiko katu is bundled into geki");
            var osu = score(0);
            osu.SetCountKatu(1);
            Assert.That(osu.GetCountKatu(), Is.Null);
        }

        // CF: ScoreInfoExtensions.GetCount100 | case 0,1,3 | case 0
        // CF: ScoreInfoExtensions.GetCount100 | case 2 | case 2
        // CF: ScoreInfoExtensions.SetCount100 | case 0,1,3 | case 0
        // CF: ScoreInfoExtensions.SetCount100 | case 2 | case 2
        // CF: ScoreInfoExtensions.GetCount100 | no case | null
        [TestCase(0, HitResult.Ok)]
        [TestCase(1, HitResult.Ok)]
        [TestCase(3, HitResult.Ok)]
        [TestCase(2, HitResult.LargeTickHit)]
        public void TestCount100(int rulesetId, HitResult expectedStatistic)
        {
            var s = score(rulesetId);
            s.SetCount100(11);

            Assert.That(s.Statistics[expectedStatistic], Is.EqualTo(11));
            Assert.That(s.GetCount100(), Is.EqualTo(11));
            Assert.That(score(99).GetCount100(), Is.Null);
            var unknown = score(99);
            unknown.SetCount100(1);
            Assert.That(unknown.Statistics.ContainsKey(HitResult.Ok), Is.False);
        }

        // CF: ScoreInfoExtensions.GetCount50 | case 0,3 | case 0
        // CF: ScoreInfoExtensions.GetCount50 | case 2 | case 2
        // CF: ScoreInfoExtensions.SetCount50 | case 0,3 | case 0
        // CF: ScoreInfoExtensions.SetCount50 | case 2 | case 2
        // CF: ScoreInfoExtensions.GetCount50 | no case | null
        [TestCase(0, HitResult.Meh)]
        [TestCase(3, HitResult.Meh)]
        [TestCase(2, HitResult.SmallTickHit)]
        public void TestCount50(int rulesetId, HitResult expectedStatistic)
        {
            var s = score(rulesetId);
            s.SetCount50(4);

            Assert.That(s.Statistics[expectedStatistic], Is.EqualTo(4));
            Assert.That(s.GetCount50(), Is.EqualTo(4));

            var taiko = score(1);
            taiko.SetCount50(4);
            Assert.That(taiko.GetCount50(), Is.Null);
            Assert.That(taiko.Statistics.ContainsKey(HitResult.Meh), Is.False);
        }

        // CF: ScoreInfoExtensions.GetCount300 | getCount | non-null
        // CF: ScoreInfoExtensions.getCount | Statistics.TryGetValue | FALSE
        [Test]
        public void TestCount300AndMissingStatisticIsNull()
        {
            var s = new ScoreInfo { Ruleset = new RulesetInfo("o", "o", "o", 0) };
            Assert.That(s.GetCount300(), Is.Null);

            s.SetCount300(8);
            Assert.That(s.GetCount300(), Is.EqualTo(8));
        }

        // CF: ScoreInfoExtensions.GetCountMiss | case 0,1,3 | case 0
        // CF: ScoreInfoExtensions.GetCountMiss | case 2 | case 2
        // CF: ScoreInfoExtensions.GetCountMiss | no case | null
        [Test]
        public void TestCountMiss()
        {
            var osu = score(0);
            Assert.That(osu.GetCountMiss(), Is.Null);
            osu.SetCountMiss(3);
            Assert.That(osu.GetCountMiss(), Is.EqualTo(3));

            var catchScore = score(2);
            Assert.That(catchScore.GetCountMiss(), Is.Zero, "catch sums two optional statistics");
            catchScore.Statistics[HitResult.Miss] = 2;
            catchScore.Statistics[HitResult.LargeTickMiss] = 5;
            Assert.That(catchScore.GetCountMiss(), Is.EqualTo(7));

            Assert.That(score(99).GetCountMiss(), Is.Null);
        }
    }
}
