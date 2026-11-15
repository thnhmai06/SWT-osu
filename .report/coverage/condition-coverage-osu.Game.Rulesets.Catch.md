# Condition coverage — osu.Game.Rulesets.Catch

Tổng: 472 | 353/944 | 167 | 286 | 37.4%

## Namespace

| Namespace | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Rulesets.Catch | 14 | 28/28 | 14 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Beatmaps | 28 | 51/56 | 24 | 1 | 91.1% |
| osu.Game.Rulesets.Catch.Difficulty | 23 | 44/46 | 21 | 0 | 95.7% |
| osu.Game.Rulesets.Catch.Difficulty.Evaluators | 17 | 29/34 | 12 | 0 | 85.3% |
| osu.Game.Rulesets.Catch.Difficulty.Preprocessing | 2 | 4/4 | 2 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Edit | 59 | 4/118 | 2 | 57 | 3.4% |
| osu.Game.Rulesets.Catch.Edit.Blueprints | 34 | 0/68 | 0 | 34 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components | 53 | 0/106 | 0 | 53 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Checks | 15 | 30/30 | 15 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Mods | 24 | 19/48 | 9 | 14 | 39.6% |
| osu.Game.Rulesets.Catch.Objects | 34 | 65/68 | 31 | 0 | 95.6% |
| osu.Game.Rulesets.Catch.Objects.Drawables | 8 | 0/16 | 0 | 8 | 0.0% |
| osu.Game.Rulesets.Catch.Replays | 24 | 46/48 | 22 | 0 | 95.8% |
| osu.Game.Rulesets.Catch.Scoring | 18 | 33/36 | 15 | 0 | 91.7% |
| osu.Game.Rulesets.Catch.Skinning.Argon | 10 | 0/20 | 0 | 10 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Default | 7 | 0/14 | 0 | 7 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Legacy | 30 | 0/60 | 0 | 30 | 0.0% |
| osu.Game.Rulesets.Catch.UI | 72 | 0/144 | 0 | 72 | 0.0% |

## Namespace — sau khi loại bỏ code UI framework

Loại 244 điều kiện trong type UI (dẫn xuất từ `osu.Framework.Graphics.Drawable`, không qua `Component`). Còn lại: 228 | 353/456 | 167 | 42 | 77.4%

| Namespace | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Rulesets.Catch | 14 | 28/28 | 14 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Beatmaps | 28 | 51/56 | 24 | 1 | 91.1% |
| osu.Game.Rulesets.Catch.Difficulty | 23 | 44/46 | 21 | 0 | 95.7% |
| osu.Game.Rulesets.Catch.Difficulty.Evaluators | 17 | 29/34 | 12 | 0 | 85.3% |
| osu.Game.Rulesets.Catch.Difficulty.Preprocessing | 2 | 4/4 | 2 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Edit | 2 | 4/4 | 2 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Edit.Checks | 15 | 30/30 | 15 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Mods | 21 | 19/42 | 9 | 11 | 45.2% |
| osu.Game.Rulesets.Catch.Objects | 34 | 65/68 | 31 | 0 | 95.6% |
| osu.Game.Rulesets.Catch.Replays | 24 | 46/48 | 22 | 0 | 95.8% |
| osu.Game.Rulesets.Catch.Scoring | 18 | 33/36 | 15 | 0 | 91.7% |
| osu.Game.Rulesets.Catch.Skinning.Argon | 4 | 0/8 | 0 | 4 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Default | 4 | 0/8 | 0 | 4 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Legacy | 22 | 0/44 | 0 | 22 | 0.0% |

## Type

| Type | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| osu.Game.Rulesets.Catch.CatchRuleset | 14 | 28/28 | 14 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Beatmaps.CatchBeatmap | 1 | 2/2 | 1 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Beatmaps.CatchBeatmapConverter | 1 | 2/2 | 1 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Beatmaps.CatchBeatmapProcessor | 26 | 47/52 | 22 | 1 | 90.4% |
| osu.Game.Rulesets.Catch.Difficulty.CatchDifficultyCalculator | 4 | 7/8 | 3 | 0 | 87.5% |
| osu.Game.Rulesets.Catch.Difficulty.CatchLegacyScoreSimulator | 7 | 14/14 | 7 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Difficulty.CatchPerformanceCalculator | 12 | 23/24 | 11 | 0 | 95.8% |
| osu.Game.Rulesets.Catch.Difficulty.Evaluators.MovementEvaluator | 17 | 29/34 | 12 | 0 | 85.3% |
| osu.Game.Rulesets.Catch.Difficulty.Preprocessing.CatchDifficultyHitObject | 2 | 4/4 | 2 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Edit.CatchBlueprintContainer _(UI)_ | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.CatchDistanceSnapGrid _(UI)_ | 5 | 0/10 | 0 | 5 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.CatchHitObjectComposer _(UI)_ | 21 | 0/42 | 0 | 21 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.CatchHitObjectInspector _(UI)_ | 5 | 0/10 | 0 | 5 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.CatchHitObjectUtils | 1 | 2/2 | 1 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Edit.CatchSelectionHandler _(UI)_ | 21 | 0/42 | 0 | 21 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.DrawableCatchEditorRuleset _(UI)_ | 2 | 0/4 | 0 | 2 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.PositionRange | 1 | 2/2 | 1 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.BananaShowerPlacementBlueprint _(UI)_ | 6 | 0/12 | 0 | 6 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.FruitPlacementBlueprint _(UI)_ | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.FruitSelectionBlueprint _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.JuiceStreamPlacementBlueprint _(UI)_ | 9 | 0/18 | 0 | 9 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.JuiceStreamSelectionBlueprint _(UI)_ | 15 | 0/30 | 0 | 15 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components.EditablePath _(UI)_ | 21 | 0/42 | 0 | 21 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components.NestedOutlineContainer _(UI)_ | 4 | 0/8 | 0 | 4 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components.PlacementEditablePath _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components.ScrollingPath _(UI)_ | 2 | 0/4 | 0 | 2 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components.SelectionEditablePath _(UI)_ | 20 | 0/40 | 0 | 20 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components.TimeSpanOutline _(UI)_ | 2 | 0/4 | 0 | 2 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Blueprints.Components.VertexPiece _(UI)_ | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.Edit.Checks.CheckBananaShowerGap | 9 | 18/18 | 9 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Edit.Checks.CheckCatchAbnormalDifficultySettings | 6 | 12/12 | 6 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModDifficultyAdjust | 10 | 19/20 | 9 | 0 | 95.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModFlashlight.CatchFlashlight _(UI)_ | 2 | 0/4 | 0 | 2 | 0.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModHidden | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModMovingFast | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModMovingFast.InvertDashInputHelper _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModNoScope | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModRelax | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Mods.CatchModSynesthesia | 5 | 0/10 | 0 | 5 | 0.0% |
| osu.Game.Rulesets.Catch.Objects.Banana.BananaHitSampleInfo | 2 | 3/4 | 1 | 0 | 75.0% |
| osu.Game.Rulesets.Catch.Objects.BananaShower | 3 | 6/6 | 3 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Objects.CatchHitObject | 4 | 8/8 | 4 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Objects.JuiceStream | 4 | 8/8 | 4 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Objects.JuiceStreamPath | 20 | 38/40 | 18 | 0 | 95.0% |
| osu.Game.Rulesets.Catch.Objects.JuiceStreamPathVertex | 1 | 2/2 | 1 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Objects.Drawables.DrawableBanana _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Objects.Drawables.DrawableCatchHitObject _(UI)_ | 4 | 0/8 | 0 | 4 | 0.0% |
| osu.Game.Rulesets.Catch.Objects.Drawables.NestedFruitContainer _(UI)_ | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.Replays.CatchAutoGenerator | 10 | 18/20 | 8 | 0 | 90.0% |
| osu.Game.Rulesets.Catch.Replays.CatchReplayFrame | 14 | 28/28 | 14 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Scoring.CatchHealthProcessor | 5 | 9/10 | 4 | 0 | 90.0% |
| osu.Game.Rulesets.Catch.Scoring.CatchScoreMultiplierCalculator | 6 | 10/12 | 4 | 0 | 83.3% |
| osu.Game.Rulesets.Catch.Scoring.CatchScoreProcessor | 7 | 14/14 | 7 | 0 | 100.0% |
| osu.Game.Rulesets.Catch.Skinning.Argon.ArgonDropletPiece _(UI)_ | 2 | 0/4 | 0 | 2 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Argon.ArgonFruitPiece _(UI)_ | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Argon.ArgonHitExplosion _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Argon.CatchArgonSkinTransformer | 4 | 0/8 | 0 | 4 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Default.CatchHitObjectPiece _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Default.CatchTrianglesSkinTransformer | 4 | 0/8 | 0 | 4 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Default.PulpFormation _(UI)_ | 2 | 0/4 | 0 | 2 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Legacy.CatchLegacySkinTransformer | 22 | 0/44 | 0 | 22 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Legacy.LegacyCatchComboCounter _(UI)_ | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Legacy.LegacyCatchHitObjectPiece _(UI)_ | 4 | 0/8 | 0 | 4 | 0.0% |
| osu.Game.Rulesets.Catch.Skinning.Legacy.LegacyHitExplosion _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.UI.CatchComboDisplay _(UI)_ | 7 | 0/14 | 0 | 7 | 0.0% |
| osu.Game.Rulesets.Catch.UI.CatchPlayfieldAdjustmentContainer _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.UI.CatchTouchInputMapper _(UI)_ | 13 | 0/26 | 0 | 13 | 0.0% |
| osu.Game.Rulesets.Catch.UI.CatchTouchInputMapper.InputArea _(UI)_ | 3 | 0/6 | 0 | 3 | 0.0% |
| osu.Game.Rulesets.Catch.UI.Catcher _(UI)_ | 35 | 0/70 | 0 | 35 | 0.0% |
| osu.Game.Rulesets.Catch.UI.CatcherArea _(UI)_ | 9 | 0/18 | 0 | 9 | 0.0% |
| osu.Game.Rulesets.Catch.UI.CatcherTrailDisplay _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.UI.DrawableCatchRuleset _(UI)_ | 1 | 0/2 | 0 | 1 | 0.0% |
| osu.Game.Rulesets.Catch.UI.HitExplosion _(UI)_ | 2 | 0/4 | 0 | 2 | 0.0% |

