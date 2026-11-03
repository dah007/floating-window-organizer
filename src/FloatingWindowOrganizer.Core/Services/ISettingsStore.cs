using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
