// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Tests.Online.API.Requests.Responses
{
    [TestFixture]
    public class SoloScoreInfoTest
    {
        // CF: SoloScoreInfo.ShouldSerializeEndedAt | EndedAt != default | FALSE
        // CF: SoloScoreInfo.ShouldSerializeStartedAt | StartedAt != null | FALSE
        // CF: SoloScoreInfo.ShouldSerializeLegacyScoreId | LegacyScoreId != null | FALSE
        // CF: SoloScoreInfo.ShouldSerializeLegacyTotalScore | LegacyTotalScore != null | FALSE
        // CF: SoloScoreInfo.ShouldSerializeMods | Mods.Length > 0 | FALSE
        // CF: SoloScoreInfo.ShouldSerializeUserID | UserID > 0 | FALSE
        // CF: SoloScoreInfo.ShouldSerializeBeatmapID | BeatmapID > 0 | FALSE
        // CF: SoloScoreInfo.ShouldSerializeBuildID | BuildID != null | FALSE
        [Test]
        public void TestDefaultsAreNotSerialised()
        {
            var s = new SoloScoreInfo();

            Assert.That(s.ShouldSerializeEndedAt(), Is.False);
            Assert.That(s.ShouldSerializeStartedAt(), Is.False);
            Assert.That(s.ShouldSerializeLegacyScoreId(), Is.False);
            Assert.That(s.ShouldSerializeLegacyTotalScore(), Is.False);
            Assert.That(s.ShouldSerializeMods(), Is.False);
            Assert.That(s.ShouldSerializeUserID(), Is.False);
            Assert.That(s.ShouldSerializeBeatmapID(), Is.False);
            Assert.That(s.ShouldSerializeBuildID(), Is.False);
        }

        // CF: SoloScoreInfo.ShouldSerializeEndedAt | EndedAt != default | TRUE
        // CF: SoloScoreInfo.ShouldSerializeStartedAt | StartedAt != null | TRUE
        // CF: SoloScoreInfo.ShouldSerializeLegacyScoreId | LegacyScoreId != null | TRUE
        // CF: SoloScoreInfo.ShouldSerializeLegacyTotalScore | LegacyTotalScore != null | TRUE
        // CF: SoloScoreInfo.ShouldSerializeMods | Mods.Length > 0 | TRUE
        // CF: SoloScoreInfo.ShouldSerializeUserID | UserID > 0 | TRUE
        // CF: SoloScoreInfo.ShouldSerializeBeatmapID | BeatmapID > 0 | TRUE
        // CF: SoloScoreInfo.ShouldSerializeBuildID | BuildID != null | TRUE
        [Test]
        public void TestPopulatedFieldsAreSerialised()
        {
            var s = new SoloScoreInfo
            {
                EndedAt = DateTimeOffset.UnixEpoch.AddDays(1), StartedAt = DateTimeOffset.UnixEpoch, LegacyScoreId = 1, LegacyTotalScore = 2, Mods = new[] { new APIMod() }, UserID = 3, BeatmapID = 4, BuildID = 5,
            };

            Assert.That(s.ShouldSerializeEndedAt(), Is.True);
            Assert.That(s.ShouldSerializeStartedAt(), Is.True);
            Assert.That(s.ShouldSerializeLegacyScoreId(), Is.True);
            Assert.That(s.ShouldSerializeLegacyTotalScore(), Is.True);
            Assert.That(s.ShouldSerializeMods(), Is.True);
            Assert.That(s.ShouldSerializeUserID(), Is.True);
            Assert.That(s.ShouldSerializeBeatmapID(), Is.True);
            Assert.That(s.ShouldSerializeBuildID(), Is.True);
        }

        // CF: SoloScoreInfo.ShouldSerializeID | always | FALSE
        [Test]
        public void TestWebOnlyFieldsNeverSerialised()
        {
            var s = new SoloScoreInfo { ID = 1, PP = 2, HasReplay = true };

            Assert.That(s.ShouldSerializeID(), Is.False);
            Assert.That(s.ShouldSerializeUser(), Is.False);
            Assert.That(s.ShouldSerializeBeatmap(), Is.False);
            Assert.That(s.ShouldSerializeBeatmapSet(), Is.False);
            Assert.That(s.ShouldSerializePP(), Is.False);
            Assert.That(s.ShouldSerializeOnlineID(), Is.False);
            Assert.That(s.ShouldSerializeHasReplay(), Is.False);
            Assert.That(s.ShouldSerializePreserve(), Is.False);
            Assert.That(s.ShouldSerializeProcessed(), Is.False);
        }

        // CF: SoloScoreInfo.OnlineID | (long?)ID ?? -1 | null
        // CF: SoloScoreInfo.IsLegacyScore | LegacyScoreId != null | FALSE
        [Test]
        public void TestIdsWhenUnset()
        {
            var s = new SoloScoreInfo();

            Assert.That(s.OnlineID, Is.EqualTo(-1));
            Assert.That(((IScoreInfo)s).LegacyOnlineID, Is.EqualTo(-1));
            Assert.That(s.IsLegacyScore, Is.False);
        }

        // CF: SoloScoreInfo.OnlineID | (long?)ID ?? -1 | non-null
        // CF: SoloScoreInfo.IsLegacyScore | LegacyScoreId != null | TRUE
        [Test]
        public void TestIdsWhenSet()
        {
            var s = new SoloScoreInfo { ID = 12, LegacyScoreId = 34 };

            Assert.That(s.OnlineID, Is.EqualTo(12));
            Assert.That(((IScoreInfo)s).LegacyOnlineID, Is.EqualTo(34));
            Assert.That(s.IsLegacyScore, Is.True);
            Assert.That(s.ToString(), Is.EqualTo("score_id: 12 user_id: 0"));
        }

        // CF: SoloScoreInfo.BeatmapSet | Beatmap is APIBeatmap | FALSE
        // CF: SoloScoreInfo.BeatmapSet | beatmapset before beatmap | throws InvalidOperationException
        [Test]
        public void TestBeatmapSetBeforeBeatmapThrows()
            => Assert.Throws<InvalidOperationException>(() => new SoloScoreInfo { BeatmapSet = new APIBeatmapSet() });

        // CF: SoloScoreInfo.BeatmapSet | Beatmap is APIBeatmap | TRUE
        [Test]
        public void TestBeatmapSetAssignedToBeatmap()
        {
            var set = new APIBeatmapSet();
            var s = new SoloScoreInfo { Beatmap = new APIBeatmap(), BeatmapSet = set };

            Assert.That(s.Beatmap!.BeatmapSet, Is.SameAs(set));
        }

        // CF: SoloScoreInfo.ToScoreInfo | beatmap is BeatmapInfo realmBeatmap | FALSE
        // CF: SoloScoreInfo.ToScoreInfo | beatmap != null | FALSE
        [Test]
        public void TestToScoreInfoWithoutBeatmap()
        {
            var s = new SoloScoreInfo
            {
                ID = 9, LegacyScoreId = 8, UserID = 7, BeatmapID = 6, RulesetID = 1, Passed = true, TotalScore = 1000, TotalScoreWithoutMods = 900, LegacyTotalScore = 500, Accuracy = 0.9, MaxCombo = 33,
                Rank = ScoreRank.A, HasReplay = true, PP = 12, Ranked = true, EndedAt = DateTimeOffset.UnixEpoch,
                Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 2 },
            };

            var info = s.ToScoreInfo(Array.Empty<Mod>());

            Assert.That(info.OnlineID, Is.EqualTo(9));
            Assert.That(info.LegacyOnlineID, Is.EqualTo(8));
            Assert.That(info.IsLegacyScore, Is.True);
            Assert.That(info.User.OnlineID, Is.EqualTo(7));
            Assert.That(info.BeatmapInfo!.OnlineID, Is.EqualTo(6));
            Assert.That(info.Ruleset.OnlineID, Is.EqualTo(1));
            Assert.That(info.Passed, Is.True);
            Assert.That(info.TotalScore, Is.EqualTo(1000));
            Assert.That(info.TotalScoreWithoutMods, Is.EqualTo(900));
            Assert.That(info.LegacyTotalScore, Is.EqualTo(500));
            Assert.That(info.Accuracy, Is.EqualTo(0.9));
            Assert.That(info.MaxCombo, Is.EqualTo(33));
            Assert.That(info.Rank, Is.EqualTo(ScoreRank.A));
            Assert.That(info.HasOnlineReplay, Is.True);
            Assert.That(info.PP, Is.EqualTo(12));
            Assert.That(info.Ranked, Is.True);
            Assert.That(info.Date, Is.EqualTo(DateTimeOffset.UnixEpoch));
            Assert.That(info.Statistics[HitResult.Great], Is.EqualTo(2));
        }

        // CF: SoloScoreInfo.ToScoreInfo | User ?? new APIUser | non-null
        [Test]
        public void TestToScoreInfoUsesProvidedUser()
        {
            var s = new SoloScoreInfo { User = new APIUser { Id = 55, Username = "u" } };

            Assert.That(s.ToScoreInfo(Array.Empty<Mod>()).User.Username, Is.EqualTo("u"));
        }

        // CF: SoloScoreInfo.ToScoreInfo | beatmap is BeatmapInfo realmBeatmap | TRUE
        [Test]
        public void TestToScoreInfoWithRealmBeatmap()
        {
            var beatmap = new BeatmapInfo { OnlineID = 77 };

            Assert.That(new SoloScoreInfo { BeatmapID = 1 }.ToScoreInfo(Array.Empty<Mod>(), beatmap).BeatmapInfo, Is.SameAs(beatmap));
        }

        // CF: SoloScoreInfo.ToScoreInfo | beatmap != null | TRUE
        [Test]
        public void TestToScoreInfoWithOtherBeatmapCopiesRulesetInfo()
        {
            var beatmap = new APIBeatmap { RulesetID = 3 };

            var info = new SoloScoreInfo { BeatmapID = 1 }.ToScoreInfo(Array.Empty<Mod>(), beatmap);

            Assert.That(info.BeatmapInfo!.OnlineID, Is.EqualTo(1));
            Assert.That(info.BeatmapInfo.Ruleset.OnlineID, Is.EqualTo(3));
            Assert.That(info.BeatmapInfo.Ruleset.ShortName, Is.EqualTo("mania"));
        }

        // CF: SoloScoreInfo.ForSubmission | Where(kvp.Value != 0) | TRUE
        // CF: SoloScoreInfo.ForSubmission | Where(kvp.Value != 0) | FALSE
        [Test]
        public void TestForSubmissionDropsZeroStatistics()
        {
            var score = new ScoreInfo
            {
                Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo,
                Rank = ScoreRank.S, TotalScore = 5, Accuracy = 0.5, MaxCombo = 4, Passed = true,
                Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 3, [HitResult.Miss] = 0 },
                MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 3, [HitResult.Perfect] = 0 },
            };

            var s = SoloScoreInfo.ForSubmission(score);

            Assert.That(s.Statistics, Is.EqualTo(new Dictionary<HitResult, int> { [HitResult.Great] = 3 }));
            Assert.That(s.MaximumStatistics, Is.EqualTo(new Dictionary<HitResult, int> { [HitResult.Great] = 3 }));
            Assert.That(s.Rank, Is.EqualTo(ScoreRank.S));
            Assert.That(s.TotalScore, Is.EqualTo(5));
            Assert.That(s.RulesetID, Is.EqualTo(0));
            Assert.That(s.Passed, Is.True);
        }

        // CF: SoloScoreInfo.Json | ignored/omitted fields not written | no throw
        [Test]
        public void TestJsonOmitsDefaults()
        {
            string json = JsonConvert.SerializeObject(new SoloScoreInfo { TotalScore = 3 });

            Assert.That(json, Does.Contain("\"total_score\":3"));
            Assert.That(json, Does.Not.Contain("\"mods\""));
            Assert.That(json, Does.Not.Contain("\"user_id\""));
            Assert.That(json, Does.Not.Contain("\"pp\""));
        }
    }
}
