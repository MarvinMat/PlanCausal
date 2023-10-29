namespace Core.Abstraction.Services;

public interface IEntityLoader<T>
{
    List<T> Load();
}