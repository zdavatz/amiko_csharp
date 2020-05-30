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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    public class FullTextDb : INotifyPropertyChanged
    {
        #region Constants
        const string KEY_ROWID = "id";
        const string KEY_KEYWORD = "keyword";
        const string KEY_REGNR = "regnr";

        const string DATABASE_TABLE = "frequency";
        #endregion

        #region Readonlys
        #endregion

        #region Private Fields
        private Favorites<FullTextEntry> _favorites = new Favorites<FullTextEntry>();
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyPropertyChanged(
        [CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dependency Properties
        private ItemsObservableCollection _searchResultItems = new ItemsObservableCollection();
        public ItemsObservableCollection SearchResultItems
        {
            get { return _searchResultItems; }
            private set
            {
                if (value != _searchResultItems)
                {
                    _searchResultItems = value;
                    // OnPropertyChanged is not necessary here...
                }
            }
        }
        #endregion

        #region Private Fields
        private DatabaseHelper _db;
        private List<FullTextEntry> _foundEntries = new List<FullTextEntry>();
        #endregion

        #region Public Methods
        public async void Init()
        {
            string dbPath = Utilities.FrequencyDBPath();
            if (File.Exists(dbPath))
            {
                _db = new DatabaseHelper();
                await _db.OpenDB(dbPath);
                long? numArticles = await _db.GetNumRecords("frequency");
                Log.WriteLine(">> OK: Opened frequency db with {0} items located in {1}", numArticles, dbPath);
            }
            else
            {
                // Cannot open amiko frequency database!
                // Todo: generate friendly message (msgbox...)
                Log.WriteLine(">> ERR: Unable to open sqlite db located in {0}", dbPath);
            }
        }

        public void Close()
        {
            if (_db != null)
                _db.CloseDB();
        }

        public void UpdateSearchResults(UIState state)
        {
            SearchResultItems.Clear();
            SearchResultItems.AddRange(state, _foundEntries);
        }

        public int GetCount()
        {
            return _foundEntries.Count;
        }

        public void ClearFoundEntries()
        {
            _foundEntries.Clear();
        }

        public async Task RetrieveFavorites()
        {
            List<string> hashes = _favorites.Ids();

            _foundEntries.Clear();
            foreach (string hash in hashes)
            {
                var entry = await GetEntryWithHash(hash);
                _foundEntries.Add(entry);
            }
        }

        public async void UpdateFavorites(FullTextEntry entry)
        {
            if (_favorites.Contains(entry?.Hash))
            {
                _favorites.Remove(entry);
            }
            else
            {
                if (entry.Hash != null)
                    _favorites.Add(entry);
            }
            // Save list of favorites to file
            _favorites.Save();
            // Update list of found entries
            foreach (FullTextEntry e in _foundEntries)
            {
                e.IsFavorite = _favorites.Contains(e?.Hash);
            }
        }

        public async Task<long> Search(UIState state, string query)
        {
            _foundEntries.Clear();

            string type = state.GetQueryTypeAsName();
            if (type.Equals("fulltext"))
            {
                _foundEntries = await SearchFullText(query);
            }

            UpdateSearchResults(state);

            return _foundEntries.Count;
        }

        // Finds entries in current _foundEntries
        public async Task<long> Filter(UIState state, string query)
        {
            string type = state.GetQueryTypeAsName();
            if (type.Equals("fulltext"))
            {
                // case insensitive
                _foundEntries = _foundEntries.Where(e => {
                    string keyword = e.Keyword.ToLower();
                    return keyword.Contains(query.ToLower());
                }).ToList();
            }

            UpdateSearchResults(state);

            return _foundEntries.Count;
        }

        public async Task<FullTextEntry> GetEntryWithHash(String hash)
        {
            FullTextEntry entry = new FullTextEntry();

            await Task.Run(() =>
            {
                _db.ReOpenIfNecessary();

                using (SQLiteCommand com = _db.Command())
                {
                    com.CommandText = "SELECT * FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_ROWID + " LIKE " + "'" + hash + "'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entry = CursorToFullTextEntry(reader);
                        }
                    }
                }
            });

            return entry;
        }

        public async Task<List<FullTextEntry>> SearchFullText(string keyword)
        {
            List<FullTextEntry> entries = new List<FullTextEntry>();

            await Task.Run(() =>
            {
                using (SQLiteCommand com = _db.Command())
                {
                    _db.ReOpenIfNecessary();

                    if (keyword.Length > 2)
                    {
                        com.CommandText = "SELECT * FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_KEYWORD + " LIKE " + "'" + keyword + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                entries.Add(CursorToFullTextEntry(reader));
                            }
                        }
                    }
                }
            });

            return entries;
        }
        #endregion

        #region Private Methods
        private FullTextEntry CursorToFullTextEntry(SQLiteDataReader reader)
        {
            FullTextEntry entry = new FullTextEntry();

            entry.Hash = reader[KEY_ROWID] as string;
            entry.Keyword = reader[KEY_KEYWORD] as string;
            entry.IsFavorite = _favorites.Contains(entry.Hash);

            string regnrsAndChapters = reader[KEY_REGNR] as string;
            string[] rac = regnrsAndChapters?.Split('|');
            // Remove double entries by using a set
            var set = new HashSet<string>(rac);
            var dict = new Dictionary<string, HashSet<string>>();
            foreach (string r in set)
            {
                string chapters = "";
                string regnr = "";
                // Extract chapters from parentheses, format 58444(6,7,8)
                if (r.Contains("("))
                {
                    int idx = r.IndexOf("(");
                    int len = r.Length - idx - 2;   // Exclude last parenthesis
                    chapters = r.Substring(r.IndexOf("(") + 1, len);
                    regnr = r.Substring(0, idx);   // Registration number
                }
                if (regnr.Length > 0)
                {
                    HashSet<string> chaptersSet = new HashSet<string>();
                    if (dict.ContainsKey(regnr))
                    {
                        chaptersSet = dict[regnr];
                    }
                    string[] c = chapters.Split(',');
                    foreach (string chapter in c)
                        chaptersSet.Add(chapter.Trim());
                    // Update dictionary
                    dict[regnr] = chaptersSet;
                }
            }
            entry.RegChaptersDict = dict;

            return entry;
        }
        #endregion
    }
}
