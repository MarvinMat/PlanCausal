using Controller.Implementation;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;

namespace Planner.Implementation
{
    public class GifflerThompsonPlanner : PlannerBase
    {
        //TODO: You might wanna look into this mess if you come back :)
        public GifflerThompsonPlanner(IWorkPlanProvider workPlanProvider, IMachineProvider machineProvider) : base(workPlanProvider, machineProvider)
        {
        }

        public override void Plan()
        {
            var rnd = new Random();
            var plan = new List<WorkOperation>();

            _productionOrders.ToList().ForEach(order =>
            {
                order.WorkPlan.WorkPlanPositions.ForEach(operation =>
                {
                    var workOperation = new WorkOperation(operation);
                    var matchingMachines = _machines.Where(machine => machine.MachineType == operation.MachineType).ToArray();
                    workOperation.Machine = matchingMachines[rnd.Next(0, matchingMachines.Length)];

                    Schedule(workOperation);
                    plan.Add(workOperation);
                });
            });

            _controller = new SimulationController(plan);
            _controller.Execute();
        }

        private static void Schedule(WorkOperation workOperation)
        {
            //TODO: Rework - Context of all Work Operations and resources needed
            workOperation.EarliestStart = DateTime.Now;
            workOperation.EarliestFinish = DateTime.Now + workOperation.Duration;
            workOperation.LatestStart = DateTime.Now;
            workOperation.LatestFinish = DateTime.Now;
        }
    }
}
