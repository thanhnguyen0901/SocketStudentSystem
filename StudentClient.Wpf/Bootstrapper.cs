using Caliburn.Micro;
using StudentClient.Wpf.ViewModels;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace StudentClient.Wpf;

public class Bootstrapper : BootstrapperBase
{
    private readonly SimpleContainer _container = new();

    public Bootstrapper()
    {
        Initialize();
    }

    protected override IEnumerable<Assembly> SelectAssemblies()
        => [Assembly.GetExecutingAssembly()];

    protected override void Configure()
    {
        _container.Instance(_container);

        // Caliburn.Micro infrastructure.
        _container
            .Singleton<IWindowManager, WindowManager>()
            .Singleton<IEventAggregator, EventAggregator>();

        // Application services (singletons share one TCP connection).
        _container
            .Singleton<Services.TcpClientService>()
            .Singleton<Services.TcpStudentService>();

        // Root ViewModel
        _container.Singleton<ShellViewModel>();
    }

    protected override object GetInstance(Type service, string key)
        => _container.GetInstance(service, key)
           ?? throw new InvalidOperationException(
               $"Could not locate any instances of contract {key ?? service.Name}.");

    protected override IEnumerable<object> GetAllInstances(Type service) => _container.GetAllInstances(service);

    protected override void BuildUp(object instance) => _container.BuildUp(instance);

    protected override async void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            await DisplayRootViewForAsync<ShellViewModel>();
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[OnStartup] {ex}");
            Application.Current.Shutdown(1);
        }
    }

    protected override void OnUnhandledException(object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Trace.TraceError($"[DispatcherUnhandledException] {e.Exception}");

        if (Application.Current.Windows.Count == 0)
        {
            Application.Current.Shutdown(1);
        }
    }
}
