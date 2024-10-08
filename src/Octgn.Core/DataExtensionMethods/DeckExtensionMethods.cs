﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Avalonia.Platform.Storage;
using Octgn.DataNew;
using Octgn.DataNew.Entities;
using Octgn.Library.Exceptions;

using log4net;
using Octgn.Library.Localization;

namespace Octgn.Core.DataExtensionMethods
{
    public static class DeckExtensionMethods
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static async Task Save(this IDeck deck, Game game, IStorageFile file)
        {
            try
            {
                using (var fs = await file.OpenWriteAsync())
                {
                    var settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.NewLineHandling = NewLineHandling.Entitize;

                    var writer = XmlWriter.Create(fs, settings);

                    await writer.WriteStartDocumentAsync(true);
                    {
                        writer.WriteStartElement("deck");
                        writer.WriteAttributeString("game", game.Id.ToString());

                        // Write Sections
                        foreach (var section in deck.Sections) {
                            writer.WriteStartElement("section");
                            writer.WriteAttributeString("name", section.Name);
                            writer.WriteAttributeString("shared", section.Shared.ToString());
                            foreach (var c in section.Cards) {
                                writer.WriteStartElement("card");
                                writer.WriteAttributeString("qty", c.Quantity.ToString());
                                writer.WriteAttributeString("id", c.Id.ToString());
                                await writer.WriteStringAsync(c.Name);
                                await writer.WriteEndElementAsync();
                            }
                            await writer.WriteEndElementAsync();
                        }
                        { // Write Notes
                            writer.WriteStartElement("notes");
                            await writer.WriteCDataAsync(deck.Notes);
                            await writer.WriteEndElementAsync();
                        }
                        { // Write Sleeve
                            if (deck.Sleeve != null) {
                                writer.WriteStartElement("sleeve");
                                var sleeveString = Sleeve.ToString(deck.Sleeve);
                                writer.WriteValue(sleeveString);
                                await writer.WriteEndElementAsync();
                            }
                        }

                        await writer.WriteEndElementAsync();
                    }
                    await writer.WriteEndDocumentAsync();
                    await writer.FlushAsync();
                    writer.Close();
                }
                // assume players will want to play with their new deck
                Prefs.LastHostedGameType = game.Id;
            }
            catch (PathTooLongException)
            {
                throw new UserMessageException(L.D.Exception__CanNotSaveDeckPathTooLong_Format, file.Name);
            }
            catch (IOException e)
            {
                Log.Error(String.Format("Problem saving deck to path {0}", file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotSaveDeckIOError_Format, file.Name, e.Message);
            }
            catch (Exception e)
            {
                Log.Error(String.Format("Problem saving deck to path {0}", file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotSaveDeckUnspecified_Format, file.Name);
            }
        }

        public static void ExportAsText(this IDeck deck, Game game, string path)
        {
            try
            {
                using (var fs = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var writer = new StreamWriter(fs))
                {
                    foreach (var sec in deck.Sections)
                    {
                        writer.WriteLine(sec.Name);
                        foreach (var card in sec.Cards)
                        {
                            writer.WriteLine(card.Quantity + "x " + card.Name);
                        }
                        writer.WriteLine("");
                    }

                    writer.WriteLine(game.Name);
                    writer.WriteLine(deck.Notes);
                }
            }
            catch (PathTooLongException)
            {
                throw new UserMessageException(L.D.Exception__CanNotSaveDeckPathTooLong_Format, path);
            }
            catch (IOException e)
            {
                Log.Warn(String.Format("Problem exporting deck to path {0}", path), e);
                throw new UserMessageException(L.D.Exception__CanNotSaveDeckIOError_Format, path, e.Message);
            }
            catch (Exception e)
            {
                Log.Warn(String.Format("Problem saving deck to path {0}", path), e);
                throw new UserMessageException(L.D.Exception__CanNotSaveDeckUnspecified_Format, path);
            }
        }

        public static async Task<IDeck> Load(this IDeck deck, IStorageFile file, bool cloneCards = true)
        {
            var ret = new Deck();
            try
            {
                Game game = null;
                using (var fs = await file.OpenReadAsync())
                {
                    var doc = XDocument.Load(fs);
                    var gameId = Guid.Parse(doc.Descendants("deck").First().Attribute("game").Value);
                    game = Octgn.Core.DataManagers.GameManager.Get().GetById(gameId);
                    if (game == null)
                    {
                        throw new UserMessageException(L.D.Exception__CanNotLoadDeckGameNotInstalled_Format, file.Name);
                    }
                }
                return await deck.Load(game, file, cloneCards);
            }
            catch (UserMessageException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error(String.Format("Problem loading deck from path {0}", file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotLoadDeckUnspecified_Format, file.Name);
            }
        }
        public static async Task<IDeck> Load(this IDeck deck, Game game, IStorageFile file, bool cloneCards = true)
        {
            var ret = new Deck();
            ret.Sections = new List<ISection>();
            try
            {
                var cards = game.Sets().SelectMany(x => x.Cards).ToArray();
                using (var fs = await file.OpenReadAsync())
                {
                    var doc = XDocument.Load(fs);
                    var gameId = Guid.Parse(doc.Descendants("deck").First().Attribute("game").Value);
                    var shared = doc.Descendants("deck").First().Attr<bool>("shared");
                    foreach (var sectionelem in doc.Descendants("section"))
                    {
                        var section = new Section();
                        section.Cards = new List<IMultiCard>();
                        section.Name = sectionelem.Attribute("name").Value;
                        section.Shared = sectionelem.Attr<bool>("shared");
                        // On old style decks, if it's shared, then all subsequent sections are shared
                        if (shared)
                            section.Shared = true;
                        foreach (var cardelem in sectionelem.Descendants("card"))
                        {
                            var cardId = Guid.Parse(cardelem.Attribute("id").Value);
                            var cardq = Int32.Parse(cardelem.Attribute("qty").Value);
                            var card = cards.FirstOrDefault(x => x.Id == cardId);
                            if (card == null)
                            {
                                var cardN = cardelem.Value;
                                card = cards.FirstOrDefault(x => x.Name.Equals(cardN, StringComparison.CurrentCultureIgnoreCase));
                                if (card == null)
                                    throw new UserMessageException(L.D.Exception__CanNotLoadDeckCardNotInstalled_Format, file.Name, cardId, cardN);
                            }
                            (section.Cards as IList<IMultiCard>).Add(card.ToMultiCard(cardq, cloneCards));
                        }
                        if (section.Cards.Any())
                            (ret.Sections as List<ISection>).Add(section);
                    }
                    // Add deck notes
                    var notesElem = doc.Descendants("notes").FirstOrDefault();
                    if (notesElem != null)
                    {
                        var cd = (notesElem.FirstNode as XCData);
                        if (cd != null)
                        {
                            ret.Notes = cd.Value.Clone() as string;
                        }
                    }
                    if (ret.Notes == null) ret.Notes = "";

                    // Add all missing sections so that the game doesn't get pissed off
                    {
                        var combinedList =
                            game.DeckSections.Select(x => x.Value).Concat(game.SharedDeckSections.Select(y => y.Value));
                        foreach (var section in combinedList)
                        {
                            if (ret.Sections.Any(x => x.Name.Equals(section.Name, StringComparison.InvariantCultureIgnoreCase) && x.Shared == section.Shared) == false)
                            {
                                // Section not defined in the deck, so add an empty version of it.
                                (ret.Sections as List<ISection>).Add(
                                    new Section
                                    {
                                        Name = section.Name.Clone() as string,
                                        Cards = new List<IMultiCard>(),
                                        Shared = section.Shared
                                    });
                            }
                        }
                    }

                    // Add card sleeve
                    {
                        var sleeveElem = doc.Descendants("sleeve").FirstOrDefault();
                        if(sleeveElem != null) {
                            var sleeveString = sleeveElem.Value;

                            if (!string.IsNullOrWhiteSpace(sleeveString)) {
                                try {
                                    ret.Sleeve = Sleeve.FromString(sleeveString);
                                } catch (SleeveException ex) {
                                    throw new UserMessageException(ex.Message, ex);
                                }
                            }
                        }
                    }

                    ret.GameId = gameId;
                    ret.IsShared = shared;
                }
                // This is an old style shared deck file, we need to convert it now, for posterity sake.
                if (ret.IsShared)
                {
                    ret.IsShared = false;
                    await ret.Save(game, file);
                }
                deck = ret;
                return deck;
            }
            catch (UserMessageException)
            {
                throw;
            }
            catch (FormatException e)
            {
                Log.Error(String.Format("Problem loading deck from path {0}", file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotLoadDeckCorrupt_Format, file.Name);
            }
            catch (NullReferenceException e)
            {
                Log.Error(String.Format("Problem loading deck from path {0}", file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotLoadDeckCorrupt_Format, file.Name);
            }
            catch (XmlException e)
            {
                Log.Error(String.Format("Problem loading deck from path {0}", file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotLoadDeckCorrupt_Format, file.Name);
            }
            catch (FileNotFoundException)
            {
                throw new UserMessageException(L.D.Exception__CanNotLoadDeckFileNotFound_Format,  file.Name);
            }
            catch (IOException e)
            {
                Log.Error(String.Format("Problem loading deck from path {0}",  file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotLoadDeckIOError_Format,  file.Name, e.Message);
            }
            catch (Exception e)
            {
                Log.Error(String.Format("Problem loading deck from path {0}",  file.Name), e);
                throw new UserMessageException(L.D.Exception__CanNotLoadDeckUnspecified_Format,  file.Name);
            }
        }
        public static int CardCount(this IDeck deck)
        {
            var qs = deck.Sections.SelectMany(x => x.Cards).Select(x => x.Quantity);
            return qs.Sum(x => x);
        }
        public static ObservableSection AsObservable(this ISection section)
        {
            if (section == null) return null;
            var ret = new ObservableSection();
            ret.Name = section.Name.Clone() as string;
            ret.Cards = section.Cards.ToArray();
            ret.Shared = section.Shared;
            return ret;
        }
        public static ObservableMultiCard AsObservable(this IMultiCard card)
        {
            if (card == null) return null;
            var ret = new ObservableMultiCard(card);
            return ret;
        }
        public static ObservableDeck AsObservable(this IDeck deck)
        {
            if (deck == null) return null;
            var ret = new ObservableDeck();
            ret.GameId = deck.GameId;
            ret.IsShared = deck.IsShared;
            ret.Sleeve = (ISleeve)deck.Sleeve?.Clone();
            if (deck.Sections == null) ret.Sections = new List<ObservableSection>();
            else
            {
                ret.Sections = deck.Sections
                    .Where(x => x != null)
                    .Select(
                        x =>
                        {
                            var sret = new ObservableSection();
                            sret.Name = (x.Name ?? "").Clone() as string;
                            if (x.Cards == null)
                                sret.Cards = new List<ObservableMultiCard>();
                            else
                                sret.Cards = x.Cards.Where(y => y != null).Select(y => y.AsObservable()).ToArray();
                            sret.Shared = x.Shared;
                            return sret;
                        });
            }
            ret.Notes = (deck.Notes ?? "").Clone() as string;
            return ret;
        }
        public static IEnumerable<IMultiCard> AddCard(this IEnumerable<IMultiCard> cards, IMultiCard card)
        {
            if (cards is ObservableCollection<ObservableMultiCard>)
            {
                if (card is ObservableMultiCard)
                    (cards as ObservableCollection<ObservableMultiCard>).Add(card as ObservableMultiCard);
                else
                    (cards as ObservableCollection<ObservableMultiCard>).Add(card.AsObservable());
            }
            else if (cards is ObservableCollection<IMultiCard>) (cards as ObservableCollection<IMultiCard>).Add(card);
            else if (cards is IList<IMultiCard>) (cards as IList<IMultiCard>).Add(card);
            else if (cards is ICollection<IMultiCard>) (cards as ICollection<IMultiCard>).Add(card);
            else
            {
                var g = cards.ToList();
                g.Add(card);
                cards = g;
            }
            return cards;
        }
        public static IEnumerable<IMultiCard> RemoveCard(this IEnumerable<IMultiCard> cards, IMultiCard card)
        {
            if (cards is ObservableCollection<ObservableMultiCard>)
            {
                if (card is ObservableMultiCard) (cards as ObservableCollection<ObservableMultiCard>).Remove(card as ObservableMultiCard);
                else
                {
                    var tcard = (cards as ObservableCollection<ObservableMultiCard>).FirstOrDefault(
                        x => x.Id == card.Id);
                    if (tcard == null) return cards;
                    (cards as ObservableCollection<ObservableMultiCard>).Remove(tcard);
                }
            }
            else if (cards is ObservableCollection<IMultiCard>)
                (cards as ObservableCollection<IMultiCard>).Remove(card);
            else if (cards is IList<IMultiCard>)
                (cards as IList<IMultiCard>).Remove(card);
            else if (cards is ICollection<IMultiCard>)
                (cards as ICollection<IMultiCard>).Remove(card);
            else
            {
                var g = cards.ToList();
                g.Remove(card);
                cards = g;
            }
            return cards;
        }
        public static IEnumerable<IMultiCard> Move(this IEnumerable<IMultiCard> cards, IMultiCard card, int newIndex)
        {
            if (cards is ObservableCollection<ObservableMultiCard>)
            {
                if (card is ObservableMultiCard) (cards as ObservableCollection<ObservableMultiCard>).Move((cards as ObservableCollection<ObservableMultiCard>).IndexOf(card as ObservableMultiCard), newIndex);
                else
                {
                    var tcard = (cards as ObservableCollection<ObservableMultiCard>).FirstOrDefault(
                        x => x.Id == card.Id);
                    if (tcard == null) return cards;
                    (cards as ObservableCollection<ObservableMultiCard>).Move(tcard, newIndex);
                }
            }
            else if (cards is ObservableCollection<IMultiCard>)
                (cards as ObservableCollection<IMultiCard>).Move(card, newIndex);
            else if (cards is IList<IMultiCard>)
                (cards as IList<IMultiCard>).Move(card, newIndex);
            else if (cards is ICollection<IMultiCard>)
                (cards as ICollection<IMultiCard>).Move(card, newIndex);
            else
            {
                var g = cards.ToList();
                g.Move(card, newIndex);
                cards = g;
            }
            return cards;
        }
    }
}
