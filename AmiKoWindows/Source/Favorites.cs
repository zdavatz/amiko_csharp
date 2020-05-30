/*
Copyright (c) ywesee GmbH

This file is part of AmiKo for Windows.

AmiKo for Windows is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AmiKoWindows
{
    class Favorites<T>
    {
        // 1. favorites: fachinfos, registration numbers
        // 2. favorites: hashes of search terms in frequency database
        HashSet<string> _setOfIds = new HashSet<string>();

        string _userDataDir;

        public Favorites()
        {
            _userDataDir = Utilities.AppRoamingDataFolder();
            CheckDir(_userDataDir);
            // Load favorites from persistent storage
            MigrateFromOldFormat();
            Load();
            GoogleSyncManager.Instance.Progress.ProgressChanged += (sender, progress) =>
            {
                if (progress is SyncProgressFile)
                {
                    var p = progress as SyncProgressFile;
                    if (p.File.FullName.Equals(this.FilePath()))
                    {
                        this.Load();
                    }
                }
            };
        }

        private void MigrateFromOldFormat()
        {
            if (typeof(T) == typeof(Article))
            {
                string favoritesFile = Path.Combine(_userDataDir, "favorites.txt");
                if (!File.Exists(favoritesFile))
                    return;
                HashSet<string> setOfIds = FileOps.ReadFromXmlFile<HashSet<string>>(favoritesFile);
                foreach (var id in setOfIds)
                {
                    this.Add(id);
                }
                File.Delete(favoritesFile);
            }

            if (typeof(T) == typeof(FullTextEntry))
            {
                string fullTextFavoritesFile = Path.Combine(_userDataDir, "favorites_fts.txt");
                if (!File.Exists(fullTextFavoritesFile))
                    return;
                HashSet<string> setOfIds = FileOps.ReadFromXmlFile<HashSet<string>>(fullTextFavoritesFile);
                foreach (var id in setOfIds)
                {
                    this.Add(id);
                }
                File.Delete(fullTextFavoritesFile);
            }

            this.Save();
        }

        private static string CheckDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public List<string> Ids()
        {
            return new List<string>(_setOfIds);
        }

        public string FilePath()
        {
            if (typeof(T) == typeof(Article))
            {
                return Path.Combine(_userDataDir, "favorites.json");
            }
            else if (typeof(T) == typeof(FullTextEntry))
            {
                return Path.Combine(_userDataDir, "favorites-full-text.json");
            }
            return null;
        }

        /*
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        */
        public void Load()
        {
            if (!Directory.Exists(_userDataDir))
                return;

            var serializer = new JavaScriptSerializer();
            var favoritesFile = this.FilePath();
            if (!File.Exists(favoritesFile))
                return;
            var jsonStr = File.ReadAllText(favoritesFile);
            _setOfIds = new HashSet<string>(serializer.Deserialize<List<string>>(jsonStr));
        }

        public void Save()
        {
            if (!Directory.Exists(_userDataDir))
                return;
            var serializer = new JavaScriptSerializer();

            if (_setOfIds.Count > 0)
            {
                string favoritesFile = this.FilePath();
                var jsonStr = serializer.Serialize(new List<string>(_setOfIds));
                File.WriteAllText(favoritesFile, jsonStr);
            }
        }

        public void Add(T entry)
        {
            if (typeof(T) == typeof(Article))
            {
                Article a = entry as Article;
                this.Add(a?.Regnrs);
            }
            else if (typeof(T) == typeof(FullTextEntry))
            {
                FullTextEntry e = entry as FullTextEntry;
                this.Add(e?.Hash);
            }
        }

        public void Add(string id)
        {
            _setOfIds.Add(id);
        }

        public void Remove(T entry)
        {
            if (typeof(T) == typeof(Article))
            {
                Article a = entry as Article;
                var regnrs = a?.Regnrs;
                if (regnrs != null)
                {
                    this.Remove(regnrs);
                }
            }
            else if (typeof(T) == typeof(FullTextEntry))
            {
                FullTextEntry e = entry as FullTextEntry;
                var hash = e?.Hash;
                if (hash != null)
                {
                    this.Remove(hash);
                }
            }
        }

        public void Remove(string id)
        {
            _setOfIds.Remove(id);
        }

        public bool Contains(string key)
        {
            return _setOfIds.Contains(key);
        }
    }
}
