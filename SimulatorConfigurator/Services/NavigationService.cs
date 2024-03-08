using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SimulatorConfigurator.Core;

namespace SimulatorConfigurator.Services;

public partial class NavigationService : ObservableObject, INavigationService 
{
    [ObservableProperty]
    private ViewModel _currentView;

    private readonly Func<Type,ViewModel> _viewModelFactory;
  
    public void NavigateTo<TViewModel>() where TViewModel : ViewModel
    {
       ViewModel viewmodel = _viewModelFactory.Invoke(typeof(TViewModel));
       CurrentView = viewmodel;
    }

    public NavigationService(Func<Type, ViewModel> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }
}