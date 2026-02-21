using Caliburn.Micro;
using StudentClient.Wpf.ViewModels;
using System.Windows;

namespace StudentClient.Wpf;

/// <summary>
/// Caliburn.Micro application bootstrapper.
/// Responsible for IoC container configuration and root view activation.
/// </summary>
public class Bootstrapper : BootstrapperBase
{
    // SimpleContainer is Caliburn's built-in lightweight IoC container.
    private readonly SimpleContainer _container = new();

    public Bootstrapper()
    {
        // Triggers Caliburn's internal setup (convention discovery, etc.).
        Initialize();
    }

    /// <summary>
    /// Register all application services and view-models with the container.
    /// </summary>
    protected override void Configure()
    {
        // Register the container itself so it can be injected if needed.
        _container.Instance(_container);

        // Caliburn infrastructure
        _container
            .Singleton<IWindowManager, WindowManager>()
            .Singleton<IEventAggregator, EventAggregator>();

        // Root view-model (one instance per request so the screen lifecycle works correctly)
        _container.PerRequest<ShellViewModel>();
    }

    /// <summary>Resolve a single service from the container.</summary>
    protected override object GetInstance(Type service, string key)
        => _container.GetInstance(service, key);

    /// <summary>Resolve all registrations for a service type.</summary>
    protected override IEnumerable<object> GetAllInstances(Type service)
        => _container.GetAllInstances(service);

    /// <summary>Perform property injection on an existing instance.</summary>
    protected override void BuildUp(object instance)
        => _container.BuildUp(instance);

    /// <summary>
    /// Called when the application starts.
    /// Displays the root <see cref="ShellViewModel"/> window via Caliburn conventions.
    /// </summary>
    protected override async void OnStartup(object sender, StartupEventArgs e)
        => await DisplayRootViewForAsync<ShellViewModel>();
}
