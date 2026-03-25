using Microsoft.JSInterop;

namespace LearningFlashCards.Web.Client.Services;

public class UserSessionService(IJSRuntime js)
{
    private const string StorageKey = "lfc_owner_id";

    private Guid? _cachedOwnerId;

    public async Task<Guid?> GetOwnerIdAsync()
    {
        if (_cachedOwnerId.HasValue)
            return _cachedOwnerId;

        var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (Guid.TryParse(stored, out var id))
        {
            _cachedOwnerId = id;
            return id;
        }

        return null;
    }

    public async Task SetOwnerIdAsync(Guid ownerId)
    {
        _cachedOwnerId = ownerId;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, ownerId.ToString());
    }

    public async Task ClearAsync()
    {
        _cachedOwnerId = null;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    public bool IsLoggedIn => _cachedOwnerId.HasValue;
}
