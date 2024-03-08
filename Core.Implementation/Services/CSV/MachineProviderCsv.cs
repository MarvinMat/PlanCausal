using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using Core.Abstraction.Services;
using Extractor.Implementation;
using Extractor.Implementation.Records;
using Transformer.Implementation;

namespace Core.Implementation.Services;

public class MachineProviderCsv : IEntityLoader<Machine>
{
    private readonly string _path;
    private int _limit;
    public MachineProviderCsv(string path, int limit = 0)
    {
        _path = path;
        _limit = limit;
    }
    public List<Machine> Load()
    {
        var extractor = new CsvExtractor<MachineCsvRecord>();
        var transformer = new FromRealToSimulationMachineTransformer();
        
        extractor.Extract(_path);
        if (_limit == 0) _limit = extractor.GetExtractedData().Count();
        
        return extractor.GetExtractedData()
            .GroupBy(item => item.WorkplaceId)
            .Where(group => group.Count() > 1)
            .Select(group => group.ToList())
            .Select(record => transformer.Transform(record))
            .Take(_limit)
            .ToList();
    }
}