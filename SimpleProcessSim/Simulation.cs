using ProcessSim.Abstraction.Domain.Interfaces;

namespace ProcessSim.Implementation
{
    public class Simulation : ISimulation
    {
        private readonly SimSharp.Simulation _sim;
        public Simulation(int seed, DateTime initialDateTime)
        {
            _sim = new SimSharp.Simulation(randomSeed: seed, initialDateTime: initialDateTime);
        }
        public void Start(TimeSpan duration)
        {
            // _sim.Process();
            _sim.Run(duration);
        }

        public void Start(DateTime until)
        {
            _sim.Run(until);
        }
    }
}
