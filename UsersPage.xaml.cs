using Iderma.Core.Catalog;
using Iderma.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Iderma_UserAdmin;

public sealed partial class UsersPage : Page
{
    private string? _editingId;
    private List<AppAccessItem> _accessItems = [];
    private bool _suppressSelection;
    private bool _notifyLogistica = true;
    private bool _notifyClinica = true;
    private bool _notifyBaseDeDatos = true;
    private bool _suspendNotify;
    private int _notifyGeneration;

    public UsersPage()
    {
        InitializeComponent();
        RoleBox.ItemsSource = new[]
        {
            "Administrador",
            "Desarrollador",
            "Médico",
            "Logística",
            "Marketing",
            "Gerencia",
            "Recepción",
            "Usuario"
        };
        RoleBox.SelectedItem = "Usuario";
        Loaded += UsersPage_Loaded;
        Unloaded += UsersPage_Unloaded;
        StartNew();
    }

    private void UsersPage_Loaded(object sender, RoutedEventArgs e)
    {
        App.Users.Changed += OnDataChanged;
        App.Sessions.Changed += OnDataChanged;
        _ = RefrescarListaAsync(_editingId);
    }

    private void UsersPage_Unloaded(object sender, RoutedEventArgs e)
    {
        App.Users.Changed -= OnDataChanged;
        App.Sessions.Changed -= OnDataChanged;
    }

    private void OnDataChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var selected = _editingId;
            _ = RefrescarListaAsync(selected);
            if (selected is null)
            {
                return;
            }

            var user = App.Users.FindById(selected);
            if (user is null)
            {
                StartNew();
                ShowMessage("La cuenta fue modificada en otra aplicación.", InfoBarSeverity.Informational);
                return;
            }

