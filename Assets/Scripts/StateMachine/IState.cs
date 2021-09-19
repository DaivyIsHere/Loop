public interface IState
{
    string Name { get; }
    void OnEnter();
    void OnExit();
    void Tick();
}
