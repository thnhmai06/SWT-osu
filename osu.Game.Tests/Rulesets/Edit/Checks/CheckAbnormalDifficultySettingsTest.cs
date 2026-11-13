// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckAbnormalDifficultySettingsTest
    {
        private class TestCheck : CheckAbnormalDifficultySettings
        {
            public override CheckMetadata Metadata => new CheckMetadata(CheckCategory.Settings, "test");
            public override IEnumerable<Issue> Run(BeatmapVerifierContext context) => [];

            public bool Range(float value, out Issue? issue) => OutOfRange("OD", value, out issue);
            public bool Decimals(float value, out Issue? issue) => HasMoreThanOneDecimalPlace("OD", value, out issue);
        }

        private readonly TestCheck check = new TestCheck();

        // CF: CheckAbnormalDifficultySettings.OutOfRange | value < 0f | TRUE
        [Test]
        public void TestBelowZero()
        {
            Assert.That(check.Range(-0.1f, out var issue), Is.True);
            Assert.That(issue!.Template, Is.InstanceOf<CheckAbnormalDifficultySettings.IssueTemplateOutOfRange>());
        }

        // CF: CheckAbnormalDifficultySettings.OutOfRange | value < 0f | FALSE
        // CF: CheckAbnormalDifficultySettings.OutOfRange | value > 10f | TRUE
        [Test]
        public void TestAboveTen()
        {
            Assert.That(check.Range(10.1f, out var issue), Is.True);
            Assert.That(issue, Is.Not.Null);
        }

        // CF: CheckAbnormalDifficultySettings.OutOfRange | value > 10f | FALSE
        [TestCase(0f)]
        [TestCase(5f)]
        [TestCase(10f)]
        public void TestInRange(float value)
        {
            Assert.That(check.Range(value, out var issue), Is.False);
            Assert.That(issue, Is.Null);
        }

        // CF: CheckAbnormalDifficultySettings.HasMoreThanOneDecimalPlace | float.Round(value, 1) != value | TRUE
        [TestCase(5.55f)]
        [TestCase(0.01f)]
        public void TestTooManyDecimals(float value)
        {
            Assert.That(check.Decimals(value, out var issue), Is.True);
            Assert.That(issue!.Template, Is.InstanceOf<CheckAbnormalDifficultySettings.IssueTemplateMoreThanOneDecimal>());
        }

        // CF: CheckAbnormalDifficultySettings.HasMoreThanOneDecimalPlace | float.Round(value, 1) != value | FALSE
        [TestCase(5f)]
        [TestCase(5.5f)]
        public void TestOneDecimalOrLess(float value)
        {
            Assert.That(check.Decimals(value, out var issue), Is.False);
            Assert.That(issue, Is.Null);
        }

        [Test]
        public void TestPossibleTemplates()
        {
            Assert.That(check.PossibleTemplates.Count(), Is.EqualTo(2));
        }
    }
}
