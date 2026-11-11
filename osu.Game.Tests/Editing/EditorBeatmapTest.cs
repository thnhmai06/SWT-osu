// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Models;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;
using osu.Game.Storyboards;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Control-flow tests for <see cref="EditorBeatmap"/>.
    /// Each test drives one path (branch outcome / condition value) of one member; the purpose is stated in the test name or a one-line comment.
    /// </summary>
    [TestFixture]
    public class EditorBeatmapTest
    {
        private const double beat_length = 500;

        #region Constructor

        [Test]
        public void TestConstructor_ExplicitBeatmapInfo_UsedInsteadOfPlayableBeatmapInfo()
        {
            // `beatmapInfo ?? playableBeatmap.BeatmapInfo`: left operand non-null.
            var playable = createBeatmap();
            var explicitInfo = new BeatmapInfo(new OsuRuleset().RulesetInfo);

            var editor = new EditorBeatmap(playable, beatmapInfo: explicitInfo);

            Assert.That(editor.BeatmapInfo, Is.SameAs(explicitInfo));
            Assert.That(editor.BeatmapInfo, Is.Not.SameAs(playable.BeatmapInfo));
            Assert.That(editor.Metadata, Is.SameAs(explicitInfo.Metadata));
            Assert.That(editor.Storyboard.BeatmapInfo, Is.SameAs(explicitInfo));
        }

        [Test]
        public void TestConstructor_NullBeatmapInfo_FallsBackToPlayableBeatmapInfo()
        {
            // `beatmapInfo ?? playableBeatmap.BeatmapInfo`: left operand null.
            var playable = createBeatmap();

            var editor = new EditorBeatmap(playable);

            Assert.That(editor.BeatmapInfo, Is.SameAs(playable.BeatmapInfo));
            Assert.That(editor.Storyboard.BeatmapInfo, Is.SameAs(playable.BeatmapInfo));
        }

        [Test]
        public void TestConstructor_LegacyBeatmapSkin_WrappedInEditorBeatmapSkin()
        {
            // `beatmapSkin is LegacyBeatmapSkin`: true.
            var editor = new EditorBeatmap(createBeatmap(), createLegacySkin());

            Assert.That(editor.BeatmapSkin, Is.Not.Null);
        }

        [Test]
        public void TestConstructor_NullSkin_NoEditorBeatmapSkin()
        {
            // `beatmapSkin is LegacyBeatmapSkin`: false (null is never a LegacyBeatmapSkin).
            var editor = new EditorBeatmap(createBeatmap());

            Assert.That(editor.BeatmapSkin, Is.Null);
        }

        [Test]
        public void TestConstructor_ProvidedStoryboard_UsedAndLinkedToEditor()
        {
            // `storyboard ?? new Storyboard()`: left operand non-null.
            var storyboard = new Storyboard();

            var editor = new EditorBeatmap(createBeatmap(), storyboard: storyboard);

            Assert.That(editor.Storyboard, Is.SameAs(storyboard));
            Assert.That(storyboard.Beatmap, Is.SameAs(editor));
        }

        [Test]
        public void TestConstructor_NullStoryboard_CreatesNewStoryboardLinkedToEditor()
        {
            // `storyboard ?? new Storyboard()`: left operand null.
            var editor = new EditorBeatmap(createBeatmap());

            Assert.That(editor.Storyboard, Is.Not.Null);
            Assert.That(editor.Storyboard.Beatmap, Is.SameAs(editor));
        }

        [Test]
        public void TestConstructor_ExistingHitObjects_StartTimeIsTracked()
        {
            // constructor `foreach` over HitObjects: >= 1 iteration, so pre-existing objects get their start time tracked.
            var first = new HitCircle { StartTime = 100 };
            var second = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(first, second));

            first.StartTime = 300;

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { second, first }));
            Assert.That(editor.UpdateInProgress.Value, Is.True);
        }

        [Test]
        public void TestConstructor_NoHitObjects_LoopSkipped()
        {
            // constructor `foreach` over HitObjects: 0 iterations.
            var editor = new EditorBeatmap(createBeatmap());

            Assert.That(editor.HitObjects, Is.Empty);
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        [Test]
        public void TestConstructor_BeatmapVersion_CopiedFromPlayableBeatmap()
        {
            var playable = createBeatmap();
            playable.BeatmapVersion = 14;

            var editor = new EditorBeatmap(playable);

            Assert.That(editor.BeatmapVersion, Is.EqualTo(14));
        }

        [Test]
        public void TestBreaksChanged_MirroredToPlayableBeatmap()
        {
            var playable = createBeatmap();
            var editor = new EditorBeatmap(playable);
            var manualBreak = new ManualBreakPeriod(1000, 2000);

            editor.Breaks.Add(manualBreak);

            Assert.That(playable.Breaks, Is.EqualTo(new[] { manualBreak }));
        }

        [Test]
        public void TestBookmarksChanged_StoredSortedAndDistinct()
        {
            var playable = createBeatmap();
            var editor = new EditorBeatmap(playable);

            editor.Bookmarks.AddRange(new[] { 30, 10, 30, 20 });

            Assert.That(playable.Bookmarks, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void TestSliderVelocityPresetsChanged_StoredSortedAndDistinct()
        {
            var playable = createBeatmap();
            playable.SliderVelocityPresets = new[] { 1.0 };
            var editor = new EditorBeatmap(playable);

            editor.SliderVelocityPresets.AddRange(new[] { 2.0, 0.5, 2.0 });

            Assert.That(playable.SliderVelocityPresets, Is.EqualTo(new[] { 0.5, 1.0, 2.0 }));
        }

        [Test]
        public void TestPreviewTimeChanged_WrittenToMetadata()
        {
            var editor = new EditorBeatmap(createBeatmap());

            editor.PreviewTime.Value = 1234;

            Assert.That(editor.BeatmapInfo.Metadata.PreviewTime, Is.EqualTo(1234));
        }

        #endregion

        #region ConvertControlPoints

        [Test]
        public void TestConvertControlPoints_NonLegacy_ReturnedUnchanged()
        {
            // `!(incoming is LegacyControlPointInfo)` true -> early return of the same instance.
            var incoming = new ControlPointInfo();

            Assert.That(EditorBeatmap.ConvertControlPoints(incoming), Is.SameAs(incoming));
        }

        [Test]
        public void TestConvertControlPoints_LegacyEmpty_ReturnsNewEmptyNonLegacyInfo()
        {
            // guard false + `foreach` over AllControlPoints with 0 iterations.
            var incoming = new LegacyControlPointInfo();

            var result = EditorBeatmap.ConvertControlPoints(incoming);

            Assert.That(result, Is.Not.SameAs(incoming));
            Assert.That(result, Is.Not.InstanceOf<LegacyControlPointInfo>());
            Assert.That(result.AllControlPoints, Is.Empty);
        }

        [Test]
        public void TestConvertControlPoints_LegacyDifficultyPoint_Skipped()
        {
            // switch case `DifficultyControlPoint` -> continue.
            var incoming = new LegacyControlPointInfo();
            var difficulty = new DifficultyControlPoint();
            difficulty.SliderVelocityBindable.Value = 2;
            incoming.Add(0, difficulty);

            var result = EditorBeatmap.ConvertControlPoints(incoming);

            Assert.That(result.AllControlPoints, Is.Empty);
        }

        [Test]
        public void TestConvertControlPoints_LegacySamplePoint_Skipped()
        {
            // switch case `SampleControlPoint` -> continue.
            var incoming = new LegacyControlPointInfo();
            var sample = new SampleControlPoint();
            sample.SampleBankBindable.Value = "drum";
            incoming.Add(0, sample);

            var result = EditorBeatmap.ConvertControlPoints(incoming);

            Assert.That(result.AllControlPoints, Is.Empty);
        }

        [Test]
        public void TestConvertControlPoints_LegacyTimingAndEffectPoints_Kept()
        {
            // switch `default` path: other point types are copied, legacy-only ones dropped from the same input.
            var incoming = new LegacyControlPointInfo();
            var timing = new TimingControlPoint { BeatLength = beat_length };
            var effect = new EffectControlPoint();
            effect.KiaiModeBindable.Value = true;
            var difficulty = new DifficultyControlPoint();
            difficulty.SliderVelocityBindable.Value = 2;
            incoming.Add(0, timing);
            incoming.Add(1000, effect);
            incoming.Add(500, difficulty);

            var result = EditorBeatmap.ConvertControlPoints(incoming);

            Assert.That(result, Is.Not.InstanceOf<LegacyControlPointInfo>());
            Assert.That(result.AllControlPoints, Is.EquivalentTo(new ControlPoint[] { timing, effect }));
        }

        [Test]
        public void TestConstructor_LegacyControlPoints_ReplacedByNonLegacy()
        {
            var playable = createBeatmap();
            playable.ControlPointInfo = new LegacyControlPointInfo();

            var editor = new EditorBeatmap(playable);

            Assert.That(editor.ControlPointInfo, Is.Not.InstanceOf<LegacyControlPointInfo>());
            Assert.That(playable.ControlPointInfo, Is.SameAs(editor.ControlPointInfo));
        }

        #endregion

        #region Property forwarding

        [Test]
        public void TestBeatmapInfoSetter_Throws()
        {
            var editor = new EditorBeatmap(createBeatmap());

            Assert.Throws<InvalidOperationException>(() => editor.BeatmapInfo = new BeatmapInfo());
        }

        [Test]
        public void TestForwardedProperties_WriteThroughToPlayableBeatmap()
        {
            var playable = createBeatmap();
            var editor = new EditorBeatmap(playable);
            var difficulty = new BeatmapDifficulty { CircleSize = 7 };
            var controlPoints = new ControlPointInfo();

            editor.Difficulty = difficulty;
            editor.ControlPointInfo = controlPoints;
            editor.AudioLeadIn = 111;
            editor.StackLeniency = 0.25f;
            editor.SpecialStyle = true;
            editor.LetterboxInBreaks = true;
            editor.WidescreenStoryboard = true;
            editor.EpilepsyWarning = true;
            editor.SamplesMatchPlaybackRate = true;
            editor.DistanceSpacing = 2.5;
            editor.GridSize = 32;
            editor.TimelineZoom = 3;
            editor.Countdown = CountdownType.DoubleSpeed;
            editor.CountdownOffset = 4;
            ((IBeatmap)editor).Bookmarks = new[] { 5 };
            ((IBeatmap)editor).SliderVelocityPresets = new[] { 0.9 };

            Assert.That(playable.Difficulty, Is.SameAs(difficulty));
            Assert.That(playable.ControlPointInfo, Is.SameAs(controlPoints));
            Assert.That(playable.AudioLeadIn, Is.EqualTo(111));
            Assert.That(playable.StackLeniency, Is.EqualTo(0.25f));
            Assert.That(playable.SpecialStyle, Is.True);
            Assert.That(playable.LetterboxInBreaks, Is.True);
            Assert.That(playable.WidescreenStoryboard, Is.True);
            Assert.That(playable.EpilepsyWarning, Is.True);
            Assert.That(playable.SamplesMatchPlaybackRate, Is.True);
            Assert.That(playable.DistanceSpacing, Is.EqualTo(2.5));
            Assert.That(playable.GridSize, Is.EqualTo(32));
            Assert.That(playable.TimelineZoom, Is.EqualTo(3));
            Assert.That(playable.Countdown, Is.EqualTo(CountdownType.DoubleSpeed));
            Assert.That(playable.CountdownOffset, Is.EqualTo(4));
            Assert.That(playable.Bookmarks, Is.EqualTo(new[] { 5 }));
            Assert.That(playable.SliderVelocityPresets, Is.EqualTo(new[] { 0.9 }));
        }

        [Test]
        public void TestForwardedProperties_ReadThroughFromPlayableBeatmap()
        {
            var playable = createBeatmap();
            playable.AudioLeadIn = 222;
            playable.Difficulty = new BeatmapDifficulty { OverallDifficulty = 3 };
            var editor = new EditorBeatmap(playable);

            Assert.That(editor.AudioLeadIn, Is.EqualTo(222));
            Assert.That(editor.Difficulty.OverallDifficulty, Is.EqualTo(3));
            Assert.That(editor.HitObjects, Is.SameAs(playable.HitObjects));
            Assert.That(((IBeatmap)editor).Breaks, Is.SameAs(playable.Breaks));
            Assert.That(editor.TotalBreakTime, Is.EqualTo(playable.TotalBreakTime));
        }

        [Test]
        public void TestClone_ReturnsDistinctEditorBeatmapSharingPlayableBeatmap()
        {
            var editor = new EditorBeatmap(createBeatmap());

            var clone = editor.Clone();

            Assert.That(clone, Is.InstanceOf<EditorBeatmap>());
            Assert.That(clone, Is.Not.SameAs(editor));
            Assert.That(((EditorBeatmap)clone).PlayableBeatmap, Is.SameAs(editor.PlayableBeatmap));
        }

        #endregion

        #region PerformOnSelection

        [Test]
        public void TestPerformOnSelection_EmptySelection_ReturnsWithoutTransactionOrAction()
        {
            // guard `SelectedHitObjects.Count == 0`: true.
            var editor = new EditorBeatmap(createBeatmap(new HitCircle { StartTime = 100 }));
            int transactions = 0;
            int actions = 0;
            editor.TransactionBegan += () => transactions++;

            editor.PerformOnSelection(_ => actions++);

            Assert.That(actions, Is.Zero);
            Assert.That(transactions, Is.Zero);
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        [Test]
        public void TestPerformOnSelection_SingleSelectedObject_ActionAndUpdateOnce()
        {
            // guard false + `foreach` with exactly 1 iteration.
            var selected = new HitCircle { StartTime = 100 };
            var other = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(selected, other));
            var log = subscribe(editor);
            editor.SelectedHitObjects.Add(selected);
            var visited = new List<HitObject>();

            editor.PerformOnSelection(visited.Add);

            Assert.That(visited, Is.EqualTo(new HitObject[] { selected }));
            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { selected }));
            Assert.That(log.TransactionsBegan, Is.EqualTo(1));
            Assert.That(log.TransactionsEnded, Is.EqualTo(1));
        }

        [Test]
        public void TestPerformOnSelection_MultipleSelectedObjects_EachProcessedInOneTransaction()
        {
            // `foreach` repeats (>= 2 iterations).
            var a = new HitCircle { StartTime = 100 };
            var b = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(a, b));
            var log = subscribe(editor);
            editor.SelectedHitObjects.AddRange(new HitObject[] { a, b });
            var visited = new List<HitObject>();

            editor.PerformOnSelection(visited.Add);

            Assert.That(visited, Is.EquivalentTo(new HitObject[] { a, b }));
            Assert.That(log.Updated, Is.EquivalentTo(new HitObject[] { a, b }));
            Assert.That(log.TransactionsBegan, Is.EqualTo(1));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
        }

        #endregion

        #region Add / AddRange / Insert (findInsertionIndex)

        [Test]
        public void TestAdd_EmptyBeatmap_LoopSkippedAndObjectInserted()
        {
            // findInsertionIndex: `for` runs 0 times -> returns Count - 1 (= -1) -> insert at 0.
            var editor = new EditorBeatmap(createBeatmap());
            var added = new HitCircle { StartTime = 100 };

            editor.Add(added);

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { added }));
        }

        [Test]
        public void TestAdd_EarlierThanAllExisting_InsertedAtFront()
        {
            // `list[i].StartTime > startTime` TRUE at i = 0 -> returns -1.
            var existing = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(existing));
            var added = new HitCircle { StartTime = 50 };

            editor.Add(added);

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { added, existing }));
        }

        [Test]
        public void TestAdd_BetweenExistingObjects_InsertedInTheMiddle()
        {
            // `list[i].StartTime > startTime` FALSE at i = 0, TRUE at i = 1 -> returns 0.
            var first = new HitCircle { StartTime = 100 };
            var last = new HitCircle { StartTime = 300 };
            var editor = new EditorBeatmap(createBeatmap(first, last));
            var added = new HitCircle { StartTime = 200 };

            editor.Add(added);

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { first, added, last }));
        }

        [Test]
        public void TestAdd_LaterThanAllExisting_LoopCompletesAndAppended()
        {
            // `for` loop terminates by its own condition (condition never TRUE) -> returns Count - 1.
            var existing = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(existing));
            var added = new HitCircle { StartTime = 200 };

            editor.Add(added);

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { existing, added }));
        }

        [Test]
        public void TestAdd_SameStartTimeAsExisting_InsertedAfterExisting()
        {
            // equal start time: `>` is FALSE, so order of equal-time objects is preserved (new one goes after).
            var existing = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(existing));
            var added = new HitCircle { StartTime = 100 };

            editor.Add(added);

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { existing, added }));
        }

        [Test]
        public void TestAdd_RaisesHitObjectAddedAfterProcessing()
        {
            var editor = new EditorBeatmap(createBeatmap());
            var log = subscribe(editor);
            var added = new HitCircle { StartTime = 100 };

            editor.Add(added);

            Assert.That(log.Added, Is.EqualTo(new HitObject[] { added }));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        [Test]
        public void TestAddRange_EmptyCollection_NoObjectsAndNoAddedEvent()
        {
            // `foreach` with 0 iterations.
            var editor = new EditorBeatmap(createBeatmap());
            var log = subscribe(editor);

            editor.AddRange(Array.Empty<HitObject>());

            Assert.That(editor.HitObjects, Is.Empty);
            Assert.That(log.Added, Is.Empty);
            Assert.That(log.Reprocessed, Is.Zero);
        }

        [Test]
        public void TestAddRange_MultipleObjects_SortedAndProcessedInSingleTransaction()
        {
            // `foreach` with several iterations; nested Begin/EndChange of each Insert must not end the outer transaction.
            var editor = new EditorBeatmap(createBeatmap());
            var log = subscribe(editor);
            var late = new HitCircle { StartTime = 300 };
            var early = new HitCircle { StartTime = 100 };
            var mid = new HitCircle { StartTime = 200 };

            editor.AddRange(new HitObject[] { late, early, mid });

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { early, mid, late }));
            Assert.That(log.Added, Is.EqualTo(new HitObject[] { late, early, mid }));
            Assert.That(log.TransactionsBegan, Is.EqualTo(1));
            Assert.That(log.TransactionsEnded, Is.EqualTo(1));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
        }

        [Test]
        public void TestInsert_ExplicitIndex_ObjectPlacedAtGivenIndexWithoutResorting()
        {
            var first = new HitCircle { StartTime = 100 };
            var second = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(first, second));
            var log = subscribe(editor);
            var inserted = new HitCircle { StartTime = 900 };

            editor.Insert(1, inserted);

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { first, inserted, second }));
            Assert.That(log.Added, Is.EqualTo(new HitObject[] { inserted }));
        }

        #endregion

        #region Update / UpdateAllHitObjects

        [Test]
        public void TestUpdate_MarksInProgressUntilStateIsSaved()
        {
            var target = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(target));
            var log = subscribe(editor);

            editor.Update(target);

            Assert.That(editor.UpdateInProgress.Value, Is.True);
            Assert.That(log.Updated, Is.Empty);

            editor.SaveState();

            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { target }));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        [Test]
        public void TestUpdate_SameObjectTwice_DebouncedToSingleEvent()
        {
            var target = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(target));
            var log = subscribe(editor);

            editor.Update(target);
            editor.Update(target);
            editor.SaveState();

            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { target }));
        }

        [Test]
        public void TestUpdateAllHitObjects_EmptyBeatmap_FlagsInProgressButQueuesNothing()
        {
            // `foreach` with 0 iterations.
            var editor = new EditorBeatmap(createBeatmap());
            var log = subscribe(editor);

            editor.UpdateAllHitObjects();
            editor.SaveState();

            Assert.That(editor.UpdateInProgress.Value, Is.True);
            Assert.That(log.Updated, Is.Empty);
            Assert.That(log.Reprocessed, Is.Zero);
        }

        [Test]
        public void TestUpdateAllHitObjects_MultipleObjects_EveryObjectUpdated()
        {
            // `foreach` with several iterations.
            var a = new HitCircle { StartTime = 100 };
            var b = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(a, b));
            var log = subscribe(editor);

            editor.UpdateAllHitObjects();
            editor.SaveState();

            Assert.That(log.Updated, Is.EquivalentTo(new HitObject[] { a, b }));
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        #endregion

        #region Remove / RemoveAt / RemoveRange / Clear

        [Test]
        public void TestRemove_ObjectNotInBeatmap_ReturnsFalseWithoutEvent()
        {
            // `index == -1`: true.
            var existing = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(existing));
            var log = subscribe(editor);

            bool removed = editor.Remove(new HitCircle { StartTime = 100 });

            Assert.That(removed, Is.False);
            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { existing }));
            Assert.That(log.Removed, Is.Empty);
        }

        [Test]
        public void TestRemove_ObjectInBeatmap_ReturnsTrueAndRaisesRemoved()
        {
            // `index == -1`: false.
            var target = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(target));
            var log = subscribe(editor);

            bool removed = editor.Remove(target);

            Assert.That(removed, Is.True);
            Assert.That(editor.HitObjects, Is.Empty);
            Assert.That(log.Removed, Is.EqualTo(new HitObject[] { target }));
        }

        [Test]
        public void TestRemoveAt_StopsTrackingStartTimeOfRemovedObject()
        {
            // the start-time bindable must be unbound: a later StartTime change must not re-insert the object.
            var target = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(target));

            editor.RemoveAt(0);
            target.StartTime = 500;

            Assert.That(editor.HitObjects, Is.Empty);
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        [Test]
        public void TestFindIndex_ReturnsIndexOfObjectOrMinusOne()
        {
            var first = new HitCircle { StartTime = 100 };
            var second = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(first, second));

            Assert.That(editor.FindIndex(second), Is.EqualTo(1));
            Assert.That(editor.FindIndex(new HitCircle()), Is.EqualTo(-1));
        }

        [Test]
        public void TestRemoveRange_MultipleObjects_RemovedInSingleTransaction()
        {
            // `foreach` with several iterations, one of them a not-present object (Remove -> false branch inside the loop).
            var a = new HitCircle { StartTime = 100 };
            var b = new HitCircle { StartTime = 200 };
            var keep = new HitCircle { StartTime = 300 };
            var editor = new EditorBeatmap(createBeatmap(a, b, keep));
            var log = subscribe(editor);

            editor.RemoveRange(new HitObject[] { a, new HitCircle { StartTime = 999 }, b });

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { keep }));
            Assert.That(log.Removed, Is.EquivalentTo(new HitObject[] { a, b }));
            Assert.That(log.TransactionsBegan, Is.EqualTo(1));
        }

        [Test]
        public void TestRemoveRange_EmptyCollection_NothingRemoved()
        {
            // `foreach` with 0 iterations.
            var existing = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(existing));
            var log = subscribe(editor);

            editor.RemoveRange(Array.Empty<HitObject>());

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { existing }));
            Assert.That(log.Removed, Is.Empty);
        }

        [Test]
        public void TestClear_NonEmptyBeatmap_RemovesAllObjects()
        {
            var a = new HitCircle { StartTime = 100 };
            var b = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(a, b));
            var log = subscribe(editor);

            editor.Clear();

            Assert.That(editor.HitObjects, Is.Empty);
            Assert.That(log.Removed, Is.EquivalentTo(new HitObject[] { a, b }));
        }

        [Test]
        public void TestClear_EmptyBeatmap_NoOp()
        {
            var editor = new EditorBeatmap(createBeatmap());
            var log = subscribe(editor);

            editor.Clear();

            Assert.That(editor.HitObjects, Is.Empty);
            Assert.That(log.Removed, Is.Empty);
        }

        #endregion

        #region UpdateState (guard `updates == 0 && deletes == 0 && inserts == 0`)

        [Test]
        public void TestUpdateState_NothingPending_ReturnsEarlyWithoutProcessing()
        {
            // a (no updates) = TRUE, b (no deletes) = TRUE, c (no inserts) = TRUE -> whole guard TRUE -> early return.
            var editor = new EditorBeatmap(createBeatmap());
            var log = subscribe(editor);
            editor.BeatmapVersion = 1;

            editor.SaveState();

            Assert.That(editor.BeatmapVersion, Is.EqualTo(1));
            Assert.That(log.Reprocessed, Is.Zero);
        }

        [Test]
        public void TestUpdateState_OnlyUpdatePending_ProcessesBecauseFirstConditionFalse()
        {
            // a (no updates) = FALSE -> guard FALSE (b and c are short-circuited).
            var target = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(target));
            var log = subscribe(editor);
            editor.BeatmapVersion = 1;
            editor.Update(target);

            editor.SaveState();

            Assert.That(editor.BeatmapVersion, Is.EqualTo(LegacyBeatmapEncoder.FIRST_LAZER_VERSION));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { target }));
        }

        [Test]
        public void TestUpdateState_OnlyDeletePending_ProcessesBecauseSecondConditionFalse()
        {
            // a = TRUE, b (no deletes) = FALSE -> guard FALSE (c is short-circuited).
            var target = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(target));
            var log = subscribe(editor);
            editor.BeatmapVersion = 1;

            editor.Remove(target);

            Assert.That(editor.BeatmapVersion, Is.EqualTo(LegacyBeatmapEncoder.FIRST_LAZER_VERSION));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
            Assert.That(log.Removed, Is.EqualTo(new HitObject[] { target }));
        }

        [Test]
        public void TestUpdateState_OnlyInsertPending_ProcessesBecauseThirdConditionFalse()
        {
            // a = TRUE, b = TRUE, c (no inserts) = FALSE -> guard FALSE.
            var editor = new EditorBeatmap(createBeatmap());
            var log = subscribe(editor);
            editor.BeatmapVersion = 1;

            editor.Add(new HitCircle { StartTime = 100 });

            Assert.That(editor.BeatmapVersion, Is.EqualTo(LegacyBeatmapEncoder.FIRST_LAZER_VERSION));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
            Assert.That(log.Added, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestUpdateState_RemovedObjectThatWasSelected_IsDeselected()
        {
            // `foreach (h in deletes) SelectedHitObjects.Remove(h)`: >= 1 iteration with a selected object.
            var selected = new HitCircle { StartTime = 100 };
            var other = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(selected, other));
            editor.SelectedHitObjects.AddRange(new HitObject[] { selected, other });

            editor.Remove(selected);

            Assert.That(editor.SelectedHitObjects, Is.EqualTo(new HitObject[] { other }));
        }

        [Test]
        public void TestUpdateState_AllEventsWithoutSubscribers_DoesNotThrow()
        {
            // null-conditional invocation `?.Invoke` takes the "no subscriber" path for every event.
            var target = new HitCircle { StartTime = 100 };
            var editor = new EditorBeatmap(createBeatmap(target));

            Assert.DoesNotThrow(() =>
            {
                editor.Add(new HitCircle { StartTime = 200 });
                editor.Update(target);
                editor.SaveState();
                editor.Remove(target);
            });
        }

        [Test]
        public void TestUpdateState_ObjectsAddedAndRemovedInOneTransaction_AllEventKindsRaisedAfterProcessing()
        {
            // every pending list non-empty at once: deletes, inserts and updates are all flushed and raised in one pass.
            var removedObject = new HitCircle { StartTime = 100 };
            var updatedObject = new HitCircle { StartTime = 200 };
            var editor = new EditorBeatmap(createBeatmap(removedObject, updatedObject));
            var log = subscribe(editor);
            var addedObject = new HitCircle { StartTime = 300 };

            editor.BeginChange();
            editor.Remove(removedObject);
            editor.Add(addedObject);
            editor.Update(updatedObject);
            editor.EndChange();

            Assert.That(log.Removed, Is.EqualTo(new HitObject[] { removedObject }));
            Assert.That(log.Added, Is.EqualTo(new HitObject[] { addedObject }));
            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { updatedObject }));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        #endregion

        #region trackStartTime

        [Test]
        public void TestStartTimeChanged_AddedObject_ReSortedAndQueuedForUpdate()
        {
            var first = new HitCircle { StartTime = 100 };
            var second = new HitCircle { StartTime = 300 };
            var editor = new EditorBeatmap(createBeatmap());
            editor.AddRange(new HitObject[] { first, second });
            var log = subscribe(editor);

            first.StartTime = 500;

            Assert.That(editor.HitObjects, Is.EqualTo(new HitObject[] { second, first }));
            Assert.That(editor.UpdateInProgress.Value, Is.True);

            editor.SaveState();

            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { first }));
        }

        #endregion

        #region Update() (frame update)

        [Test]
        public void TestFrameUpdate_PendingUpdate_StateIsApplied()
        {
            // `batchPendingUpdates.Count > 0`: true.
            var target = new HitCircle { StartTime = 100 };
            var editor = createInjectedEditor(createBeatmap(target), 1);
            var log = subscribe(editor);
            editor.Update(target);

            editor.RunFrameUpdate();

            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { target }));
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        [Test]
        public void TestFrameUpdate_NothingPending_StateNotApplied()
        {
            // `batchPendingUpdates.Count > 0`: false.
            var editor = createInjectedEditor(createBeatmap(new HitCircle { StartTime = 100 }), 1);
            var log = subscribe(editor);

            editor.RunFrameUpdate();

            Assert.That(log.Reprocessed, Is.Zero);
            Assert.That(log.Updated, Is.Empty);
        }

        [Test]
        public void TestFrameUpdate_NoTimingPoints_HasTimingIsFalse()
        {
            // `ReferenceEquals(TimingPointAt(..), TimingControlPoint.DEFAULT)`: true -> HasTiming = false.
            var editor = createInjectedEditor(createBeatmap(withTiming: false), 1);

            editor.RunFrameUpdate();

            Assert.That(editor.HasTiming.Value, Is.False);
        }

        [Test]
        public void TestFrameUpdate_TimingPointPresent_HasTimingIsTrue()
        {
            // `ReferenceEquals(TimingPointAt(..), TimingControlPoint.DEFAULT)`: false -> HasTiming = true.
            var editor = createInjectedEditor(createBeatmap(), 1);

            editor.RunFrameUpdate();

            Assert.That(editor.HasTiming.Value, Is.True);
        }

        #endregion

        #region BeatDivisor / SnapTime / GetBeatLengthAtTime

        [Test]
        public void TestBeatDivisor_NotResolved_DefaultsToOne()
        {
            // `beatDivisor?.Value ?? 1`: beatDivisor is null.
            var editor = new EditorBeatmap(createBeatmap());

            Assert.That(editor.BeatDivisor, Is.EqualTo(1));
            Assert.That(editor.GetBeatLengthAtTime(0), Is.EqualTo(beat_length));
            Assert.That(editor.SnapTime(130, null), Is.EqualTo(0));
        }

        [Test]
        public void TestBeatDivisor_Resolved_UsesBindableValue()
        {
            // `beatDivisor?.Value ?? 1`: beatDivisor is non-null.
            var editor = createInjectedEditor(createBeatmap(), 4);

            Assert.That(editor.BeatDivisor, Is.EqualTo(4));
            Assert.That(editor.GetBeatLengthAtTime(0), Is.EqualTo(beat_length / 4));
            Assert.That(editor.SnapTime(130, null), Is.EqualTo(125));
        }

        #endregion

        #region SnapAllHitObjectsToCurrentDivisor

        [Test]
        public void TestSnapAll_EmptyBeatmap_ReturnsWithoutTransaction()
        {
            // guard `HitObjects.Count == 0`: true.
            var editor = new EditorBeatmap(createBeatmap());
            int transactions = 0;
            editor.TransactionBegan += () => transactions++;

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(transactions, Is.Zero);
            Assert.That(editor.UpdateInProgress.Value, Is.False);
        }

        [Test]
        public void TestSnapAll_HitCircle_OnlyStartTimeSnappedAndSwitchMatchesNothing()
        {
            // guard false; switch: no case matches (not IHasPathWithRepeats, not IHasDuration). Divisor 1 -> grid of 500 ms.
            var circle = new HitCircle { StartTime = 130 };
            var editor = new EditorBeatmap(createBeatmap(circle));
            var log = subscribe(editor);

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(circle.StartTime, Is.EqualTo(0));
            Assert.That(log.Updated, Is.EqualTo(new HitObject[] { circle }));
            Assert.That(log.TransactionsBegan, Is.EqualTo(1));
        }

        [Test]
        public void TestSnapAll_HitCircleWithDivisorFour_SnapsToQuarterBeat()
        {
            // non-null beat divisor feeds SnapTime: 130 -> nearest multiple of 125.
            var circle = new HitCircle { StartTime = 130 };
            var editor = createInjectedEditor(createBeatmap(circle), 4);

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(circle.StartTime, Is.EqualTo(125));
        }

        [Test]
        public void TestSnapAll_SliderLikeWithVelocity_VelocityScaledToSnappedDuration()
        {
            // switch case `IHasPathWithRepeats and IHasSliderVelocity` (both pattern conditions TRUE), `oldDuration > 0` TRUE.
            // old duration 400, snapped target duration 500 -> multiplier 1 * 400 / 500.
            var slider = new PathWithVelocityObject { StartTime = 0, Duration = 400 };
            var editor = new EditorBeatmap(createBeatmap(slider));

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(slider.StartTime, Is.EqualTo(0));
            Assert.That(slider.SliderVelocityMultiplier, Is.EqualTo(0.8).Within(1e-9));
            Assert.That(slider.Duration, Is.EqualTo(400), "duration of a path object is derived, only the velocity may be changed");
        }

        [Test]
        public void TestSnapAll_SliderLikeWithZeroDuration_VelocityLeftUntouched()
        {
            // switch case `IHasPathWithRepeats and IHasSliderVelocity`, `oldDuration > 0` FALSE.
            var slider = new PathWithVelocityObject { StartTime = 0, Duration = 0 };
            var editor = new EditorBeatmap(createBeatmap(slider));

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(slider.SliderVelocityMultiplier, Is.EqualTo(1));
        }

        [Test]
        public void TestSnapAll_PathWithoutSliderVelocity_FallsThroughToDurationCase()
        {
            // pattern `IHasPathWithRepeats` TRUE but `IHasSliderVelocity` FALSE -> first case fails, `case IHasDuration` taken.
            var pathObject = new PathOnlyObject { StartTime = 0, Duration = 400 };
            var editor = new EditorBeatmap(createBeatmap(pathObject));

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(pathObject.Duration, Is.EqualTo(500));
        }

        [Test]
        public void TestSnapAll_ShortSpinner_EndExtendedToAtLeastOneBeat()
        {
            // `Math.Max(newStart + beatLength, snappedEnd)`: first argument larger (snapped end 0 < 500). `case IHasDuration` taken.
            var spinner = new Spinner { StartTime = 0, Duration = 0 };
            var editor = new EditorBeatmap(createBeatmap(spinner));

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(spinner.Duration, Is.EqualTo(beat_length));
        }

        [Test]
        public void TestSnapAll_LongSpinner_EndSnappedToGrid()
        {
            // `Math.Max(newStart + beatLength, snappedEnd)`: second argument larger (snapped end 1000 > 500).
            var spinner = new Spinner { StartTime = 0, Duration = 980 };
            var editor = new EditorBeatmap(createBeatmap(spinner));

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(spinner.StartTime, Is.EqualTo(0));
            Assert.That(spinner.Duration, Is.EqualTo(1000));
        }

        [Test]
        public void TestSnapAll_MultipleObjects_EachSnappedInOneTransaction()
        {
            // `foreach` repeats with objects hitting different switch outcomes.
            var circle = new HitCircle { StartTime = 520 };
            var spinner = new Spinner { StartTime = 1010, Duration = 480 };
            var editor = new EditorBeatmap(createBeatmap(circle, spinner));
            var log = subscribe(editor);

            editor.SnapAllHitObjectsToCurrentDivisor();

            Assert.That(circle.StartTime, Is.EqualTo(500));
            Assert.That(spinner.StartTime, Is.EqualTo(1000));
            Assert.That(spinner.Duration, Is.EqualTo(500));
            Assert.That(log.TransactionsBegan, Is.EqualTo(1));
            Assert.That(log.Reprocessed, Is.EqualTo(1));
        }

        #endregion

        #region Dispose

        [Test]
        public void TestDispose_WithoutBeatmapSkin_Succeeds()
        {
            // `BeatmapSkin?.Dispose()`: BeatmapSkin is null.
            var editor = new EditorBeatmap(createBeatmap());

            Assert.DoesNotThrow(() => editor.Dispose());
                    }

        [Test]
        public void TestDispose_WithBeatmapSkin_DisposesEditorAndSkin()
        {
            // `BeatmapSkin?.Dispose()`: BeatmapSkin is non-null.
            var editor = new EditorBeatmap(createBeatmap(), createLegacySkin());

            Assert.DoesNotThrow(() => editor.Dispose());
                    }

        #endregion

        #region Helpers

        private static OsuBeatmap createBeatmap(params HitObject[] hitObjects) => createBeatmap(true, hitObjects);

        private static OsuBeatmap createBeatmap(bool withTiming, params HitObject[] hitObjects)
        {
            var beatmap = new OsuBeatmap
            {
                BeatmapInfo = { Ruleset = new OsuRuleset().RulesetInfo },
            };

            if (withTiming)
                beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = beat_length });

            foreach (var h in hitObjects)
                beatmap.HitObjects.Add((OsuHitObject)h);

            return beatmap;
        }

        private static LegacyBeatmapSkin createLegacySkin()
        {
            // LegacyBeatmapSkin requires BeatmapInfo.Path (derived from the beatmap set's file list); no resource provider is needed.
            var set = new BeatmapSetInfo();
            set.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = "hash" }, "beatmap.osu"));

            var info = new BeatmapInfo(new OsuRuleset().RulesetInfo)
            {
                Hash = "hash",
                BeatmapSet = set,
            };

            return new LegacyBeatmapSkin(info, null);
        }

        private static TestEditorBeatmap createInjectedEditor(OsuBeatmap playable, int divisor)
        {
            var editor = new TestEditorBeatmap(playable);
            var beatDivisor = new BindableBeatDivisor(divisor);

            // the [Resolved] members are private properties; without a game host they are assigned directly.
            setResolved(editor, "beatDivisor", beatDivisor);
            setResolved(editor, "editorClock", new EditorClock(playable, beatDivisor));
            return editor;
        }

        private static void setResolved(EditorBeatmap editor, string name, object value) =>
            typeof(EditorBeatmap).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, value);

        private static EventLog subscribe(EditorBeatmap editor)
        {
            var log = new EventLog();
            editor.HitObjectAdded += log.Added.Add;
            editor.HitObjectRemoved += log.Removed.Add;
            editor.HitObjectUpdated += log.Updated.Add;
            editor.BeatmapReprocessed += () => log.Reprocessed++;
            editor.TransactionBegan += () => log.TransactionsBegan++;
            editor.TransactionEnded += () => log.TransactionsEnded++;
            return log;
        }

        private class EventLog
        {
            public readonly List<HitObject> Added = new List<HitObject>();
            public readonly List<HitObject> Removed = new List<HitObject>();
            public readonly List<HitObject> Updated = new List<HitObject>();
            public int Reprocessed;
            public int TransactionsBegan;
            public int TransactionsEnded;
        }

        /// <summary>
        /// Exposes the protected frame <c>Update()</c> without needing a game host.
        /// </summary>
        private partial class TestEditorBeatmap : EditorBeatmap
        {
            public TestEditorBeatmap(IBeatmap playableBeatmap)
                : base(playableBeatmap)
            {
            }

            public void RunFrameUpdate() => Update();
        }

        /// <summary>
        /// A hit object with a path and repeats but without slider velocity (second condition of the slider pattern is FALSE).
        /// </summary>
        private class PathOnlyObject : OsuHitObject, IHasPathWithRepeats
        {
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; }
            public double Distance => Duration;
            public SliderPath Path { get; } = new SliderPath();
            public int RepeatCount { get; set; }
            public IList<IList<HitSampleInfo>> NodeSamples { get; } = new List<IList<HitSampleInfo>>();
        }

        /// <summary>
        /// A hit object with a path, repeats and slider velocity (both conditions of the slider pattern are TRUE).
        /// </summary>
        private class PathWithVelocityObject : PathOnlyObject, IHasSliderVelocity
        {
            public BindableNumber<double> SliderVelocityMultiplierBindable { get; } = new BindableDouble(1) { MinValue = 0.01, MaxValue = 10 };

            public double SliderVelocityMultiplier
            {
                get => SliderVelocityMultiplierBindable.Value;
                set => SliderVelocityMultiplierBindable.Value = value;
            }
        }

        #endregion
    }
}
