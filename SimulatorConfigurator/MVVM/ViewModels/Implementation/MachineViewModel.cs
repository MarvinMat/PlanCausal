using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Abstraction.Domain.Models;
using SimulatorConfigurator.Core;
using SimulatorConfigurator.MVVM.Model;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SimulatorConfigurator.MVVM.ViewModels.Implementation;

public partial class MachineViewModel : ViewModel
{
    private readonly WorkplanModel workplanModel;
    [ObservableProperty] private MachineTypeVO? _selectedMachine;

    [Required(ErrorMessage = "TypeId is required.")]
    [MinLength(1)]
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddMachineCommand))]
    private int _typeId = 0;

    [Required]
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddMachineCommand))]
    private int _count = 1;

    [Required]
    [MinLength(3)]
    [MaxLength(30)]
    [ObservableProperty] private string name = "MyMachine";

    public ObservableCollection<ComboBoxItem<Tool>> AllowedTools { get; set; }

    [ObservableProperty] private double[][] _changeoverTimes = new double[0][];

    [ObservableProperty]
    [Required]
    [NotifyCanExecuteChangedFor(nameof(AddMachineCommand))]
    private MachineTypeVO? _machineToBeAdded = new(0, 0, "YourMachine", new int[0], new double[0][]);

    [ObservableProperty]
    private bool _isEditable = false;
    public ObservableCollection<MachineTypeVO> Machines => workplanModel.MachineTypes;

    private void Tools_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        AllowedTools = new ObservableCollection<ComboBoxItem<Tool>>();
        foreach (var tool in Tools)
        {
            AllowedTools.Add(new ComboBoxItem<Tool>(tool, false));
        }
    }
    private void AllowedTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AddMachineCommand.NotifyCanExecuteChanged();
    }

    private bool CanMachineBeAdded() => TypeId > 0 && Count != 0 && HasErrors == false && AllowedTools.Any(item => item.IsChecked == true);

    [RelayCommand(CanExecute = nameof(CanMachineBeAdded))]
    private void AddMachine()
    {
        var machineToBeAdded = new MachineTypeVO(
            TypeId,
            Count,
            Name,
            AllowedTools.Where(item => item.IsChecked == true).Select(item => item.Item.TypeId).ToArray(),
            ChangeoverTimes);


        if (!Machines.Any(machine => machine.TypeId == machineToBeAdded.TypeId))
        {
            Machines.Add(machineToBeAdded);
            SelectedMachine = null;
        }
    }
    [RelayCommand]
    private void EditMachine()
    {
        IsEditable = !IsEditable;
    }
    [RelayCommand]
    private void DeleteMachine(object parameter)
    {
        // Assuming 'parameter' is the item to delete
        var machine = parameter as MachineTypeVO;

        if (machine != null)
        {
            // Remove the item from the Machines collection
            Machines.Remove(machine);
        }
        SelectedMachine = null;
    }

    public MachineViewModel(WorkplanModel workplanModel) : base(workplanModel)
    {
        this.workplanModel = workplanModel;
        Tools_CollectionChanged(null, null);
        Tools.CollectionChanged += Tools_CollectionChanged;
        AllowedTools.CollectionChanged += AllowedTools_CollectionChanged;
    }


}


