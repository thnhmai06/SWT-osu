// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Rulesets.Edit;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    internal static class CheckTestUtils
    {
        public static BeatmapVerifierContext Context(IBeatmap beatmap) => new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap));
    }
}
