// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Mods
{
    [TestFixture]
    public class ManiaModDualStagesTest
    {
        private static ManiaBeatmapConverter converter(RulesetInfo ruleset)
        {
            var b = new Beatmap<HitObject>();
            b.BeatmapInfo.Ruleset = ruleset;
            return new ManiaBeatmapConverter(b, new ManiaRuleset());
        }

        // CF: ManiaModDualStages.ApplyToBeatmapConverter | isForCurrentRuleset | FALSE
        [Test]
        public void TestConvertedBeatmapBecomesDual()
        {
            var c = converter(new RulesetInfo { OnlineID = 0, ShortName = "osu" });
            new ManiaModDualStages().ApplyToBeatmapConverter(c);
            Assert.That(c.Dual, Is.True);
        }

        // CF: ManiaModDualStages.ApplyToBeatmapConverter | isForCurrentRuleset | TRUE
        [Test]
        public void TestNativeBeatmapUnchanged()
        {
            var c = converter(new ManiaRuleset().RulesetInfo);
            new ManiaModDualStages().ApplyToBeatmapConverter(c);
            Assert.That(c.Dual, Is.False);
        }
    }
}
