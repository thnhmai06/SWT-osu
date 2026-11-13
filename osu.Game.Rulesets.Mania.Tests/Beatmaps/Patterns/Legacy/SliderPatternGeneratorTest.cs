// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns.Legacy;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps.Patterns.Legacy
{
    /// <summary>
    /// With beat length 1000, slider multiplier 1 and a linear path of length d, a slider with s spans lasts d * 10 * s ms,
    /// i.e. every span lasts d * 10 ms. The tests pick d to select the segment-duration tier they want to reach.
    /// </summary>
    [TestFixture]
    public class SliderPatternGeneratorTest
    {
        private const double start = 1000;

        private static (Pattern[] patterns, SliderPatternGenerator generator) generate(double segmentDuration, int spans, int columns = 4, Pattern? previous = null,
                                                                                       IBeatmap? beatmap = null, int seed = 1, float x = 100)
        {
            var slider = new FakeSlider(segmentDuration / 10, spans) { StartTime = start, X = x };
            var generator = new SliderPatternGenerator(new LegacyRandom(seed), slider, beatmap ?? PatternTestHelper.LowDifficulty(), columns, previous ?? new Pattern());
            return (generator.Generate().ToArray(), generator);
        }

        private static ManiaHitObject[] all(Pattern[] patterns) => patterns.SelectMany(p => p.HitObjects).OrderBy(h => h.StartTime).ToArray();

        // CF: SliderPatternGenerator..ctor | repeatsData?.SpanCount() ?? 1 | non-null
        // CF: SliderPatternGenerator..ctor | pathData.Path.ExpectedDistance.Value ?? 0 | non-null
        // CF: SliderPatternGenerator..ctor | hitObject is IHasSliderVelocity | FALSE
        [Test]
        public void TestTimingDerivedFromPathLength()
        {
            var (_, g) = generate(100, 3);

            Assert.That(g.SpanCount, Is.EqualTo(3));
            Assert.That(g.StartTime, Is.EqualTo(1000));
            Assert.That(g.EndTime, Is.EqualTo(1300));
            Assert.That(g.SegmentDuration, Is.EqualTo(100));
        }

        // CF: SliderPatternGenerator.Generate | TotalColumns == 1 | TRUE
        // CF: SliderPatternGenerator.Generate | originalPattern.HitObjects.Count() == 1 | TRUE
        [Test]
        public void TestSingleColumnProducesOneHold()
        {
            var (patterns, g) = generate(100, 2, columns: 1);

            Assert.That(patterns, Has.Length.EqualTo(1));
            var hold = (HoldNote)patterns[0].HitObjects.Single();
            Assert.That(hold.Column, Is.EqualTo(0));
            Assert.That(hold.StartTime, Is.EqualTo(1000));
            Assert.That(hold.EndTime, Is.EqualTo(g.EndTime));
        }

        // CF: SliderPatternGenerator.Generate | TotalColumns == 1 | FALSE
        // CF: SliderPatternGenerator.generate | SpanCount > 1 | TRUE
        // CF: SliderPatternGenerator.generate | SegmentDuration <= 90 | TRUE
        // CF: SliderPatternGenerator.generateRandomHoldNotes | for i < Math.Min(usableColumns, noteCount) | loop 1
        // CF: SliderPatternGenerator.generateRandomHoldNotes | for i < noteCount - usableColumns | loop 0
        [Test]
        public void TestVeryShortRepeatingSliderIsOneHoldNote()
        {
            var (patterns, g) = generate(80, 2);

            Assert.That(patterns, Has.Length.EqualTo(1));
            var hold = (HoldNote)patterns[0].HitObjects.Single();
            Assert.That(hold.StartTime, Is.EqualTo(1000));
            Assert.That(hold.EndTime, Is.EqualTo(g.EndTime));
        }

        // CF: SliderPatternGenerator.generateRandomHoldNotes | for i < noteCount - usableColumns | loop 1
        [Test]
        public void TestHoldNoteReusesColumnWhenPreviousPatternFillsEverything()
        {
            var (patterns, _) = generate(80, 2, previous: PatternTestHelper.PatternOf(0, 1, 2, 3));
            Assert.That(all(patterns), Has.Length.EqualTo(1));
        }

        // CF: SliderPatternGenerator.generate | SegmentDuration <= 90 | FALSE
        // CF: SliderPatternGenerator.generate | SegmentDuration <= 120 | TRUE
        // CF: SliderPatternGenerator.generateRandomNotes | ForceNotStack && PreviousPattern.ColumnWithObjects < TotalColumns | TRUE
        // CF: SliderPatternGenerator.generateRandomNotes | for i < noteCount | loop n
        // CF: SliderPatternGenerator.Generate | originalPattern.HitObjects.Count() == 1 | FALSE
        // CF: SliderPatternGenerator.Generate | EndTime != (int)Math.Round(obj.GetEndTime()) | TRUE
        // CF: SliderPatternGenerator.Generate | EndTime != (int)Math.Round(obj.GetEndTime()) | FALSE
        [Test]
        public void TestShortRepeatsBecomeAlternatingNotesOnEverySpanBoundary()
        {
            var (patterns, g) = generate(100, 2, previous: PatternTestHelper.PatternOf(0));
            var notes = all(patterns);

            Assert.That(notes, Has.Length.EqualTo(3));
            Assert.That(notes, Is.All.TypeOf<Note>());
            Assert.That(notes.Select(n => n.StartTime), Is.EqualTo(new[] { 1000.0, 1100.0, 1200.0 }));
            Assert.That(notes[0].Column, Is.Not.EqualTo(0), "must not stack on the previous pattern");
            Assert.That(notes[1].Column, Is.Not.EqualTo(notes[0].Column));
            Assert.That(notes[2].Column, Is.Not.EqualTo(notes[1].Column));

            // objects not ending at the slider end come first, the rest form the pattern used for further generation
            Assert.That(patterns, Has.Length.EqualTo(2));
            Assert.That(patterns[0].HitObjects.Count(), Is.EqualTo(2));
            Assert.That(patterns[1].HitObjects.Single().StartTime, Is.EqualTo(g.EndTime));
        }

        // CF: SliderPatternGenerator.generate | SegmentDuration <= 120 | FALSE
        // CF: SliderPatternGenerator.generate | SegmentDuration <= 160 | TRUE
        // CF: SliderPatternGenerator.generateStair | for i <= SpanCount | loop n
        // CF: SliderPatternGenerator.generateStair | increasing | TRUE
        // CF: SliderPatternGenerator.generateStair | increasing | FALSE
        // CF: SliderPatternGenerator.generateStair | column >= TotalColumns - 1 | TRUE
        // CF: SliderPatternGenerator.generateStair | column <= RandomStart | TRUE
        [Test]
        public void TestMediumRepeatsFormAStairThatBouncesOffTheBorders()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var (patterns, _) = generate(140, 6, seed: seed, x: 0);
                var columns = all(patterns).Select(n => n.Column).ToArray();

                Assert.That(columns, Has.Length.EqualTo(7));
                Assert.That(columns, Is.All.InRange(0, 3));
                for (int i = 1; i < columns.Length; i++)
                    Assert.That(System.Math.Abs(columns[i] - columns[i - 1]), Is.EqualTo(1));
            }
        }

        // CF: SliderPatternGenerator.generateStair | column >= TotalColumns - 1 | FALSE
        // CF: SliderPatternGenerator.generateStair | column <= RandomStart | FALSE
        [Test]
        public void TestStairStartsAtColumnOfX()
        {
            var (patterns, _) = generate(140, 2, x: 200); // 200 / 128 -> column 1
            Assert.That(all(patterns).First().Column, Is.EqualTo(1));
        }

        // CF: SliderPatternGenerator.generate | SegmentDuration <= 160 | FALSE
        // CF: SliderPatternGenerator.generate | SegmentDuration <= 200 && ConversionDifficulty > 3 | TRUE
        // CF: SliderPatternGenerator.generateRandomMultipleNotes | for i <= SpanCount | loop n
        [Test]
        public void TestHighDifficultyRepeatsAddNotesOnEveryNode()
        {
            var (patterns, _) = generate(180, 2, beatmap: PatternTestHelper.HighDifficulty());
            var times = all(patterns).Select(n => n.StartTime).Distinct().ToArray();

            Assert.That(times, Is.EqualTo(new[] { 1000.0, 1180.0, 1360.0 }));
            Assert.That(all(patterns), Is.All.TypeOf<Note>());
        }

        // CF: SliderPatternGenerator.generate | ConversionDifficulty > 3 | FALSE
        // CF: SliderPatternGenerator.generate | duration >= 4000 | TRUE
        // CF: SliderPatternGenerator.generateNRandomNotes | generateRandomHoldNotes | non-null
        [Test]
        public void TestVeryLongRepeatingSliderBecomesHoldNotes()
        {
            var (patterns, g) = generate(2500, 2);
            var objects = all(patterns);

            Assert.That(objects, Is.Not.Empty);
            Assert.That(objects, Is.All.TypeOf<HoldNote>());
            Assert.That(objects.Select(o => o.StartTime), Is.All.EqualTo(1000));
            Assert.That(objects.Cast<HoldNote>().Select(h => h.EndTime), Is.All.EqualTo(g.EndTime));
        }

        // CF: SliderPatternGenerator.generate | duration >= 4000 | FALSE
        // CF: SliderPatternGenerator.generate | SegmentDuration > 400 && SpanCount < TotalColumns - 1 - RandomStart | TRUE
        // CF: SliderPatternGenerator.generateTiledHoldNotes | for i < columnRepeat | loop n
        [Test]
        public void TestLongSegmentsAreTiledAcrossColumns()
        {
            var (patterns, g) = generate(500, 2);
            var holds = all(patterns).Cast<HoldNote>().ToArray();

            Assert.That(holds, Has.Length.EqualTo(2));
            Assert.That(holds.Select(h => h.StartTime), Is.EqualTo(new[] { 1000.0, 1500.0 }));
            Assert.That(holds.Select(h => h.EndTime), Is.All.EqualTo(g.EndTime));
            Assert.That(holds.Select(h => h.Column).Distinct().Count(), Is.EqualTo(2));
        }

        // CF: SliderPatternGenerator.generate | SegmentDuration > 400 | FALSE
        // CF: SliderPatternGenerator.generate | SpanCount < TotalColumns - 1 - RandomStart | FALSE
        // CF: SliderPatternGenerator.generateHoldAndNormalNotes | ConversionDifficulty > 2.5 | FALSE
        // CF: SliderPatternGenerator.generateHoldAndNormalNotes | for i <= SpanCount | loop n
        [TestCase(300, 2, 4)]
        [TestCase(500, 3, 4)]
        public void TestOtherwiseHoldNoteWithoutExtraNotesAtLowDifficulty(double segment, int spans, int columns)
        {
            var (patterns, g) = generate(segment, spans, columns);
            var hold = (HoldNote)all(patterns).Single();

            Assert.That(hold.StartTime, Is.EqualTo(1000));
            Assert.That(hold.EndTime, Is.EqualTo(g.EndTime));
        }

        // CF: SliderPatternGenerator.generateHoldAndNormalNotes | ConversionDifficulty > 6.5 | TRUE
        // CF: SliderPatternGenerator.generateHoldAndNormalNotes | ignoreHead | TRUE
        [Test]
        public void TestHighDifficultyHoldAndNormalNotesKeepsTheHold()
        {
            var (patterns, g) = generate(300, 2, beatmap: PatternTestHelper.HighDifficulty());
            Assert.That(all(patterns).OfType<HoldNote>().Count(), Is.EqualTo(1));
            Assert.That(all(patterns).OfType<HoldNote>().Single().EndTime, Is.EqualTo(g.EndTime));
        }

        // CF: SliderPatternGenerator.generate | SpanCount > 1 | FALSE
        // CF: SliderPatternGenerator.generate | SegmentDuration <= 110 | TRUE
        // CF: SliderPatternGenerator.generate | SegmentDuration < 80 | TRUE
        // CF: SliderPatternGenerator.generate | PreviousPattern.ColumnWithObjects < TotalColumns | TRUE
        [Test]
        public void TestVeryShortSingleSpanSliderIsOneNote()
        {
            var (patterns, _) = generate(70, 1);
            var n = all(patterns).Single();
            Assert.That(n, Is.TypeOf<Note>());
            Assert.That(n.StartTime, Is.EqualTo(1000));
        }

        // CF: SliderPatternGenerator.generate | SegmentDuration < 80 | FALSE
        // CF: SliderPatternGenerator.generate | PreviousPattern.ColumnWithObjects < TotalColumns | FALSE
        [Test]
        public void TestShortSingleSpanSliderIsTwoNotesEvenWhenPreviousPatternIsFull()
        {
            var (patterns, _) = generate(100, 1, previous: PatternTestHelper.PatternOf(0, 1, 2, 3));
            var notes = all(patterns);

            Assert.That(notes.Select(n => n.StartTime), Is.EqualTo(new[] { 1000.0, 1100.0 }));
            Assert.That(notes[1].Column, Is.Not.EqualTo(notes[0].Column));
        }

        // CF: SliderPatternGenerator.generate | SegmentDuration <= 110 | FALSE
        // CF: SliderPatternGenerator.generate | ConversionDifficulty > 6.5 | TRUE
        // CF: SliderPatternGenerator.generate | ConversionDifficulty > 6.5 | FALSE
        // CF: SliderPatternGenerator.generate | ConversionDifficulty > 4 | FALSE
        // CF: SliderPatternGenerator.generate | ConversionDifficulty > 2.5 | FALSE
        [Test]
        public void TestSingleSpanSliderAtLowAndHighDifficultyProducesHoldNotesOnly()
        {
            foreach (var beatmap in new[] { PatternTestHelper.LowDifficulty(), PatternTestHelper.HighDifficulty() })
            {
                var (patterns, g) = generate(500, 1, beatmap: beatmap);
                var objects = all(patterns);

                Assert.That(objects, Is.Not.Empty);
                Assert.That(objects, Is.All.TypeOf<HoldNote>());
                Assert.That(objects.Cast<HoldNote>().Select(h => h.EndTime), Is.All.EqualTo(g.EndTime));
            }
        }

        // CF: SliderPatternGenerator.generate | ConversionDifficulty > 4 / > 2.5 (middle tiers) | infeasible: needs a beatmap with difficulty in (2.5, 6.5]; counts are RNG-driven (probabilities < 1), no intent-derived expectation beyond hold-note validity
        // CF: SliderPatternGenerator.generateNRandomNotes | switch(TotalColumns) case 2/3/4/5 caps | infeasible: only alter probabilities of random counts
        // CF: SliderPatternGenerator..ctor | EffectPointAt(..).KiaiMode | infeasible: only toggles LowProbability, which affects probabilities of random counts and not asserted structure
        // CF: SliderPatternGenerator..ctor | hitObject is IHasSliderVelocity | infeasible: IHasSliderVelocity needs osu-ruleset-specific objects; precision-adjusted beat length is exercised by the osu! converter tests
        // CF: SliderPatternGenerator.nodeSamplesAt | HitObject is not IHasPathWithRepeats | infeasible: constructor asserts IHasPath, so generator always sees a path (IHasPathWithRepeats can still lack repeats only via a custom type)
    }
}
