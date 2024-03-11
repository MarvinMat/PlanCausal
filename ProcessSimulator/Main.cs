using CoreAbstraction = Core.Abstraction;
using Core.Implementation.Services;
using ProcessSim.Abstraction;
using ProcessSim.Implementation;
using ProcessSim.Implementation.Core.SimulationModels;
using ProcessSimulator.Scenarios;
using Serilog;
using SimSharp;
using static SimSharp.Distributions;
using ProcessSim.Implementation.Core.InfluencingFactors;
using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSimulator;
using Python.Runtime;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .CreateLogger();


Runtime.PythonDLL = "python311.dll";
PythonEngine.Initialize();

//var inferenceModel = new InferenceModel();

var scenario = new ProductionScenario("ElevenMachinesProblem", "Test")
{
    Duration = TimeSpan.FromDays(30),
    Seed = 42,
    RePlanningInterval = TimeSpan.FromHours(8),
    StartTime = DateTime.Now,
    InitialCustomerOrdersGenerated = 10
}
    .WithEntityLoader(new MachineProviderJson($"../../../../Machines_11Machines.json"))
    .WithEntityLoader(new WorkPlanProviderJson($"../../../../Workplans_11Machines.json"))
    //.WithEntityLoader(new MachineProviderCsv($"../../../../data_machines.csv"))
    //.WithEntityLoader(new WorkPlanProviderCsv($"../../../../data.csv", 5))
    .WithEntityLoader(new CustomerProviderJson("../../../../Customers.json"))
    .WithInterrupt(
        predicate: process => new Random().NextDouble() < 0.7,
        distribution: () => TimeSpan.FromHours(new MathNet.Numerics.Distributions.Exponential(1.0/20.0).Sample()),
        interruptAction: InterruptAction
    )
    .WithOrderGenerationFrequency(
        CoreAbstraction.Distributions.DiscreteDistribution(
            new List<TimeSpan> { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30) },
            new List<double> { 0.25, 0.60, 0.15 }
        )
    )
    .WithReporting(".")
    .WithInfluencingFactor("Temperature", SimulateTemperature, 8.0)
    .WithInfluencingFactor("Shift", SimulateShift, Shift.Day)
    .WithAdjustedOperationTime(CalculateDurationFactor);

double CalculateDurationFactor(Dictionary<string, IFactor> influencingFactors)
{
    if (!influencingFactors.TryGetValue("Temperature", out var tempFactor))
        return 1;
    if (tempFactor is not InfluencingFactor<double> temperature)
        return 1;

    if (!influencingFactors.TryGetValue(InternalInfluenceFactorName.NeededChangeover.ToString(), out var neededChangeoverFactor))
        return 1;
    if (neededChangeoverFactor is not InfluencingFactor<bool> neededChangeover)
        return 1;

    if (!influencingFactors.TryGetValue(InternalInfluenceFactorName.CurrentTime.ToString(), out var currentTimeFactor))
        return 1;
    if (currentTimeFactor is not InfluencingFactor<DateTime> currentTime)
        return 1;

    var isWorkingDay = currentTime.CurrentValue.DayOfWeek is not DayOfWeek.Saturday && currentTime.CurrentValue.DayOfWeek is not DayOfWeek.Sunday;

    if (!influencingFactors.TryGetValue(InternalInfluenceFactorName.TimeSinceLastInterrupt.ToString(), out var timeSinceLastInterruptFactor))
        return 1;
    if (timeSinceLastInterruptFactor is not InfluencingFactor<TimeSpan> timeSinceLastInterrupt)
        return 1;

    if (!influencingFactors.TryGetValue("Shift", out var shiftFactor))
        return 1;
    if (shiftFactor is not InfluencingFactor<Shift> shift)
        return 1;

    //var factor = inferenceModel.Infer(temperature.CurrentValue, isWorkingDay, timeSinceLastInterrupt.CurrentValue.TotalDays, shift.CurrentValue);

    double factor;
    using (Py.GIL())
    {
        // Put the path to the folder containing the inference model into the sys.path so that it can be imported
        dynamic sys = Py.Import("sys");
        // Assuming the executable for this C# program is e.g. in ./bin/Debug/net6.0/ and the python file is three directories up from there (in ./)
        sys.path.append("../../.."); 

        // Import the module containing the inference model and infer a value for the duration factor
        dynamic test = Py.Import("bayesianNetwork1");
        factor = (double)test.infer(temperature.CurrentValue, isWorkingDay, timeSinceLastInterrupt.CurrentValue.TotalDays, shift.CurrentValue.ToString());
    }
    return factor;
}    

IEnumerable<Event> SimulateTemperature(ProductionScenario scenario, Action<double> setCurrentValue)
{
    if (scenario.Simulator is not Simulator simulator)
        throw new Exception("scenario.Simulator should be a Simulator.");

    while (true)
    {
        var curTime = simulator.CurrentSimulationTime;
        var temperatureMean = 20 + 2 * Math.Sin(2 * Math.PI * curTime.Hour / 24 - Math.PI/2);
        var temperature = new MathNet.Numerics.Distributions.Normal(temperatureMean, 0.3).Sample();
        setCurrentValue(temperature);

        yield return simulator.Timeout(TimeSpan.FromMinutes(60 - curTime.Minute));
    }
}


IEnumerable<Event> SimulateShift(ProductionScenario scenario, Action<Shift> setCurrentValue)
{
    if (scenario.Simulator is not Simulator simulator)
        throw new Exception("scenario.Simulator should be a Simulator.");

    while (true)
    {
        var curTime = simulator.CurrentSimulationTime;
        if (curTime.Hour >= 7 && curTime.Hour < 19) setCurrentValue(Shift.Day);
        else setCurrentValue(Shift.Night);

        yield return simulator.Timeout(TimeSpan.FromMinutes(60 - curTime.Minute));
    }
}

IEnumerable<Event> InterruptAction(ActiveObject<Simulation> simProcess, IScenario productionScenario)
{
    if (productionScenario is not ProductionScenario prodScenario)
        throw new NullReferenceException("Scenario is null.");
    if (prodScenario.Simulator is not Simulator simulator)
        throw new NullReferenceException("Simulator is null.");

    if (simProcess is MachineModel machineModel)
    {
        var waitFor = POS(N(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30)));
        var start = simulator.CurrentSimulationTime;

        Log.Logger.Warning("Interrupted {Machine} at {Time}",
            machineModel.Machine.Description, simulator.CurrentSimulationTime);
        yield return simulator.Timeout(waitFor);
        Log.Logger.Warning("{Machine} waited {Waited} hours (done at {Time})",
            machineModel.Machine.Description, simulator.CurrentSimulationTime - start, simulator.CurrentSimulationTime);
    }
}

scenario.Run();
scenario.CollectStats();


Log.CloseAndFlush();
PythonEngine.Shutdown();
