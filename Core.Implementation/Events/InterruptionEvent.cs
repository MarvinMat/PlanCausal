using Core.Abstraction.Domain.Resources;

namespace Core.Implementation.Events
{
    public class InterruptionEvent : EventArgs
    {
        public DateTime CurrentDate { get; }
        public IResource AffectedResource { get; }
        public InterruptionEvent(DateTime currentDate, IResource affectedResource)
        {
            CurrentDate = currentDate;
            AffectedResource = affectedResource;
        }
    }
}
