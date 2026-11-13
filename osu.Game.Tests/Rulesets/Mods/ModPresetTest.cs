// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModPresetTest
    {
        private static ModPreset preset() => new ModPreset { Ruleset = new OsuRuleset().RulesetInfo };

        // CF: ModPreset.Mods | string.IsNullOrEmpty(ModsJson) | TRUE
        [Test]
        public void TestNoJsonMeansNoMods()
        {
            Assert.That(preset().Mods, Is.Empty);
        }

        // CF: ModPreset.Mods | string.IsNullOrEmpty(ModsJson) | FALSE
        [Test]
        public void TestModsRoundTripWithSettings()
        {
            var preset = ModPresetTest.preset();
            var doubleTime = new OsuModDoubleTime { SpeedChange = { Value = 1.3 } };

            preset.Mods = [new OsuModHidden(), doubleTime];
            var restored = preset.Mods.ToArray();

            Assert.That(preset.ModsJson, Is.Not.Empty);
            Assert.That(restored.Select(m => m.GetType()), Is.EqualTo(new[] { typeof(OsuModHidden), typeof(OsuModDoubleTime) }));
            Assert.That(((OsuModDoubleTime)restored[1]).SpeedChange.Value, Is.EqualTo(1.3));
            Assert.That(restored[1], Is.Not.SameAs(doubleTime));
        }

        [Test]
        public void TestEmptyModListSerialisesToEmptyCollection()
        {
            var preset = ModPresetTest.preset();
            preset.Mods = [];

            Assert.That(preset.Mods, Is.Empty);
        }

        [Test]
        public void TestDefaults()
        {
            var p = new ModPreset();

            Assert.That(p.ID, Is.Not.EqualTo(System.Guid.Empty));
            Assert.That((p.Name, p.Description, p.DeletePending), Is.EqualTo((string.Empty, string.Empty, false)));
        }
    }
}
