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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace AmiKoWindows
{
    public class ChildItem
    {
        // Properties, must be public (these are not fields!)
        public long? Id { get; set; }
        public string Ean { get; set; }
        public string Text { get; set; }
        public string Color { get; set; }
        public string Decoration { get; set; }
    }

    public class Item
    {
        // Properties, must be public (these are not fields!)
        public long? Id { get; set; }
        public string Hash { get; set; }
        public string Text { get; set; }
        public bool IsFavorite { get; set; }
        public ChildItemsObservableCollection ChildItems { get; set; }
    }

    public class TitleItem
    {
        // Properties, must be public (these are not fields!)
        public string Id { get; set; }
        public string Title { get; set; }
        public string Color { get; set; }
    }

    public class ChildItemsObservableCollection : ObservableCollection<ChildItem>
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

                    // author.Contains("ibsa")
                    string decoration = author.Contains("desitin") ? "Underline" : "";

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
    
    public class ItemsObservableCollection : ObservableCollection<Item>
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

            if (type.Equals("title"))
            {
                foreach (Article article in list)
                {
                    if (uiState.IsCompendium() || (uiState.IsFavorites() && article.IsFavorite))
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
                    else if (uiState.IsInteractions())
                    {
                        // Calculate number of child items (number of packages)
                        var packagesList = article.Packages.Split('\n');
                        int numPacks = packagesList.Length - 1;
                        string numPackagesStr = "1 Packung";
                        if (numPacks==0 || numPacks>1)
                            numPackagesStr = string.Format("{0} Packungen", numPacks);
                        ChildItemsObservableCollection ci = new ChildItemsObservableCollection();

                        Add(new Item()
                        {
                            Id = article.Id,
                            Text = article.Title,
                            IsFavorite = article.IsFavorite,
                            ChildItems = new ChildItemsObservableCollection
                            {
                                new ChildItem() { Text = numPackagesStr, Color=Colors.SearchBoxChildItems }
                            }
                        });
                    }
                }
            }
            else if (type.Equals("author"))
            {
                foreach (Article article in list)
                {
                    bool cond = uiState.IsCompendium() || uiState.IsInteractions() || (uiState.IsFavorites() && article.IsFavorite);
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
                    if (uiState.IsCompendium() || (uiState.IsFavorites() && article.IsFavorite))
                    {
                        ChildItemsObservableCollection ci = new ChildItemsObservableCollection();
                        // ATC code + ATC name
                        string atcCode = article?.AtcCode.Replace(";", " - ");
                        // ATC class
                        string[] atc1 = article?.AtcClass?.Split(';');
                        string atcClass = "";
                        if (atc1 != null)
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
                    else if (uiState.IsInteractions())
                    {
                        ChildItemsObservableCollection ci = new ChildItemsObservableCollection();
                        // ATC code + ATC name
                        string atcCode = article?.AtcCode.Replace(";", " - ");

                        List<string> listOfAtcInfo = new List<string> { atcCode, "", "" };
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
                    bool cond = uiState.IsCompendium() || uiState.IsInteractions() || (uiState.IsFavorites() && article.IsFavorite);
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
                    bool cond = uiState.IsCompendium() || uiState.IsInteractions() || (uiState.IsFavorites() && article.IsFavorite);
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

        public void AddRange(UIState uiState, IEnumerable<FullTextEntry> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;
            string type = uiState.SearchQueryType();

            if (type.Equals("fulltext"))
            {
                foreach (FullTextEntry entry in list)
                {
                    bool cond = uiState.IsFullTextSearch();
                    if (cond)
                    {
                        Add(new Item()
                        {
                            Hash = entry.Hash,
                            Text = entry.GetKeywordPlus(),
                            IsFavorite = entry.IsFavorite
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

    public class TitlesObservableCollection : ObservableCollection<TitleItem>
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
}
