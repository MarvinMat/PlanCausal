using BenchmarkDotNet.Running;
using Benchmarks;
using Core.Implementation.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
 .WriteTo.Console()
 .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
 .MinimumLevel.Information()
 .Enrich.FromLogContext()
 .CreateLogger();

 
 var productionScenario = new ProductionScenario(100, 
     new MachineProviderJson($"../../../../Machines.json").Load(),
     new WorkPlanProviderJson($"../../../../WorkPlans.json").Load());
 productionScenario.Run(TimeSpan.FromDays(30));
//var benchmark = BenchmarkRunner.Run<StatsBenchmark>();

Log.CloseAndFlush();