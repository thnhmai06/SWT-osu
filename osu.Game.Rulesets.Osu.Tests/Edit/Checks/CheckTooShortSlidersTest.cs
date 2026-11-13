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
    public class CheckTooShortSlidersTest
    {
        private readonly CheckTooShortSliders check = new CheckTooShortSliders();

        private static Slider slider(float length, int repeats = 0)
        {
            var s = new Slider
            {
                RepeatCount = repeats,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(length, 0) }),
            };
            s.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return s;
        }

        private List<Issue> run(DifficultyRating rating, params HitObject[] objects)
        {
            var beatmap = new Beatmap<HitObject> { HitObjects = objects.ToList() };
            return check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap), rating)).ToList();
        }

        // CF: CheckTooShortSliders.Run | context.InterpretedDifficulty > DifficultyRating.Easy | TRUE
        [Test]
        public void TestHigherDifficultiesAreSkipped()
        {
            Assert.That(run(DifficultyRating.Expert, slider(10)), Is.Empty);
        }

        // CF: CheckTooShortSliders.Run | context.InterpretedDifficulty > DifficultyRating.Easy | FALSE
        // CF: CheckTooShortSliders.Run | foreach hitObject | loop 0
        [Test]
        public void TestNoObjects()
        {
            Assert.That(run(DifficultyRating.Easy), Is.Empty);
        }

        // CF: CheckTooShortSliders.Run | foreach hitObject | loop n
        // CF: CheckTooShortSliders.Run | hitObject is Slider slider | FALSE
        // CF: CheckTooShortSliders.Run | slider.SpanDuration < span_duration_threshold | FALSE
        [Test]
        public void TestCircleAndLongSliderAreOk()
        {
            Assert.That(run(DifficultyRating.Easy, new HitCircle(), slider(100)), Is.Empty);
        }

        // CF: CheckTooShortSliders.Run | hitObject is Slider slider | TRUE
        // CF: CheckTooShortSliders.Run | slider.SpanDuration < span_duration_threshold | TRUE
        [Test]
        public void TestTooShortSliderIgnoringRepeats()
        {
            // repeats extend the total duration, but only the span duration counts
            var issues = run(DifficultyRating.Easy, slider(10, repeats: 2));
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTooShortSliders.IssueTemplateTooShort>());
        }
    }
}
