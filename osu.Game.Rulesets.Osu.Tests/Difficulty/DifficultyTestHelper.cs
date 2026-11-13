// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Difficulty
{
    /// <summary>
    /// Builds difficulty hit objects the same way <c>OsuDifficultyCalculator.CreateDifficultyHitObjects</c> does.
    /// </summary>
    internal static class DifficultyTestHelper
    {
        public static OsuHitObject Circle(double time, float x, float y = 192) => new HitCircle { StartTime = time, Position = new Vector2(x, y) };

        public static OsuHitObject Slider(double time, float x, float y, float dx, float dy, int repeats = 0)
            => new Slider
            {
                StartTime = time,
                Position = new Vector2(x, y),
                RepeatCount = repeats,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(dx, dy) }),
            };

        public static OsuHitObject Spinner(double time, double duration = 500) => new Spinner { StartTime = time, Duration = duration, Position = new Vector2(256, 192) };

        /// <summary>A straight line of circles (all angles are wide).</summary>
        public static OsuHitObject[] Line(int count, double spacing, float distance, double start = 1000)
            => Enumerable.Range(0, count).Select(i => Circle(start + i * spacing, 50 + i * distance)).ToArray();

        /// <summary>Circles going back and forth (all angles are acute).</summary>
        public static OsuHitObject[] ZigZag(int count, double spacing, float distance, double start = 1000)
            => Enumerable.Range(0, count).Select(i => Circle(start + i * spacing, 100 + (i % 2) * distance)).ToArray();

        public static List<DifficultyHitObject> Build(IEnumerable<OsuHitObject> objects, double clockRate = 1, BeatmapDifficulty? difficulty = null)
        {
            var all = objects.ToList();

            foreach (var o in all)
                o.ApplyDefaults(new ControlPointInfo(), difficulty ?? new BeatmapDifficulty());

            var result = new List<DifficultyHitObject>();

            for (int i = 1; i < all.Count; i++)
                result.Add(new OsuDifficultyHitObject(all[i], all[i - 1], clockRate, result, result.Count));

            return result;
        }
    }
}
