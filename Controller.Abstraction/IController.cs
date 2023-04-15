namespace Controller.Abstraction
{
    public interface IController
    {
        public event EventHandler RescheduleEvent;

        void Execute(TimeSpan duration);
    }
}
