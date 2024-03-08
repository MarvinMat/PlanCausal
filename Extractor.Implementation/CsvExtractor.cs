using System.Globalization;
using Core.Abstraction.Domain.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Extractor.Abstraction;
using Extractor.Implementation.Records;

namespace Extractor.Implementation;

public class CsvExtractor<T> : IExtractor<IEnumerable<T>>
{
    private IEnumerable<T>? _extractedData;

    public void Extract(string path)
    {
        if (!File.Exists(path)) throw new DirectoryNotFoundException($"Could not find {path}");
        
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim 
        } );
        _extractedData = csv.GetRecords<T>().ToList();
    }

    public IEnumerable<T> GetExtractedData()
    {
        if (_extractedData is not null) return _extractedData;
        
        throw new NullReferenceException("Extracted data is null. Please call Extract method first.");
    }
}