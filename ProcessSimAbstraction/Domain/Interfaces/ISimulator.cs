using Core.Abstraction.Domain.Resources;

namespace ProcessSim.Abstraction.Domain.Interfaces
{
    public interface ISimulator
    {
        public event EventHandler InterruptEvent;
        public void CreateSimulationResource(IResource resource);
        public void CreateSimulationResources(IEnumerable<IResource> resources);
        public void Start(TimeSpan duration);
    }
}
