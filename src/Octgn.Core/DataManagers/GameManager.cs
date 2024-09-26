using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Avalonia.Platform.Storage;

// using NuGet;

using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew;
using Octgn.DataNew.Entities;
using Octgn.Library;
using Octgn.Library.Exceptions;
using Octgn.Library.ExtensionMethods;

using log4net;
using NuGet.Protocol.Core.Types;
using Octgn.Library.Localization;
using Octgn.Library.Networking;
using OctgnCross.Core;

namespace Octgn.Core.DataManagers
{
    public class GameManager
    {
        #region Singleton
        private static GameManager Context { get; set; }
        private static object locker = new object();
        public static GameManager Get()
        {
            lock (locker) return Context ?? (Context = new GameManager());
        }
        internal GameManager()
        {

        }
        #endregion Singleton

        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler GameListChanged;

        public int GameCount
        {
            get
            {
                return DbContext.Get().Games.Count();
            }
        }

        public Game GetById(Guid id)
        {
            try
            {
                //Log.InfoFormat("Getting game by id {0}",id);
                return DbContext.Get().GameById(id);
            }
            finally
            {
                //Log.InfoFormat("Finished {0}", id);
            }
        }

        public IEnumerable<Game> Games
        {
            get
            {
                try
                {
                    //Log.Info("Getting games");
                    return DbContext.Get().Games.OrderBy(x => x.Name);
                }
                finally
                {
                    //Log.Info("Finished");
                }
            }
        }

        public void UninstallAllGames()
        {
            var games = Games.ToList();
            foreach (var g in games)
                UninstallGame(g);
        }

