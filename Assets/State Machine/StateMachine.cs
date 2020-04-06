using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Spellplague.AI
{
    /// <summary>
    /// Create a new State Machine. Create new states by inheriting from the State class 
    /// and extend it's functionality by overriding the virtual IState methods.
    /// Remember to call dispose on the State Machine when not in use.
    /// </summary>
    public class StateMachine : IDisposable
    {
        #region Main State Stack
        /// <summary>
        /// Main state stack, representing a "memory" or a "history" of main states.
        /// </summary>
        public Stack<IState> MainStateStack { get; set; }

        /// <summary>
        /// Return (check) the state that is previous to the current one. If there is no previous state, return null.
        /// </summary>
        /// <returns></returns>
        public IState PeekPreviousState()
        {
            if (MainStateStack.Count < 2)
            {
                Debug.Log("Main state stack does not have enough states to check the previous state.");
                return null;
            }

            return MainStateStack.ElementAt(MainStateStack.Count - 2);
        }

        /// <summary>
        /// Roll back states from the stack as many times as specified, if there are not enough states to fall back on, do nothing.
        /// </summary>
        public void FallbackState(int amount)
        {
            if (MainStateStack.Count < amount)
            {
                Debug.Log("Main state stack does not have enough states to fall back this amount of times.");
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                MainStateStack.Pop();
            }

            mainState = MainStateStack.Peek();
        }
        #endregion

        #region States
        /// <summary>
        /// Main state of the state machine. This should never be null. Updating main state will also update the stack.
        /// </summary>
        private IState mainState;
        public IState MainState
        {
            get
            {
                return mainState;
            }
            set
            {
                SetStateBrain(value);
                MainStateStack.Push(value);
                mainState?.Exit();
                value?.Enter();
                mainState = value;
            }
        }

        /// <summary>
        /// Sub state of the main state. Not required to be non-null like main state, does not have stack history.
        /// </summary>
        private IState subState;
        public IState SubState
        {
            get
            {
                return subState;
            }
            set
            {
                SetStateBrain(value);
                subState?.Exit();
                value?.Enter();
                subState = value;
            }
        }

        private void SetStateBrain(IState state)
        {
            if (state.StateBrain == null)
            {
                state.StateBrain = this;
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// State machine update rate in milliseconds. Ex. 16.67 equals 60 updates a second.
        /// </summary>
        public float UpdateRate { get; set; }

        /// <summary>
        /// Turn the state machine update on and off. True equals on, false equals off.
        /// </summary>
        public bool UpdateSwitch { get; set; }
        private Task updateTask;

        public void StartUpdate()
        {
            UpdateSwitch = true;
            updateTask = Update();
        }

        public void StopUpdate() => UpdateSwitch = false;

        private async Task Update()
        {
            while (UpdateSwitch)
            {
                mainState?.Tick();
                subState?.Tick();
                await Task.Delay(TimeSpan.FromMilliseconds(UpdateRate));
            }
        }
        #endregion

        #region Constructor
        public StateMachine(IState startingState, IState startingSubState,
               bool initialUpdateState, float startingUpdateRate)
        {
            MainStateStack = new Stack<IState>();
            MainStateStack.Push(startingState);
            mainState = startingState;
            subState = startingSubState;
            UpdateRate = startingUpdateRate;
            if (initialUpdateState)
            {
                UpdateSwitch = true;
                updateTask = Update();
            }

            SetStateBrain(mainState);
            SetStateBrain(subState);
            mainState?.Enter();
            subState?.Enter();
        }
        #endregion

        #region Dispose Pattern
        private bool isDisposed;

        /// <summary>
        /// Call this when stopping the state machine to ensure that the 
        /// update task won't run in the background and is disposed correctly.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~StateMachine() => Dispose(false);

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposed) return;

            if (isDisposing)
            {
                UpdateSwitch = false;
                if (updateTask.Status == TaskStatus.Running)
                {
                    updateTask?.Dispose();
                }
            }

            isDisposed = true;
        }
        #endregion
    }
}