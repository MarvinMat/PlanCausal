using Core.Abstraction.Domain.Models;
using SimulatorConfigurator.Services.Provider;
using System.Collections.ObjectModel;

namespace SimulatorConfigurator.MVVM.Model
{
    public class WorkplanModel
    {
        public ObservableCollection<WorkPlanVO> WorkPlans { get; set; }
        public ObservableCollection<MachineTypeVO> MachineTypes { get; set; }
        public ObservableCollection<Tool> Tools { get; set; }


        public WorkplanModel()
        {

            WorkPlans = new ObservableCollection<WorkPlanVO>(DataProvider.Load<WorkPlanVO>("../../../../WorkPlans.json"));
            MachineTypes = new ObservableCollection<MachineTypeVO>(DataProvider.Load<MachineTypeVO>("../../../../Machines.json"));
            Tools = new ObservableCollection<Tool>(DataProvider.Load<Tool>("../../../../Tools.json"));


        }
    }
}
