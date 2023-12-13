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
        string jsonData = JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        try
        {
            File.WriteAllText(filePath, jsonData, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.ForContext<FeedbackWriter>().Error("Production feedbacks could not be written: " + ex.Message);
        }
    }

    public static async Task WriteFeedbacksToCSVAsync(List<ProductionFeedback> feedbackList, string filePath)
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

    public static void WriteFeedbacksToCSV(List<ProductionFeedback> feedbackList, string filePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Operation;Planned Start;Planned Finish;Actual Start;Actual End;Machine;Tool;DonePercent");
        foreach (var feedback in feedbackList)
        {
            var operation = feedback.WorkOperation;
            var newLine = $"{operation.WorkPlanPosition.Name};{operation.PlannedStart};{operation.PlannedFinish};{operation.ActualStart};{operation.ActualFinish};{operation.Machine?.Description};{operation.WorkPlanPosition.ToolId};{feedback.DoneInPercent}";
            csv.AppendLine(newLine);
        }
        try { 
            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.ForContext<FeedbackWriter>().Error("Production feedbacks could not be written: " + ex.Message);
        }
    }

    public static void WriteCustomerOrdersToCSV(List<Customer> customers, string filePath)
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
