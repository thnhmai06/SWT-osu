// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using MessagePack;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Tests.Online.API
{
    [TestFixture]
    public class APIModTest
    {
        private static readonly OsuRuleset ruleset = new OsuRuleset();

        // CF: APIMod.ctor(Mod) | foreach settings | loop 0
        // CF: APIMod.ctor(Mod) | !bindable.IsDefault | FALSE
        [Test]
        public void TestFromModWithDefaultSettingsHasNoSettings()
        {
            var mod = new APIMod(new OsuModDoubleTime());

            Assert.That(mod.Acronym, Is.EqualTo("DT"));
            Assert.That(mod.Settings, Is.Empty);
            Assert.That(mod.ShouldSerializeSettings(), Is.False);
        }

        // CF: APIMod.ctor(Mod) | foreach settings | loop n
        // CF: APIMod.ctor(Mod) | !bindable.IsDefault | TRUE
        [Test]
        public void TestFromModWithChangedSetting()
        {
            var mod = new APIMod(new OsuModDoubleTime { SpeedChange = { Value = 1.3 } });

            Assert.That(mod.Settings, Has.Count.EqualTo(1));
            Assert.That(mod.Settings["speed_change"], Is.EqualTo(1.3));
            Assert.That(mod.ShouldSerializeSettings(), Is.True);
        }

        // CF: APIMod.ToMod | resultMod == null | TRUE
        [Test]
        public void TestUnknownAcronymBecomesUnknownMod()
        {
            var mod = new APIMod { Acronym = "ZZ" }.ToMod(ruleset);

            Assert.That(mod, Is.TypeOf<UnknownMod>());
            Assert.That(((UnknownMod)mod).OriginalAcronym, Is.EqualTo("ZZ"));
            Assert.That(mod.Acronym, Is.EqualTo("ZZ??"));
        }

        // CF: APIMod.ToMod | resultMod == null | FALSE
        // CF: APIMod.ToMod | Settings.Count > 0 | FALSE
        [Test]
        public void TestKnownAcronymWithoutSettings() => Assert.That(new APIMod { Acronym = "HD" }.ToMod(ruleset), Is.TypeOf<OsuModHidden>());

        // CF: APIMod.ToMod | Settings.Count > 0 | TRUE
        // CF: APIMod.ToMod | foreach properties | loop n
        // CF: APIMod.ToMod | Settings.TryGetValue | TRUE
        // CF: APIMod.ToMod | Settings.TryGetValue | FALSE
        [Test]
        public void TestSettingsAreApplied()
        {
            var mod = (OsuModDoubleTime)new APIMod { Acronym = "DT", Settings = new Dictionary<string, object> { ["speed_change"] = 1.4, ["unknown_setting"] = 5 } }.ToMod(ruleset);

            Assert.That(mod.SpeedChange.Value, Is.EqualTo(1.4));
        }

        // CF: APIMod.ToMod | CopyAdjustedSetting throws | no throw
        [Test]
        public void TestInvalidSettingValueIsIgnored()
        {
            var mod = (OsuModDoubleTime)new APIMod { Acronym = "DT", Settings = new Dictionary<string, object> { ["speed_change"] = "abc" } }.ToMod(ruleset);

            Assert.That(mod.SpeedChange.IsDefault, Is.True);
        }

        // CF: APIMod.Equals | other == null | TRUE
        [Test]
        public void TestEqualsNull() => Assert.That(new APIMod { Acronym = "HD" }.Equals(null), Is.False);

        // CF: APIMod.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestEqualsSelf()
        {
            var m = new APIMod { Acronym = "HD" };
            Assert.That(m.Equals(m), Is.True);
        }

        // CF: APIMod.Equals | Acronym == other.Acronym | TRUE
        // CF: APIMod.Equals | Settings.SequenceEqual | TRUE
        [Test]
        public void TestEqualsSameAcronymAndSettings()
        {
            var a = new APIMod { Acronym = "DT", Settings = { ["speed_change"] = 1.5 } };
            var b = new APIMod { Acronym = "DT", Settings = { ["speed_change"] = 1.5 } };

            Assert.That(a.Equals(b), Is.True);
        }

        // CF: APIMod.Equals | Acronym == other.Acronym | FALSE
        [Test]
        public void TestEqualsDifferentAcronym() => Assert.That(new APIMod { Acronym = "DT" }.Equals(new APIMod { Acronym = "HT" }), Is.False);

        // CF: APIMod.Equals | Settings.SequenceEqual | FALSE
        // CF: ModSettingsEqualityComparer.Equals | x.Key == y.Key | FALSE
        [Test]
        public void TestEqualsDifferentSettings()
        {
            var a = new APIMod { Acronym = "DT", Settings = { ["speed_change"] = 1.5 } };

            Assert.That(a.Equals(new APIMod { Acronym = "DT", Settings = { ["speed_change"] = 1.2 } }), Is.False);
            Assert.That(a.Equals(new APIMod { Acronym = "DT", Settings = { ["other"] = 1.5 } }), Is.False);
            Assert.That(a.Equals(new APIMod { Acronym = "DT" }), Is.False);
        }

        // CF: APIMod.ToString | Settings.Count > 0 | TRUE
        [Test]
        public void TestToStringWithSettings() => Assert.That(new APIMod { Acronym = "DT", Settings = { ["speed_change"] = 1.5 } }.ToString(), Is.EqualTo("DT (speed_change:1.5)"));

        // CF: APIMod.ToString | Settings.Count > 0 | FALSE
        [Test]
        public void TestToStringWithoutSettings() => Assert.That(new APIMod { Acronym = "HD" }.ToString(), Is.EqualTo("HD"));

        // CF: ModSettingsDictionaryFormatter.Serialize | round trip of settings | no throw
        // CF: ModSettingsDictionaryFormatter.Deserialize | for itemCount | loop n
        [Test]
        public void TestMessagePackRoundTrip()
        {
            var original = new APIMod { Acronym = "DT", Settings = { ["speed_change"] = 1.5, ["adjust_pitch"] = true, ["name"] = "x" } };

            var read = MessagePackSerializer.Deserialize<APIMod>(MessagePackSerializer.Serialize(original));

            Assert.That(read.Acronym, Is.EqualTo("DT"));
            Assert.That(read.Settings["speed_change"], Is.EqualTo(1.5));
            Assert.That(read.Settings["adjust_pitch"], Is.EqualTo(true));
            Assert.That(read.Settings["name"], Is.EqualTo("x"));
        }

        // CF: ModSettingsDictionaryFormatter.Deserialize | for itemCount | loop 0
        [Test]
        public void TestMessagePackRoundTripWithoutSettings()
        {
            var read = MessagePackSerializer.Deserialize<APIMod>(MessagePackSerializer.Serialize(new APIMod { Acronym = "HD" }));

            Assert.That(read.Settings, Is.Empty);
        }
    }
}
