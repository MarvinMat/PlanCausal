using Core.Abstraction.Domain.Processes;
using Planner.Abstraction;
using ProcessSim.Abstraction.Domain.Interfaces;

namespace Controller.Abstraction
{
    public interface IController
    {
        void Execute(TimeSpan duration);
    }
}
