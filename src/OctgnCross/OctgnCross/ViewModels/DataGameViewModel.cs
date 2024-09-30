using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.DataManagers;
using Octgn.Extentions;
using Octgn.UI;

namespace Octgn.ViewModels;

public class DataGameViewModel:ViewModelBase
{
    private bool isSelected;
    private Bitmap cardBack;


    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Version Version { get; private set; }
    public Uri CardBackUri { get; private set; }
//public string FullPath { get; private set; }
    public Bitmap CardBack
    {
        get
        {
            if (cardBack == null)
            {
                cardBack = Task.Run(()=>CardBackUri.BitmapFromUri()).Result;//TODO: Async!
            }
            return cardBack;
        }
    }
    public bool IsSelected
    {
        get{return this.isSelected;}
        private set
        {
            this.isSelected = value;
            this.OnPropertyChanged();
        }
    }

    public DataGameViewModel(DataNew.Entities.Game game)
    {
        Id = game.Id;
        Name = game.Name;
        Version = game.Version;
        CardBackUri = new Uri(game.DefaultSize().Back);
        //FullPath = game.FullPath;
        IsSelected = false;
    }

    public DataNew.Entities.Game GetGame()
    {
        return GameManager.Get().GetById(Id);
    }

    public override string ToString()
    {
        return Name;
    }
}