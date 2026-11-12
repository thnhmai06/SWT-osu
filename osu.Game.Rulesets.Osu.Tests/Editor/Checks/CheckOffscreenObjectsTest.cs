// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using static osu.Game.Rulesets.Osu.Tests.Editor.Checks.CheckTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    [TestFixture]
    public class CheckOffscreenObjectsTest
    {
        private readonly CheckOffscreenObjects check = new CheckOffscreenObjects();

        // Radius 64; allowed centre range is x in [-3, 515], y in [4, 364].

        private int circleIssues(float x, float y, int stackHeight = 0)
        {
            var circle = Circle(0, x, y);
            circle.StackHeight = stackHeight;
            return check.Run(Context(new HitObject[] { circle })).Count(i => i.Template is CheckOffscreenObjects.IssueTemplateOffscreenCircle);
        }

        [Test]
        public void TestCircleInsidePlayfield_NoIssue() => Assert.That(circleIssues(256, 192), Is.EqualTo(0));

        [Test]
        public void TestCircleOnAllExactEdges_NoIssue()
        {
            // every comparison is strict: x-r == -67, x+r == 579, y-r == -60, y+r == 428 are still onscreen
            Assert.That(circleIssues(-3, 4), Is.EqualTo(0));
            Assert.That(circleIssues(515, 364), Is.EqualTo(0));
        }

        [Test]
        public void TestCircleOffLeft_Reported() => Assert.That(circleIssues(-4, 100), Is.EqualTo(1)); // X - r < min_x TRUE

        [Test]
        public void TestCircleOffRight_Reported() => Assert.That(circleIssues(516, 100), Is.EqualTo(1)); // X + r > max_x TRUE (left FALSE)

        [Test]
        public void TestCircleOffTop_Reported() => Assert.That(circleIssues(100, 3), Is.EqualTo(1)); // Y - r < min_y TRUE (x conditions FALSE)

        [Test]
        public void TestCircleOffBottom_Reported() => Assert.That(circleIssues(100, 365), Is.EqualTo(1)); // Y + r > max_y TRUE (others FALSE)

        [Test]
        public void TestCircle_StackOffsetIsTakenIntoAccount()
        {
            // raw position is fine, but the stack shifts it by (-6.4, -6.4) per level
            Assert.That(circleIssues(-3, 100), Is.EqualTo(0));
            Assert.That(circleIssues(-3, 100, stackHeight: 1), Is.EqualTo(1));
        }

        [Test]
        public void TestSpinner_IsIgnored()
        {
            // switch: neither Slider nor HitCircle
            Assert.That(check.Run(Context(new HitObject[] { Spinner(0, 1000) })), Is.Empty);
        }

        // ---- sliders ----

        [Test]
        public void TestSliderInsidePlayfield_NoIssue()
        {
            Assert.That(check.Run(Context(new HitObject[] { Slider(0, 100, 100, 100) })), Is.Empty);
        }

        [Test]
        public void TestSliderLeavingScreenMidway_ReportsFirstOffscreenPoint()
        {
            // starts at x 400, extends to x 700; the first sampled point beyond x 515 is at 5px steps => offset 120 (progress 0.4)
            var slider = Slider(0, 400, 200, 300);

            var issues = check.Run(Context(new HitObject[] { slider })).ToList();

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckOffscreenObjects.IssueTemplateOffscreenSlider>());
            Assert.That(issues[0].Time, Is.EqualTo(slider.StartTime + 0.4 * slider.SpanDuration).Within(1e-3));
        }

        [Test]
        public void TestSliderStartingOffscreen_ReportedAtItsStart()
        {
            // first iteration (progress 0) is already offscreen
            var slider = Slider(1000, -100, 200, 50);

            var issue = check.Run(Context(new HitObject[] { slider })).Single();

            Assert.That(issue.Time, Is.EqualTo(1000).Within(1e-3));
        }

        [Test]
        public void TestSliderOnlyEndOffscreen_ReportedAtEndByFallbackCheck()
        {
            // length 101: sampling visits offsets 0..100 (all onscreen) and skips the final point which lies at x = 515.2 (> 515)
            var slider = Slider(0, 414.2f, 200, 101);

            var issue = check.Run(Context(new HitObject[] { slider })).Single();

            Assert.That(issue.Template, Is.TypeOf<CheckOffscreenObjects.IssueTemplateOffscreenSlider>());
            Assert.That(issue.Time, Is.EqualTo(slider.EndTime).Within(1e-3));
        }

        [Test]
        public void TestSliderEndOnscreen_NoFallbackIssue()
        {
            // same shape, shifted 1px left so the end is exactly onscreen
            Assert.That(check.Run(Context(new HitObject[] { Slider(0, 413.2f, 200, 101) })), Is.Empty);
        }

        [Test]
        public void TestMultipleObjects_EachCheckedIndependently()
        {
            var issues = check.Run(Context(new HitObject[] { Circle(0, -50, 100), Circle(100, 256, 192), Slider(200, 600, 100, 50) })).ToList();

            Assert.That(issues.Count, Is.EqualTo(2));
        }
    }
}
