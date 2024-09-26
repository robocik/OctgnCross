/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using Avalonia;
using Octgn.Core.Util;
using Octgn.Library;
using OctgnCross.Core;

namespace Octgn.Core
{
    public static class Prefs
    {
        public enum ZoomType : byte { OriginalOrProxy, OriginalAndProxy, ProxyOnKeypress };
        public enum SoundType : byte { DingDong, KnockKnock, None };
        public enum CardAnimType : byte { None, NormalAnimation, MinimalAnimation };

        public static IPreferencesStore Store { get; set; }
        public static bool InstallOnBoot
        {
            get
            {
                return Task.Run(() => Store.GetValue("InstallOnBoot", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("InstallOnBoot", value)).GetAwaiter().GetResult();
            }
        }

        public static bool CleanDatabase
        {
            get
            {
                return Task.Run(() => Store.GetValue("CleanDatabase", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("CleanDatabase", value)).GetAwaiter().GetResult();
            }
        }

        public static string Password
        {
            get
            {
                return Task.Run(() => SavedPasswordManager.GetPassword()).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => SavedPasswordManager.SavePassword(value)).GetAwaiter().GetResult();
            }
        }

        public static string SessionKey
        {
            get
            {
                return Task.Run(() => Store.GetValue("SessionKey", "")).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("SessionKey", value)).GetAwaiter().GetResult();
            }
        }

