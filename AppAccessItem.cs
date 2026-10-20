using System.ComponentModel;
using System.Runtime.CompilerServices;
using Iderma.Core.Catalog;

namespace Iderma_UserAdmin;

public sealed class AppAccessItem : INotifyPropertyChanged
{
    private bool _isGranted;

    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string StatusLabel { get; init; }
    public required string LogoPath { get; init; }

    public bool IsGranted
    {
        get => _isGranted;
        set
        {
            if (_isGranted == value)
            {
                return;
            }

            _isGranted = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static List<AppAccessItem> CreateAll(IEnumerable<string>? grantedIds = null)
    {
        var granted = new HashSet<string>(grantedIds ?? [], StringComparer.OrdinalIgnoreCase);
        return AppCatalog.All
            .OrderBy(app => AppCatalog.DisplayOrder(app.Status))
            .ThenBy(app => app.Id.Equals(AppCatalog.Cuentas, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(app => new AppAccessItem
            {
                Id = app.Id,
                Name = app.Name,
                Description = app.Description,
                StatusLabel = AppCatalog.StatusLabel(app.Status),
                LogoPath = AppCatalog.GetLogoUri(app.LogoFileName),
                IsGranted = granted.Contains(app.Id)
            }).ToList();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
