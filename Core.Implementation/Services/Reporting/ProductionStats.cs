using Core.Abstraction.Domain;
using Core.Abstraction.Domain.Resources;

namespace Core.Implementation.Services.Reporting;

public class ProductionStats
{
    private readonly List<IFeedback> _feedbacks;

    public ProductionStats(List<IFeedback> feedbacks)
    {
        _feedbacks = feedbacks;
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