## Method

### osu.Game.Rulesets.Catch.CatchRuleset

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ConvertFromLegacyMods(LegacyMods)` | 14 | 28/28 | 14 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Beatmaps.CatchBeatmap

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetPalpableObjects(IEnumerable<HitObject>)` | 1 | 2/2 | 1 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Beatmaps.CatchBeatmapConverter

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ConvertHitObject(HitObject, IBeatmap, CancellationToken)` | 1 | 2/2 | 1 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Beatmaps.CatchBeatmapProcessor

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ApplyPositionOffsets(IBeatmap)` | 3 | 5/6 | 2 | 0 | 83.3% |
| `PostProcess()` | 2 | 3/4 | 1 | 0 | 75.0% |
| `PreProcess()` | 3 | 6/6 | 3 | 0 | 100.0% |
| `applyHardRockOffset(CatchHitObject, float?, double, LegacyRandom)` | 5 | 10/10 | 5 | 0 | 100.0% |
| `applyOffset(float, float)` | 3 | 6/6 | 3 | 0 | 100.0% |
| `applyRandomOffset(float, double, LegacyRandom)` | 3 | 3/6 | 1 | 1 | 50.0% |
| `initialiseHyperDash(IBeatmap)` | 7 | 14/14 | 7 | 0 | 100.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`ApplyPositionOffsets(IBeatmap)`**

- `Beatmaps/CatchBeatmapProcessor.cs:105` `catchObject is Droplet` — thiếu `true`

**`PostProcess()`**

- `Beatmaps/CatchBeatmapProcessor.cs:55` `obj.NestedHitObjects.LastOrDefault() is IHasComboInformation lastNested` — thiếu `false`

**`applyRandomOffset(float, double, LegacyRandom)`**

- `Beatmaps/CatchBeatmapProcessor.cs:173` `right` — thiếu `true`
- `Beatmaps/CatchBeatmapProcessor.cs:176` `position + rand <= CatchPlayfield.WIDTH` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Difficulty.CatchDifficultyCalculator

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `CreateDifficultyAttributes(IBeatmap, Mod[], Skill[])` | 1 | 2/2 | 1 | 0 | 100.0% |
| `CreateDifficultyHitObjects(IBeatmap, Mod[])` | 3 | 5/6 | 2 | 0 | 83.3% |

<details><summary>Điều kiện còn thiếu</summary>

**`CreateDifficultyHitObjects(IBeatmap, Mod[])`**

- `Difficulty/CatchDifficultyCalculator.cs:65` `hitObject is TinyDroplet` — thiếu `true`

</details>

### osu.Game.Rulesets.Catch.Difficulty.CatchLegacyScoreSimulator

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetLegacyScoreMultiplier(IReadOnlyList<Mod>, LegacyBeatmapConversionDifficultyInfo)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `Simulate(IWorkingBeatmap, IBeatmap)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `simulateHit(HitObject, LegacyScoreAttributes)` | 3 | 6/6 | 3 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Difficulty.CatchPerformanceCalculator

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `CreatePerformanceAttributes(ScoreInfo, DifficultyAttributes)` | 11 | 21/22 | 10 | 0 | 95.5% |
| `accuracy()` | 1 | 2/2 | 1 | 0 | 100.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`CreatePerformanceAttributes(ScoreInfo, DifficultyAttributes)`**

- `Difficulty/CatchPerformanceCalculator.cs:85` `approachRate > 10.0` — thiếu `false`

</details>

### osu.Game.Rulesets.Catch.Difficulty.Evaluators.MovementEvaluator

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `EvaluateDifficultyOf(DifficultyHitObject)` | 17 | 29/34 | 12 | 0 | 85.3% |

<details><summary>Điều kiện còn thiếu</summary>

**`EvaluateDifficultyOf(DifficultyHitObject)`**

- `Difficulty/Evaluators/MovementEvaluator.cs:56` `catchCurrent.DistanceMoved == 0` — thiếu `true`
- `Difficulty/Evaluators/MovementEvaluator.cs:56` `catchPrevObj.DistanceMoved == 0` — thiếu `true`
- `Difficulty/Evaluators/MovementEvaluator.cs:87` `catchCurrent.ExactDistanceMoved == -catchLast.ExactDistanceMoved` — thiếu `false`
- `Difficulty/Evaluators/MovementEvaluator.cs:87` `catchLast.ExactDistanceMoved == -catchLastLast.ExactDistanceMoved` — thiếu `false`
- `Difficulty/Evaluators/MovementEvaluator.cs:88` `catchLast.StrainTime == catchLastLast.StrainTime` — thiếu `false`

</details>

### osu.Game.Rulesets.Catch.Difficulty.Preprocessing.CatchDifficultyHitObject

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `setMovementState()` | 2 | 4/4 | 2 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Edit.CatchBlueprintContainer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `TryMoveBlueprints(DragEvent, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)>)` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`TryMoveBlueprints(DragEvent, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)>)`**

- `Edit/CatchBlueprintContainer.cs:58` `distanceSnapResult != null` — chưa được đánh giá
- `Edit/CatchBlueprintContainer.cs:58` `Vector2.Distance(gridSnapResult.ScreenSpacePosition, distanceSnapResult.ScreenSpacePosition) < CatchHitObjectComposer.DISTANCE_SNAP_RADIUS` — chưa được đánh giá
- `Edit/CatchBlueprintContainer.cs:64` `moved` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.CatchDistanceSnapGrid

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `.ctor(double[])` | 1 | 0/2 | 0 | 1 | 0.0% |
| `GetSnappedPosition(Vector2)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `Update()` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`.ctor(double[])`**

