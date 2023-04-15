using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Domain.Processes
{
    public class WorkOperation
    {
        private readonly WorkPlanPosition _workPlanPosition;
        public Machine? Machine { get; set; }
        public DateTime EarliestStart { get; set; }
        public DateTime EarliestFinish { get; set; }
        public DateTime LatestStart { get; set; }
        public DateTime LatestFinish { get; set; }
        public TimeSpan Duration => _workPlanPosition.Duration;
        public WorkOperation? Successor { get; set; }
        public WorkOperation(WorkPlanPosition workPlanPosition)
        {
            _workPlanPosition = workPlanPosition;
        }
    }
}
