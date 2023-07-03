using System.Text;

namespace Core.Abstraction.Domain.Processes
{
    /// <summary>
    /// A concrete plan containing a number of work operations. The contained operations are scheduled and linked to the resources.
    /// </summary>
    public class Plan
    {
        public List<WorkOperation> Operations { get; }
        public bool IsComplete { get; set; }

        public Plan(List<WorkOperation> operations, bool isComplete)
        {
            Operations = operations;
            IsComplete = isComplete;
        }

        public override string ToString()
        {
            var machines = Operations.Select(op => op.Machine).Distinct().ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Plan:");
            foreach (var machine in machines)
            {
                sb.AppendLine();
                sb.AppendLine($"\tMachine: {machine?.Description} ({machine?.Id})");
                sb.AppendLine();
                var operations = Operations.Where(op => op.Machine == machine).OrderBy(op => op.EarliestStart).ToList();
                operations.ForEach(operation =>
                {
                    sb.AppendLine($"Operation: {operation.WorkPlanPosition.Name}");
                    sb.AppendLine($"Earliest Start: {operation.EarliestStart}");
                    sb.AppendLine($"Earliest Finish: {operation.EarliestFinish}");
                    sb.AppendLine($"Duration: {operation.MeanDuration}");
                    sb.AppendLine();
                });
            }

            return sb.ToString();
        }
    }
}
