using ProcessSim.Abstraction.Domain.Interfaces;
using ProcessSim.Abstraction.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using ProcessSimImplementation.Domain;
using System.Diagnostics;
using ProcessSim.Abstraction.Domain.Models;
using System.IO;

namespace ProcessSim.Implementation.Services
{
    public class WorkPlanProviderJson : IWorkPlanProvider
    {
        private readonly string _path;
        public WorkPlanProviderJson(string path) {
            _path = path;
        }


        public List<List<WorkOperationVO>> Load()
        {
            try
            {
                string json = File.ReadAllText(_path);

                var workPlans = JsonSerializer.Deserialize<List<List<WorkOperationVO>>>(json);
                if (workPlans == null)
                {
                    Debug.WriteLine("Empty Workplan list");
                    return new List<List<WorkOperationVO>>();
                }
                return workPlans;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to deserialize work plans. {ex}");
            }
        }
    }
}
