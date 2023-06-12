﻿using Core.Abstraction.Domain.Enums;
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
        public DateTime EarliestStart { get; set; }
        public DateTime EarliestFinish { get; set; }
        public DateTime LatestStart { get; set; }
        public DateTime LatestFinish { get; set; }
        public TimeSpan Duration => WorkPlanPosition.Duration;
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
            EarliestStart = DateTime.MinValue;
            LatestStart = DateTime.MinValue;
            EarliestFinish = DateTime.MinValue;
            LatestFinish = DateTime.MinValue;
        }
    }
}
