using System;
using System.ComponentModel;
using NuGet.Protocol.Core.Types;
using Octgn.Core;
using Octgn.Core.Annotations;
using Octgn.Core.DataManagers;
using Octgn.Library.Networking;
using OctgnCross.Core;

namespace OctgnCross.Tabs.GameManagement;

public class FeedGameViewModel : INotifyPropertyChanged,IEquatable<FeedGameViewModel>,IDisposable
{
    private readonly GameInfo _game;
    private Guid id;

    public GameInfo Game => _game;
    public IPackageSearchMetadata Package
    {
        get
        {
            return this._game.Package;
        }
        // set
        // {
        //     this.package = value;
        //     if (!Guid.TryParse(package.Identity.Id, out this.id)) this.id = Guid.Empty;
        //     this.OnPropertyChanged("Package");
        //     this.OnPropertyChanged("Installed");
        //     this.OnPropertyChanged("ImageUri");
        //     this.OnPropertyChanged("Id");
        //     this.OnPropertyChanged("InstallButtonText");
        // }
    }

    public Guid Id => id;

    public bool Installed
    {
        get
        {
            var isInstalled = GameManager.Get().GetById(Id) != null;
            return isInstalled;
        }
    }
    public Version InstalledVersion
    {
        get
        {
            if(!Installed)return new Version();
            return GameManager.Get().GetById(Id).Version;
        }
    }
    public string InstallButtonText
    {
        get
        {
            return Installed ? "Uninstall" : "Install";
        }
    }
    public Uri ImageUri
    {
        get
        {
            return Package == null
                       ? new Uri("pack://application:,,,/Octgn;Component/Resources/FileIcons/Game.ico")
                       : Package.IconUrl;
        }
    }
    public String Authors
    {
        get
        {
            return Package.Authors == null ? "" : String.Join(" ", Package.Authors);
        }
    }
    public FeedGameViewModel(GameInfo game)
    {
        _game = game;
        Guid.TryParse(game.Package.Identity.Id, out id);
        GameManager.Get().GameListChanged += OnGameListChanged;
    }

    private void OnGameListChanged(object sender, EventArgs eventArgs)
    {
        this.OnPropertyChanged("Package");
        this.OnPropertyChanged("Installed");
        this.OnPropertyChanged("ImageUri");
        this.OnPropertyChanged("Id");
        this.OnPropertyChanged("InstallButtonText");
    }

    #region PropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        if (handler != null)
        {
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion PropertyChanged

    public bool Equals(FeedGameViewModel other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    #region Implementation of IDisposable

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        GameManager.Get().GameListChanged -= OnGameListChanged;
    }

    #endregion
}