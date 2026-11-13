// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Mania.Tests
{
    [TestFixture]
    public class ManiaRulesetTest
    {
        private readonly ManiaRuleset ruleset = new ManiaRuleset();

        private class FakeSkin : ISkin
        {
            public Drawable? GetDrawableComponent(ISkinComponentLookup lookup) => null;
            public Texture? GetTexture(string componentName, osu.Framework.Graphics.Textures.WrapMode wrapModeS, osu.Framework.Graphics.Textures.WrapMode wrapModeT) => null;
            public ISample? GetSample(ISampleInfo sampleInfo) => null;
            public IBindable<TValue>? GetConfig<TLookup, TValue>(TLookup lookup) where TLookup : notnull where TValue : notnull => null;
        }

        // CF: ManiaRuleset.CreateSkinTransformer | switch(skin) no case matches | default
        // CF: ManiaRuleset.CreateSkinTransformer | switch(skin) no case matches | null
        [Test]
        public void TestUnknownSkinHasNoTransformer() => Assert.That(ruleset.CreateSkinTransformer(new FakeSkin(), new Beatmap()), Is.Null);

        // CF: ManiaRuleset.ConvertFromLegacyMods | every HasFlag TRUE | TRUE
        [Test]
        public void TestConvertFromLegacyModsAllFlags()
        {
            var legacy = LegacyMods.Nightcore | LegacyMods.Perfect | LegacyMods.Cinema | LegacyMods.Easy | LegacyMods.FadeIn | LegacyMods.Flashlight | LegacyMods.HalfTime | LegacyMods.HardRock
                         | LegacyMods.Hidden | LegacyMods.Key1 | LegacyMods.Key2 | LegacyMods.Key3 | LegacyMods.Key4 | LegacyMods.Key5 | LegacyMods.Key6 | LegacyMods.Key7 | LegacyMods.Key8 | LegacyMods.Key9
                         | LegacyMods.KeyCoop | LegacyMods.NoFail | LegacyMods.Random | LegacyMods.Mirror | LegacyMods.ScoreV2;

            var types = ruleset.ConvertFromLegacyMods(legacy).Select(m => m.GetType()).ToArray();

            Assert.That(types, Is.EquivalentTo(new[]
            {
                typeof(ManiaModNightcore), typeof(ManiaModPerfect), typeof(ManiaModCinema), typeof(ManiaModEasy), typeof(ManiaModFadeIn), typeof(ManiaModFlashlight),
                typeof(ManiaModHalfTime), typeof(ManiaModHardRock), typeof(ManiaModHidden), typeof(ManiaModKey1), typeof(ManiaModKey2), typeof(ManiaModKey3), typeof(ManiaModKey4),
                typeof(ManiaModKey5), typeof(ManiaModKey6), typeof(ManiaModKey7), typeof(ManiaModKey8), typeof(ManiaModKey9), typeof(ManiaModDualStages), typeof(ManiaModNoFail),
                typeof(ManiaModRandom), typeof(ManiaModMirror), typeof(ManiaModScoreV2)
            }));
        }

        // CF: ManiaRuleset.ConvertFromLegacyMods | Nightcore HasFlag | FALSE
        // CF: ManiaRuleset.ConvertFromLegacyMods | DoubleTime HasFlag | TRUE
        // CF: ManiaRuleset.ConvertFromLegacyMods | Perfect HasFlag | FALSE
        // CF: ManiaRuleset.ConvertFromLegacyMods | SuddenDeath HasFlag | TRUE
        // CF: ManiaRuleset.ConvertFromLegacyMods | Cinema HasFlag | FALSE
        // CF: ManiaRuleset.ConvertFromLegacyMods | Autoplay HasFlag | TRUE
        [Test]
        public void TestConvertFromLegacyModsElseBranches()
        {
            var types = ruleset.ConvertFromLegacyMods(LegacyMods.DoubleTime | LegacyMods.SuddenDeath | LegacyMods.Autoplay).Select(m => m.GetType()).ToArray();
            Assert.That(types, Is.EquivalentTo(new[] { typeof(ManiaModDoubleTime), typeof(ManiaModSuddenDeath), typeof(ManiaModAutoplay) }));
        }

        // CF: ManiaRuleset.ConvertFromLegacyMods | every HasFlag FALSE | FALSE
        [Test]
        public void TestConvertFromNoLegacyMods() => Assert.That(ruleset.ConvertFromLegacyMods(LegacyMods.None), Is.Empty);

        // CF: ManiaRuleset.ConvertToLegacyMods | case ManiaModKey1..Key9 | case ManiaModKey1
        // CF: ManiaRuleset.ConvertToLegacyMods | case ManiaModDualStages | case ManiaModDualStages
        // CF: ManiaRuleset.ConvertToLegacyMods | case ManiaModFadeIn | case ManiaModFadeIn
        // CF: ManiaRuleset.ConvertToLegacyMods | case ManiaModMirror | case ManiaModMirror
        // CF: ManiaRuleset.ConvertToLegacyMods | case ManiaModRandom | case ManiaModRandom
        [Test]
        public void TestConvertToLegacyMods()
        {
            Mod[] mods =
            [
                new ManiaModKey1(), new ManiaModKey2(), new ManiaModKey3(), new ManiaModKey4(), new ManiaModKey5(), new ManiaModKey6(), new ManiaModKey7(), new ManiaModKey8(), new ManiaModKey9(),
                new ManiaModDualStages(), new ManiaModMirror(), new ManiaModRandom()
            ];

            var expected = LegacyMods.Key1 | LegacyMods.Key2 | LegacyMods.Key3 | LegacyMods.Key4 | LegacyMods.Key5 | LegacyMods.Key6 | LegacyMods.Key7 | LegacyMods.Key8 | LegacyMods.Key9
                           | LegacyMods.KeyCoop | LegacyMods.Mirror | LegacyMods.Random;

            Assert.That(ruleset.ConvertToLegacyMods(mods), Is.EqualTo(expected));
        }

        // CF: ManiaRuleset.ConvertToLegacyMods | case ManiaModFadeIn | case ManiaModFadeIn
        [Test]
        public void TestFadeInIsNotHidden()
        {
            var legacy = ruleset.ConvertToLegacyMods([new ManiaModFadeIn()]);
            Assert.That(legacy.HasFlag(LegacyMods.FadeIn), Is.True);
            Assert.That(legacy.HasFlag(LegacyMods.Hidden), Is.False);
        }

        // CF: ManiaRuleset.ConvertToLegacyMods | switch(mod) no case matches | default
        [Test]
        public void TestConvertToLegacyModsIgnoresUnrelated() => Assert.That(ruleset.ConvertToLegacyMods([new ManiaModNoRelease()]), Is.EqualTo(LegacyMods.None));

        // CF: ManiaRuleset.GameplayVariants | for i <= MAX_STAGE_KEYS | loop n
        // CF: ManiaRuleset.GameplayVariants | for i += 2 <= 2 * MAX_STAGE_KEYS | loop n
        [Test]
        public void TestGameplayVariants()
        {
            var variants = ruleset.GameplayVariants.ToArray();
            Assert.That(variants, Has.Length.EqualTo(20));
            Assert.That(variants.Take(10), Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(variants.Skip(10), Is.EqualTo(Enumerable.Range(1, 10).Select(i => 1000 + 2 * i)));
        }

        // CF: ManiaRuleset.GetDefaultKeyBindings | variant == EDITOR_VARIANT | TRUE
        [Test]
        public void TestEditorKeyBindings()
        {
            var bindings = ruleset.GetDefaultKeyBindings(Ruleset.EDITOR_VARIANT).ToArray();
            Assert.That(bindings.Select(b => (ManiaAction)b.Action), Is.EqualTo(new[] { ManiaAction.EditorNoteTool, ManiaAction.EditorHoldNoteTool }));
        }

        // CF: ManiaRuleset.GetDefaultKeyBindings | variant == EDITOR_VARIANT | FALSE
        // CF: ManiaRuleset.GetDefaultKeyBindings | case PlayfieldType.Single | case Single
        // CF: ManiaRuleset.GetDefaultKeyBindings | case PlayfieldType.Dual | case Dual
        [TestCase(4, 4)]
        [TestCase(1008, 8)]
        public void TestKeyBindingsCoverEveryColumn(int variant, int columns)
        {
            var actions = ruleset.GetDefaultKeyBindings(variant).Select(b => (ManiaAction)b.Action).Distinct().ToArray();
            Assert.That(actions, Is.EqualTo(Enumerable.Range(0, columns).Select(i => (ManiaAction)i)));
        }

        // CF: ManiaRuleset.GetVariantName | variant == EDITOR_VARIANT | TRUE
        // CF: ManiaRuleset.GetVariantName | switch playfield type | default
        // CF: ManiaRuleset.GetVariantName | case PlayfieldType.Dual | case Dual
        [Test]
        public void TestVariantNames()
        {
            Assert.That(ruleset.GetVariantName(7).ToString(), Is.EqualTo("7K"));
            Assert.That(ruleset.GetVariantName(1008).ToString(), Is.EqualTo("4K + 4K"));
            Assert.That(ruleset.GetVariantName(Ruleset.EDITOR_VARIANT).ToString(), Is.Not.EqualTo($"{Ruleset.EDITOR_VARIANT}K"));
        }

        // CF: ManiaRuleset.GetValidHitResults | list | non-null
        [Test]
        public void TestValidHitResults() => Assert.That(ruleset.GetValidHitResults(), Does.Contain(HitResult.Perfect).And.Contain(HitResult.Miss).And.Not.Contain(HitResult.LargeTickHit));

        private static BeatmapInfo mapInfo(float od = 5, float cs = 4) => new BeatmapInfo(new ManiaRuleset().RulesetInfo, new BeatmapDifficulty { OverallDifficulty = od, CircleSize = cs, DrainRate = 5 });

        // CF: ManiaRuleset.GetAdjustedDisplayDifficulty | mods.Any(ManiaModHardRock) | FALSE
        // CF: ManiaRuleset.GetAdjustedDisplayDifficulty | mods.Any(ManiaModEasy) | FALSE
        [Test]
        public void TestAdjustedDifficultyWithoutMods()
        {
            var d = ruleset.GetAdjustedDisplayDifficulty(mapInfo(), []);
            Assert.That(d.OverallDifficulty, Is.EqualTo(5).Within(1e-4));
            Assert.That(d.CircleSize, Is.EqualTo(4));
        }

        // CF: ManiaRuleset.GetAdjustedDisplayDifficulty | mods.Any(ManiaModHardRock) | TRUE
        [Test]
        public void TestHardRockMakesHitWindowsSmaller()
        {
            var d = ruleset.GetAdjustedDisplayDifficulty(mapInfo(), [new ManiaModHardRock()]);
            Assert.That(d.OverallDifficulty, Is.GreaterThan(5));
        }

        // CF: ManiaRuleset.GetAdjustedDisplayDifficulty | mods.Any(ManiaModEasy) | TRUE
        [Test]
        public void TestEasyMakesHitWindowsLarger()
        {
            var d = ruleset.GetAdjustedDisplayDifficulty(mapInfo(), [new ManiaModEasy()]);
            Assert.That(d.OverallDifficulty, Is.LessThan(5));
        }

        // CF: ManiaRuleset.GetKeyCount | mods | null
        // CF: ManiaRuleset.GetVariantForBeatmap | key mods applied on conversion | non-null
        [Test]
        public void TestKeyCountAndVariant()
        {
            Assert.That(ruleset.GetKeyCount(mapInfo(cs: 7)), Is.EqualTo(7));
            Assert.That(ruleset.GetVariantForBeatmap(new BeatmapInfo(new RulesetInfo { OnlineID = 0, ShortName = "osu" }, new BeatmapDifficulty { CircleSize = 7 }), [new ManiaModKey4()]), Is.EqualTo(4));
            Assert.That(ruleset.GetKeyCount(mapInfo(cs: 7), null), Is.EqualTo(7));
        }

        // CF: ManiaRuleset.GetBeatmapAttributesForRankedPlayCard | beatmapInfo.TotalObjectCount == 0 | TRUE
        [Test]
        public void TestRankedPlayCardNoObjects()
        {
            var info = mapInfo();
            info.TotalObjectCount = 0;
            info.EndTimeObjectCount = 0;
            var attributes = ruleset.GetBeatmapAttributesForRankedPlayCard(info, []).ToList();

            Assert.That(attributes[0].Acronym, Is.EqualTo("HN"));
            Assert.That(attributes[0].OriginalValue, Is.Zero);
            Assert.That(attributes.Any(a => a.Acronym == "KC"), Is.False);
        }

        // CF: ManiaRuleset.GetBeatmapAttributesForRankedPlayCard | beatmapInfo.TotalObjectCount == 0 | FALSE
        [Test]
        public void TestRankedPlayCardHoldNoteRatio()
        {
            var info = mapInfo();
            info.TotalObjectCount = 200;
            info.EndTimeObjectCount = 50;

            Assert.That(ruleset.GetBeatmapAttributesForRankedPlayCard(info, []).First().OriginalValue, Is.EqualTo(0.25f));
        }

        // CF: ManiaRuleset.GetBeatmapAttributesForDisplay | hitWindows.IsConvert = !Ruleset.Equals | FALSE
        // CF: ManiaRuleset.GetBeatmapAttributesForDisplay | mods.Any(ManiaModClassic) | FALSE
        [Test]
        public void TestDisplayAttributes()
        {
            var attributes = ruleset.GetBeatmapAttributesForDisplay(mapInfo(), []).ToList();
            Assert.That(attributes.Select(a => a.Acronym), Is.EqualTo(new[] { "KC", "OD", "HP" }));
        }

        // CF: ManiaRuleset.GetBeatmapAttributesForDisplay | hitWindows.IsConvert = !Ruleset.Equals | TRUE
        // CF: ManiaRuleset.GetBeatmapAttributesForDisplay | mods.Any(ManiaModClassic) | TRUE
        [Test]
        public void TestDisplayAttributesConvertedClassic()
        {
            var info = new BeatmapInfo(new RulesetInfo { OnlineID = 0, ShortName = "osu" }, new BeatmapDifficulty { OverallDifficulty = 5, CircleSize = 4, DrainRate = 5 });
            Assert.That(ruleset.GetBeatmapAttributesForDisplay(info, [new ManiaModClassic()]).Count(), Is.EqualTo(3));
        }

        // CF: ManiaRuleset.CreateRulesetFilterCriteria | factory | non-null
        [Test]
        public void TestMiscFactories()
        {
            Assert.That(ruleset.CreateRulesetFilterCriteria(), Is.TypeOf<ManiaFilterCriteria>());
            Assert.That(ruleset.CreateScoreProcessor(), Is.TypeOf<ManiaScoreProcessor>());
            Assert.That(ruleset.CreateHealthProcessor(0), Is.TypeOf<ManiaHealthProcessor>());
            Assert.That(ruleset.LegacyID, Is.EqualTo(3));
        }
    }
}
