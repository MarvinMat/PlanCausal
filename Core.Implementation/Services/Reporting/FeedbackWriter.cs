using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Customers;
using System.Text;
using System.Text.Json;
using Serilog;

namespace Core.Implementation.Services.Reporting;

public class FeedbackWriter
{
    public static async Task WriteFeedbacksToJsonAsync(List<ProductionFeedback> feedbackList, string filePath)
    {
        string jsonData = JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        try
        {
            await File.WriteAllTextAsync(filePath, jsonData, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.ForContext<FeedbackWriter>().Error("Production feedbacks could not be written: " + ex.Message);
        }
    }

    public static void WriteFeedbacksToJson(List<ProductionFeedback> feedbackList, string filePath)
    {
        var jsonData = GetJsonStringFromFeedbacks(feedbackList);

        try
        {
            File.WriteAllText(filePath, jsonData, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.ForContext<FeedbackWriter>().Error("Production feedbacks could not be written: " + ex.Message);
        }
    }

    private static string GetJsonStringFromFeedbacks(List<ProductionFeedback> feedbackList)
    {
        return JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        
    }

    public static async Task WriteFeedbacksToCsvAsync(List<ProductionFeedback> feedbackList, string filePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Operation;Planned Start;Planned Finish;Actual Start;Actual End;Machine;Tool;DonePercent");
        foreach (var feedback in feedbackList)
        {
            var operation = feedback.WorkOperation;
            var newLine = $"{operation.WorkPlanPosition.Name};{operation.PlannedStart};{operation.PlannedFinish};{operation.ActualStart};{operation.ActualFinish};{operation.Machine?.Description};{operation.WorkPlanPosition.ToolId};{feedback.DoneInPercent}";
            csv.AppendLine(newLine);
        }
        try
        {
            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.ForContext<FeedbackWriter>().Error("Production feedbacks could not be written: " + ex.Message);
        }
    }

    public static void WriteFeedbacksToCsv(List<ProductionFeedback> feedbackList, string filePath)
    {
        var csv = GetCsvStringFromFeedbacks(feedbackList, filePath);
        
        try { 
            File.WriteAllText(filePath, csv, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.ForContext<FeedbackWriter>().Error("Production feedbacks could not be written: " + ex.Message);
        }
        File.WriteAllText(filePath, csv, Encoding.UTF8);
    }

    private static string GetCsvStringFromFeedbacks(List<ProductionFeedback> feedbackList, string filePath)
    {
        var csv = new StringBuilder();
        var headerLine = "Operation;Planned Start;Planned Finish;Actual Start;Actual End;Machine;Tool;DonePercent";
        var influenceFactorNames = feedbackList.First().InfluenceFactors.Keys.ToList();
        foreach (var influenceFactorName in influenceFactorNames)
        {
            headerLine += ";" + influenceFactorName;
        }
        csv.AppendLine(headerLine);

        foreach (var feedback in feedbackList)
        {
            var operation = feedback.WorkOperation;
            var nextLine = $"{operation.WorkOrder.ProductionOrder.WorkPlan.Name}:{operation.WorkPlanPosition.Name};{operation.PlannedStart};{operation.PlannedFinish};{operation.ActualStart};{operation.ActualFinish};{operation.Machine?.Description};{operation.WorkPlanPosition.ToolId};{feedback.DoneInPercent}";
            foreach (var influenceFactorName in influenceFactorNames)
            {
                feedback.InfluenceFactors.TryGetValue(influenceFactorName, out var value);
                if (value != null) {
                    nextLine += ";" + value;
                }
            }
            csv.AppendLine(nextLine);
        }

        return csv.ToString();
    }

    public static void WriteCustomerOrdersToCsv(List<Customer> customers, string filePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Customer ID;Customer Name;Order ID;Quantity;Product;Order Received;Order Started;Order Completed;Order Delivered");
        customers.ForEach(customer =>
        {
            customer.Orders.ForEach(customerOrder =>
            {
                customerOrder.ProductionOrders.ForEach(prodOrder =>
                {
                    var newLine = $"{customer.Id};{customer.Name};{customerOrder.Id};{prodOrder.Quantity};{prodOrder.WorkPlan.Name};{customerOrder.OrderReceivedDate};{customerOrder.StartedDate};{customerOrder.CompletedDate};{customerOrder.DeliveryDate}";
                    csv.AppendLine(newLine);
                });
            });
        });
        try
        {
            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.ForContext<FeedbackWriter>().Error("Customer order report could not be written: " + ex.Message);
        }
    }
}
