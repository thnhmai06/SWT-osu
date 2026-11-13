// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class HitObjectTest
    {
        private static readonly BeatmapDifficulty difficulty = new BeatmapDifficulty { OverallDifficulty = 5 };

        private class ParentObject : HitObject
        {
            public HitObject? Child1;
            public HitObject? Child2;

            protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
            {
                // intentionally added out of order to verify sorting.
                AddNested(Child2 = new HitObject { StartTime = StartTime + 200 });
                AddNested(Child1 = new HitObject { StartTime = StartTime + 100 });
            }
        }

        private static HitSampleInfo sample(string name, string bank = HitSampleInfo.BANK_NORMAL, bool auto = true) => new HitSampleInfo(name, bank, editorAutoBank: auto);

        // CF: HitObject.StartTime | set | no throw
        // CF: HitObject.Samples | set | no throw
        [Test]
        public void TestStartTimeAndSamplesReplacement()
        {
            var obj = new HitObject { StartTime = 500 };
            obj.Samples = new List<HitSampleInfo> { sample("hitnormal") };
            obj.Samples = new List<HitSampleInfo> { sample("hitclap"), sample("hitfinish") };

            Assert.That(obj.StartTimeBindable.Value, Is.EqualTo(500));
            Assert.That(obj.Samples.Select(s => s.Name), Is.EqualTo(new[] { "hitclap", "hitfinish" }));
            Assert.That(obj.AuxiliarySamples, Is.Empty);
        }

        // CF: HitObject.ApplyDefaultsToSelf | EffectPointAt(StartTime + leniency).KiaiMode | TRUE
        // CF: HitObject.ApplyDefaultsToSelf | EffectPointAt(StartTime + leniency).KiaiMode | FALSE
        // CF: HitObject.ApplyDefaultsToSelf | HitWindows ??= CreateHitWindows() | non-null
        [Test]
        public void TestKiaiLookupUsesOneMillisecondLeniency()
        {
            var controlPoints = new ControlPointInfo();
            controlPoints.Add(1000, new EffectControlPoint { KiaiMode = true });

            var inside = new HitObject { StartTime = 999 };
            inside.ApplyDefaults(controlPoints, difficulty);
            var outside = new HitObject { StartTime = 998 };
            outside.ApplyDefaults(controlPoints, difficulty);

            Assert.That(inside.Kiai, Is.True);
            Assert.That(outside.Kiai, Is.False);
            Assert.That(inside.HitWindows, Is.TypeOf<DefaultHitWindows>());
        }

        // CF: HitObject.ApplyDefaultsToSelf | HitWindows ??= CreateHitWindows() | null
        // CF: HitObject.MaximumJudgementOffset | HitWindows?.WindowFor(Miss) ?? 0 | null
        // CF: HitObject.MaximumJudgementOffset | HitWindows?.WindowFor(Miss) ?? 0 | non-null
        [Test]
        public void TestExistingHitWindowsKeptAndMaximumJudgementOffset()
        {
            var obj = new HitObject { HitWindows = HitWindows.Empty };
            Assert.That(obj.MaximumJudgementOffset, Is.Zero);

            obj.ApplyDefaults(new ControlPointInfo(), difficulty);
            Assert.That(obj.HitWindows, Is.SameAs(HitWindows.Empty));

            var withDefault = new HitObject();
            Assert.That(withDefault.MaximumJudgementOffset, Is.Zero, "no hit windows before defaults are applied");
            withDefault.ApplyDefaults(new ControlPointInfo(), difficulty);
            Assert.That(withDefault.MaximumJudgementOffset, Is.GreaterThan(0));
        }

        // CF: HitObject.ApplyDefaults | DefaultsApplied?.Invoke | non-null
        // CF: HitObject.ApplyDefaults | cancellationToken.ThrowIfCancellationRequested | throws OperationCanceledException
        [Test]
        public void TestDefaultsAppliedEventAndCancellation()
        {
            var obj = new HitObject();
            int raised = 0;
            obj.DefaultsApplied += _ => raised++;

            obj.ApplyDefaults(new ControlPointInfo(), difficulty);
            Assert.That(raised, Is.EqualTo(1));

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<OperationCanceledException>(() => obj.ApplyDefaults(new ControlPointInfo(), difficulty, cts.Token));
        }

        // CF: HitObject.ApplyDefaults | nestedHitObjects.Sort | no throw
        // CF: HitObject.ApplyDefaults | foreach nested | loop n
        // CF: HitObject.ApplyDefaults | foreach nested | loop 0
        // CF: HitObject.ApplyDefaults | this is IHasComboInformation | FALSE
        [Test]
        public void TestNestedObjectsAreSortedAndGetDefaults()
        {
            var parent = new ParentObject { StartTime = 1000 };
            parent.ApplyDefaults(new ControlPointInfo(), difficulty);

            Assert.That(parent.NestedHitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 1100.0, 1200 }));
            Assert.That(parent.Child1!.HitWindows, Is.Not.Null);

            // defaults can be re-applied without duplicating nested objects.
            parent.ApplyDefaults(new ControlPointInfo(), difficulty);
            Assert.That(parent.NestedHitObjects.Count, Is.EqualTo(2));

            var leaf = new HitObject();
            leaf.ApplyDefaults(new ControlPointInfo(), difficulty);
            Assert.That(leaf.NestedHitObjects.Count, Is.Zero);
        }

        // CF: HitObject.ApplyDefaults | onStartTimeChanged | foreach nested | loop n
        [Test]
        public void TestMovingParentMovesNestedObjectsExactlyOnceEvenAfterReapplyingDefaults()
        {
            var parent = new ParentObject { StartTime = 1000 };
            parent.ApplyDefaults(new ControlPointInfo(), difficulty);
            parent.ApplyDefaults(new ControlPointInfo(), difficulty);

            var children = parent.NestedHitObjects.ToArray();
            parent.StartTime = 1500;

            Assert.That(children.Select(c => c.StartTime), Is.EqualTo(new[] { 1600.0, 1700 }));
        }

        // CF: HitObject.ApplyDefaults | this is IHasComboInformation | TRUE
        // CF: HitObject.ApplyDefaults | hitObject is IHasComboInformation | TRUE
        [Test]
        public void TestNestedComboInformationIsBoundToParent()
        {
            var slider = new Slider
            {
                StartTime = 0,
                Position = Vector2.Zero,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }, 100),
            };
            slider.ApplyDefaults(new ControlPointInfo(), difficulty);

            slider.IndexInCurrentComboBindable.Value = 4;
            slider.ComboIndexBindable.Value = 2;

            var nestedWithCombo = slider.NestedHitObjects.OfType<IHasComboInformation>().ToArray();
            Assert.That(nestedWithCombo, Is.Not.Empty);
            Assert.That(nestedWithCombo.All(n => n.IndexInCurrentComboBindable.Value == 4 && n.ComboIndexBindable.Value == 2), Is.True);
        }

        // CF: HitObject.Judgement | judgement ??= CreateJudgement() | non-null
        [Test]
        public void TestJudgementIsCreatedOnceAndCached()
        {
            var obj = new HitObject();

            Assert.That(obj.Judgement, Is.SameAs(obj.Judgement));
            Assert.That(obj.Judgement.GetType(), Is.EqualTo(typeof(Judgement)));
        }

        // CF: HitObject.CreateSlidingSamples | normalSample != null | TRUE
        // CF: HitObject.CreateSlidingSamples | whistleSample != null | TRUE
        // CF: HitObject.CreateSlidingSamples | normalSample != null | FALSE
        // CF: HitObject.CreateSlidingSamples | whistleSample != null | FALSE
        [Test]
        public void TestSlidingSamples()
        {
            var both = new HitObject { Samples = new List<HitSampleInfo> { sample("hitnormal", "soft"), sample("hitwhistle", "drum") } };
            var slides = both.CreateSlidingSamples();
            Assert.That(slides.Select(s => (s.Name, s.Bank)), Is.EqualTo(new[] { ("sliderslide", "soft"), ("sliderwhistle", "drum") }));

            Assert.That(new HitObject { Samples = new List<HitSampleInfo> { sample("hitclap") } }.CreateSlidingSamples(), Is.Empty);
        }

        // CF: HitObject.CreateHitSampleInfo | sampleName != HIT_NORMAL | TRUE
        // CF: HitObject.CreateHitSampleInfo | existing addition is HitSampleInfo | TRUE
        // CF: HitObject.CreateHitSampleInfo | existing addition is HitSampleInfo | FALSE
        // CF: HitObject.CreateHitSampleInfo | existing normal is HitSampleInfo | TRUE
        // CF: HitObject.CreateHitSampleInfo | existing normal is HitSampleInfo | FALSE
        // CF: HitObject.CreateHitSampleInfo | sampleName != HIT_NORMAL | FALSE
        [Test]
        public void TestCreateHitSampleInfo()
        {
            var withAddition = new HitObject { Samples = new List<HitSampleInfo> { sample("hitnormal", "soft"), sample("hitwhistle", "drum", false) } };
            Assert.That(withAddition.CreateHitSampleInfo("hitclap").Bank, Is.EqualTo("drum"), "additions share one bank");
            Assert.That(withAddition.CreateHitSampleInfo().Bank, Is.EqualTo("soft"), "normal sample uses the normal bank");

            var onlyNormal = new HitObject { Samples = new List<HitSampleInfo> { sample("hitnormal", "soft", false) } };
            var derived = onlyNormal.CreateHitSampleInfo("hitfinish");
            Assert.That((derived.Name, derived.Bank, derived.EditorAutoBank), Is.EqualTo(("hitfinish", "soft", true)));

            var none = new HitObject().CreateHitSampleInfo("hitclap");
            Assert.That(none.Name, Is.EqualTo("hitclap"));
        }

        // CF: HitObject.ToString | interpolation | no throw
        // CF: HitObjectExtensions.GetEndTime | hitObject as IHasDuration ?? StartTime | null
        // CF: HitObjectExtensions.GetEndTime | hitObject as IHasDuration ?? StartTime | non-null
        [Test]
        public void TestToStringAndEndTime()
        {
            Assert.That(new HitObject { StartTime = 5 }.ToString(), Is.EqualTo("HitObject @ 5"));
            Assert.That(new HitObject { StartTime = 5 }.GetEndTime(), Is.EqualTo(5));
            Assert.That(new Spinner { StartTime = 5, Duration = 100 }.GetEndTime(), Is.EqualTo(105));
        }
    }
}
