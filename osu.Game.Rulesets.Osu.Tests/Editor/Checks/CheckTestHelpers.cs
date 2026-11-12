// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    internal static class CheckTestHelpers
    {
        public static BeatmapVerifierContext Context(IEnumerable<HitObject> hitObjects, DifficultyRating rating = DifficultyRating.Easy, BeatmapDifficulty? difficulty = null)
        {
            var beatmap = new Beatmap { HitObjects = new List<HitObject>(hitObjects) };

            if (difficulty != null)
                beatmap.Difficulty = difficulty;

            return new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap), rating);
        }

        public static HitCircle Circle(double time, float x = 100, float y = 100) => new HitCircle { StartTime = time, Position = new Vector2(x, y) };

        /// <summary>
        /// A slider going right from <paramref name="x"/>,<paramref name="y"/> by <paramref name="length"/> pixels, with defaults applied
        /// (velocity 0.14 px/ms with the default difficulty).
        /// </summary>
        public static Slider Slider(double time, float x, float y, float length, int repeats = 0)
        {
            var slider = new Slider
            {
                StartTime = time,
                Position = new Vector2(x, y),
                RepeatCount = repeats,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(length, 0) })
            };
            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return slider;
        }

        public static Spinner Spinner(double time, double duration) => new Spinner { StartTime = time, Duration = duration };

        public sealed class PlainHitObject : HitObject
        {
        }
    }
}
