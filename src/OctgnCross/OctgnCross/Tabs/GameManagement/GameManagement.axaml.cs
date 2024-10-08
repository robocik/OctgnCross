using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DialogHostAvalonia;
using log4net;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NuGet.Protocol.Core.Types;
using Octgn;
using Octgn.Core;
using Octgn.Core.Annotations;
using Octgn.Core.DataManagers;
using Octgn.Extentions;
using Octgn.Library;
using Octgn.Library.Exceptions;
using Octgn.Library.Networking;
using Octgn.Utils;
using OctgnCross.Controls;
using OctgnCross.Core;
using OctgnCross.UI;

namespace OctgnCross.Tabs.GameManagement;

public partial class GameManagement : UserControlBase
{
    private static ILog Log = LogManager.GetLogger( MethodBase.GetCurrentMethod().DeclaringType );

	public ObservableCollection<NamedUrl> Feeds { get; set; }
	private FeedGameViewModel selectedGame;
	private NamedUrl selected;
	public NamedUrl Selected {
		get { return this.selected; }
		set {
			if(Equals( value, this.selected )) {
				return;
			}
			if(value?.Name == null ||
				FeedProvider.Instance.ReservedFeeds.Any( x => x.Name.Equals( value ) )) {
				RemoveButtonEnabled = false;
			} else { RemoveButtonEnabled = true; }
			this.selected = value;
			this.OnPropertyChanged( nameof( Selected ) );
			this.OnPropertyChanged( nameof( Packages ) );
			this.OnPropertyChanged( nameof( IsGameSelected ) );
			this.OnPropertyChanged( nameof( SelectedGame ) );
		}
	}

	public FeedGameViewModel SelectedGame {
		get { return this.selectedGame; }
		set {
			if(Equals( value, this.selectedGame )) {
				return;
			}
			if(selectedGame != null) {
				var old = selectedGame;
				old.Dispose();
			}
			this.selectedGame = value;
			this.OnPropertyChanged( nameof( IsGameSelected ) );
			this.OnPropertyChanged( nameof( SelectedGame ) );
		}
	}

	public bool IsGameSelected => ListBoxGames?.SelectedIndex > -1;

	private bool buttonsEnabled;
	public bool ButtonsEnabled {
		get { return buttonsEnabled; }
		set {
			buttonsEnabled = value;
			OnPropertyChanged( nameof( ButtonsEnabled ) );
		}
	}
	private bool removeButtonEnabled;
	public bool RemoveButtonEnabled {
		get { return removeButtonEnabled && buttonsEnabled; }
		set {
			removeButtonEnabled = value;
			OnPropertyChanged( nameof( RemoveButtonEnabled ) );
		}
	}

	public ObservableCollection<FeedGameViewModel> Packages { get; }

	public bool NoGamesInstalled => GameManager.Get().GameCount == 0;

	public GameManagement() {
		Packages = new ObservableCollection<FeedGameViewModel>();
		ButtonsEnabled = true;
		if(!Avalonia.Controls.Design.IsDesignMode) {
			Feeds = new ObservableCollection<NamedUrl>( Octgn.Core.GameFeedManager.Get().GetFeeds() );
			GameFeedManager.Get().OnUpdateFeedList += OnOnUpdateFeedList;
			GameManager.Get().GameListChanged += OnGameListChanged;
		} else Feeds = new ObservableCollection<NamedUrl>();
		InitializeComponent();
		ListBoxGames.SelectionChanged += delegate {
			OnPropertyChanged( nameof( SelectedGame ) );
			OnPropertyChanged( nameof( IsGameSelected ) );
		};
		this.PropertyChanged += OnPropertyChanged;
		this.Loaded += OnLoaded;
	}

	private void OnLoaded( object sender, RoutedEventArgs routedEventArgs ) {
		this.Loaded -= OnLoaded;
		Task.Run(async () => await UpdatePackageList() );
		Selected = Feeds.FirstOrDefault( x => x.Url == null );
	}

