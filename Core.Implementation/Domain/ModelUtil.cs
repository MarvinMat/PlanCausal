using Core.Abstraction.Domain.Processes;

namespace Core.Implementation.Domain
{
    public class ModelUtil
    {
        public static List<WorkOperation> GetWorkOperationsFromOrders(List<ProductionOrder> orders)
        {
            var operations = new List<WorkOperation>();

            orders.ForEach(productionOrder =>
            {
                var workOrders = new List<WorkOrder>();
                for (var i = 0; i < productionOrder.Quantity; i++)
                {
                    var workOrder = new WorkOrder(productionOrder);
                    var workOrderOperations = new List<WorkOperation>();

                    WorkOperation? prevOperation = null;
                    productionOrder.WorkPlan.WorkPlanPositions.ForEach(planPosition =>
                    {
                        var workOperation = new WorkOperation(planPosition, workOrder);

                        if (prevOperation is not null)
                        {
                            prevOperation.Successor = workOperation;
                            workOperation.Predecessor = prevOperation;
                        }
                        prevOperation = workOperation;
                        workOrderOperations.Add(workOperation);
                    });

                    workOrder.WorkOperations = workOrderOperations;
                    operations.AddRange(workOrderOperations);
                    workOrders.Add(workOrder);
                }
                productionOrder.WorkOrders = workOrders;
            });

            return operations;
        }
    }
}
