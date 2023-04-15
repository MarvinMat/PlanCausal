using ProcessSimImplementation.Domain;
using SimSharp;
using System.Diagnostics;
using static SimSharp.Distributions;

namespace ProcessSim.Implementation.Core.SimulationModels
{
    internal class WorkOperationModel : ActiveObject<Simulator>
    {
        private readonly WorkOperation _operation;
        private Store _store;
        public WorkOperationModel(Simulator environment, Store store, WorkOperation operation) : base(environment)
        {
            _store = store;
            _operation = operation;
            environment.Process(Producing());
        }

        private IEnumerable<Event> Producing()
        {
            var preemptiveResources = new Dictionary<PreemptiveResource, PreemptiveRequest>();
            foreach (var resourceId in _operation.Resources)
            {
                SimWorkShop.Instance.Resources.TryGetValue(resourceId, out var resource);
                if (resource == null) throw new ArgumentException($"Tried to access a resource with ID {resourceId} that does not exist in the WorkShop.");         

                if (resource is PreemptiveResource preemptiveResource)
                {
                    var req = preemptiveResource.Request();
                    try
                    {
                        preemptiveResources.TryAdd(preemptiveResource, req);
                    }
                    catch (ArgumentNullException ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    yield return req;

                    continue;
                }
                // if (resource is Container containerResource) {
                // }
            }

            var durationDistribution = N(_operation.Duration, TimeSpan.FromMinutes(2));
            var doneIn = Environment.Rand(POS(durationDistribution));
            yield return Environment.Timeout(doneIn);
            //Environment.Log($"Completed work operation {_operation.Name} at {Environment.Now}.");

            preemptiveResources.ToList().ForEach(p => { p.Key.Release(p.Value); });
            _store.Put(new object());
        }

        private IEnumerable<Event> HandleRequest(Request request)
        {
            yield return request;
        }

    }
}