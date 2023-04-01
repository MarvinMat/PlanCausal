using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Abstraction.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSim.Abstraction.Services
{
    public interface IWorkPlanProvider
    {
        List<List<WorkOperationVO>> Load();
    }
}
