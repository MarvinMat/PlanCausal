using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Implementation
{
    public class PythonDelegatePlanner : Abstraction.Planner
    {
        private dynamic _pythonDelegate;
        public PythonDelegatePlanner(dynamic pythonDelegate) : base()
        {
            _pythonDelegate = pythonDelegate;
        }

        protected override Plan ScheduleInternal(List<WorkOperation> workOperations, List<Machine> machines, DateTime currentTime)
        {
            return _pythonDelegate(workOperations, machines, currentTime);
        }
    }
}
