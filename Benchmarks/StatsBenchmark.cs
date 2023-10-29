using BenchmarkDotNet.Attributes;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Implementation.Services;
using Serilog;

namespace Benchmarks;

[RPlotExporter]
public class StatsBenchmark
{
    private readonly ILogger _logger = Log.ForContext<StatsBenchmark>();
    private ProductionScenario? _productionScenario;
    private const int DaysToSimulate = 90;
    private List<Machine>? _machines;
    private List<WorkPlan>? _workPlans;
    private ProductionScenario? _productionScenario50;
    private ProductionScenario? _productionScenario100;
    private ProductionScenario? _productionScenario200;
    private int _quantity = 50;
    //private ProductionScenario? _productionScenario500;

    
    [GlobalSetup]
    public void Init()
    {
        _logger.Information("Starting benchmark...");
        _machines = new MachineProviderJson("../../../../../../../../Machines.json").Load();
        _workPlans = new WorkPlanProviderJson("../../../../../../../../WorkPlans.json").Load();
        _productionScenario50 = new(_machines, _workPlans);
        _productionScenario50.Run(TimeSpan.FromDays(DaysToSimulate));
        _productionScenario100 = new(_machines, _workPlans);
        _productionScenario100.Run(TimeSpan.FromDays(DaysToSimulate));
        _productionScenario200 = new( _machines, _workPlans);
        _productionScenario200.Run(TimeSpan.FromDays(DaysToSimulate));
        // _productionScenario500 = new(500, _machines, _workPlans);
        // _productionScenario500.Run(TimeSpan.FromDays(DaysToSimulate));
    }
    
    //[Benchmark]
    public void BenchMeanLeadTimeForTwoProductsForNinetyDaysQuantityFifty()
    {
        _productionScenario = new ProductionScenario( _machines, _workPlans);
        _productionScenario.Run(TimeSpan.FromDays(90));
        _productionScenario.CollectStats();
    }
    
    //[Benchmark]
    public void BenchMeanLeadTimeForTwoProductsForNinetyDaysQuantityTwoHundred()
    {
        _productionScenario = new ProductionScenario( _machines, _workPlans);
        _productionScenario.Run(TimeSpan.FromDays(90));
        _productionScenario.CollectStats();
    }
    
    [Benchmark]
    public void BenchMeanLeadTimeStatForQuantityFiftySimulatedNinetyDays()
    {
        _productionScenario50?.CollectStats();
    }
    
    [Benchmark]
    public void BenchMeanLeadTimeStatForQuantityOneHundredSimulatedNinetyDays()
    {
        _productionScenario100?.CollectStats();
    }
    
    [Benchmark]
    public void BenchMeanLeadTimeStatForQuantityTwoHundredSimulatedNinetyDays()
    {
        _productionScenario200?.CollectStats();
    }
    
    // [Benchmark]
    // public void BenchMeanLeadTimeStatForQuantityFiveHundredSimulatedNinetyDays()
    // {
    //     _productionScenario500?.CollectStats();
    // }
} 