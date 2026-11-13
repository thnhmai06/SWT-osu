// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Rulesets.Difficulty
{
    [TestFixture]
    public class DifficultyCalculatorTest
    {
        /// <summary>Records every processed object's start time.</summary>
        private class RecordingSkill : Skill
        {
            public readonly List<double> StartTimes = new List<double>();

            public RecordingSkill(Mod[] mods)
                : base(mods)
            {
            }

            protected override double ProcessInternal(DifficultyHitObject current)
            {
                StartTimes.Add(current.StartTime);
                return 1;
            }

            public override double DifficultyValue() => StartTimes.Count;
        }

        private class TestCalculator : DifficultyCalculator
        {
            private readonly Mod[] adjustmentMods;

            public Action<RecordingSkill>? OnSkillCreated;

            public TestCalculator(IWorkingBeatmap beatmap, Mod[]? adjustmentMods = null)
                : base(new OsuRuleset().RulesetInfo, beatmap)
            {
                this.adjustmentMods = adjustmentMods ?? [];
            }

            protected override Mod[] DifficultyAdjustmentMods => adjustmentMods;

            protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills)
                => new DifficultyAttributes(mods, skills.Sum(s => s.DifficultyValue())) { MaxCombo = beatmap.HitObjects.Count };

            // deliberately returned in reverse order, to prove that SortObjects is applied
            protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, Mod[] mods)
            {
                var list = new List<DifficultyHitObject>();
                HitObject last = beatmap.HitObjects[0];

                for (int i = 0; i < beatmap.HitObjects.Count; i++)
                {
                    list.Add(new DifficultyHitObject(beatmap.HitObjects[i], last, 1, list, i));
                    last = beatmap.HitObjects[i];
                }

                return Enumerable.Reverse(list);
            }

            protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods)
            {
                var skill = new RecordingSkill(mods);
                OnSkillCreated?.Invoke(skill);
                return [skill];
            }
        }

        private static TestCalculator calculator(Mod[]? adjustmentMods = null, params double[] times)
        {
            var beatmap = new Beatmap { BeatmapInfo = { Ruleset = new OsuRuleset().RulesetInfo } };
            foreach (double t in times)
                beatmap.HitObjects.Add(new HitCircle { StartTime = t });

            return new TestCalculator(new TestWorkingBeatmap(beatmap), adjustmentMods);
        }

        [Test]
        public void TestDefaultVersion()
        {
            Assert.That(calculator().Version, Is.EqualTo(0));
        }

        // CF: DifficultyCalculator.Calculate | !Beatmap.HitObjects.Any() | TRUE
        [Test]
        public void TestCalculateEmptyBeatmapSkipsProcessing()
        {
            var attributes = calculator().Calculate();

            Assert.That(attributes.StarRating, Is.EqualTo(0));
            Assert.That(attributes.MaxCombo, Is.EqualTo(0));
        }

        // CF: DifficultyCalculator.Calculate | !Beatmap.HitObjects.Any() | FALSE
        // CF: DifficultyCalculator.Calculate | foreach hitObject | loop n
        // CF: DifficultyCalculator.Calculate | foreach skill | loop n
        // CF: DifficultyCalculator.Calculate | !cancellationToken.CanBeCanceled | TRUE
        [Test]
        public void TestCalculateProcessesAllObjects()
        {
            var attributes = calculator(null, 0, 500, 1000).Calculate();

            Assert.That(attributes.StarRating, Is.EqualTo(3));
            Assert.That(attributes.MaxCombo, Is.EqualTo(3));
        }

        // CF: DifficultyCalculator.SortObjects | input.OrderBy(StartTime) | non-null
        [Test]
        public void TestObjectsProcessedInStartTimeOrder()
        {
            var calc = calculator(null, 0, 500, 1000);
            RecordingSkill? captured = null;
            calc.OnSkillCreated = s => captured = s;
            var attributes = calc.Calculate();

            Assert.That(attributes.StarRating, Is.EqualTo(3));
            Assert.That(captured!.StartTimes, Is.EqualTo(new[] { 0d, 500d, 1000d }));
        }

        // CF: DifficultyCalculator.Calculate | !cancellationToken.CanBeCanceled | FALSE
        [Test]
        public void TestCalculateHonoursCancelledToken()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.That(() => calculator(null, 0, 500).Calculate(cts.Token), Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public void TestCalculateClonesMods()
        {
            var mod = new OsuModHardRock();

            var attributes = calculator(null, 0, 500).Calculate([mod]);

            Assert.That(attributes.Mods.Single(), Is.InstanceOf<OsuModHardRock>());
            Assert.That(attributes.Mods.Single(), Is.Not.SameAs(mod));
        }

        // CF: DifficultyCalculator.CalculateTimed | !Beatmap.HitObjects.Any() | TRUE
        [Test]
        public void TestCalculateTimedEmpty()
        {
            Assert.That(calculator().CalculateTimed(), Is.Empty);
        }

        // CF: DifficultyCalculator.CalculateTimed | !Beatmap.HitObjects.Any() | FALSE
        // CF: DifficultyCalculator.CalculateTimed | foreach obj in Beatmap.HitObjects | loop n
        // CF: DifficultyCalculator.CalculateTimed | currentIndex < difficultyObjects.Length && BaseObject.GetEndTime() <= obj.GetEndTime() | TRUE
        // CF: DifficultyCalculator.CalculateTimed | currentIndex < difficultyObjects.Length && BaseObject.GetEndTime() <= obj.GetEndTime() | FALSE
        [Test]
        public void TestCalculateTimedIsProgressive()
        {
            // objects 1 and 2 share an end time, so both are processed while handling object 1 and nothing is left for object 2.
            var timed = calculator(null, 0, 1000, 1000).CalculateTimed();

            Assert.That(timed.Select(t => t.Time), Is.EqualTo(new[] { 0d, 1000d, 1000d }));
            Assert.That(timed.Select(t => t.Attributes.StarRating), Is.EqualTo(new[] { 1d, 3d, 3d }));
            Assert.That(timed.Select(t => t.Attributes.MaxCombo), Is.EqualTo(new[] { 1, 2, 3 }), "attributes see only the objects added so far");
        }

        // CF: DifficultyCalculator.CalculateTimed | !cancellationToken.CanBeCanceled | FALSE
        [Test]
        public void TestCalculateTimedHonoursCancelledToken()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.That(() => calculator(null, 0, 500).CalculateTimed(cts.Token), Throws.InstanceOf<OperationCanceledException>());
        }

        // CF: DifficultyCalculator.createDifficultyAdjustmentModCombinations | currentSetCount | case 0
        [Test]
        public void TestNoAdjustmentModsYieldsOnlyNoMod()
        {
            Assert.That(calculator().CreateDifficultyAdjustmentModCombinations().Single(), Is.InstanceOf<ModNoMod>());
        }

        // CF: DifficultyCalculator.createDifficultyAdjustmentModCombinations | currentSetCount | case 1
        // CF: DifficultyCalculator.createDifficultyAdjustmentModCombinations | currentSetCount | default
        // CF: DifficultyCalculator.createDifficultyAdjustmentModCombinations | currentSet.SelectMany(IncompatibleMods).Any(c => nextSet.Any(c.IsInstanceOfType)) | TRUE
        // CF: DifficultyCalculator.createDifficultyAdjustmentModCombinations | currentSet.SelectMany(IncompatibleMods).Any(c => nextSet.Any(c.IsInstanceOfType)) | FALSE
        [Test]
        public void TestCombinationsSkipIncompatibleMods()
        {
            var combos = calculator([new OsuModEasy(), new OsuModHardRock(), new OsuModDoubleTime()]).CreateDifficultyAdjustmentModCombinations();

            Assert.That(describe(combos), Is.EqualTo(new[] { "NM", "EZ", "EZ+DT", "HR", "HR+DT", "DT" }));
        }

        // CF: DifficultyCalculator.createDifficultyAdjustmentModCombinations | currentSet.Any(c => nextSet.Any(n => c.GetType() == n.GetType())) | TRUE
        [Test]
        public void TestCombinationsSkipSameTypeMods()
        {
            var combos = calculator([new OsuModDoubleTime(), new OsuModDoubleTime()]).CreateDifficultyAdjustmentModCombinations();

            Assert.That(describe(combos), Is.EqualTo(new[] { "NM", "DT", "DT" }));
        }

        // CF: DifficultyCalculator.flatten | !(mod is MultiMod multi) | FALSE
        // CF: DifficultyCalculator.flatten | foreach nested in multi.Mods | loop n
        [Test]
        public void TestCombinationsFlattenMultiMods()
        {
            var combos = calculator([new MultiMod(new OsuModDoubleTime(), new OsuModHidden()), new OsuModHardRock()]).CreateDifficultyAdjustmentModCombinations();

            Assert.That(describe(combos), Is.EqualTo(new[] { "NM", "DT+HD", "DT+HD+HR", "HR" }));
        }

        // CF: DifficultyCalculator.CalculateAllLegacyCombinations | classicMod != null | TRUE
        // CF: DifficultyCalculator.CalculateAllLegacyCombinations | classicMod != null | infeasible: FALSE needs a ruleset without ModClassic, and the calculator then needs a working converter for it
        // CF: DifficultyCalculator.CalculateAllLegacyCombinations | foreach combination | loop n
        [Test]
        public void TestAllLegacyCombinationsAppendClassic()
        {
            var results = calculator([new OsuModEasy(), new OsuModDoubleTime()], 0, 500).CalculateAllLegacyCombinations().ToArray();

            Assert.That(results, Has.Length.EqualTo(4));
            Assert.That(results.Select(r => r.Mods.Last()), Has.All.InstanceOf<OsuModClassic>());
            Assert.That(results.Select(r => r.Mods.Length), Is.EqualTo(new[] { 2, 2, 3, 2 }), "NM, EZ, EZ+DT, DT each plus Classic");
        }

        private static string[] describe(Mod[] combos) => combos.Select(c => c is MultiMod m ? string.Join("+", m.Mods.Select(x => x.Acronym)) : c is ModNoMod ? "NM" : c.Acronym).ToArray();
    }
}
