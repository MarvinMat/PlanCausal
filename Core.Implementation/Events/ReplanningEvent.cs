namespace Core.Implementation.Events
{
    public class ReplanningEvent : EventArgs
    {
        public DateTime CurrentDate { get; }
        public ReplanningEvent(DateTime currentDate)
        {
            CurrentDate = currentDate;
        }
    }
}
