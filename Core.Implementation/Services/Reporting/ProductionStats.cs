using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Core.Implementation.Services.Reporting;

public class ProductionStats
{
    private readonly List<IFeedback> _feedbacks;
    private readonly List<ProductionOrder> _orders;

    public ProductionStats(List<ProductionOrder> orders, List<IFeedback> feedbacks)
    {
        _feedbacks = feedbacks;
        _orders = orders;
    }

    public double MeanLeadTimeInMintesForProduct(WorkPlan product)
    {
        return _orders.Where(order => order.WorkPlan == product).SelectMany(order => order.WorkOrders).Average(order => (order.EndTime - order.StartTime).TotalMinutes);
    }

    public double CalculateMeanLeadTimeInMinutes()
    {
        return _feedbacks.OfType<ProductionFeedback>().Average(feedback => feedback.LeadTime.TotalMinutes);
    }

    public double CalculateMeanLeadTimeOfAGivenMachineTypeInMinutes(int machineType)
    {
        return _feedbacks
            .OfType<ProductionFeedback>()
            .Where(feedback => feedback.Resources
                .OfType<Machine>()
                .First().MachineType == machineType)
            .Average(feedback => feedback.LeadTime.TotalMinutes);
    }

    public double CalculateVarianceOfLeadTime()
    {
        throw new NotImplementedException();
    }

    public double CalculateStandardDeviation()
    {
        throw new NotImplementedException();
    }
}