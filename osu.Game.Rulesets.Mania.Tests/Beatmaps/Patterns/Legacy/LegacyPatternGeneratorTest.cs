// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns.Legacy;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps.Patterns.Legacy
{
    [TestFixture]
    public class LegacyPatternGeneratorTest
    {
        private class Exposed : LegacyPatternGenerator
        {
            public Exposed(int columns, IBeatmap? beatmap = null, LegacyRandom? random = null, HitObject? hitObject = null, Pattern? previous = null)
                : base(random ?? new LegacyRandom(1), hitObject ?? new Note(), beatmap ?? PatternTestHelper.LowDifficulty(), previous ?? new Pattern(), columns)
            {
            }

            public override IEnumerable<Pattern> Generate() => [];

            public int RandomStartValue => RandomStart;
            public int Column(float x, bool special = false) => GetColumn(x, special);
            public int NoteCount(double p2, double p3, double p4 = 0, double p5 = 0, double p6 = 0) => GetRandomNoteCount(p2, p3, p4, p5, p6);
            public double Difficulty => ConversionDifficulty;
            public int Available(int initial, int? lower = null, int? upper = null, Func<int, bool>? validation = null, params Pattern[] patterns)
                => FindAvailableColumn(initial, lower, upper, null, validation, patterns);
            public int RandomColumn(int? lower = null, int? upper = null) => GetRandomColumn(lower, upper);
        }

        // CF: PatternGenerator..ctor | hitObject == null (ThrowIfNull) | throws ArgumentNullException
        // CF: PatternGenerator..ctor | beatmap == null (ThrowIfNull) | throws ArgumentNullException
        // CF: PatternGenerator..ctor | previousPattern == null (ThrowIfNull) | throws ArgumentNullException
        // CF: LegacyPatternGenerator..ctor | random == null (ThrowIfNull) | throws ArgumentNullException
        [Test]
        public void TestConstructorRejectsNulls()
        {
            var b = PatternTestHelper.LowDifficulty();
            Assert.Throws<ArgumentNullException>(() => new PassThroughPatternGenerator(new LegacyRandom(1), null!, b, 4, new Pattern()));
            Assert.Throws<ArgumentNullException>(() => new PassThroughPatternGenerator(new LegacyRandom(1), new Note(), null!, 4, new Pattern()));
            Assert.Throws<ArgumentNullException>(() => new PassThroughPatternGenerator(new LegacyRandom(1), new Note(), b, 4, null!));
            Assert.Throws<ArgumentNullException>(() => new PassThroughPatternGenerator(null!, new Note(), b, 4, new Pattern()));
        }

        // CF: LegacyPatternGenerator..ctor | TotalColumns == 8 | TRUE
        // CF: LegacyPatternGenerator..ctor | TotalColumns == 8 | FALSE
        [Test]
        public void TestRandomStartOnlyForEightColumns()
        {
            Assert.That(new Exposed(8).RandomStartValue, Is.EqualTo(1));
            Assert.That(new Exposed(7).RandomStartValue, Is.EqualTo(0));
        }

        // CF: LegacyPatternGenerator.GetColumn | allowSpecial && TotalColumns == 8 | TRUE
        // CF: LegacyPatternGenerator.GetColumn | allowSpecial | FALSE
        // CF: LegacyPatternGenerator.GetColumn | TotalColumns == 8 | FALSE
        [TestCase(8, true, 0f, 1)]
        [TestCase(8, true, 511f, 7)]
        [TestCase(8, true, -50f, 1)]
        [TestCase(8, true, 9999f, 7)]
        [TestCase(8, false, 0f, 0)]
        [TestCase(8, false, 511f, 7)]
        [TestCase(4, true, 300f, 2)]
        [TestCase(4, false, -1f, 0)]
        [TestCase(4, false, 600f, 3)]
        public void TestGetColumn(int columns, bool special, float x, int expected)
        {
            Assert.That(new Exposed(columns).Column(x, special), Is.EqualTo(expected));
        }

        // CF: LegacyPatternGenerator.GetRandomNoteCount | p2 < 0 || p2 > 1 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | p3 < 0 || p3 > 1 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | p4 < 0 || p4 > 1 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | p5 < 0 || p5 > 1 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | p6 < 0 || p6 > 1 | TRUE
        [Test]
        public void TestProbabilitiesOutOfRangeThrow()
        {
            var g = new Exposed(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => g.NoteCount(-0.1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.NoteCount(1.1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.NoteCount(0, 1.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.NoteCount(0, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.NoteCount(0, 0, 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.NoteCount(0, 0, 0, 0, 1.5));
        }

        // CF: LegacyPatternGenerator.GetRandomNoteCount | p2..p6 in range | FALSE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | val >= 1 - p6 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | val >= 1 - p5 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | val >= 1 - p4 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | val >= 1 - p3 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | val >= 1 - p2 | TRUE
        // CF: LegacyPatternGenerator.GetRandomNoteCount | val >= 1 - p2 | FALSE
        [Test]
        public void TestCertainProbabilitiesPickThatCount()
        {
            var g = new Exposed(8);
            Assert.That(g.NoteCount(0, 0), Is.EqualTo(1));
            Assert.That(g.NoteCount(1, 0), Is.EqualTo(2));
            Assert.That(g.NoteCount(1, 1), Is.EqualTo(3));
            Assert.That(g.NoteCount(1, 1, 1), Is.EqualTo(4));
            Assert.That(g.NoteCount(1, 1, 1, 1), Is.EqualTo(5));
            Assert.That(g.NoteCount(1, 1, 1, 1, 1), Is.EqualTo(6));
        }

        // CF: LegacyPatternGenerator.ConversionDifficulty | conversionDifficulty != null | FALSE
        // CF: LegacyPatternGenerator.ConversionDifficulty | drainTime == 0 | TRUE
        // CF: LegacyPatternGenerator.ConversionDifficulty | Math.Min(conversionDifficulty, 12) | non-null
        [Test]
        public void TestConversionDifficultyUsesDefaultDrainTimeForShortMaps()
        {
            var b = new Beatmap<HitObject> { Difficulty = new BeatmapDifficulty { DrainRate = 5, ApproachRate = 5 }, HitObjects = { new Note { StartTime = 0 } } };
            double expected = ((5 + 5) / 1.5 + 1.0 / 10000 * 9) / 38 * 5 / 1.15;

            Assert.That(new Exposed(4, b).Difficulty, Is.EqualTo(expected).Within(1e-9));
        }

        // CF: LegacyPatternGenerator.ConversionDifficulty | drainTime == 0 | FALSE
        // CF: LegacyPatternGenerator.ConversionDifficulty | Math.Clamp(ApproachRate, 4, 7) | non-null
        [Test]
        public void TestConversionDifficultyClampsApproachRateAndSubtractsBreaks()
        {
            var b = new Beatmap<HitObject>
            {
                Difficulty = new BeatmapDifficulty { DrainRate = 0, ApproachRate = 10 },
                HitObjects = { new Note { StartTime = 0 }, new Note { StartTime = 100_000 } },
            };
            b.Breaks.Add(new osu.Game.Beatmaps.Timing.BreakPeriod(10_000, 40_000)); // 30s break -> drain time 70s

            double expected = (7 / 1.5 + 2.0 / 70 * 9) / 38 * 5 / 1.15;
            Assert.That(new Exposed(4, b).Difficulty, Is.EqualTo(expected).Within(1e-9));
        }

        // CF: LegacyPatternGenerator.ConversionDifficulty | conversionDifficulty != null | TRUE
        [Test]
        public void TestConversionDifficultyIsCappedAtTwelveAndCached()
        {
            var g = new Exposed(4, PatternTestHelper.HighDifficulty());
            Assert.That(g.Difficulty, Is.EqualTo(12));
            Assert.That(g.Difficulty, Is.EqualTo(12));
        }

        // CF: LegacyPatternGenerator.FindAvailableColumn | isValid(initialColumn) | TRUE
        // CF: LegacyPatternGenerator.FindAvailableColumn | validation?.Invoke(column) == false | FALSE
        // CF: LegacyPatternGenerator.FindAvailableColumn | foreach p in patterns | loop 0
        [Test]
        public void TestFreeInitialColumnIsReturned() => Assert.That(new Exposed(4).Available(2), Is.EqualTo(2));

        // CF: LegacyPatternGenerator.FindAvailableColumn | isValid(initialColumn) | FALSE
        // CF: LegacyPatternGenerator.FindAvailableColumn | hasValidColumns | TRUE
        // CF: LegacyPatternGenerator.FindAvailableColumn | foreach p in patterns | loop n
        // CF: LegacyPatternGenerator.FindAvailableColumn | p.ColumnHasObject(column) | TRUE
        // CF: LegacyPatternGenerator.FindAvailableColumn | p.ColumnHasObject(column) | FALSE
        // CF: LegacyPatternGenerator.FindAvailableColumn | do-while !isValid(next) | loop n
        [Test]
        public void TestOccupiedInitialColumnMovesToFreeOne()
        {
            var taken = PatternTestHelper.PatternOf(0, 1, 2);
            for (int i = 0; i < 20; i++)
                Assert.That(new Exposed(4, random: new LegacyRandom(i)).Available(1, patterns: taken), Is.EqualTo(3));
        }

        // CF: LegacyPatternGenerator.FindAvailableColumn | hasValidColumns | FALSE
        // CF: LegacyPatternGenerator.FindAvailableColumn | hasValidColumns | throws NotEnoughColumnsException
        [Test]
        public void TestNoFreeColumnThrows()
        {
            var taken = PatternTestHelper.PatternOf(0, 1, 2, 3);
            Assert.Throws<LegacyPatternGenerator.NotEnoughColumnsException>(() => new Exposed(4).Available(1, patterns: taken));
        }

        // CF: LegacyPatternGenerator.FindAvailableColumn | validation?.Invoke(column) == false | TRUE
        [Test]
        public void TestValidationRejectsColumns()
        {
            Assert.That(new Exposed(4).Available(0, validation: c => c == 2), Is.EqualTo(2));
        }

        // CF: LegacyPatternGenerator.FindAvailableColumn | lowerBound ??= RandomStart | TRUE
        // CF: LegacyPatternGenerator.FindAvailableColumn | upperBound ??= TotalColumns | TRUE
        // CF: LegacyPatternGenerator.GetRandomColumn | lowerBound ?? RandomStart | null
        // CF: LegacyPatternGenerator.GetRandomColumn | upperBound ?? TotalColumns | null
        [Test]
        public void TestRandomColumnDefaultBounds()
        {
            var g = new Exposed(8);
            for (int i = 0; i < 50; i++)
                Assert.That(g.RandomColumn(), Is.InRange(1, 7));
        }

        // CF: LegacyPatternGenerator.GetRandomColumn | lowerBound ?? RandomStart | non-null
        // CF: LegacyPatternGenerator.GetRandomColumn | upperBound ?? TotalColumns | non-null
        [Test]
        public void TestRandomColumnExplicitBounds()
        {
            var g = new Exposed(8);
            for (int i = 0; i < 50; i++)
                Assert.That(g.RandomColumn(2, 4), Is.InRange(2, 3));
        }
    }
}
