// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Tests.Rulesets.Objects.Legacy
{
    [TestFixture]
    public class ConvertHitObjectParserTest
    {
        private CultureInfo previousCulture = null!;

        [SetUp]
        public void SetUp()
        {
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previousCulture;

        private static ConvertHitObjectParser parser(int version = 14, double offset = 0) => new ConvertHitObjectParser(offset, version);

        private static ConvertSlider slider(string line, int version = 14) => (ConvertSlider)parser(version).Parse(line);

        // CF: ConvertHitObjectParser.Parse | formatVersion >= FIRST_LAZER_VERSION | FALSE
        // CF: ConvertHitObjectParser.Parse | type.HasFlag(Circle) | TRUE
        // CF: ConvertHitObjectParser.Parse | split.Length > 5 | FALSE
        // CF: ConvertHitObjectParser.Parse | result == null | FALSE
        // CF: ConvertHitObjectParser.createHitCircle | firstObject | TRUE
        // CF: ConvertHitObjectParser.Parse | result.Samples.Count == 0 | TRUE
        [Test]
        public void TestCircleTruncatesPositionInLegacyFormat()
        {
            var circle = (ConvertHitCircle)parser().Parse("100.7,200.3,1000,1,0");

            Assert.That(circle.Position, Is.EqualTo(new Vector2(100, 200)));
            Assert.That(circle.StartTime, Is.EqualTo(1000));
            Assert.That(circle.NewCombo, Is.True, "the first object always starts a new combo");
            Assert.That(circle.Samples.Select(s => s.Name), Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL }));
        }

        // CF: ConvertHitObjectParser.Parse | formatVersion >= FIRST_LAZER_VERSION | TRUE
        [Test]
        public void TestCircleKeepsFractionalPositionInLazerFormat()
        {
            var parsed = (ConvertHitCircle)parser(128).Parse("100.5,200.25,1000,1,0");

            Assert.That(parsed.Position, Is.EqualTo(new Vector2(100.5f, 200.25f)));
        }

        // CF: ConvertHitObjectParser.Parse | startTime offset applied | no throw
        [Test]
        public void TestOffsetAppliedToStartTime()
        {
            Assert.That(parser(14, 24).Parse("0,0,1000,1,0").StartTime, Is.EqualTo(1024));
        }

        // CF: ConvertHitObjectParser.createHitCircle | firstObject | FALSE
        // CF: ConvertHitObjectParser.createHitCircle | lastObject is ConvertSpinner | FALSE
        // CF: ConvertHitObjectParser.createHitCircle | newCombo | TRUE
        // CF: ConvertHitObjectParser.createHitCircle | newCombo ? comboOffset : 0 | TRUE
        [Test]
        public void TestNewComboAndComboOffset()
        {
            var p = parser();
            p.Parse("0,0,0,1,0");

            var circle = (ConvertHitCircle)p.Parse($"0,0,100,{1 | 4 | (2 << 4)},0");

            Assert.That(circle.NewCombo, Is.True);
            Assert.That(circle.ComboOffset, Is.EqualTo(2));
        }

        // CF: ConvertHitObjectParser.createHitCircle | newCombo | FALSE
        // CF: ConvertHitObjectParser.createHitCircle | newCombo ? comboOffset : 0 | FALSE
        [Test]
        public void TestComboOffsetIgnoredWithoutNewCombo()
        {
            var p = parser();
            p.Parse("0,0,0,1,0");

            var circle = (ConvertHitCircle)p.Parse($"0,0,100,{1 | (2 << 4)},0");

            Assert.That(circle.NewCombo, Is.False);
            Assert.That(circle.ComboOffset, Is.Zero);
        }

        // CF: ConvertHitObjectParser.createHitCircle | lastObject is ConvertSpinner | TRUE
        // CF: ConvertHitObjectParser.Parse | type.HasFlag(Spinner) | TRUE
        // CF: ConvertHitObjectParser.Parse | type.HasFlag(Circle) | FALSE
        // CF: ConvertHitObjectParser.Parse | type.HasFlag(Slider) | FALSE
        // CF: ConvertHitObjectParser.Parse | split.Length > 6 | FALSE
        [Test]
        public void TestObjectAfterSpinnerStartsNewCombo()
        {
            var p = parser();
            var spinner = (ConvertSpinner)p.Parse("256,192,1000,8,0,3000");
            var circle = (ConvertHitCircle)p.Parse("0,0,4000,1,0");

            Assert.That(spinner.Duration, Is.EqualTo(2000));
            Assert.That(spinner.Position, Is.EqualTo(new Vector2(256, 192)));
            Assert.That(spinner.NewCombo, Is.False, "spinners only take the explicit new-combo flag");
            Assert.That(circle.NewCombo, Is.True);
        }

        // CF: ConvertHitObjectParser.Parse | split.Length > 6 | TRUE
        [Test]
        public void TestSpinnerWithEndBeforeStartHasZeroDuration()
        {
            var spinner = (ConvertSpinner)parser().Parse("0,0,1000,8,0,500,1:2:0:40:");

            Assert.That(spinner.Duration, Is.Zero);
            Assert.That(spinner.Samples[0].Volume, Is.EqualTo(40));
        }

        // CF: ConvertHitObjectParser.Parse | type.HasFlag(Hold) | TRUE
        // CF: ConvertHitObjectParser.Parse | split.Length > 5 && !string.IsNullOrEmpty(split[5]) | TRUE
        [Test]
        public void TestHold()
        {
            var hold = (ConvertHold)parser().Parse("0,0,1000,128,0,3000:1:0:0:30:");

            Assert.That(hold.Duration, Is.EqualTo(2000));
            Assert.That(hold.Samples[0].Volume, Is.EqualTo(30));
        }

        // CF: ConvertHitObjectParser.Parse | split.Length > 5 | FALSE
        // CF: ConvertHitObjectParser.Parse | string.IsNullOrEmpty(split[5]) | TRUE
        [Test]
        public void TestHoldWithoutEndHasZeroDuration()
        {
            Assert.That(((ConvertHold)parser().Parse("0,0,1000,128,0")).Duration, Is.Zero);
            Assert.That(((ConvertHold)parser().Parse("0,0,1000,128,0,")).Duration, Is.Zero);
        }

        // CF: ConvertHitObjectParser.Parse | result == null | throws InvalidDataException
        // CF: ConvertHitObjectParser.Parse | type.HasFlag(Hold) | FALSE
        [Test]
        public void TestUnknownTypeThrows()
        {
            Assert.Throws<InvalidDataException>(() => parser().Parse("0,0,1000,0,0"));
            Assert.Throws<InvalidDataException>(() => parser().Parse($"0,0,1000,{2 << 4},0"));
        }

        // CF: ConvertHitObjectParser.Parse | type.HasFlag(Slider) | TRUE
        // CF: ConvertHitObjectParser.Parse | repeatCount > 9000 | FALSE
        // CF: ConvertHitObjectParser.Parse | split.Length > 7 | TRUE
        // CF: ConvertHitObjectParser.Parse | length == 0 | FALSE
        // CF: ConvertHitObjectParser.Parse | split.Length > 10 | FALSE
        // CF: ConvertHitObjectParser.Parse | split.Length > 9 && split[9].Length > 0 | FALSE
        // CF: ConvertHitObjectParser.Parse | split.Length > 8 && split[8].Length > 0 | FALSE
        // CF: ConvertHitObjectParser.Parse | for nodes | loop n
        [Test]
        public void TestSimpleSlider()
        {
            var s = slider("0,0,1000,2,0,L|100:0,1,100");

            Assert.That(s.RepeatCount, Is.Zero, "stable counts the first span as a repeat");
            Assert.That(s.NodeSamples.Count, Is.EqualTo(2));
            Assert.That(s.Path.ExpectedDistance.Value, Is.EqualTo(100));
            Assert.That(s.Path.ControlPoints.Count, Is.EqualTo(2));
            Assert.That(s.Path.ControlPoints[0].Type, Is.EqualTo(PathType.LINEAR));
            Assert.That(s.Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(100, 0)));
        }

        // CF: ConvertHitObjectParser.Parse | repeatCount > 9000 | throws FormatException
        [Test]
        public void TestExcessiveRepeatCountThrows()
        {
            Assert.Throws<FormatException>(() => slider("0,0,1000,2,0,L|100:0,9001,100"));
        }

        // CF: ConvertHitObjectParser.Parse | split.Length > 7 | FALSE
        // CF: ConvertHitObjectParser.Parse | length == 0 | TRUE
        [Test]
        public void TestSliderWithMissingOrZeroLengthHasNoExpectedDistance()
        {
            Assert.That(slider("0,0,1000,2,0,L|100:0,1").Path.ExpectedDistance.Value, Is.Null);
            Assert.That(slider("0,0,1000,2,0,L|100:0,1,0").Path.ExpectedDistance.Value, Is.Null);
            Assert.That(slider("0,0,1000,2,0,L|100:0,1,-5").Path.ExpectedDistance.Value, Is.Null);
        }

        // CF: ConvertHitObjectParser.Parse | for nodes (repeats) | loop n
        [Test]
        public void TestRepeatsCreateExtraNodes()
        {
            var s = slider("0,0,1000,2,0,L|100:0,3,100");

            Assert.That(s.RepeatCount, Is.EqualTo(2));
            Assert.That(s.NodeSamples.Count, Is.EqualTo(4));
        }

        // CF: ConvertHitObjectParser.Parse | split.Length > 8 && split[8].Length > 0 | TRUE
        // CF: ConvertHitObjectParser.Parse | i >= adds.Length | FALSE
        // CF: ConvertHitObjectParser.Parse | i >= adds.Length | TRUE
        [Test]
        public void TestPerNodeSoundTypes()
        {
            // edge sounds: whistle, finish; the third node falls back to the object's own sound type (clap = 8).
            var s = slider("0,0,1000,2,8,L|100:0,3,100,2|4");

            Assert.That(s.NodeSamples[0].Select(x => x.Name), Is.EqualTo(new[] { "hitnormal", "hitwhistle" }));
            Assert.That(s.NodeSamples[1].Select(x => x.Name), Is.EqualTo(new[] { "hitnormal", "hitfinish" }));
            Assert.That(s.NodeSamples[2].Select(x => x.Name), Is.EqualTo(new[] { "hitnormal", "hitclap" }));
        }

        // CF: ConvertHitObjectParser.Parse | split.Length > 9 && split[9].Length > 0 | TRUE
        // CF: ConvertHitObjectParser.Parse | i >= sets.Length | TRUE
        // CF: ConvertHitObjectParser.Parse | i >= sets.Length | FALSE
        [Test]
        public void TestPerNodeSampleBanks()
        {
            // 3 nodes but only two edge sets given: the last node keeps the default bank.
            var s = slider("0,0,1000,2,0,L|100:0,2,100,0|0,2:0|3:0");

            Assert.That(s.NodeSamples.Count, Is.EqualTo(3));
            Assert.That(s.NodeSamples[0][0].Bank, Is.EqualTo("soft"));
            Assert.That(s.NodeSamples[1][0].Bank, Is.EqualTo("drum"));
            Assert.That(s.NodeSamples[2][0].Bank, Is.EqualTo("normal"));
        }

        // CF: ConvertHitObjectParser.Parse | split.Length > 10 | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | banksOnly | TRUE
        [Test]
        public void TestSliderHitSampleOnlyReadsBanks()
        {
            var s = slider("0,0,1000,2,0,L|100:0,1,100,0,0:0,3:2:5:70:");

            Assert.That(s.Samples[0].Bank, Is.EqualTo("drum"));
            Assert.That(s.Samples[0].Volume, Is.Zero, "volume is not read from slider hit samples");
        }

        // CF: ConvertHitObjectParser.createSlider | Precision.AlmostEquals(path.Distance, 0) | TRUE
        // CF: ConvertHitObjectParser.createSlider | Precision.AlmostEquals(path.Distance, 0) | FALSE
        [Test]
        public void TestZeroLengthSliderDropsRepeats()
        {
            var s = slider("0,0,1000,2,0,L|0:0,5,0");

            Assert.That(s.RepeatCount, Is.Zero);
            Assert.That(s.NodeSamples.Count, Is.EqualTo(2));
        }

        // CF: ConvertHitObjectParser.convertPathType | case C | case C
        // CF: ConvertHitObjectParser.convertPathType | case B | case B
        // CF: ConvertHitObjectParser.convertPathType | case L | case L
        // CF: ConvertHitObjectParser.convertPathType | case P | case P
        // CF: ConvertHitObjectParser.convertPathType | default | default
        // CF: ConvertHitObjectParser.convertPathType | input.Length > 1 && TryParse && degree > 0 | TRUE
        // CF: ConvertHitObjectParser.convertPathType | input.Length > 1 | FALSE
        // CF: ConvertHitObjectParser.convertPathType | TryParse | FALSE
        // CF: ConvertHitObjectParser.convertPathType | degree > 0 | FALSE
        [TestCase("C", "Catmull")]
        [TestCase("X", "Catmull")]
        [TestCase("L", "Linear")]
        [TestCase("B", "BSpline")]
        [TestCase("Bx", "BSpline")]
        [TestCase("B0", "BSpline")]
        [TestCase("B3", "BSpline")]
        [TestCase("P", "PerfectCurve")]
        public void TestPathTypeLetters(string letter, string expectedSplineType)
        {
            var s = slider($"0,0,1000,2,0,{letter}|50:50|100:0,1,100");

            Assert.That(s.Path.ControlPoints[0].Type!.Value.Type.ToString(), Is.EqualTo(expectedSplineType));
        }

        // CF: ConvertHitObjectParser.convertPathType | degree > 0 | TRUE
        [Test]
        public void TestBSplineDegreeIsParsed()
        {
            Assert.That(slider("0,0,1000,2,0,B3|50:50|100:0|150:0,1,100").Path.ControlPoints[0].Type, Is.EqualTo(PathType.BSpline(3)));
            Assert.That(slider("0,0,1000,2,0,B0|50:50|100:0,1,100").Path.ControlPoints[0].Type, Is.EqualTo(PathType.BEZIER));
        }

        // CF: ConvertHitObjectParser.convertPoints | type == PERFECT_CURVE | TRUE
        // CF: ConvertHitObjectParser.convertPoints | formatVersion < FIRST_LAZER_VERSION | TRUE
        // CF: ConvertHitObjectParser.convertPoints | vertices.Length + endPointLength != 3 | FALSE
        // CF: ConvertHitObjectParser.convertPoints | isLinear | FALSE
        // CF: ConvertHitObjectParser.convertPoints | endPoint == null | TRUE
        [Test]
        public void TestPerfectCurveWithThreePointsKept()
        {
            Assert.That(slider("0,0,1000,2,0,P|50:50|100:0,1,100").Path.ControlPoints[0].Type, Is.EqualTo(PathType.PERFECT_CURVE));
        }

        // CF: ConvertHitObjectParser.convertPoints | vertices.Length + endPointLength != 3 | TRUE
        [Test]
        public void TestPerfectCurveWithWrongPointCountBecomesBezierInLegacyFormat()
        {
            Assert.That(slider("0,0,1000,2,0,P|50:50|100:0|150:0,1,100").Path.ControlPoints[0].Type, Is.EqualTo(PathType.BEZIER));
            Assert.That(slider("0,0,1000,2,0,P|100:0,1,100").Path.ControlPoints[0].Type, Is.EqualTo(PathType.BEZIER));
        }

        // CF: ConvertHitObjectParser.convertPoints | isLinear | TRUE
        [Test]
        public void TestColinearPerfectCurveBecomesLinearInLegacyFormat()
        {
            Assert.That(slider("0,0,1000,2,0,P|50:0|100:0,1,100").Path.ControlPoints[0].Type, Is.EqualTo(PathType.LINEAR));
        }

        // CF: ConvertHitObjectParser.convertPoints | endPoint == null | FALSE
        [Test]
        public void TestPerfectCurveSegmentUsesFollowingSegmentStartAsEndPoint()
        {
            var cps = slider("0,0,1000,2,0,P|50:50|L|100:0,1,100").Path.ControlPoints;
            Assert.That(cps[0].Type, Is.EqualTo(PathType.PERFECT_CURVE));
            Assert.That(cps.Count, Is.EqualTo(3));

            var colinear = slider("0,0,1000,2,0,P|50:0|L|100:0,1,100").Path.ControlPoints;
            Assert.That(colinear[0].Type, Is.EqualTo(PathType.LINEAR));
        }

        // CF: ConvertHitObjectParser.convertPoints | vertices.Length + endPointLength > 3 | TRUE
        // CF: ConvertHitObjectParser.convertPoints | vertices.Length + endPointLength > 3 | FALSE
        [Test]
        public void TestLazerPerfectCurveRules()
        {
            Assert.That(slider("0,0,1000,2,0,P|100:0,1,100", 128).Path.ControlPoints[0].Type, Is.EqualTo(PathType.PERFECT_CURVE), "fewer than 3 points allowed");
            Assert.That(slider("0,0,1000,2,0,P|50:0|100:0,1,100", 128).Path.ControlPoints[0].Type, Is.EqualTo(PathType.PERFECT_CURVE), "colinear allowed");
            Assert.That(slider("0,0,1000,2,0,P|50:50|100:0|150:0,1,100", 128).Path.ControlPoints[0].Type, Is.EqualTo(PathType.BEZIER));
        }

        // CF: ConvertHitObjectParser.convertPoints | Position != previous Position | TRUE
        // CF: ConvertHitObjectParser.convertPoints | type == CATMULL && endIndex > 1 && legacy | FALSE
        // CF: ConvertHitObjectParser.convertPoints | endIndex == vertices.Length - 1 | FALSE
        // CF: ConvertHitObjectParser.convertPoints | startIndex < endIndex | TRUE
        [Test]
        public void TestDuplicatePointStartsImplicitSegment()
        {
            var cps = slider("0,0,1000,2,0,L|50:0|50:0|100:0,1,100").Path.ControlPoints;

            Assert.That(cps.Select(c => c.Type), Is.EqualTo(new PathType?[] { PathType.LINEAR, PathType.LINEAR, null }));
            Assert.That(cps.Select(c => c.Position.X), Is.EqualTo(new[] { 0f, 50f, 100f }));
        }

        // CF: ConvertHitObjectParser.convertPoints | type == CATMULL && endIndex > 1 && legacy | TRUE
        // CF: ConvertHitObjectParser.convertPoints | endIndex == vertices.Length - 1 | TRUE
        [Test]
        public void TestLegacyCatmullDoesNotSplitAndLastPointNeverStartsSegment()
        {
            Assert.That(slider("0,0,1000,2,0,C|50:0|50:0|100:0,1,100").Path.ControlPoints.Count, Is.EqualTo(4));
            Assert.That(slider("0,0,1000,2,0,L|50:0|100:0|100:0,1,100").Path.ControlPoints.Count, Is.EqualTo(4));
            Assert.That(slider("0,0,1000,2,0,C|50:0|50:0|100:0,1,100", 128).Path.ControlPoints.Count, Is.EqualTo(3));
        }

        // CF: ConvertHitObjectParser.convertPathString | char.IsLetter(s[0]) | TRUE
        // CF: ConvertHitObjectParser.convertPathString | currentPointsIndex == 0 | TRUE
        // CF: ConvertHitObjectParser.convertPathString | char.IsLetter(s[0]) | FALSE
        // CF: ConvertHitObjectParser.convertPathString | currentPointsIndex == 0 | FALSE
        // CF: ConvertHitObjectParser.convertPathString | i < segmentsCount - 1 | TRUE
        // CF: ConvertHitObjectParser.convertPathString | i < segmentsCount - 1 | FALSE
        // CF: ConvertHitObjectParser.mergeControlPointsLists | foreach arr | loop n
        [Test]
        public void TestMultiSegmentPathPointsAreRelativeToObjectPosition()
        {
            var cps = slider("100,100,1000,2,0,L|150:100|B|200:150|250:100,1,100").Path.ControlPoints;

            Assert.That(cps.Select(c => c.Position), Is.EqualTo(new[] { new Vector2(0, 0), new Vector2(50, 0), new Vector2(100, 50), new Vector2(150, 0) }));
            Assert.That(cps.Select(c => c.Type), Is.EqualTo(new PathType?[] { PathType.LINEAR, null, PathType.BEZIER, null }));
        }

        // CF: ConvertHitObjectParser.Parse | formatVersion >= FIRST_LAZER_VERSION (path points) | TRUE
        [Test]
        public void TestLazerPathPointsKeepFraction()
        {
            Assert.That(slider("0,0,1000,2,0,L|50.5:0.5,1,100", 128).Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(50.5f, 0.5f)));
            Assert.That(slider("0,0,1000,2,0,L|50.5:0.5,1,100").Path.ControlPoints[1].Position, Is.EqualTo(new Vector2(50, 0)));
        }

        // CF: ConvertHitObjectParser.readCustomSampleBanks | string.IsNullOrEmpty(str) | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | string.IsNullOrEmpty(str) | FALSE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | !Enum.IsDefined(bank) | FALSE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | stringAddBank == none | FALSE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | stringBank == none | FALSE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | split.Length > 2 | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | split.Length > 3 | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | split.Length > 4 | FALSE
        // CF: ConvertHitObjectParser.convertSoundType | string.IsNullOrEmpty(bankInfo.Filename) | TRUE
        [Test]
        public void TestCircleSampleBankDetails()
        {
            // normal bank, soft additions bank, custom bank 3, volume 50. Whistle sound.
            var circle = parser().Parse("0,0,1000,1,2,1:2:3:50:");

            var normal = (ConvertHitObjectParser.LegacyHitSampleInfo)circle.Samples[0];
            var whistle = (ConvertHitObjectParser.LegacyHitSampleInfo)circle.Samples[1];

            Assert.That(normal.Name, Is.EqualTo("hitnormal"));
            Assert.That(normal.Bank, Is.EqualTo("normal"));
            Assert.That(normal.Volume, Is.EqualTo(50));
            Assert.That(normal.CustomSampleBank, Is.EqualTo(3));
            Assert.That(normal.IsLayered, Is.True, "no Normal flag set on a non-None sound type");

            Assert.That(whistle.Name, Is.EqualTo("hitwhistle"));
            Assert.That(whistle.Bank, Is.EqualTo("soft"));
            Assert.That(whistle.Volume, Is.EqualTo(50));
        }

        // CF: ConvertHitObjectParser.readCustomSampleBanks | !Enum.IsDefined(bank) | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | !Enum.IsDefined(addBank) | TRUE
        [Test]
        public void TestUndefinedBanksFallBackToNormal()
        {
            var circle = parser().Parse("0,0,1000,1,2,99:98:0:0:");

            Assert.That(circle.Samples[0].Bank, Is.EqualTo("normal"));
            Assert.That(circle.Samples[1].Bank, Is.EqualTo("normal"));
        }

        // CF: ConvertHitObjectParser.readCustomSampleBanks | stringBank == none | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | stringAddBank == none | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | string.IsNullOrEmpty(stringAddBank) | TRUE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | string.IsNullOrEmpty(stringAddBank) | FALSE
        [Test]
        public void TestNoneBanksUseDefaultBankAndAutoAdditions()
        {
            var circle = parser().Parse("0,0,1000,1,2,0:0:0:0:");

            var normal = (ConvertHitObjectParser.LegacyHitSampleInfo)circle.Samples[0];
            Assert.That(normal.Bank, Is.EqualTo("normal"));
            Assert.That(normal.BankSpecified, Is.False);
            Assert.That(circle.Samples[1].EditorAutoBank, Is.True);

            var withAdd = parser().Parse("0,0,1000,1,2,3:0:0:0:");
            Assert.That(withAdd.Samples[1].Bank, Is.EqualTo("drum"), "additions inherit the normal bank when add bank is None");
            Assert.That(withAdd.Samples[1].EditorAutoBank, Is.True);

            var explicitAdd = parser().Parse("0,0,1000,1,2,3:2:0:0:");
            Assert.That(explicitAdd.Samples[1].EditorAutoBank, Is.False);
        }

        // CF: ConvertHitObjectParser.readCustomSampleBanks | split.Length > 4 | TRUE
        // CF: ConvertHitObjectParser.convertSoundType | string.IsNullOrEmpty(bankInfo.Filename) | FALSE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | split.Length > 2 | FALSE
        // CF: ConvertHitObjectParser.readCustomSampleBanks | split.Length > 3 | FALSE
        [Test]
        public void TestCustomFilenameSample()
        {
            var circle = parser().Parse("0,0,1000,1,0,0:0:0:60:custom.wav");

            var file = (ConvertHitObjectParser.FileHitSampleInfo)circle.Samples.Single();
            Assert.That(file.Filename, Is.EqualTo("custom.wav"));
            Assert.That(file.Volume, Is.EqualTo(60));

            var shortForm = parser().Parse("0,0,1000,1,0,2:2");
            Assert.That(shortForm.Samples[0].Bank, Is.EqualTo("soft"));
        }

        // CF: ConvertHitObjectParser.convertSoundType | type != None && !type.HasFlag(Normal) | FALSE
        // CF: ConvertHitObjectParser.convertSoundType | type.HasFlag(Finish) | TRUE
        // CF: ConvertHitObjectParser.convertSoundType | type.HasFlag(Whistle) | TRUE
        // CF: ConvertHitObjectParser.convertSoundType | type.HasFlag(Clap) | TRUE
        // CF: ConvertHitObjectParser.convertSoundType | type.HasFlag(Finish) | FALSE
        // CF: ConvertHitObjectParser.convertSoundType | type.HasFlag(Whistle) | FALSE
        // CF: ConvertHitObjectParser.convertSoundType | type.HasFlag(Clap) | FALSE
        [Test]
        public void TestSoundTypeFlags()
        {
            var all = parser().Parse("0,0,1000,1,15");
            Assert.That(all.Samples.Select(s => s.Name), Is.EqualTo(new[] { "hitnormal", "hitfinish", "hitwhistle", "hitclap" }));
            Assert.That(((ConvertHitObjectParser.LegacyHitSampleInfo)all.Samples[0]).IsLayered, Is.False);

            var none = parser().Parse("0,0,1000,1,0");
            Assert.That(((ConvertHitObjectParser.LegacyHitSampleInfo)none.Samples[0]).IsLayered, Is.False);
        }

        // CF: ConvertHitObjectParser.LegacyHitSampleInfo.With | suffix != null | TRUE
        // CF: ConvertHitObjectParser.LegacyHitSampleInfo.With | newCustomSampleBank == 0 && useBeatmapSamples | TRUE
        // CF: ConvertHitObjectParser.LegacyHitSampleInfo.CustomSampleBank | Suffix != null | TRUE
        // CF: ConvertHitObjectParser.LegacyHitSampleInfo.CustomSampleBank | UseBeatmapSamples ? 1 : 0 | TRUE
        // CF: ConvertHitObjectParser.LegacyHitSampleInfo.CustomSampleBank | UseBeatmapSamples ? 1 : 0 | FALSE
        [Test]
        public void TestLegacyHitSampleInfoCustomBankAndEquality()
        {
            var plain = new ConvertHitObjectParser.LegacyHitSampleInfo("hitnormal");
            var beatmapSamples = new ConvertHitObjectParser.LegacyHitSampleInfo("hitnormal", customSampleBank: 1);
            var suffixed = new ConvertHitObjectParser.LegacyHitSampleInfo("hitnormal", customSampleBank: 3);

            Assert.That(plain.CustomSampleBank, Is.Zero);
            Assert.That(beatmapSamples.CustomSampleBank, Is.EqualTo(1));
            Assert.That(suffixed.CustomSampleBank, Is.EqualTo(3));
            Assert.That(suffixed, Is.EqualTo(new ConvertHitObjectParser.LegacyHitSampleInfo("hitnormal", customSampleBank: 3)));
            Assert.That(suffixed, Is.Not.EqualTo(beatmapSamples));

            var changed = (ConvertHitObjectParser.LegacyHitSampleInfo)suffixed.With(newVolume: 20);
            Assert.That(changed.Volume, Is.EqualTo(20));
            Assert.That(changed.CustomSampleBank, Is.EqualTo(3));

            var fromBeatmapFlag = (ConvertHitObjectParser.LegacyHitSampleInfo)plain.With(newUseBeatmapSamples: true);
            Assert.That(fromBeatmapFlag.CustomSampleBank, Is.EqualTo(1));
        }

        // CF: ConvertHitObjectParser.FileHitSampleInfo.LookupNames | Filename + extensionless + base | loop n
        // CF: ConvertHitObjectParser.FileHitSampleInfo.With | new FileHitSampleInfo | non-null
        [Test]
        public void TestFileHitSampleInfo()
        {
            var file = new ConvertHitObjectParser.FileHitSampleInfo("a.wav", 40);

            Assert.That(file.LookupNames.Take(2), Is.EqualTo(new[] { "a.wav", "a" }));
            Assert.That(file, Is.EqualTo(new ConvertHitObjectParser.FileHitSampleInfo("a.wav", 40)));
            Assert.That(file, Is.Not.EqualTo(new ConvertHitObjectParser.FileHitSampleInfo("b.wav", 40)));
            Assert.That(((ConvertHitObjectParser.FileHitSampleInfo)file.With(newVolume: 10)).Filename, Is.EqualTo("a.wav"));
        }
    }
}
