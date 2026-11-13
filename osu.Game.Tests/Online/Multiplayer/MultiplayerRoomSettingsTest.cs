// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;

namespace osu.Game.Tests.Online.Multiplayer
{
    [TestFixture]
    public class MultiplayerRoomSettingsTest
    {
        // CF: MultiplayerRoomSettings.AutoStartEnabled | AutoStartDuration != Zero | FALSE
        [Test]
        public void TestAutoStartDisabledByDefault() => Assert.That(new MultiplayerRoomSettings().AutoStartEnabled, Is.False);

        // CF: MultiplayerRoomSettings.AutoStartEnabled | AutoStartDuration != Zero | TRUE
        [Test]
        public void TestAutoStartEnabled() => Assert.That(new MultiplayerRoomSettings { AutoStartDuration = TimeSpan.FromSeconds(1) }.AutoStartEnabled, Is.True);

        // CF: MultiplayerRoomSettings.ctor(Room) | room.Password ?? string.Empty | null
        [Test]
        public void TestFromRoomWithoutPassword()
        {
            var settings = new MultiplayerRoomSettings(new Room { Name = "n" });

            Assert.That(settings.Password, Is.EqualTo(string.Empty));
            Assert.That(settings.Name, Is.EqualTo("n"));
        }

        // CF: MultiplayerRoomSettings.ctor(Room) | room.Password ?? string.Empty | non-null
        [Test]
        public void TestFromRoomCopiesFields()
        {
            var settings = new MultiplayerRoomSettings(new Room
            {
                Password = "p", Type = MatchType.TeamVersus, QueueMode = QueueMode.AllPlayers, AutoStartDuration = TimeSpan.FromSeconds(3), AutoSkip = true, MaxParticipants = 8,
            });

            Assert.That(settings.Password, Is.EqualTo("p"));
            Assert.That(settings.MatchType, Is.EqualTo(MatchType.TeamVersus));
            Assert.That(settings.QueueMode, Is.EqualTo(QueueMode.AllPlayers));
            Assert.That(settings.AutoStartDuration, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(settings.AutoSkip, Is.True);
            Assert.That(settings.MaxParticipants, Is.EqualTo((byte)8));
        }

        // CF: MultiplayerRoomSettings.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestEqualsSelf()
        {
            var s = new MultiplayerRoomSettings();
            Assert.That(s.Equals(s), Is.True);
        }

        // CF: MultiplayerRoomSettings.Equals | other == null | TRUE
        [Test]
        public void TestEqualsNull() => Assert.That(new MultiplayerRoomSettings().Equals(null), Is.False);

        // CF: MultiplayerRoomSettings.Equals | other == null | FALSE
        // CF: MultiplayerRoomSettings.Equals | Password.Equals | TRUE
        // CF: MultiplayerRoomSettings.Equals | Name.Equals | TRUE
        // CF: MultiplayerRoomSettings.Equals | PlaylistItemId == other.PlaylistItemId | TRUE
        // CF: MultiplayerRoomSettings.Equals | MatchType == other.MatchType | TRUE
        // CF: MultiplayerRoomSettings.Equals | QueueMode == other.QueueMode | TRUE
        // CF: MultiplayerRoomSettings.Equals | AutoStartDuration == other.AutoStartDuration | TRUE
        // CF: MultiplayerRoomSettings.Equals | AutoSkip == other.AutoSkip | TRUE
        // CF: MultiplayerRoomSettings.Equals | MaxParticipants == other.MaxParticipants | TRUE
        [Test]
        public void TestEqualsSameValues() => Assert.That(new MultiplayerRoomSettings().Equals(new MultiplayerRoomSettings()), Is.True);

        private static void assertDiffers(Action<MultiplayerRoomSettings> mutate)
        {
            var other = new MultiplayerRoomSettings();
            mutate(other);
            Assert.That(new MultiplayerRoomSettings().Equals(other), Is.False);
        }

        // CF: MultiplayerRoomSettings.Equals | Password.Equals | FALSE
        [Test]
        public void TestDiffersPassword() => assertDiffers(s => s.Password = "x");

        // CF: MultiplayerRoomSettings.Equals | Name.Equals | FALSE
        [Test]
        public void TestDiffersName() => assertDiffers(s => s.Name = "x");

        // CF: MultiplayerRoomSettings.Equals | PlaylistItemId == other.PlaylistItemId | FALSE
        [Test]
        public void TestDiffersItem() => assertDiffers(s => s.PlaylistItemId = 5);

        // CF: MultiplayerRoomSettings.Equals | MatchType == other.MatchType | FALSE
        [Test]
        public void TestDiffersMatchType() => assertDiffers(s => s.MatchType = MatchType.TeamVersus);

        // CF: MultiplayerRoomSettings.Equals | QueueMode == other.QueueMode | FALSE
        [Test]
        public void TestDiffersQueueMode() => assertDiffers(s => s.QueueMode = QueueMode.AllPlayers);

        // CF: MultiplayerRoomSettings.Equals | AutoStartDuration == other.AutoStartDuration | FALSE
        [Test]
        public void TestDiffersAutoStart() => assertDiffers(s => s.AutoStartDuration = TimeSpan.FromSeconds(1));

        // CF: MultiplayerRoomSettings.Equals | AutoSkip == other.AutoSkip | FALSE
        [Test]
        public void TestDiffersAutoSkip() => assertDiffers(s => s.AutoSkip = true);

        // CF: MultiplayerRoomSettings.Equals | MaxParticipants == other.MaxParticipants | FALSE
        [Test]
        public void TestDiffersMaxParticipants() => assertDiffers(s => s.MaxParticipants = 2);

        // CF: MultiplayerRoomSettings.ToString | string.IsNullOrEmpty(Password) | TRUE
        // CF: MultiplayerRoomSettings.ToString | MaxParticipants?.ToString() ?? "no limit" | null
        [Test]
        public void TestToStringNoPasswordNoLimit()
        {
            string s = new MultiplayerRoomSettings().ToString();

            Assert.That(s, Does.Contain("Password:no"));
            Assert.That(s, Does.Contain("MaxParticipants:no limit"));
        }

        // CF: MultiplayerRoomSettings.ToString | string.IsNullOrEmpty(Password) | FALSE
        // CF: MultiplayerRoomSettings.ToString | MaxParticipants?.ToString() ?? "no limit" | non-null
        [Test]
        public void TestToStringPasswordAndLimit()
        {
            string s = new MultiplayerRoomSettings { Password = "x", MaxParticipants = 5 }.ToString();

            Assert.That(s, Does.Contain("Password:yes"));
            Assert.That(s, Does.Contain("MaxParticipants:5"));
        }
    }
}
