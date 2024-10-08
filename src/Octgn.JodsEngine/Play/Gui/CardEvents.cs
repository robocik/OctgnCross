using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Octgn.Play.Gui
{
    public class CardEventArgs : RoutedEventArgs
    {
        public readonly Card Card;
        public readonly DataNew.Entities.ICard CardModel;

        public CardEventArgs(RoutedEvent routedEvent, object src)
            : base(routedEvent, src)
        {
        }

        public CardEventArgs(Card card, RoutedEvent routedEvent, object src)
            : base(routedEvent, src)
        {
            Card = card;
        }

        public CardEventArgs(DataNew.Entities.ICard model, RoutedEvent routedEvent, object src)
            : base(routedEvent, src)
        {
            CardModel = model;
        }

        public Vector MouseOffset { get; set; }
    }

    public class CardsEventArgs : RoutedEventArgs
    {
        public readonly Card[] Cards;
        public readonly Size[] CardSizes;
        public readonly Card ClickedCard;

        public CardsEventArgs(Card card, IEnumerable<Card> cards, RoutedEvent routedEvent, object src)
            : base(routedEvent, src)
        {
            ClickedCard = card;
            Cards = cards.ToArray();
			CardSizes = new Size[Cards.Length];
        }

        public IInputElement Handler { get; set; }
        public Vector MouseOffset { get; set; }
        public bool? FaceUp { get; set; }
        public bool CanDrop { get; set; }
        //public Size CardSize { get; set; }
        // internal List<CardDragAdorner> Adorners { get; set; }
    }

    public delegate void CardEventHandler(object sender, CardEventArgs e);

    public delegate void CardsEventHandler(object sender, CardsEventArgs e);
}