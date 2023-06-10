using Core.Abstraction.Domain.Models;

namespace Core.Abstraction.Services
{
    public interface IToolProvider
    {
        List<Tool> Load();
    }
}
