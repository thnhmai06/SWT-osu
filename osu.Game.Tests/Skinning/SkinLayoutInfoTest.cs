// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets;
using osu.Game.Skinning;

namespace osu.Game.Tests.Skinning
{
    [TestFixture]
    public class SkinLayoutInfoTest
    {
        private static readonly RulesetInfo osu = new RulesetInfo { ShortName = "osu" };

        // CF: SkinLayoutInfo.TryGetDrawableInfo | ruleset?.ShortName ?? global_identifier | null
        // CF: SkinLayoutInfo.TryGetDrawableInfo | DrawableInfo.TryGetValue | FALSE
        [Test]
        public void TestMissingGlobal()
        {
            Assert.That(new SkinLayoutInfo().TryGetDrawableInfo(null, out var components), Is.False);
            Assert.That(components, Is.Null);
        }

        // CF: SkinLayoutInfo.Update | ruleset?.ShortName ?? global_identifier | null
        // CF: SkinLayoutInfo.TryGetDrawableInfo | DrawableInfo.TryGetValue | TRUE
        [Test]
        public void TestUpdateAndGetGlobal()
        {
            var info = new SkinLayoutInfo();
            var drawables = new[] { new SerialisedDrawableInfo() };
            info.Update(null, drawables);

            Assert.That(info.TryGetDrawableInfo(null, out var components), Is.True);
            Assert.That(components, Is.SameAs(drawables));
        }

        // CF: SkinLayoutInfo.Update | ruleset?.ShortName ?? global_identifier | non-null
        // CF: SkinLayoutInfo.TryGetDrawableInfo | ruleset?.ShortName ?? global_identifier | non-null
        [Test]
        public void TestRulesetEntriesAreSeparateFromGlobal()
        {
            var info = new SkinLayoutInfo();
            info.Update(osu, new[] { new SerialisedDrawableInfo(), new SerialisedDrawableInfo() });
            info.Update(null, new[] { new SerialisedDrawableInfo() });

            Assert.That(info.TryGetDrawableInfo(osu, out var osuComponents), Is.True);
            Assert.That(osuComponents, Has.Length.EqualTo(2));
            Assert.That(info.AllDrawables.Count(), Is.EqualTo(3));
        }

        // CF: SkinLayoutInfo.Reset | ruleset?.ShortName ?? global_identifier | non-null
        // CF: SkinLayoutInfo.Reset | ruleset?.ShortName ?? global_identifier | null
        [Test]
        public void TestReset()
        {
            var info = new SkinLayoutInfo();
            info.Update(osu, new[] { new SerialisedDrawableInfo() });
            info.Update(null, new[] { new SerialisedDrawableInfo() });

            info.Reset(osu);
            Assert.That(info.TryGetDrawableInfo(osu, out _), Is.False);
            Assert.That(info.TryGetDrawableInfo(null, out _), Is.True);

            info.Reset(null);
            Assert.That(info.AllDrawables, Is.Empty);
        }
    }
}
