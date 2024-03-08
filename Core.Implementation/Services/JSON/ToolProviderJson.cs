using Core.Abstraction.Domain.Models;
using Core.Abstraction.Services;
using System.Text;
using System.Text.Json;

namespace Core.Implementation.Services;

public class ToolProviderJson : IEntityLoader<Tool>
{
    private readonly string _path;
    public ToolProviderJson(string path)
    {
        _path = path;
    }
    public List<Tool> Load()
    {
        try
        {
            string json = File.ReadAllText(_path, Encoding.UTF8);

            var tools = JsonSerializer.Deserialize<List<Tool>>(json);

            if (tools == null)
            {
                throw new Exception($"Deserialization returned null.");
            }

            return tools;
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to deserialize tools. {ex}");
        }
    }
}