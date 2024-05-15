using Client.JSWrappers;
using MudBlazor.Components.Chart;

namespace Client.Singletons;

public class ApplicationState
{
    private LocalStorageAccessor _localStorageAccessor = null!;
    private bool _darkMode;

    public bool LoggedIn { get; private set; }

    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            _darkMode = value;
            OnDarkModeChanged?.Invoke();
        }
    }

    public string Token { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Matrix? SelectedMatrix { get; set; }
    public int[]? RowRatings { get; set; }

    public bool GuestLogin => Email.StartsWith("Guest");

    public event Action? OnLoggedInStateChanged;
    public event Action? OnDarkModeChanged;

    public async Task LoginAsync(string email, string token)
    {
        LoggedIn = true;
        Email = email;
        Token = token;
        await _localStorageAccessor.SetValueAsync("email", email);
        await _localStorageAccessor.SetValueAsync("token", token);
        OnLoggedInStateChanged?.Invoke();
    }
    
    public async Task LogoutAsync()
    {
        LoggedIn = false;
        Email = "";
        Token = "";
        await _localStorageAccessor.RemoveAsync("email");
        await _localStorageAccessor.RemoveAsync("token");
        OnLoggedInStateChanged?.Invoke();
    }
    
    public async Task InitializeAsync(LocalStorageAccessor localStorageAccessor)
    {
        _localStorageAccessor = localStorageAccessor;
        if (!await localStorageAccessor.CheckValueExistsAsync("token"))
        {
            LoggedIn = false;
            return;
        }

        LoggedIn = true;
        Token = await localStorageAccessor.GetValueAsync<string>("token");
        Email = await localStorageAccessor.GetValueAsync<string>("email");
    }
}