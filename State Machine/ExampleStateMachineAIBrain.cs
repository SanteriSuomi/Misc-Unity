using Spellplague.Characters;
using UnityEngine;

namespace Spellplague.AI
{
    /// <summary>
    /// Example class on how you could use State Machine and Generic Factory class.
    /// </summary>
    public class ExampleStateMachineAIBrain : Character
    {
        private StateMachine stateMachine;
        private TestState testState;
        private TestSubState testSubState;

        private void Awake()
        {
            // Get the states from in the gameObject.
            testState = GetComponent<TestState>();
            testSubState = GetComponent<TestSubState>();
            // Start the state machine with only a main state.
            stateMachine = new StateMachine(testState, testSubState, true, 16.67f);
        }

        private void Update()
        {
            float frameTime = Time.deltaTime * 1000;
            stateMachine.UpdateRate = frameTime;
            TestStateInputs();
        }

        private void TestStateInputs()
        {
            // States would normally change their states in their own classes.
            if (Input.GetKeyDown(KeyCode.V))
            {
                // Change machine main state.
                //stateMachine.CurrentState = Create<TestState>();
                stateMachine.MainState = testState;
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                // Change state machine substate.
                //stateMachine.SubState = Create<TestSubState>();
                stateMachine.SubState = testSubState;
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                // Start state machine update.
                stateMachine.StartUpdate();

            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                // Stop state machine update.
                stateMachine.StopUpdate();
            }
            else if (Input.GetKeyDown(KeyCode.Comma))
            {
                stateMachine.FallbackState(2);
                //Debug.Log(stateMachine.PeekPreviousState()?.ToString());
                Debug.Log(stateMachine.MainStateStack.Count);
            }
        }

        private void OnDestroy() => stateMachine.Dispose();
    }
}