using System.Text.Json.Serialization;

namespace Core.Abstraction.Domain.Models
{
    public record WorkOperationVO([property: JsonPropertyName("machineId")] int MachineId,
                                  [property: JsonPropertyName("duration")] double Duration,
                                  [property: JsonPropertyName("name")] string Name);
}