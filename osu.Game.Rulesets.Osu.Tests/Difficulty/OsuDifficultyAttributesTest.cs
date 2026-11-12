// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    [TestFixture]
    public class OsuDifficultyAttributesTest
    {
        private static OsuDifficultyAttributes create(params Mod[] mods) => new OsuDifficultyAttributes
        {
            Mods = mods,
            StarRating = 6.5,
            MaxCombo = 1234,
            AimDifficulty = 3.1,
            SpeedDifficulty = 2.2,
            ReadingDifficulty = 1.3,
            FlashlightDifficulty = 4.4,
            SliderFactor = 0.9,
            AimDifficultStrainCount = 50,
            SpeedDifficultStrainCount = 40,
            SpeedNoteCount = 30,
            AimDifficultSliderCount = 20,
            AimTopWeightedSliderFactor = 0.8,
            SpeedTopWeightedSliderFactor = 0.7,
            NestedScorePerObject = 12.5,
            LegacyScoreBaseMultiplier = 3,
            MaximumLegacyComboScore = 99999,
            ReadingDifficultNoteCount = 15,
        };

        [Test]
        public void TestFlashlightSerialisation_WithFlashlightMod_True()
        {
            // Mods.Any(m => m is ModFlashlight) TRUE
            Assert.That(create(new OsuModFlashlight()).ShouldSerializeFlashlightDifficulty(), Is.True);
        }

        [Test]
        public void TestFlashlightSerialisation_WithoutFlashlightMod_False()
        {
            Assert.That(create(new OsuModHidden()).ShouldSerializeFlashlightDifficulty(), Is.False);
            Assert.That(create().ShouldSerializeFlashlightDifficulty(), Is.False);
        }

        [Test]
        public void TestToDatabaseAttributes_FlashlightEntryOnlyWithFlashlightMod()
        {
            // `if (ShouldSerializeFlashlightDifficulty())` TRUE adds exactly one entry compared to FALSE
            int withFlashlight = create(new OsuModFlashlight()).ToDatabaseAttributes().Count();
            int withoutFlashlight = create().ToDatabaseAttributes().Count();

            Assert.That(withFlashlight, Is.EqualTo(withoutFlashlight + 1));
            Assert.That(create().ToDatabaseAttributes().Select(a => a.attributeId), Is.Unique);
        }

        [Test]
        public void TestFromDatabaseAttributes_RoundTripsValues()
        {
            var original = create(new OsuModFlashlight());
            var values = original.ToDatabaseAttributes().ToDictionary(a => a.attributeId, a => Convert.ToDouble(a.value));
            var online = new APIBeatmap { CircleCount = 11, SliderCount = 22, SpinnerCount = 3 };

            var restored = new OsuDifficultyAttributes();
            restored.FromDatabaseAttributes(values, online);

            Assert.That(restored.MaxCombo, Is.EqualTo(1234));
            Assert.That(restored.StarRating, Is.EqualTo(6.5));
            Assert.That(restored.AimDifficulty, Is.EqualTo(3.1));
            Assert.That(restored.SpeedDifficulty, Is.EqualTo(2.2));
            Assert.That(restored.ReadingDifficulty, Is.EqualTo(1.3));
            Assert.That(restored.FlashlightDifficulty, Is.EqualTo(4.4));
            Assert.That(restored.SliderFactor, Is.EqualTo(0.9));
            Assert.That(restored.AimDifficultStrainCount, Is.EqualTo(50));
            Assert.That(restored.SpeedDifficultStrainCount, Is.EqualTo(40));
            Assert.That(restored.SpeedNoteCount, Is.EqualTo(30));
            Assert.That(restored.AimDifficultSliderCount, Is.EqualTo(20));
            Assert.That(restored.AimTopWeightedSliderFactor, Is.EqualTo(0.8));
            Assert.That(restored.SpeedTopWeightedSliderFactor, Is.EqualTo(0.7));
            Assert.That(restored.NestedScorePerObject, Is.EqualTo(12.5));
            Assert.That(restored.LegacyScoreBaseMultiplier, Is.EqualTo(3));
            Assert.That(restored.MaximumLegacyComboScore, Is.EqualTo(99999));
            Assert.That(restored.ReadingDifficultNoteCount, Is.EqualTo(15));
            Assert.That(restored.HitCircleCount, Is.EqualTo(11));
            Assert.That(restored.SliderCount, Is.EqualTo(22));
            Assert.That(restored.SpinnerCount, Is.EqualTo(3));
        }

        [Test]
        public void TestFromDatabaseAttributes_MissingFlashlight_DefaultsToZero()
        {
            // GetValueOrDefault: the flashlight entry is optional
            var values = create().ToDatabaseAttributes().ToDictionary(a => a.attributeId, a => Convert.ToDouble(a.value));

            var restored = new OsuDifficultyAttributes { FlashlightDifficulty = 9 };
            restored.FromDatabaseAttributes(values, new APIBeatmap());

            Assert.That(restored.FlashlightDifficulty, Is.EqualTo(0));
        }

        [Test]
        public void TestFromDatabaseAttributes_MissingRequiredEntry_Throws()
        {
            var values = create().ToDatabaseAttributes().ToDictionary(a => a.attributeId, a => Convert.ToDouble(a.value));
            values.Remove(values.Keys.Max());

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => new OsuDifficultyAttributes().FromDatabaseAttributes(values, new APIBeatmap()));
        }

        // ---- performance attributes display ----

        [Test]
        public void TestPerformanceAttributes_DisplayListsAllComponents()
        {
            var attributes = new OsuPerformanceAttributes { Total = 100, Aim = 40, Speed = 30, Accuracy = 20, Flashlight = 7, Reading = 3 };

            var display = attributes.GetAttributesForDisplay().ToList();

            Assert.That(display.Select(d => d.PropertyName), Is.SupersetOf(new[] { nameof(OsuPerformanceAttributes.Aim), nameof(OsuPerformanceAttributes.Speed), nameof(OsuPerformanceAttributes.Accuracy), nameof(OsuPerformanceAttributes.Flashlight), nameof(OsuPerformanceAttributes.Reading) }));
            Assert.That(display.Single(d => d.PropertyName == nameof(OsuPerformanceAttributes.Flashlight)).Value, Is.EqualTo(7));
            Assert.That(display.Single(d => d.PropertyName == nameof(OsuPerformanceAttributes.Flashlight)).DisplayName.ToString(), Is.EqualTo("Flashlight Bonus"));
        }
    }
}
