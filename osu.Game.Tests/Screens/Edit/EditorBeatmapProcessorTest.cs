// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Screens.Edit
{
    [TestFixture]
    public class EditorBeatmapProcessorTest
    {
        private static (EditorBeatmap beatmap, EditorBeatmapProcessor processor) create(params HitCircle[] circles)
        {
            var playable = new Beatmap<HitObject> { BeatmapInfo = { Ruleset = new OsuRuleset().RulesetInfo } };
            playable.ControlPointInfo.Add(0, new osu.Game.Beatmaps.ControlPoints.TimingControlPoint { BeatLength = 500 });
            playable.HitObjects.AddRange(circles);
            var editorBeatmap = new EditorBeatmap(playable);
            return (editorBeatmap, new EditorBeatmapProcessor(editorBeatmap, new OsuRuleset()));
        }

        private static HitCircle circle(double time, bool newCombo = false) => new HitCircle { StartTime = time, NewCombo = newCombo, TimePreempt = 600 };

        [Test]
        public void TestProcessorExposesBeatmap()
        {
            var (beatmap, processor) = create();
            Assert.That(processor.Beatmap, Is.SameAs(beatmap));
            Assert.That(((IBeatmapProcessor)processor).Beatmap, Is.SameAs(beatmap));
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | objectDuration.SetEquals(objectDurationCache) | TRUE
        [Test]
        public void TestUnchangedObjectsDoNotRegenerateBreaks()
        {
            var (beatmap, processor) = create();
            processor.PreProcess();
            processor.PostProcess();
            Assert.That(beatmap.Breaks, Is.Empty);
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | objectDuration.SetEquals(objectDurationCache) | FALSE
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | loop over HitObjects pairs | loop n
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | nextObject.StartTime - currentMaxEndTime < MIN_GAP_DURATION | FALSE
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | nextObject is IHasTimePreempt | TRUE
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | breakEndTime - breakStartTime < MIN_BREAK_DURATION | FALSE
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | Beatmap.Breaks.Any(b => b.Intersects(breakPeriod)) | FALSE
        [Test]
        public void TestBreakGeneratedForLargeGap()
        {
            var (beatmap, processor) = create(circle(0), circle(10000));
            processor.PostProcess();

            Assert.That(beatmap.Breaks, Has.Count.EqualTo(1));
            Assert.That(beatmap.Breaks[0].StartTime, Is.EqualTo(0 + BreakPeriod.GAP_BEFORE_BREAK).Within(1));
            Assert.That(beatmap.Breaks[0].EndTime, Is.EqualTo(10000 - 600).Within(1));
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | nextObject.StartTime - currentMaxEndTime < MIN_GAP_DURATION | TRUE
        [Test]
        public void TestNoBreakForSmallGap()
        {
            var (beatmap, processor) = create(circle(0), circle(1000));
            processor.PostProcess();
            Assert.That(beatmap.Breaks, Is.Empty);
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | breakEndTime - breakStartTime < MIN_BREAK_DURATION | TRUE
        [Test]
        public void TestNoBreakWhenPreemptMakesItTooShort()
        {
            var (beatmap, processor) = create(circle(0), new HitCircle { StartTime = 1400, TimePreempt = 1200 });
            processor.PostProcess();
            Assert.That(beatmap.Breaks, Is.Empty);
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | Beatmap.Breaks.RemoveAll(b => b is not ManualBreakPeriod) | non-null
        [Test]
        public void TestStaleAutomaticBreaksRemovedOnChange()
        {
            var (beatmap, processor) = create(circle(0), circle(10000));
            processor.PostProcess();
            Assert.That(beatmap.Breaks, Has.Count.EqualTo(1));

            beatmap.HitObjects.ToList().ForEach(h => beatmap.Remove(h));
            beatmap.Add(circle(0));
            beatmap.Add(circle(1000));
            processor.PostProcess();

            Assert.That(beatmap.Breaks, Is.Empty);
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | manualBreak.EndTime <= first object start | FALSE
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | manualBreak.StartTime >= last object time | FALSE
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | any hit object overlaps manual break | FALSE
        // CF: EditorBeatmapProcessor.autoGenerateBreaks | loop over manual breaks | loop 1
        [Test]
        public void TestValidManualBreakKept()
        {
            var (beatmap, processor) = create(circle(0), circle(20000));
            beatmap.Breaks.Add(new ManualBreakPeriod(5000, 8000));
            processor.PostProcess();

            Assert.That(beatmap.Breaks.OfType<ManualBreakPeriod>().Count(), Is.EqualTo(1));
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | manualBreak.EndTime <= first object start | TRUE
        [Test]
        public void TestManualBreakBeforeFirstObjectRemoved()
        {
            var (beatmap, processor) = create(circle(10000), circle(20000));
            beatmap.Breaks.Add(new ManualBreakPeriod(0, 5000));
            processor.PostProcess();

            Assert.That(beatmap.Breaks.OfType<ManualBreakPeriod>(), Is.Empty);
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | manualBreak.StartTime >= last object time | TRUE
        [Test]
        public void TestManualBreakAfterLastObjectRemoved()
        {
            var (beatmap, processor) = create(circle(0), circle(1000));
            beatmap.Breaks.Add(new ManualBreakPeriod(50000, 55000));
            processor.PostProcess();

            Assert.That(beatmap.Breaks.OfType<ManualBreakPeriod>(), Is.Empty);
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | any hit object overlaps manual break | TRUE
        [Test]
        public void TestManualBreakOverlappingObjectRemoved()
        {
            var (beatmap, processor) = create(circle(0), circle(5500), circle(20000));
            beatmap.Breaks.Add(new ManualBreakPeriod(5000, 8000));
            processor.PostProcess();

            Assert.That(beatmap.Breaks.OfType<ManualBreakPeriod>(), Is.Empty);
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | Beatmap.Breaks.Any(b => b.Intersects(breakPeriod)) | TRUE
        [Test]
        public void TestAutomaticBreakSkippedWhenIntersectingManualBreak()
        {
            var (beatmap, processor) = create(circle(0), circle(20000));
            beatmap.Breaks.Add(new ManualBreakPeriod(5000, 8000));
            processor.PostProcess();

            Assert.That(beatmap.Breaks, Has.Count.EqualTo(1));
        }

        // CF: EditorBeatmapProcessor.autoGenerateBreaks | nextObject is IHasTimePreempt | FALSE
        // infeasible via the osu ruleset: every osu! hit object has a time preempt.

        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | breakEnds.Count == 0 | TRUE
        [Test]
        public void TestNoBreaksNoComboChanges()
        {
            var (beatmap, processor) = create(circle(0), circle(1000));
            processor.PostProcess();
            Assert.That(beatmap.HitObjects.Cast<HitCircle>().Select(h => h.NewCombo), Is.All.False);
        }

        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | breakEnds.Count == 0 | FALSE
        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | loop over HitObjects | loop n
        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | currentBreak < breakEnds.Count && hitObject.StartTime >= breakEnds[currentBreak] | TRUE
        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | !hasCombo.NewCombo | TRUE
        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | comboInformationUpdateRequired | TRUE
        [Test]
        public void TestObjectAfterBreakStartsNewCombo()
        {
            var (beatmap, processor) = create(circle(0), circle(10000), circle(10500));
            processor.PostProcess();

            var objects = beatmap.HitObjects.Cast<HitCircle>().ToList();
            Assert.That(objects[1].NewCombo, Is.True);
            Assert.That(objects[2].NewCombo, Is.False);
        }

        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | currentBreak < breakEnds.Count && hitObject.StartTime >= breakEnds[currentBreak] | FALSE
        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | !hasCombo.NewCombo | FALSE
        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | comboInformationUpdateRequired | FALSE
        [Test]
        public void TestObjectAlreadyNewComboLeftAlone()
        {
            var (beatmap, processor) = create(circle(0), circle(10000, true));
            processor.PostProcess();
            Assert.That(beatmap.HitObjects.Cast<HitCircle>().Last().NewCombo, Is.True);
        }

        // CF: EditorBeatmapProcessor.ensureNewComboAfterBreaks | hitObject is not IHasComboInformation | TRUE
        // infeasible via the osu ruleset: all osu! hit objects carry combo information.
    }
}
