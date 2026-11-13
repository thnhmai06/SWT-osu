// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Game.Beatmaps;
using osu.Game.IO.Legacy;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Tests.Scoring.Legacy
{
    [TestFixture]
    public class LegacyScoreDecoderTest
    {
        private CultureInfo previousCulture = null!;

        [SetUp]
        public void SetUp()
        {
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previousCulture;

        private static MemoryStream buildStream(int version, long onlineId = 1234, int legacyMods = 0)
        {
            var stream = new MemoryStream();

            using (var sw = new SerializationWriter(stream, true))
            {
                sw.Write((byte)0); // osu!
                sw.Write(version);
                sw.Write(string.Empty.ComputeMD5Hash()); // beatmap hash
                sw.Write("username");
                sw.Write(string.Empty.ComputeMD5Hash()); // score hash
                sw.Write((ushort)198); // 300
                sw.Write((ushort)0); // 100
                sw.Write((ushort)1); // 50
                sw.Write((ushort)0); // geki
                sw.Write((ushort)0); // katu
                sw.Write((ushort)1); // miss
                sw.Write(12345678); // total score
                sw.Write((ushort)1000); // max combo
                sw.Write(false); // perfect
                sw.Write(legacyMods);
                sw.Write(string.Empty); // hp graph
                sw.Write(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                sw.Write(Array.Empty<byte>()); // replay data

                if (version >= 20140721)
                    sw.Write(onlineId);
                else if (version >= 20121008)
                    sw.Write((int)onlineId);

                if (version >= 30000001)
                    sw.Write(Array.Empty<byte>()); // score info json
            }

            stream.Position = 0;
            return stream;
        }

        private static Score parse(MemoryStream stream) => new LegacyScoreEncoderTest.TestDecoder().Parse(stream);

        // CF: LegacyScoreDecoder.Parse | workingBeatmap is DummyWorkingBeatmap | FALSE
        // CF: LegacyScoreDecoder.Parse | version < FIRST_LAZER_VERSION | FALSE
        // CF: LegacyScoreDecoder.Parse | version < 30000002 | TRUE
        // CF: LegacyScoreDecoder.Parse | version >= 20140721 | TRUE
        // CF: LegacyScoreDecoder.Parse | scoreInfo.LegacyOnlineID == 0 | FALSE
        // CF: LegacyScoreDecoder.Parse | version >= 30000001 | FALSE
        // CF: LegacyScoreDecoder.Parse | compressedReplay?.Length > 0 | FALSE
        // CF: LegacyScoreDecoder.Parse | score.ScoreInfo.IsLegacyScore | FALSE
        // CF: LegacyScoreDecoder.Parse | decodedRank != null | FALSE
        [Test]
        public void TestFirstLazerVersionScore()
        {
            var score = parse(buildStream(LegacyScoreEncoder.FIRST_LAZER_VERSION));

            Assert.That(score.ScoreInfo.IsLegacyScore, Is.False);
            Assert.That(score.ScoreInfo.TotalScoreVersion, Is.EqualTo(LegacyScoreEncoder.LATEST_VERSION), "the score is migrated to the latest scoring version after being bumped from 30000001");
            Assert.That(score.ScoreInfo.LegacyOnlineID, Is.EqualTo(1234));
            Assert.That(score.ScoreInfo.User.Username, Is.EqualTo("username"));
            Assert.That(score.ScoreInfo.Mods, Is.Empty, "no Classic mod for lazer scores");
            Assert.That(score.Replay.Frames, Is.Empty);
            Assert.That(score.ScoreInfo.Statistics[HitResult.Great], Is.EqualTo(198));
        }

        // CF: LegacyScoreDecoder.Parse | version < 30000002 | FALSE
        // CF: LegacyScoreDecoder.Parse | version >= 30000001 | TRUE
        // CF: LegacyScoreDecoder.Parse | compressedScoreInfo?.Length > 0 | FALSE
        [Test]
        public void TestRecentLazerVersionKeepsItsVersion()
        {
            var score = parse(buildStream(30000019));

            Assert.That(score.ScoreInfo.TotalScoreVersion, Is.EqualTo(LegacyScoreEncoder.LATEST_VERSION));
        }

        // CF: LegacyScoreDecoder.Parse | scoreInfo.LegacyOnlineID == 0 | TRUE
        [Test]
        public void TestZeroOnlineIdBecomesMinusOne()
        {
            Assert.That(parse(buildStream(LegacyScoreEncoder.FIRST_LAZER_VERSION, onlineId: 0)).ScoreInfo.LegacyOnlineID, Is.EqualTo(-1));
        }

        // CF: LegacyScoreDecoder.Parse | version < FIRST_LAZER_VERSION | TRUE
        // CF: LegacyScoreDecoder.Parse | version >= 20140721 | FALSE
        // CF: LegacyScoreDecoder.Parse | version >= 20121008 | TRUE
        // CF: LegacyScoreDecoder.Parse | score.ScoreInfo.IsLegacyScore | TRUE
        [Test]
        public void TestStableScoreFromBetweenOnlineIdFormatsIsClassicLegacy()
        {
            var score = parse(buildStream(20130101, onlineId: 77));

            Assert.That(score.ScoreInfo.IsLegacyScore, Is.True);
            Assert.That(score.ScoreInfo.LegacyOnlineID, Is.EqualTo(77), "32-bit online id in this version range");
            Assert.That(score.ScoreInfo.Mods.OfType<ModClassic>().Count(), Is.EqualTo(1), "legacy scores get the Classic mod");
            Assert.That(score.ScoreInfo.LegacyTotalScore, Is.EqualTo(12345678));
            Assert.That(score.ScoreInfo.TotalScoreVersion, Is.EqualTo(LegacyScoreEncoder.LATEST_VERSION));
        }

        // CF: LegacyScoreDecoder.Parse | version >= 20121008 | FALSE
        [Test]
        public void TestVeryOldScoreHasNoOnlineId()
        {
            var score = parse(buildStream(20100101));

            Assert.That(score.ScoreInfo.LegacyOnlineID, Is.EqualTo(-1));
        }

        [Test]
        public void TestLegacyModsAreConverted()
        {
            var score = parse(buildStream(LegacyScoreEncoder.FIRST_LAZER_VERSION, legacyMods: (int)osu.Game.Beatmaps.Legacy.LegacyMods.Hidden));

            Assert.That(score.ScoreInfo.Mods.Select(m => m.Acronym), Is.EqualTo(new[] { "HD" }));
        }

        private static ScoreInfo statisticsScore(bool legacy)
            => new ScoreInfo
            {
                Ruleset = new OsuRuleset().RulesetInfo,
                BeatmapInfo = new BeatmapInfo(),
                IsLegacyScore = legacy,
            };

        private static WorkingBeatmap oneCircleBeatmap()
            => new osu.Game.Tests.Beatmaps.TestWorkingBeatmap(new Beatmap
            {
                BeatmapInfo = new BeatmapInfo { Ruleset = new OsuRuleset().RulesetInfo },
                HitObjects = { new osu.Game.Rulesets.Osu.Objects.HitCircle() },
            });

        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | MaximumStatistics sum > 0 | TRUE
        [Test]
        public void TestPopulateMaximumStatisticsSkippedWhenAlreadyPresent()
        {
            var score = statisticsScore(false);
            score.Statistics[HitResult.Great] = 5;
            score.MaximumStatistics[HitResult.Great] = 9;

            LegacyScoreDecoder.PopulateMaximumStatistics(score, oneCircleBeatmap());

            Assert.That(score.MaximumStatistics, Is.EqualTo(new System.Collections.Generic.Dictionary<HitResult, int> { [HitResult.Great] = 9 }));
        }

        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | MaximumStatistics sum > 0 | FALSE
        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | foreach statistic | loop n
        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | case LargeTickHit/LargeTickMiss | case LargeTickHit
        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | case SmallTickHit/SmallTickMiss | case SmallTickHit
        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | case IgnoreHit/IgnoreMiss/SmallBonus/LargeBonus | case IgnoreHit
        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | default | default
        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | !score.IsLegacyScore | TRUE
        [Test]
        public void TestPopulateMaximumStatisticsMapsResultsToTheirMaximum()
        {
            var score = statisticsScore(false);
            score.Statistics[HitResult.Great] = 5;
            score.Statistics[HitResult.Ok] = 2;
            score.Statistics[HitResult.Miss] = 1;
            score.Statistics[HitResult.LargeTickHit] = 3;
            score.Statistics[HitResult.LargeTickMiss] = 1;
            score.Statistics[HitResult.SmallTickMiss] = 2;
            score.Statistics[HitResult.SmallBonus] = 10;
            score.Statistics[HitResult.IgnoreHit] = 1;

            LegacyScoreDecoder.PopulateMaximumStatistics(score, oneCircleBeatmap());

            Assert.That(score.MaximumStatistics[HitResult.Great], Is.EqualTo(8), "every basic result counts as one maximum judgement");
            Assert.That(score.MaximumStatistics[HitResult.LargeTickHit], Is.EqualTo(4));
            Assert.That(score.MaximumStatistics[HitResult.SmallTickHit], Is.EqualTo(2));
            Assert.That(score.MaximumStatistics.ContainsKey(HitResult.SmallBonus), Is.False);
            Assert.That(score.MaximumStatistics.ContainsKey(HitResult.IgnoreHit), Is.False);
        }

        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | !score.IsLegacyScore | FALSE
        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | attributes.MaxCombo > maxComboFromStatistics | TRUE
        [Test]
        public void TestLegacyScoreWithoutStatisticsPadsComboFromDifficultyAttributes()
        {
#pragma warning disable CS0618
            var score = statisticsScore(true);

            LegacyScoreDecoder.PopulateMaximumStatistics(score, oneCircleBeatmap());

            Assert.That(score.MaximumStatistics[HitResult.LegacyComboIncrease], Is.EqualTo(1));
#pragma warning restore CS0618
        }

        // CF: LegacyScoreDecoder.PopulateMaximumStatistics | attributes.MaxCombo > maxComboFromStatistics | FALSE
        [Test]
        public void TestLegacyScoreWithEnoughComboFromStatisticsNeedsNoPadding()
        {
#pragma warning disable CS0618
            var score = statisticsScore(true);
            score.Statistics[HitResult.Great] = 5;

            LegacyScoreDecoder.PopulateMaximumStatistics(score, oneCircleBeatmap());

            Assert.That(score.MaximumStatistics.ContainsKey(HitResult.LegacyComboIncrease), Is.False);
#pragma warning restore CS0618
        }

        [Test]
        public void TestPopulateTotalScoreWithoutMods()
        {
            var noMods = statisticsScore(false);
            noMods.TotalScore = 900_000;
            LegacyScoreDecoder.PopulateTotalScoreWithoutMods(noMods);
            Assert.That(noMods.TotalScoreWithoutMods, Is.EqualTo(900_000));

            var easy = statisticsScore(false);
            easy.Mods = new Mod[] { new OsuModEasy() };
            easy.TotalScore = 900_000;
            LegacyScoreDecoder.PopulateTotalScoreWithoutMods(easy);
            Assert.That(easy.TotalScoreWithoutMods, Is.GreaterThan(900_000), "Easy lowers the multiplier, so removing it increases the score");
        }
    }
}