- `Edit/CatchDistanceSnapGrid.cs:50` `i < velocities.Length` — chưa được đánh giá

**`GetSnappedPosition(Vector2)`**

- `Edit/CatchDistanceSnapGrid.cs:113` `time <= StartTime` — chưa được đánh giá

**`Update()`**

- `Edit/CatchDistanceSnapGrid.cs:70` `i < velocities.Length` — chưa được đánh giá
- `Edit/CatchDistanceSnapGrid.cs:79` `velocity > 0` — chưa được đánh giá
- `Edit/CatchDistanceSnapGrid.cs:81` `velocity < 0` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.CatchHitObjectComposer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `FindSnappedPositionAndTime(Vector2)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `OnKeyDown(KeyDownEvent)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `SelectFromTimestamp(double, string)` | 8 | 0/16 | 0 | 8 | 0.0% |
| `TryDistanceSnap(Vector2)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `getDistanceSnapGridSourceHitObject()` | 4 | 0/8 | 0 | 4 | 0.0% |
| `getLastSnappableHitObject(double)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `updateDistanceSnapGrid()` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`FindSnappedPositionAndTime(Vector2)`**

- `Edit/CatchHitObjectComposer.cs:84` `limitPlacementToCurrentTime.Value` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:85` `BlueprintContainer.CurrentHitObjectPlacement?.PlacementActive == PlacementBlueprint.PlacementState.Waiting` — chưa được đánh giá

**`OnKeyDown(KeyDownEvent)`**

- `Edit/CatchHitObjectComposer.cs:143` `e.Repeat` — chưa được đánh giá

**`SelectFromTimestamp(double, string)`**

- `Edit/CatchHitObjectComposer.cs:256` `selection_regex.IsMatch(objectDescription)` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:262` `i < splitDescription.Length` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:264` `int.TryParse(splitDescription[i], out int combo)` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:264` `combo < 1` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:269` `current == null` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:274` `i < splitDescription.Length - 1` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:275` `h != current` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:275` `h.StartTime >= current.StartTime` — chưa được đánh giá

**`TryDistanceSnap(Vector2)`**

- `Edit/CatchHitObjectComposer.cs:163` `distanceSnapGrid.IsPresent` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:163` `distanceSnapGrid.GetSnappedPosition(screenSpacePosition) is SnapResult snapResult` — chưa được đánh giá

**`getDistanceSnapGridSourceHitObject()`**

- `Edit/CatchHitObjectComposer.cs:191` `EditorBeatmap.SelectedHitObjects.Count == 0` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:199` `CursorInPlacementArea` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:202` `EditorBeatmap.PlacementObject.Value is JuiceStream` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:205` `BlueprintContainer.CurrentPlacement.PlacementActive is PlacementBlueprint.PlacementState.Active` — chưa được đánh giá

**`getLastSnappableHitObject(double)`**

- `Edit/CatchHitObjectComposer.cs:171` `h.GetEndTime() < time` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:171` `h is BananaShower` — chưa được đánh giá

**`updateDistanceSnapGrid()`**

- `Edit/CatchHitObjectComposer.cs:226` `DistanceSnapProvider.DistanceSnapToggle.Value != TernaryState.True` — chưa được đánh giá
- `Edit/CatchHitObjectComposer.cs:234` `sourceHitObject == null` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.CatchHitObjectInspector

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `AddInspectorValues(HitObject[])` | 5 | 0/10 | 0 | 5 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`AddInspectorValues(HitObject[])`**

- `Edit/CatchHitObjectInspector.cs:17` `objects.Length > 0` — chưa được đánh giá
- `Edit/CatchHitObjectInspector.cs:25` `precedingObject != null` — chưa được đánh giá
- `Edit/CatchHitObjectInspector.cs:25` `precedingObject is not BananaShower` — chưa được đánh giá
- `Edit/CatchHitObjectInspector.cs:32` `nextObject != null` — chưa được đánh giá
- `Edit/CatchHitObjectInspector.cs:32` `nextObject is not BananaShower` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.CatchHitObjectUtils

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetPositionRange(HitObject)` | 1 | 2/2 | 1 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Edit.CatchSelectionHandler

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `HandleFlip(Direction, bool)` | 3 | 0/6 | 0 | 3 | 0.0% |
| `HandleMovement(MoveSelectionEvent<HitObject>)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `HandleReverse()` | 2 | 0/4 | 0 | 2 | 0.0% |
| `OnKeyDown(KeyDownEvent)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `OnKeyUp(KeyUpEvent)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `OnSelectionChanged()` | 6 | 0/12 | 0 | 6 | 0.0% |
| `handleFlip(PositionRange, CatchHitObject, bool)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `limitMovement(float, IEnumerable<HitObject>)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `moveSelection(float)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `nudgeSelection(float)` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`HandleFlip(Direction, bool)`**

- `Edit/CatchSelectionHandler.cs:119` `SelectedItems.Count == 0` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:123` `direction == Direction.Vertical` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:132` `h is CatchHitObject catchObject` — chưa được đánh giá

**`HandleMovement(MoveSelectionEvent<HitObject>)`**

- `Edit/CatchSelectionHandler.cs:37` `deltaX == 0` — chưa được đánh giá

**`HandleReverse()`**

- `Edit/CatchSelectionHandler.cs:158` `h is JuiceStream juiceStream` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:170` `i < hitObjects.Count` — chưa được đánh giá

**`OnKeyDown(KeyDownEvent)`**

- `Edit/CatchSelectionHandler.cs:68` `e.ShiftPressed` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:71` `e.ControlPressed` — chưa được đánh giá

**`OnKeyUp(KeyUpEvent)`**

- `Edit/CatchSelectionHandler.cs:90` `nudgeMovementActive` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:90` `e.ControlPressed` — chưa được đánh giá

**`OnSelectionChanged()`**

- `Edit/CatchSelectionHandler.cs:181` `selectionRange.Length > 0` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:181` `h is CatchHitObject` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:181` `h is BananaShower` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:181` `SelectedItems.Any(h => h is CatchHitObject && !(h is BananaShower))` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:182` `SelectedItems.Count > 1` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:182` `SelectedItems.Any(h => h is JuiceStream)` — chưa được đánh giá

**`handleFlip(PositionRange, CatchHitObject, bool)`**

- `Edit/CatchSelectionHandler.cs:228` `flipOverOrigin` — chưa được đánh giá

**`limitMovement(float, IEnumerable<HitObject>)`**

- `Edit/CatchSelectionHandler.cs:201` `lowerBound > upperBound` — chưa được đánh giá

**`moveSelection(float)`**

- `Edit/CatchSelectionHandler.cs:52` `h is CatchHitObject catchObject` — chưa được đánh giá

**`nudgeSelection(float)`**

- `Edit/CatchSelectionHandler.cs:102` `nudgeMovementActive` — chưa được đánh giá
- `Edit/CatchSelectionHandler.cs:110` `firstBlueprint == null` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.DrawableCatchEditorRuleset

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Dispose(bool)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `onBeatmapReprocessed()` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Dispose(bool)`**

- `Edit/DrawableCatchEditorRuleset.cs:48` `editorBeatmap.IsNotNull()` — chưa được đánh giá

**`onBeatmapReprocessed()`**

- `Edit/DrawableCatchEditorRuleset.cs:54` `Playfield is CatchEditorPlayfield catchPlayfield` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.PositionRange

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetFlippedPosition(float)` | 1 | 2/2 | 1 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Edit.Blueprints.BananaShowerPlacementBlueprint

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `IsValidForPlacement` | 3 | 0/6 | 0 | 3 | 0.0% |
| `OnMouseDown(MouseDownEvent)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `UpdateTimeAndPosition(Vector2, double)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`IsValidForPlacement`**

