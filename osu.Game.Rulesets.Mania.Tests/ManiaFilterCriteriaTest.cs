// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Rulesets.Mania.Tests
{
    [TestFixture]
    public class ManiaFilterCriteriaTest
    {
        [SetUp]
        public void SetUp()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        private static BeatmapInfo maniaMap(float keys, int total = -1, int holds = -1) => new BeatmapInfo(new ManiaRuleset().RulesetInfo, new BeatmapDifficulty { CircleSize = keys })
        {
            TotalObjectCount = total,
            EndTimeObjectCount = holds
        };

        private static bool matches(ManiaFilterCriteria c, BeatmapInfo b, FilterCriteria? f = null) => c.Matches(b, f ?? new FilterCriteria());

        private static ManiaFilterCriteria withKeys(Operator op, string value)
        {
            var c = new ManiaFilterCriteria();
            Assert.That(c.TryParseCustomKeywordCriteria("keys", op, value), Is.True);
            return c;
        }

        // CF: ManiaFilterCriteria.Matches | keyCountMatch (includedKeyCounts.Contains) | TRUE
        // CF: ManiaFilterCriteria.Matches | keyCountMatch (includedKeyCounts.Contains) | FALSE
        // CF: ManiaFilterCriteria.Matches | !longNotePercentage.HasFilter | TRUE
        // CF: ManiaFilterCriteria.Matches | keyCountMatch && longNotePercentageMatch | TRUE
        // CF: ManiaFilterCriteria.Matches | keyCountMatch && longNotePercentageMatch | FALSE
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case key | case key
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case Operator.Equal | case Equal
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | foreach strValue | loop n
        [Test]
        public void TestKeysEqualMultipleValues()
        {
            var c = withKeys(Operator.Equal, "1,3");

            Assert.That(matches(c, maniaMap(1)), Is.True);
            Assert.That(matches(c, maniaMap(2)), Is.False);
            Assert.That(matches(c, maniaMap(3)), Is.True);
        }

        // CF: ManiaFilterCriteria.Matches | key count taken from mods | non-null
        [Test]
        public void TestKeyModOverridesKeyCountOnConvert()
        {
            var c = withKeys(Operator.Equal, "1");

            var converted = new BeatmapInfo(new RulesetInfo { OnlineID = 0 }, new BeatmapDifficulty { CircleSize = 4 });
            Assert.That(matches(c, converted, new FilterCriteria { Mods = [new ManiaModKey1()] }), Is.True);
            Assert.That(matches(c, converted), Is.False);
        }

        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case key | case key
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case Operator.NotEqual | case NotEqual
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | keyCounts.Count == 1 | FALSE
        [Test]
        public void TestKeysNotEqual()
        {
            var c = new ManiaFilterCriteria();
            Assert.That(c.TryParseCustomKeywordCriteria("key", Operator.NotEqual, "1,2"), Is.True);

            Assert.That(matches(c, maniaMap(1)), Is.False);
            Assert.That(matches(c, maniaMap(2)), Is.False);
            Assert.That(matches(c, maniaMap(3)), Is.True);
        }

        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | int.TryParse | FALSE
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | !int.TryParse(strValue) | TRUE
        [Test]
        public void TestKeysUnparsableValueRejected()
        {
            Assert.That(new ManiaFilterCriteria().TryParseCustomKeywordCriteria("keys", Operator.Equal, "1,x"), Is.False);
        }

        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | keyCounts.Count == 1 | TRUE
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case Operator.Less | case Less
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case Operator.LessOrEqual | case LessOrEqual
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case Operator.Greater | case Greater
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case Operator.GreaterOrEqual | case GreaterOrEqual
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | singleKeyCount == null | FALSE
        [TestCase(Operator.Less, 4, false)]
        [TestCase(Operator.Less, 3, true)]
        [TestCase(Operator.LessOrEqual, 5, false)]
        [TestCase(Operator.LessOrEqual, 4, true)]
        [TestCase(Operator.Greater, 4, false)]
        [TestCase(Operator.Greater, 5, true)]
        [TestCase(Operator.GreaterOrEqual, 3, false)]
        [TestCase(Operator.GreaterOrEqual, 4, true)]
        public void TestKeysSingleValueComparison(Operator op, int probe, bool expected)
        {
            var c = withKeys(op, "4");
            Assert.That(matches(c, maniaMap(probe)), Is.EqualTo(expected));
        }

        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | singleKeyCount == null (Less) | TRUE
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | singleKeyCount == null (LessOrEqual) | TRUE
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | singleKeyCount == null (Greater) | TRUE
        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | singleKeyCount == null (GreaterOrEqual) | TRUE
        [TestCase(Operator.Less)]
        [TestCase(Operator.LessOrEqual)]
        [TestCase(Operator.Greater)]
        [TestCase(Operator.GreaterOrEqual)]
        public void TestKeysRangeOperatorWithMultipleValuesRejected(Operator op)
        {
            Assert.That(new ManiaFilterCriteria().TryParseCustomKeywordCriteria("keys", op, "1,2"), Is.False);
        }

        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | switch(key) no case matches | default
        [Test]
        public void TestUnknownKeyRejected()
        {
            Assert.That(new ManiaFilterCriteria().TryParseCustomKeywordCriteria("bogus", Operator.Equal, "1"), Is.False);
        }

        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case ln/lns | case ln
        // CF: ManiaFilterCriteria.Matches | !longNotePercentage.HasFilter | FALSE
        // CF: ManiaFilterCriteria.Matches | !isConvertedBeatmap(beatmapInfo) | TRUE
        // CF: ManiaFilterCriteria.Matches | longNotePercentage.IsInRange | TRUE
        // CF: ManiaFilterCriteria.Matches | longNotePercentage.IsInRange | FALSE
        // CF: ManiaFilterCriteria.isConvertedBeatmap | !Ruleset.Equals(mania) | FALSE
        // CF: ManiaFilterCriteria.calculateLongNotePercentage | Math.Max(1, TotalObjectCount) | non-null
        [Test]
        public void TestLongNotePercentage()
        {
            var c = new ManiaFilterCriteria();
            Assert.That(c.TryParseCustomKeywordCriteria("ln", Operator.GreaterOrEqual, "50"), Is.True);

            Assert.That(matches(c, maniaMap(4, total: 10, holds: 5)), Is.True);
            Assert.That(matches(c, maniaMap(4, total: 10, holds: 4)), Is.False);
            // no objects at all must not divide by zero
            Assert.That(matches(c, maniaMap(4, total: 0, holds: 0)), Is.False);
        }

        // CF: ManiaFilterCriteria.TryParseCustomKeywordCriteria | case lns | case lns
        // CF: ManiaFilterCriteria.Matches | !isConvertedBeatmap(beatmapInfo) | FALSE
        // CF: ManiaFilterCriteria.isConvertedBeatmap | !Ruleset.Equals(mania) | TRUE
        [Test]
        public void TestLongNoteFilterRejectsConvertedBeatmaps()
        {
            var c = new ManiaFilterCriteria();
            Assert.That(c.TryParseCustomKeywordCriteria("lns", Operator.GreaterOrEqual, "0"), Is.True);

            var converted = new BeatmapInfo(new RulesetInfo { OnlineID = 0 }, new BeatmapDifficulty { CircleSize = 4 }) { TotalObjectCount = 10, EndTimeObjectCount = 10 };
            Assert.That(matches(c, converted), Is.False);
        }

        private static ValueChangedEvent<IReadOnlyList<Mod>> modChange(IReadOnlyList<Mod> old, IReadOnlyList<Mod> @new) => new ValueChangedEvent<IReadOnlyList<Mod>>(old, @new);

        // CF: ManiaFilterCriteria.FilterMayChangeFromMods | includedKeyCounts.Count != 2 * MAX_STAGE_KEYS | TRUE
        // CF: ManiaFilterCriteria.FilterMayChangeFromMods | !oldSet.SetEquals(newSet) | TRUE
        [Test]
        public void TestFilteredKeyCountsAndKeyModChanged()
        {
            var c = withKeys(Operator.Equal, "4");
            Assert.That(c.FilterMayChangeFromMods(new FilterCriteria(), modChange([], [new ManiaModKey4()])), Is.True);
        }

        // CF: ManiaFilterCriteria.FilterMayChangeFromMods | !oldSet.SetEquals(newSet) | FALSE
        [Test]
        public void TestFilteredKeyCountsAndKeyModsUnchanged()
        {
            var c = withKeys(Operator.Equal, "4");
            var mod = new ManiaModKey4();
            Assert.That(c.FilterMayChangeFromMods(new FilterCriteria(), modChange([mod], [mod])), Is.False);
        }

        // CF: ManiaFilterCriteria.FilterMayChangeFromMods | includedKeyCounts.Count != 2 * MAX_STAGE_KEYS | FALSE
        // CF: ManiaFilterCriteria.FilterMayChangeFromMods | criteria.Group == GroupMode.Variant | FALSE
        [Test]
        public void TestUnfilteredAndNotGroupedByVariantIgnoresModChange()
        {
            var c = new ManiaFilterCriteria();
            Assert.That(c.FilterMayChangeFromMods(new FilterCriteria { Group = GroupMode.None }, modChange([], [new ManiaModKey4()])), Is.False);
        }

        // CF: ManiaFilterCriteria.FilterMayChangeFromMods | criteria.Group == GroupMode.Variant | TRUE
        [Test]
        public void TestVariantGroupingConsidersKeyModChange()
        {
            var c = new ManiaFilterCriteria();
            Assert.That(c.FilterMayChangeFromMods(new FilterCriteria { Group = GroupMode.Variant }, modChange([], [new ManiaModKey4()])), Is.True);
        }
    }
}
