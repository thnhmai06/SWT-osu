# Plan: Control-flow unit tests for `EditorBeatmap` (Lv2)

## Context
User request: Lv2 control-flow tests (C2 + C3) for `osu.Game/Screens/Edit/EditorBeatmap.cs` (class `osu.Game.Screens.Edit.EditorBeatmap`, project `osu.Game` → in scope). Only one candidate; no rejected ones.
Mid-turn user update: **building/running tests is now allowed** → task boundary relaxed; verify with `dotnet test` and report real results (section 6 changes accordingly).

Discovery facts:
- Branch `testing-class`: commit `1442c46184` removed all test projects. `osu.Game.Tests/` has only `bin/obj`.
- Precedent on branch `testing-method` (commit `05df81123b`): minimal `osu.Game.Tests/osu.Game.Tests.csproj` (NUnit 4.6.1, NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.0, imports `osu.TestProject.props`, refs `osu.Game.Rulesets.Osu`). Re-create the same csproj (required: no test project exists → say so in report). Do **not** touch `osu.sln` (run tests via csproj).
- Upstream (pre-1442c46184) `osu.Game.Tests/Editing/BeatmapEditorChangeHandlerTest.cs` constructs `new EditorBeatmap(new OsuBeatmap { BeatmapInfo = { Ruleset = new OsuRuleset().RulesetInfo } })` in a plain `[TestFixture]` → reuse pattern, no host needed.
- Base class `TransactionalCommitComponent`: `EndChange()` of outermost transaction and `SaveState()` (no active txn) call `UpdateState()` → drives protected `UpdateState` publicly.
- `[Resolved] beatDivisor / editorClock`: injectable with framework `DependencyContainer.Inject(obj)` after caching `BindableBeatDivisor` + `EditorClock(beatmap, divisor)`. Protected `Update()` reached through a tiny test subclass `TestEditorBeatmap : EditorBeatmap` exposing `RunFrameUpdate() => Update()`.

## Files
- NEW `osu.Game.Tests/osu.Game.Tests.csproj` (copy of precedent).
- NEW `osu.Game.Tests/Editing/EditorBeatmapTest.cs` — namespace `osu.Game.Tests.Editing`, `[TestFixture] public class EditorBeatmapTest`, `#nullable disable` not needed (file nullable-enabled), method names `Test<Member>_<Path>`, one control-flow purpose per test (one-line comment), regions per member like precedent file.
- Helpers inside file: `createBeatmap(params HitObject[])` (OsuBeatmap + OsuRuleset info + one `TimingControlPoint{BeatLength=500}` at 0 when needed), `TestEditorBeatmap` subclass, `injectDependencies(beatmap, divisor)`, fake `PathOnlyHitObject : HitObject, IHasPathWithRepeats` (no `IHasSliderVelocity`) for the C3 FALSE value of the `and` pattern.

