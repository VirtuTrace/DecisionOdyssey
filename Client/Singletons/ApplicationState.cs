using Client.JSWrappers;
using Client.Models.DecisionElements.DecisionMatrix;
using Common.DataStructures.Http.Responses;

namespace Client.Singletons;

public class ApplicationState
{
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string EmailKey = "email";
    private const string DarkModeKey = "darkMode";
    
    
    private LocalStorageAccessor _localStorageAccessor = null!;
    private bool _darkMode;

    public bool LoggedIn { get; private set; }
    public bool IsAdmin { get; set; }

    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            _darkMode = value;
            OnDarkModeChanged?.Invoke();
        }
    }

    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DecisionMatrix? SelectedMatrix { get; set; }
    public int[]? RowRatings { get; set; }

    public bool GuestLogin => Email.StartsWith("Guest");

    public event Action? OnLoggedInStateChanged;
    public event Action? OnDarkModeChanged;

    public async Task StoreCredentials(AuthResponse authResponse, string? email = null)
    {
        LoggedIn = true;
        
        if (authResponse is GuestAuthResponse guestAuthResponse)
        {
            Email = guestAuthResponse.GuestId;
        }
        else if (email is null)
        {
            throw new ArgumentNullException(nameof(email));
        }
        else
        {
            Email = email;
        }
        
        AccessToken = authResponse.AccessToken;
        RefreshToken = authResponse.RefreshToken;
        await _localStorageAccessor.SetValueAsync(EmailKey, email);
        await _localStorageAccessor.SetValueAsync(AccessTokenKey, AccessToken);
        await _localStorageAccessor.SetValueAsync(RefreshTokenKey, RefreshToken);
        OnLoggedInStateChanged?.Invoke();
    }
    
    public async Task ClearCredentials()
    {
        LoggedIn = false;
        Email = "";
        AccessToken = "";
        RefreshToken = "";
        await _localStorageAccessor.RemoveAsync(EmailKey);
        await _localStorageAccessor.RemoveAsync(AccessTokenKey);
        await _localStorageAccessor.RemoveAsync(RefreshTokenKey);
        OnLoggedInStateChanged?.Invoke();
    }
    
    public async Task InitializeAsync(LocalStorageAccessor localStorageAccessor)
    {
        _localStorageAccessor = localStorageAccessor;
        AccessToken = await localStorageAccessor.GetValueOrDefaultAsync(AccessTokenKey, string.Empty);
        RefreshToken = await localStorageAccessor.GetValueOrDefaultAsync(RefreshTokenKey, string.Empty);
        Email = await localStorageAccessor.GetValueOrDefaultAsync(EmailKey, string.Empty);
        DarkMode = await localStorageAccessor.GetValueOrDefaultAsync(DarkModeKey, false);
        LoggedIn = !string.IsNullOrWhiteSpace(RefreshToken) && !string.IsNullOrWhiteSpace(Email);
    }
}