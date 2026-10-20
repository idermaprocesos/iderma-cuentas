using Iderma.Core.Models;

namespace Iderma_UserAdmin;

public sealed class UserListItem
{
    public required UserAccount Account { get; init; }
    public required bool IsOnline { get; init; }
    public IReadOnlyList<string> Equipos { get; init; } = [];
    public string Id => Account.Id;
    public string DisplayName => Account.DisplayName;
    public string Username => Account.Username;
    public string Detail =>
        Equipos.Count == 0
            ? Account.Role
            : $"{Account.Role} · En línea · {string.Join(", ", Equipos)}";

    public string Avisos =>
        $"Avisos: {(Account.NotifyLogistica ? "logística" : "sin logística")} · {(Account.NotifyClinica ? "clínica" : "sin clínica")} · {(Account.NotifyBaseDeDatos ? "base de datos" : "sin base de datos")}";
}
