namespace ProcessSim.Abstraction.Domain.Interfaces;

public interface IScenario
{
    protected Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public void Run();
}