        public static string Username
        {
            get
            {
                return Task.Run(() => Store.GetValue("Username", "")).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("Username", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableWhisperSound
        {
            get
            {
                return Task.Run(() => Store.GetValue("EnableWhisperSound", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("EnableWhisperSound", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableNameSound
        {
            get
            {
                return Task.Run(() => Store.GetValue("EnableNameSound", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("EnableNameSound", value)).GetAwaiter().GetResult();
            }
        }

        public static ZoomType ZoomOption
        {
            get
            {
                return Task.Run(() => Store.GetValue("ZoomOption", ZoomType.OriginalOrProxy)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ZoomOption", value)).GetAwaiter().GetResult();
            }
        }

        public static SoundType SoundOption
        {
            get
            {
                return Task.Run(() => Store.GetValue("JoinSound", SoundType.DingDong)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("JoinSound", value)).GetAwaiter().GetResult();
            }
        }

        public static CardAnimType CardMoveNotification
        {
            get
            {
                return Task.Run(() => Store.GetValue("CardMoveNotification", CardAnimType.NormalAnimation)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("CardMoveNotification", value)).GetAwaiter().GetResult();
            }
        }

        public static int MaxChatHistory
        {
            get
            {
                return Task.Run(() => Store.GetValue("MaxChatHistory", 100)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("MaxChatHistory", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableChatImages
        {
            get
            {
                return Task.Run(() => Store.GetValue("EnableChatImages", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("EnableChatImages", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableChatGifs
        {
            get
            {
                return Task.Run(() => Store.GetValue("EnableChatGifs", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("EnableChatGifs", value)).GetAwaiter().GetResult();
            }
        }

        public static string Nickname
        {
            get
            {
                return Task.Run(() => Store.GetValue("Nickname", "null")).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("Nickname", value)).GetAwaiter().GetResult();
            }
        }

        public static string LastFolder
        {
            get
            {
                return Task.Run(() => Store.GetValue("lastFolder", "")).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("lastFolder", value)).GetAwaiter().GetResult();
            }
        }

        public static string LastRoomName
        {
            get
            {
                return Task.Run(() => Store.GetValue<string>("lastroomname", null)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("lastroomname", value)).GetAwaiter().GetResult();
            }
        }
            
         public static Guid LastHostedGameType
        {
            get
            {
                return Task.Run(() => {
                    var ret = Guid.Empty;
                    if (Guid.TryParse(Store.GetValue("lasthostedgametype", Guid.Empty.ToString()).Result, out ret))
                        return ret;
                    return Guid.Empty;
                }).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("lasthostedgametype", value.ToString())).GetAwaiter().GetResult();
            }
        }

        public static bool TwoSidedTable
        {
            get
            {
                return Task.Run(() => Store.GetValue("twosidedtable", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("twosidedtable", value)).GetAwaiter().GetResult();
            }
        }

        public static Point LoginLocation
        {
            get
            {
                return Task.Run(() => Store.GetValue("LoginLoc", new Point(100, 100))).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("LoginLoc", value)).GetAwaiter().GetResult();
            }
        }

        public static Point MainLocation
        {
            get
            {
                return Task.Run(() => Store.GetValue("MainLoc", new Point(100, 100))).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("MainLoc", value)).GetAwaiter().GetResult();
            }
        }

        public static Rect PreviewCardWindowLocation
        {
            get
            {
                return Task.Run(() => {
                    try
                    {
                        return Rect.Parse(Store.GetValue("PreviewCardLoc", "100, 100, 200, 300").Result);
                    }
                    catch
                    {
                        return new Rect(100, 100, 200, 300);
                    }
                }).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("PreviewCardLoc", value.ToString())).GetAwaiter().GetResult();
            }
        }

        public static Rect ChatWindowLocation
        {
            get
            {
                return Task.Run(() => {
                    try
                    {
                        return Rect.Parse(Store.GetValue("ChatWindowLoc", "100, 100, 200, 300").Result);
                    }
                    catch
                    {
                        return new Rect(100, 100, 200, 300);
                    }
                }).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ChatWindowLoc", value.ToString())).GetAwaiter().GetResult();
            }
        }

        public static int LoginTimeout
        {
            get
            {
                return Task.Run(() => Store.GetValue("LoginTimeout", 30000)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("LoginTimeout", value)).GetAwaiter().GetResult();
            }
        }

        public static bool UseLightChat
        {
            get
            {
                return Task.Run(() => Store.GetValue("LightChat", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("LightChat", value)).GetAwaiter().GetResult();
            }
        }

        public static bool UseHardwareRendering
        {
            get
            {
                return Task.Run(() => Store.GetValue("UseHardwareRendering", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("UseHardwareRendering", value)).GetAwaiter().GetResult();
            }
        }

        public static bool UseWindowTransparency
        {
            get
            {
                return Task.Run(() => Store.GetValue("UseWindowTransparency", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("UseWindowTransparency", value)).GetAwaiter().GetResult();
            }
        }

        public static int HistoryPageSize
        {
            get
            {
                return Task.Run(() => Store.GetValue("HistoryPageSize", 16)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("HistoryPageSize", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableGameSound
        {
            get
            {
                return Task.Run(() => Store.GetValue("ReallyEnableGameSound", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ReallyEnableGameSound", value)).GetAwaiter().GetResult();
            }
        }

        public static string WindowBorderDecorator
        {
            get
            {
                return Task.Run(() => {
                    var border = Store.GetValue<string>("WindowBorderDecorator", null).Result;
                    if (string.IsNullOrEmpty(border))
                    {
                        WindowBorderDecorator = "Octgn";
                        return "Octgn";
                    }
                    return border;
                }).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("WindowBorderDecorator", value)).GetAwaiter().GetResult();
            }
        }

        public static string ImportImagesLastPath
        {
            get
            {
                return Task.Run(() => {
                    var fpath = Store.GetValue("ImportImagesLastPath", "").Result;
                    if (string.IsNullOrWhiteSpace(fpath)) return fpath;
                    if (!Directory.Exists(fpath))
                    {
                        ImportImagesLastPath = "";
                        return "";
                    }
                    return fpath;
                }).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ImportImagesLastPath", value)).GetAwaiter().GetResult();
            }
        }

        public static string WindowSkin
        {
            get
            {
                return Task.Run(() => {
                    var fpath = Store.GetValue("WindowSkin", "").Result;
                    if (string.IsNullOrWhiteSpace(fpath)) return fpath;
                    if (!File.Exists(fpath))
                    {
                        WindowSkin = "";
                        return "";
                    }
                    return fpath;
                }).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("WindowSkin", value)).GetAwaiter().GetResult();
            }
        }

        public static bool TileWindowSkin
        {
            get
            {
                return Task.Run(() => Store.GetValue("TileWindowSkin", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("TileWindowSkin", value)).GetAwaiter().GetResult();
            }
        }

        public static string DefaultGameBack
        {
            get
            {
                return Task.Run(() => {
                    var fpath = Store.GetValue("DefaultGameBack", "").Result;
                    if (string.IsNullOrWhiteSpace(fpath)) return fpath;
                    if (!File.Exists(fpath))
                    {
                        DefaultGameBack = "";
                        return "";
                    }
                    return fpath;
                }).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("DefaultGameBack", value)).GetAwaiter().GetResult();
            }
        }

        public static bool HideUninstalledGamesInList
        {
            get
            {
                return Task.Run(() => Store.GetValue("HideUninstalledGamesInList", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("HideUninstalledGamesInList", value)).GetAwaiter().GetResult();
            }
        }

        public static bool IgnoreSSLCertificates
        {
            get
            {
                return Task.Run(() => Store.GetValue("IgnoreSSLCertificates", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("IgnoreSSLCertificates", value)).GetAwaiter().GetResult();
            }
        }


        public static T GetGameSetting<T>(Octgn.DataNew.Entities.Game game, string propName, T def)
        {
            var defSettings = new Hashtable();
            defSettings["name"] = game.Name;
            var settings = Store.GetValue("GameSettings_" + game.Id.ToString(), defSettings).Result;

            if (settings.ContainsKey(propName))
            {
                if (settings[propName] is T)
                    return (T)settings[propName];
            }
            SetGameSetting(game, propName, def);
            return def;
        }

        public static void SetGameSetting<T>(DataNew.Entities.Game game, string propName, T val)
        {
            var defSettings = new Hashtable();
            defSettings["name"] = game.Name;
            var settings = Store.GetValue("GameSettings_" + game.Id.ToString(), defSettings).Result;

            if (!settings.ContainsKey(propName))
                settings.Add(propName, val);
            else
                settings[propName] = val;

            Store.SetValue("GameSettings_" + game.Id.ToString(), settings);
        }

        public static bool UseWindowsForChat
        {
            get
            {
                return Task.Run(() => Store.GetValue("UseWindowsForChat", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("UseWindowsForChat", value)).GetAwaiter().GetResult();
            }
        }

        public static bool InstantSearch
        {
            get
            {
                return Task.Run(() => Store.GetValue("InstantSearch", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("InstantSearch", value)).GetAwaiter().GetResult();
            }
        }

        public static bool HideResultCount
        {
            get
            {
                return Task.Run(() => Store.GetValue("HideResultCount", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("HideResultCount", value)).GetAwaiter().GetResult();
            }
        }

        public static bool AcceptedCustomDataAgreement
        {
            get
            {
                return Task.Run(() => Store.GetValue("AcceptedCustomDataAgreement", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("AcceptedCustomDataAgreement", value)).GetAwaiter().GetResult();
            }
        }

        public static string CustomDataAgreementHash
        {
            get
            {
                return Task.Run(() => Store.GetValue("CustomDataAgreementHash", "")).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("CustomDataAgreementHash", value)).GetAwaiter().GetResult();
            }
        }

        public static int LastLocalHostedGamePort
        {
            get
            {
                return Task.Run(() => Store.GetValue("LastLocalHostedGamePort", 5000)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("LastLocalHostedGamePort", value)).GetAwaiter().GetResult();
            }
        }

        public static ulong PrivateKey
        {
            get
            {
                return Task.Run(() => Store.GetValue("PrivateKey", ((ulong)Crypto.PositiveRandom()) << 32 | Crypto.PositiveRandom())).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("PrivateKey", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableAdvancedOptions
        {
            get
            {
                return Task.Run(() => Store.GetValue("EnableAdvancedOptions", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("EnableAdvancedOptions", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableLanGames
        {
            get
            {
                return Task.Run(() => Store.GetValue("EnableLanGamesWhileOnline", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("EnableLanGamesWhileOnline", value)).GetAwaiter().GetResult();
            }
        }

        public static bool UseGameFonts
        {
            get
            {
                return Task.Run(() => Store.GetValue("UseGameFonts", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("UseGameFonts", value)).GetAwaiter().GetResult();
            }
        }

        public static int DeckEditorFontSize
        {
            get
            {
                return Task.Run(() => Store.GetValue("DeckEditorFontSize", 12)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("DeckEditorFontSize", value)).GetAwaiter().GetResult();
            }
        }

        public static int ChatFontSize
        {
            get
            {
                return Task.Run(() => Store.GetValue("ChatFontSize", 12)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ChatFontSize", value)).GetAwaiter().GetResult();
            }
        }

        public static int NoteFontSize
        {
            get
            {
                return Task.Run(() => Store.GetValue("NoteFontSize", 12)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("NoteFontSize", value)).GetAwaiter().GetResult();
            }
        }

        public static int ContextFontSize
        {
            get
            {
                return Task.Run(() => Store.GetValue("ContextFontSize", 12)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ContextFontSize", value)).GetAwaiter().GetResult();
            }
        }

        public static bool UnderstandsChat
        {
            get
            {
                return Task.Run(() => Store.GetValue("UnderstandsChat", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("UnderstandsChat", value)).GetAwaiter().GetResult();
            }
        }

        public static bool EnableGameScripts
        {
            get
            {
                return Task.Run(() => Store.GetValue("EnableGameScripts", true)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("EnableGameScripts", value)).GetAwaiter().GetResult();
            }
        }

        public static double HandDensity
        {
            get
            {
                return Task.Run(() => Store.GetValue("HandDensity", 20d)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("HandDensity", value)).GetAwaiter().GetResult();
            }
        }

        public static bool ExtendedTooltips
        {
            get
            {
                return Task.Run(() => Store.GetValue("ExtendedTooltips", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ExtendedTooltips", value)).GetAwaiter().GetResult();
            }
        }

        public static bool HasSeenSpectateMessage
        {
            get
            {
                return Task.Run(() => Store.GetValue("HasSeenSpectateMessage", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("HasSeenSpectateMessage", value)).GetAwaiter().GetResult();
            }
        }

        public static bool UsingWine
        {
            get
            {
                return Task.Run(() => Store.GetValue("UsingWine", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("UsingWine", value)).GetAwaiter().GetResult();
            }
        }

        public static bool AskedIfUsingWine
        {
            get
            {
                return Task.Run(() => Store.GetValue("AskedIfUsingWine", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("AskedIfUsingWine", value)).GetAwaiter().GetResult();
            }
        }

        public static bool IsAdmin
        {
            get
            {
                return Task.Run(() => Store.GetValue("IsAdmin", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("IsAdmin", value)).GetAwaiter().GetResult();
            }
        }

        public static bool UseTestReleases
        {
            get
            {
                return Task.Run(() => Store.GetValue("UseTestReleases", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("UseTestReleases", value)).GetAwaiter().GetResult();
            }
        }

        public static string DeviceId
        {
            get
            {
                return Task.Run(() => Store.GetValue(nameof(DeviceId), Guid.NewGuid().ToString())).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue(nameof(DeviceId), value)).GetAwaiter().GetResult();
            }
        }

        public static bool ShowAltsInDeckEditor
        {
            get
            {
                return Task.Run(() => Store.GetValue("ShowAltsInDeckEditor", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("ShowAltsInDeckEditor", value)).GetAwaiter().GetResult();
            }
        }

        public static bool InGameChatTextShadows
        {
            get
            {
                return Task.Run(() => Store.GetValue("InGameChatTextShadows", false)).GetAwaiter().GetResult();
            }
            set
            {
                Task.Run(() => Store.SetValue("InGameChatTextShadows", value)).GetAwaiter().GetResult();
            }
        }

        
        
        
        // public static async Task<bool> GetInstallOnBootAsync()
        // {
        //     return await Store.GetValue("InstallOnBoot", true);
        // }
        //
        // public static async Task SetInstallOnBootAsync(bool value)
        // {
        //     await Store.SetValue("InstallOnBoot", value);
        // }
        //
        // public static async Task<bool> GetCleanDatabaseAsync()
        // {
        //     return await Store.GetValue("CleanDatabase", true);
        // }
        //
        // public static async Task SetCleanDatabaseAsync(bool value)
        // {
        //     await Store.SetValue("CleanDatabase", value);
        // }
        //
        // public static async Task<string> GetSessionKeyAsync()
        // {
        //     return await Store.GetValue("SessionKey", "");
        // }
        //
        // public static async Task SetSessionKeyAsync(string value)
        // {
        //     await Store.SetValue("SessionKey", value);
        // }
        //
        // public static async Task<string> GetUsernameAsync()
        // {
        //     return await Store.GetValue("Username", "");
        // }
        //
        // public static async Task SetUsernameAsync(string value)
        // {
        //     await Store.SetValue("Username", value);
        // }
        //
        // public static async Task<bool> GetEnableWhisperSoundAsync()
        // {
        //     return await Store.GetValue("EnableWhisperSound", true);
        // }
        //
        // public static async Task SetEnableWhisperSoundAsync(bool value)
        // {
        //     await Store.SetValue("EnableWhisperSound", value);
        // }
        //
        // public static async Task<bool> GetEnableNameSoundAsync()
        // {
        //     return await Store.GetValue("EnableNameSound", true);
        // }
        //
        // public static async Task SetEnableNameSoundAsync(bool value)
        // {
        //     await Store.SetValue("EnableNameSound", value);
        // }
        //
        // public static async Task<ZoomType> GetZoomOptionAsync()
        // {
        //     return await Store.GetValue("ZoomOption", ZoomType.OriginalOrProxy);
        // }
        //
        // public static async Task SetZoomOptionAsync(ZoomType value)
        // {
        //     await Store.SetValue("ZoomOption", value);
        // }
        //
        // public static async Task<SoundType> GetSoundOptionAsync()
        // {
        //     return await Store.GetValue("JoinSound", SoundType.DingDong);
        // }
        //
        // public static async Task SetSoundOptionAsync(SoundType value)
        // {
        //     await Store.SetValue("JoinSound", value);
        // }
        //
        // public static async Task<CardAnimType> GetCardMoveNotificationAsync()
        // {
        //     return await Store.GetValue("CardMoveNotification", CardAnimType.NormalAnimation);
        // }
        //
        // public static async Task SetCardMoveNotificationAsync(CardAnimType value)
        // {
        //     await Store.SetValue("CardMoveNotification", value);
        // }
        //
        // public static async Task<int> GetMaxChatHistoryAsync()
        // {
        //     return await Store.GetValue("MaxChatHistory", 100);
        // }
        //
        // public static async Task SetMaxChatHistoryAsync(int value)
        // {
        //     await Store.SetValue("MaxChatHistory", value);
        // }
        //
        // public static async Task<bool> GetEnableChatImagesAsync()
        // {
        //     return await Store.GetValue("EnableChatImages", true);
        // }
        //
        // public static async Task SetEnableChatImagesAsync(bool value)
        // {
        //     await Store.SetValue("EnableChatImages", value);
        // }
        //
        // public static async Task<bool> GetEnableChatGifsAsync()
        // {
        //     return await Store.GetValue("EnableChatGifs", true);
        // }
        //
        // public static async Task SetEnableChatGifsAsync(bool value)
        // {
        //     await Store.SetValue("EnableChatGifs", value);
        // }
        //
        // public static async Task<string> GetNicknameAsync()
        // {
        //     return await Store.GetValue("Nickname", "null");
        // }
        //
        // public static async Task SetNicknameAsync(string value)
        // {
        //     await Store.SetValue("Nickname", value);
        // }
        //
        // public static async Task<string> GetLastFolderAsync()
        // {
        //     return await Store.GetValue("lastFolder", "");
        // }
        //
        // public static async Task SetLastFolderAsync(string value)
        // {
        //     await Store.SetValue("lastFolder", value);
        // }
        //
        // public static async Task<string> GetLastRoomNameAsync()
        // {
        //     return await Store.GetValue<string>("lastroomname", null);
        // }
        //
        // public static async Task SetLastRoomNameAsync(string value)
        // {
        //     await Store.SetValue("lastroomname", value);
        // }
        //
        // public static async Task<Guid> GetLastHostedGameTypeAsync()
        // {
        //     var result = await Store.GetValue("lasthostedgametype", Guid.Empty.ToString());
        //     return Guid.TryParse(result, out var guid) ? guid : Guid.Empty;
        // }
        //
        // public static async Task SetLastHostedGameTypeAsync(Guid value)
        // {
        //     await Store.SetValue("lasthostedgametype", value.ToString());
        // }
        //
        // public static async Task<bool> GetTwoSidedTableAsync()
        // {
        //     return await Store.GetValue("twosidedtable", true);
        // }
        //
        // public static async Task SetTwoSidedTableAsync(bool value)
        // {
        //     await Store.SetValue("twosidedtable", value);
        // }
        //
        // public static async Task<Point> GetLoginLocationAsync()
        // {
        //     return await Store.GetValue("LoginLoc", new Point(100, 100));
        // }
        //
        // public static async Task SetLoginLocationAsync(Point value)
        // {
        //     await Store.SetValue("LoginLoc", value);
        // }
        //
        // public static async Task<Point> GetMainLocationAsync()
        // {
        //     return await Store.GetValue("MainLoc", new Point(100, 100));
        // }
        //
        // public static async Task SetMainLocationAsync(Point value)
        // {
        //     await Store.SetValue("MainLoc", value);
        // }
        //
        // public static async Task<Rect> GetPreviewCardWindowLocationAsync()
        // {
        //     try
        //     {
        //         return Rect.Parse(await Store.GetValue("PreviewCardLoc", "100, 100, 200, 300"));
        //     }
        //     catch
        //     {
        //         return new Rect(100, 100, 200, 300);
        //     }
        // }
        //
        // public static async Task SetPreviewCardWindowLocationAsync(Rect value)
        // {
        //     await Store.SetValue("PreviewCardLoc", value.ToString());
        // }
        //
        // public static async Task<Rect> GetChatWindowLocationAsync()
        // {
        //     try
        //     {
        //         return Rect.Parse(await Store.GetValue("ChatWindowLoc", "100, 100, 200, 300"));
        //     }
        //     catch
        //     {
        //         return new Rect(100, 100, 200, 300);
        //     }
        // }
        //
        // public static async Task SetChatWindowLocationAsync(Rect value)
        // {
        //     await Store.SetValue("ChatWindowLoc", value.ToString());
        // }
        //
        // public static async Task<int> GetLoginTimeoutAsync()
        // {
        //     return await Store.GetValue("LoginTimeout", 30000);
        // }
        //
        // public static async Task SetLoginTimeoutAsync(int value)
        // {
        //     await Store.SetValue("LoginTimeout", value);
        // }
        //
        // public static async Task<bool> GetUseLightChatAsync()
        // {
        //     return await Store.GetValue("LightChat", false);
        // }
        //
        // public static async Task SetUseLightChatAsync(bool value)
        // {
        //     await Store.SetValue("LightChat", value);
        // }
        //
        // public static async Task<bool> GetUseHardwareRenderingAsync()
        // {
        //     return await Store.GetValue("UseHardwareRendering", true);
        // }
        //
        // public static async Task SetUseHardwareRenderingAsync(bool value)
        // {
        //     await Store.SetValue("UseHardwareRendering", value);
        // }
        //
        // public static async Task<bool> GetUseWindowTransparencyAsync()
        // {
        //     return await Store.GetValue("UseWindowTransparency", false);
        // }
        //
        // public static async Task SetUseWindowTransparencyAsync(bool value)
        // {
        //     await Store.SetValue("UseWindowTransparency", value);
        // }
        //
        // public static async Task<int> GetHistoryPageSizeAsync()
        // {
        //     return await Store.GetValue("HistoryPageSize", 16);
        // }
        //
        // public static async Task SetHistoryPageSizeAsync(int value)
        // {
        //     await Store.SetValue("HistoryPageSize", value);
        // }
        //
        // public static async Task<bool> GetEnableGameSoundAsync()
        // {
        //     return await Store.GetValue("ReallyEnableGameSound", true);
        // }
        //
        // public static async Task SetEnableGameSoundAsync(bool value)
        // {
        //     await Store.SetValue("ReallyEnableGameSound", value);
        // }
        //
        // public static async Task<string> GetWindowBorderDecoratorAsync()
        // {
        //     var border = await Store.GetValue<string>("WindowBorderDecorator", null);
        //     if (string.IsNullOrEmpty(border))
        //     {
        //         await SetWindowBorderDecoratorAsync("Octgn");
        //         return "Octgn";
        //     }
        //     return border;
        // }
        //
        // public static async Task SetWindowBorderDecoratorAsync(string value)
        // {
        //     await Store.SetValue("WindowBorderDecorator", value);
        // }
        //
        // public static async Task<string> GetImportImagesLastPathAsync()
        // {
        //     var fpath = await Store.GetValue("ImportImagesLastPath", "");
        //     if (string.IsNullOrWhiteSpace(fpath)) return fpath;
        //     if (!Directory.Exists(fpath))
        //     {
        //         await SetImportImagesLastPathAsync("");
        //         return "";
        //     }
        //     return fpath;
        // }
        //
        // public static async Task SetImportImagesLastPathAsync(string value)
        // {
        //     await Store.SetValue("ImportImagesLastPath", value);
        // }
        //
        // public static async Task<string> GetWindowSkinAsync()
        // {
        //     var fpath = await Store.GetValue("WindowSkin", "");
        //     if (string.IsNullOrWhiteSpace(fpath)) return fpath;
        //     if (!File.Exists(fpath))
        //     {
        //         await SetWindowSkinAsync("");
        //         return "";
        //     }
        //     return fpath;
        // }
        //
        // public static async Task SetWindowSkinAsync(string value)
        // {
        //     await Store.SetValue("WindowSkin", value);
        // }
        //
        // public static async Task<bool> GetTileWindowSkinAsync()
        // {
        //     return await Store.GetValue("TileWindowSkin", false);
        // }
        //
        // public static async Task SetTileWindowSkinAsync(bool value)
        // {
        //     await Store.SetValue("TileWindowSkin", value);
        // }
        //
        // public static async Task<string> GetDefaultGameBackAsync()
        // {
        //     var fpath = await Store.GetValue("DefaultGameBack", "");
        //     if (string.IsNullOrWhiteSpace(fpath)) return fpath;
        //     if (!File.Exists(fpath))
        //     {
        //         await SetDefaultGameBackAsync("");
        //         return "";
        //     }
        //     return fpath;
        // }
        //
        // public static async Task SetDefaultGameBackAsync(string value)
        // {
        //     await Store.SetValue("DefaultGameBack", value);
        // }
        //
        // public static async Task<bool> GetHideUninstalledGamesInListAsync()
        // {
        //     return await Store.GetValue("HideUninstalledGamesInList", false);
        // }
        //
        // public static async Task SetHideUninstalledGamesInListAsync(bool value)
        // {
        //     await Store.SetValue("HideUninstalledGamesInList", value);
        // }
        //
        // public static async Task<bool> GetIgnoreSSLCertificatesAsync()
        // {
        //     return await Store.GetValue("IgnoreSSLCertificates", false);
        // }
        //
        // public static async Task SetIgnoreSSLCertificatesAsync(bool value)
        // {
        //     await Store.SetValue("IgnoreSSLCertificates", value);
        // }
        //
        // public static async Task<bool> GetUseWindowsForChatAsync()
        // {
        //     return await Store.GetValue("UseWindowsForChat", false);
        // }
        //
        // public static async Task SetUseWindowsForChatAsync(bool value)
        // {
        //     await Store.SetValue("UseWindowsForChat", value);
        // }
        //
        // public static async Task<bool> GetInstantSearchAsync()
        // {
        //     return await Store.GetValue("InstantSearch", true);
        // }
        //
        // public static async Task SetInstantSearchAsync(bool value)
        // {
        //     await Store.SetValue("InstantSearch", value);
        // }
        //
        // public static async Task<bool> GetHideResultCountAsync()
        // {
        //     return await Store.GetValue("HideResultCount", false);
        // }
        //
        // public static async Task SetHideResultCountAsync(bool value)
        // {
        //     await Store.SetValue("HideResultCount", value);
        // }
        //
        // public static async Task<bool> GetAcceptedCustomDataAgreementAsync()
        // {
        //     return await Store.GetValue("AcceptedCustomDataAgreement", false);
        // }
        //
        // public static async Task SetAcceptedCustomDataAgreementAsync(bool value)
        // {
        //     await Store.SetValue("AcceptedCustomDataAgreement", value);
        // }
        //
        // public static async Task<string> GetCustomDataAgreementHashAsync()
        // {
        //     return await Store.GetValue("CustomDataAgreementHash", "");
        // }
        //
        // public static async Task SetCustomDataAgreementHashAsync(string value)
        // {
        //     await Store.SetValue("CustomDataAgreementHash", value);
        // }
        //
        // public static async Task<int> GetLastLocalHostedGamePortAsync()
        // {
        //     return await Store.GetValue("LastLocalHostedGamePort", 5000);
        // }
        //
        // public static async Task SetLastLocalHostedGamePortAsync(int value)
        // {
        //     await Store.SetValue("LastLocalHostedGamePort", value);
        // }
        //
        // public static async Task<ulong> GetPrivateKeyAsync()
        // {
        //     return await Store.GetValue("PrivateKey", ((ulong)Crypto.PositiveRandom()) << 32 | Crypto.PositiveRandom());
        // }
        //
        // public static async Task SetPrivateKeyAsync(ulong value)
        // {
        //     await Store.SetValue("PrivateKey", value);
        // }
        //
        // public static async Task<bool> GetEnableAdvancedOptionsAsync()
        // {
        //     return await Store.GetValue("EnableAdvancedOptions", false);
        // }
        //
        // public static async Task SetEnableAdvancedOptionsAsync(bool value)
        // {
        //     await Store.SetValue("EnableAdvancedOptions", value);
        // }
        //
        // public static async Task<bool> GetEnableLanGamesAsync()
        // {
        //     return await Store.GetValue("EnableLanGamesWhileOnline", false);
        // }
        //
        // public static async Task SetEnableLanGamesAsync(bool value)
        // {
        //     await Store.SetValue("EnableLanGamesWhileOnline", value);
        // }
        //
        // public static async Task<bool> GetUseGameFontsAsync()
        // {
        //     return await Store.GetValue("UseGameFonts", false);
        // }
        //
        // public static async Task SetUseGameFontsAsync(bool value)
        // {
        //     await Store.SetValue("UseGameFonts", value);
        // }
        //
        // public static async Task<int> GetDeckEditorFontSizeAsync()
        // {
        //     return await Store.GetValue("DeckEditorFontSize", 12);
        // }
        //
        // public static async Task SetDeckEditorFontSizeAsync(int value)
        // {
        //     await Store.SetValue("DeckEditorFontSize", value);
        // }
        //
        // public static async Task<int> GetChatFontSizeAsync()
        // {
        //     return await Store.GetValue("ChatFontSize", 12);
        // }
        //
        // public static async Task SetChatFontSizeAsync(int value)
        // {
        //     await Store.SetValue("ChatFontSize", value);
        // }
        //
        // public static async Task<int> GetNoteFontSizeAsync()
        // {
        //     return await Store.GetValue("NoteFontSize", 12);
        // }
        //
        // public static async Task SetNoteFontSizeAsync(int value)
        // {
        //     await Store.SetValue("NoteFontSize", value);
        // }
        //
        // public static async Task<int> GetContextFontSizeAsync()
        // {
        //     return await Store.GetValue("ContextFontSize", 12);
        // }
        //
        // public static async Task SetContextFontSizeAsync(int value)
        // {
        //     await Store.SetValue("ContextFontSize", value);
        // }
        //
        // public static async Task<bool> GetUnderstandsChatAsync()
        // {
        //     return await Store.GetValue("UnderstandsChat", false);
        // }
        //
        // public static async Task SetUnderstandsChatAsync(bool value)
        // {
        //     await Store.SetValue("UnderstandsChat", value);
        // }
        //
        // public static async Task<bool> GetEnableGameScriptsAsync()
        // {
        //     return await Store.GetValue("EnableGameScripts", true);
        // }
        //
        // public static async Task SetEnableGameScriptsAsync(bool value)
        // {
        //     await Store.SetValue("EnableGameScripts", value);
        // }
        //
        // public static async Task<double> GetHandDensityAsync()
        // {
        //     return await Store.GetValue("HandDensity", 20d);
        // }
        //
        // public static async Task SetHandDensityAsync(double value)
        // {
        //     await Store.SetValue("HandDensity", value);
        // }
        //
        // public static async Task<bool> GetExtendedTooltipsAsync()
        // {
        //     return await Store.GetValue("ExtendedTooltips", false);
        // }
        //
        // public static async Task SetExtendedTooltipsAsync(bool value)
        // {
        //     await Store.SetValue("ExtendedTooltips", value);
        // }
        //
        // public static async Task<bool> GetHasSeenSpectateMessageAsync()
        // {
        //     return await Store.GetValue("HasSeenSpectateMessage", false);
        // }
        //
        // public static async Task SetHasSeenSpectateMessageAsync(bool value)
        // {
        //     await Store.SetValue("HasSeenSpectateMessage", value);
        // }
        //
        // public static async Task<bool> GetUsingWineAsync()
        // {
        //     return await Store.GetValue("UsingWine", false);
        // }
        //
        // public static async Task SetUsingWineAsync(bool value)
        // {
        //     await Store.SetValue("UsingWine", value);
        // }
        //
        // public static async Task<bool> GetAskedIfUsingWineAsync()
        // {
        //     return await Store.GetValue("AskedIfUsingWine", false);
        // }
        //
        // public static async Task SetAskedIfUsingWineAsync(bool value)
        // {
        //     await Store.SetValue("AskedIfUsingWine", value);
        // }
        //
        // public static async Task<bool> GetIsAdminAsync()
        // {
        //     return await Store.GetValue("IsAdmin", false);
        // }
        //
        // public static async Task SetIsAdminAsync(bool value)
        // {
        //     await Store.SetValue("IsAdmin", value);
        // }
        //
        // public static async Task<bool> GetUseTestReleasesAsync()
        // {
        //     return await Store.GetValue("UseTestReleases", false);
        // }
        //
        // public static async Task SetUseTestReleasesAsync(bool value)
        // {
        //     await Store.SetValue("UseTestReleases", value);
        // }
        //
        // public static async Task<string> GetDeviceIdAsync()
        // {
        //     return await Store.GetValue("DeviceId", Guid.NewGuid().ToString());
        // }
        //
        // public static async Task SetDeviceIdAsync(string value)
        // {
        //     await Store.SetValue("DeviceId", value);
        // }
        //
        // public static async Task<bool> GetShowAltsInDeckEditorAsync()
        // {
        //     return await Store.GetValue("ShowAltsInDeckEditor", false);
        // }
        //
        // public static async Task SetShowAltsInDeckEditorAsync(bool value)
        // {
        //     await Store.SetValue("ShowAltsInDeckEditor", value);
        // }
        //
        // public static async Task<bool> GetInGameChatTextShadowsAsync()
        // {
        //     return await Store.GetValue("InGameChatTextShadows", false);
        // }
        //
        // public static async Task SetInGameChatTextShadowsAsync(bool value)
        // {
        //     await Store.SetValue("InGameChatTextShadows", value);
        // }
    }
    
    
}