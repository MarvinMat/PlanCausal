namespace Extractor.Abstraction;

public interface IExtractor<out T>
{
    public void Extract(string path);
    
    public T GetExtractedData();
}