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
    public class CheckLowDiffOverlapsTest
    {
        private readonly CheckLowDiffOverlaps check = new CheckLowDiffOverlaps();

        private static HitCircle circle(double time, float x) => new HitCircle { StartTime = time, Position = new Vector2(x, 0) };

        private List<Issue> run(DifficultyRating rating, params HitObject[] objects)
        {
            var beatmap = new Beatmap<HitObject> { HitObjects = objects.ToList() };
            return check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap), rating)).ToList();
        }

        private List<Issue> run(params HitObject[] objects) => run(DifficultyRating.Easy, objects);

        private static Slider slider()
        {
            var s = new Slider
            {
                StartTime = 0,
                Position = Vector2.Zero,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return s;
        }

        // CF: CheckLowDiffOverlaps.Run | context.InterpretedDifficulty > DifficultyRating.Easy | TRUE
        [Test]
        public void TestHigherDifficultiesAreSkipped()
        {
            Assert.That(run(DifficultyRating.Expert, circle(0, 0), circle(100, 200)), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | for (i < hitObjects.Count - 1) | loop 0
        // CF: CheckLowDiffOverlaps.Run | context.InterpretedDifficulty > DifficultyRating.Easy | FALSE
        [Test]
        public void TestSingleObjectHasNoPairs()
        {
            Assert.That(run(circle(0, 0)), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | !(hitObjects[i] is OsuHitObject hitObject) | TRUE
        // CF: CheckLowDiffOverlaps.Run | hitObject is Spinner | TRUE
        [Test]
        public void TestSpinnerFirstIsSkipped()
        {
            Assert.That(run(new Spinner { StartTime = 0, Duration = 50 }, circle(100, 200)), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | !(hitObjects[i] is OsuHitObject hitObject) | FALSE
        // CF: CheckLowDiffOverlaps.Run | hitObject is Spinner | FALSE
        // CF: CheckLowDiffOverlaps.Run | !(hitObjects[i + 1] is OsuHitObject nextHitObject) | FALSE
        // CF: CheckLowDiffOverlaps.Run | nextHitObject is Spinner | TRUE
        [Test]
        public void TestSpinnerSecondIsSkipped()
        {
            Assert.That(run(circle(0, 0), new Spinner { StartTime = 100, Duration = 50 }), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | !(hitObjects[i + 1] is OsuHitObject nextHitObject) | TRUE
        [Test]
        public void TestNonOsuObjectSecondIsSkipped()
        {
            Assert.That(run(circle(0, 0), new NonOsuObject { StartTime = 100 }), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | nextHitObject is Spinner | FALSE
        // CF: CheckLowDiffOverlaps.Run | deltaTime >= hitObject.TimeFadeIn + hitObject.TimePreempt | TRUE
        [Test]
        public void TestObjectsNotVisibleTogetherAreSkipped()
        {
            Assert.That(run(circle(0, 0), circle(2000, 20)), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | deltaTime >= hitObject.TimeFadeIn + hitObject.TimePreempt | FALSE
        // CF: CheckLowDiffOverlaps.Run | !areOverlapping | TRUE
        // CF: CheckLowDiffOverlaps.Run | !(hitObject is Slider) | TRUE
        // CF: CheckLowDiffOverlaps.Run | deltaTime < should_overlap_threshold | TRUE
        // CF: CheckLowDiffOverlaps.Run | areOverlapping | FALSE
        [Test]
        public void TestTooCloseWithoutOverlapShouldOverlap()
        {
            var issues = run(circle(0, 0), circle(100, 200));
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckLowDiffOverlaps.IssueTemplateShouldOverlap>());
        }

        // CF: CheckLowDiffOverlaps.Run | deltaTime < should_overlap_threshold | FALSE
        // CF: CheckLowDiffOverlaps.Run | deltaTime < should_probably_overlap_threshold | TRUE
        [Test]
        public void TestCloseWithoutOverlapShouldProbablyOverlap()
        {
            var issues = run(circle(0, 0), circle(167, 200));
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckLowDiffOverlaps.IssueTemplateShouldProbablyOverlap>());
        }

        // CF: CheckLowDiffOverlaps.Run | deltaTime < should_probably_overlap_threshold | FALSE
        [Test]
        public void TestFarApartWithoutOverlapIsOk()
        {
            Assert.That(run(circle(0, 0), circle(500, 200)), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | areOverlapping | TRUE
        // CF: CheckLowDiffOverlaps.Run | deltaTime > should_not_overlap_threshold | TRUE
        [Test]
        public void TestOverlapFarApartShouldNotOverlap()
        {
            var issues = run(circle(0, 0), circle(500, 20));
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckLowDiffOverlaps.IssueTemplateShouldNotOverlap>());
        }

        // CF: CheckLowDiffOverlaps.Run | !areOverlapping | FALSE
        // CF: CheckLowDiffOverlaps.Run | deltaTime > should_not_overlap_threshold | FALSE
        [Test]
        public void TestOverlapCloseIsOk()
        {
            Assert.That(run(circle(0, 0), circle(167, 20)), Is.Empty);
        }

        // CF: CheckLowDiffOverlaps.Run | !(hitObject is Slider) | FALSE
        [Test]
        public void TestSliderTailNeedsNoOverlap()
        {
            var s = slider();
            Assert.That(run(s, circle(s.EndTime + 100, 300)), Is.Empty);
        }

        [Test]
        public void TestSliderTailOverlapFarApartShouldNotOverlap()
        {
            var s = slider();
            var issues = run(s, circle(s.EndTime + 500, 120));
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckLowDiffOverlaps.IssueTemplateShouldNotOverlap>());
        }

        private class NonOsuObject : HitObject
        {
        }
    }
}
