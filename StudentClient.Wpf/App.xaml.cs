using System.Windows;

namespace StudentClient.Wpf;

public partial class App : Application
{
    private readonly Bootstrapper _bootstrapper;

    public App()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _bootstrapper = new Bootstrapper();
    }
}