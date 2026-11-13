// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class ModUtilsTest
    {
        [SetUp]
        public void SetUp()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        // CF: ModUtils.CheckCompatibleSetAndAllowed | combination as ICollection<Mod> ?? combination.ToArray() | non-null
        // CF: ModUtils.CheckCompatibleSetAndAllowed | CheckCompatibleSet(...) | TRUE
        // CF: ModUtils.CheckCompatibleSetAndAllowed | CheckAllowed(...) | TRUE
        // CF: ModUtils.CheckAllowed | loop over mods | loop n
        [Test]
        public void TestCompatibleAndAllowed()
        {
            var mods = new Mod[] { new OsuModHidden(), new OsuModDoubleTime() };
            Assert.That(ModUtils.CheckCompatibleSetAndAllowed(mods, new[] { typeof(OsuModHidden), typeof(OsuModDoubleTime) }), Is.True);
        }

        // CF: ModUtils.CheckCompatibleSetAndAllowed | combination as ICollection<Mod> ?? combination.ToArray() | null
        // CF: ModUtils.CheckAllowed | allowedSet.Contains(m.GetType()) | FALSE
        [Test]
        public void TestCompatibleButNotAllowed()
        {
            var mods = new Mod[] { new OsuModHidden(), new OsuModDoubleTime() }.Select(m => m);
            Assert.That(ModUtils.CheckCompatibleSetAndAllowed(mods, new[] { typeof(OsuModHidden) }), Is.False);
        }

        // CF: ModUtils.CheckCompatibleSetAndAllowed | CheckCompatibleSet(...) | FALSE
        [Test]
        public void TestIncompatibleShortCircuitsAllowedCheck()
        {
            var mods = new Mod[] { new OsuModEasy(), new OsuModHardRock() };
            Assert.That(ModUtils.CheckCompatibleSetAndAllowed(mods, new[] { typeof(OsuModEasy), typeof(OsuModHardRock) }), Is.False);
        }

        // CF: ModUtils.CheckAllowed | loop over mods | loop 0
        [Test]
        public void TestAllowedEmpty()
        {
            Assert.That(ModUtils.CheckAllowed(Array.Empty<Mod>(), Array.Empty<Type>()), Is.True);
        }

        // CF: ModUtils.CheckCompatibleSet | candidate.GetType() == m.GetType() | TRUE
        // CF: ModUtils.CheckCompatibleSet | invalidMods ??= new List<Mod>() | non-null
        // CF: ModUtils.CheckCompatibleSet | invalidMods == null | FALSE
        [Test]
        public void TestDuplicateModsIncompatible()
        {
            var a = new OsuModHidden();
            var b = new OsuModHidden();
            Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { a, b }, out var invalid), Is.False);
            Assert.That(invalid, Is.EquivalentTo(new[] { b }));
        }

        // CF: ModUtils.CheckCompatibleSet | candidate.GetType() == m.GetType() | FALSE
        // CF: ModUtils.CheckCompatibleSet | invalid == mod | FALSE
        // CF: ModUtils.CheckCompatibleSet | invalidMods == null | TRUE
        [Test]
        public void TestCompatibleSet()
        {
            Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { new OsuModHidden(), new OsuModDoubleTime() }, out var invalid), Is.True);
            Assert.That(invalid, Is.Null);
            Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { new OsuModHidden() }), Is.True);
        }

        // CF: ModUtils.CheckCompatibleSet | loop over mods.IncompatibleMods | loop n
        // CF: ModUtils.CheckCompatibleSet | loop over matching mods | loop 1
        // CF: ModUtils.CheckCompatibleSet | invalid == mod | FALSE
        [Test]
        public void TestIncompatibleMods()
        {
            var easy = new OsuModEasy();
            var hr = new OsuModHardRock();
            Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { easy, hr }, out var invalid), Is.False);
            Assert.That(invalid, Is.EquivalentTo(new Mod[] { easy, hr }));
        }

        // CF: ModUtils.CheckCompatibleSet | loop over mods | loop 0
        // CF: ModUtils.CheckCompatibleSet | loop over matching mods | loop 0
        [Test]
        public void TestCompatibleEmpty()
        {
            Assert.That(ModUtils.CheckCompatibleSet(Array.Empty<Mod>()), Is.True);
        }

        // CF: ModUtils.CheckCompatibleSet | invalid == mod | TRUE
        [Test]
        public void TestModIncompatibleWithOwnTypeIsSkipped()
        {
            Assert.That(ModUtils.CheckCompatibleSet(new Mod[] { new SelfIncompatibleMod() }), Is.True);
        }

        // CF: ModUtils.CheckValidForGameplay | !checkValid(mods, not MultiMod) | TRUE
        [Test]
        public void TestGameplayRejectsMultiMod()
        {
            var multi = new MultiMod(new OsuModHidden());
            Assert.That(ModUtils.CheckValidForGameplay(new Mod[] { multi }, out var invalid), Is.False);
            Assert.That(invalid, Is.EquivalentTo(new Mod[] { multi }));
        }

        // CF: ModUtils.CheckValidForGameplay | !checkValid(mods, not MultiMod) | FALSE
        // CF: ModUtils.CheckValidForGameplay | !CheckCompatibleSet(mods) | TRUE
        [Test]
        public void TestGameplayRejectsIncompatible()
        {
            Assert.That(ModUtils.CheckValidForGameplay(new Mod[] { new OsuModEasy(), new OsuModHardRock() }, out var invalid), Is.False);
            Assert.That(invalid, Is.Not.Empty);
        }

        // CF: ModUtils.CheckValidForGameplay | !CheckCompatibleSet(mods) | FALSE
        // CF: ModUtils.CheckValidForGameplay | checkValid(m.HasImplementation) | FALSE
        [Test]
        public void TestGameplayRejectsModWithoutImplementation()
        {
            var noImpl = new NoImplementationMod();
            Assert.That(ModUtils.CheckValidForGameplay(new Mod[] { noImpl }, out var invalid), Is.False);
            Assert.That(invalid, Is.EquivalentTo(new Mod[] { noImpl }));
        }

        // CF: ModUtils.CheckValidForGameplay | checkValid(m.HasImplementation) | TRUE
        // CF: ModUtils.checkValid | !valid(mod) | FALSE
        // CF: ModUtils.checkValid | invalidMods == null | TRUE
        [Test]
        public void TestGameplayValid()
        {
            Assert.That(ModUtils.CheckValidForGameplay(new Mod[] { new OsuModHidden() }, out var invalid), Is.True);
            Assert.That(invalid, Is.Null);
        }

        // CF: ModUtils.checkValid | !valid(mod) | TRUE
        // CF: ModUtils.checkValid | invalidMods ??= new List<Mod>() | null
        // CF: ModUtils.checkValid | invalidMods ??= new List<Mod>() | non-null
        // CF: ModUtils.checkValid | loop over mods | loop n
        [Test]
        public void TestCheckValidAccumulatesInvalid()
        {
            var a = new NoImplementationMod();
            var b = new NoImplementationRateMod();
            Assert.That(ModUtils.CheckValidForGameplay(new Mod[] { new OsuModHidden(), a, b }, out var invalid), Is.False);
            Assert.That(invalid, Is.EquivalentTo(new Mod[] { a, b }));
        }

        // CF: ModUtils.checkValid | loop over mods | loop 0
        [Test]
        public void TestCheckValidEmpty()
        {
            Assert.That(ModUtils.CheckValidForGameplay(Array.Empty<Mod>(), out var invalid), Is.True);
            Assert.That(invalid, Is.Null);
        }

        // CF: ModUtils.CheckValidRequiredModsForMultiplayer | !checkValid(not MultiMod) | TRUE
        [Test]
        public void TestRequiredMultiplayerRejectsMultiMod()
        {
            Assert.That(ModUtils.CheckValidRequiredModsForMultiplayer(new Mod[] { new MultiMod(new OsuModHidden()) }, false, out _), Is.False);
        }

        // CF: ModUtils.CheckValidRequiredModsForMultiplayer | !checkValid(not MultiMod) | FALSE
        // CF: ModUtils.CheckValidRequiredModsForMultiplayer | !CheckCompatibleSet(mods) | TRUE
        [Test]
        public void TestRequiredMultiplayerRejectsIncompatible()
        {
            Assert.That(ModUtils.CheckValidRequiredModsForMultiplayer(new Mod[] { new OsuModEasy(), new OsuModHardRock() }, true, out _), Is.False);
        }

        // CF: ModUtils.CheckValidRequiredModsForMultiplayer | !CheckCompatibleSet(mods) | FALSE
        // CF: ModUtils.CheckValidRequiredModsForMultiplayer | checkValid(IsValidModForMatch) | TRUE
        [Test]
        public void TestRequiredMultiplayerValid()
        {
            Assert.That(ModUtils.CheckValidRequiredModsForMultiplayer(new Mod[] { new OsuModHidden() }, false, out var invalid), Is.True);
            Assert.That(invalid, Is.Null);
        }

        // CF: ModUtils.CheckValidRequiredModsForMultiplayer | checkValid(IsValidModForMatch) | FALSE
        [Test]
        public void TestRequiredMultiplayerFreestyleRejectsNonFreestyleMod()
        {
            // hidden is not valid as a required mod in freestyle.
            Assert.That(ModUtils.CheckValidRequiredModsForMultiplayer(new Mod[] { new OsuModHidden() }, true, out var invalid), Is.False);
            Assert.That(invalid, Has.Count.EqualTo(1));
        }

        // CF: ModUtils.CheckValidAllowedModsForMultiplayer | checkValid(IsValidModForMatch) | TRUE
        // CF: ModUtils.CheckValidAllowedModsForMultiplayer | checkValid(IsValidModForMatch) | FALSE
        [Test]
        public void TestAllowedMultiplayer()
        {
            Assert.That(ModUtils.CheckValidAllowedModsForMultiplayer(new Mod[] { new OsuModHidden() }, false, out _), Is.True);
            Assert.That(ModUtils.CheckValidAllowedModsForMultiplayer(new Mod[] { new NotFreeMod() }, false, out _), Is.False);
        }

        // CF: ModUtils.FlattenMod | mod is MultiMod multi | TRUE
        // CF: ModUtils.FlattenMod | loop over multi.Mods | loop n
        // CF: ModUtils.FlattenMods | mods.SelectMany(FlattenMod) | loop n
        [Test]
        public void TestFlattenNestedMultiMods()
        {
            var hidden = new OsuModHidden();
            var dt = new OsuModDoubleTime();
            var hr = new OsuModHardRock();
            var flattened = ModUtils.FlattenMods(new Mod[] { new MultiMod(hidden, new MultiMod(dt)), hr });
            Assert.That(flattened, Is.EqualTo(new Mod[] { hidden, dt, hr }));
        }

        // CF: ModUtils.FlattenMod | mod is MultiMod multi | FALSE
        // CF: ModUtils.FlattenMod | loop over multi.Mods | loop 0
        // CF: ModUtils.FlattenMods | mods.SelectMany(FlattenMod) | loop 0
        [Test]
        public void TestFlattenPlainAndEmpty()
        {
            var hidden = new OsuModHidden();
            Assert.That(ModUtils.FlattenMod(hidden), Is.EqualTo(new Mod[] { hidden }));
            Assert.That(ModUtils.FlattenMod(new MultiMod()), Is.Empty);
            Assert.That(ModUtils.FlattenMods(Array.Empty<Mod>()), Is.Empty);
        }

        // CF: ModUtils.InstantiateValidModsForRuleset | loop over proposedMods | loop n
        // CF: ModUtils.InstantiateValidModsForRuleset | mod is UnknownMod | TRUE
        // CF: ModUtils.InstantiateValidModsForRuleset | mod is UnknownMod | FALSE
        [Test]
        public void TestInstantiateValidModsWithUnknown()
        {
            var proposed = new[] { new APIMod(new OsuModHidden()), new APIMod { Acronym = "ZZ" } };
            Assert.That(ModUtils.InstantiateValidModsForRuleset(new OsuRuleset(), proposed, out var valid), Is.False);
            Assert.That(valid.Select(m => m.GetType()), Is.EqualTo(new[] { typeof(OsuModHidden) }));
        }

        // CF: ModUtils.InstantiateValidModsForRuleset | loop over proposedMods | loop 0
        [Test]
        public void TestInstantiateValidModsEmpty()
        {
            Assert.That(ModUtils.InstantiateValidModsForRuleset(new OsuRuleset(), Array.Empty<APIMod>(), out var valid), Is.True);
            Assert.That(valid, Is.Empty);
        }

        // CF: ModUtils.CheckModsBelongToRuleset | loop over proposedMods | loop 0
        // CF: ModUtils.CheckModsBelongToRuleset | loop over proposedMods | loop n
        // CF: ModUtils.CheckModsBelongToRuleset | rulesetModType.IsAssignableFrom(proposedModType) | TRUE
        // CF: ModUtils.CheckModsBelongToRuleset | !found | FALSE
        [Test]
        public void TestModsBelongToRuleset()
        {
            Assert.That(ModUtils.CheckModsBelongToRuleset(new OsuRuleset(), Array.Empty<Mod>()), Is.True);
            Assert.That(ModUtils.CheckModsBelongToRuleset(new OsuRuleset(), new Mod[] { new OsuModDoubleTime(), new OsuModHidden() }), Is.True);
        }

        // CF: ModUtils.CheckModsBelongToRuleset | rulesetModType.IsAssignableFrom(proposedModType) | FALSE
        // CF: ModUtils.CheckModsBelongToRuleset | !found | TRUE
        // CF: ModUtils.CheckModsBelongToRuleset | loop over rulesetModsTypes | loop n
        [Test]
        public void TestModsDoNotBelongToRuleset()
        {
            Assert.That(ModUtils.CheckModsBelongToRuleset(new OsuRuleset(), new Mod[] { new OsuModDoubleTime(), new TaikoModFlashlight() }), Is.False);
        }

        // CF: ModUtils.FormatScoreMultiplier | scoreMultiplier > 1 | TRUE
        // CF: ModUtils.FormatScoreMultiplier | scoreMultiplier > 1 | FALSE
        [TestCase(1.0001, "1.01x")]
        [TestCase(1.1, "1.10x")]
        [TestCase(0.9999, "0.99x")]
        [TestCase(1.0, "1.00x")]
        public void TestFormatScoreMultiplier(double value, string expected)
        {
            Assert.That(ModUtils.FormatScoreMultiplier(value).ToString(), Is.EqualTo(expected));
        }

        // CF: ModUtils.CalculateRateWithMods | loop over IApplicableToRate mods | loop 0
        [Test]
        public void TestRateWithNoRateMods()
        {
            Assert.That(ModUtils.CalculateRateWithMods(new Mod[] { new OsuModHidden() }), Is.EqualTo(1));
        }

        // CF: ModUtils.CalculateRateWithMods | loop over IApplicableToRate mods | loop 1
        // CF: ModUtils.CalculateRateWithMods | loop over IApplicableToRate mods | loop n
        [Test]
        public void TestRateWithRateMods()
        {
            Assert.That(ModUtils.CalculateRateWithMods(new Mod[] { new OsuModDoubleTime { SpeedChange = { Value = 1.5 } } }), Is.EqualTo(1.5).Within(1e-6));
            Assert.That(ModUtils.CalculateRateWithMods(new Mod[]
            {
                new OsuModDoubleTime { SpeedChange = { Value = 1.5 } },
                new OsuModHalfTime { SpeedChange = { Value = 0.5 } }
            }), Is.EqualTo(0.75).Within(1e-6));
        }

        // CF: ModUtils.IsValidModForMatch | mod.Type == ModType.System | TRUE
        [Test]
        public void TestMatchRejectsSystemMod()
        {
            Assert.That(ModUtils.IsValidModForMatch(new OsuModTouchDevice(), false, MatchType.Playlists, false), Is.False);
        }

        // CF: ModUtils.IsValidModForMatch | mod.Type == ModType.System | FALSE
        // CF: ModUtils.IsValidModForMatch | !mod.UserPlayable | TRUE
        [Test]
        public void TestMatchRejectsNonUserPlayableMod()
        {
            Assert.That(ModUtils.IsValidModForMatch(new OsuModAutoplay(), false, MatchType.Playlists, false), Is.False);
        }

        // CF: ModUtils.IsValidModForMatch | !mod.UserPlayable | FALSE
        // CF: ModUtils.IsValidModForMatch | !mod.HasImplementation | TRUE
        [Test]
        public void TestMatchRejectsModWithoutImplementation()
        {
            Assert.That(ModUtils.IsValidModForMatch(new NoImplementationMod(), false, MatchType.Playlists, false), Is.False);
        }

        // CF: ModUtils.IsValidModForMatch | !mod.HasImplementation | FALSE
        // CF: ModUtils.IsValidModForMatch | freestyle && required && !mod.ValidForFreestyleAsRequiredMod | TRUE
        [Test]
        public void TestMatchFreestyleRequiredRejectsNonFreestyleMod()
        {
            Assert.That(ModUtils.IsValidModForMatch(new OsuModHidden(), true, MatchType.Playlists, true), Is.False);
        }

        // CF: ModUtils.IsValidModForMatch | freestyle (false) | FALSE
        // CF: ModUtils.IsValidModForMatch | freestyle (true) && required (false) | FALSE
        // CF: ModUtils.IsValidModForMatch | freestyle && required && !ValidForFreestyleAsRequiredMod (ValidForFreestyle true) | FALSE
        // CF: ModUtils.IsValidModForMatch | matchType | case Playlists
        [Test]
        public void TestMatchPlaylistsAllowsEverythingElse()
        {
            Assert.That(ModUtils.IsValidModForMatch(new OsuModHidden(), true, MatchType.Playlists, false), Is.True);
            Assert.That(ModUtils.IsValidModForMatch(new OsuModHidden(), false, MatchType.Playlists, true), Is.True);
            Assert.That(ModUtils.IsValidModForMatch(new OsuModHardRock(), true, MatchType.Playlists, true), Is.True);
        }

        // CF: ModUtils.IsValidModForMatch | matchType | default
        // CF: ModUtils.IsValidModForMatch | required | TRUE
        // CF: ModUtils.IsValidModForMatch | required | FALSE
        [Test]
        public void TestMatchHeadToHeadUsesRequiredOrFreeModFlag()
        {
            Assert.That(ModUtils.IsValidModForMatch(new NotFreeMod(), true, MatchType.HeadToHead, false), Is.True);
            Assert.That(ModUtils.IsValidModForMatch(new NotFreeMod(), false, MatchType.HeadToHead, false), Is.False);
            Assert.That(ModUtils.IsValidModForMatch(new OsuModHidden(), false, MatchType.HeadToHead, false), Is.True);
            Assert.That(ModUtils.IsValidModForMatch(new ModAdaptiveSpeed(), true, MatchType.HeadToHead, false), Is.False);
        }

        // CF: ModUtils.EnumerateUserSelectableFreeMods | freestyle | TRUE
        // CF: ModUtils.EnumerateUserSelectableFreeMods | Where IsValidModForMatch | loop n
        // CF: ModUtils.EnumerateUserSelectableFreeMods | Where CheckCompatibleSet(required + m) | loop n
        [Test]
        public void TestFreeModsFreestyle()
        {
            var required = new[] { new APIMod(new OsuModHardRock()) };
            var result = ModUtils.EnumerateUserSelectableFreeMods(MatchType.Playlists, required, Array.Empty<APIMod>(), true, new OsuRuleset());

            Assert.That(result, Is.Not.Empty);
            Assert.That(result.Any(m => m is OsuModHidden), Is.True);
            // incompatible with / duplicate of the required hard rock mod.
            Assert.That(result.Any(m => m is OsuModEasy), Is.False);
            Assert.That(result.Any(m => m is OsuModHardRock), Is.False);
            // system mods are never selectable.
            Assert.That(result.Any(m => m is OsuModTouchDevice), Is.False);
        }

        // CF: ModUtils.EnumerateUserSelectableFreeMods | freestyle | FALSE
        [Test]
        public void TestFreeModsNonFreestyleUsesAllowedMods()
        {
            var allowed = new[] { new APIMod(new OsuModHidden()), new APIMod(new OsuModDoubleTime()) };
            var result = ModUtils.EnumerateUserSelectableFreeMods(MatchType.Playlists, Array.Empty<APIMod>(), allowed, false, new OsuRuleset());
            Assert.That(result.Select(m => m.GetType()), Is.EqualTo(new[] { typeof(OsuModHidden), typeof(OsuModDoubleTime) }));
        }

        private class SelfIncompatibleMod : OsuModHidden
        {
            public override Type[] IncompatibleMods => new[] { typeof(OsuModHidden) };
        }

        private class NotFreeMod : OsuModHidden
        {
            public override bool ValidForMultiplayerAsFreeMod => false;
        }

        private class NoImplementationMod : OsuModHidden
        {
            public override bool HasImplementation => false;
        }

        private class NoImplementationRateMod : OsuModDoubleTime
        {
            public override bool HasImplementation => false;
        }
    }
}
