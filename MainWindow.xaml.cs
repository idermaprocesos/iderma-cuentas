using Iderma.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace Iderma_UserAdmin;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Iderma.Shell.BotonLauncher.Colocar(AppTitleBar);
        AppWindow.SetIcon(RutaIcono());
        AppTitleBar.Title = AppInfo.AdminProductName;
        Title = AppInfo.AdminProductName;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        RootFrame.Navigate(typeof(UsersPage));
    }

    private static string RutaIcono()
    {
        var juntoAlEjecutable = Path.Combine(AppContext.BaseDirectory, "img", "logo-cuentas.ico");
        if (File.Exists(juntoAlEjecutable))
        {
            return juntoAlEjecutable;
        }

        return Path.Combine(Environment.CurrentDirectory, "img", "logo-cuentas.ico");
    }
}
