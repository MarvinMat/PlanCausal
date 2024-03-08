using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulatorConfigurator.Core;
using SimulatorConfigurator.MVVM.Model;
using System.Text.Json;

namespace SimulatorConfigurator.MVVM.ViewModels.Implementation;

public partial class HomeViewModel : ViewModel
{
    private readonly WorkplanModel _workplanModel;

    [ObservableProperty]
    private string _jsonStringWorkPlan = string.Empty;

    [ObservableProperty]
    private string _jsonStringMachineTypes = string.Empty;

    [ObservableProperty]
    private string _jsonStringTools = string.Empty;

    [RelayCommand]
    private void GenerateJsonOutput()
    {
        var options = new JsonSerializerOptions() { WriteIndented = true };

        JsonStringWorkPlan = JsonSerializer.Serialize(_workplanModel.WorkPlans, options);
        JsonStringMachineTypes = JsonSerializer.Serialize(_workplanModel.MachineTypes, options);
        JsonStringTools = JsonSerializer.Serialize(_workplanModel.Tools, options);

    }

    public HomeViewModel(WorkplanModel workplanModel) : base(workplanModel)
    {
        _workplanModel = workplanModel;
    }
}