// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osuTK.Graphics;

namespace osu.Game.Tests.Graphics
{
    [TestFixture]
    public class OsuColourTest
    {
        private readonly OsuColour colours = new OsuColour();

        private static Color4 hex(string h) => Color4Extensions.FromHex(h);

        [Test]
        public void TestGray()
        {
            Assert.That(OsuColour.Gray(0.5f), Is.EqualTo(new Color4(0.5f, 0.5f, 0.5f, 1f)));
            Assert.That(OsuColour.Gray((byte)255), Is.EqualTo(new Color4(255, 255, 255, 255)));
        }

        // CF: OsuColour.ForStarDifficultyText | starDifficulty < STAR_DIFFICULTY_DEFINED_COLOUR_CUTOFF | TRUE
        // CF: OsuColour.ForStarDifficultyText | starDifficulty < STAR_DIFFICULTY_TEXT_GRADIENT_CUTOFF | TRUE
        // CF: OsuColour.ForStarDifficultyText | starDifficulty < STAR_DIFFICULTY_TEXT_GRADIENT_CUTOFF | FALSE
        [Test]
        public void TestStarDifficultyText()
        {
            Assert.That(colours.ForStarDifficultyText(3), Is.EqualTo(Color4.Black.Opacity(0.75f)));
            Assert.That(colours.ForStarDifficultyText(7), Is.EqualTo(colours.Orange1));
            Assert.That(colours.ForStarDifficultyText(9), Is.EqualTo(hex("f6f05c")));
        }

        [Test]
        public void TestStarDifficultyColourEndpoints()
        {
            Assert.That(colours.ForStarDifficulty(9.0), Is.EqualTo(Color4.Black));
            Assert.That(colours.ForStarDifficulty(1.25), Is.EqualTo(hex("4fc0ff")));
        }

        // CF: OsuColour.ForRank | rank | case XH
        // CF: OsuColour.ForRank | rank | case X
        // CF: OsuColour.ForRank | rank | case SH
        // CF: OsuColour.ForRank | rank | case S
        // CF: OsuColour.ForRank | rank | case A
        // CF: OsuColour.ForRank | rank | case B
        // CF: OsuColour.ForRank | rank | case C
        // CF: OsuColour.ForRank | rank | case D
        // CF: OsuColour.ForRank | rank | case F
        // CF: OsuColour.ForRank | rank | default
        [TestCase(ScoreRank.XH, "de31ae")]
        [TestCase(ScoreRank.X, "de31ae")]
        [TestCase(ScoreRank.SH, "02b5c3")]
        [TestCase(ScoreRank.S, "02b5c3")]
        [TestCase(ScoreRank.A, "88da20")]
        [TestCase(ScoreRank.B, "e3b130")]
        [TestCase(ScoreRank.C, "ff8e5d")]
        [TestCase(ScoreRank.D, "ff5a5a")]
        [TestCase(ScoreRank.F, "3f3f3f")]
        [TestCase((ScoreRank)99, "3f3f3f")]
        public void TestForRank(ScoreRank rank, string expected) => Assert.That(OsuColour.ForRank(rank), Is.EqualTo(hex(expected)));

