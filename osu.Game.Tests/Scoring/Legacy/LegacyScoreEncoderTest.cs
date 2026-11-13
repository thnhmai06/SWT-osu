// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Replays;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Tests.Scoring.Legacy
{
    [TestFixture]
    public class LegacyScoreEncoderTest
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

        internal class TestDecoder : LegacyScoreDecoder
        {
            private readonly int beatmapVersion;
            private readonly bool missingBeatmap;

            private static readonly Dictionary<int, Ruleset> rulesets = new Ruleset[] { new OsuRuleset(), new TaikoRuleset(), new CatchRuleset(), new ManiaRuleset() }
                .ToDictionary(r => ((ILegacyRuleset)r).LegacyID);

            public TestDecoder(int beatmapVersion = LegacyBeatmapDecoder.LATEST_VERSION, bool missingBeatmap = false)
            {
                this.beatmapVersion = beatmapVersion;
                this.missingBeatmap = missingBeatmap;
            }

            protected override Ruleset GetRuleset(int rulesetId) => rulesets[rulesetId];

            protected override WorkingBeatmap GetBeatmap(string md5Hash)
            {
                if (missingBeatmap)
                    return new DummyWorkingBeatmap(null!, null);

                return new TestWorkingBeatmap(new Beatmap
                {
                    BeatmapInfo = new BeatmapInfo
                    {
                        MD5Hash = md5Hash,
                        Ruleset = new OsuRuleset().RulesetInfo,
                        Difficulty = new BeatmapDifficulty { ApproachRate = 3, CircleSize = 4, DrainRate = 5, OverallDifficulty = 6 },
                    },
                    HitObjects = { new HitCircle() },
                    BeatmapVersion = beatmapVersion,
                });
            }
        }

        private static ScoreInfo createScoreInfo(RulesetInfo? ruleset = null)
            => new ScoreInfo
            {
                Ruleset = ruleset ?? new OsuRuleset().RulesetInfo,
                BeatmapInfo = new BeatmapInfo { MD5Hash = "d41d8cd98f00b204e9800998ecf8427e" },
                User = new APIUser { Username = "tester" },
                // the legacy format stores the wall-clock part and the decoder reads it as local time, so use a local-offset date to stay machine time zone independent.
                Date = new DateTimeOffset(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local)),
                TotalScoreVersion = LegacyScoreEncoder.LATEST_VERSION,
                TotalScore = 123_456,
                TotalScoreWithoutMods = 123_456,
                MaxCombo = 7,
                Statistics = { [HitResult.Great] = 5, [HitResult.Ok] = 2, [HitResult.Miss] = 1 },
                MaximumStatistics = { [HitResult.Great] = 8 },
            };

        private static Score createScore(params LegacyReplayFrame[] frames)
        {
            var score = new Score { ScoreInfo = createScoreInfo(), Replay = new Replay() };
            score.Replay.Frames.AddRange(frames);
            return score;
        }

        private static Score roundTrip(Score score, IBeatmap? beatmap = null, int beatmapVersion = LegacyBeatmapDecoder.LATEST_VERSION)
        {
            using var stream = new MemoryStream();
            new LegacyScoreEncoder(score, beatmap).Encode(stream, true);
            stream.Position = 0;
            return new TestDecoder(beatmapVersion).Parse(stream);
        }

        private static LegacyReplayFrame frame(double time, float x, float y, ReplayButtonState buttons = ReplayButtonState.None) => new LegacyReplayFrame(time, x, y, buttons);

        // CF: LegacyScoreEncoder.LegacyScoreEncoder | beatmap == null && !Frames.All(LegacyReplayFrame) | TRUE
        // CF: LegacyScoreEncoder.LegacyScoreEncoder | beatmap == null && !Frames.All(LegacyReplayFrame) | throws ArgumentException
        [Test]
        public void TestNonLegacyFramesWithoutBeatmapThrows()
        {
            var score = createScore();
            score.Replay.Frames.Add(new OsuReplayFrame(100, new Vector2(10, 10), OsuAction.LeftButton));

            Assert.Throws<ArgumentException>(() => new LegacyScoreEncoder(score, null));
        }

        // CF: LegacyScoreEncoder.LegacyScoreEncoder | beatmap == null | FALSE
        // CF: LegacyScoreEncoder.LegacyScoreEncoder | beatmap == null && !All | FALSE
        // CF: LegacyScoreEncoder.LegacyScoreEncoder | !IsLegacyRuleset() | TRUE
        // CF: LegacyScoreEncoder.LegacyScoreEncoder | !IsLegacyRuleset() | throws ArgumentException
        [Test]
        public void TestNonLegacyRulesetThrows()
        {
            var score = createScore();
            score.ScoreInfo.Ruleset = new RulesetInfo("custom", "Custom", "x", 12);

            Assert.Throws<ArgumentException>(() => new LegacyScoreEncoder(score, null));
        }

        // CF: LegacyScoreEncoder.LegacyScoreEncoder | !IsLegacyRuleset() | FALSE
        // CF: LegacyScoreEncoder.replayStringContent | score.Replay != null | TRUE
        // CF: LegacyScoreEncoder.replayStringContent | foreach frame | loop n
        // CF: LegacyScoreEncoder.getLegacyFrame | case LegacyReplayFrame | case LegacyReplayFrame
        [Test]
        public void TestScoreAndReplayRoundTrip()
        {
            var decoded = roundTrip(createScore(
                frame(10, 100, 120, ReplayButtonState.Left1),
                frame(20, 150, 130),
                frame(35, 200, 140, ReplayButtonState.Right1)));

            Assert.That(decoded.ScoreInfo.Ruleset.OnlineID, Is.EqualTo(0));
            Assert.That(decoded.ScoreInfo.User.Username, Is.EqualTo("tester"));
            Assert.That(decoded.ScoreInfo.TotalScore, Is.EqualTo(123_456));
            Assert.That(decoded.ScoreInfo.MaxCombo, Is.EqualTo(7));
            Assert.That(decoded.ScoreInfo.Date, Is.EqualTo(new DateTimeOffset(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local))));
            Assert.That(decoded.ScoreInfo.Statistics[HitResult.Great], Is.EqualTo(5));
            Assert.That(decoded.ScoreInfo.Statistics[HitResult.Miss], Is.EqualTo(1));
            Assert.That(decoded.ScoreInfo.IsLegacyScore, Is.False);

            var frames = decoded.Replay.Frames.Cast<OsuReplayFrame>().ToArray();
            Assert.That(frames.Select(f => f.Time), Is.EqualTo(new[] { 10.0, 20, 35 }));
            Assert.That(frames[0].Position, Is.EqualTo(new Vector2(100, 120)));
            Assert.That(frames[0].Actions, Is.EqualTo(new[] { OsuAction.LeftButton }));
            Assert.That(frames[1].Actions, Is.Empty);
        }

        // CF: LegacyScoreEncoder.replayStringContent | foreach frame | loop 0
        [Test]
        public void TestNoFramesRoundTrip()
        {
            Assert.That(roundTrip(createScore()).Replay.Frames, Is.Empty);
        }

        // CF: LegacyScoreEncoder.replayStringContent | beatmap?.BeatmapVersion < 5 | TRUE
        // CF: LegacyScoreEncoder.replayStringContent | beatmap?.BeatmapVersion < 5 | FALSE
        [Test]
        public void TestEarlyBeatmapVersionTimingOffsetIsSymmetric()
        {
            var early = new Beatmap { BeatmapVersion = 4 };
            var score = createScore(frame(100, 1, 1), frame(200, 2, 2), frame(300, 3, 3));

            var decoded = roundTrip(score, early, beatmapVersion: 4);
            Assert.That(decoded.Replay.Frames.Select(f => f.Time), Is.EqualTo(new[] { 100.0, 200, 300 }), "encoder subtracts the early-version offset and the decoder adds it back");

            var latest = roundTrip(score, new Beatmap { BeatmapVersion = 14 }, beatmapVersion: 14);
            Assert.That(latest.Replay.Frames.Select(f => f.Time), Is.EqualTo(new[] { 100.0, 200, 300 }));
        }

        // CF: LegacyScoreEncoder.getLegacyFrame | case IConvertibleReplayFrame | case IConvertibleReplayFrame
        [Test]
        public void TestConvertibleFramesAreConvertedWithBeatmap()
        {
            var score = createScore();
            score.Replay.Frames.Add(new OsuReplayFrame(50, new Vector2(40, 60), OsuAction.RightButton));
            score.Replay.Frames.Add(new OsuReplayFrame(80, new Vector2(41, 61)));
            score.Replay.Frames.Add(new OsuReplayFrame(120, new Vector2(42, 62)));

            var decoded = roundTrip(score, new Beatmap());

            var first = (OsuReplayFrame)decoded.Replay.Frames[0];
            Assert.That(first.Position, Is.EqualTo(new Vector2(40, 60)));
            Assert.That(first.Actions, Is.EqualTo(new[] { OsuAction.RightButton }));
        }

        // CF: LegacyScoreEncoder.getLegacyFrame | default | throws ArgumentException
        [Test]
        public void TestUnconvertibleFrameThrows()
        {
            var score = createScore();
            score.Replay.Frames.Add(new ReplayFrame(10));

            using var stream = new MemoryStream();
            Assert.Throws<ArgumentException>(() => new LegacyScoreEncoder(score, new Beatmap()).Encode(stream, true));
        }

        // CF: LegacyScoreEncoder.Encode | leaveOpen | TRUE
        [Test]
        public void TestEncodeLeaveOpenKeepsStreamUsable()
        {
            using var stream = new MemoryStream();
            new LegacyScoreEncoder(createScore(), null).Encode(stream, true);

            Assert.That(stream.CanWrite, Is.True);
            Assert.That(stream.Length, Is.GreaterThan(0));
        }

        // CF: LegacyScoreDecoder.readLegacyReplay | legacyFrames[1].Time < legacyFrames[0].Time | TRUE
        [Test]
        public void TestDecoderFixesSecondFrameBeforeFirst()
        {
            var decoded = roundTrip(createScore(frame(100, 1, 1), frame(50, 2, 2), frame(200, 3, 3)));

            Assert.That(decoded.Replay.Frames.Select(f => f.Time), Is.EqualTo(new[] { 0.0, 100, 200 }));
        }

        // CF: LegacyScoreDecoder.readLegacyReplay | legacyFrames[0].Time > legacyFrames[2].Time | TRUE
        [Test]
        public void TestDecoderFixesFirstFrameAfterThird()
        {
            var decoded = roundTrip(createScore(frame(300, 1, 1), frame(310, 2, 2), frame(200, 3, 3)));

            Assert.That(decoded.Replay.Frames.Select(f => f.Time), Is.EqualTo(new[] { 200.0, 200, 200 }));
        }

        // CF: LegacyScoreDecoder.readLegacyReplay | legacyFrames[1].Position == (256,-500) | TRUE
        // CF: LegacyScoreDecoder.readLegacyReplay | legacyFrames[0].Position == (256,-500) | TRUE
        [Test]
        public void TestDecoderDropsStableSkipBoundaryFrames()
        {
            var decoded = roundTrip(createScore(frame(0, 256, -500), frame(1, 256, -500), frame(100, 10, 10), frame(200, 20, 20)));

            Assert.That(decoded.Replay.Frames.Select(f => f.Time), Is.EqualTo(new[] { 100.0, 200 }));
        }

        // CF: LegacyScoreDecoder.readLegacyReplay | currentFrame != null && legacyFrame.Time < currentFrame.Time | TRUE
        // CF: LegacyScoreDecoder.readLegacyReplay | currentFrame != null && legacyFrame.Time < currentFrame.Time | FALSE
        [Test]
        public void TestDecoderSkipsBackwardsFrames()
        {
            var decoded = roundTrip(createScore(frame(50, 1, 1), frame(100, 2, 2), frame(80, 3, 3), frame(150, 4, 4)));

            Assert.That(decoded.Replay.Frames.Select(f => f.Time), Is.EqualTo(new[] { 50.0, 100, 150 }));
        }
    }
}
