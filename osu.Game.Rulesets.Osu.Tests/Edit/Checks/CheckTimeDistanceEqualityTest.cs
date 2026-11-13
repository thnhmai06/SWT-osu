// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckTimeDistanceEqualityTest
    {
        private static HitCircle circle(double time, float x) => new HitCircle { StartTime = time, Position = new Vector2(x, 0) };

        private static List<Issue> run(DifficultyRating rating, params HitObject[] objects)
        {
            var beatmap = new Beatmap<HitObject> { HitObjects = objects.ToList() };
            return new CheckTimeDistanceEquality().Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap), rating)).ToList();
        }

        private static List<Issue> run(params HitObject[] objects) => run(DifficultyRating.Easy, objects);

        // CF: CheckTimeDistanceEquality.Run | context.InterpretedDifficulty > DifficultyRating.Normal | TRUE
        [Test]
        public void TestHardDifficultiesAreSkipped()
        {
            Assert.That(run(DifficultyRating.Hard, circle(0, 0), circle(200, 100), circle(400, 400)), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.Run | context.InterpretedDifficulty > DifficultyRating.Normal | FALSE
        // CF: CheckTimeDistanceEquality.Run | for (i < hitObjects.Count - 1) | loop 0
        [Test]
        public void TestNoPairs()
        {
            Assert.That(run(circle(0, 0)), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.Run | !(hitObjects[i] is OsuHitObject hitObject) | TRUE
        // CF: CheckTimeDistanceEquality.Run | !(hitObjects[i + 1] is OsuHitObject nextHitObject) | TRUE
        [Test]
        public void TestNonOsuObjectsAreSkipped()
        {
            Assert.That(run(new NonOsuObject(), circle(100, 100), new NonOsuObject()), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.Run | hitObject is Spinner | TRUE
        // CF: CheckTimeDistanceEquality.Run | nextHitObject is Spinner | TRUE
        [Test]
        public void TestSpinnersAreSkipped()
        {
            Assert.That(run(new Spinner { Duration = 10 }, circle(100, 100), new Spinner { StartTime = 200, Duration = 10 }), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.Run | deltaTime > pattern_lifetime | TRUE
        [Test]
        public void TestObjectsFarApartInTimeAreSkipped()
        {
            Assert.That(run(circle(0, 0), circle(700, 500)), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.Run | distance < stack_leniency | TRUE
        [Test]
        public void TestStackedObjectsAreSkipped()
        {
            Assert.That(run(circle(0, 0), circle(100, 5)), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.Run | for (i < hitObjects.Count - 1) | loop n
        // CF: CheckTimeDistanceEquality.Run | deltaTime > pattern_lifetime | FALSE
        // CF: CheckTimeDistanceEquality.Run | distance < stack_leniency | FALSE
        // CF: CheckTimeDistanceEquality.Run | expectedDistance == 0 | TRUE
        // CF: CheckTimeDistanceEquality.Run | expectedDistance == 0 | FALSE
        // CF: CheckTimeDistanceEquality.Run | (Math.Abs(expectedDistance - distance) - distance_leniency_absolute_problem) / distance > distance_leniency_percent_problem | FALSE
        // CF: CheckTimeDistanceEquality.Run | (Math.Abs(expectedDistance - distance) - distance_leniency_absolute_warning) / distance > distance_leniency_percent_warning | FALSE
        // CF: CheckTimeDistanceEquality.getExpectedDistance | for (i = observations - 1; i >= 0; --i) | loop n
        [Test]
        public void TestConsistentSpacingHasNoIssues()
        {
            Assert.That(run(circle(0, 0), circle(200, 100), circle(400, 200), circle(600, 300)), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.Run | (Math.Abs(expectedDistance - distance) - distance_leniency_absolute_problem) / distance > distance_leniency_percent_problem | TRUE
        [Test]
        public void TestSpacingFarFromExpectedIsProblem()
        {
            var issues = run(circle(0, 0), circle(200, 100), circle(400, 200), circle(600, 500));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTimeDistanceEquality.IssueTemplateIrregularSpacingProblem>());
        }

        // CF: CheckTimeDistanceEquality.Run | (Math.Abs(expectedDistance - distance) - distance_leniency_absolute_warning) / distance > distance_leniency_percent_warning | TRUE
        [Test]
        public void TestSpacingSlightlyOffIsWarning()
        {
            var issues = run(circle(0, 0), circle(200, 100), circle(400, 200), circle(600, 340));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTimeDistanceEquality.IssueTemplateIrregularSpacingWarning>());
        }

        // CF: CheckTimeDistanceEquality.getExpectedDistance | Math.Abs(observedTimeDistance.DeltaTime - prevObservedTimeDistance.DeltaTime) > similar_time_leniency | TRUE
        [Test]
        public void TestDifferentTimeGapsAreNotCompared()
        {
            // second gap is half as long as the first, so it has nothing relevant to be compared with
            Assert.That(run(circle(0, 0), circle(200, 100), circle(300, 500)), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.getExpectedDistance | Math.Abs(observedTimeDistance.DeltaTime - prevObservedTimeDistance.DeltaTime) > similar_time_leniency | FALSE
        // CF: CheckTimeDistanceEquality.getExpectedDistance | observedTimeDistance.ObservationTime - prevObservedTimeDistance.ObservationTime > observation_lifetime | TRUE
        // CF: CheckTimeDistanceEquality.getExpectedDistance | observedTimeDistance.ObservationTime - prevObservedTimeDistance.ObservationTime > observation_lifetime | FALSE
        [Test]
        public void TestOldObservationsAreForgotten()
        {
            // a long steady run: observations older than 4 seconds are no longer consulted, and nothing is flagged
            var objects = Enumerable.Range(0, 25).Select(i => (HitObject)circle(i * 200, i * 100)).ToArray();

            Assert.That(run(objects), Is.Empty);
        }

        // CF: CheckTimeDistanceEquality.getExpectedDistance | observedTimeDistance.ObservationTime - prevObservedTimeDistance.ObservationTime > observation_lifetime | TRUE
        [Test]
        public void TestExpectationOnlyUsesRecentObservations()
        {
            // 5s of regular 100px spacing, then spacing doubles gradually via a pause in observations is not possible, so check
            // that the very recent observation alone defines the expectation after the old ones expire.
            var objects = Enumerable.Range(0, 25).Select(i => (HitObject)circle(i * 200, i * 100)).ToList();
            objects.Add(circle(24 * 200 + 200, 24 * 100 + 300));

            var issues = run(objects.ToArray());

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTimeDistanceEquality.IssueTemplateIrregularSpacingProblem>());
        }

        private class NonOsuObject : HitObject
        {
        }
    }
}
