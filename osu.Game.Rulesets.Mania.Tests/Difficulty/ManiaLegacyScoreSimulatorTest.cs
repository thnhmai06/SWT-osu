// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Scoring.Legacy;

namespace osu.Game.Rulesets.Mania.Tests.Difficulty
{
    [TestFixture]
    public class ManiaLegacyScoreSimulatorTest
    {
        private readonly ManiaLegacyScoreSimulator simulator = new ManiaLegacyScoreSimulator();

        private static LegacyBeatmapConversionDifficultyInfo mania() => new LegacyBeatmapConversionDifficultyInfo { SourceRuleset = new ManiaRuleset().RulesetInfo, CircleSize = 4 };

        // osu! source, no hit object counts, OD 5 -> 6 columns before mods.
        private static LegacyBeatmapConversionDifficultyInfo converted() => new LegacyBeatmapConversionDifficultyInfo
        {
            SourceRuleset = new RulesetInfo { OnlineID = 0, ShortName = "osu" },
            CircleSize = 4,
            OverallDifficulty = 5
        };

        // CF: ManiaLegacyScoreSimulator.Simulate | constant attributes | non-null
        [Test]
        public void TestSimulateConstants()
        {
            var attributes = simulator.Simulate(null!, null!);
            Assert.That(attributes.ComboScore, Is.EqualTo(1000000));
            Assert.That(attributes.MaxCombo, Is.Zero);
        }

        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | foreach mod | loop 0
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | RulesetInfo.Equals(SourceRuleset) | TRUE
        [Test]
        public void TestNativeNoMods() => Assert.That(simulator.GetLegacyScoreMultiplier([], mania()), Is.EqualTo(1.0));

        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | case ManiaModNoFail | case ManiaModNoFail
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | scoreV2 ? 1.0 : 0.5 | FALSE
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | mods.Any(ModScoreV2) | FALSE
        [Test]
        public void TestNoFail() => Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModNoFail()], mania()), Is.EqualTo(0.5));

        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | scoreV2 ? 1.0 : 0.5 | TRUE
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | mods.Any(ModScoreV2) | TRUE
        [Test]
        public void TestNoFailWithScoreV2() => Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModNoFail(), new ManiaModScoreV2()], mania()), Is.EqualTo(1.0));

        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | case ManiaModEasy | case ManiaModEasy
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | case ManiaModHalfTime | case ManiaModHalfTime
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | case ManiaModDaycore | case ManiaModDaycore
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | switch(mod) no case matches | default
        [Test]
        public void TestHalvingMods()
        {
            Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModEasy()], mania()), Is.EqualTo(0.5));
            Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModHalfTime()], mania()), Is.EqualTo(0.5));
            Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModDaycore()], mania()), Is.EqualTo(0.5));
            Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModHidden(), new ManiaModEasy(), new ManiaModHalfTime()], mania()), Is.EqualTo(0.25));
        }

        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | RulesetInfo.Equals(SourceRuleset) | FALSE
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | actualColumns > originalColumns | FALSE
        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | actualColumns < originalColumns | FALSE
        [Test]
        public void TestConvertedSameColumnCount() => Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModKey6()], converted()), Is.EqualTo(1.0));

        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | actualColumns > originalColumns | TRUE
        [Test]
        public void TestConvertedMoreColumns() => Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModKey8()], converted()), Is.EqualTo(0.9));

        // CF: ManiaLegacyScoreSimulator.GetLegacyScoreMultiplier | actualColumns < originalColumns | TRUE
        [Test]
        public void TestConvertedFewerColumns() => Assert.That(simulator.GetLegacyScoreMultiplier([new ManiaModKey4()], converted()), Is.EqualTo(0.9 - 0.04 * 2).Within(1e-9));
    }
}
