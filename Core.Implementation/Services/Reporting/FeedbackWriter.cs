using Core.Abstraction.Domain;
using System.Text;
using System.Text.Json;

namespace Core.Implementation.Services.Reporting;

public class FeedbackWriter
{
    public static async Task WriteFeedbackToJsonAsync(List<ProductionFeedback> feedbackList, string filePath)
    {
        string jsonData = JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, jsonData, Encoding.UTF8);
    }

    public static async Task WriteFeedbackToCSVAsync(List<ProductionFeedback> feedbackList, string filePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Operation;Planned Start;Planned Finish;Actual Start;Actual End;Machine;Tool;DonePercent");
        foreach (var feedback in feedbackList)
        {
            var operation = feedback.WorkOperation;
            var newLine = $"{operation.WorkPlanPosition.Name};{operation.PlannedStart};{operation.PlannedFinish};{operation.ActualStart};{operation.ActualFinish};{operation.Machine.Description};{operation.WorkPlanPosition.ToolId};{feedback.DoneInPercent}";
            csv.AppendLine(newLine);
        }
        await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
    }
}
