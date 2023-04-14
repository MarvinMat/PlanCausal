
using Core.Abstraction.Domain.Models;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;
using System.Diagnostics;
using System.Text.Json;


namespace ProcessSim.Implementation.Services
{
    public class WorkPlanProviderJson : IWorkPlanProvider
    {
        private readonly string _path;
        public WorkPlanProviderJson(string path)
        {
            _path = path;
        }


        public List<WorkPlan> Load()
        {
            try
            {
                string json = File.ReadAllText(_path);

                var workPlanVOs = JsonSerializer.Deserialize<List<List<WorkOperationVO>>>(json);

                if (workPlanVOs == null)
                {
                    Debug.WriteLine("Empty Workplan list");
                    return new List<WorkPlan>();
                }

                var workPlans = new List<WorkPlan>();
                workPlanVOs.ForEach(plan =>
                {
                    var operations = new List<WorkPlanPosition>();
                    plan.ForEach(operation =>
                    {
                        operations.Add(new WorkPlanPosition()
                        {
                            Name = operation.Name,
                            Duration = TimeSpan.FromMinutes(operation.Duration),
                        });
                    });

                    workPlans.Add(new WorkPlan()
                    {
                        Name = $"Produkt {workPlanVOs.Count + 1}",
                        WorkPlanPositions = operations
                    });
                });


                return workPlans;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to deserialize work plans. {ex}");
            }
        }
    }

}
