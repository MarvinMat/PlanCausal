using ProcessSimImplementation.Domain;
using SimSharp;
using static SimSharp.Distributions;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    internal class WorkOperationModel : ActiveObject<Simulation>
    {
        private WorkOperation _operation;
        private Store _store;
        public WorkOperationModel(Simulation environment, Store store, WorkOperation operation) : base(environment)
        {
            _store = store;
            _operation = operation;
            environment.Process(Producing());
        }

        private IEnumerable<Event> Producing()
        {
            var preemptiveResources = new List<PreemptiveResource>();
            foreach (var resourceId in _operation.Resources)
            {
                SimWorkShop.Instance.Resources.TryGetValue(resourceId, out var resource);
                if (resource == null) throw new ArgumentException($"Tried to access a resource with ID {resourceId} that does not exist in the WorkShop.");
                if (resource is PreemptiveResource preemptiveResource)
                {
                    preemptiveResources.Add(preemptiveResource);
                    var req = preemptiveResource.Request();
                    yield return req;
                    continue;
                }
                // if (resource is Container containerResource) {
                // }
                
            }
            
            var durationDistribution = N(_operation.Duration, TimeSpan.FromMinutes(2));
            var doneIn = Environment.Rand(POS(durationDistribution));
            yield return Environment.Timeout(doneIn);

            foreach (var resource in preemptiveResources) resource.Release();
        }
    }
}