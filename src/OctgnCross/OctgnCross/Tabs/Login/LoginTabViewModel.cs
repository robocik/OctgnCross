using System;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using Octgn.Core;
using Octgn.Extentions;
using Octgn.Library.Localization;
using Octgn.Site.Api;
using ViewModelBase = Octgn.UI.ViewModelBase;

namespace Octgn.Tabs.Login;

public class LoginTabViewModel : ViewModelBase
{
    internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

    private bool _isBusy;
    private string _username;
    private string _password;
    private string _errorString;
    private bool _loggedIn;

    public bool IsBusy
    {
        get { return _isBusy; }
        set
        {
            if (value == _isBusy) return;
            _isBusy = value;
            RaisePropertyChanged(() => IsBusy);
            RaisePropertyChanged(() => NotBusy);
        }
    }

    public bool NotBusy { get { return !_isBusy; } }

    public string Username
    {
        get { return _username; }
        set
        {
            if (_username == value) return;
            _username = value;
            RaisePropertyChanged(() => Username);
        }
    }

    public string Password
    {
        get { return _password; }
        set
        {
            if (_password == value) return;
            _password = value;
            RaisePropertyChanged(() => Password);
        }
    }

    public string ErrorString
    {
        get { return _errorString; }
        set
        {
            if (_errorString == value) return;
            _errorString = value;
            RaisePropertyChanged(() => ErrorString);
            RaisePropertyChanged(() => HasError);
        }
    }

    public bool HasError
    {
        get { return String.IsNullOrWhiteSpace(ErrorString) == false; }
    }

    public bool LoggedIn
    {
        get { return _loggedIn; }
        set
        {
            if (_loggedIn == value) return;
            _loggedIn = value;
            RaisePropertyChanged(()=>LoggedIn);
        }
    }

    public NewsViewModel News { get; } = new NewsViewModel();

    public LoginTabViewModel()
    {
        // Password = Prefs.Password != null ? Prefs.Password.Decrypt() : null;
        Password = Prefs.Password != null ? Prefs.Password/*.Decrypt()*/ : null;//TODO
        Username = Prefs.Username;
    }

    public void LoginAsync()
    {
        Task.Factory.StartNew(Login);
    }

    public async Task Login()
    {
        try
        {
            lock (this)
            {
                if (IsBusy) return;
                IsBusy = true;
            }
            ErrorString = "";

            var webLoginResult = await LoginWithWebsite();
            if (!webLoginResult.success)
                return;

            //The rest of the application and the api uses Username as a key. To avoid changing the rest of the application,
            //     I use this to get the username back from the server instead of using the one the user typed in.
            //     This is just in case we logged in using the users email instead of their username.
            Username = webLoginResult.username;

            App.LobbyClient.ConfigureSession(webLoginResult.sessionKey, new Communication.User(webLoginResult.userId, webLoginResult.username), Prefs.DeviceId);

            Prefs.Username = webLoginResult.username;
            Prefs.Nickname = webLoginResult.username;
            Prefs.Password = Password;//.Encrypt();
            Prefs.SessionKey = webLoginResult.sessionKey;

            await App.LobbyClient.Connect(default); //TODO: Cancellation for login timeouts, but only if it's not already built into the com library

            if (Prefs.Username == null || Prefs.Username.Equals(Username, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                // Logging in with a new username, so clear admin flag
                Prefs.IsAdmin = false;
            }

            LoggedIn = true;
        }
        catch (Exception e)
        {
            Log.Error("Login", e);
            ErrorString = "An unknown error occured. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<(bool success, string sessionKey, string username, string userId)> LoginWithWebsite() {
        Log.Debug(nameof(LoginWithWebsite));

        var client = new ApiClient();
        var result = await client.CreateSession(Username, Password, Prefs.DeviceId);

        Log.Info($"{nameof(LoginWithWebsite)}: {result.Result.Type}: {result.Result.Username}");

        if (!HandleLoginResultType(result.Result.Type.ToString())) {
            Log.Warn($"{nameof(LoginWithWebsite)}: Login failed: {result.Result.Type}");
            return (false, null, null, null);
        }

        return (true, result.SessionKey, result.Result.Username, result.UserId);
    }

    private bool HandleLoginResultType(string resultTypeString) {

        switch (resultTypeString) {
            case nameof(LoginResultType.Ok):
                return true;
            case nameof(LoginResultType.UnknownError):
                ErrorString = L.D.LoginMessage__UnknownError;
                return false;
            case nameof(LoginResultType.EmailUnverified):
                ErrorString = L.D.LoginMessage__EmailUnverified;
                return false;
            case nameof(LoginResultType.UnknownUsername):
                ErrorString = L.D.LoginMessage__UnknownUsername;
                return false;
            case nameof(LoginResultType.PasswordWrong):
                ErrorString = L.D.LoginMessage__WrongPassword;
                return false;
            case nameof(LoginResultType.NotSubscribed):
                ErrorString = L.D.LoginMessage__NotSubscribed;
                return false;
            case nameof(LoginResultType.NoEmailAssociated):
                ErrorString = L.D.LoginMessage__NoEmail;
                return false;
            default:
                throw new NotImplementedException($"{nameof(HandleLoginResultType)}: {resultTypeString}");
        }
    }
}