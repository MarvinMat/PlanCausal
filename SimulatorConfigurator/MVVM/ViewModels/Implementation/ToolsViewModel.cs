using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Abstraction.Domain.Models;
using SimulatorConfigurator.Core;
using SimulatorConfigurator.MVVM.Model;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SimulatorConfigurator.MVVM.ViewModels.Implementation;

public partial class ToolsViewModel : ViewModel
{
    private readonly WorkplanModel workplanModel;
    [ObservableProperty]
    [Required]
    [NotifyCanExecuteChangedFor(nameof(AddToolCommand))]
    private string _toolToAddName;

    [ObservableProperty]
    [Required]
    [NotifyCanExecuteChangedFor(nameof(AddToolCommand))]
    private string _toolToAddDescription;

    public ToolsViewModel(WorkplanModel workplanModel) : base(workplanModel)
    {

        ToolToAddName = "";
        ToolToAddDescription = "";
        this.workplanModel = workplanModel;
    }

    private bool HasToolToAdd => ToolToAddName != "" && ToolToAddDescription != "";

    [RelayCommand(CanExecute = nameof(HasToolToAdd))]
    private void AddTool()
    {
        var toolToAdd = new Tool(Tools.Count + 1, ToolToAddName, ToolToAddDescription);
        Tools.Add(toolToAdd);
        ToolToAddName = "";
        ToolToAddDescription = "";
    }

    [RelayCommand]
    private void DeleteTool(Tool toolToDelete)
    {
        Tools.Remove(toolToDelete);
        workplanModel.MachineTypes.ToList().ForEach(machine =>
        {

        });
    }
}