namespace ProcessSim.Abstraction;

public interface IScenario
{
    protected Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public void Run();
}