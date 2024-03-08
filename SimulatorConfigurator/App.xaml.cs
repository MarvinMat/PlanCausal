
using Microsoft.Extensions.DependencyInjection;
using SimulatorConfigurator.Core;
using SimulatorConfigurator.MVVM.Model;
using SimulatorConfigurator.MVVM.ViewModels.Implementation;
using SimulatorConfigurator.Services;
using System;
using System.Windows;

namespace SimulatorConfigurator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<MainWindow>(provider => new MainWindow()
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            });

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MachineViewModel>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<ToolsViewModel>();
            services.AddSingleton<WorkPlanViewModel>();
            services.AddSingleton<WorkplanModel>();

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<Func<Type, ViewModel>>(serviceProvider =>
                viewModelType => (ViewModel)serviceProvider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow?.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            await _serviceProvider.DisposeAsync();
        }
    }

}