	internal async Task UpdatePackageList() {
		Dispatcher.UIThread.Invoke( new Action( () => { this.ButtonsEnabled = false; } ) );
		try {
			IEnumerable<GameInfo> packs;
			if(Selected.Url == null) {
				// This means read all game feeds
				packs = (await Task.WhenAll(
						Feeds
							.Where(x => x != Selected)
							.AsParallel()
							.Select(feed => GameFeedManager.Get().GetPackagesAsync(feed))
					))
					.SelectMany(p => p);
				
			} else
			{
				packs = await GameFeedManager.Get().GetPackagesAsync(Selected);
			}
			List<FeedGameViewModel> games = packs//.Where( x => x.IsAbsoluteLatestVersion )
				.OrderBy( x => x.Package.Title )
				.GroupBy( x => x.Package.Identity.Id )
				.Select( x => x.OrderByDescending( y => y.Package.Identity.Version ).First() )
				.Select( x => new FeedGameViewModel( x) )
				.ToList();
			Dispatcher.UIThread.Invoke( new Action( () => {
				foreach(FeedGameViewModel package in Packages.ToList()) {
					Packages.Remove( package );
					package.Dispose();
				}
				foreach(FeedGameViewModel package in games.OrderBy( x => x.Package.Title )) {
					Packages.Add( package );
				}
			} ) );
			if(Selected != null) {
				SelectedGame = Packages.FirstOrDefault();
				OnPropertyChanged( nameof( SelectedGame ) );
				OnPropertyChanged( nameof( IsGameSelected ) );
			}

		} catch(WebException e) {
			Dispatcher.UIThread.Invoke( new Action( () => Packages.Clear() ) );
			if((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized) {
				var box = MessageBoxManager
					.GetMessageBoxStandard("Authentication Error","This feed requires authentication(or your credentials are incorrect). Please delete this feed and re-add it.",icon:Icon.Error);

				await box.ShowAsync();
			} else {
				var box = MessageBoxManager
					.GetMessageBoxStandard("Feed Error","There was an error fetching this feed. Please try again or delete and re add it." ,icon:Icon.Error);
				await box.ShowAsync();
				
				var url = "unknown";
				if(Selected != null)
					url = Selected.Url;
				Log.Warn( url + " is an invalid feed. StatusCode=" + (e.Response as HttpWebResponse).StatusCode, e );
			}
		} catch(Exception e) {
			Dispatcher.UIThread.Invoke( new Action( () => Packages.Clear() ) );
			var box = MessageBoxManager
				.GetMessageBoxStandard("Feed Error","There was an error fetching this feed. Please try again or delete and re add it.", icon:Icon.Error);
			await box.ShowAsync();
			
			var url = "unknown";
			if(Selected != null)
				url = Selected.Url;
			Log.Warn( "GameManagement fetch url error " + url, e );
		}
		Dispatcher.UIThread.Invoke( new Action( () => { this.ButtonsEnabled = true; } ) );
	}

	#region Events

	private void OnPropertyChanged( object sender, PropertyChangedEventArgs propertyChangedEventArgs ) {
		if(propertyChangedEventArgs.PropertyName == nameof( Packages )) {
			new Task(async () => {
				try {
					await this.UpdatePackageList();
				} catch(Exception e) {
					Log.Warn( "", e );
				}
			} ).Start();
		}
	}

	private void OnGameListChanged( object sender, EventArgs eventArgs ) {
		OnPropertyChanged( nameof( Selected ) );
		OnPropertyChanged( nameof( Packages ) );
		OnPropertyChanged( nameof( NoGamesInstalled ) );
		OnPropertyChanged( nameof( IsGameSelected ) );
	}

	private void OnOnUpdateFeedList( object sender, EventArgs eventArgs ) {
		Dispatcher.UIThread.Invoke( new Action( () => {
			var realList = GameFeedManager.Get().GetFeeds().ToList();
			foreach(var f in Feeds.ToArray()) {
				if(realList.All( x => x.Name != f.Name )) Feeds.Remove( f );
			}
			foreach(var f in realList) {
				if(this.Feeds.All( x => x.Name != f.Name ))
					Feeds.Add( f );
			}
		} ) );
	}

	private async void ButtonAddClick( object sender, RoutedEventArgs e ) {
		ButtonsEnabled = false;
		var dialog = new AddFeed();
		await DialogHost.Show(dialog,delegate(object _, DialogOpenedEventArgs args)
		{
			dialog.DialogSession = args.Session;
		});
		ButtonsEnabled = true;
	}

	private void ButtonRemoveClick( object sender, RoutedEventArgs e ) {
		if(Selected == null) 
			return;
		GameFeedManager.Get().RemoveFeed( Selected.Name );
	}

	private async void ButtonAddo8gClick( object sender, RoutedEventArgs e ) {
		var topLevel = TopLevel.GetTopLevel(this);
		var param = new FilePickerOpenOptions
		{
			Title = "Open Text File",
			AllowMultiple = false,
			FileTypeFilter = [new ("Octgn Game File (*.o8g)") { Patterns = ["*.o8g"]}]
		};
		var files = await topLevel.StorageProvider.OpenFilePickerAsync(param);

		if (files.Count >= 1)
		{
			var file = files.First();
			try {
				GameFeedManager.Get().AddToLocalFeed( file.Path.ToString() );
				OnPropertyChanged( nameof(Packages) );
			} catch(UserMessageException ex) {
				Log.Warn( "Could not add " + file.Name + " to local feed.", ex );
				var box = MessageBoxManager
					.GetMessageBoxStandard("Error",ex.Message,icon:Icon.Error);
				await box.ShowAsync();

			} catch(Exception ex) {
				Log.Warn( "Could not add " + file.Name + " to local feed.", ex );
				var box = MessageBoxManager
					.GetMessageBoxStandard("Error","Could not add file " + file.Name
					   + ". Please make sure it isn't in use and that you have access to it.",
						icon:Icon.Error);
				await box.ShowAsync();
			}
		}
		
		// var of = new System.Windows.Forms.OpenFileDialog();
		// of.Filter = "Octgn Game File (*.o8g) |*.o8g";
		// var result = of.ShowDialog();
		// if(result == DialogResult.OK) {
		// 	
		// }
	}

	private bool installo8cprocessing = false;
	private async void ButtonAddo8cClick( object sender, RoutedEventArgs e ) {
		if(installo8cprocessing) return;
		installo8cprocessing = true;
		var topLevel = TopLevel.GetTopLevel(this);
		var param = new FilePickerOpenOptions
		{
			Title = "Open Text File",
			AllowMultiple = true,
			FileTypeFilter = [new ("Octgn Card Package (*.o8c;*.zip) ") { Patterns = ["*.o8c","*.zip"]}]
		};
		var files = await topLevel.StorageProvider.OpenFilePickerAsync(param);

		
		if (files.Count >= 1)
		{
			var filesToImport = (from f in files
				select new ImportFile { File = f, Status = ImportFileStatus.Unprocessed }).ToList();
			await this.ProcessTask(async () => {

					foreach(var f in filesToImport) {
						try {
							// if(!File.Exists( f.Filename )) {
							// 	f.Status = ImportFileStatus.FileNotFound;
							// 	f.Message = "File not found.";
							// 	continue;
							// }
							await GameManager.Get().Installo8c( f.File );
							f.Status = ImportFileStatus.Imported;
							f.Message = "Installed Successfully";
						} catch(UserMessageException ex) {
							var message = ex.Message;
							Log.Warn( message, ex );
							f.Message = message;
							f.Status = ImportFileStatus.Error;
						} catch(Exception ex) {
							var message = "Could not install o8c.";
							Log.Warn( message, ex );
							f.Message = message;
							f.Status = ImportFileStatus.Error;
						}
					}
				}, async () => {
					this.installo8cprocessing = false;

					var message = "The following image packs were installed:\n\n{0}"
						.With( filesToImport.Where( f => f.Status == ImportFileStatus.Imported ).Aggregate( "",
							( current, file ) =>
								current +
								"· {0}\n".With( file.SafeFileName ) ) );
					if(filesToImport.Any( f => f.Status != ImportFileStatus.Imported )) {
						message += "\nThe following image packs could not be installed:\n\n{0}"
							.With( filesToImport.Where( f => f.Status != ImportFileStatus.Imported )
								.Aggregate( "", ( current, file ) => current +
								                                     "· {0}: {1}\n".With( file.SafeFileName, file.Message ) ) );
					}

					var box = MessageBoxManager
						.GetMessageBoxStandard("Install Image Packs",message
							,icon:Icon.Info);
					await box.ShowAsync();

				},
				"Installing image pack.",
				"Please wait while your image pack is installed. You can switch tabs if you like." );
		}
		else {
			installo8cprocessing = false;
		}
	}

	private async Task ProcessTask( Func<Task> action, Func<Task> completeAction, string title, string message ) {
		ButtonsEnabled = false;
		// var dialog = new WaitingDialog();
		// dialog.OnClose += async ( o, result ) => {
		// 	ButtonsEnabled = true;
		// 	await completeAction();
		// 	dialog.Dispose();
		// };
		// await dialog.Show( DialogPlaceHolder, action, title, message );
		// return dialog;

		try
		{
			await Progress.RunAsync(VisualRoot as Window, async () =>
			{
				await action();
				return null;
			},()=>
			{
				ButtonsEnabled = true;
				return Task.FromResult<object>(null);
			},title,message);
		}
		catch (Exception ex)
		{
			Log.Error("Error",ex);
		}
		finally
		{
			await completeAction();
			ButtonsEnabled = true;
		}
		
		
	}

	private bool installuninstallprocessing = false;
	private async void ButtonInstallUninstallClick( object sender, RoutedEventArgs e ) {
		if(installuninstallprocessing) return;
		installuninstallprocessing = true;
		try {
			var button = e.Source as Button;
			if(button == null || button.DataContext == null) return;
			var model = button.DataContext as FeedGameViewModel;
			if(model == null) return;
			if(model.Installed) {
				var game = GameManager.Get().GetById( model.Id );
				if(game != null) {
					await this.ProcessTask(async () => {
							try {
								GameManager.Get().UninstallGame( game );
							} catch(IOException) {
								var box = MessageBoxManager
									.GetMessageBoxStandard("Error","Could not uninstall the game. Please try exiting all running instances of OCTGN and try again.\nYou can also try switching feeds, and then switching back and try again.",
										icon:Icon.Error);
								await box.ShowAsync();
							} catch(UnauthorizedAccessException) {
								var box = MessageBoxManager
									.GetMessageBoxStandard("Error","Could not uninstall the game. Please try exiting all running instances of OCTGN and try again.\nYou can also try switching feeds, and then switching back and try again.",
										icon:Icon.Error);
								await box.ShowAsync();
							} catch(Exception ex) {
								Log.Error( "Could not fully uninstall game " + model.Package.Title, ex );
							}
						},
						() =>
						{
							this.installuninstallprocessing = false;
							return Task.CompletedTask;
						},
						"Uninstalling Game",
						"Please wait while your game is uninstalled. You can switch tabs if you like." );
				}
			} else {
				await this.ProcessTask(async () => {
					try {
						await GameManager.Get().InstallGame( model.Game);
					} catch(UnauthorizedAccessException) {
						var box = MessageBoxManager
							.GetMessageBoxStandard("Error","Could not install the game. Please try exiting all running instances of OCTGN and try again.\nYou can also try switching feeds, and then switching back and try again.",
								icon:Icon.Error);
						await box.ShowAsync();
					} catch(Exception ex) {
						Log.Error( "Could not install game " + model.Package.Title, ex );
						var box = MessageBoxManager
							.GetMessageBoxStandard("Error","There was a problem installing " + model.Package.Title
								+ ". \n\nThis is likely an issue with the game plugin, and not OCTGN."
								+ "\n\nDo you want to proceed to the information webpage associated with this game?",
								ButtonEnum.YesNo,Icon.Warning);
						var res=await box.ShowAsync();

						if(res == ButtonResult.Yes) {
							try {
								await App.LaunchUrl( model.Package.ProjectUrl.ToString() );

							} catch(Exception exx) {
								Log.Warn(
									"Could not launch " + model.Package.ProjectUrl.ToString() + " In default browser",
									exx );
								box = MessageBoxManager
									.GetMessageBoxStandard("Error","We could not open your browser. Please set a default browser and try again",
										icon:Icon.Error);
								await box.ShowAsync();
							}
						}
					}

				},
				() =>
				{
					this.installuninstallprocessing = false;
					return Task.CompletedTask;
				},
				"Installing Game",
				"Please wait while your game is installed. You can switch tabs if you like." );
			}

		} catch(Exception ex) {
			Log.Error( "Mega Error", ex );
			var box = MessageBoxManager
				.GetMessageBoxStandard("Error","There was an error, please try again later or get in contact with us at http://www.octgn.net",
					icon:Icon.Error);
			await box.ShowAsync();
		}
	}

	private void ButtonRefreshClick( object sender, RoutedEventArgs e ) {
		Task.Run( UpdatePackageList );
	}

	private async void UrlMouseButtonUp( object sender, PointerReleasedEventArgs whatever ) {
		if(!(sender is TextBlock)) return;
		try {
	        if ((sender as TextBlock).DataContext is Uri url) {
		        await App.LaunchUrl(url.OriginalString);
	        }
	    } catch {

		}
	}

	// private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
	// {
	// 	var failedImage = (sender as Image);
	// 	failedImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/OCTGN;component/Resources/noimage.png"));
	// 	failedImage.Effect = new System.Windows.Media.Effects.DropShadowEffect() { ShadowDepth=1f, Color=System.Windows.Media.Color.FromRgb(255,255,255) };
	// 	e.Handled = true;
	// }

	#endregion Events

}