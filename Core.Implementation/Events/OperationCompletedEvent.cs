using Core.Abstraction.Domain.Processes;

namespace Core.Implementation.Events
{
    public class OperationCompletedEvent : EventArgs
    {
        public DateTime CurrentDate { get; }
        public WorkOperation CompletedOperation { get; }
        public Dictionary<string, object> InfluenceFactors { get; }
        public OperationCompletedEvent(DateTime currentDate, WorkOperation completedOperation, Dictionary<string, object> influenceFactors)
        {
            CurrentDate = currentDate;
            CompletedOperation = completedOperation;
            InfluenceFactors = influenceFactors;
        }
    }
}
