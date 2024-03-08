using CommunityToolkit.Mvvm.ComponentModel;
using Core.Abstraction.Domain.Models;
using SimulatorConfigurator.MVVM.Model;
using System.Collections.ObjectModel;

namespace SimulatorConfigurator.Core;

public abstract class ViewModel : ObservableValidator
{
    private WorkplanModel workplanModel;

    public ObservableCollection<Tool> Tools => workplanModel.Tools;

    public ViewModel(WorkplanModel workplanModel)
    {
        this.workplanModel = workplanModel;
    }
}