using Core.Abstraction.Domain.Enums;
using Core.Abstraction.Domain.Models;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Extractor.Implementation.Records;
using Transformer.Abstraction;

namespace Transformer.Implementation;

public class FromRealToSimulationMachineTransformer : ITransformer<IEnumerable<MachineCsvRecord>, Machine>
{
    public Machine Transform(IEnumerable<MachineCsvRecord> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var csvRecords = input.ToList();
        var machineData = csvRecords.First();

        var machine = new Machine()
        {
            Name = machineData.Workplace,
            MachineType = machineData.WorkplaceId,
            State = MachineState.Idle
            
        };
        return machine;
    }
}