using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Octgn.Core.DataExtensionMethods;
using Octgn.Play;

namespace Octgn.JodsEngine.Play.Gui;

public partial class ChatControl : UserControl
{
    public ChatControl()
    {
        InitializeComponent();
    }
}

internal class CardModelEventArgs : RoutedEventArgs
{
    public readonly ChatCard CardModel;

    public CardModelEventArgs(ChatCard model, RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
        CardModel = model;
    }
}

internal class ChatCard
{
    public DataNew.Entities.Card Card { get; private set; }
    public Card GameCard { get; private set; }

    private Action<DataNew.Entities.Card, Card> _updateAction;

    public ChatCard(CardIdentity ci)
    {
        this.Card = ci.Model;
    }

    public ChatCard(DataNew.Entities.Card model)
    {
        this.Card = model;
    }

    public void SetGameCard(Card card)
    {
        GameCard = card;
        GameCard.PropertyChanged += (x, y) =>
        {
            if (_updateAction != null)
            {
                _updateAction(Card, GameCard);
            }
        };
    }

    public void SetCardModel(DataNew.Entities.Card model)
    {
        Debug.Assert(this.Card == null, "Cannot set the CardModel of a CardRun if it is already defined");
        this.Card = model;
        if (_updateAction != null)
            _updateAction.Invoke(model, GameCard);
    }

    public void UpdateCardText(Action<DataNew.Entities.Card, Card> action)
    {
        _updateAction = action;
    }

    public override string ToString()
    {
        if (this.Card == null)
            return "[?}";
        return this.Card.GetName();
    }
}