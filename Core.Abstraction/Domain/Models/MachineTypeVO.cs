using System.Text.Json.Serialization;

namespace Core.Abstraction.Domain.Models
{
    public record MachineTypeVO([property: JsonPropertyName("typeId")] int MachineTypeId,
                                  [property: JsonPropertyName("count")] int Count,
                                  [property: JsonPropertyName("name")] string Name);
}