- `Edit/Blueprints/BananaShowerPlacementBlueprint.cs:22` `base.IsValidForPlacement` — chưa được đánh giá
- `Edit/Blueprints/BananaShowerPlacementBlueprint.cs:22` `PlacementActive == PlacementState.Waiting` — chưa được đánh giá
- `Edit/Blueprints/BananaShowerPlacementBlueprint.cs:22` `Precision.DefinitelyBigger(HitObject.Duration, 0)` — chưa được đánh giá

**`OnMouseDown(MouseDownEvent)`**

- `Edit/Blueprints/BananaShowerPlacementBlueprint.cs:48` `e.Button != MouseButton.Left` — chưa được đánh giá
- `Edit/Blueprints/BananaShowerPlacementBlueprint.cs:54` `e.Button != MouseButton.Right` — chưa được đánh giá

**`UpdateTimeAndPosition(Vector2, double)`**

- `Edit/Blueprints/BananaShowerPlacementBlueprint.cs:69` `result.Time is double time` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.FruitPlacementBlueprint

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `OnMouseDown(MouseDownEvent)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `UpdateTimeAndPosition(Vector2, double)` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`OnMouseDown(MouseDownEvent)`**

- `Edit/Blueprints/FruitPlacementBlueprint.cs:39` `e.Button != MouseButton.Left` — chưa được đánh giá

**`UpdateTimeAndPosition(Vector2, double)`**

- `Edit/Blueprints/FruitPlacementBlueprint.cs:51` `distanceSnapResult != null` — chưa được đánh giá
- `Edit/Blueprints/FruitPlacementBlueprint.cs:51` `Vector2.Distance(gridSnapResult.ScreenSpacePosition, distanceSnapResult.ScreenSpacePosition) < CatchHitObjectComposer.DISTANCE_SNAP_RADIUS` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.FruitSelectionBlueprint

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Update()` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Update()`**

- `Edit/Blueprints/FruitSelectionBlueprint.cs:23` `IsSelected` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.JuiceStreamPlacementBlueprint

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `IsValidForPlacement` | 3 | 0/6 | 0 | 3 | 0.0% |
| `OnMouseDown(MouseDownEvent)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `Update()` | 1 | 0/2 | 0 | 1 | 0.0% |
| `UpdateTimeAndPosition(Vector2, double)` | 4 | 0/8 | 0 | 4 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`IsValidForPlacement`**

- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:28` `base.IsValidForPlacement` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:28` `PlacementActive == PlacementState.Waiting` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:28` `Precision.DefinitelyBigger(HitObject.Duration, 0)` — chưa được đánh giá

**`OnMouseDown(MouseDownEvent)`**

- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:62` `e.Button != MouseButton.Left` — chưa được đánh giá

**`Update()`**

- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:44` `PlacementActive == PlacementState.Active` — chưa được đánh giá

**`UpdateTimeAndPosition(Vector2, double)`**

- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:92` `distanceSnapResult != null` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:92` `Vector2.Distance(gridSnapResult.ScreenSpacePosition, distanceSnapResult.ScreenSpacePosition) < CatchHitObjectComposer.DISTANCE_SNAP_RADIUS` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:100` `result.Time is double snappedTime` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamPlacementBlueprint.cs:117` `lastEditablePathId != editablePath.PathId` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.JuiceStreamSelectionBlueprint

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `OnKeyDown(KeyDownEvent)` | 4 | 0/8 | 0 | 4 | 0.0% |
| `OnMouseDown(MouseDownEvent)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `Update()` | 3 | 0/6 | 0 | 3 | 0.0% |
| `convertToStream()` | 6 | 0/12 | 0 | 6 | 0.0% |
| `onDefaultsApplied(HitObject)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`OnKeyDown(KeyDownEvent)`**

- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:132` `IsSelected` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:135` `e.Key == Key.F` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:135` `e.ControlPressed` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:135` `e.ShiftPressed` — chưa được đánh giá

**`OnMouseDown(MouseDownEvent)`**

- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:113` `IsSelected` — chưa được đánh giá

**`Update()`**

- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:87` `IsSelected` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:89` `editablePath.PathId != lastEditablePathId` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:97` `pathCache.IsValid` — chưa được đánh giá

**`convertToStream()`**

- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:197` `editorBeatmap == null` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:197` `beatDivisor == null` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:208` `Precision.DefinitelyBigger(time, HitObject.GetEndTime(), 1)` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:215` `positionWithRepeats % 2 >= 1` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:224` `i == 0` — chưa được đánh giá
- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:224` `HitObject.NewCombo` — chưa được đánh giá

**`onDefaultsApplied(HitObject)`**

- `Edit/Blueprints/JuiceStreamSelectionBlueprint.cs:149` `lastSliderPathVersion != HitObject.Path.Version.Value` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.Components.EditablePath

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `InitializeFromHitObject(JuiceStream)` | 3 | 0/6 | 0 | 3 | 0.0% |
| `MoveSelectedVertices(double, float)` | 8 | 0/16 | 0 | 8 | 0.0% |
| `RemoveVertex(int)` | 4 | 0/8 | 0 | 4 | 0.0% |
| `UpdateFrom(ScrollingHitObjectContainer, JuiceStream)` | 3 | 0/6 | 0 | 3 | 0.0% |
| `UpdateHitObjectFromPath(JuiceStream)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `correctFixedVertexPositions()` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`InitializeFromHitObject(JuiceStream)`**

- `Edit/Blueprints/Components/EditablePath.cs:81` `p.Type != null` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:81` `p.Type != PathType.LINEAR` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:81` `sliderPath.ControlPoints.Any(p => p.Type != null && p.Type != PathType.LINEAR)` — chưa được đánh giá

**`MoveSelectedVertices(double, float)`**

- `Edit/Blueprints/Components/EditablePath.cs:177` `i < vertexCount` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:181` `state.IsSelected` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:189` `i < vertexCount` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:191` `vertexStates[i].IsSelected` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:191` `vertexStates[i].IsFixed` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:196` `i < vertexCount` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:198` `vertexStates[i].IsSelected` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:198` `vertexStates[i].IsFixed` — chưa được đánh giá

**`RemoveVertex(int)`**

- `Edit/Blueprints/Components/EditablePath.cs:150` `index < 0` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:150` `index >= path.Vertices.Count` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:153` `vertexStates[index].IsFixed` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:159` `vertexStates.Count == 0` — chưa được đánh giá

**`UpdateFrom(ScrollingHitObjectContainer, JuiceStream)`**

- `Edit/Blueprints/Components/EditablePath.cs:58` `path.Vertices.Count < InternalChildren.Count` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:61` `InternalChildren.Count < path.Vertices.Count` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:66` `i < VertexCount` — chưa được đánh giá

**`UpdateHitObjectFromPath(JuiceStream)`**

- `Edit/Blueprints/Components/EditablePath.cs:120` `beatSnapProvider == null` — chưa được đánh giá

**`correctFixedVertexPositions()`**

- `Edit/Blueprints/Components/EditablePath.cs:208` `i < VertexCount` — chưa được đánh giá
- `Edit/Blueprints/Components/EditablePath.cs:210` `vertexStates[i].IsFixed` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.Components.NestedOutlineContainer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `UpdateNestedObjectsFrom(ScrollingHitObjectContainer, CatchHitObject)` | 4 | 0/8 | 0 | 4 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`UpdateNestedObjectsFrom(ScrollingHitObjectContainer, CatchHitObject)`**

