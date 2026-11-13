// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckOffscreenObjectsTest
    {
        private static List<Issue> run(params HitObject[] objects)
        {
            var beatmap = new Beatmap<HitObject> { HitObjects = objects.ToList() };
            return new CheckOffscreenObjects().Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap))).ToList();
        }

        private static HitCircle circle(float x, float y) => new HitCircle { Position = new Vector2(x, y) };

        private static Slider slider(float x, float y, float length)
        {
            var s = new Slider
            {
                Position = new Vector2(x, y),
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(length, 0) }),
            };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return s;
        }

        // CF: CheckOffscreenObjects.Run | foreach hitobject | loop 0
        [Test]
        public void TestNoObjects()
        {
            Assert.That(run(), Is.Empty);
        }

        // CF: CheckOffscreenObjects.Run | switch (hitobject) | case HitCircle
        // CF: CheckOffscreenObjects.Run | isOffscreen(circle.StackedPosition, circle.Radius) | FALSE
        // CF: CheckOffscreenObjects.isOffscreen | position.X - radius < min_x | FALSE
        // CF: CheckOffscreenObjects.isOffscreen | position.X + radius > max_x | FALSE
        // CF: CheckOffscreenObjects.isOffscreen | position.Y - radius < min_y | FALSE
        // CF: CheckOffscreenObjects.isOffscreen | position.Y + radius > max_y | FALSE
        [Test]
        public void TestOnscreenCircle()
        {
            Assert.That(run(circle(256, 192)), Is.Empty);
        }

        // CF: CheckOffscreenObjects.Run | foreach hitobject | loop n
        // CF: CheckOffscreenObjects.Run | isOffscreen(circle.StackedPosition, circle.Radius) | TRUE
        // CF: CheckOffscreenObjects.isOffscreen | position.X - radius < min_x | TRUE
        // CF: CheckOffscreenObjects.isOffscreen | position.X + radius > max_x | TRUE
        // CF: CheckOffscreenObjects.isOffscreen | position.Y - radius < min_y | TRUE
        // CF: CheckOffscreenObjects.isOffscreen | position.Y + radius > max_y | TRUE
        [Test]
        public void TestOffscreenCirclesOnEachSide()
        {
            var issues = run(circle(-10, 192), circle(520, 192), circle(256, 0), circle(256, 370));

            Assert.That(issues, Has.Count.EqualTo(4));
            Assert.That(issues, Has.All.Matches<Issue>(i => i.Template is CheckOffscreenObjects.IssueTemplateOffscreenCircle));
        }

        [Test]
        public void TestSpinnerIsIgnored()
        {
            Assert.That(run(new Spinner { Position = new Vector2(-1000, -1000) }), Is.Empty);
        }

        // CF: CheckOffscreenObjects.Run | switch (hitobject) | case Slider
        // CF: CheckOffscreenObjects.sliderIssues | for (i < slider.Distance) | loop n
        // CF: CheckOffscreenObjects.sliderIssues | !isOffscreen(position, slider.Radius) | TRUE
        // CF: CheckOffscreenObjects.sliderIssues | !isOffscreen(slider.StackedEndPosition, slider.Radius) | TRUE
        [Test]
        public void TestOnscreenSlider()
        {
            Assert.That(run(slider(100, 192, 200)), Is.Empty);
        }

        // CF: CheckOffscreenObjects.sliderIssues | !isOffscreen(position, slider.Radius) | FALSE
        [Test]
        public void TestSliderLeavingScreenReportsFirstOffscreenTime()
        {
            var s = slider(256, 192, 400);

            var issues = run(s);

            // sliders with defaults applied have radius 32 at CS5: first sampled point offscreen is x = 256 + 295 = 551 (+ radius > 579)
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckOffscreenObjects.IssueTemplateOffscreenSlider>());
            Assert.That(issues[0].Time, Is.EqualTo(s.StartTime + 295.0 / 400 * s.SpanDuration).Within(1e-3));
        }

        // CF: CheckOffscreenObjects.sliderIssues | !isOffscreen(slider.StackedEndPosition, slider.Radius) | FALSE
        [Test]
        public void TestSliderOnlyEndOffscreenBetweenSteps()
        {
            // sampled every 5 units up to 100 -> x <= 545 is fine, but the end at 548 is offscreen
            var s = slider(445, 192, 103);

            var issues = run(s);

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Time, Is.EqualTo(s.EndTime).Within(1e-3));
        }

        // CF: CheckOffscreenObjects.sliderIssues | for (i < slider.Distance) | loop 0
        [Test]
        public void TestZeroLengthSliderOnscreen()
        {
            Assert.That(run(slider(100, 192, 0)), Is.Empty);
        }
    }
}
