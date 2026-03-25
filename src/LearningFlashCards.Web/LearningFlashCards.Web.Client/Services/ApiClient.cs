using System.Net.Http.Json;
using LearningFlashCards.Core.Domain.Entities;

namespace LearningFlashCards.Web.Client.Services;

public class ApiClient(HttpClient http, UserSessionService session)
{
    // Users
    public async Task<UserProfile?> CreateProfileAsync(string username, string email, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/users", new { Username = username, Email = email }, ct);
        response.EnsureSuccessStatusCode();

        if (response.Headers.TryGetValues("X-Owner-Id", out var values) &&
            Guid.TryParse(values.FirstOrDefault(), out var ownerId))
        {
            await session.SetOwnerIdAsync(ownerId);
        }

        return await response.Content.ReadFromJsonAsync<UserProfile>(ct);
    }

    public async Task<UserProfile?> GetProfileAsync(CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Get, "api/users/me", ct);
        var response = await http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfile>(ct);
    }

    // Decks
    public async Task<List<Deck>> GetDecksAsync(CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Get, "api/decks", ct);
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Deck>>(ct) ?? [];
    }

    public async Task<Deck?> GetDeckAsync(Guid deckId, CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Get, $"api/decks/{deckId}", ct);
        var response = await http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Deck>(ct);
    }

    public async Task<Deck?> UpsertDeckAsync(object deckRequest, CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Post, "api/decks", ct);
        request.Content = JsonContent.Create(deckRequest);
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Deck>(ct);
    }

    public async Task DeleteDeckAsync(Guid deckId, CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Delete, $"api/decks/{deckId}", ct);
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    // Cards
    public async Task<List<Card>> GetCardsAsync(Guid deckId, CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Get, $"api/decks/{deckId}/cards", ct);
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Card>>(ct) ?? [];
    }

    public async Task<Card?> UpsertCardAsync(Guid deckId, object cardRequest, CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Post, $"api/decks/{deckId}/cards", ct);
        request.Content = JsonContent.Create(cardRequest);
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Card>(ct);
    }

    public async Task DeleteCardAsync(Guid deckId, Guid cardId, CancellationToken ct = default)
    {
        using var request = await BuildRequest(HttpMethod.Delete, $"api/decks/{deckId}/cards/{cardId}", ct);
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpRequestMessage> BuildRequest(HttpMethod method, string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        var ownerId = await session.GetOwnerIdAsync();
        if (ownerId.HasValue)
            request.Headers.Add("X-Owner-Id", ownerId.Value.ToString());
        return request;
    }
}
