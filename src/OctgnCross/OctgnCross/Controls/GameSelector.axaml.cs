using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew;
using Octgn.DataNew.Entities;
using Octgn.Extentions;
using Octgn.UI;

namespace OctgnCross.Controls;

public class GameViewModel
{
    public GameViewModel(Game game)
    {
        var cardSize = game.DefaultSize();
        BackImage = cardSize.Back.BitmapFromFile();
        Game = game;
        Id = game.Id;
    }

    public Bitmap BackImage { get; private set; }
    
    public Guid Id { get; }
    public Game Game { get; }
}
public partial class GameSelector : UserControl
{
    public event EventHandler<Game> GameChanged;

    private GameViewModel[] _games;

    public GameSelector() {
        InitializeComponent();

        if (Design.IsDesignMode) return;

        Loaded += GameSelector_Loaded;
    }

    private void GameSelector_Loaded(object sender, RoutedEventArgs e) {
        var context = DbContext.Get();

        _games = context.Games.Select(x=>new GameViewModel(x)).ToArray();

        slides.ItemsSource = _games;
    }

    public Game Game {
        get {
            Dispatcher.UIThread.VerifyAccess();

            if (_games.Length == 0) return null;

            var item = (GameViewModel) slides.SelectedItem;
            return item?.Game;
        }
    }


    public void Select(Guid? gameId) {

        if (_games == null) return;

        if (_games.Length == 0) return;

        if (gameId == null)
        {
            slides.SelectedIndex = -1;
        } else {
            var gameIndex = _games.FindIndex(g => g.Id == gameId);
            
            if (gameIndex == -1) gameIndex = 0;
            
            
            slides.SelectedIndex = gameIndex;
        }

    }

    private void PreviousClick(object sender, RoutedEventArgs e)
    {
        slides.Previous();
    }

    private void NextClick(object sender, RoutedEventArgs e)
    {
        slides.Next();
    }

    private void Slides_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GameChanged?.Invoke(this, Game);
    }
}