            LoadUser(user);
        });
    }

    private async Task RefrescarListaAsync(string? selectId = null)
    {
        List<UserListItem> users;
        try
        {
            users = await Task.Run(() => App.Users.GetAll()
                .Select(user =>
                {
                    var equipos = App.Sessions.EquiposDe(user.Id);
                    return new UserListItem
                    {
                        Account = user,
                        IsOnline = equipos.Count > 0,
                        Equipos = equipos
                    };
                })
                .ToList());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return;
        }

        _suppressSelection = true;
        UsersList.ItemsSource = users;
        CountText.Text = users.Count == 1 ? "1 cuenta" : $"{users.Count} cuentas";

        if (selectId is not null)
        {
            UsersList.SelectedItem = users.FirstOrDefault(item => item.Id == selectId);
        }

        _suppressSelection = false;
    }

    private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection)
        {
            return;
        }

        if (UsersList.SelectedItem is UserListItem item)
        {
            LoadUser(App.Users.FindById(item.Id) ?? item.Account);
        }
    }

    private void NewButton_Click(object sender, RoutedEventArgs e) => StartNew();

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingId is null)
        {
            StartNew();
            return;
        }

        var current = App.Users.FindById(_editingId);
        if (current is not null)
        {
            LoadUser(current);
        }
        else
        {
            StartNew();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var appIds = _accessItems.Where(item => item.IsGranted).Select(item => item.Id);
            var username = UsernameBox.Text ?? string.Empty;
            var displayName = DisplayNameBox.Text ?? string.Empty;
            var role = RoleBox.Text ?? "Usuario";
            var password = PasswordBox.Password ?? string.Empty;
            var isActive = ActiveSwitch.IsOn;
            var canOpenSettings = SettingsSwitch.IsOn;
            var canExportLogins = ExportLoginsSwitch.IsOn;
            var notifyLogistica = _notifyLogistica;
            var notifyClinica = _notifyClinica;
            var notifyBaseDeDatos = _notifyBaseDeDatos;

            UserAccount saved;
            if (_editingId is null)
            {
                saved = App.Users.Create(
                    username, displayName, password, role, isActive, canOpenSettings, canExportLogins,
                    notifyLogistica, notifyClinica, notifyBaseDeDatos, appIds, mustChangePassword: true);
                ShowMessage("Usuario creado. Deberá cambiar la contraseña al iniciar sesión.", InfoBarSeverity.Success);
            }
            else
            {
                saved = App.Users.Update(
                    _editingId, username, displayName, password, role, isActive, canOpenSettings, canExportLogins,
                    notifyLogistica, notifyClinica, notifyBaseDeDatos, appIds);
                ShowMessage("Usuario actualizado.", InfoBarSeverity.Success);
            }

            PasswordBox.Password = string.Empty;
            _ = RefrescarListaAsync(saved.Id);
            LoadUser(saved);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void LogoutSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingId is null)
        {
            ShowMessage("Selecciona un usuario para cerrar su sesión.", InfoBarSeverity.Warning);
            return;
        }

        var equipos = App.Sessions.EquiposDe(_editingId);
        if (equipos.Count == 0)
        {
            ShowMessage("Esa cuenta no tiene una sesión abierta.", InfoBarSeverity.Informational);
            return;
        }

        var donde = string.Join(", ", equipos);
        var dialog = new ContentDialog
        {
            Title = "Cerrar sesión",
            Content = $"¿Cerrar la sesión de {UsernameBox.Text} en {donde}?",
            PrimaryButtonText = "Cerrar sesión",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            if (!App.Sessions.ForceLogout(_editingId))
            {
                ShowMessage("Esa cuenta ya no tenía una sesión abierta.", InfoBarSeverity.Informational);
                _ = RefrescarListaAsync(_editingId);
                return;
            }

            ShowMessage($"Se cerró la sesión en {donde}.", InfoBarSeverity.Success);
            _ = RefrescarListaAsync(_editingId);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingId is null)
        {
            ShowMessage("Selecciona un usuario para eliminarlo.", InfoBarSeverity.Warning);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Eliminar usuario",
            Content = $"¿Eliminar la cuenta {UsernameBox.Text}?",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            App.Users.Delete(_editingId);
            ShowMessage("Usuario eliminado.", InfoBarSeverity.Success);
            _ = RefrescarListaAsync();
            StartNew();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, InfoBarSeverity.Error);
        }
    }

    private void StartNew()
    {
        _suppressSelection = true;
        UsersList.SelectedItem = null;
        _suppressSelection = false;
        _editingId = null;
        EditorTitle.Text = "Nuevo usuario";
        UsernameBox.Text = string.Empty;
        DisplayNameBox.Text = string.Empty;
        RoleBox.Text = "Usuario";
        PasswordBox.Password = string.Empty;
        PasswordBox.Header = "Contraseña";
        PasswordBox.PlaceholderText = "Mínimo 10 caracteres, letra y número";
        ActiveSwitch.IsOn = true;
        SettingsSwitch.IsOn = false;
        ExportLoginsSwitch.IsOn = false;
        ShowNotificationSwitches(logistica: false, clinica: false, baseDeDatos: false);
        BindAccess([AppCatalog.Hardware, AppCatalog.Reclamos, AppCatalog.Incidencias]);
        MessageBar.IsOpen = false;
    }

    private void LoadUser(UserAccount user)
    {
        _editingId = user.Id;
        EditorTitle.Text = user.DisplayName;
        UsernameBox.Text = user.Username;
        DisplayNameBox.Text = user.DisplayName;
        RoleBox.Text = user.Role;
        PasswordBox.Password = string.Empty;
        PasswordBox.Header = "Nueva contraseña";
        PasswordBox.PlaceholderText = "Déjala vacía para no cambiarla";
        ActiveSwitch.IsOn = user.IsActive;
        SettingsSwitch.IsOn = user.CanOpenSettings;
        ExportLoginsSwitch.IsOn = user.CanExportLogins;
        ShowNotificationSwitches(user.NotifyLogistica, user.NotifyClinica, user.NotifyBaseDeDatos);
        BindAccess(user.AppIds);
    }

    private void NotifyLogisticaSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suspendNotify)
        {
            return;
        }

        _notifyLogistica = NotifyLogisticaSwitch.IsOn;
    }

    private void NotifyClinicaSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suspendNotify)
        {
            return;
        }

        _notifyClinica = NotifyClinicaSwitch.IsOn;
    }

    private void NotifyBaseDeDatosSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suspendNotify)
        {
            return;
        }

        _notifyBaseDeDatos = NotifyBaseDeDatosSwitch.IsOn;
    }

    private void ShowNotificationSwitches(bool logistica, bool clinica, bool baseDeDatos)
    {
        _notifyLogistica = logistica;
        _notifyClinica = clinica;
        _notifyBaseDeDatos = baseDeDatos;
        var generation = ++_notifyGeneration;
        _suspendNotify = true;
        NotifyLogisticaSwitch.IsOn = logistica;
        NotifyClinicaSwitch.IsOn = clinica;
        NotifyBaseDeDatosSwitch.IsOn = baseDeDatos;
        DispatcherQueue.TryEnqueue(() =>
        {
            if (generation != _notifyGeneration)
            {
                return;
            }

            if (NotifyLogisticaSwitch.IsOn != logistica)
            {
                NotifyLogisticaSwitch.IsOn = logistica;
            }

            if (NotifyClinicaSwitch.IsOn != clinica)
            {
                NotifyClinicaSwitch.IsOn = clinica;
            }

            if (NotifyBaseDeDatosSwitch.IsOn != baseDeDatos)
            {
                NotifyBaseDeDatosSwitch.IsOn = baseDeDatos;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                if (generation == _notifyGeneration)
                {
                    _suspendNotify = false;
                }
            });
        });
    }

    private void BindAccess(IEnumerable<string> granted)
    {
        _accessItems = AppAccessItem.CreateAll(granted);
        AppsList.ItemsSource = _accessItems;
    }

    private void ShowMessage(string message, InfoBarSeverity severity)
    {
        MessageBar.Severity = severity;
        MessageBar.Title = severity == InfoBarSeverity.Error ? "No se pudo guardar" : "Listo";
        MessageBar.Message = message;
        MessageBar.IsOpen = true;
    }
}
