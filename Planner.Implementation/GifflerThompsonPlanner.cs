using Controller.Abstraction;
using Controller.Implementation;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;

namespace Planner.Implementation
{
    public class GifflerThompsonPlanner : PlannerBase
    {

        public GifflerThompsonPlanner(IWorkPlanProvider workPlanProvider, IMachineProvider machineProvider) : base(workPlanProvider, machineProvider)
        {
        }

        public override void Plan()
        {
            List<WorkOperation> plan = InitializePlan();

            _controller = new SimulationController(plan, _machines);
            SubscribeToControllerEvents(_controller);
            _controller.Execute(TimeSpan.FromDays(1));
        }

        private List<WorkOperation> InitializePlan(IEnumerable<WorkOperation>? workOperations = null)
        {
            var rnd = new Random();
            List<WorkOperation> plan;

            if (workOperations is null)
            {
                plan = new List<WorkOperation>();
            }
            else
            {
                plan = workOperations.ToList();
            }

            _productionOrders.ToList().ForEach(order =>
            {
                WorkOperation? prevOperation = null;
                order.WorkPlan.WorkPlanPositions.ForEach(operation =>
                {
                    var workOperation = new WorkOperation(operation);
                    var matchingMachines = _machines.Where(machine => machine.MachineType == operation.MachineType).ToArray();
                    workOperation.Machine = matchingMachines[rnd.Next(0, matchingMachines.Length)];

                    if (prevOperation is not null)
                    {
                        prevOperation.Successor = workOperation;
                    }
                    prevOperation = workOperation;

                    Schedule(workOperation);
                    plan.Add(workOperation);
                });
            });
            return plan;
        }

        private static void Schedule(WorkOperation workOperation)
        {
            //TODO: Rework - Context of all Work Operations and resources needed
            workOperation.EarliestStart = DateTime.Now;
            workOperation.EarliestFinish = DateTime.Now + workOperation.Duration;
            workOperation.LatestStart = DateTime.Now;
            workOperation.LatestFinish = DateTime.Now + workOperation.Duration;
        }

        protected override void SubscribeToControllerEvents(IController controller)
        {
            controller.RescheduleEvent += RescheduleHandler;
        }

        private void RescheduleHandler(object? sender, EventArgs e)
        {
            InitializePlan();
        }
    }
}
