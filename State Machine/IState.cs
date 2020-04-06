namespace Spellplague.AI
{
    public interface IState
    {
        StateMachine StateBrain { get; set; }
        void Enter();
        void Tick();
        void Exit();
    }
}