- `Edit/Blueprints/Components/NestedOutlineContainer.cs:30` `nestedHitObjects.Count < InternalChildren.Count` — chưa được đánh giá
- `Edit/Blueprints/Components/NestedOutlineContainer.cs:33` `InternalChildren.Count < nestedHitObjects.Count` — chưa được đánh giá
- `Edit/Blueprints/Components/NestedOutlineContainer.cs:36` `i < nestedHitObjects.Count` — chưa được đánh giá
- `Edit/Blueprints/Components/NestedOutlineContainer.cs:42` `hitObject is Droplet` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.Components.PlacementEditablePath

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `AddNewVertex()` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`AddNewVertex()`**

- `Edit/Blueprints/Components/PlacementEditablePath.cs:28` `i < VertexCount` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.Components.ScrollingPath

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `computeTimeXs(JuiceStream)` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`computeTimeXs(JuiceStream)`**

- `Edit/Blueprints/Components/ScrollingPath.cs:54` `sliderVertices.Count == 0` — chưa được đánh giá
- `Edit/Blueprints/Components/ScrollingPath.cs:60` `repeat < hitObject.RepeatCount + 1` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.Components.SelectionEditablePath

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `OnDragStart(DragStartEvent)` | 4 | 0/8 | 0 | 4 | 0.0% |
| `OnMouseDown(MouseDownEvent)` | 6 | 0/12 | 0 | 6 | 0.0% |
| `UpdateHitObjectFromPath(JuiceStream)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `deleteSelectedVertices()` | 2 | 0/4 | 0 | 2 | 0.0% |
| `getContextMenuItems()` | 2 | 0/4 | 0 | 2 | 0.0% |
| `getMouseTargetVertex(Vector2)` | 3 | 0/6 | 0 | 3 | 0.0% |
| `selectOnly(int)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`OnDragStart(DragStartEvent)`**

- `Edit/Blueprints/Components/SelectionEditablePath.cs:74` `index == -1` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:74` `VertexStates[index].IsFixed` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:77` `e.Button != MouseButton.Left` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:82` `i < VertexCount` — chưa được đánh giá

**`OnMouseDown(MouseDownEvent)`**

- `Edit/Blueprints/Components/SelectionEditablePath.cs:49` `index == -1` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:49` `VertexStates[index].IsFixed` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:52` `e.Button == MouseButton.Right` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:52` `e.ShiftPressed` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:62` `e.ControlPressed` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:64` `VertexStates[index].IsSelected` — chưa được đánh giá

**`UpdateHitObjectFromPath(JuiceStream)`**

- `Edit/Blueprints/Components/SelectionEditablePath.cs:146` `hitObject.Path.ControlPoints.Count <= 1` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:146` `hitObject.Path.HasValidLengthForPlacement` — chưa được đánh giá

**`deleteSelectedVertices()`**

- `Edit/Blueprints/Components/SelectionEditablePath.cs:131` `i >= 0` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:133` `VertexStates[i].IsSelected` — chưa được đánh giá

**`getContextMenuItems()`**

- `Edit/Blueprints/Components/SelectionEditablePath.cs:117` `selectedCount != 0` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:118` `selectedCount == 1` — chưa được đánh giá

**`getMouseTargetVertex(Vector2)`**

- `Edit/Blueprints/Components/SelectionEditablePath.cs:104` `i >= 0` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:106` `i < VertexCount` — chưa được đánh giá
- `Edit/Blueprints/Components/SelectionEditablePath.cs:106` `InternalChildren[i].ReceivePositionalInputAt(screenSpacePosition)` — chưa được đánh giá

**`selectOnly(int)`**

- `Edit/Blueprints/Components/SelectionEditablePath.cs:123` `i < VertexCount` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.Components.TimeSpanOutline

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `UpdateFrom(ScrollingHitObjectContainer, BananaShower)` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`UpdateFrom(ScrollingHitObjectContainer, BananaShower)`**

- `Edit/Blueprints/Components/TimeSpanOutline.cs:57` `wasEmpty != isEmpty` — chưa được đánh giá
- `Edit/Blueprints/Components/TimeSpanOutline.cs:58` `isEmpty` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Blueprints.Components.VertexPiece

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `updateMarkerDisplay()` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`updateMarkerDisplay()`**

- `Edit/Blueprints/Components/VertexPiece.cs:52` `IsHovered` — chưa được đánh giá
- `Edit/Blueprints/Components/VertexPiece.cs:52` `state.IsSelected` — chưa được đánh giá
- `Edit/Blueprints/Components/VertexPiece.cs:56` `state.IsFixed` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Edit.Checks.CheckBananaShowerGap

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Run(BeatmapVerifierContext)` | 9 | 18/18 | 9 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Edit.Checks.CheckCatchAbnormalDifficultySettings

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Run(BeatmapVerifierContext)` | 6 | 12/12 | 6 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Mods.CatchModDifficultyAdjust

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ApplySettings(BeatmapDifficulty)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `ExtendedIconInformation.get` | 5 | 9/10 | 4 | 0 | 90.0% |
| `SettingDescription.get` | 3 | 6/6 | 3 | 0 | 100.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`ExtendedIconInformation.get`**

- `Mods/CatchModDifficultyAdjust.cs:50` `DrainRate.IsDefault` — thiếu `true`

</details>

### osu.Game.Rulesets.Catch.Mods.CatchModFlashlight.CatchFlashlight

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetComboScaleFor(int)` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`GetComboScaleFor(int)`**

- `Mods/CatchModFlashlight.cs:59` `combo >= 200` — chưa được đánh giá
- `Mods/CatchModFlashlight.cs:61` `combo >= 100` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Mods.CatchModHidden

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ApplyNormalVisibilityState(DrawableHitObject, ArmedState)` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`ApplyNormalVisibilityState(DrawableHitObject, ArmedState)`**

- `Mods/CatchModHidden.cs:36` `hitObject is DrawableCatchHitObject catchDrawable` — chưa được đánh giá
- `Mods/CatchModHidden.cs:39` `catchDrawable.NestedHitObjects.Any()` — chưa được đánh giá
- `Mods/CatchModHidden.cs:43` `nestedDrawable is DrawableCatchHitObject nestedCatchDrawable` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Mods.CatchModMovingFast

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ApplyToPlayer(Player)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`ApplyToPlayer(Player)`**

- `Mods/CatchModMovingFast.cs:38` `drawableRuleset.HasReplayLoaded.Value` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Mods.CatchModMovingFast.InvertDashInputHelper

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `OnReleased(KeyBindingReleaseEvent<CatchAction>)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`OnReleased(KeyBindingReleaseEvent<CatchAction>)`**

- `Mods/CatchModMovingFast.cs:76` `e.Action == CatchAction.Dash` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Mods.CatchModNoScope

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Update(Playfield)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Update(Playfield)`**

- `Mods/CatchModNoScope.cs:28` `shouldAlwaysShowCatcher` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Mods.CatchModRelax

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ApplyToPlayer(Player)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`ApplyToPlayer(Player)`**

- `Mods/CatchModRelax.cs:35` `drawableRuleset.HasReplayLoaded.Value` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Mods.CatchModSynesthesia

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ApplyToBeatmap(IBeatmap)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `ApplyToDrawableHitObject(DrawableHitObject)` | 4 | 0/8 | 0 | 4 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`ApplyToBeatmap(IBeatmap)`**

- `Mods/CatchModSynesthesia.cs:27` `currentBeatmap != beatmap` — chưa được đánh giá

**`ApplyToDrawableHitObject(DrawableHitObject)`**

- `Mods/CatchModSynesthesia.cs:33` `currentBeatmap == null` — chưa được đánh giá
- `Mods/CatchModSynesthesia.cs:40` `d.HitObject is not Banana` — chưa được đánh giá
- `Mods/CatchModSynesthesia.cs:46` `d.HitObject is Droplet` — chưa được đánh giá
- `Mods/CatchModSynesthesia.cs:55` `timingBasedColour != null` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Objects.Banana.BananaHitSampleInfo

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Equals(object?)` | 2 | 3/4 | 1 | 0 | 75.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Equals(object?)`**

