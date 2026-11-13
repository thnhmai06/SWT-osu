// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Mania.Edit.Checks;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Mania.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckManiaConcurrentObjectsTest
    {
        private readonly CheckManiaConcurrentObjects check = new CheckManiaConcurrentObjects();

        private static HoldNote hold(double start, double end, int column) => new HoldNote { StartTime = start, EndTime = end, Column = column };

        private List<Issue> run(params HitObject[] objects)
        {
            var beatmap = new Beatmap<HitObject> { HitObjects = objects.ToList() };
            return check.Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap))).ToList();
        }

        // CF: CheckManiaConcurrentObjects.Run | for i < Count - 1 | loop 0
        // CF: CheckManiaConcurrentObjects.Run | for j | loop 0
        [Test]
        public void TestSingleObject() => Assert.That(run(hold(0, 100, 1)), Is.Empty);

        // CF: CheckManiaConcurrentObjects.Run | for i < Count - 1 | loop n
        // CF: CheckManiaConcurrentObjects.Run | for j | loop 1
        // CF: CheckManiaConcurrentObjects.Run | column != nextColumn | FALSE
        // CF: CheckManiaConcurrentObjects.Run | !AreConcurrent && !AreAlmostConcurrent | TRUE
        // CF: CheckManiaConcurrentObjects.Run | !AreConcurrent && !AreAlmostConcurrent | break
        [Test]
        public void TestSeparateOnSameColumn() => Assert.That(run(hold(100, 400.75, 1), hold(500, 900.75, 1)), Is.Empty);

        // CF: CheckManiaConcurrentObjects.Run | column != nextColumn | TRUE
        // CF: CheckManiaConcurrentObjects.Run | continue | loop n
        [Test]
        public void TestConcurrentOnDifferentColumnsIgnored() => Assert.That(run(hold(100, 400.75, 1), hold(300, 700.75, 2)), Is.Empty);

        // CF: CheckManiaConcurrentObjects.Run | (hitobject as IHasColumn)?.Column null side | null
        [Test]
        public void TestObjectWithoutColumnIsNotMatchedToColumnObject()
        {
            Assert.That(run(hold(100, 400, 1), new Note { StartTime = 200, Column = 1 }), Has.Count.EqualTo(1));
        }

        // CF: CheckManiaConcurrentObjects.Run | !AreConcurrent && !AreAlmostConcurrent | FALSE
        // CF: CheckManiaConcurrentObjects.Run | AreConcurrent | TRUE
        // CF: CheckManiaConcurrentObjects.Run | AreConcurrent(hitobject, nextHitobject) first operand | TRUE
        [Test]
        public void TestConcurrentOnSameColumn()
        {
            var issues = run(hold(100, 400.75, 1), hold(300, 700.75, 1));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckConcurrentObjects.IssueTemplateConcurrent>());
        }

        // CF: CheckManiaConcurrentObjects.Run | AreConcurrent | FALSE
        // CF: CheckManiaConcurrentObjects.Run | AreAlmostConcurrent | TRUE
        // CF: CheckManiaConcurrentObjects.Run | AreAlmostConcurrent (second operand of break guard) | TRUE
        [Test]
        public void TestAlmostConcurrentOnSameColumn()
        {
            var issues = run(hold(100, 400.75, 1), hold(408, 700.75, 1));

            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckConcurrentObjects.IssueTemplateAlmostConcurrent>());
        }

        // CF: CheckManiaConcurrentObjects.Run | AreAlmostConcurrent (else-if) | infeasible: reaching it requires AreConcurrent || AreAlmostConcurrent (else the guard breaks), so the else-if is always TRUE when AreConcurrent is FALSE
    }
}
