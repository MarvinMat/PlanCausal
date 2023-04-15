using Core.Abstraction.Domain.Processes;

namespace Core.Implementation.Events
{
    public class OperationCompletedEvent : EventArgs
    {
        public DateTime CurrentDate { get; }
        public WorkOperation CompletedOperation { get; }
        public OperationCompletedEvent(DateTime currentDate, WorkOperation completedOperation)
        {
            CurrentDate = currentDate;
            CompletedOperation = completedOperation;
        }
    }
}
