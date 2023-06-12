using Core.Abstraction.Domain.Resources;

namespace Core.Implementation.Events
{
    public class InterruptionEvent : EventArgs
    {
        public DateTime CurrentDate { get; }
        public List<IResource> AffectedResources { get; }
        public InterruptionEvent(DateTime currentDate, List<IResource> affectedResources)
        {
            CurrentDate = currentDate;
            AffectedResources = affectedResources;
        }
    }
}