        public async Task InstallGame(GameInfo gameInfo,
            Action<int, int> onProgressUpdate = null)
        {
            if (onProgressUpdate == null) onProgressUpdate = (i, i1) => { };

            Log.InfoFormat("Installing game {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
            try
            {
                onProgressUpdate(-1, 1);
                Log.InfoFormat("Creating path {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                var dirPath = Path.GetTempPath();
                dirPath = Path.Combine(dirPath, "o8ginstall-" + Guid.NewGuid());
                Log.InfoFormat("Extracting package {0} {1} {2}", dirPath, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                await GameFeedManager.Get().ExtractPackage(dirPath, gameInfo,onProgressUpdate);
                onProgressUpdate(-1, 1);
                Log.InfoFormat("Making def path {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                var defPath = Path.Combine(dirPath, "def");
                if (!Directory.Exists(defPath))
                {
                    Log.WarnFormat("Def path doesn't exist in the game package {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                    return;
                }
                var di = new DirectoryInfo(defPath);
                Log.InfoFormat("Copying temp files {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                var files = di.GetFiles("*", SearchOption.AllDirectories).ToArray();
                var curFileNum = 0;
                onProgressUpdate(curFileNum, files.Length);
                var packagePath = Path.Combine(Config.Instance.Paths.DatabasePath, gameInfo.Package.Identity.Id);
                var packagePathInfo = new DirectoryInfo(packagePath);
                if (packagePathInfo.Exists)
                {
                    Log.InfoFormat("Package path {0} exists.  Relocating.", packagePathInfo.FullName);
                    var curFiles = packagePathInfo.GetFiles("*", SearchOption.AllDirectories).ToArray();
                    Log.InfoFormat("Checking {0} for files that are no longer used.", packagePath);
                    foreach (var cf in curFiles)
                    {
                        var relPath = cf.FullName.Replace(packagePathInfo.FullName, "");
                        relPath = relPath.TrimStart('\\', '/');
                        var newPath = Path.Combine(di.FullName, relPath);
                        var newFileInfo = new FileInfo(newPath);
                        if (!newFileInfo.Exists)
                        {
                            Log.DebugFormat("Removing {0}", relPath);
                            X.Instance.Retry(cf.Delete);
                        }
                        curFileNum++;
                        onProgressUpdate(curFileNum, curFiles.Length);
                    }
                }
                curFileNum = 0;
                onProgressUpdate(-1, 1);
                foreach (var f in files)
                {
                    try
                    {
                        Log.DebugFormat("Copying temp file {0} {1} {2}", f.FullName, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                        var relPath = f.FullName.Replace(di.FullName, "");
                        relPath = relPath.TrimStart('\\', '/');
                        var newPath = Path.Combine(Config.Instance.Paths.DatabasePath, gameInfo.Package.Identity.Id);
                        newPath = Path.Combine(newPath, relPath);
                        var newFileInfo = new FileInfo(newPath);
                        if (newFileInfo.Directory != null)
                        {
                            Log.DebugFormat("Creating directory {0} {1} {2}", newFileInfo.Directory.FullName, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                            Directory.CreateDirectory(newFileInfo.Directory.FullName);
                        }
                        Log.DebugFormat("Copying file {0} {1} {2} {3}", f.FullName, newPath, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                        f.MegaCopyTo(newPath);
                        Log.DebugFormat("File copied {0} {1} {2} {3}", f.FullName, newPath, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                        curFileNum++;
                        onProgressUpdate(curFileNum, files.Length);

                    }
                    catch(Exception e)
                    {
                        Log.Warn(String.Format("InstallGame 1 {0} {1} {2}", f.FullName, gameInfo.Package.Identity.Id, gameInfo.Package.Title), e);
                        throw;
                    }
                }
                onProgressUpdate(-1, 1);
                //Sets//setid//Cards//Proxies

                var setsDir = Path.Combine(Config.Instance.Paths.DatabasePath, gameInfo.Package.Identity.Id, "Sets");
                var imageSetsDir = Path.Combine(Config.Instance.ImageDirectoryFull, gameInfo.Package.Identity.Id, "Sets");
                if (!Directory.Exists(imageSetsDir))
                {
                    Directory.CreateDirectory(imageSetsDir);
                }

                var game = GameManager.Get().GetById(new Guid(gameInfo.Package.Identity.Id));
                if (game == null)
                    throw new UserMessageException(L.D.Exception__CanNotInstallGameTryRestart_Format, gameInfo.Package.Title);

                Log.InfoFormat("Installing plugins {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                if (Directory.Exists(Path.Combine(game.InstallPath, "Plugins")))
                {
                    var pluginFiles = new DirectoryInfo(Path.Combine(game.InstallPath, "Plugins")).GetFiles("*.dll", SearchOption.AllDirectories).ToArray();
                    var curPluginFileNum = 0;
                    onProgressUpdate(curPluginFileNum, pluginFiles.Length);
                    foreach (var f in pluginFiles)
                    {
                        try
                        {
                            Log.DebugFormat("Found plugin file {0} {1} {2}", f.FullName, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                            var relPath = f.FullName.Replace(new DirectoryInfo(Path.Combine(game.InstallPath, "Plugins")).FullName, "").TrimStart('\\');
                            var newPath = Path.Combine(Config.Instance.Paths.PluginPath, relPath);
                            Log.DebugFormat("Creating directories {0} {1} {2}", f.FullName, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                            if (new DirectoryInfo(newPath).Exists)
                                Directory.Move(newPath, Config.Instance.Paths.GraveyardPath);
                            Directory.CreateDirectory(new FileInfo(newPath).Directory.FullName);
                            Log.DebugFormat("Copying plugin to {0} {1} {2} {3}", f.FullName, newPath, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                            f.MegaCopyTo(newPath);
                            curPluginFileNum++;
                            onProgressUpdate(curPluginFileNum, pluginFiles.Length);

                        }
                        catch (Exception e)
                        {
                            Log.Warn(String.Format("InstallGame Plugin {0} {1} {2}", f.FullName, gameInfo.Package.Identity.Id, gameInfo.Package.Title), e);
                            throw;
                        }
                    }
                }
                onProgressUpdate(-1, 1);

                var setsDeckFolders =
                    game.Sets()
                        .Select(x => new { Set = x, DeckDirectory = new DirectoryInfo(x.DeckPath) })
                        .Where(x => x.DeckDirectory.Exists)
                        .Select(x => new { Set = x.Set, DeckDirectory = x.DeckDirectory, files = x.DeckDirectory.GetFiles("*.o8d", SearchOption.AllDirectories) })
                        .ToArray();
                var max = setsDeckFolders.SelectMany(x => x.files).Count();
                var curSetDeckNum = 0;
                onProgressUpdate(curSetDeckNum, max);
                foreach (var set in setsDeckFolders)
                {
                    foreach (var deck in set.files)
                    {
                        try
                        {
                            Log.DebugFormat("Found deck file {0} {1} {2} {3}", deck.FullName, setsDir, gameInfo.Package.Identity.Id,gameInfo.Package.Title);
                            var relPath = deck.FullName.Replace(set.DeckDirectory.FullName, "").TrimStart('\\');
                            var newPath = Path.Combine(Config.Instance.Paths.DeckPath, game.Name, relPath);
                            Log.DebugFormat("Creating directories {0} {1} {2} {3}", deck.FullName, setsDir, gameInfo.Package.Identity.Id,gameInfo.Package.Title);
                            Directory.CreateDirectory(new FileInfo(newPath).Directory.FullName);
                            Log.DebugFormat("Copying deck to {0} {1} {2} {3} {4}", deck.FullName, newPath, setsDir,
                                gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                            deck.MegaCopyTo(newPath);
                            curSetDeckNum++;
                            onProgressUpdate(curSetDeckNum, max);
                        }
                        catch (Exception e)
                        {
                            Log.Warn(String.Format("InstallGame 3 {0} {1} {2} {3}", deck.FullName, setsDir, gameInfo.Package.Identity.Id,gameInfo.Package.Title), e);
                            throw;
                        }
                    }
                }
                onProgressUpdate(-1, 1);

                Log.InfoFormat("Deleting proxy cards {0} {1} {2}", setsDir, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                // Clear out all proxies if they exist
                var proxyFiles = new DirectoryInfo(imageSetsDir).GetDirectories().Select(x => new DirectoryInfo(Path.Combine(x.FullName, "Cards", "Proxies"))).Where(x => x.Exists).ToArray();
                var currentProxyFilesNum = 0;
                onProgressUpdate(currentProxyFilesNum, proxyFiles.Length);
                foreach (var pdir in proxyFiles)
                {
                    try
                    {
                        var pstring = pdir.FullName;
                        Log.DebugFormat("Deleting proxy dir {0} {1} {2}", pdir, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                        pdir.MoveTo(Config.Instance.Paths.GraveyardPath);
                        Log.DebugFormat("Deleted proxy dir {0} {1} {2}", pdir, gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                        Directory.CreateDirectory(pstring);
                    }
                    catch (Exception)
                    {
                        Log.WarnFormat("Could not delete proxy directory {0}", pdir.FullName);
                    }
                    currentProxyFilesNum++;
                    onProgressUpdate(currentProxyFilesNum, proxyFiles.Length);
                }
                onProgressUpdate(-1, 1);
                Log.InfoFormat("Fire game list changed {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
                this.OnGameListChanged();
                Log.InfoFormat("Game list changed fired {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);

                //copy images over to imagedatabase
                var cardImageList = new DirectoryInfo(setsDir)
                    .GetDirectories()
                    .Select(x => new { SetDirectory = x, CardsDirectory = new DirectoryInfo(Path.Combine(x.FullName, "Cards")) })
                    .Where(x => x.CardsDirectory.Exists)
                    .Select(x => new
                                 {
                                     SetDirectory = x.SetDirectory,
                                     CardsDirectory = x.CardsDirectory,
                                     ImageFiles = x.CardsDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(s => !s.FullName.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase)).ToArray()
                                 })
                    .SelectMany(x => x.ImageFiles.Select(i =>
                                new
                                {
                                    SetDirectory = x.SetDirectory,
                                    CardsDirectory = x.CardsDirectory,
                                    Image = i
                                }))
                    .ToArray();

                X.Instance.ForEachProgress(cardImageList.Length, cardImageList,
                    x =>
                    {
                        string copyDirPath = Path.Combine(Config.Instance.ImageDirectoryFull, gameInfo.Package.Identity.Id, "Sets", x.SetDirectory.Name, "Cards");
                        if (!Directory.Exists(copyDirPath))
                        {
                            Directory.CreateDirectory(copyDirPath);
                        }
                        x.Image.CopyTo(Path.Combine(copyDirPath, x.Image.Name), true);
                        x.Image.Delete();
                    }, onProgressUpdate);

            }
            finally
            {
                Log.InfoFormat("Done {0} {1}", gameInfo.Package.Identity.Id, gameInfo.Package.Title);
            }
        }

        public async Task Installo8c(IStorageFile file)
        {
            try
            {
                Log.InfoFormat("Checking if zip file {0}", file.Name);
                using var stream=await file.OpenReadAsync();
                //if (!Ionic.Zip.ZipFile.IsZipFile(stream,true)) throw new UserMessageException(L.D.Exception__CanNotInstallo8cInvalid_Format, file.Name);
                // Log.InfoFormat("Checking if zip file {0}", file.Name);
                // if (!Ionic.Zip.ZipFile.CheckZip(filename)) throw new UserMessageException(L.D.Exception__CanNotInstallo8cInvalid_Format, file.Name);

                Guid gameGuid = Guid.Empty;

                
                Log.InfoFormat("Reading zip file {0}", file.Name);
                // using (ZipArchive archive = ZipFile.OpenRead(filename))
                // {
                //     var result = from currEntry in archive.Entries
                //         where Path.GetDirectoryName(currEntry.FullName) == directory
                //         where !String.IsNullOrEmpty(currEntry.Name)
                //         select currEntry;
                //
                //
                //     foreach (ZipArchiveEntry entry in result)
                //     {
                //         entry.ExtractToFile(Path.Combine(extractPath, entry.Name));
                //     }
                // } 
                
                using (var zip = Ionic.Zip.ZipFile.Read(stream))
                {
                    Log.InfoFormat("Getting zip files {0}", file.Name);
                    var selection = from e in zip.Entries where !e.IsDirectory select e;

                    foreach (var e in selection)
                    {
                        Log.DebugFormat("Checking zip file {0} {1}", e.FileName, file.Name);
                        if (e.FileName.ToLowerInvariant().EndsWith("db"))
                        {
                            continue;
                        }
                        bool extracted = extract(e, out gameGuid, gameGuid);
                        if (!extracted)
                        {
                            Log.Warn(string.Format("Invalid entry in {0}. Entry: {1}.", file.Name, e.FileName));
                            throw new UserMessageException(L.D.Exception__CanNotInstallo8cInvalid_Format, file.Name);
                        }
                        Log.DebugFormat("Extracted {0} {1}", e.FileName, file.Name);
                    }
                }
                Log.InfoFormat("Installed successfully {0}", file.Name);

                //zipFile.ExtractAll(Config.Instance.Paths.DatabasePath,ExtractExistingFileAction.OverwriteSilently);
            }
            catch (Ionic.Zip.ZipException e)
            {
                throw new UserMessageException(String.Format(L.D.Exception__CanNotInstallo8cInvalid_Format, file.Name),e);
            }
            catch (UserMessageException e)
            {
                Log.Warn("User message error", e);
                throw;
            }
        }

        internal struct O8cEntry
        {
            public string gameGuid;
            public string setsDir;
            public string setGuid;
            public string cardsDir;
            public string cardImage;
        }


        internal bool extract(Ionic.Zip.ZipEntry entry, out Guid gameGuid, Guid testGuid)
        {
            try
            {
                Log.DebugFormat("Extracting {0},{1}", entry.FileName, testGuid);
                bool ret = false;
                gameGuid = testGuid;
                string[] split = entry.FileName.Split('/');
                Log.DebugFormat("Split file name {0},{1}", entry.FileName, testGuid);
                if (split.Length == 5 || (split.Length == 6 && split.Contains("Crops")))
                {
                    Log.DebugFormat("File name right count {0},{1}", entry.FileName, testGuid);
                    O8cEntry o8cEntry = new O8cEntry()
                                            {
                                                gameGuid = split[0],
                                                setsDir = split[1],
                                                setGuid = split[2],
                                                cardsDir = split[3],
                                                cardImage = split[4]
                                            };
                    if(split.Contains("Crops"))
                    {
                        o8cEntry.cardImage = split[5];
                    }
                    Log.DebugFormat("Checking if testGuid is empty {0},{1}", entry.FileName, testGuid);
                    if (testGuid.Equals(Guid.Empty))
                    {
                        Log.DebugFormat("testGuid is empty {0},{1}", entry.FileName, testGuid);
                        testGuid = Guid.Parse(o8cEntry.gameGuid);
                        gameGuid = Guid.Parse(o8cEntry.gameGuid);
                        Log.DebugFormat("Setting gameguid and testguid {0},{1},{2}", entry.FileName, testGuid, gameGuid);
                    }
                    Log.DebugFormat("Checking if {0}=={1} {2}", testGuid, o8cEntry.gameGuid, entry.FileName);
                    if (!testGuid.Equals(Guid.Parse(o8cEntry.gameGuid)))
                    {
                        Log.DebugFormat("{0}!={1} {2}", testGuid, o8cEntry.gameGuid, entry.FileName);
                        return (ret);
                    }
                    Log.DebugFormat("Checking if should extract part {0},{1}", entry.FileName, testGuid);
                    if (ShouldExtract(o8cEntry))
                    {
                        Log.DebugFormat(
                            "Should extract, so extracting {0},{1},{2}",
                            Config.Instance.ImageDirectoryFull,
                            entry.FileName,
                            testGuid);
                        entry.Extract(Config.Instance.ImageDirectoryFull, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
                        Log.DebugFormat("Extracted {0},{1},{2}", Config.Instance.ImageDirectoryFull, entry.FileName, testGuid);
                        ret = true;
                    }
                }
                Log.DebugFormat("Finishing {0},{1},{2}", ret, entry.FileName, testGuid);
                return (ret);

            }
            catch (IOException e)
            {
                throw new UserMessageException(L.D.Exception__CanNotExtract_Format, entry.FileName, Config.Instance.Paths.DatabasePath, e.Message);
            }
            finally
            {
                Log.InfoFormat("Finished {0},{1}", entry.FileName, testGuid);
            }
        }

        internal bool ShouldExtract(O8cEntry o8centry)
        {
            try
            {
                Log.InfoFormat("Checking if should extract {0}", o8centry.cardImage);
                bool ret = false;
                Log.InfoFormat("Grabbing game {0},{1}", o8centry.gameGuid, o8centry.cardImage);
                var game = GetById(Guid.Parse(o8centry.gameGuid));
                if (game != null)
                {
                    Log.InfoFormat("Game exists {0},{1}", o8centry.gameGuid, o8centry.cardImage);
                    Guid cardGuid = Guid.Parse(o8centry.cardImage.Split('.')[0]);
                    Log.InfoFormat("Checking Paths {0},{1},{2}", o8centry.setsDir, o8centry.cardsDir, o8centry.cardImage);
                    if (o8centry.setsDir == "Sets" && o8centry.cardsDir == "Cards")
                    {
                        Log.InfoFormat("Paths good {0},{1},{2}", o8centry.setsDir, o8centry.cardsDir, o8centry.cardImage);
                        ret = true;
                    }
                }
                else
                {
                    Log.InfoFormat("Couldn't find game {0},{1}", o8centry.gameGuid, o8centry.cardImage);
                }
                Log.InfoFormat("Finishing {0}", o8centry.cardImage);
                return (ret);

            }
            finally
            {
                Log.InfoFormat("Finished {0}", o8centry.cardImage);
            }
        }

        public void UninstallGame(Game game)
        {
            try
            {
                Log.InfoFormat("Uninstalling game {0}", game.Id);
                var path = Path.Combine(Config.Instance.Paths.DatabasePath, game.Id.ToString());
                var gamePathDi = new DirectoryInfo(path);
                Log.InfoFormat("Deleting folder {0} {1}", path, game.Id);
                int tryCount = 0;
                while (tryCount < 5)
                {
                    try
                    {
                        if (DbContext.Get() is IDbContextCaching dbWithCaching)
                            dbWithCaching.Invalidate(game);

                        var gravePath = Config.Instance.Paths.GraveyardPath;
                        gamePathDi.MoveDirectory(gravePath);
                        break;
                    }
                    catch(Exception ex)
                    {
                        Log.Error("Uninstall error",ex);
                        tryCount++;
                        if (tryCount == 4) throw;
                    }
                }
                Log.InfoFormat("Folder deleted {0} {1}", path, game.Id);
            }
            finally
            {
                Log.InfoFormat("Finished {0}", game.Id);
            }
            //foreach (var file in gamePathDi.GetFiles("*", SearchOption.AllDirectories))
            //{
            //    File.Delete(file.FullName);
            //}
            //foreach (var dir in gamePathDi.GetDirectories("*", SearchOption.AllDirectories))
            //{
            //    Directory.Delete(dir.FullName,true);
            //}
            //Directory.Delete(gamePathDi.FullName);
            this.OnGameListChanged();
        }

        protected virtual void OnGameListChanged()
        {
            var handler = this.GameListChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
