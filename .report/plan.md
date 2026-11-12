# Plan: Control-Flow unit tests cho `osu.Game.Rulesets.Osu` (Lv3)

## Context
User yêu cầu Lv3 (module) test cho project `osu.Game.Rulesets.Osu` (in scope), thiết kế bằng Control Flow Testing (C2 branch + C3 condition), KHÔNG build/run. Commit HEAD `1442c46184 remove unused projects` đã xoá mọi test project (gồm `osu.Game.Rulesets.Osu.Tests`). User đã chọn: **tạo csproj mới + thêm vào `osu.sln`**, và **batch = toàn bộ class logic thuần**.

## Discovery (đã xong)
- TARGET → `osu.Game.Rulesets.Osu/osu.Game.Rulesets.Osu.csproj` (278 file .cs). Rejected: không có candidate khác cùng level (sln/slnf là Lv4; namespace con không phải project).
- Framework: NUnit 4.6.1 + Moq 4.20.72 (từ csproj gốc `git show HEAD~1:osu.Game.Rulesets.Osu.Tests/osu.Game.Rulesets.Osu.Tests.csproj`). Convention: `[TestFixture]`, `Assert.That(..., Is...)`, `[TestCase]/[TestCaseSource]`, header license 2 dòng, namespace `osu.Game.Rulesets.Osu.Tests[.Sub]`, tên test `TestXxx`.
- Helper tái dùng trong `osu.Game/Tests/`: `Beatmaps/TestBeatmap.cs`, `TestWorkingBeatmap.cs`, `DifficultyCalculatorTest.cs`, `Rulesets/RulesetScoreMultiplierTest.cs`, `Gameplay/TestGameplayState.cs`. `osu.TestProject.props` link `osu.Game/Tests/VisualTestRunner.cs` (còn tồn tại).

## Thay đổi file
1. **Tạo** `osu.Game.Rulesets.Osu.Tests/osu.Game.Rulesets.Osu.Tests.csproj` — copy y nguyên nội dung csproj gốc (HEAD~1).
2. **Sửa** `osu.sln` — thêm `Project(...) = "osu.Game.Rulesets.Osu.Tests", ...` với GUID mới + 4 dòng ProjectConfigurationPlatforms (Debug/Release ActiveCfg/Build.0). `osu.Desktop.slnf` đã liệt kê sẵn path này, không sửa.
3. **Tạo** file test mới, thư mục mirror namespace production. Không sửa production code.
4. State nội bộ (research/inventory) giữ trong scratchpad, không vào repo.

## Batch (theo mật độ control flow, chỉ class logic thuần)
Mỗi class: đọc source → inventory decision/condition → test 1 path/test, comment 1 dòng mục đích.

| # | Nhóm | Class (file test) |
|---|------|-------------------|
| B1 | Scoring | `OsuHitWindows`, `OsuHealthProcessor`, `OsuLegacyHealthProcessor`, `OsuScoreMultiplierCalculatorV1/V2`, `OsuScoreProcessor` → `Scoring/*Test.cs` |
| B2 | Hit policy | `StartTimeOrderedHitPolicy`, `LegacyHitPolicy`, `AnyOrderHitPolicy` (fake `DrawableHitObject` qua constructor nếu không cần DI; nếu cần load → not-unit-testable) → `UI/*Test.cs` |
| B3 | Objects | `Slider`, `OsuHitObject`, `Spinner`, `SliderEndCircle/TailCircle/HeadCircle/Tick`, `SpinnerSpinHistory`, `OsuReplayFrame` → `Objects/*Test.cs`, `Replays/*Test.cs` |
| B4 | Beatmaps/Utils | `OsuBeatmapProcessor` (stacking old/new), `OsuBeatmapConverter`, `OsuBeatmap`, `OsuHitObjectGenerationUtils(.Reposition)` (Random seed cố định) → `Beatmaps/`, `Utils/` |
| B5 | Edit/Checks | `CheckLowDiffOverlaps`, `CheckOffscreenObjects`, `CheckTimeDistanceEquality`, `CheckTooShortSliders/Spinners`, `CheckOsuAbnormalDifficultySettings` → `Editor/Checks/*Test.cs` (dùng `BeatmapVerifierContext` + `TestWorkingBeatmap`) |
| B6 | Difficulty | `OsuDifficultyHitObject`, evaluators (`SnapAim`, `FlowAim`, `Agility`, `Rhythm`, `Speed`, `Reading`, `Flashlight`), skills (`Aim`, `Speed`, `Reading`, `Flashlight`), `OsuPerformanceCalculator`, `OsuLegacyScoreMissCalculator`, `OsuLegacyScoreSimulator`, `LegacyScoreUtils`, `OsuDifficultyAttributes`/`OsuPerformanceAttributes` (ToDatabaseAttributes/FromDatabase) → `Difficulty/*Test.cs`. Assertion theo hướng/quan hệ (monotonic, 0 khi guard, clamp) từ ý nghĩa code, không chép số. |
| B7 | Ruleset & Mods logic | `OsuRuleset` (`ConvertFromLegacyMods`/`ConvertToLegacyMods`/`GetModsFor` switch, `CreateConvertibleReplayFrame`…), `OsuInputManager` non-drawable phần, mods có `ApplyToBeatmap/ApplyToHitObject/ApplyToDifficulty` thuần: `OsuModDifficultyAdjust`, `OsuModRandom` (seed), `OsuModMirror`, `OsuModHardRock`, `OsuModClassic`, `OsuModStrictTracking`, `OsuModTargetPractice` (phần generate beat), `OsuModSpunOut`… → `Mods/*Test.cs` |

## Not unit-testable (liệt kê trong report, không viết test)
Drawable/Skinning/UI/Edit blueprint/HUD/Statistics/Cursor/Popover/ToolboxGroup/ReplayAnalysis (cần game host, DI, scene graph): vd `PathControlPointVisualiser`, `SliderSelectionBlueprint`, `SliderPlacementBlueprint`, `OsuLegacySkinTransformer`, `DrawableSlider`, `SliderInputManager`, `OsuSelection*Handler`, `OsuHitObjectComposer`, `CursorTrail`, `OsuTouchInputMapper`, mods drawable-only (`OsuModHidden` transforms, `OsuModRelax`, `OsuModBubbles`…). Liệt kê đầy đủ từng file trong report §4/§5.

## Ràng buộc thực thi
- Không build/run/coverage (prompt người dùng thắng skill). Không spawn agent (user không yêu cầu).
- Input deterministic: `new Random(seed)`, không clock/IO/network.
- Assertion từ intent (tên, doc, test gốc trong git history để hiểu ý định — không copy file cũ).
- Infeasible/blocker ghi rõ lý do + thay đổi tối thiểu đề xuất.

## Verification
Chỉ static self-review: đối chiếu từng test với inventory (C2 outcome, C3 TRUE/FALSE, short-circuit), kiểm namespace/using/member name bằng grep production code. Report theo đúng 6 mục user quy định; ghi rõ test **chưa build, chưa chạy, chưa đo**. Gợi ý user tự chạy: `dotnet test osu.Game.Rulesets.Osu.Tests/osu.Game.Rulesets.Osu.Tests.csproj`.
