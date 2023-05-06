using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace ProcessSim.Abstraction.Domain.Interfaces
{
    public interface ISimulator
    {
        public event EventHandler InterruptEvent;

        /// <summary>
        /// Set the plan to be simulated.
        /// </summary>
        /// <param name="modifiedPlan">The plan to be simulated</param>
        public void SetCurrentPlan(List<WorkOperation> modifiedPlan);

        /// <summary>
        /// Create the simulation representation of a resource so that the resource can be used in the simulation.
        /// </summary>
        /// <param name="resource">A resource to be used in the simulation.</param>
        public void CreateSimulationResource(IResource resource);

        /// <summary>
        /// Create the simulation representation of multiple resources so that these resources can be used in the simulation.
        /// </summary>
        /// <param name="resources">A list of resources to be used in the simulation.</param>
        public void CreateSimulationResources(IEnumerable<IResource> resources);
        
        /// <summary>
        /// Start the simulation. The simulation resources and the plan to simulate should be set before calling this method.
        /// </summary>
        /// <param name="duration">The time span over which the simulations is going to be run.</param>
        public void Start(TimeSpan duration);

        /// <summary>
        /// Continue the simulation after it paused.
        /// </summary>
        public void Continue();
    }
}