## Control-flow inventory → planned tests
| Id | Decision / condition | Outcomes → test(s) |
|---|---|---|
| C-1 | ctor `beatmapInfo ?? playable.BeatmapInfo` | non-null → `TestConstructor_ExplicitBeatmapInfo_UsedOverPlayable`; null → `TestConstructor_NullBeatmapInfo_FallsBackToPlayable` |
| C-2 | ctor `beatmapSkin is LegacyBeatmapSkin` | T → `TestConstructor_LegacySkin_WrappedInEditorBeatmapSkin` (`new LegacyBeatmapSkin(info, null)`); F → `TestConstructor_NullSkin_NoEditorSkin` |
| C-3 | ctor `storyboard ?? new Storyboard()` | non-null (same instance, Beatmap/BeatmapInfo set) / null (new one created) |
| C-4 | ctor loop over existing HitObjects | 0 iter (empty) / ≥1 iter: changing StartTime of pre-existing object reorders it (`TestConstructor_ExistingObjects_StartTimeTracked`) |
| C-5 | Breaks / Bookmarks / SliderVelocityPresets / PreviewTime change callbacks | Breaks mirrored to playable; Bookmarks sorted+distinct; SV presets sorted+distinct; PreviewTime written to metadata |
| CP-1 | `ConvertControlPoints`: `!(incoming is Legacy)` | T → same instance returned; F → new non-legacy instance |
| CP-2 | loop over AllControlPoints | 0 iter (empty legacy) / many |
| CP-3 | switch: `DifficultyControlPoint` / `SampleControlPoint` / default | difficulty dropped; sample dropped; timing + effect kept |
| P-1 | `BeatmapInfo` setter | throws `InvalidOperationException` |
| P-2 | pass-through props (Difficulty, ControlPointInfo, AudioLeadIn, StackLeniency, flags, DistanceSpacing, GridSize, TimelineZoom, Countdown, CountdownOffset, IBeatmap.Breaks/Bookmarks/SVPresets, TotalBreakTime, Metadata, HitObjects) | one forwarding test (set via editor → read on playable); `Clone()` returns distinct EditorBeatmap sharing PlayableBeatmap |
| S-1 | `PerformOnSelection`: `Count == 0` | T → action not invoked, no `TransactionBegan`; F → action per object + HitObjectUpdated for each |
| S-2 | its loop | 1 iter / many (covered by 2 selected objects) |
| A-1 | `findInsertionIndex` loop/`list[i].StartTime > t` | empty list (0 iter, returns -1 → index 0); first element greater (T at i=0 → front); none greater (loop exits → append); greater in middle; equal start time → inserted after existing |
| A-2 | `AddRange` loop | 0 iter (no txn effect/events) / many (all added, sorted, single `TransactionEnded`) |
| A-3 | `Insert(index,h)` | inserted at explicit index + HitObjectAdded fired |
| U-1 | `Update(h)` | UpdateInProgress true; processed on next SaveState → HitObjectUpdated, then false |
| U-2 | `UpdateAllHitObjects` loop | 0 iter (empty: still sets UpdateInProgress) / many (each object updated) |
| R-1 | `Remove`: `index == -1` | T → returns false, no event; F → true, removed, HitObjectRemoved |
| R-2 | `RemoveAt` | start-time bindable unbound: changing StartTime after removal doesn't re-insert |
| R-3 | `RemoveRange` loop / `Clear()` | many → all removed; Clear on empty → no-op |
| US-1 | `UpdateState` compound `a && b && c` (a=updates==0, b=deletes==0, c=inserts==0) | all T → early return (`SaveState` on fresh: BeatmapVersion unchanged, no BeatmapReprocessed); a=F (pending update only); a=T,b=F (delete only: Remove outside txn); a=T,b=T,c=F (insert only: Add outside txn) |
| US-2 | `BeatmapReprocessed?.` / `HitObjectRemoved?.` / `Added?.` / `Updated?.` | subscribed (fires) / not subscribed (no NRE – the not-subscribed tests above) |
| US-3 | deletes loop removes from SelectedHitObjects | selected deleted object deselected |
| US-4 | side effects | BeatmapVersion = `LegacyBeatmapEncoder.FIRST_LAZER_VERSION`; UpdateInProgress false |
| T-1 | trackStartTime callback | StartTime change re-sorts list + queues update |
| UP-1 | protected `Update()`: `batchPendingUpdates.Count > 0` | T → pending update applied (HitObjectUpdated); F → nothing fires (via `TestEditorBeatmap` + DI) |
| UP-2 | `ReferenceEquals(TimingPointAt(t), DEFAULT)` | T → HasTiming false (no timing points); F → HasTiming true |
| SN-1 | `SnapAllHitObjectsToCurrentDivisor`: `Count == 0` | T → no transaction; F → snapping |
| SN-2 | switch `IHasPathWithRepeats and IHasSliderVelocity` / `IHasDuration` / none | Slider → SV multiplier scaled; Spinner → Duration set; HitCircle → only StartTime snapped |
| SN-3 | C3 of `and` pattern | path T & sv T (Slider); path F (HitCircle); path T & sv F (`PathOnlyHitObject` → falls to IHasDuration case) |
| SN-4 | `oldDuration > 0` | T (normal slider); F (zero-length slider / fake with 0 duration) |
| SN-5 | `Math.Max(newStart + beatLen, snappedEnd)` | first arm (zero-duration circle) / second arm (long spinner) — asserted via spinner Duration |
| B-1 | `BeatDivisor => beatDivisor?.Value ?? 1` | null (no DI) → 1; non-null (DI divisor 4) → 4; checked through `GetBeatLengthAtTime` (500 vs 125) and `SnapTime` |
| D-1 | `Dispose`: `BeatmapSkin?.Dispose()` | null skin → no throw; legacy skin → disposed without throw |

Expected values derive from doc comments/names (sorted insertion, events after transaction end, version bump comment, snapping to beat grid of 500 ms / divisor).

Infeasible: none expected. Not-unit-testable: none expected (Update via subclass+DI). If `DependencyContainer.Inject` or `LegacyBeatmapSkin(info, null)` fails at runtime (e.g. `beatmapInfo.Path` null), mark that item blocked and report.

## Verification
1. `dotnet build osu.Game.Tests/osu.Game.Tests.csproj`
2. `dotnet test osu.Game.Tests/osu.Game.Tests.csproj --filter FullyQualifiedName~EditorBeatmapTest`
3. Fix test-side errors only (never production). Wrong expectations → re-check against code intent, not just copy output.
4. Report in user's 6-section format; section 6 cites actual run result; `git status` shows only the two new files.
