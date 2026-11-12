// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using static osu.Game.Rulesets.Osu.Tests.Editor.Checks.CheckTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    [TestFixture]
    public class CheckTooShortSlidersTest
    {
        private readonly CheckTooShortSliders check = new CheckTooShortSliders();

        // velocity 0.14 px/ms: span duration = length / 0.14

        [Test]
        public void TestShortSlider_OnEasy_ReportsProblem()
        {
            // difficulty <= Easy; Slider TRUE && SpanDuration < 125 TRUE (10px => ~71ms)
            var issues = check.Run(Context(new HitObject[] { Slider(0, 100, 100, 10) })).ToList();

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTooShortSliders.IssueTemplateTooShort>());
        }

        [Test]
        public void TestLongSlider_OnEasy_NoIssue()
        {
            // Slider TRUE && SpanDuration < 125 FALSE (100px => ~714ms)
            Assert.That(check.Run(Context(new HitObject[] { Slider(0, 100, 100, 100) })), Is.Empty);
        }

        [Test]
        public void TestSliderAtExactThreshold_NoIssue()
        {
            // 17.5px / 0.14 = 125ms is not strictly below the threshold
            Assert.That(check.Run(Context(new HitObject[] { Slider(0, 100, 100, 17.5f) })), Is.Empty);
        }

        [Test]
        public void TestCircle_NoIssue()
        {
            // `hitObject is Slider` FALSE
            Assert.That(check.Run(Context(new HitObject[] { Circle(0) })), Is.Empty);
        }

        [Test]
        public void TestShortSlider_AboveEasy_IsSkipped()
        {
            // InterpretedDifficulty > Easy TRUE -> yield break
            Assert.That(check.Run(Context(new HitObject[] { Slider(0, 100, 100, 10) }, DifficultyRating.Normal)), Is.Empty);
        }

        [Test]
        public void TestSpanDurationIsUsedRatherThanTotalDuration()
        {
            // the per-span duration is compared: 20px with 3 repeats has ~143ms spans (ok), 10px with a repeat has ~71ms spans (too short)
            Assert.That(check.Run(Context(new HitObject[] { Slider(0, 100, 100, 20, repeats: 3) })), Is.Empty);
            Assert.That(check.Run(Context(new HitObject[] { Slider(0, 100, 100, 10, repeats: 1) })).Count(), Is.EqualTo(1));
        }

        [Test]
        public void TestOnlyShortSlidersAreReported()
        {
            var issues = check.Run(Context(new HitObject[] { Slider(0, 100, 100, 10), Circle(500), Slider(1000, 100, 100, 100), Slider(3000, 100, 100, 5) })).ToList();

            Assert.That(issues, Has.Count.EqualTo(2));
        }
    }
}
