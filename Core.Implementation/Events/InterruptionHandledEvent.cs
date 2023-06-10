using Core.Abstraction.Domain.Resources;

namespace Core.Implementation.Events
{
    public class InterruptionHandledEvent : EventArgs
    {
        public DateTime CurrentDate { get; }
        public IResource Resource { get; }
        public InterruptionHandledEvent(DateTime currentDate, IResource resource)
        {
            CurrentDate = currentDate;
            Resource = resource;
        }
    }
}
