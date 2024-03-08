using Core.Abstraction.Domain.Processes;
using Extractor.Implementation.Records;
using Transformer.Abstraction;

namespace Transformer.Implementation;

public class FromRealToSimulationModelWorkPlanTransformer : ITransformer<IEnumerable<WorkPlanCsvRecord>, WorkPlan>
{
    public WorkPlan Transform(IEnumerable<WorkPlanCsvRecord> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var csvRecords = input.ToList();
        var workPlanData = csvRecords.First();

        var workPlan = new WorkPlan()
        {
            Name = workPlanData.MATNR,
            Description = "",
            WorkPlanPositions = csvRecords.OrderBy(operation => operation.VORNR).Select(operation => new WorkPlanPosition
            {
                Id = Guid.NewGuid(),
                Name = operation.LTXA1,
                Description = operation.LTXA1,
                Duration = operation.VGE01.Equals("MIN") ? TimeSpan.FromMinutes(double.Parse(operation.VGW01)) : TimeSpan.FromSeconds(double.Parse(operation.VGW01)),
                MachineType = int.Parse(new string(operation.KTSCH.Skip(1).Take (4).ToArray())), // omit the first character and take the following four
            }).ToList()
            
        };
        //TODO: map input to WorkPlan
        return workPlan;
    }
}