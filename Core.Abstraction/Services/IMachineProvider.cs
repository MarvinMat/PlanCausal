using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Services
{
    public interface IMachineProvider
    {
        IEnumerable<Machine> Load();
    }
}
