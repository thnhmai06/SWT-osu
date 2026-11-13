// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns.Legacy;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps.Patterns.Legacy
{
    [TestFixture]
    public class HitCirclePatternGeneratorTest
    {
        private const double now = 1000;

        private static (Pattern pattern, HitCirclePatternGenerator generator) generate(
            double timeSeparation, int columns, Pattern? previous = null, PatternType lastStair = PatternType.Stair, bool samePosition = true,
            double density = 0, IBeatmap? beatmap = null, FakeCircle? circle = null, int seed = 1)
        {
            circle ??= new FakeCircle { X = 100, Y = 100 };
            circle.StartTime = now;

            var previousPosition = samePosition ? circle.Position : circle.Position + new Vector2(200, 0);
            var generator = new HitCirclePatternGenerator(new LegacyRandom(seed), circle, beatmap ?? PatternTestHelper.LowDifficulty(), columns, previous ?? new Pattern(),
                now - timeSeparation, previousPosition, density, lastStair);

            var patterns = generator.Generate().ToArray();
            Assert.That(patterns, Has.Length.EqualTo(1));
            return (patterns[0], generator);
        }

        // CF: HitCirclePatternGenerator.Generate | TotalColumns == 1 | TRUE
        [Test]
        public void TestSingleColumn() => Assert.That(PatternTestHelper.Columns(generate(1000, 1).pattern), Is.EqualTo(new[] { 0 }));

        // CF: HitCirclePatternGenerator..ctor | timeSeparation <= 125 | FALSE
        // CF: HitCirclePatternGenerator..ctor | timeSeparation <= 135 && positionSeparation < 20 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(Cycle) && PreviousPattern.HitObjects.Count() == 1 | TRUE
        // CF: HitCirclePatternGenerator.Generate | TotalColumns != 8 || lastColumn != 0 | TRUE
        // CF: HitCirclePatternGenerator.Generate | TotalColumns % 2 == 0 || lastColumn != TotalColumns / 2 | TRUE
        // CF: HitCirclePatternGenerator.Generate | lastColumn = FirstOrDefault()?.Column ?? 0 | non-null
        [TestCase(4, 1, 2)]
        [TestCase(4, 0, 3)]
        [TestCase(6, 5, 0)]
        public void TestCycleMirrorsSingleNoteColumn(int columns, int lastColumn, int expected)
        {
            var (pattern, _) = generate(130, columns, PatternTestHelper.PatternOf(lastColumn));
            Assert.That(PatternTestHelper.Columns(pattern), Is.EqualTo(new[] { expected }));
        }

        // CF: HitCirclePatternGenerator.Generate | TotalColumns != 8 || lastColumn != 0 | FALSE
        // CF: HitCirclePatternGenerator.Generate | TotalColumns % 2 == 0 || lastColumn != TotalColumns / 2 | FALSE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(Cycle) && Count() == 1 | FALSE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(KeepSingle) | TRUE
        [Test]
        public void TestCycleSkippedForSpecialKeysFallsBackToSingleNote()
        {
            // 8K with the special column used last, centre column of 5K used last, and a previous pattern of two notes.
            foreach (var (columns, previous) in new[] { (8, PatternTestHelper.PatternOf(0)), (5, PatternTestHelper.PatternOf(2)), (4, PatternTestHelper.PatternOf(0, 1)) })
            {
                var pattern = generate(130, columns, previous).pattern;
                Assert.That(pattern.HitObjects.Count(), Is.EqualTo(1));
                Assert.That(pattern.HitObjects.Single().Column, Is.InRange(0, columns - 1));
            }
        }

        // CF: HitCirclePatternGenerator..ctor | timeSeparation <= 150 && positionSeparation < 20 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(ForceStack) && PreviousPattern.HitObjects.Any() | TRUE
        // CF: HitCirclePatternGenerator.Generate | foreach i; PreviousPattern.ColumnHasObject(i) | TRUE
        // CF: HitCirclePatternGenerator.Generate | foreach i; PreviousPattern.ColumnHasObject(i) | FALSE
        [Test]
        public void TestForceStackRepeatsPreviousColumns()
        {
            var (pattern, _) = generate(140, 4, PatternTestHelper.PatternOf(1, 3));
            Assert.That(PatternTestHelper.Columns(pattern), Is.EquivalentTo(new[] { 1, 3 }));
        }

        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(ForceStack) && PreviousPattern.HitObjects.Any() | FALSE
        [Test]
        public void TestForceStackWithoutPreviousObjectsStillProducesNote()
        {
            Assert.That(generate(140, 4).pattern.HitObjects.Count(), Is.GreaterThanOrEqualTo(1));
        }

        // CF: HitCirclePatternGenerator..ctor | positionSeparation < 20 && density >= BeatLength / 2.5 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(Reverse) && PreviousPattern.HitObjects.Any() | TRUE
        // CF: HitCirclePatternGenerator.Generate | for i = RandomStart; i < TotalColumns | loop n
        [Test]
        public void TestLowDensityStreamReversesPreviousColumns()
        {
            var (pattern, _) = generate(1000, 4, PatternTestHelper.PatternOf(0, 2), density: 400);
            Assert.That(PatternTestHelper.Columns(pattern), Is.EquivalentTo(new[] { 3, 1 }));
        }

        // CF: HitCirclePatternGenerator.Generate | for i = RandomStart (8 columns) | loop n
        [Test]
        public void TestReverseOnEightColumnsKeepsSpecialColumnOut()
        {
            var (pattern, _) = generate(1000, 8, PatternTestHelper.PatternOf(1, 3), density: 400);
            Assert.That(PatternTestHelper.Columns(pattern), Is.EquivalentTo(new[] { 7, 5 }));
        }

        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(Reverse) && PreviousPattern.HitObjects.Any() | FALSE
        [Test]
        public void TestReverseWithoutPreviousObjectsStillProducesNote()
        {
            Assert.That(generate(1000, 4, density: 400).pattern.HitObjects.Count(), Is.GreaterThanOrEqualTo(1));
        }

        // CF: HitCirclePatternGenerator..ctor | timeSeparation <= 95 | TRUE
        // CF: HitCirclePatternGenerator.Generate | PreviousPattern.HitObjects.Count() == 1 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(Stair) | TRUE
        // CF: HitCirclePatternGenerator.Generate | targetColumn == TotalColumns | FALSE
        [Test]
        public void TestStairMovesToNextColumn()
        {
            var (pattern, generator) = generate(90, 4, PatternTestHelper.PatternOf(1), PatternType.Stair);
            Assert.That(PatternTestHelper.Columns(pattern), Is.EqualTo(new[] { 2 }));
            Assert.That(generator.StairType, Is.EqualTo(PatternType.Stair));
        }

        // CF: HitCirclePatternGenerator.Generate | obj.Column == TotalColumns - 1 (Stair) | TRUE
        [Test]
        public void TestStairReachingLastColumnSwitchesToReverseStair()
        {
            var (pattern, generator) = generate(90, 4, PatternTestHelper.PatternOf(2), PatternType.Stair);
            Assert.That(PatternTestHelper.Columns(pattern), Is.EqualTo(new[] { 3 }));
            Assert.That(generator.StairType, Is.EqualTo(PatternType.ReverseStair));
        }

        // CF: HitCirclePatternGenerator.Generate | targetColumn == TotalColumns | TRUE
        [Test]
        public void TestStairWrapsToFirstColumn()
        {
            Assert.That(PatternTestHelper.Columns(generate(90, 4, PatternTestHelper.PatternOf(3), PatternType.Stair).pattern), Is.EqualTo(new[] { 0 }));
            Assert.That(PatternTestHelper.Columns(generate(90, 8, PatternTestHelper.PatternOf(7), PatternType.Stair).pattern), Is.EqualTo(new[] { 1 }));
        }

        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(ReverseStair) | TRUE
        // CF: HitCirclePatternGenerator.Generate | targetColumn == RandomStart - 1 | FALSE
        [Test]
        public void TestReverseStairMovesToPreviousColumn()
        {
            var (pattern, generator) = generate(90, 4, PatternTestHelper.PatternOf(2), PatternType.ReverseStair);
            Assert.That(PatternTestHelper.Columns(pattern), Is.EqualTo(new[] { 1 }));
            Assert.That(generator.StairType, Is.EqualTo(PatternType.ReverseStair));
        }

        // CF: HitCirclePatternGenerator.Generate | obj.Column == RandomStart (ReverseStair) | TRUE
        [Test]
        public void TestReverseStairReachingFirstColumnSwitchesToStair()
        {
            var (pattern, generator) = generate(90, 4, PatternTestHelper.PatternOf(1), PatternType.ReverseStair);
            Assert.That(PatternTestHelper.Columns(pattern), Is.EqualTo(new[] { 0 }));
            Assert.That(generator.StairType, Is.EqualTo(PatternType.Stair));
        }

        // CF: HitCirclePatternGenerator.Generate | targetColumn == RandomStart - 1 | TRUE
        [Test]
        public void TestReverseStairWrapsToLastColumn()
        {
            Assert.That(PatternTestHelper.Columns(generate(90, 4, PatternTestHelper.PatternOf(0), PatternType.ReverseStair).pattern), Is.EqualTo(new[] { 3 }));
            Assert.That(PatternTestHelper.Columns(generate(90, 8, PatternTestHelper.PatternOf(1), PatternType.ReverseStair).pattern), Is.EqualTo(new[] { 7 }));
        }

        // CF: HitCirclePatternGenerator.Generate | PreviousPattern.HitObjects.Count() == 1 | FALSE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(Stair) | FALSE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(ReverseStair) | FALSE
        [Test]
        public void TestStairNeedsExactlyOnePreviousNote()
        {
            var (pattern, generator) = generate(90, 4, PatternTestHelper.PatternOf(1, 2), PatternType.Stair);
            Assert.That(pattern.HitObjects.Count(), Is.EqualTo(1));
            Assert.That(generator.StairType, Is.Not.Null);
        }

        // CF: HitCirclePatternGenerator..ctor | timeSeparation <= 80 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(KeepSingle) | TRUE
        // CF: HitCirclePatternGenerator.generateRandomNotes | noteCount = Math.Min(noteCount, TotalColumns - RandomStart - ColumnWithObjects) | non-null
        // CF: HitCirclePatternGenerator.generateRandomNotes | allowStacking | FALSE
        [Test]
        public void TestVeryFastNotesAreSingleAndAvoidPreviousColumns()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var (pattern, _) = generate(50, 4, PatternTestHelper.PatternOf(0, 1), seed: seed);
                Assert.That(pattern.HitObjects.Count(), Is.EqualTo(1));
                Assert.That(pattern.HitObjects.Single().Column, Is.AnyOf(2, 3));
            }
        }

        // CF: HitCirclePatternGenerator.generateRandomNotes | allowStacking | TRUE
        // CF: HitCirclePatternGenerator..ctor | timeSeparation <= 105 | TRUE
        [Test]
        public void TestForceNotStackFastNotesAvoidPreviousColumns()
        {
            for (int seed = 0; seed < 10; seed++)
                Assert.That(generate(100, 4, PatternTestHelper.PatternOf(0, 1, 2), seed: seed).pattern.HitObjects.Single().Column, Is.EqualTo(3));
        }

        // CF: HitCirclePatternGenerator..ctor | timeSeparation <= 125 | TRUE
        [Test]
        public void TestForceNotStackAt120()
        {
            for (int seed = 0; seed < 10; seed++)
                Assert.That(generate(120, 4, PatternTestHelper.PatternOf(0, 1, 2), seed: seed).pattern.HitObjects.Single().Column, Is.EqualTo(3));
        }

        // CF: HitCirclePatternGenerator..ctor | density < BeatLength / 2.5 || KiaiMode | TRUE
        // CF: HitCirclePatternGenerator..ctor | KeepSingle flag | FALSE
        // CF: HitCirclePatternGenerator.Generate | ConversionDifficulty > 2 | FALSE
        [Test]
        public void TestLowDifficultyHighDensityGivesSingleNote()
        {
            var (pattern, _) = generate(1000, 4, density: 100);
            Assert.That(pattern.HitObjects.Count(), Is.EqualTo(1));
        }

        // CF: HitCirclePatternGenerator..ctor | else (not high density, no kiai) | default
        // CF: HitCirclePatternGenerator..ctor | positionSeparation < 20 | FALSE
        [Test]
        public void TestSlowFarApartNotesGiveSingleNoteAtLowDifficulty()
        {
            var (pattern, _) = generate(1000, 4, samePosition: false, density: 400);
            Assert.That(pattern.HitObjects.Count(), Is.EqualTo(1));
        }

        // CF: HitCirclePatternGenerator..ctor | HIT_CLAP | TRUE (Gathered)
        // CF: HitCirclePatternGenerator.getRandomNoteCount | HIT_CLAP | p2 = 1
        // CF: HitCirclePatternGenerator.generateRandomNotes | convertType.HasFlag(Gathered) | TRUE
        // CF: HitCirclePatternGenerator.generateRandomNotes | allowStacking | TRUE
        [Test]
        public void TestClapGathersTwoAdjacentNotes()
        {
            var circle = new FakeCircle { X = 0, Y = 100 }.With(HitSampleInfo.HIT_CLAP);
            var (pattern, _) = generate(1000, 4, density: 100, circle: circle);

            Assert.That(PatternTestHelper.Columns(pattern), Is.EquivalentTo(new[] { 0, 1 }));
        }

        // CF: HitCirclePatternGenerator.generateRandomNotes | last == TotalColumns (Gathered wrap) | TRUE
        [Test]
        public void TestGatheredNotesWrapAroundLastColumn()
        {
            var circle = new FakeCircle { X = 511, Y = 100 }.With(HitSampleInfo.HIT_CLAP);
            var (pattern, _) = generate(1000, 4, density: 100, circle: circle);

            Assert.That(PatternTestHelper.Columns(pattern), Is.EquivalentTo(new[] { 3, 0 }));
        }

        // CF: HitCirclePatternGenerator.generateRandomPattern | RandomStart > 0 && hasSpecialColumn | TRUE
        // CF: HitCirclePatternGenerator..ctor | HIT_FINISH && TotalColumns != 8 | FALSE
        [Test]
        public void TestEightColumnsClapAndFinishAddsSpecialColumn()
        {
            var circle = new FakeCircle { X = 100, Y = 100 }.With(HitSampleInfo.HIT_CLAP, HitSampleInfo.HIT_FINISH);
            var (pattern, _) = generate(1000, 8, density: 100, circle: circle);

            Assert.That(PatternTestHelper.Columns(pattern), Does.Contain(0));
            Assert.That(pattern.HitObjects.Count(), Is.EqualTo(3));
        }

        // CF: HitCirclePatternGenerator.generateRandomPattern | RandomStart > 0 | FALSE
        // CF: HitCirclePatternGenerator.generateRandomPattern | hasSpecialColumn | FALSE
        [Test]
        public void TestNoSpecialColumnWithoutBothSamples()
        {
            var circle = new FakeCircle { X = 100, Y = 100 }.With(HitSampleInfo.HIT_CLAP);
            Assert.That(PatternTestHelper.Columns(generate(1000, 8, density: 100, circle: circle).pattern), Does.Not.Contain(0));
        }

        // CF: HitCirclePatternGenerator..ctor | HIT_FINISH && TotalColumns != 8 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(Mirror) | TRUE
        // CF: HitCirclePatternGenerator.generateRandomPatternWithMirrored | convertType.HasFlag(ForceNotStack) | FALSE
        // CF: HitCirclePatternGenerator.Generate | ConversionDifficulty > 6.5 (mirror) | TRUE
        // CF: HitCirclePatternGenerator.Generate | ConversionDifficulty > 6.5 (mirror) | FALSE
        [TestCase(true)]
        [TestCase(false)]
        public void TestFinishProducesSymmetricPattern(bool high)
        {
            var circle = new FakeCircle { X = 100, Y = 100 }.With(HitSampleInfo.HIT_FINISH);
            for (int seed = 0; seed < 30; seed++)
            {
                var (pattern, _) = generate(1000, 4, density: 100, beatmap: high ? PatternTestHelper.HighDifficulty() : PatternTestHelper.LowDifficulty(), circle: new FakeCircle { X = 100, Y = 100 }.With(HitSampleInfo.HIT_FINISH), seed: seed);
                var columns = PatternTestHelper.Columns(pattern).ToHashSet();

                Assert.That(columns, Is.Not.Empty);
                Assert.That(columns.Select(c => 3 - c).ToHashSet(), Is.EquivalentTo(columns));
            }

            Assert.That(circle.Samples, Is.Not.Empty);
        }

        // CF: HitCirclePatternGenerator.Generate | ConversionDifficulty > 6.5 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(LowProbability) | FALSE
        [Test]
        public void TestHighDifficultyAlwaysProducesAtLeastTwoNotes()
        {
            for (int seed = 0; seed < 20; seed++)
                Assert.That(generate(1000, 7, density: 100, beatmap: PatternTestHelper.HighDifficulty(), seed: seed).pattern.HitObjects.Count(), Is.GreaterThanOrEqualTo(2));
        }

        // CF: HitCirclePatternGenerator.Generate | ConversionDifficulty > 6.5 | TRUE
        // CF: HitCirclePatternGenerator.Generate | convertType.HasFlag(LowProbability) | TRUE
        [Test]
        public void TestHighDifficultyLowProbabilityProducesValidPattern()
        {
            // low probability is set for fast-ish notes (<= 105ms); 8 columns so that ForceNotStack leaves room.
            for (int seed = 0; seed < 20; seed++)
            {
                var pattern = generate(100, 8, beatmap: PatternTestHelper.HighDifficulty(), seed: seed).pattern;
                Assert.That(pattern.HitObjects.Select(h => h.Column), Is.All.InRange(0, 7));
            }
        }

        // CF: HitCirclePatternGenerator.Generate | ConversionDifficulty > 4 / > 2 (middle tiers) | infeasible: tiers depend on the RNG-free formula but the resulting note counts are random (p between 0 and 1), so no intent-derived expectation exists beyond range validity
        // CF: HitCirclePatternGenerator.getRandomNoteCount | switch(TotalColumns) case 2/3/4/5 | infeasible: p-caps only change probabilities of random counts; asserting them would copy the RNG output
        // note: 4K caps p2 at 0.23, so "always >= 2 notes" is only asserted for 7K
        // CF: HitCirclePatternGenerator.getRandomNoteCountMirrored | switch(TotalColumns) cases | infeasible: same reason (random counts)
    }
}
