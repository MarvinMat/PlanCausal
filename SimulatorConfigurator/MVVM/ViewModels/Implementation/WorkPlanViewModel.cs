using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Abstraction.Domain.Models;
using SimulatorConfigurator.Core;
using SimulatorConfigurator.MVVM.Model;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SimulatorConfigurator.MVVM.ViewModels.Implementation;

public partial class WorkPlanViewModel : ViewModel
{
    private readonly WorkplanModel _workplanModel;

    [ObservableProperty]
    [Required]
    [NotifyCanExecuteChangedFor(nameof(AddWorkPlanCommand))]
    private string _workPlanToAddDescription;

    public ObservableCollection<WorkPlanVO> WorkPlans
    {
        get { return _workplanModel.WorkPlans; }
        set { _workplanModel.WorkPlans = value; }
    }

    [ObservableProperty]
    [Required]
    [NotifyCanExecuteChangedFor(nameof(AddWorkPlanCommand))]
    private string _workPlanToAddName;

    [ObservableProperty]
    [Required]
    private WorkPlanVO? _selectedWorkPlan;

    [ObservableProperty]
    private WorkOperationVO? _selectedWorkOperation;

    public WorkPlanViewModel(WorkplanModel workplanModel) : base(workplanModel)
    {

        _workplanModel = workplanModel;
        WorkPlanToAddName = "";
        WorkPlanToAddDescription = "";
        SelectedWorkOperation = new WorkOperationVO(1, 10, 2.0 ,"MyOperation", 1);
    }

    private bool HasWorkPlanToAdd => WorkPlanToAddName != "" && WorkPlanToAddDescription != "";

    [RelayCommand(CanExecute = nameof(HasWorkPlanToAdd))]
    private void AddWorkPlan()
    {
        var workPlanToAdd = new WorkPlanVO(WorkPlans.Count + 1, WorkPlanToAddDescription, WorkPlanToAddName);
        WorkPlans.Add(workPlanToAdd);
        WorkPlanToAddName = "";
        WorkPlanToAddDescription = "";

    }

    [RelayCommand]
    private void AddWorkOperationToSelectedWorkPlan()
    {
        if (SelectedWorkOperation == null || SelectedWorkPlan == null) return;

        var workOperationToAdd = new WorkOperationVO(SelectedWorkOperation.MachineId, SelectedWorkOperation.Duration, 2.0,SelectedWorkOperation.Name, SelectedWorkOperation.ToolId);
        var selectedWorkPlan = SelectedWorkPlan;
        var index = WorkPlans.IndexOf(selectedWorkPlan);
        var correctPlan = WorkPlans[index];

        if (correctPlan?.Operations == null)
        {
            WorkPlans.Remove(correctPlan);
            WorkPlans.Insert(index, new WorkPlanVO(correctPlan.WorkPlanId, correctPlan.Description, correctPlan.Name) { Operations = new[] { workOperationToAdd } });
        }
        else
        {
            WorkPlans.Remove(correctPlan);
            WorkPlans.Insert(index, new WorkPlanVO(correctPlan.WorkPlanId, correctPlan.Description, correctPlan.Name)
            {
                Operations = correctPlan.Operations.Append(workOperationToAdd).ToArray()
            });
        }
        WorkPlans.OrderBy(x => x.WorkPlanId);

    }

}