- `Objects/Banana.cs:81` `Equals(other)` — thiếu `false`

</details>

### osu.Game.Rulesets.Catch.Objects.BananaShower

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `createBananas(CancellationToken)` | 3 | 6/6 | 3 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Objects.CatchHitObject

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `UpdateComboInformation(IHasComboInformation?)` | 4 | 8/8 | 4 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Objects.JuiceStream

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `CreateNestedHitObjects(CancellationToken)` | 4 | 8/8 | 4 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Objects.JuiceStreamPath

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ComputeRequiredVelocity()` | 2 | 4/4 | 2 | 0 | 100.0% |
| `ConvertFromSliderPath(SliderPath, double)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `ConvertToSliderPath(SliderPath, float, double)` | 4 | 8/8 | 4 | 0 | 100.0% |
| `InsertVertex(double)` | 1 | 2/2 | 1 | 0 | 100.0% |
| `RemoveVertices(Func<JuiceStreamPathVertex, int, bool>)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `ResampleVertices(IEnumerable<double>)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `SetVertexPosition(int, float)` | 3 | 6/6 | 3 | 0 | 100.0% |
| `positionAtTime(double, int)` | 3 | 5/6 | 2 | 0 | 83.3% |
| `vertexIndexAtTime(double)` | 1 | 1/2 | 0 | 0 | 50.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`positionAtTime(double, int)`**

- `Objects/JuiceStreamPath.cs:310` `Precision.AlmostEquals(duration, 0)` — thiếu `true`

**`vertexIndexAtTime(double)`**

- `Objects/JuiceStreamPath.cs:296` `i < 0` — thiếu `false`

</details>

### osu.Game.Rulesets.Catch.Objects.JuiceStreamPathVertex

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `CompareTo(JuiceStreamPathVertex)` | 1 | 2/2 | 1 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Objects.Drawables.DrawableBanana

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Update()` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Update()`**

- `Objects/Drawables/DrawableBanana.cs:71` `Result.IsHit` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Objects.Drawables.DrawableCatchHitObject

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `CheckForResult(bool, double)` | 4 | 0/8 | 0 | 4 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`CheckForResult(bool, double)`**

- `Objects/Drawables/DrawableCatchHitObject.cs:63` `CheckPosition == null` — chưa được đánh giá
- `Objects/Drawables/DrawableCatchHitObject.cs:65` `timeOffset >= 0` — chưa được đánh giá
- `Objects/Drawables/DrawableCatchHitObject.cs:65` `Result != null` — chưa được đánh giá
- `Objects/Drawables/DrawableCatchHitObject.cs:67` `CheckPosition.Invoke(HitObject)` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Objects.Drawables.NestedFruitContainer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Compare(Drawable, Drawable)` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Compare(Drawable, Drawable)`**

- `Objects/Drawables/NestedFruitContainer.cs:19` `x is not DrawableCatchHitObject xObj` — chưa được đánh giá
- `Objects/Drawables/NestedFruitContainer.cs:19` `y is not DrawableCatchHitObject yObj` — chưa được đánh giá
- `Objects/Drawables/NestedFruitContainer.cs:23` `result == 0` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Replays.CatchAutoGenerator

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GenerateFrames()` | 10 | 18/20 | 8 | 0 | 90.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`GenerateFrames()`**

- `Replays/CatchAutoGenerator.cs:53` `lastPosition - halfCatcherWidth < h.EffectiveX` — thiếu `false`
- `Replays/CatchAutoGenerator.cs:105` `nestedObj is PalpableCatchHitObject palpableNestedObject` — thiếu `false`

</details>

### osu.Game.Rulesets.Catch.Replays.CatchReplayFrame

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `.ctor(double, float?, bool, CatchReplayFrame?)` | 4 | 8/8 | 4 | 0 | 100.0% |
| `FromLegacy(LegacyReplayFrame, IBeatmap, ReplayFrame?)` | 4 | 8/8 | 4 | 0 | 100.0% |
| `IsEquivalentTo(ReplayFrame)` | 5 | 10/10 | 5 | 0 | 100.0% |
| `ToLegacy(IBeatmap)` | 1 | 2/2 | 1 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Scoring.CatchHealthProcessor

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `CheckDefaultFailCondition(JudgementResult)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `EnumerateTopLevelHitObjects()` | 3 | 5/6 | 2 | 0 | 83.3% |

<details><summary>Điều kiện còn thiếu</summary>

**`EnumerateTopLevelHitObjects()`**

- `Scoring/CatchHealthProcessor.cs:21` `h is Banana` — thiếu `true`

</details>

### osu.Game.Rulesets.Catch.Scoring.CatchScoreMultiplierCalculator

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `.ctor(ScoreMultiplierContext)` | 3 | 4/6 | 1 | 0 | 66.7% |
| `classicMultiplier(ScoreInfo?)` | 2 | 4/4 | 2 | 0 | 100.0% |
| `rateAdjustMultiplier(double)` | 1 | 2/2 | 1 | 0 | 100.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`.ctor(ScoreMultiplierContext)`**

- `Scoring/CatchScoreMultiplierCalculator.cs:27` `hardRock.UsesDefaultConfiguration` — thiếu `false`
- `Scoring/CatchScoreMultiplierCalculator.cs:32` `hidden.UsesDefaultConfiguration` — thiếu `false`

</details>

### osu.Game.Rulesets.Catch.Scoring.CatchScoreProcessor

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ComputeTotalScore(double, double, double)` | 1 | 2/2 | 1 | 0 | 100.0% |
| `RankFromScore(double, IReadOnlyDictionary<HitResult, int>)` | 5 | 10/10 | 5 | 0 | 100.0% |
| `Reset(bool)` | 1 | 2/2 | 1 | 0 | 100.0% |

_Mọi điều kiện đã phủ đủ cả true và false._

### osu.Game.Rulesets.Catch.Skinning.Argon.ArgonDropletPiece

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Update()` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Update()`**

- `Skinning/Argon/ArgonDropletPiece.cs:111` `i < layers.Count` — chưa được đánh giá
- `Skinning/Argon/ArgonDropletPiece.cs:117` `i % 2 == 1` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Argon.ArgonFruitPiece

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `LoadComplete()` | 1 | 0/2 | 0 | 1 | 0.0% |
| `Update()` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`LoadComplete()`**

- `Skinning/Argon/ArgonFruitPiece.cs:102` `RNG.NextBool()` — chưa được đánh giá

**`Update()`**

- `Skinning/Argon/ArgonFruitPiece.cs:109` `i < layers.Count` — chưa được đánh giá
- `Skinning/Argon/ArgonFruitPiece.cs:117` `i % 2 == 1` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Argon.ArgonHitExplosion

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Animate(HitExplosionEntry)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Animate(HitExplosionEntry)`**

- `Skinning/Argon/ArgonHitExplosion.cs:100` `entry.HitObject is Droplet` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Argon.CatchArgonSkinTransformer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetDrawableComponent(ISkinComponentLookup)` | 4 | 0/8 | 0 | 4 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`GetDrawableComponent(ISkinComponentLookup)`**

- `Skinning/Argon/CatchArgonSkinTransformer.cs:25` `containerLookup.Ruleset == null` — chưa được đánh giá
- `Skinning/Argon/CatchArgonSkinTransformer.cs:43` `comboCounter != null` — chưa được đánh giá
- `Skinning/Argon/CatchArgonSkinTransformer.cs:51` `hitError is ColourHitErrorMeter colourHitError` — chưa được đánh giá
- `Skinning/Argon/CatchArgonSkinTransformer.cs:60` `hitError2 is ColourHitErrorMeter colourHitError2` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Default.CatchHitObjectPiece

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `LoadComplete()` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`LoadComplete()`**

- `Skinning/Default/CatchHitObjectPiece.cs:43` `hyper.NewValue` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Default.CatchTrianglesSkinTransformer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetDrawableComponent(ISkinComponentLookup)` | 4 | 0/8 | 0 | 4 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`GetDrawableComponent(ISkinComponentLookup)`**

