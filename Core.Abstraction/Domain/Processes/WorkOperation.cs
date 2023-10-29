using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Resources;
using System.Diagnostics;

namespace Core.Abstraction.Domain.Processes
{
    /// <summary>
    /// A concrete, scheduled instance of a workplan position.
    /// </summary>
    [DebuggerDisplay("{WorkPlanPosition.Name}")]
    public class WorkOperation
    {
        public readonly WorkPlanPosition WorkPlanPosition;
        public Machine? Machine { get; set; }
        public DateTime PlannedStart { get; set; }
        public DateTime PlannedFinish { get; set; }
        public DateTime ActualStart { get; set; }
        public DateTime ActualFinish { get; set; }
        public TimeSpan MeanDuration => WorkPlanPosition.Duration;
        public double VariationCoefficient => WorkPlanPosition.VariationCoefficient;
        public WorkOperation? Successor { get; set; }
        public WorkOperation? Predecessor { get; set; }
        public OperationState State { get; set; }
        public readonly WorkOrder WorkOrder;
        public List<IFeedback> Feedbacks { get; set; } = new List<IFeedback>();
        public WorkOperation(WorkPlanPosition workPlanPosition, WorkOrder workOrder)
        {
            WorkPlanPosition = workPlanPosition;
            WorkOrder = workOrder;
            State = OperationState.Created;
            PlannedStart = DateTime.MinValue;
            ActualStart = DateTime.MinValue;
            PlannedFinish = DateTime.MinValue;
            ActualFinish = DateTime.MinValue;
        }
    }
}
