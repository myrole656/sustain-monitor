using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using SustainMonitor.Domain;
using SustainMonitor.Web.Models;

namespace SustainMonitor.Web.Services;

/// <summary>
/// The signed-in user for this browser tab's circuit. Tokens are kept in the browser's local storage,
/// encrypted with ASP.NET Data Protection, so a page reload keeps you signed in.
/// </summary>
/// <summary>Where the session is kept. Browser local storage in the app; in memory for previews and tests.</summary>
public interface ISessionStore
{
    Task<AuthSession.Stored?> GetAsync();
    Task SetAsync(AuthSession.Stored value);
    Task DeleteAsync();
}

public sealed class BrowserSessionStore(ProtectedLocalStorage storage) : ISessionStore
{
    private const string Key = "sm.session";

    public async Task<AuthSession.Stored?> GetAsync()
    {
        var r = await storage.GetAsync<AuthSession.Stored>(Key);
        return r.Success ? r.Value : null;
    }

    public async Task SetAsync(AuthSession.Stored value) => await storage.SetAsync(Key, value);
    public async Task DeleteAsync() => await storage.DeleteAsync(Key);
}

public sealed class AuthSession(ISessionStore storage, ILogger<AuthSession> log)
{
    public sealed record Stored(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc, MeDto Me);

    private Stored? _data;
    public bool Loaded { get; private set; }
    public event Action? Changed;

    public bool IsAuthenticated => _data is not null;
    public MeDto? Me => _data?.Me;
    public string? AccessToken => _data?.AccessToken;
    public string? RefreshToken => _data?.RefreshToken;
    public bool AccessTokenExpired => _data is null || DateTime.UtcNow >= _data.ExpiresAtUtc.AddSeconds(-30);

    public bool IsPlatformAdmin => Me?.Roles.Contains(Roles.PlatformAdmin) == true;
    public CompanyRole? Role => Me?.Membership?.Role;
    public bool IsCeo => Role == CompanyRole.CEO;
    public bool IsPm => Role == CompanyRole.ProjectManager;
    public bool IsSuperior => Role == CompanyRole.Superior;
    public string DisplayName => Me?.FullName ?? Me?.Email ?? "";
    public string RoleLabel => IsPlatformAdmin ? "Platform admin" : Role switch
    {
        CompanyRole.CEO => "CEO",
        CompanyRole.ProjectManager => "Project Manager",
        CompanyRole.Superior => "Superior",
        _ => "",
    };

    /// <summary>Reads the stored session. Must run after the first render (needs JS interop).</summary>
    public async Task LoadAsync()
    {
        if (Loaded) return;
        try
        {
            _data = await storage.GetAsync();
        }
        catch (Exception ex)
        {
            // Keys changed or storage tampered with: start signed out.
            log.LogInformation(ex, "Stored session could not be read");
            _data = null;
        }
        Loaded = true;
        Changed?.Invoke();
    }

    public async Task SetAsync(TokenResponse tokens, MeDto me)
    {
        _data = new Stored(tokens.AccessToken, tokens.RefreshToken, DateTime.UtcNow.AddSeconds(tokens.ExpiresIn), me);
        await storage.SetAsync(_data);
        Changed?.Invoke();
    }

    public async Task UpdateTokensAsync(TokenResponse tokens)
    {
        if (_data is null) return;
        _data = _data with
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn),
        };
        await storage.SetAsync(_data);
    }

    public async Task UpdateMeAsync(MeDto me)
    {
        if (_data is null) return;
        _data = _data with { Me = me };
        await storage.SetAsync(_data);
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        _data = null;
        try { await storage.DeleteAsync(); } catch { /* circuit may be closing */ }
        Changed?.Invoke();
    }
}
