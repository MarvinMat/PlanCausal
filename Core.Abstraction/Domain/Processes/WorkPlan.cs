using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Abstraction.Domain.Processes
{
    public class WorkPlan
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public List<WorkPlanPosition> WorkPlanPositions { get; set; }

        public WorkPlan()
        {
            Id = Guid.NewGuid();
            Name = "Unnamed work plan";
            WorkPlanPositions = new List<WorkPlanPosition>();
        }
    }
}
