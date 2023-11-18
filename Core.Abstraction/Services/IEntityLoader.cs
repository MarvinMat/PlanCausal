namespace Core.Abstraction.Services;

/// <summary>
/// Used to have the typed EntityLoader list in the scenario.
/// </summary>
public interface IEntityLoader {}

public interface IEntityLoader<T> : IEntityLoader
{
    List<T> Load();
}