using Iderma.Core.Services;
using Microsoft.UI.Xaml;

namespace Iderma_UserAdmin;

public partial class App : Application
{
    private Window? _window;
    public static UserStore Users { get; } = new();
    public static SessionStore Sessions { get; } = new();

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
