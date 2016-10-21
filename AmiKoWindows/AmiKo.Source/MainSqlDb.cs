/*
Copyright (c) 2016 Max Lungarella <cybrmx@gmail.com>

This file is part of AmiKoWindows.

AmiKoDesk is free software: you can redistribute it and/or modify
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class ChildItem
    {
        // Properties, must be public (these are not fields!)
        public long? Id { get; set; }
        public string Ean { get; set; }
        public string Text { get; set; }
        public string Color { get; set; }
        public string Decoration { get; set; }
    }

    class Item
    {
        // Properties, must be public (these are not fields!)
        public long? Id { get; set; }
        public string Text { get; set; }
        public bool IsFavorite { get; set; }
        public ChildItemsObservableCollection ChildItems { get; set; }
    }

    class TitleItem
    {
        // Properties, must be public (these are not fields!)
        public string Id { get; set; }
        public string Title { get; set; }
        public string Color { get; set; }
    }

    class ChildItemsObservableCollection : ObservableCollection<ChildItem>
    {
        private bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(long? id, string author, IEnumerable<string> packInfoList, IEnumerable<string> packagesList)
        {
            if (packInfoList == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;
            using (var e1 = packInfoList.GetEnumerator())
            using (var e2 = packagesList.GetEnumerator())
            {
                while (e1.MoveNext() && e2.MoveNext())
                {
                    // Extract package info and set color
                    string item = e1.Current;
                    string color = Colors.SearchBoxChildItems;
                    if (item.Contains(", O"))
                        color = Colors.Originals;
                    else if (item.Contains(", G"))
                        color = Colors.Generics;

                    // Extract Eancode
                    string[] packages = e2.Current.Split('|');
                    string ean = "";
                    if (packages.Length > 9)
                        ean = packages[9];

                    string decoration = (author.Contains("ibsa") || author.Contains("desitin")) ? "Underline" : "";

                    if (item != string.Empty)
                    {
                        Add(new ChildItem()
                        {
                            Id = id,
                            Ean = ean,
                            Text = item,
                            Color = color,
                            Decoration = decoration
                        });
                    }
                }
            }

            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(IEnumerable<string> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;
            foreach (string str in list)
            {
                if (str != string.Empty)
                {
                    Add(new ChildItem()
                    {
                        Text = str,
                        Color = Colors.SearchBoxChildItems
                    });
                }
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    class ItemsObservableCollection : ObservableCollection<Item>
    {
        private bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(UIState uiState, IEnumerable<Article> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;
            string type = uiState.SearchQueryType();
            UIState.State state = uiState.GetState();

            if (type.Equals("title"))
            {
                foreach (Article article in list)
                {
                    bool cond = (state == UIState.State.Compendium) || (state == UIState.State.Favorites && article.IsFavorite);
                    if (cond)
                    {
                        List<string> packInfoList = article.PackInfo.Split('\n').ToList();
                        List<string> packagesList = article.Packages.Split('\n').ToList();
                        ChildItemsObservableCollection ci = new ChildItemsObservableCollection();
                        ci.AddRange(article.Id, article.Author.ToLower(), packInfoList, packagesList);

                        Add(new Item()
                        {
                            Id = article.Id,
                            Text = article.Title,
                            IsFavorite = article.IsFavorite,
                            ChildItems = ci
                        });
                    }
                }
            }
            else if (type.Equals("author"))
            {
                foreach (Article article in list)
                {
                    bool cond = (state == UIState.State.Compendium) || (state == UIState.State.Favorites && article.IsFavorite);
                    if (cond)
                    {
                        Add(new Item()
                        {
                            Id = article.Id,
                            Text = article.Title,
                            IsFavorite = article.IsFavorite,
                            ChildItems = new ChildItemsObservableCollection
                            {
                                new ChildItem() { Text = article.Author, Color=Colors.SearchBoxChildItems }
                            }
                        });
                    }
                }
            }
            else if (type.Equals("atc"))
            {
                foreach (Article article in list)
                {
                    bool cond = (state == UIState.State.Compendium) || (state == UIState.State.Favorites && article.IsFavorite);
                    if (cond)
                    {
                        ChildItemsObservableCollection ci = new ChildItemsObservableCollection();
                        // ATC code + ATC name
                        string atcCode = article?.AtcCode.Replace(";", " - ");
                        // ATC class
                        string[] atc1 = article?.AtcClass?.Split(';');
                        string atcClass = "";
                        if (atc1!=null)
                            atcClass = (atc1.Length > 1) ? atc1[1] : "";
                        // ATC Subgroup
                        string atcSubClass = (atcClass.Length > 2) ? atc1[2] : "";
                        string[] atc2 = atcSubClass?.Split('#');
                        string subGroup = (atc2.Length > 1) ? atc2[1] : "";

                        List<string> listOfAtcInfo = new List<string> { atcCode, subGroup, atcClass };
                        ci.AddRange(listOfAtcInfo);

                        Add(new Item()
                        {
                            Id = article.Id,
                            Text = article.Title,
                            IsFavorite = article.IsFavorite,
                            ChildItems = ci
                        });
                    }
                }
            }
            else if (type.Equals("regnr"))
            {
                foreach (Article article in list)
                {
                    bool cond = (state == UIState.State.Compendium) || (state == UIState.State.Favorites && article.IsFavorite);
                    if (cond)
                    {
                        Add(new Item()
                        {
                            Id = article.Id,
                            Text = article.Title,
                            IsFavorite = article.IsFavorite,
                            ChildItems = new ChildItemsObservableCollection
                            {
                                new ChildItem() { Text = article.Regnrs, Color=Colors.SearchBoxChildItems }
                            }
                        });
                    }
                }
            }
            else if (type.Equals("application"))
            {
                foreach (Article article in list)
                {
                    bool cond = (state == UIState.State.Compendium) || (state == UIState.State.Favorites && article.IsFavorite);
                    if (cond)
                    {
                        ChildItemsObservableCollection ci = new ChildItemsObservableCollection();
                        List<string> listOfApplications = article?.Application.Split(';').ToList();
                        ci.AddRange(listOfApplications);

                        Add(new Item()
                        {
                            Id = article.Id,
                            Text = article.Title,
                            IsFavorite = article.IsFavorite,
                            ChildItems = ci
                        });
                    }
                }
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(IEnumerable<string> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;
            foreach (string str in list)
            {
                Add(new Item()
                {
                    Text = str
                });
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    class TitlesObservableCollection : ObservableCollection<TitleItem>
    {
        private bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<TitleItem> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;
            foreach (TitleItem item in list)
            {
                Add(new TitleItem()
                {
                    Id = item.Id,
                    Title = item.Title,
                    Color = Colors.SectionTitles
                });
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    class MainSqlDb : INotifyPropertyChanged
    {
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

        /**
         * Table columns used for fast queries
         */
        static readonly string SHORT_TABLE = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}",
            KEY_ROWID, KEY_TITLE, KEY_AUTHOR, KEY_ATCCODE, KEY_SUBSTANCES, KEY_REGNRS, KEY_ATCCLASS, KEY_THERAPY,
            KEY_APPLICATION, KEY_INDICATIONS, KEY_CUSTOMER_ID, KEY_PACK_INFO, KEY_ADD_INFO, KEY_PACKAGES);

        static readonly string PACKAGES_TABLE = String.Format("{0},{1},{2},{3},{4}",
            KEY_ROWID, KEY_TITLE, KEY_AUTHOR, KEY_REGNRS, KEY_PACKAGES);

        private SQLiteConnection _db;
        private List<Article> _foundArticles = new List<Article>();
        private Favorites _favorites = new Favorites();

        /**
         * Properties
         */
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

        private string _statusBarText;
        public string StatusBarText
        {
            get { return _statusBarText; }
            set
            {
                if (value != _statusBarText)
                {
                    _statusBarText = value;
                    NotifyPropertyChanged();
                    // OnPropertyChanged("StatusBarText");
                }
            }
        }

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

        private TitlesObservableCollection _sectionTitles = new TitlesObservableCollection();
        public TitlesObservableCollection SectionTitles
        {
            get { return _sectionTitles; }
            private set
            {
                if (value != _sectionTitles)
                {
                    _sectionTitles = value;
                    // OnPropertyChanged is not necessary here...
                }
            }
        }

        /**
         * Private functions
         */
        private async Task ConnectToDB(string db_path)
        {
            if (_db != null)
                return;

            await Task.Run(() =>
            {
                if (File.Exists(db_path))
                {
                    _db = new SQLiteConnection("Data Source=" + db_path);
                    _db.Open();
                }
            });
        }

        private void CloseDB()
        {
            _db.Close();
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

        /**
         * Public functions
         */
        public async void UpdateFavorites(Article article)
        {
            if (_favorites.Contains(article?.Regnrs))
                _favorites.Remove(article);
            else
                _favorites.Add(article);
            // Save list of favorites to file
            await _favorites.Save();
        }

        public async Task<string> GetFachInfoFromId(long? id)
        {
            Article article = await GetArticleWithId(id);

            // List of section titles
            SectionTitles.Clear();
            List<TitleItem> listOfSectionTitles = article.ListOfSectionTitleItems();
            SectionTitles.AddRange(listOfSectionTitles);

            return article.Content;
        }

        public async Task<string> GetFachInfoFromEan(string ean)
        {
            Article article = await GetArticleWithEan(ean);

            SectionTitles.Clear();
            List<TitleItem> listOfSectionTitles = article.ListOfSectionTitleItems();
            SectionTitles.AddRange(listOfSectionTitles);

            return article.Content;
        }

        public async void StartSQLite()
        {
            string app_folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string db_path = app_folder + @"\dbs\amiko_db_full_idx_de.db";
            await ConnectToDB(db_path);
        }

        public async void Search(UIState state, string query)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _foundArticles.Clear();
            string type = state.SearchQueryType();
            switch (type)
            {
                case "title":
                    _foundArticles = await SearchTitle(query);
                    break;
                case "author":
                    _foundArticles = await SearchAuthor(query);
                    break;
                case "atc":
                    _foundArticles = await SearchATC(query);
                    break;
                case "ingredient":
                    _foundArticles = await SearchIngredient(query);
                    break;
                case "regnr":
                    _foundArticles = await SearchRegNr(query);
                    break;
                case "eancode":
                    _foundArticles = await SearchEanCode(query);
                    break;
                case "application":
                    _foundArticles = await SearchApplication(query);
                    break;
                default:
                    break;
            }
            UpdateSearchResults(state);
            sw.Stop();

            int count = _foundArticles.Count;
            StatusBarText = string.Format("{0} Suchresultate in {1} Sekunden", count, sw.ElapsedMilliseconds / 1000.0);
        }

        public void UpdateSearchResults(UIState state)
        {
            SearchResultItems.Clear();
            SearchResultItems.AddRange(state, _foundArticles);
        }

        public async Task<Article> GetArticleWithId(long? id)
        {
            Article med = new Article();

            await Task.Run(() =>
            {
                if (_db.State != System.Data.ConnectionState.Open)
                    _db.Open();

                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    com.CommandText = "SELECT * FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_ROWID + " LIKE " + "'" + id + "'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            med = CursorToArticle(reader);
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
                if (_db.State != System.Data.ConnectionState.Open)
                    _db.Open();

                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    com.CommandText = "SELECT * FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_PACKAGES + " LIKE " + "'%" + eancode + "%'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            med = CursorToArticle(reader);
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
                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    if (_db.State != System.Data.ConnectionState.Open)
                        _db.Open();

                    if (title.Length > 2)
                    {
                        com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_TITLE + " LIKE " + "'" + title + "%' OR "
                            + KEY_TITLE + " LIKE " + "'%" + title + "%'";
                    }
                    else
                    {
                        com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                            + KEY_TITLE + " LIKE " + "'" + title + "%'";
                    }

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medTitles.Add(CursorToShortArticle(reader));
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
                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    if (_db.State != System.Data.ConnectionState.Open)
                        _db.Open();

                    com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_AUTHOR + " LIKE " + "'" + author + "%'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medTitles.Add(CursorToShortArticle(reader));
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
                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    if (_db.State != System.Data.ConnectionState.Open)
                        _db.Open();

                    com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_ATCCODE + " like " + "'%;" + atccode + "%' or "
                        + KEY_ATCCODE + " like " + "'" + atccode + "%' or "
                        + KEY_ATCCODE + " like " + "'% " + atccode + "%' or "
                        + KEY_ATCCLASS + " like " + "'" + atccode + "%' or "
                        + KEY_ATCCLASS + " like " + "'%;" + atccode + "%' or "
                        + KEY_ATCCLASS + " like " + "'%#" + atccode + "%' or "
                        + KEY_SUBSTANCES + " like " + "'%, " + atccode + "%' or "
                        + KEY_SUBSTANCES + " like " + "'" + atccode + "%'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medTitles.Add(CursorToShortArticle(reader));
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
                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    if (_db.State != System.Data.ConnectionState.Open)
                        _db.Open();

                    com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_SUBSTANCES + " LIKE " + "'%, " + ingredient + "%' or "
                        + KEY_SUBSTANCES + " LIKE " + "'" + ingredient + "%'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medTitles.Add(CursorToShortArticle(reader));
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
                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    if (_db.State != System.Data.ConnectionState.Open)
                        _db.Open();

                    com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_REGNRS + " LIKE " + "'%, " + regnr + "%' or "
                        + KEY_REGNRS + " LIKE " + "'" + regnr + "%'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medTitles.Add(CursorToShortArticle(reader));
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
                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    if (_db.State != System.Data.ConnectionState.Open)
                        _db.Open();

                    com.CommandText = "SELECT " + SHORT_TABLE + " FROM " + DATABASE_TABLE + " WHERE " 
                        + KEY_APPLICATION + " LIKE " + "'%," + application + "%' OR " 
                        + KEY_APPLICATION + " LIKE " + "'" + application + "%' OR " 
                        + KEY_APPLICATION + " LIKE " + "'% " + application + "%' OR " 
                        + KEY_APPLICATION + " LIKE " + "'%;" + application + "%' OR "
                        + KEY_INDICATIONS + " LIKE " + "'" + application + "%' OR "
                        + KEY_INDICATIONS + " LIKE " + "'%;" + application + "%'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medTitles.Add(CursorToShortArticle(reader));
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
                using (SQLiteCommand com = new SQLiteCommand(_db))
                {
                    if (_db.State != System.Data.ConnectionState.Open)
                        _db.Open();

                    com.CommandText = "SELECT " + PACKAGES_TABLE + " FROM " + DATABASE_TABLE + " WHERE "
                        + KEY_PACKAGES + " LIKE " + "'%" + eancode + "%'";

                    using (SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medTitles.Add(CursorToShortArticle(reader));
                        }
                    }
                }
            });

            return medTitles;
        }
    }
}
