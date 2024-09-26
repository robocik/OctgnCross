using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DialogHostAvalonia;
using Octgn.Core;
using OctgnCross.Controls;

namespace OctgnCross.Tabs.GameManagement;

public partial class AddFeed:UserControlBase
{
    private string _error;
    private string _feedName;
    private string _feedUrl;
    private string _feedUsername;
    private string _feedPassword;
    
    public DialogSession DialogSession { get; set; }
    public bool HasErrors { get; private set; }
    public string Error
    {
        get { return _error; }
        private set
        {
            _error = value;
            OnPropertyChanged(nameof(Error));
        }
    }

    public string FeedName
    {
        get { return _feedName; }
        private set { _feedName = value; }
    }

    public string FeedUrl
    {
        get { return _feedUrl; }
        private set { _feedUrl = value; }
    }
    
    public string FeedUsername
    {
        get { return _feedUsername; }
        private set { _feedUsername=value; }
    }

    public string FeedPassword
    {
        get { return _feedPassword; }
        private set { _feedPassword=value; }
    }

    // private Decorator Placeholder;

    public AddFeed()
    {
        InitializeComponent();
    }

    void ValidateFields(string name, string feed, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("You must enter a feed name");
        if (String.IsNullOrWhiteSpace(feed))
            throw new Exception("You must enter a feed path");
        var result = GameFeedManager.Get().ValidateFeedUrl(feed, username, password);
        if (result != FeedValidationResult.Valid)
        {
            switch (result)
            {
                case FeedValidationResult.InvalidFormat:
                    throw new Exception("This feed is in an invalid format.");
                case FeedValidationResult.InvalidUrl:
                    throw new Exception("The feed is down or it is not a valid feed");
                case FeedValidationResult.RequiresAuthentication:
                    throw new Exception("This feed requires authentication. Please enter a username and password.");
                case FeedValidationResult.Unknown:
                    throw new Exception("An unknown error has occured.");
            }
        }
    }

    void SetError(string error = "")
    {
        this.HasErrors = !string.IsNullOrWhiteSpace(error);
        this.Error = error;
    }

    #region Dialog


    void StartWait()
    {
        BorderHostGame.IsEnabled = false;
        ProgressBar.IsVisible = true;
        this.ProgressBar.IsIndeterminate = true;
    }

    void EndWait()
    {
        BorderHostGame.IsEnabled = true;
        ProgressBar.IsVisible = false;
        ProgressBar.IsIndeterminate = false;
    }

    #endregion

    #region UI Events
    private void ButtonCancelClick(object _, RoutedEventArgs e)
    {
        DialogSession.Close(DialogResult.Cancel);
    }

    private void ButtonAddClick(object sender, RoutedEventArgs e)
    {
        if (FeedName == null) FeedName = "";
        if (FeedUrl == null) FeedUrl = "";
        FeedName = FeedName.Trim();
        FeedUrl = FeedUrl.Trim();
        var feedName = FeedName;
        var feedUrl = FeedUrl;
        var username = FeedUsername;
        var password = FeedPassword;
	    this.StartWait();
	    // Program.Dispatcher = this.Dispatcher;
        var task = new Task(() =>
        {
            this.ValidateFields(feedName,feedUrl,username,password);
            GameFeedManager.Get().AddFeed(feedName, feedUrl, username, password);
        });
        task.ContinueWith((continueTask) =>
            {
                var error = "";
                if (continueTask.IsFaulted)
                {
                    if (continueTask.Exception != null)
                    {
                        error = continueTask.Exception.InnerExceptions[0].Message;
                    }
                    else error = "Unable to add feed. Try again later.";
                }
                Dispatcher.UIThread.Invoke(new Action(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(error))
                            this.SetError(error);
                        this.EndWait();
                        if (string.IsNullOrWhiteSpace(error))
                            DialogSession.Close(DialogResult.OK);
                    }));
            });
        task.Start();
    }
    #endregion

}