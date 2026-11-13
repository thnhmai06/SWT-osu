// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Game.Overlays.Mods;
using osu.Game.Overlays.Mods.Input;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osuTK.Input;

namespace osu.Game.Tests.Overlays.Mods.Input
{
    [TestFixture]
    public class ModHotkeyHandlerTest
    {
        private static KeyDownEvent press(Key key, bool shift = false)
        {
            var state = new InputState();
            if (shift)
                state.Keyboard.Keys.SetPressed(Key.LShift, true);
            return new KeyDownEvent(state, key);
        }

        private static ModState[] states(params Mod[] mods) => mods.Select(m => new ModState(m)).ToArray();

        #region Classic

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | !mod_type_lookup.TryGetValue(e.Key) | TRUE
        [Test]
        public void TestClassicUnmappedKey()
        {
            Assert.That(new ClassicModHotkeyHandler(false).HandleModHotkeyPressed(press(Key.M), states(new OsuModEasy())), Is.False);
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | !mod_type_lookup.TryGetValue(e.Key) | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | matchingMods.Length == 0 | TRUE
        // CF: ClassicModHotkeyHandler.matches | typesToMatch.Any(IsInstanceOfType) | FALSE
        [Test]
        public void TestClassicNoMatchingMod()
        {
            Assert.That(new ClassicModHotkeyHandler(false).HandleModHotkeyPressed(press(Key.Q), states(new OsuModHidden())), Is.False);
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | modState.Visible | FALSE
        [Test]
        public void TestClassicInvisibleModNotMatched()
        {
            var s = states(new OsuModEasy());
            s[0].ValidForSelection.Value = false;
            Assert.That(new ClassicModHotkeyHandler(false).HandleModHotkeyPressed(press(Key.Q), s), Is.False);
            Assert.That(s[0].Active.Value, Is.False);
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | matchingMods.Length == 0 | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | matchingMods.Length == 1 | TRUE
        // CF: ClassicModHotkeyHandler.matches | typesToMatch.Any(IsInstanceOfType) | TRUE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | modState.Visible | TRUE
        [Test]
        public void TestClassicSingleMatchToggles()
        {
            var s = states(new OsuModEasy(), new OsuModHidden());
            var handler = new ClassicModHotkeyHandler(false);

            Assert.That(handler.HandleModHotkeyPressed(press(Key.Q), s), Is.True);
            Assert.That(s[0].Active.Value, Is.True);
            Assert.That(handler.HandleModHotkeyPressed(press(Key.Q), s), Is.True);
            Assert.That(s[0].Active.Value, Is.False);
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | matchingMods.Length == 1 | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | allowIncompatibleSelection | TRUE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | anyActive | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | foreach matchingMods | loop n
        [Test]
        public void TestClassicIncompatibleAllowedTogglesAll()
        {
            var s = states(new OsuModHalfTime(), new OsuModDaycore());
            var handler = new ClassicModHotkeyHandler(true);

            Assert.That(handler.HandleModHotkeyPressed(press(Key.E), s), Is.True);
            Assert.That(s.Select(x => x.Active.Value), Is.All.True);

            // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | anyActive | TRUE
            s[0].Active.Value = false;
            Assert.That(handler.HandleModHotkeyPressed(press(Key.E), s), Is.True);
            Assert.That(s.Select(x => x.Active.Value), Is.All.False);
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | allowIncompatibleSelection | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | currentSelectedIndex < 0 && e.ShiftPressed | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | e.ShiftPressed | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | currentSelectedIndex >= 0 && currentSelectedIndex <= Length - 1 | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | indexToSelect >= 0 && indexToSelect <= Length - 1 | TRUE
        [Test]
        public void TestClassicForwardFromNothingSelectsFirst()
        {
            var s = states(new OsuModHalfTime(), new OsuModDaycore());
            Assert.That(new ClassicModHotkeyHandler(false).HandleModHotkeyPressed(press(Key.E), s), Is.True);
            Assert.That(s.Select(x => x.Active.Value), Is.EqualTo(new[] { true, false }));
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | currentSelectedIndex >= 0 && currentSelectedIndex <= Length - 1 | TRUE
        [Test]
        public void TestClassicForwardCycles()
        {
            var s = states(new OsuModHalfTime(), new OsuModDaycore());
            var handler = new ClassicModHotkeyHandler(false);
            s[0].Active.Value = true;

            handler.HandleModHotkeyPressed(press(Key.E), s);
            Assert.That(s.Select(x => x.Active.Value), Is.EqualTo(new[] { false, true }));
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | indexToSelect >= 0 && indexToSelect <= Length - 1 | FALSE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | indexToSelect >= 0 | TRUE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | indexToSelect <= Length - 1 | FALSE
        [Test]
        public void TestClassicForwardPastEndDeselects()
        {
            var s = states(new OsuModHalfTime(), new OsuModDaycore());
            s[1].Active.Value = true;

            new ClassicModHotkeyHandler(false).HandleModHotkeyPressed(press(Key.E), s);
            Assert.That(s.Select(x => x.Active.Value), Is.All.False);
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | currentSelectedIndex < 0 && e.ShiftPressed | TRUE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | currentSelectedIndex < 0 | TRUE
        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | e.ShiftPressed | TRUE
        [Test]
        public void TestClassicBackwardFromNothingSelectsLast()
        {
            var s = states(new OsuModHalfTime(), new OsuModDaycore());
            new ClassicModHotkeyHandler(false).HandleModHotkeyPressed(press(Key.E, true), s);
            Assert.That(s.Select(x => x.Active.Value), Is.EqualTo(new[] { false, true }));
        }

        // CF: ClassicModHotkeyHandler.HandleModHotkeyPressed | indexToSelect >= 0 | FALSE
        [Test]
        public void TestClassicBackwardPastStartDeselects()
        {
            var s = states(new OsuModHalfTime(), new OsuModDaycore());
            s[0].Active.Value = true;

            new ClassicModHotkeyHandler(false).HandleModHotkeyPressed(press(Key.E, true), s);
            Assert.That(s.Select(x => x.Active.Value), Is.All.False);
        }

        #endregion

        #region Sequential

        // CF: SequentialModHotkeyHandler.Create | modType | case DifficultyReduction
        // CF: SequentialModHotkeyHandler.Create | modType | case DifficultyIncrease
        // CF: SequentialModHotkeyHandler.Create | modType | case Automation
        [Test]
        public void TestSequentialCreateKnownTypes()
        {
            var s = states(new OsuModEasy());
            Assert.That(SequentialModHotkeyHandler.Create(ModType.DifficultyReduction).HandleModHotkeyPressed(press(Key.Q), s), Is.True);
            Assert.That(SequentialModHotkeyHandler.Create(ModType.DifficultyIncrease).HandleModHotkeyPressed(press(Key.A), s), Is.True);
            Assert.That(SequentialModHotkeyHandler.Create(ModType.Automation).HandleModHotkeyPressed(press(Key.Z), s), Is.True);
        }

        // CF: SequentialModHotkeyHandler.Create | modType | default
        [Test]
        public void TestSequentialCreateUnknownType()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SequentialModHotkeyHandler.Create(ModType.Fun));
        }

        // CF: SequentialModHotkeyHandler.HandleModHotkeyPressed | index < 0 | TRUE
        [Test]
        public void TestSequentialUnmappedKey()
        {
            Assert.That(SequentialModHotkeyHandler.Create(ModType.DifficultyReduction).HandleModHotkeyPressed(press(Key.A), states(new OsuModEasy())), Is.False);
        }

        // CF: SequentialModHotkeyHandler.HandleModHotkeyPressed | index < 0 | FALSE
        // CF: SequentialModHotkeyHandler.HandleModHotkeyPressed | modState == null | TRUE
        [Test]
        public void TestSequentialIndexBeyondMods()
        {
            Assert.That(SequentialModHotkeyHandler.Create(ModType.DifficultyReduction).HandleModHotkeyPressed(press(Key.W), states(new OsuModEasy())), Is.False);
        }

        // CF: SequentialModHotkeyHandler.HandleModHotkeyPressed | modState == null | FALSE
        // CF: SequentialModHotkeyHandler.HandleModHotkeyPressed | modState.Visible | TRUE
        [Test]
        public void TestSequentialTogglesNthVisibleMod()
        {
            var s = states(new OsuModEasy(), new OsuModNoFail(), new OsuModHalfTime());
            s[0].MatchingTextFilter.Value = false;

            Assert.That(SequentialModHotkeyHandler.Create(ModType.DifficultyReduction).HandleModHotkeyPressed(press(Key.Q), s), Is.True);
            Assert.That(s.Select(x => x.Active.Value), Is.EqualTo(new[] { false, true, false }));
        }

        #endregion
    }
}
