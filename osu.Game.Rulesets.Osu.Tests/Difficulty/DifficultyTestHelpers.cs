// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    /// <summary>
    /// Builds playable beatmaps and difficulty hit objects without any game host.
    /// </summary>
    internal static class DifficultyTestHelpers
    {
        public static HitCircle Circle(double time, float x, float y) => new HitCircle { StartTime = time, Position = new Vector2(x, y) };

        /// <summary>A linear slider going right by <paramref name="length"/> pixels (velocity 0.14 px/ms with the default slider multiplier).</summary>
        public static Slider Slider(double time, float x, float y, float length, int repeats = 0) => new Slider
        {
            StartTime = time,
            Position = new Vector2(x, y),
            RepeatCount = repeats,
            Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(length, 0) })
        };

        public static Spinner Spinner(double time, double duration) => new Spinner { StartTime = time, Duration = duration, Position = new Vector2(256, 192) };

        public static OsuBeatmap Beatmap(IEnumerable<OsuHitObject> objects, float cs = 4, float ar = 9, float od = 8, float hp = 5, double sliderMultiplier = 1.4)
        {
            var beatmap = new OsuBeatmap();
            beatmap.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            beatmap.BeatmapInfo.Difficulty = new BeatmapDifficulty { CircleSize = cs, ApproachRate = ar, OverallDifficulty = od, DrainRate = hp, SliderMultiplier = sliderMultiplier };

            foreach (var o in objects)
                beatmap.HitObjects.Add(o);

            return beatmap;
        }

        public static TestWorkingBeatmap Working(IEnumerable<OsuHitObject> objects, float cs = 4, float ar = 9, float od = 8, float hp = 5, double sliderMultiplier = 1.4)
            => new TestWorkingBeatmap(Beatmap(objects, cs, ar, od, hp, sliderMultiplier));

        public static IBeatmap Playable(IEnumerable<OsuHitObject> objects, Mod[]? mods = null, float cs = 4, float ar = 9, float od = 8)
            => Working(objects, cs, ar, od).GetPlayableBeatmap(new OsuRuleset().RulesetInfo, mods ?? new Mod[0]);

        /// <summary>Evenly spaced circles: <paramref name="count"/> objects <paramref name="spacing"/> pixels and <paramref name="deltaTime"/> ms apart.</summary>
        public static List<OsuHitObject> Stream(int count, double deltaTime, float spacing, float startX = 50, float y = 100)
            => Enumerable.Range(0, count).Select(i => (OsuHitObject)Circle(1000 + i * deltaTime, startX + (i % 2 == 0 ? 0 : spacing), y)).ToList();

        public static List<OsuDifficultyHitObject> DifficultyObjects(IEnumerable<OsuHitObject> objects, Mod[]? mods = null, float cs = 4, float ar = 9, float od = 8, double sliderMultiplier = 1.4)
        {
            mods ??= new Mod[0];
            var working = Working(objects, cs, ar, od, 5, sliderMultiplier);
            var playable = working.GetPlayableBeatmap(new OsuRuleset().RulesetInfo, mods);
            return new ExposedCalculator(working).Create(playable, mods);
        }

        public sealed class ExposedCalculator : OsuDifficultyCalculator
        {
            public ExposedCalculator(IWorkingBeatmap beatmap)
                : base(new OsuRuleset().RulesetInfo, beatmap)
            {
            }

            public List<OsuDifficultyHitObject> Create(IBeatmap beatmap, Mod[] mods) => CreateDifficultyHitObjects(beatmap, mods).Cast<OsuDifficultyHitObject>().ToList();

            public osu.Game.Rulesets.Difficulty.Skills.Skill[] Skills(IBeatmap beatmap, Mod[] mods) => CreateSkills(beatmap, mods);
        }
    }
}
