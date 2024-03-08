using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;
using Extractor.Implementation;
using Extractor.Implementation.Records;
using Transformer.Implementation;

namespace Core.Implementation.Services;

public class WorkPlanProviderCsv : IEntityLoader<WorkPlan>
{
    private readonly string _path;
    private int _limit;
    public WorkPlanProviderCsv(string path, int limit = 0)
    {
        _path = path;
        _limit = limit;
    }
    public List<WorkPlan> Load()
    {
        var extractor = new CsvExtractor<WorkPlanCsvRecord>();
        var transformer = new FromRealToSimulationModelWorkPlanTransformer();
        
        extractor.Extract(_path);
        if (_limit == 0) _limit = extractor.GetExtractedData().Count();
        
        return extractor.GetExtractedData()
            .Where(data => !data.STD.Equals(string.Empty))
            .GroupBy(item => item.MATNR)
            .Where(group => group.Count() > 1)
            .Select(group => group.ToList())
            .Select(record => transformer.Transform(record))
            .Take(_limit)
            .ToList();
    }
}