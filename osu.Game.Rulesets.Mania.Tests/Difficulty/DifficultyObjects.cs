// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mania.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Difficulty
{
    /// <summary>Builds <see cref="ManiaDifficultyHitObject"/>s the same way the difficulty calculator does.</summary>
    internal static class DifficultyObjects
    {
        public static List<ManiaDifficultyHitObject> Create(int columns, params ManiaHitObject[] objects)
        {
            var all = new List<DifficultyHitObject>();
            var perColumn = Enumerable.Range(0, columns).Select(_ => new List<DifficultyHitObject>()).ToArray();

            for (int i = 0; i < objects.Length; i++)
            {
                objects[i].ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
                var last = i == 0 ? objects[0] : objects[i - 1];
                var obj = new ManiaDifficultyHitObject(objects[i], last, 1, all, perColumn, i);
                all.Add(obj);
                perColumn[obj.Column].Add(obj);
            }

            return all.Cast<ManiaDifficultyHitObject>().ToList();
        }
    }
}
