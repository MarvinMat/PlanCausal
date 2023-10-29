
using Core.Abstraction.Domain.Models;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;
using System.Text.Json;


namespace Core.Implementation.Services
{
    public class WorkPlanProviderJson : IEntityLoader<WorkPlan>
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

                var workPlanVOs = JsonSerializer.Deserialize<List<WorkPlanVO>>(json);

                if (workPlanVOs == null)
                {
                    throw new Exception($"Deserialization returned null.");
                }

                var workPlans = new List<WorkPlan>();
                workPlanVOs.ForEach(plan =>
                {   
                    var Workplan = new WorkPlan()
                    {
                        Name = plan.Name,
                        Description = plan.Description,
                        WorkPlanPositions = plan.Operations.Select(operation => new WorkPlanPosition()
                        {
                            Name = operation.Name,
                            Duration = TimeSpan.FromMinutes(operation.Duration),
                            VariationCoefficient = operation.VariationCoefficient,
                            MachineType = operation.MachineId,
                            ToolId = operation.ToolId,
                        }).ToList()
                        
                    };
                    workPlans.Add(Workplan);
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
