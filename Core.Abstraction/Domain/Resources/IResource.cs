namespace Core.Abstraction.Domain.Resources
{
    public interface IResource
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
    }
}
