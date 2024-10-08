﻿using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Octgn.Core;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.DataManagers;
using Octgn.DataNew.Entities;
using Octgn.Library.Exceptions;
using Octgn.Site.Api;

namespace Octgn.Extentions
{
    using System;
    using System.IO;

    public static class DeckExtensionMethods
    {
        public static async Task<string> Share(this IDeck deck, IStorageFile file)
        {
            try
            {
                var tempFile = Path.GetTempFileName();
                var game = GameManager.Get().GetById(deck.GameId);
                await deck.Save(game,file);

                var client = new ApiClient();
                if (!App.LobbyClient.IsConnected) throw new UserMessageException("You must be logged in to share a deck.");
                // if (string.IsNullOrWhiteSpace(name)) throw new UserMessageException("The deck name can't be blank.");
                // if (name.Length > 32) throw new UserMessageException("The deck name is too long.");
                using var fileStream = await file.OpenReadAsync();
                var result = client.ShareDeck(Prefs.Username, App.SessionKey, file.Name, fileStream);
                if (result.Error)
                {
                    throw new UserMessageException(result.Message);
                }
                return result.DeckPath;
            }
            catch (Exception)
            {
                throw new UserMessageException("There was an error sharing your deck. 0xFFFF");
            }
        }
    }
}