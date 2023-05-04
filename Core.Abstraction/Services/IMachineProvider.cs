using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Services
{
    public interface IMachineProvider
    {
        List<Machine> Load();
    }
}
