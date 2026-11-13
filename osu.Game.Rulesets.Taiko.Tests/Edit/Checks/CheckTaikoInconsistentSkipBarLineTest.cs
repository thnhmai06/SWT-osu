// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Edit.Checks.Components;
using osu.Game.Rulesets.Taiko.Edit.Checks;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Taiko.Tests.Edit.Checks
{
    [TestFixture]
    public class CheckTaikoInconsistentSkipBarLineTest
    {
        private CheckTaikoInconsistentSkipBarLine check = null!;

        [SetUp]
        public void SetUp() => check = new CheckTaikoInconsistentSkipBarLine();

        private static IBeatmap create(RulesetInfo ruleset, string name, params (double time, bool omit)[] points)
        {
            var beatmap = new Beatmap<HitObject>
            {
                BeatmapInfo = new BeatmapInfo { DifficultyName = name, Metadata = new BeatmapMetadata(), Ruleset = ruleset }
            };

            foreach ((double time, bool omit) in points)
                beatmap.ControlPointInfo.Add(time, new TimingControlPoint { BeatLength = 500, OmitFirstBarLine = omit });

            return beatmap;
        }

        private static IBeatmap taiko(string name, params (double, bool)[] points) => create(new TaikoRuleset().RulesetInfo, name, points);

        private List<Issue> run(IBeatmap current, params IBeatmap[] all)
        {
            var context = new BeatmapVerifierContext(
                new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(current), current),
                all.Select(b => new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(b), b)).ToList(),
                DifficultyRating.ExpertPlus);
            return check.Run(context).ToList();
        }

        // CF: CheckTaikoInconsistentSkipBarLine.Run | context.AllDifficulties.Count() <= 1 | TRUE
        [Test]
        public void TestSingleDifficulty()
        {
            var a = taiko("a", (1000, false));
            Assert.That(run(a, a), Is.Empty);
        }

        // CF: CheckTaikoInconsistentSkipBarLine.Run | context.AllDifficulties.Count() <= 1 | FALSE
        // CF: CheckTaikoInconsistentSkipBarLine.Run | taikoBeatmaps.Count <= 1 | TRUE
        [Test]
        public void TestOnlyOneTaikoDifficulty()
        {
            var a = taiko("a", (1000, false));
            var b = create(new RulesetInfo { ShortName = "osu", OnlineID = 0, Name = "osu!" }, "b", (1000, true));
            Assert.That(run(a, a, b), Is.Empty);
        }

        // CF: CheckTaikoInconsistentSkipBarLine.Run | taikoBeatmaps.Count <= 1 | FALSE
        // CF: CheckTaikoInconsistentSkipBarLine.Run | foreach otherTaikoBeatmaps | loop 1
        // CF: CheckTaikoInconsistentSkipBarLine.Run | foreach referenceTimingPoints | loop n
        // CF: CheckTaikoInconsistentSkipBarLine.Run | matchingPoint == null | FALSE
        // CF: CheckTaikoInconsistentSkipBarLine.Run | referencePoint.OmitFirstBarLine != matchingPoint.OmitFirstBarLine | FALSE
        // CF: CheckTaikoInconsistentSkipBarLine.Run | referencePoint.OmitFirstBarLine != matchingPoint.OmitFirstBarLine | TRUE
        [Test]
        public void TestPartiallyInconsistent()
        {
            var a = taiko("a", (1000, false), (2000, true), (3000, false));
            var b = taiko("b", (1000, false), (2000, false), (3000, false));
            var issues = run(a, a, b);
            Assert.That(issues, Has.Count.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckTaikoInconsistentSkipBarLine.IssueTemplateInconsistentOmitFirstBarLine>());
            Assert.That(issues[0].Time, Is.EqualTo(2000));
        }

        // CF: CheckTaikoInconsistentSkipBarLine.Run | foreach otherTaikoBeatmaps | loop n
        [Test]
        public void TestMultipleOthers()
        {
            var a = taiko("a", (1000, false), (2000, true));
            var b = taiko("b", (1000, true), (2000, true));
            var c = taiko("c", (1000, false), (2000, false));
            var issues = run(a, a, b, c);
            Assert.That(issues.Select(i => i.Time), Is.EqualTo(new double?[] { 1000, 2000 }));
        }

        // CF: CheckTaikoInconsistentSkipBarLine.Run | matchingPoint == null | TRUE
        [Test]
        public void TestMissingMatchingPointIgnored()
        {
            var a = taiko("a", (1000, false), (2000, true));
            var b = taiko("b", (1000, false));
            Assert.That(run(a, a, b), Is.Empty);
        }

        // CF: CheckTaikoInconsistentSkipBarLine.Run | foreach referenceTimingPoints | loop 0
        [Test]
        public void TestReferenceWithoutTimingPoints()
        {
            var a = taiko("a");
            var b = taiko("b", (1000, true));
            Assert.That(run(a, a, b), Is.Empty);
        }

        // CF: CheckTaikoInconsistentSkipBarLine.Run | foreach otherTaikoBeatmaps | loop 0
        [Test]
        public void TestReferenceListedTwiceHasNoOthers()
        {
            var a = taiko("a", (1000, true));
            Assert.That(run(a, a, a), Is.Empty);
        }
    }
}
