//using ProcessSimImplementation.Domain;
//using SimSharp;

//namespace ProcessSim.Implementation.Core.SimulationModels
//{
//    internal class WorkOrderModel : ActiveObject<Simulator>
//    {
//        public WorkPlan WorkPlan { get; init; }
//        public Store _store;
//        public WorkOrderModel(Simulator environment, Store store) : base(environment)
//        {
//            WorkPlan = new WorkPlan();
//            _store = store;
//            environment.Process(Producing());
//        }

//        private IEnumerable<Event> Producing()
//        {
//            foreach (var operation in WorkPlan.WorkOperations) 
//            {
//                var store = new Store(Environment, 1);
//                new WorkOperationModel(Environment, store, operation);
//                yield return store.WhenFull();
//            }
//            _store.Put(new object());
//        }
      
//    }
//}