        // CF: OsuColour.ForHitResult | result | case IgnoreMiss
        // CF: OsuColour.ForHitResult | result | case SmallTickMiss
        // CF: OsuColour.ForHitResult | result | case Miss
        // CF: OsuColour.ForHitResult | result | case LargeTickMiss
        // CF: OsuColour.ForHitResult | result | case ComboBreak
        // CF: OsuColour.ForHitResult | result | case Meh
        // CF: OsuColour.ForHitResult | result | case Ok
        // CF: OsuColour.ForHitResult | result | case Good
        // CF: OsuColour.ForHitResult | result | case SmallTickHit
        // CF: OsuColour.ForHitResult | result | case LargeTickHit
        // CF: OsuColour.ForHitResult | result | case SliderTailHit
        // CF: OsuColour.ForHitResult | result | case Great
        // CF: OsuColour.ForHitResult | result | default
        [Test]
        public void TestForHitResult()
        {
            Assert.That(colours.ForHitResult(HitResult.IgnoreMiss), Is.EqualTo(Color4.Gray));
            Assert.That(colours.ForHitResult(HitResult.SmallTickMiss), Is.EqualTo(Color4.Gray));
            Assert.That(colours.ForHitResult(HitResult.Miss), Is.EqualTo(colours.Red));
            Assert.That(colours.ForHitResult(HitResult.LargeTickMiss), Is.EqualTo(colours.Red));
            Assert.That(colours.ForHitResult(HitResult.ComboBreak), Is.EqualTo(colours.Red));
            Assert.That(colours.ForHitResult(HitResult.Meh), Is.EqualTo(colours.Yellow));
            Assert.That(colours.ForHitResult(HitResult.Ok), Is.EqualTo(colours.Green));
            Assert.That(colours.ForHitResult(HitResult.Good), Is.EqualTo(colours.GreenLight));
            Assert.That(colours.ForHitResult(HitResult.SmallTickHit), Is.EqualTo(colours.Blue));
            Assert.That(colours.ForHitResult(HitResult.LargeTickHit), Is.EqualTo(colours.Blue));
            Assert.That(colours.ForHitResult(HitResult.SliderTailHit), Is.EqualTo(colours.Blue));
            Assert.That(colours.ForHitResult(HitResult.Great), Is.EqualTo(colours.Blue));
            Assert.That(colours.ForHitResult(HitResult.Perfect), Is.EqualTo(colours.BlueLight));
        }

        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case None
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case LocallyModified
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case Ranked
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case Approved
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case Loved
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case Qualified
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case Pending
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case WIP
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | case Graveyard
        // CF: OsuColour.ForBeatmapSetOnlineStatus | status | default
        [Test]
        public void TestForBeatmapSetOnlineStatus()
        {
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.None), Is.EqualTo(Color4.RosyBrown));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.LocallyModified), Is.EqualTo(Color4.OrangeRed));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.Ranked), Is.EqualTo(hex("b3ff66")));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.Approved), Is.EqualTo(hex("b3ff66")));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.Loved), Is.EqualTo(hex("ff66ab")));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.Qualified), Is.EqualTo(hex("66ccff")));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.Pending), Is.EqualTo(hex("ffd966")));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.WIP), Is.EqualTo(hex("ff9966")));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus(BeatmapOnlineStatus.Graveyard), Is.EqualTo(Color4.Black));
            Assert.That(OsuColour.ForBeatmapSetOnlineStatus((BeatmapOnlineStatus)99), Is.Null);
        }

        // CF: OsuColour.ForModType | modType | case Automation
        // CF: OsuColour.ForModType | modType | case DifficultyIncrease
        // CF: OsuColour.ForModType | modType | case DifficultyReduction
        // CF: OsuColour.ForModType | modType | case Conversion
        // CF: OsuColour.ForModType | modType | case Fun
        // CF: OsuColour.ForModType | modType | case System
        // CF: OsuColour.ForModType | modType | default
        [Test]
        public void TestForModType()
        {
            Assert.That(colours.ForModType(ModType.Automation), Is.EqualTo(colours.Blue1));
            Assert.That(colours.ForModType(ModType.DifficultyIncrease), Is.EqualTo(colours.Red1));
            Assert.That(colours.ForModType(ModType.DifficultyReduction), Is.EqualTo(colours.Lime1));
            Assert.That(colours.ForModType(ModType.Conversion), Is.EqualTo(colours.Purple1));
            Assert.That(colours.ForModType(ModType.Fun), Is.EqualTo(colours.Pink1));
            Assert.That(colours.ForModType(ModType.System), Is.EqualTo(colours.Yellow));
            Assert.Throws<ArgumentOutOfRangeException>(() => colours.ForModType((ModType)99));
        }

        // CF: OsuColour.ForRoomCategory | roomCategory | case Spotlight
        // CF: OsuColour.ForRoomCategory | roomCategory | case FeaturedArtist
        // CF: OsuColour.ForRoomCategory | roomCategory | default
        [Test]
        public void TestForRoomCategory()
        {
            Assert.That(colours.ForRoomCategory(RoomCategory.Spotlight), Is.EqualTo(colours.SpotlightColour));
            Assert.That(colours.ForRoomCategory(RoomCategory.FeaturedArtist), Is.EqualTo(colours.FeaturedArtistColour));
            Assert.That(colours.ForRoomCategory(RoomCategory.Normal), Is.Null);
        }

        // CF: OsuColour.ForRoomStatus | room.HasEnded | TRUE
        // CF: OsuColour.ForRoomStatus | room.HasEnded | FALSE
        // CF: OsuColour.ForRoomStatus | room.Status | case Playing
        // CF: OsuColour.ForRoomStatus | room.Status | default
        // CF: OsuColour.ForRoomStatus | room.HasPassword | TRUE
        // CF: OsuColour.ForRoomStatus | room.HasPassword | FALSE
        [Test]
        public void TestForRoomStatus()
        {
            Assert.That(colours.ForRoomStatus(new Room { EndDate = DateTimeOffset.Now.AddDays(-1) }), Is.EqualTo(colours.YellowDarker));
            Assert.That(colours.ForRoomStatus(new Room { Status = RoomStatus.Playing }), Is.EqualTo(colours.Purple));
            Assert.That(colours.ForRoomStatus(new Room { Password = "secret" }), Is.EqualTo(colours.GreenDark));
            Assert.That(colours.ForRoomStatus(new Room()), Is.EqualTo(colours.GreenLight));
        }

        // CF: OsuColour.ForRankingTier | tier | case Iron
        // CF: OsuColour.ForRankingTier | tier | default
        [Test]
        public void TestRankingTierFallsBackToIron()
        {
            Assert.That(OsuColour.ForRankingTier(RankingTier.Iron), Is.EqualTo(osu.Framework.Graphics.Colour.ColourInfo.SingleColour(hex("BAB3AB"))));
            Assert.That(OsuColour.ForRankingTier((RankingTier)99), Is.EqualTo(OsuColour.ForRankingTier(RankingTier.Iron)));
        }

        // CF: OsuColour.ForRankingTier | tier | case Bronze
        // CF: OsuColour.ForRankingTier | tier | case Silver
        // CF: OsuColour.ForRankingTier | tier | case Gold
        // CF: OsuColour.ForRankingTier | tier | case Platinum
        // CF: OsuColour.ForRankingTier | tier | case Rhodium
        // CF: OsuColour.ForRankingTier | tier | case Radiant
        // CF: OsuColour.ForRankingTier | tier | case Lustrous
        [Test]
        public void TestRankingTiersAreGradients()
        {
            foreach (var tier in new[] { RankingTier.Bronze, RankingTier.Silver, RankingTier.Gold, RankingTier.Platinum, RankingTier.Rhodium, RankingTier.Radiant, RankingTier.Lustrous })
                Assert.That(OsuColour.ForRankingTier(tier).HasSingleColour, Is.False, tier.ToString());
        }

        // CF: OsuColour.ForegroundTextColourFor | brightness > 0.5f | TRUE
        // CF: OsuColour.ForegroundTextColourFor | brightness > 0.5f | FALSE
        [Test]
        public void TestForegroundTextColour()
        {
            Assert.That(OsuColour.ForegroundTextColourFor(Color4.White), Is.EqualTo(OsuColour.Gray(0.2f)));
            Assert.That(OsuColour.ForegroundTextColourFor(Color4.Black), Is.EqualTo(OsuColour.Gray(0.9f)));
        }
    }
}