- `Skinning/Default/CatchTrianglesSkinTransformer.cs:28` `containerLookup.Ruleset == null` — chưa được đánh giá
- `Skinning/Default/CatchTrianglesSkinTransformer.cs:47` `spectatorList != null` — chưa được đánh giá
- `Skinning/Default/CatchTrianglesSkinTransformer.cs:56` `hitError is ColourHitErrorMeter colourHitError` — chưa được đánh giá
- `Skinning/Default/CatchTrianglesSkinTransformer.cs:65` `hitError2 is ColourHitErrorMeter colourHitError2` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Default.PulpFormation

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `AddPulp(Vector2, Vector2)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `Clear()` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`AddPulp(Vector2, Vector2)`**

- `Skinning/Default/PulpFormation.cs:45` `pulpsInUse == InternalChildren.Count` — chưa được đánh giá

**`Clear()`**

- `Skinning/Default/PulpFormation.cs:38` `i < pulpsInUse` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Legacy.CatchLegacySkinTransformer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `GetConfig(TLookup)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `GetDrawableComponent(ISkinComponentLookup)` | 15 | 0/30 | 0 | 15 | 0.0% |
| `IsProvidingLegacyResources` | 2 | 0/4 | 0 | 2 | 0.0% |
| `hasNewStyleCatcherSprite()` | 2 | 0/4 | 0 | 2 | 0.0% |
| `hasOldStyleCatcherSprite()` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`GetConfig(TLookup)`**

- `Skinning/Legacy/CatchLegacySkinTransformer.cs:174` `result == null` — chưa được đánh giá

**`GetDrawableComponent(ISkinComponentLookup)`**

- `Skinning/Legacy/CatchLegacySkinTransformer.cs:37` `containerLookup.Ruleset == null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:41` `IsProvidingLegacyResources` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:56` `keyCounter != null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:64` `spectatorList != null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:71` `leaderboard != null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:78` `hitError is ColourHitErrorMeter colourHitError` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:109` `hasPear` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:115` `GetTexture("fruit-bananas") != null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:121` `GetTexture("fruit-drop") != null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:129` `version < 2.3m` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:131` `hasOldStyleCatcherSprite()` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:135` `hasNewStyleCatcherSprite()` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:141` `providesComboCounter` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:147` `hasOldStyleCatcherSprite()` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:147` `hasNewStyleCatcherSprite()` — chưa được đánh giá

**`IsProvidingLegacyResources`**

- `Skinning/Legacy/CatchLegacySkinTransformer.cs:17` `base.IsProvidingLegacyResources` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:17` `hasPear` — chưa được đánh giá

**`hasNewStyleCatcherSprite()`**

- `Skinning/Legacy/CatchLegacySkinTransformer.cs:165` `GetTexture(@"fruit-catcher-idle") != null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:166` `GetTexture(@"fruit-catcher-idle-0") != null` — chưa được đánh giá

**`hasOldStyleCatcherSprite()`**

- `Skinning/Legacy/CatchLegacySkinTransformer.cs:161` `GetTexture(@"fruit-ryuuta") != null` — chưa được đánh giá
- `Skinning/Legacy/CatchLegacySkinTransformer.cs:162` `GetTexture(@"fruit-ryuuta-0") != null` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Legacy.LegacyCatchComboCounter

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `UpdateCombo(int, Color4?)` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`UpdateCombo(int, Color4?)`**

- `Skinning/Legacy/LegacyCatchComboCounter.cs:54` `combo == lastDisplayedCombo` — chưa được đánh giá
- `Skinning/Legacy/LegacyCatchComboCounter.cs:63` `(Clock as IGameplayClock)?.IsRewinding == true` — chưa được đánh giá
- `Skinning/Legacy/LegacyCatchComboCounter.cs:72` `combo == 0` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Legacy.LegacyCatchHitObjectPiece

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `LoadComplete()` | 1 | 0/2 | 0 | 1 | 0.0% |
| `SetTexture(Texture?, Texture?)` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`LoadComplete()`**

- `Skinning/Legacy/LegacyCatchHitObjectPiece.cs:82` `hyper.NewValue` — chưa được đánh giá

**`SetTexture(Texture?, Texture?)`**

- `Skinning/Legacy/LegacyCatchHitObjectPiece.cs:90` `colouredSprite.Texture != texture` — chưa được đánh giá
- `Skinning/Legacy/LegacyCatchHitObjectPiece.cs:96` `overlaySprite.Texture != overlayTexture` — chưa được đánh giá
- `Skinning/Legacy/LegacyCatchHitObjectPiece.cs:102` `hyperSprite.Texture != texture` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.Skinning.Legacy.LegacyHitExplosion

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Animate(HitExplosionEntry)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Animate(HitExplosionEntry)`**

- `Skinning/Legacy/LegacyHitExplosion.cs:73` `entry.HitObject is Droplet` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.CatchComboDisplay

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `LoadComplete()` | 2 | 0/4 | 0 | 2 | 0.0% |
| `OnNewResult(DrawableCatchHitObject, JudgementResult)` | 3 | 0/6 | 0 | 3 | 0.0% |
| `OnRevertResult(JudgementResult)` | 2 | 0/4 | 0 | 2 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`LoadComplete()`**

- `UI/CatchComboDisplay.cs:39` `player != null` — chưa được đánh giá
- `UI/CatchComboDisplay.cs:42` `s.NewValue` — chưa được đánh giá

**`OnNewResult(DrawableCatchHitObject, JudgementResult)`**

- `UI/CatchComboDisplay.cs:54` `result.Type.AffectsCombo()` — chưa được đánh giá
- `UI/CatchComboDisplay.cs:54` `result.HasResult` — chưa được đánh giá
- `UI/CatchComboDisplay.cs:57` `result.IsHit` — chưa được đánh giá

**`OnRevertResult(JudgementResult)`**

- `UI/CatchComboDisplay.cs:68` `result.Type.AffectsCombo()` — chưa được đánh giá
- `UI/CatchComboDisplay.cs:68` `result.HasResult` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.CatchPlayfieldAdjustmentContainer

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `load(OsuGame?)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`load(OsuGame?)`**

- `UI/CatchPlayfieldAdjustmentContainer.cs:66` `osuGame != null` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.CatchTouchInputMapper

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `getTouchCatchActionFromInput(Vector2)` | 4 | 0/8 | 0 | 4 | 0.0% |
| `updateAction(object, TouchCatchAction?)` | 3 | 0/6 | 0 | 3 | 0.0% |
| `updatePressedActions()` | 6 | 0/12 | 0 | 6 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`getTouchCatchActionFromInput(Vector2)`**

- `UI/CatchTouchInputMapper.cs:167` `leftDashBox.Contains(screenSpaceInputPosition)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:169` `rightDashBox.Contains(screenSpaceInputPosition)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:171` `leftBox.Contains(screenSpaceInputPosition)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:173` `rightBox.Contains(screenSpaceInputPosition)` — chưa được đánh giá

**`updateAction(object, TouchCatchAction?)`**

- `UI/CatchTouchInputMapper.cs:129` `trackedActionSources.TryGetValue(source, out TouchCatchAction found)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:132` `actionBefore != newAction` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:134` `newAction != null` — chưa được đánh giá

**`updatePressedActions()`**

