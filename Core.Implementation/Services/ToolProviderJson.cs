using System.Text;
using System.Text.Json;
using Core.Abstraction.Domain.Models;

namespace Core.Implementation.Services;

public class ToolProviderJson
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

            var toolTypes = JsonSerializer.Deserialize<List<Tool>>(json);

            if (toolTypes == null)
            {
                throw new Exception($"Deserialization returned null.");
            }

            var tools = new List<Tool>();
            toolTypes.ToList().ForEach(toolType =>
            {
                tools.Add(new Tool(toolType.TypeId, toolType.Size ,toolType.Name));
            });

            return tools;
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to deserialize machines. {ex}");
        }
    }
}