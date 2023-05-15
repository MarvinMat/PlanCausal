using System.Text.Json.Serialization;

namespace Core.Abstraction.Domain.Models
{
    public record MachineTypeVO(
        [property: JsonPropertyName("typeId")] int TypeId,
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("allowedToolIds")] int [] AllowedToolIds,
        [property: JsonPropertyName("changeoverTimes")] double[][] ChangeoverTimes
    );

    
    public record Tool(
        [property: JsonPropertyName("typeId")] int TypeId,
        [property: JsonPropertyName("size")] string Size,
        [property: JsonPropertyName("name")] string Name
    );
}