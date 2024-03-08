using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulatorConfigurator.Core;
using SimulatorConfigurator.MVVM.Model;
using SimulatorConfigurator.Services;

namespace SimulatorConfigurator.MVVM.ViewModels.Implementation;

public partial class MainViewModel : ViewModel
{
    [ObservableProperty]
    private INavigationService _navigationService;

    [RelayCommand]
    private void NavigateToHome()
    {
        NavigationService.NavigateTo<HomeViewModel>();
    }
    [RelayCommand]
    private void NavigateToTools()
    {
        NavigationService.NavigateTo<ToolsViewModel>();
    }
    [RelayCommand]
    private void NavigateToWorkPlans()
    {
        NavigationService.NavigateTo<WorkPlanViewModel>();
    }

    [RelayCommand]
    private void NavigateToMachines()
    {
        NavigationService.NavigateTo<MachineViewModel>();

    }

    public MainViewModel(INavigationService navigationService, WorkplanModel workplanModel) : base(workplanModel)
    {
        NavigationService = navigationService;
        NavigationService.NavigateTo<HomeViewModel>();
    }
}