- `UI/CatchTouchInputMapper.cs:149` `trackedActionSources.ContainsValue(TouchCatchAction.DashLeft)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:149` `trackedActionSources.ContainsValue(TouchCatchAction.MoveLeft)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:154` `trackedActionSources.ContainsValue(TouchCatchAction.DashRight)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:154` `trackedActionSources.ContainsValue(TouchCatchAction.MoveRight)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:159` `trackedActionSources.ContainsValue(TouchCatchAction.DashLeft)` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:159` `trackedActionSources.ContainsValue(TouchCatchAction.DashRight)` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.CatchTouchInputMapper.InputArea

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `updateHighlight()` | 3 | 0/6 | 0 | 3 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`updateHighlight()`**

- `UI/CatchTouchInputMapper.cs:238` `isHandling == isHighlighted` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:242` `isHighlighted` — chưa được đánh giá
- `UI/CatchTouchInputMapper.cs:242` `isHighlighted` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.Catcher

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `ApplyDifficulty(IBeatmapDifficultyInfo?)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `CanCatch(CatchHitObject)` | 3 | 0/6 | 0 | 3 | 0.0% |
| `OnNewResult(DrawableCatchHitObject, JudgementResult)` | 14 | 0/28 | 0 | 14 | 0.0% |
| `OnRevertResult(JudgementResult)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `SetHyperDashState(double, float)` | 4 | 0/8 | 0 | 4 | 0.0% |
| `Speed` | 1 | 0/2 | 0 | 1 | 0.0% |
| `Update()` | 4 | 0/8 | 0 | 4 | 0.0% |
| `clearPlate(DroppedObjectAnimation)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `computePositionInStack(Vector2, float)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `placeCaughtObject(DrawablePalpableCatchHitObject, Vector2)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `runHyperDashStateTransition(bool)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`ApplyDifficulty(IBeatmapDifficultyInfo?)`**

- `UI/Catcher.cs:317` `difficulty != null` — chưa được đánh giá

**`CanCatch(CatchHitObject)`**

- `UI/Catcher.cs:197` `hitObject is PalpableCatchHitObject fruit` — chưa được đánh giá
- `UI/Catcher.cs:201` `fruit.EffectiveX >= X - halfCatchWidth` — chưa được đánh giá
- `UI/Catcher.cs:202` `fruit.EffectiveX <= X + halfCatchWidth` — chưa được đánh giá

**`OnNewResult(DrawableCatchHitObject, JudgementResult)`**

- `UI/Catcher.cs:212` `drawableObject is DrawablePalpableCatchHitObject palpableObject` — chưa được đánh giá
- `UI/Catcher.cs:216` `result.IsHit` — chưa được đánh giá
- `UI/Catcher.cs:220` `CatchFruitOnPlate` — chưa được đánh giá
- `UI/Catcher.cs:223` `hitLighting.Value` — chưa được đánh giá
- `UI/Catcher.cs:227` `result.IsHit` — chưa được đánh giá
- `UI/Catcher.cs:228` `hitObject.Kiai` — chưa được đánh giá
- `UI/Catcher.cs:229` `hitObject is not Banana` — chưa được đánh giá
- `UI/Catcher.cs:232` `palpableObject.HitObject.LastInCombo` — chưa được đánh giá
- `UI/Catcher.cs:234` `result.Judgement is CatchJudgement catchJudgement` — chưa được đánh giá
- `UI/Catcher.cs:234` `catchJudgement.ShouldExplodeFor(result)` — chưa được đánh giá
- `UI/Catcher.cs:241` `hitObject is TinyDroplet` — chưa được đánh giá
- `UI/Catcher.cs:247` `lastHyperDashStartTime != Time.Current` — chưa được đánh giá
- `UI/Catcher.cs:249` `result.IsHit` — chưa được đánh giá
- `UI/Catcher.cs:249` `hitObject.HyperDashTarget is CatchHitObject target` — chưa được đánh giá

**`OnRevertResult(JudgementResult)`**

- `UI/Catcher.cs:268` `HyperDashing != catchResult.CatcherHyperDash` — chưa được đánh giá
- `UI/Catcher.cs:270` `catchResult.CatcherHyperDash` — chưa được đánh giá

**`SetHyperDashState(double, float)`**

- `UI/Catcher.cs:289` `modifier <= 1` — chưa được đánh giá
- `UI/Catcher.cs:289` `X == targetPosition` — chưa được đánh giá
- `UI/Catcher.cs:294` `wasHyperDashing` — chưa được đánh giá
- `UI/Catcher.cs:305` `wasHyperDashing` — chưa được đánh giá

**`Speed`**

- `UI/Catcher.cs:81` `Dashing` — chưa được đánh giá

**`Update()`**

- `UI/Catcher.cs:360` `hyperDashDirection > 0` — chưa được đánh giá
- `UI/Catcher.cs:360` `hyperDashTargetPosition < X` — chưa được đánh giá
- `UI/Catcher.cs:361` `hyperDashDirection < 0` — chưa được đánh giá
- `UI/Catcher.cs:361` `hyperDashTargetPosition > X` — chưa được đánh giá

**`clearPlate(DroppedObjectAnimation)`**

- `UI/Catcher.cs:442` `i < caughtCount` — chưa được đánh giá
- `UI/Catcher.cs:447` `i < caughtCount` — chưa được đánh giá

**`computePositionInStack(Vector2, float)`**

- `UI/Catcher.cs:393` `caughtObjectContainer.Any(f => Vector2Extensions.DistanceSquared(f.Position, position) < checkDistance)` — chưa được đánh giá

**`placeCaughtObject(DrawablePalpableCatchHitObject, Vector2)`**

- `UI/Catcher.cs:372` `caughtObject == null` — chưa được đánh giá
- `UI/Catcher.cs:381` `caughtObject.StaysOnPlate` — chưa được đánh giá

**`runHyperDashStateTransition(bool)`**

- `UI/Catcher.cs:335` `hyperDashing` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.CatcherArea

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `SetCatcherPosition(float)` | 2 | 0/4 | 0 | 2 | 0.0% |
| `UpdateAfterChildren()` | 7 | 0/14 | 0 | 7 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`SetCatcherPosition(float)`**

- `UI/CatcherArea.cs:129` `lastPosition < newPosition` — chưa được đánh giá
- `UI/CatcherArea.cs:131` `lastPosition > newPosition` — chưa được đánh giá

**`UpdateAfterChildren()`**

- `UI/CatcherArea.cs:100` `(Clock as IGameplayClock)?.IsRewinding == true` — chưa được đánh giá
- `UI/CatcherArea.cs:108` `lastHyperDashState` — chưa được đánh giá
- `UI/CatcherArea.cs:108` `Catcher.HyperDashing` — chưa được đánh giá
- `UI/CatcherArea.cs:111` `Catcher.Dashing` — chưa được đánh giá
- `UI/CatcherArea.cs:111` `Catcher.HyperDashing` — chưa được đánh giá
- `UI/CatcherArea.cs:115` `Time.Current - CatcherTrails.LastDashTrailTime >= trail_generation_interval` — chưa được đánh giá
- `UI/CatcherArea.cs:116` `Catcher.HyperDashing` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.CatcherTrailDisplay

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `Dispose(bool)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`Dispose(bool)`**

- `UI/CatcherTrailDisplay.cs:153` `skin.IsNotNull()` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.DrawableCatchRuleset

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `load()` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`load()`**

- `UI/DrawableCatchRuleset.cs:40` `Mods.Any(m => m is ModRelax)` — chưa được đánh giá

</details>

### osu.Game.Rulesets.Catch.UI.HitExplosion

| Method | Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |
|---|---|---|---|---|---|
| `OnApply(HitExplosionEntry)` | 1 | 0/2 | 0 | 1 | 0.0% |
| `apply(HitExplosionEntry?)` | 1 | 0/2 | 0 | 1 | 0.0% |

<details><summary>Điều kiện còn thiếu</summary>

**`OnApply(HitExplosionEntry)`**

- `UI/HitExplosion.cs:32` `IsLoaded` — chưa được đánh giá

**`apply(HitExplosionEntry?)`**

- `UI/HitExplosion.cs:44` `entry == null` — chưa được đánh giá

</details>

