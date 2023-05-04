using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Domain.Processes
{
    /// <summary>
    /// A concrete, scheduled instance of a workplan position.
    /// </summary>
    public class WorkOperation
    {
        public readonly WorkPlanPosition WorkPlanPosition;
        public Machine? Machine { get; set; }
        public DateTime EarliestStart { get; set; }
        public DateTime EarliestFinish { get; set; }
        public DateTime LatestStart { get; set; }
        public DateTime LatestFinish { get; set; }
        public TimeSpan Duration => WorkPlanPosition.Duration;
        public WorkOperation? Successor { get; set; }
        public WorkOperation? Predecessor { get; set; }
        public OperationState State { get; set; }
        public WorkOperation(WorkPlanPosition workPlanPosition)
        {
            WorkPlanPosition = workPlanPosition;
            State = OperationState.Created;
        }
    }
}
