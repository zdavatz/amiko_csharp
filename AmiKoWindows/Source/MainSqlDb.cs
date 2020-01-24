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
    public class MainSqlDb : INotifyPropertyChanged
    {
        #region Constants
        const string KEY_ROWID = "_id";
        const string KEY_TITLE = "title";
        const string KEY_AUTHOR = "auth";
        const string KEY_ATCCODE = "atc";
        const string KEY_SUBSTANCES = "substances";
        const string KEY_REGNRS = "regnrs";
        const string KEY_ATCCLASS = "atc_class";
        const string KEY_THERAPY = "tindex_str";
        const string KEY_APPLICATION = "application_str";
        const string KEY_INDICATIONS = "indications_str";
        const string KEY_CUSTOMER_ID = "customer_id";
        const string KEY_PACK_INFO = "pack_info_str";
        const string KEY_ADD_INFO = "add_info_str";
        const string KEY_SECTION_IDS = "ids_str";
        const string KEY_SECTION_TITLES = "titles_str";
        const string KEY_CONTENT = "content";
        const string KEY_STYLE = "style_str";
        const string KEY_PACKAGES = "packages";

        const string DATABASE_TABLE = "amikodb";
        #endregion

        #region Readonlys
        // Table columns used for fast queries
        static readonly string SHORT_TABLE = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}",
            KEY_ROWID, KEY_TITLE, KEY_AUTHOR, KEY_ATCCODE, KEY_SUBSTANCES, KEY_REGNRS, KEY_ATCCLASS, KEY_THERAPY,
            KEY_APPLICATION, KEY_INDICATIONS, KEY_CUSTOMER_ID, KEY_PACK_INFO, KEY_ADD_INFO, KEY_PACKAGES);

        static readonly string PACKAGES_TABLE = String.Format("{0},{1},{2},{3},{4}",
            KEY_ROWID, KEY_TITLE, KEY_AUTHOR, KEY_REGNRS, KEY_PACKAGES);

        static readonly string FT_SEARCH_TABLE = String.Format("{0},{1},{2},{3},{4},{5}",
            KEY_ROWID, KEY_TITLE, KEY_AUTHOR, KEY_REGNRS, KEY_SECTION_IDS, KEY_SECTION_TITLES);

        #endregion

        #region Private Fields
        private DatabaseHelper _db;
        private List<Article> _foundArticles = new List<Article>();
        private Favorites<Article> _favorites = new Favorites<Article>();
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

        #region Public Methods
        public async void Init()
        {
            string dbPath = Utilities.SQLiteDBPath();
            if (File.Exists(dbPath))
            {
                _db = new DatabaseHelper();
                await _db.OpenDB(dbPath);
                long? numArticles = await _db.GetNumRecords("amikodb");
                Log.WriteLine(">> OK: Opened sqlite db with {0} items located in {1}", numArticles, dbPath);
            }
            else
            {
                // Cannot open main sqlite database!
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
            SearchResultItems.AddRange(state, _foundArticles);
        }

        public async void UpdateFavorites(Article article)
        {
            if (_favorites.Contains(article?.Regnrs))
            {
                _favorites.Remove(article);
            }
            else
            {
                if (article.Regnrs != null)
                    _favorites.Add(article);
            }
            // Save list of favorites to file
            await _favorites.Save();
            // Update list of found articles
            foreach (Article a in _foundArticles)
            {
                a.IsFavorite = _favorites.Contains(a?.Regnrs);
            }
        }

        public async Task<Article> GetArticleFromEan(string ean)
        {
           return await GetArticleWithEan(ean);
        }

        public async Task<Article> GetArticleFromId(long? id)
        {
            return await GetArticleWithId(id);
        }

        public async Task<long> Search(UIState state, string query)
        {
            Log.WriteLine("query: {0}", query);
            _foundArticles.Clear();

            string type = state.GetQueryTypeAsName();
            switch (type)
            {
                case "title":
                    _foundArticles = await SearchTitle(query);
                    break;
                case "author":
                    _foundArticles = await SearchAuthor(query);
                    break;
                case "atccode":
                    _foundArticles = await SearchATC(query);
                    break;
                case "ingredient":
                    _foundArticles = await SearchIngredient(query);
                    break;
                case "regnr":
                    _foundArticles = await SearchRegNr(query);
                    break;
                case "application":
                    _foundArticles = await SearchApplication(query);
                    break;
                case "eancode":
                    _foundArticles = await SearchEanCode(query);
                    break;
                default:
                    break;
            }
            UpdateSearchResults(state);

            return _foundArticles.Count;
        }

        // Finds articles in current _foundArticles
        public async Task<long> Filter(UIState state, string query)
        {
            Log.WriteLine("query: {0}", query);

            string type = state.GetQueryTypeAsName();
            _foundArticles = _foundArticles.Where(a => {
                string key = "";
                switch (type)
                {
                    // case insensitive filter
                    case "title":
                        if (a.Title != null) key = a.Title.ToLower();
                        break;
                    case "author":
                        if (a.Author != null) key = a.Author.ToLower();
                        break;
                    case "atccode":
                        if (a.AtcCode != null) key = a.AtcCode.ToLower();
                        break;
                    case "ingredient":
                        if (a.AtcCode != null) key = a.Substances.ToLower();
                        break;
                    case "regnr":
                        if (a.Regnrs != null) key = a.Regnrs.ToLower();
                        break;
                    case "application":
                        if (a.Application != null) key = a.Application.ToLower();
                        break;
                    case "eancode":
                        if (a.Packages != null) key = a.Packages.ToLower();
                        break;
                    default:
                        break;
                }
                return key.Contains(query);
            }).ToList();
            UpdateSearchResults(state);

            return _foundArticles.Count;
        }

        public async Task<Article> GetArticleWithId(long? id)
        {
            Article med = new Article();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT * FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_ROWID + " LIKE '" + id + "'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                med = CursorToArticle(reader);
                            }
                        }
                    }
                }
            });

            return med;
        }

        public async Task<Article> GetArticleWithEan(string eancode)
        {
            Article med = new Article();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT * FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_PACKAGES + " LIKE '%" + eancode + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                med = CursorToArticle(reader);
                            }
                        }
                    }
                }
            });

            return med;
        }

        public async Task<Article> GetArticleWithRegnr(string regnr)
        {
            Article med = new Article();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT * FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_REGNRS + " LIKE '%, " + regnr + "%' OR "
                            + KEY_REGNRS + " LIKE '" + regnr + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                med = CursorToArticle(reader);
                            }
                        }
                    }
                }
            });

            return med;
        }

        public async Task<List<Article>> SearchTitle(string title)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        title = title
                            .Replace("a", "[aáàäâã]")
                            .Replace("e", "[eéèëê]")
                            .Replace("i", "[iíìî]")
                            .Replace("o", "[oóòöôõ]")
                            .Replace("u", "[uúùüû]");

                        if (title.Length == 0)
                        {
                            com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE;

                        } else
                        {
                            com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE lower("
                                + KEY_TITLE + ") GLOB '" + title + "*'";
                        }

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> SearchAuthor(string author)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_AUTHOR + " LIKE '" + author + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> SearchATC(string atccode)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_ATCCODE + " like '%;" + atccode + "%' OR "
                            + KEY_ATCCODE + " like '" + atccode + "%' OR "
                            + KEY_ATCCODE + " like '% " + atccode + "%' OR "
                            + KEY_ATCCLASS + " like '" + atccode + "%' OR "
                            + KEY_ATCCLASS + " like '%;" + atccode + "%' OR "
                            + KEY_ATCCLASS + " like '%#" + atccode + "%' OR "
                            + KEY_SUBSTANCES + " like '%, " + atccode + "%' OR "
                            + KEY_SUBSTANCES + " like '" + atccode + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> SearchIngredient(string ingredient)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_SUBSTANCES + " LIKE '%, " + ingredient + "%' OR "
                            + KEY_SUBSTANCES + " LIKE " + "'" + ingredient + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> SearchRegNr(string regnr)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_REGNRS + " LIKE '%, " + regnr + "%' OR "
                            + KEY_REGNRS + " LIKE '" + regnr + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> SearchListOfRegNrs(List<string> listOfRegnrs)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        const int N = 40;   // Size of chunks
                        int A = (listOfRegnrs.Count / N) * N;
                        List<string> listA = listOfRegnrs.GetRange(0, A); // First list, contains most of the articles
                        List<string> listB = listOfRegnrs.GetRange(A, listOfRegnrs.Count - A); // Second list, left overs
                        int count = 0;
                        string subQuery = "";
                        // Loop through first list
                        foreach (string reg in listA)
                        {
                            subQuery += KEY_REGNRS + " LIKE '%, " + reg + "%' OR "
                                + KEY_REGNRS + " LIKE '" + reg + "%'";
                            count++;
                            if (count % N == 0)
                            {
                                com.CommandText = "SELECT " + FT_SEARCH_TABLE + " FROM " + DATABASE_TABLE + " WHERE " + subQuery;
                                using (SQLiteDataReader reader = com.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        medTitles.Add(CursorToVeryShortArticle(reader));
                                    }
                                }
                                subQuery = "";
                            }
                            else
                            {
                                subQuery += " OR ";
                            }
                        }
                        // Loop through second list (the rest)
                        foreach (string reg in listB)
                        {
                            subQuery += KEY_REGNRS + " LIKE '%, " + reg + "%' OR "
                                + KEY_REGNRS + " LIKE '" + reg + "%' OR ";
                        }
                        string query = "";
                        if (subQuery.Length > 4)
                        {
                            query = "SELECT " + FT_SEARCH_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                                + subQuery.Substring(0, subQuery.Length - 4);
                        }

                        com.CommandText = query;
                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToVeryShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> SearchApplication(string application)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_APPLICATION + " LIKE '%," + application + "%' OR "
                            + KEY_APPLICATION + " LIKE '" + application + "%' OR "
                            + KEY_APPLICATION + " LIKE '% " + application + "%' OR "
                            + KEY_APPLICATION + " LIKE '%;" + application + "%' OR "
                            + KEY_INDICATIONS + " LIKE '" + application + "%' OR "
                            + KEY_INDICATIONS + " LIKE '%;" + application + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> SearchEanCode(string eancode)
        {
            List<Article> medTitles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        com.CommandText = "SELECT " + PACKAGES_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_PACKAGES + " LIKE '%" + eancode + "%'";

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medTitles.Add(CursorToShortArticle(reader));
                            }
                        }
                    }
                }
            });

            return medTitles;
        }

        public async Task<List<Article>> FindArticlesByEans(string[] eancodes)
        {
            List<Article> articles = new List<Article>();

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand com = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        string[] codes = new List<string>(eancodes).Where(c => !c.Equals("")).Distinct().ToArray();
                        string conditions = String.Join(" OR ", eancodes.Select((c, i) => {
                            return String.Format("({0} LIKE @t{1})", KEY_PACKAGES, i);
                        }));
                        var q = String.Format(
                            @"SELECT * FROM {0} WHERE {1} ORDER BY {2} ASC;", DATABASE_TABLE, conditions, KEY_PACKAGES);
                        Log.WriteLine("Query: {0}", q);
                        com.CommandText = q;

                        for (var i = 0; i < eancodes.Length; i++)
                        {
                            com.Parameters.AddWithValue(String.Format("@t{0}", i),
                                String.Format("%{0}%", eancodes[i]));
                        }

                        using (SQLiteDataReader reader = com.ExecuteReader())
                        {
                            while (reader.Read())
                                articles.Add(CursorToShortArticle(reader));
                        }
                    }
                }
            });
            Log.WriteLine("articles.Length: {0}", articles.Count);
            return articles;
        }
        #endregion

        #region Private Methods
        private Article CursorToVeryShortArticle(SQLiteDataReader reader)
        {
            Article article = new Article();

            article.Id = reader[KEY_ROWID] as long?;
            article.Title = reader[KEY_TITLE] as string;
            article.Author = reader[KEY_AUTHOR] as string;
            article.Regnrs = reader[KEY_REGNRS] as string;
            article.SectionIds = reader[KEY_SECTION_IDS] as string;
            article.SectionTitles = reader[KEY_SECTION_TITLES] as string;

            return article;
        }

        private Article CursorToShortArticle(SQLiteDataReader reader)
        {
            Article article = new Article();

            article.Id = reader[KEY_ROWID] as long?;
            article.Title = reader[KEY_TITLE] as string;
            article.Author = reader[KEY_AUTHOR] as string;
            article.AtcCode = reader[KEY_ATCCODE] as string;
            article.Substances = reader[KEY_SUBSTANCES] as string;
            article.Regnrs = reader[KEY_REGNRS] as string;
            article.AtcClass = reader[KEY_ATCCLASS] as string;
            article.Therapy = reader[KEY_THERAPY] as string;
            article.Application = reader[KEY_APPLICATION] as string;
            article.Indications = reader[KEY_INDICATIONS] as string;
            article.CustomerId = reader[KEY_CUSTOMER_ID] as long?;
            article.PackInfo = reader[KEY_PACK_INFO] as string;
            article.AddInfo = reader[KEY_ADD_INFO] as string;
            article.Packages = reader[KEY_PACKAGES] as string;
            article.IsFavorite = _favorites.Contains(article.Regnrs);

            return article;
        }

        private Article CursorToArticle(SQLiteDataReader reader)
        {
            Article article = new Article();

            article.Id = reader[KEY_ROWID] as long?;
            article.Title = reader[KEY_TITLE] as string;
            article.Author = reader[KEY_AUTHOR] as string;
            article.AtcCode = reader[KEY_ATCCODE] as string;
            article.Substances = reader[KEY_SUBSTANCES] as string;
            article.Regnrs = reader[KEY_REGNRS] as string;
            article.AtcClass = reader[KEY_ATCCLASS] as string;
            article.Therapy = reader[KEY_THERAPY] as string;
            article.Application = reader[KEY_APPLICATION] as string;
            article.Indications = reader[KEY_INDICATIONS] as string;
            article.CustomerId = reader[KEY_CUSTOMER_ID] as long?;
            article.PackInfo = reader[KEY_PACK_INFO] as string;
            article.AddInfo = reader[KEY_ADD_INFO] as string;
            article.SectionIds = reader[KEY_SECTION_IDS] as string;
            article.SectionTitles = reader[KEY_SECTION_TITLES] as string;
            article.Content = reader[KEY_CONTENT] as string;
            article.Packages = reader[KEY_PACKAGES] as string;
            article.IsFavorite = _favorites.Contains(article.Regnrs);

            return article;
        }
        #endregion
    }
}
