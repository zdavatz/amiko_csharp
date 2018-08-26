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

namespace AmiKoWindows
{
    class Favorites<T>
    {
        // 1. favorites: fachinfos, registration numbers
        // 2. favorites: hashes of search terms in frequency database
        HashSet<string> _setOfIds = new HashSet<string>();
        HashSet<T> _setOfEntries = new HashSet<T>();

        string _userDataDir;

        public Favorites()
        {
            _userDataDir = Utilities.AppRoamingDataFolder();
            CheckDir(_userDataDir);
            // Load favorites from persistent storage
            Load();
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

        /*
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        */
        public void Load()
        {
            if (!Directory.Exists(_userDataDir))
                return;

            if (typeof(T) == typeof(Article))
            {
                string favoritesFile = Path.Combine(_userDataDir, "favorites.txt");
                if (File.Exists(favoritesFile))
                    _setOfIds = FileOps.ReadFromXmlFile<HashSet<string>>(favoritesFile);
            }
            else if (typeof(T) == typeof(FullTextEntry))
            {
                string favoritesFile = Path.Combine(_userDataDir, "favorites_fts.txt");
                if (File.Exists(favoritesFile))
                    _setOfIds = FileOps.ReadFromXmlFile<HashSet<string>>(favoritesFile);
            }
        }

        public async Task Save()
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(_userDataDir))
                    return;

                if (typeof(T) == typeof(Article))
                {
                    if (_setOfIds.Count > 0)
                    {
                        string favoritesFile = Path.Combine(_userDataDir, "favorites.txt");
                        FileOps.WriteToXmlFile<HashSet<string>>(favoritesFile, _setOfIds, false);
                    }
                }
                else if (typeof(T) == typeof(FullTextEntry))
                {
                    if (_setOfIds.Count > 0)
                    {
                        string favoritesFile = Path.Combine(_userDataDir, "favorites_fts.txt");
                        FileOps.WriteToXmlFile<HashSet<string>>(favoritesFile, _setOfIds, false);
                    }
                }
            });
        }

        public void Add(T entry)
        {
            if (typeof(T) == typeof(Article))
            {
                Article a = entry as Article;
                _setOfIds.Add(a?.Regnrs);
            }
            else if (typeof(T) == typeof(FullTextEntry))
            {
                FullTextEntry e = entry as FullTextEntry;
                _setOfIds.Add(e?.Hash);
            }
            _setOfEntries.Add(entry);
        }

        public void Remove(T entry)
        {
            if (typeof(T) == typeof(Article))
            {
                Article a = entry as Article;
                var regnrs = a?.Regnrs;
                if (regnrs != null)
                {
                    _setOfIds.Remove(regnrs);
                    _setOfEntries.RemoveWhere(e1 => (e1 as Article).Regnrs.Equals(regnrs));
                }
            }
            else if (typeof(T) == typeof(FullTextEntry))
            {
                FullTextEntry e = entry as FullTextEntry;
                var hash = e?.Hash;
                if (hash != null)
                {
                    _setOfIds.Remove(hash);
                    _setOfEntries.RemoveWhere(e2 => (e2 as FullTextEntry).Hash.Equals(hash));
                }
            }
        }

        public bool Contains(string key)
        {
            return _setOfIds.Contains(key);
        }

        public bool Contains(T entry)
        {
            return _setOfEntries.Contains(entry);
        }
    }
}
