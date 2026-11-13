// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Tests.Models
{
    [TestFixture]
    public class TournamentMatchTest
    {
        private static TournamentTeam team(string acronym)
        {
            var t = new TournamentTeam();
            t.Acronym.Value = acronym;
            return t;
        }

        // CF: TournamentMatch.Acronyms | Team1Acronym != null | FALSE
        // CF: TournamentMatch.Acronyms | Team2Acronym != null | FALSE
        [Test]
        public void TestAcronymsEmpty()
        {
            Assert.That(new TournamentMatch().Acronyms, Is.Empty);
        }

        // CF: TournamentMatch.Acronyms | Team1Acronym != null | TRUE
        // CF: TournamentMatch.Acronyms | Team2Acronym != null | FALSE
        [Test]
        public void TestAcronymsOnlyTeam1()
        {
            Assert.That(new TournamentMatch(team("AAA")).Acronyms, Is.EqualTo(new[] { "AAA" }));
        }

        // CF: TournamentMatch.Acronyms | Team2Acronym != null | TRUE
        [Test]
        public void TestAcronymsBothTeams()
        {
            Assert.That(new TournamentMatch(team("AAA"), team("BBB")).Acronyms, Is.EqualTo(new[] { "AAA", "BBB" }));
        }

        // CF: TournamentMatch.TournamentMatch | t.NewValue?.Acronym.Value | non-null
        [Test]
        public void TestTeamChangeUpdatesAcronym()
        {
            var match = new TournamentMatch(team("AAA"), team("BBB"));

            Assert.That(match.Team1Acronym, Is.EqualTo("AAA"));
            Assert.That(match.Team2Acronym, Is.EqualTo("BBB"));
        }

        // CF: TournamentMatch.TournamentMatch | t.NewValue?.Acronym.Value | null
        [Test]
        public void TestTeamClearedNullsAcronym()
        {
            var match = new TournamentMatch(team("AAA"), team("BBB"));
            match.Team1.Value = null;
            match.Team2.Value = null;

            Assert.That(match.Team1Acronym, Is.Null);
            Assert.That(match.Team2Acronym, Is.Null);
        }

        private static TournamentMatch completed(int score1, int score2)
        {
            var match = new TournamentMatch(team("AAA"), team("BBB"));
            match.Team1Score.Value = score1;
            match.Team2Score.Value = score2;
            match.Completed.Value = true;
            return match;
        }

        // CF: TournamentMatch.Winner | !Completed.Value | TRUE
        // CF: TournamentMatch.Loser | !Completed.Value | TRUE
        [Test]
        public void TestNoWinnerOrLoserWhenIncomplete()
        {
            var match = new TournamentMatch(team("AAA"), team("BBB"));
            match.Team1Score.Value = 5;
            match.Team2Score.Value = 0;

            Assert.That(match.Winner, Is.Null);
            Assert.That(match.Loser, Is.Null);
        }

        // CF: TournamentMatch.Winner | !Completed.Value | FALSE
        // CF: TournamentMatch.Winner | Team1Score.Value > Team2Score.Value | TRUE
        // CF: TournamentMatch.Loser | !Completed.Value | FALSE
        // CF: TournamentMatch.Loser | Team1Score.Value > Team2Score.Value | TRUE
        // CF: TournamentMatch.WinnerColour | Winner == Team1.Value | TRUE
        [Test]
        public void TestTeam1Wins()
        {
            var match = completed(5, 2);

            Assert.That(match.Winner, Is.SameAs(match.Team1.Value));
            Assert.That(match.Loser, Is.SameAs(match.Team2.Value));
            Assert.That(match.WinnerColour, Is.EqualTo(TeamColour.Red));
        }

        // CF: TournamentMatch.Winner | Team1Score.Value > Team2Score.Value | FALSE
        // CF: TournamentMatch.Loser | Team1Score.Value > Team2Score.Value | FALSE
        // CF: TournamentMatch.WinnerColour | Winner == Team1.Value | FALSE
        [Test]
        public void TestTeam2Wins()
        {
            var match = completed(2, 5);

            Assert.That(match.Winner, Is.SameAs(match.Team2.Value));
            Assert.That(match.Loser, Is.SameAs(match.Team1.Value));
            Assert.That(match.WinnerColour, Is.EqualTo(TeamColour.Blue));
        }

        // CF: TournamentMatch.PointsToWin | Round.Value?.BestOf.Value / 2 + 1 ?? 0 | null
        [Test]
        public void TestPointsToWinWithoutRound()
        {
            Assert.That(new TournamentMatch().PointsToWin, Is.EqualTo(0));
        }

        // CF: TournamentMatch.PointsToWin | Round.Value?.BestOf.Value / 2 + 1 ?? 0 | non-null
        [TestCase(9, 5)]
        [TestCase(3, 2)]
        public void TestPointsToWinIsMajority(int bestOf, int expected)
        {
            var match = new TournamentMatch();
            match.Round.Value = new TournamentRound();
            match.Round.Value.BestOf.Value = bestOf;

            Assert.That(match.PointsToWin, Is.EqualTo(expected));
        }

        // CF: TournamentMatch.CancelMatchStart | clears both scores | no throw
        [Test]
        public void TestCancelMatchStartClearsScores()
        {
            var match = completed(3, 1);
            match.CancelMatchStart();

            Assert.That(match.Team1Score.Value, Is.Null);
            Assert.That(match.Team2Score.Value, Is.Null);
        }

        // CF: TournamentMatch.StartMatch | Team1.Value == null | TRUE
        [Test]
        public void TestStartMatchNoTeam1()
        {
            var match = new TournamentMatch(null, team("BBB"));
            match.StartMatch();

            Assert.That(match.Team1Score.Value, Is.Null);
        }

        // CF: TournamentMatch.StartMatch | Team1.Value == null | FALSE
        // CF: TournamentMatch.StartMatch | Team2.Value == null | TRUE
        [Test]
        public void TestStartMatchNoTeam2()
        {
            var match = new TournamentMatch(team("AAA"), null);
            match.StartMatch();

            Assert.That(match.Team1Score.Value, Is.Null);
        }

        // CF: TournamentMatch.StartMatch | Team2.Value == null | FALSE
        // CF: TournamentMatch.StartMatch | Team1Score.Value > 0 | TRUE
        [Test]
        public void TestStartMatchTeam1AlreadyScored()
        {
            var match = new TournamentMatch(team("AAA"), team("BBB"));
            match.Team1Score.Value = 2;
            match.StartMatch();

            Assert.That(match.Team1Score.Value, Is.EqualTo(2));
            Assert.That(match.Team2Score.Value, Is.Null);
        }

        // CF: TournamentMatch.StartMatch | Team1Score.Value > 0 | FALSE
        // CF: TournamentMatch.StartMatch | Team2Score.Value > 0 | TRUE
        [Test]
        public void TestStartMatchTeam2AlreadyScored()
        {
            var match = new TournamentMatch(team("AAA"), team("BBB"));
            match.Team1Score.Value = 0;
            match.Team2Score.Value = 1;
            match.StartMatch();

            Assert.That(match.Team1Score.Value, Is.EqualTo(0));
            Assert.That(match.Team2Score.Value, Is.EqualTo(1));
        }

        // CF: TournamentMatch.StartMatch | Team2Score.Value > 0 | FALSE
        [Test]
        public void TestStartMatchZeroesScores()
        {
            var match = new TournamentMatch(team("AAA"), team("BBB"));
            match.StartMatch();

            Assert.That(match.Team1Score.Value, Is.EqualTo(0));
            Assert.That(match.Team2Score.Value, Is.EqualTo(0));
        }

        // CF: TournamentMatch.Reset | PicksBans.Clear | loop 0
        [Test]
        public void TestResetEmptyPicksBans()
        {
            var match = completed(3, 1);
            match.Reset();

            Assert.That(match.Team1.Value, Is.Null);
            Assert.That(match.Team2.Value, Is.Null);
            Assert.That(match.Team1Score.Value, Is.Null);
            Assert.That(match.Completed.Value, Is.False);
            Assert.That(match.PicksBans, Is.Empty);
        }

        // CF: TournamentMatch.Reset | PicksBans.Clear | loop n
        [Test]
        public void TestResetClearsPicksBans()
        {
            var match = completed(3, 1);
            match.PicksBans.Add(new BeatmapChoice());
            match.PicksBans.Add(new BeatmapChoice());
            match.Reset();

            Assert.That(match.PicksBans, Is.Empty);
        }
    }
}
