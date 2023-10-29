namespace Core.Implementation.Events
{
    public class OrderGenerationEvent : EventArgs
    {
        public DateTime CurrentDate { get; }
        public OrderGenerationEvent(DateTime currentDate)
        {
            CurrentDate = currentDate;
        }
    }
}
