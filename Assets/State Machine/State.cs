using UnityEngine;

namespace Spellplague.AI
{
    public class State : MonoBehaviour, IState
    {
        public StateMachine StateBrain { get; set; }

        public virtual void Enter(){}
        public virtual void Tick(){}
        public virtual void Exit(){}
    }
}