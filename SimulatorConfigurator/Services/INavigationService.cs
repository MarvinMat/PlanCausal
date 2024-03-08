using SimulatorConfigurator.Core;

namespace SimulatorConfigurator.Services;

public interface INavigationService
{
    ViewModel CurrentView { get; set; }
    void NavigateTo<T>() where T : ViewModel;

}