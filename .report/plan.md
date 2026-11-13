# Plan: Lv4 Control-Flow unit tests for osu.sln

## Context
Branch `testing-solution`, HEAD `1442c46184 remove unused projects` deleted every test project. TARGET `osu.sln` (Lv4) resolves to 6 production projects, all in scope: osu.Game, osu.Game.Tournament, osu.Game.Rulesets.{Osu,Catch,Taiko,Mania}. No rejected candidates (`*.slnf` are filters, not the full solution).
User decisions: build, run tests, coverage tool allowed; create 6 new test projects AND add to osu.sln; batch scope = ALL unit-testable control-flow classes (~450+ non-UI classes with ≥8 decision points; osu.Game 332, Osu 55, Taiko 32, Catch 27, Tournament 5, Mania TBD).

## Test projects (new, since none exist)
`osu.Game.Tests`, `osu.Game.Tournament.Tests`, `osu.Game.Rulesets.{Osu,Catch,Taiko,Mania}.Tests` — csproj modelled on HEAD^ versions (net10.0, NUnit 4.6.1, NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.0, Moq 4.20.72 only in osu.Game.Tests as before, + coverlet.collector for coverage). Do NOT import `osu.TestProject.props` (visual runner/WinExe not needed for pure unit tests). Add all 6 to osu.sln via `dotnet sln add`.

## Selection rule (unit-testable)
Include: pure logic classes (utils, extensions, parsers/decoders, beatmap converters, difficulty/perf calculators & evaluators, scoring/judgement processors, mods' logic, online API models/requests logic, configs, IO/archive helpers using temp dirs, tournament models). Exclude as not-unit-testable: Drawable/Container/Screen/Overlay classes, anything needing GameHost/Realm DB/audio/GL/network — listed in report with reason.

## Execution (per project, densest classes first)
1. Generate density ranking per project (grep decision tokens, exclude UI) → inventory list in scratchpad.
2. For each class: read source, build control-flow inventory (decisions, atomic conditions, short-circuit), design C2+C3 tests, write NUnit tests mirroring production namespace/path (`osu.Game.Tests/<Area>/<Class>Test.cs`), one control-flow purpose per test name.
3. After each batch (~20 classes): `dotnet build` test project, `dotnet test`, fix failing assertions by re-reading intent (never production code).
4. Coverage: `dotnet test --collect:"XPlat Code Coverage"` per project; use branch report to find unmapped branches; still report C2/C3 by traceability, quote measured numbers as tool output only.
5. State tracking in scratchpad (research.md/plan.md/status.md), never in repo.
   Order: Rulesets (Osu, Taiko, Catch, Mania) → Tournament → osu.Game (largest).

## Report
Exact 6-section format requested; traceability table per class; full list of not-reached/not-unit-testable classes with reasons. Given size, work continues across context compactions until all listed classes done or explicitly reported as blocked.

## Verification
`dotnet build osu.sln`, `dotnet test osu.sln`, coverage via coverlet per test project; `git status` shows only new test projects, test files, osu.sln change.
