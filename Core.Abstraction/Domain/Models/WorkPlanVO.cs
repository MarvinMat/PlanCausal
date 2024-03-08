using System.Text.Json.Serialization;

namespace Core.Abstraction.Domain.Models
{
    public record WorkPlanVO([property: JsonPropertyName("workPlanId")] int WorkPlanId,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("name")] string Name)
    {
        [property: JsonPropertyName("operations")]
        public WorkOperationVO[]? Operations { get; set; }
    }
}