// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Tests.Models
{
    [TestFixture]
    public class TournamentBeatmapTest
    {
        // CF: TournamentBeatmap.TournamentBeatmap | beatmap.BeatmapSet?.Covers ?? new | non-null
        // CF: TournamentBeatmap.TournamentBeatmap | beatmap.BeatmapSet == null | FALSE
        [Test]
        public void TestCoversTakenFromBeatmapSet()
        {
            var beatmap = new APIBeatmap
            {
                OnlineID = 12,
                DifficultyName = "Insane",
                BPM = 180,
                StarRating = 5.5,
                BeatmapSet = new APIBeatmapSet { Covers = new BeatmapSetOnlineCovers { Cover = "cover.jpg" } }
            };

            var result = new TournamentBeatmap(beatmap);

            Assert.That(result.Covers.Cover, Is.EqualTo("cover.jpg"));
            Assert.That(result.OnlineID, Is.EqualTo(12));
            Assert.That(result.DifficultyName, Is.EqualTo("Insane"));
            Assert.That(result.BPM, Is.EqualTo(180));
            Assert.That(result.StarRating, Is.EqualTo(5.5));
        }

        // CF: TournamentBeatmap.TournamentBeatmap | beatmap.BeatmapSet?.Covers ?? new | null
        // CF: TournamentBeatmap.TournamentBeatmap | beatmap.BeatmapSet == null | TRUE
        [Test]
        public void TestCoversDefaultWithoutBeatmapSet()
        {
            var result = new TournamentBeatmap(new APIBeatmap { OnlineID = 1 });

            Assert.That(result.Covers.Cover, Is.Null);
        }

        // CF: TournamentBeatmap.Equals | other is TournamentBeatmap | TRUE
        // CF: TournamentBeatmap.Equals | this.MatchesOnlineID(b) | TRUE
        [Test]
        public void TestEqualsSameOnlineId()
        {
            Assert.That(new TournamentBeatmap { OnlineID = 5 }.Equals(new TournamentBeatmap { OnlineID = 5 }), Is.True);
        }

        // CF: TournamentBeatmap.Equals | this.MatchesOnlineID(b) | FALSE
        [Test]
        public void TestEqualsDifferentOnlineId()
        {
            Assert.That(new TournamentBeatmap { OnlineID = 5 }.Equals(new TournamentBeatmap { OnlineID = 6 }), Is.False);
        }

        // CF: TournamentBeatmap.Equals | other is TournamentBeatmap | FALSE
        [Test]
        public void TestEqualsNullOrOtherType()
        {
            var beatmap = new TournamentBeatmap { OnlineID = 5 };

            Assert.That(beatmap.Equals(null), Is.False);
            Assert.That(beatmap.Equals(new APIBeatmap { OnlineID = 5 }), Is.False);
        }
    }
}
