using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Models;

namespace ProcessSimulator
{
    public enum Shift
    {
        Day,
        Night
    }

    public class InferenceModel
    {
        private readonly Variable<double> temperatureVar;
        private readonly Variable<bool> workingDayVar;
        private readonly Variable<double> daysSinceLastInterruptVar;
        private readonly Variable<Shift> shiftVar;
        private readonly Variable<double> operationDurationVar;

        private readonly InferenceEngine engine;
        public InferenceModel()
        {
            temperatureVar = Variable.New<double>().Named("Temperature");
            workingDayVar = Variable.New<bool>().Named("Working Day");
            daysSinceLastInterruptVar = Variable.New<double>().Named("Days Since Last Interrupt");
            shiftVar = Variable.New<Shift>().Named("Shift");

            operationDurationVar = Variable.GaussianFromMeanAndVariance(1, 0.01).Named("Initial Operation duration factor");

            operationDurationVar *= (0.9 + 0.2 * (Variable.Exp(Variable.Min(Variable.Constant(30.0), daysSinceLastInterruptVar)) / Math.Exp(30))).Named("Time Since Last Interrupt Factor");

            operationDurationVar *= (temperatureVar / 20).Named("Temperature Factor");
            
            var nightShiftWeekendFactor = Variable.New<double>().Named("Night Shift Weekend Factor");
            using (Variable.If(workingDayVar))
            {
                nightShiftWeekendFactor.SetTo(Variable.Constant(1.0));
            }
            using (Variable.IfNot(workingDayVar))
            {
                var isNightShiftVar = (shiftVar == Shift.Night).Named("Is Night Shift");
                using (Variable.If(isNightShiftVar))
                {
                    nightShiftWeekendFactor.SetTo(Variable.Constant(1.1));
                }
                using (Variable.IfNot(isNightShiftVar))
                {
                    nightShiftWeekendFactor.SetTo(Variable.Constant(1.0));
                }
            }

            operationDurationVar *= nightShiftWeekendFactor;
            operationDurationVar.Named("Operation duration factor");

            engine = new InferenceEngine();
            engine.SaveFactorGraphToFolder = "graphs";
        }

        public double Infer(double temperature, bool workingDay, double daysSinceLastInterrupt, Shift shift)
        {
            temperatureVar.ObservedValue = temperature;
            workingDayVar.ObservedValue = workingDay;
            shiftVar.ObservedValue = shift;
            daysSinceLastInterruptVar.ObservedValue = daysSinceLastInterrupt;

            var operationDurationDistribution = engine.Infer(operationDurationVar);
            if (operationDurationDistribution is not Gaussian operationDurationGaussian)
                return 1;

            //return operationDurationGaussian.GetMean();
            return operationDurationGaussian.Sample();
        }
    }
}
