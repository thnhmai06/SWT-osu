// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Tests.Models
{
    [TestFixture]
    public class TournamentTeamTest
    {
        private static Bindable<string> read(string json, Bindable<string> existing)
        {
            using var reader = new JsonTextReader(new StringReader(json));
            reader.Read();
            return (Bindable<string>)new TournamentTeam.LastYearPlacingConverter().ReadJson(reader, typeof(Bindable<string>), existing, new JsonSerializer());
        }

        // CF: TournamentTeam.AverageRank | ranks.Length == 0 | TRUE
        // CF: TournamentTeam.AverageRank | Players.Select(p => p.Rank).Where(i => i.HasValue) | loop 0
        [Test]
        public void TestAverageRankNoPlayers()
        {
            Assert.That(new TournamentTeam().AverageRank, Is.EqualTo(0));
        }

        // CF: TournamentTeam.AverageRank | ranks.Length == 0 | TRUE
        // CF: TournamentTeam.AverageRank | i.HasValue | FALSE
        [Test]
        public void TestAverageRankIgnoresUnrankedPlayers()
        {
            var team = new TournamentTeam();
            team.Players.Add(new TournamentUser { Rank = null });

            Assert.That(team.AverageRank, Is.EqualTo(0));
        }

        // CF: TournamentTeam.AverageRank | ranks.Length == 0 | FALSE
        // CF: TournamentTeam.AverageRank | i.HasValue | TRUE
        // CF: TournamentTeam.AverageRank | Players.Select(p => p.Rank).Where(i => i.HasValue) | loop n
        [Test]
        public void TestAverageRankOfRankedPlayers()
        {
            var team = new TournamentTeam();
            team.Players.Add(new TournamentUser { Rank = 100 });
            team.Players.Add(new TournamentUser { Rank = null });
            team.Players.Add(new TournamentUser { Rank = 300 });

            Assert.That(team.AverageRank, Is.EqualTo(200));
        }

        // CF: TournamentTeam.AverageRank | Players.Select(p => p.Rank).Where(i => i.HasValue) | loop 1
        [Test]
        public void TestAverageRankSinglePlayer()
        {
            var team = new TournamentTeam();
            team.Players.Add(new TournamentUser { Rank = 42 });

            Assert.That(team.AverageRank, Is.EqualTo(42));
        }

        // CF: TournamentTeam.TournamentTeam | val.OldValue.StartsWith(FlagName.Value) | TRUE
        // CF: TournamentTeam.TournamentTeam | val.NewValue?.Length >= 2 | TRUE
        [Test]
        public void TestAcronymDerivesFlagName()
        {
            var team = new TournamentTeam();
            team.Acronym.Value = "jpn";

            Assert.That(team.FlagName.Value, Is.EqualTo("JP"));
        }

        // CF: TournamentTeam.TournamentTeam | val.NewValue?.Length >= 2 | FALSE
        [Test]
        public void TestShortAcronymClearsFlagName()
        {
            var team = new TournamentTeam();
            team.Acronym.Value = "A";

            Assert.That(team.FlagName.Value, Is.Empty);
        }

        // CF: TournamentTeam.TournamentTeam | val.NewValue?.Length >= 2 | FALSE
        // CF: TournamentTeam.TournamentTeam | val.NewValue?. | null
        [Test]
        public void TestNullAcronymClearsFlagName()
        {
            var team = new TournamentTeam();
            team.Acronym.Value = null!;

            Assert.That(team.FlagName.Value, Is.Empty);
        }

        // CF: TournamentTeam.TournamentTeam | val.OldValue.StartsWith(FlagName.Value) | FALSE
        [Test]
        public void TestCustomFlagNameNotOverwrittenByAcronym()
        {
            var team = new TournamentTeam();
            team.FlagName.Value = "XX";
            team.Acronym.Value = "ABC";

            Assert.That(team.FlagName.Value, Is.EqualTo("XX"));
        }

        // CF: TournamentTeam.TournamentTeam | val.OldValue.StartsWith(Acronym.Value) | TRUE
        // CF: TournamentTeam.TournamentTeam | val.NewValue?.Length >= 3 | TRUE
        [Test]
        public void TestFullNameDerivesAcronym()
        {
            var team = new TournamentTeam();
            team.FullName.Value = "Japan";

            Assert.That(team.Acronym.Value, Is.EqualTo("JAP"));
            Assert.That(team.FlagName.Value, Is.EqualTo("JA"));
        }

        // CF: TournamentTeam.TournamentTeam | val.NewValue?.Length >= 3 | FALSE
        [Test]
        public void TestShortFullNameClearsAcronym()
        {
            var team = new TournamentTeam();
            team.FullName.Value = "Jp";

            Assert.That(team.Acronym.Value, Is.Empty);
        }

        // CF: TournamentTeam.TournamentTeam | val.NewValue?.Length >= 3 | FALSE
        // CF: TournamentTeam.TournamentTeam | val.NewValue?. | null
        [Test]
        public void TestNullFullNameClearsAcronym()
        {
            var team = new TournamentTeam();
            team.FullName.Value = null!;

            Assert.That(team.Acronym.Value, Is.Empty);
        }

        // CF: TournamentTeam.TournamentTeam | val.OldValue.StartsWith(Acronym.Value) | FALSE
        [Test]
        public void TestCustomAcronymNotOverwrittenByFullName()
        {
            var team = new TournamentTeam();
            team.Acronym.Value = "ZZZ";
            team.FullName.Value = "Japan";

            Assert.That(team.Acronym.Value, Is.EqualTo("ZZZ"));
        }

        // CF: TournamentTeam.ToString | FullName.Value ?? Acronym.Value | non-null
        [Test]
        public void TestToStringUsesFullName()
        {
            var team = new TournamentTeam();
            team.FullName.Value = "Japan";

            Assert.That(team.ToString(), Is.EqualTo("Japan"));
        }

        // CF: TournamentTeam.ToString | FullName.Value ?? Acronym.Value | null
        [Test]
        public void TestToStringFallsBackToAcronym()
        {
            var team = new TournamentTeam();
            team.Acronym.Value = "JPN";
            team.FullName.Value = null!;

            Assert.That(team.ToString(), Is.EqualTo("JPN"));
        }

        // CF: LastYearPlacingConverter.CanConvert | objectType == typeof(Bindable<string>) | TRUE
        // CF: LastYearPlacingConverter.CanConvert | objectType == typeof(Bindable<string>) | FALSE
        [Test]
        public void TestCanConvert()
        {
            var converter = new TournamentTeam.LastYearPlacingConverter();

            Assert.That(converter.CanConvert(typeof(Bindable<string>)), Is.True);
            Assert.That(converter.CanConvert(typeof(string)), Is.False);
        }

        // CF: LastYearPlacingConverter.WriteJson | serialises Bindable value | no throw
        [Test]
        public void TestWriteJsonWritesBareValue()
        {
            var team = new TournamentTeam();
            team.LastYearPlacing.Value = "#3";

            Assert.That(JsonConvert.SerializeObject(team.LastYearPlacing, new TournamentTeam.LastYearPlacingConverter()), Is.EqualTo("\"#3\""));
        }

        // CF: LastYearPlacingConverter.ReadJson | switch (reader.TokenType) | case String
        // CF: LastYearPlacingConverter.ReadJson | (string?)reader.Value ?? Default | non-null
        [Test]
        public void TestReadString()
        {
            Assert.That(read("\"#7\"", new Bindable<string>("N/A")).Value, Is.EqualTo("#7"));
        }

        // CF: LastYearPlacingConverter.ReadJson | switch (reader.TokenType) | case Integer
        // CF: LastYearPlacingConverter.ReadJson | value > 0 | TRUE
        [Test]
        public void TestReadPositiveInteger()
        {
            Assert.That(read("7", new Bindable<string>("N/A")).Value, Is.EqualTo("#7"));
        }

        // CF: LastYearPlacingConverter.ReadJson | value > 0 | FALSE
        [Test]
        public void TestReadNonPositiveIntegerKeepsDefault()
        {
            var existing = new Bindable<string>("N/A") { Value = "#1" };
            existing.Default = "N/A";

            Assert.That(read("0", existing).Value, Is.EqualTo("N/A"));
        }

        // CF: LastYearPlacingConverter.ReadJson | switch (reader.TokenType) | default
        [Test]
        public void TestReadOtherTokenLeavesValueUnchanged()
        {
            var existing = new Bindable<string>("N/A") { Value = "#2" };

            Assert.That(read("null", existing).Value, Is.EqualTo("#2"));
        }
    }
}
