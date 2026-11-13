// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Screens.Edit
{
    [TestFixture]
    public class EditorChangeHandlerTest
    {
        private FakeChangeHandler handler = null!;

        [SetUp]
        public void SetUp() => handler = new FakeChangeHandler();

        // CF: EditorChangeHandler.UpdateState | isRestoring | FALSE
        // CF: EditorChangeHandler.UpdateState | savedStates.Count > 0 && newState.SequenceEqual(...) | FALSE
        // CF: EditorChangeHandler.UpdateState | currentState < savedStates.Count - 1 | FALSE
        // CF: EditorChangeHandler.UpdateState | savedStates.Count > MAX_SAVED_STATES | FALSE
        [Test]
        public void TestInitialStateSavedWithoutUndo()
        {
            int changes = 0;
            handler.OnStateChange += () => changes++;

            handler.SaveState();

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(handler.CanUndo.Value, Is.False);
            Assert.That(handler.CanRedo.Value, Is.False);
        }

        // CF: EditorChangeHandler.UpdateState | savedStates.Count > 0 | TRUE
        // CF: EditorChangeHandler.UpdateState | newState.SequenceEqual(savedStates[currentState]) | TRUE
        [Test]
        public void TestIdenticalStateNotPushed()
        {
            int changes = 0;
            handler.SaveState();
            handler.OnStateChange += () => changes++;

            handler.SaveState();

            Assert.That(changes, Is.EqualTo(0));
        }

        // CF: EditorChangeHandler.UpdateState | newState.SequenceEqual(savedStates[currentState]) | FALSE
        [Test]
        public void TestChangedStateEnablesUndo()
        {
            handler.SaveState();
            handler.Value = 1;
            handler.SaveState();

            Assert.That(handler.CanUndo.Value, Is.True);
            Assert.That(handler.CanRedo.Value, Is.False);
        }

        // CF: EditorChangeHandler.RestoreState | TransactionActive | TRUE
        [Test]
        public void TestRestoreIgnoredDuringTransaction()
        {
            handler.SaveState();
            handler.Value = 1;
            handler.SaveState();

            handler.BeginChange();
            handler.RestoreState(-1);

            Assert.That(handler.Applied, Is.Empty);
        }

        // CF: EditorChangeHandler.RestoreState | TransactionActive | FALSE
        // CF: EditorChangeHandler.RestoreState | savedStates.Count == 0 | TRUE
        [Test]
        public void TestRestoreWithNoStatesIgnored()
        {
            handler.RestoreState(-1);
            Assert.That(handler.Applied, Is.Empty);
        }

        // CF: EditorChangeHandler.RestoreState | savedStates.Count == 0 | FALSE
        // CF: EditorChangeHandler.RestoreState | currentState == newState | TRUE
        [Test]
        public void TestRestoreClampedAtBoundsIgnored()
        {
            handler.SaveState();
            handler.Value = 1;
            handler.SaveState();

            handler.RestoreState(1);
            handler.RestoreState(10);

            Assert.That(handler.Applied, Is.Empty);
        }

        // CF: EditorChangeHandler.RestoreState | currentState == newState | FALSE
        [Test]
        public void TestUndoThenRedo()
        {
            handler.SaveState();
            handler.Value = 1;
            handler.SaveState();

            int changes = 0;
            handler.OnStateChange += () => changes++;

            handler.RestoreState(-1);
            Assert.That(handler.Applied, Has.Count.EqualTo(1));
            Assert.That(handler.Applied[0], Is.EqualTo((0, 1)).Or.EqualTo((1, 0)));
            Assert.That(handler.CanUndo.Value, Is.False);
            Assert.That(handler.CanRedo.Value, Is.True);
            Assert.That(changes, Is.EqualTo(1));

            handler.RestoreState(1);
            Assert.That(handler.CanUndo.Value, Is.True);
            Assert.That(handler.CanRedo.Value, Is.False);
            Assert.That(handler.Applied, Has.Count.EqualTo(2));
        }

        // CF: EditorChangeHandler.UpdateState | isRestoring | TRUE
        [Test]
        public void TestSaveStateDuringRestoreIgnored()
        {
            handler.SaveState();
            handler.Value = 1;
            handler.SaveState();

            handler.OnApply = handler.SaveState;
            handler.RestoreState(-1);

            Assert.That(handler.CanRedo.Value, Is.True);
            Assert.That(handler.CanUndo.Value, Is.False);
        }

        // CF: EditorChangeHandler.UpdateState | currentState < savedStates.Count - 1 | TRUE
        [Test]
        public void TestNewChangeAfterUndoDropsRedoHistory()
        {
            handler.SaveState();
            handler.Value = 1;
            handler.SaveState();
            handler.RestoreState(-1);

            handler.Value = 2;
            handler.SaveState();

            Assert.That(handler.CanRedo.Value, Is.False);
            Assert.That(handler.CanUndo.Value, Is.True);
        }

        // CF: EditorChangeHandler.UpdateState | savedStates.Count > MAX_SAVED_STATES | TRUE
        [Test]
        public void TestHistoryIsCapped()
        {
            handler.SaveState();

            for (int i = 1; i <= EditorChangeHandler.MAX_SAVED_STATES + 10; i++)
            {
                handler.Value = i;
                handler.SaveState();
            }

            int undos = 0;

            while (handler.CanUndo.Value)
            {
                handler.RestoreState(-1);
                undos++;
            }

            Assert.That(undos, Is.EqualTo(EditorChangeHandler.MAX_SAVED_STATES));
        }

        // CF: EditorChangeHandler.BeginChange | savedStates.Count == 0 | TRUE
        // CF: TransactionalCommitComponent.BeginChange | bulkChangesStarted++ == 0 | TRUE
        // CF: TransactionalCommitComponent.BeginChange | bulkChangesStarted++ == 0 | FALSE
        [Test]
        public void TestBeginChangeSavesInitialStateAndRaisesEventOnce()
        {
            int began = 0;
            handler.TransactionBegan += () => began++;

            handler.BeginChange();
            handler.BeginChange();

            Assert.That(began, Is.EqualTo(1));
            Assert.That(handler.TransactionActive, Is.True);
            Assert.That(handler.CurrentStateHash, Is.Not.Empty);
        }

        // CF: EditorChangeHandler.BeginChange | savedStates.Count == 0 | FALSE
        // CF: TransactionalCommitComponent.EndChange | bulkChangesStarted == 0 | FALSE
        // CF: TransactionalCommitComponent.EndChange | --bulkChangesStarted == 0 | TRUE
        // CF: TransactionalCommitComponent.EndChange | --bulkChangesStarted == 0 | FALSE
        [Test]
        public void TestNestedTransactionCommitsOnOutermostEnd()
        {
            int ended = 0;
            handler.TransactionEnded += () => ended++;
            handler.SaveState();

            handler.BeginChange();
            handler.BeginChange();
            handler.Value = 5;
            handler.EndChange();

            Assert.That(ended, Is.EqualTo(0));
            Assert.That(handler.CanUndo.Value, Is.False);

            handler.EndChange();
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(handler.CanUndo.Value, Is.True);
            Assert.That(handler.TransactionActive, Is.False);
        }

        // CF: TransactionalCommitComponent.EndChange | bulkChangesStarted == 0 | TRUE
        [Test]
        public void TestEndChangeWithoutBeginThrows()
        {
            Assert.Throws<InvalidOperationException>(() => handler.EndChange());
        }

        // CF: TransactionalCommitComponent.SaveState | bulkChangesStarted > 0 | TRUE
        // CF: TransactionalCommitComponent.SaveState | bulkChangesStarted > 0 | FALSE
        [Test]
        public void TestSaveStateSuppressedInsideTransaction()
        {
            int triggered = 0;
            handler.SaveStateTriggered += () => triggered++;

            handler.BeginChange(); // raises once for the initial state, before the transaction becomes active.
            int afterBegin = triggered;
            handler.SaveState();
            Assert.That(triggered, Is.EqualTo(afterBegin));

            handler.EndChange();
            handler.SaveState();
            Assert.That(triggered, Is.EqualTo(afterBegin + 1));
        }

        [Test]
        public void TestStateHashDiffersBetweenStates()
        {
            string first = handler.CurrentStateHash;
            handler.Value = 3;
            handler.SaveState();
            Assert.That(handler.CurrentStateHash, Is.Not.EqualTo(first));
        }

        private class FakeChangeHandler : EditorChangeHandler
        {
            public int Value;
            public Action? OnApply;
            public readonly List<(int previous, int next)> Applied = new List<(int, int)>();

            protected override void WriteCurrentStateToStream(MemoryStream stream) => stream.Write(BitConverter.GetBytes(Value));

            protected override void ApplyStateChange(byte[] previousState, byte[] newState)
            {
                Applied.Add((BitConverter.ToInt32(previousState), BitConverter.ToInt32(newState)));
                Value = BitConverter.ToInt32(newState);
                OnApply?.Invoke();
            }
        }
    }
}
