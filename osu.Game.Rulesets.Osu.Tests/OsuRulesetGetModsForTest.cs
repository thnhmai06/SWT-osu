// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class OsuRulesetGetModsForTest
    {
        private static Mod[] modsFor(ModType type) => new OsuRuleset().GetModsFor(type).ToArray();

        private static Type[] flatten(Mod[] mods) => mods.SelectMany(m => m is MultiMod multi ? multi.Mods : new[] { m }).Select(m => m.GetType()).ToArray();

        // One test per switch arm: each returned mod must declare the requested type.
        [TestCase(ModType.DifficultyReduction, typeof(OsuModEasy), typeof(OsuModNoFail), typeof(OsuModHalfTime), typeof(OsuModDaycore))]
        [TestCase(ModType.DifficultyIncrease, typeof(OsuModHardRock), typeof(OsuModSuddenDeath), typeof(OsuModPerfect), typeof(OsuModDoubleTime), typeof(OsuModNightcore), typeof(OsuModHidden), typeof(OsuModTraceable), typeof(OsuModFlashlight), typeof(OsuModBlinds), typeof(OsuModStrictTracking), typeof(OsuModAccuracyChallenge))]
        [TestCase(ModType.Conversion, typeof(OsuModTargetPractice), typeof(OsuModDifficultyAdjust), typeof(OsuModClassic), typeof(OsuModRandom), typeof(OsuModMirror), typeof(OsuModAlternate), typeof(OsuModSingleTap))]
        [TestCase(ModType.Automation, typeof(OsuModAutoplay), typeof(OsuModCinema), typeof(OsuModRelax), typeof(OsuModAutopilot), typeof(OsuModSpunOut))]
        [TestCase(ModType.System, typeof(OsuModTouchDevice), typeof(ModScoreV2))]
        public void TestArm_ContainsExpectedMods(ModType type, params Type[] expected)
        {
            var types = flatten(modsFor(type));

            Assert.That(types, Is.SupersetOf(expected));
            Assert.That(flatten(modsFor(type)).Select(t => (Mod)Activator.CreateInstance(t)!).All(m => m.Type == type), Is.True);
        }

        // Fun arm: non-empty and contains a few representative mods.
        [Test]
        public void TestFunArm_ContainsRepresentativeMods()
        {
            var types = flatten(modsFor(ModType.Fun));

            Assert.That(types, Is.SupersetOf(new[] { typeof(OsuModTransform), typeof(OsuModWiggle), typeof(OsuModBubbles), typeof(OsuModBloom) }));
        }

        // default arm: type with no osu! mods yields empty (Conversion etc. are handled, Fun handled; use out-of-range enum).
        [Test]
        public void TestDefaultArm_UnknownType_ReturnsEmpty()
        {
            Assert.That(modsFor((ModType)999), Is.Empty);
        }
    }
}
