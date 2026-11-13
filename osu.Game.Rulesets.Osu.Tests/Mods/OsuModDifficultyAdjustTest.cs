// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [TestFixture]
    public class OsuModDifficultyAdjustTest
    {
        [SetUp]
        public void SetUp()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !IsExactlyOneSettingChanged | TRUE
        [Test]
        public void TestIconInformationEmptyWhenNothingOrManyChanged()
        {
            var mod = new OsuModDifficultyAdjust();
            Assert.That(mod.ExtendedIconInformation, Is.Empty);

            mod.CircleSize.Value = 3;
            mod.ApproachRate.Value = 9;
            Assert.That(mod.ExtendedIconInformation, Is.Empty);
        }

        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !CircleSize.IsDefault | TRUE
        [Test]
        public void TestIconInformationCircleSize()
        {
            Assert.That(new OsuModDifficultyAdjust { CircleSize = { Value = 3 } }.ExtendedIconInformation, Is.EqualTo("CS3"));
        }

        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !CircleSize.IsDefault | FALSE
        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !ApproachRate.IsDefault | TRUE
        [Test]
        public void TestIconInformationApproachRate()
        {
            Assert.That(new OsuModDifficultyAdjust { ApproachRate = { Value = 9.5f } }.ExtendedIconInformation, Is.EqualTo("AR9.5"));
        }

        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !ApproachRate.IsDefault | FALSE
        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !OverallDifficulty.IsDefault | TRUE
        [Test]
        public void TestIconInformationOverallDifficulty()
        {
            Assert.That(new OsuModDifficultyAdjust { OverallDifficulty = { Value = 8 } }.ExtendedIconInformation, Is.EqualTo("OD8"));
        }

        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !OverallDifficulty.IsDefault | FALSE
        // CF: OsuModDifficultyAdjust.ExtendedIconInformation | !DrainRate.IsDefault | TRUE
        [Test]
        public void TestIconInformationDrainRate()
        {
            Assert.That(new OsuModDifficultyAdjust { DrainRate = { Value = 2 } }.ExtendedIconInformation, Is.EqualTo("HP2"));
        }

        // CF: OsuModDifficultyAdjust.SettingDescription | !CircleSize.IsDefault | FALSE
        // CF: OsuModDifficultyAdjust.SettingDescription | !ApproachRate.IsDefault | FALSE
        [Test]
        public void TestDescriptionOmitsDefaults()
        {
            Assert.That(new OsuModDifficultyAdjust().SettingDescription.Select(d => d.setting.ToString()), Does.Not.Contain("Circle size").And.Not.Contain("Approach rate"));
        }

        // CF: OsuModDifficultyAdjust.SettingDescription | !CircleSize.IsDefault | TRUE
        // CF: OsuModDifficultyAdjust.SettingDescription | !ApproachRate.IsDefault | TRUE
        [Test]
        public void TestDescriptionIncludesCircleSizeFirstAndApproachRateLast()
        {
            var mod = new OsuModDifficultyAdjust { CircleSize = { Value = 4 }, ApproachRate = { Value = 9 }, OverallDifficulty = { Value = 7 } };

            var items = mod.SettingDescription.Select(d => (d.setting.ToString(), d.value.ToString())).ToList();

            Assert.That(items.First(), Is.EqualTo(("Circle size", "4.0")));
            Assert.That(items.Last(), Is.EqualTo(("Approach rate", "9.0")));
            Assert.That(items, Has.Count.GreaterThan(2));
        }

        [Test]
        public void TestIncompatibleWithTargetPractice()
        {
            Assert.That(new OsuModDifficultyAdjust().IncompatibleMods, Does.Contain(typeof(OsuModTargetPractice)));
        }

        // CF: OsuModDifficultyAdjust.ApplySettings | CircleSize.Value != null | TRUE
        // CF: OsuModDifficultyAdjust.ApplySettings | ApproachRate.Value != null | TRUE
        [Test]
        public void TestOverridesAreApplied()
        {
            var difficulty = new BeatmapDifficulty { CircleSize = 4, ApproachRate = 5 };
            new OsuModDifficultyAdjust { CircleSize = { Value = 7 }, ApproachRate = { Value = 9 } }.ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(7));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(9));
        }

        // CF: OsuModDifficultyAdjust.ApplySettings | CircleSize.Value != null | FALSE
        // CF: OsuModDifficultyAdjust.ApplySettings | ApproachRate.Value != null | FALSE
        [Test]
        public void TestNoOverridesLeaveDifficultyUntouched()
        {
            var difficulty = new BeatmapDifficulty { CircleSize = 4, ApproachRate = 5 };
            new OsuModDifficultyAdjust().ApplyToDifficulty(difficulty);

            Assert.That(difficulty.CircleSize, Is.EqualTo(4));
            Assert.That(difficulty.ApproachRate, Is.EqualTo(5));
        }
    }
}
