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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.Windows.Navigation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        class ExtendedDataContext
        {
            public Network Network { get; set; }
            public MainSqlDb MainSqlDb { get; set; }
        }

        UIState _uiState;
        MainSqlDb _sqlDb;
        FullTextDb _fullTextDb;
        FachInfo _fachInfo;
        FullTextSearch _fullTextSearch;
        InteractionsCart _interactions;
        StatusBarHelper _statusBarHelper;

        static bool _willNavigate = false;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize state machine
            _uiState = new UIState();
            this.SearchTextBox.DataContext = _uiState;
            // Set state machine
            _uiState.SetState(UIState.State.Compendium);
            _uiState.SetQuery(UIState.Query.Title);

            // Initialize SQLite DB
            _sqlDb = new MainSqlDb();
            _sqlDb.Init();

            // Initialize expert info browser frame
            _fachInfo = new FachInfo(this, _sqlDb);

            // Initialize Fulltext DB
            _fullTextDb = new FullTextDb();
            _fullTextDb.Init();
            _fullTextSearch = new FullTextSearch();

            // Initialize interactions cart
            _interactions = new InteractionsCart();
            _interactions.LoadFiles();

            _statusBarHelper = new StatusBarHelper();

            this.Spinner.Spin = false;

            // Set data context
            SetDataContext(UIState.State.Compendium);
        }

        public void SetState(string state)
        {
            if (state.Equals("Compendium"))
            {
                SetState(UIState.State.Compendium);
            }
            else if (state.Equals("Favorites"))
            {
                SetState(UIState.State.Favorites);
            }
            else if (state.Equals("Interactions"))
            {
                SetState(UIState.State.Interactions);
                _interactions.ShowBasket();
            }
            else if (state.Equals("FullText"))
            {
                SetState(UIState.State.FullTextSearch);
            }
        }

        public void SetState(UIState.State state)
        {
            _uiState.SetState(state);
            SetDataContext(state);

            if (state == UIState.State.Compendium)
            {
                _sqlDb.UpdateSearchResults(_uiState);
                this.Compendium.IsChecked = true;
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = false;
            }
            else if (state == UIState.State.Favorites)
            {
                _sqlDb.UpdateSearchResults(_uiState);
                this.Compendium.IsChecked = false;
                this.Favorites.IsChecked = true;
                this.Interactions.IsChecked = false;
            }
            else if (state == UIState.State.Interactions)
            {
                if (_uiState.GetQuery() == UIState.Query.Fulltext)
                    _uiState.SetQuery(UIState.Query.Title);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Compendium.IsChecked = false;
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = true;
                _interactions.ShowBasket();
            }
            else if (state == UIState.State.FullTextSearch)
            {
                this.Compendium.IsChecked = true;
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = false;
                _uiState.SetQuery(UIState.Query.Fulltext);
            }
        }

        public void SetDataContext(UIState.State state)
        {
            if (state == UIState.State.Compendium || state == UIState.State.Favorites)
            {
                this.SearchResult.DataContext = _sqlDb;
                this.Browser.DataContext = _fachInfo;
                this.SectionTitles.DataContext = _fachInfo;
                this.Browser.ObjectForScripting = _fachInfo;

            }
            else if (state == UIState.State.Interactions)
            {
                this.SearchResult.DataContext = _sqlDb;
                this.Browser.DataContext = _interactions;
                this.SectionTitles.DataContext = _interactions;
                this.Browser.ObjectForScripting = _interactions;
            }
            else if (state == UIState.State.FullTextSearch)
            {
                this.SearchResult.DataContext = _fullTextDb;
                this.Browser.DataContext = _fullTextSearch;
                this.SectionTitles.DataContext = _fullTextSearch;
                this.Browser.ObjectForScripting = _fachInfo;
            }
            this.StatusBar.DataContext = _statusBarHelper;
        }

        /**
         * Injects javascript into the current browser
         */
        public void InjectJS(string jsCode)
        {
            this.Browser.InvokeScript("execScript", new Object[] { jsCode, "JavaScript" });
        }

        public string SearchFieldText()
        {
            return this.SearchTextBox.Text;
        }

        private void SetSpinnerEnabled(bool enabled)
        {
            if (enabled)
            {
                this.Spinner.Visibility = Visibility.Visible;
                this.Spinner.Spin = true;
            }
            else
            {
                this.Spinner.Visibility = Visibility.Hidden;
                this.Spinner.Spin = false;
            }
        }

        private async void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            /*
            Uri iconUri = new Uri("./images/desitin_icon.ico", UriKind.RelativeOrAbsolute); //make sure your path is correct, and the icon set as Resource
            this.Icon = BitmapFrame.Create(iconUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            */
            _statusBarHelper.IsConnectedToInternet();
            // 
            await _sqlDb?.Search(_uiState, "");
        }

        private void WebBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            /*
            string script = "document.body.style.overflow ='hidden'";
            WebBrowser wb = (WebBrowser)sender;
            wb.InvokeScript("execScript", new Object[] { script, "JavaScript" });
            */
        }

        private async void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            // First page needs to be loaded in webBrowser control
            if (!_willNavigate)
            {
                _willNavigate = true;
                return;
            }

            await Task.Run(() =>
            {
                // Cancel navigation to the clicked link in the webBrowser control
                e.Cancel = true;
                // Open new window
                if (e.Uri != null)
                {
                    var startInfo = new ProcessStartInfo { FileName = e.Uri?.ToString() };
                    Process.Start(startInfo);
                }
                e.Cancel = false;
            });
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // ... Get control that raised this event.
            var textBox = sender as TextBox;
            // ... Change Window Title.
            string text = textBox.Text;
            if (text.Length > 0)
            {
                // Change the data context of the status bar
                Stopwatch sw = new Stopwatch();
                sw.Start();
                long numResults = 0;
                if (_uiState.IsFullTextSearch())
                    numResults = await _fullTextDb?.Search(_uiState, text);
                else
                    numResults = await _sqlDb?.Search(_uiState, text);
                sw.Stop();
                double elapsedTime = sw.ElapsedMilliseconds / 1000.0;
                _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numResults, elapsedTime));
            }
        }

        /**
         * Listens to click events in search box
         */
        private async void OnSearchTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            this.SearchTextBox.Text = "";
            // Change the data context of the status bar
            Stopwatch sw = new Stopwatch();
            long numResults = 0;
            if (_uiState.IsFullTextSearch())
                numResults = await _fullTextDb?.Search(_uiState, "");
            else
                numResults = await _sqlDb?.Search(_uiState, "");
            sw.Stop();
            double elapsedTime = sw.ElapsedMilliseconds / 1000.0;
            _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numResults, elapsedTime));
        }

        /**
         * Little hack to deselect the currently selected item in list. This is necessary
         * to circumnavigate the SelectionChanged event which is only fired when the currently
         * selected item is different from the previous one.
         */
        private void OnSearchResultChild_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(sender as ListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                item.IsSelected = false;
            }
        }

        /**
         * This event handler is called when the user selects the title in the search result
         */
        static long? _searchSelectionItemId = 0;
        static string _searchSelectionItemHash = "";
        private async void OnSearchItem_Selection(object sender, SelectionChangedEventArgs e)
        {
            ListBox searchResultList = sender as ListBox;
            if (searchResultList?.Items.Count > 0)
            {
                object selectedItem = searchResultList.SelectedItem;
                if (selectedItem?.GetType() == typeof(Item))
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    int numResults = 0;

                    SetSpinnerEnabled(true);

                    Item selection = selectedItem as Item;
                    if (_uiState.IsFullTextSearch())
                    {
                        if (_searchSelectionItemHash != selection.Hash)
                        {
                            _searchSelectionItemHash = selection.Hash;
                            FullTextEntry entry = await _fullTextDb.GetEntryWithHash(_searchSelectionItemHash);
                            List<Article> listOfArticles = await _sqlDb.SearchListOfRegNrs(entry.GetRegnrsAsList());
                            if (listOfArticles != null)
                            {
                                _fullTextSearch.Filter = "";
                                _fullTextSearch.ShowTableWithArticles(listOfArticles, entry.RegChaptersDict);
                                numResults = listOfArticles.Count;
                            }
                        }
                    }
                    else
                    {
                        if (_searchSelectionItemId != selection.Id)
                        {
                            _searchSelectionItemId = selection.Id;
                            if (_uiState.IsCompendium() || _uiState.IsFavorites())
                            {
                                Article a = await _sqlDb.GetArticleFromId(_searchSelectionItemId);
                                _fachInfo.ShowFull(a);   // Load html in browser window
                            }
                            else if (_uiState.IsInteractions())
                            {
                                Article a = await _sqlDb.GetArticleWithId(_searchSelectionItemId);
                                if (a?.Id != null)
                                {
                                    _interactions.AddArticle(a);
                                    _interactions.ShowBasket();
                                }
                            }
                        }
                    }

                    SetSpinnerEnabled(false);

                    sw.Stop();
                    double elapsedTime = sw.ElapsedMilliseconds / 1000.0;
                    if (numResults > 0)
                        _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numResults, elapsedTime));
                    // Console.WriteLine("Item " + _searchSelectionItemId + " -> " + sw.ElapsedMilliseconds + "ms");
                }
            }
        }

        /**
         * This event handler is called when the user selects a package
         */
        static long? _searchSelectionChildItemId = 0;
        private async void OnSearchChildItem_Selection(object sender, SelectionChangedEventArgs e)
        {
            ListBox searchResultList = sender as ListBox;
            if (searchResultList?.Items.Count > 0)
            {
                object selectedItem = searchResultList.SelectedItem;
                if (selectedItem?.GetType() == typeof(ChildItem))
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    SetSpinnerEnabled(true);

                    ChildItem selection = selectedItem as ChildItem;
                    if (_searchSelectionChildItemId != selection.Id)
                    {
                        _searchSelectionChildItemId = selection.Id;
                        if (_searchSelectionChildItemId != null)
                        {
                            if (_uiState.IsCompendium() || _uiState.IsFavorites())
                            {
                                Article a = await _sqlDb.GetArticleFromId(_searchSelectionChildItemId);
                                _fachInfo.ShowFull(a);   // Load html in browser window
                            }
                        }
                    }

                    SetSpinnerEnabled(false);

                    sw.Stop();
                    // Console.WriteLine("ChildItem " + _searchSelectionChildItemId + "/" + selection.Id + " -> " + sw.ElapsedMilliseconds + "ms");
                }
            }
        }

        /**
        * Event handler called when the user selects a section title, injects javascript into Browser window
        */
        private void OnSectionTitle_Selection(object sender, SelectionChangedEventArgs e)
        {
            ListBox searchTitlesList = sender as ListBox;
            if (searchTitlesList?.Items.Count > 0)
            {
                TitleItem sectionTitle = searchTitlesList.SelectedItem as TitleItem;               
                if (sectionTitle!=null && sectionTitle.Id != null)
                {
                    if (!_uiState.IsFullTextSearch())
                    {
                        // Inject javascript to move to anchor
                        string jsCode = "document.getElementById('" + sectionTitle.Id + "').scrollIntoView(true);";
                        InjectJS(jsCode);
                    }
                    else
                    {
                        // Set filter
                        _fullTextSearch.Filter = sectionTitle.Id;
                        // Update result table
                        _fullTextSearch.UpdateTable();
                    }
                }
            }
        }

        private async void FavoriteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox favoriteCheckBox = sender as CheckBox;

            object fav = favoriteCheckBox.DataContext;
            if (fav?.GetType() == typeof(Item))
            {
                Item item = fav as Item;
                if (item.Id != null)
                {
                    Article article = await _sqlDb.GetArticleWithId(item.Id);
                    _sqlDb.UpdateFavorites(article);
                }
            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            var name = source.Name;
            if (name.Equals("Update"))
            {
                _sqlDb.Close();
                _fullTextDb.Close();
                ProgressDialog progressDialog = new ProgressDialog();
                progressDialog.UpdateDbAsync();
                progressDialog.ShowDialog();
                // Re-init db
                 _sqlDb.Init();
                _fullTextDb.Init();
            }
            else if (name.Equals("Report"))
            {
                this.Browser.DataContext = _fachInfo;
                await _fachInfo.ShowReport();
            }
            else if (name.Equals("Settings"))
            {
                // TODO
            }
            else if (name.Equals("Feedback"))
            {
                var url = "mailto:zdavatz@ywesee.com?subject=AmiKo%20Desitin%20Feedback";
                Process.Start(url);
            }
            else if (name.Equals("About"))
            {
                AboutDialog aboutDialog = new AboutDialog();
                aboutDialog.ShowDialog();
            }
        }

        private void StateButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            SetState(source.Name);
        }

        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            if (_uiState.IsFullTextSearch())
                SetState(UIState.State.Compendium);

            if (source.Name.Equals("Title"))
                _uiState.SetQuery(UIState.Query.Title);
            else if (source.Name.Equals("Author"))
                _uiState.SetQuery(UIState.Query.Author);
            else if (source.Name.Equals("AtcCode"))
                _uiState.SetQuery(UIState.Query.AtcCode);
            else if (source.Name.Equals("RegNr"))
                _uiState.SetQuery(UIState.Query.Regnr);
            else if (source.Name.Equals("Application"))
                _uiState.SetQuery(UIState.Query.Application);
            else if (source.Name.Equals("Fulltext"))
            {
                SetState(UIState.State.FullTextSearch);
            }

            _sqlDb.UpdateSearchResults(_uiState);
        }
    }

    /// <summary>
    /// Register dependency property for browser
    /// </summary>
    public static class BrowserBehavior
    {
        public static readonly DependencyProperty HtmlProperty =
            DependencyProperty.RegisterAttached("Html", typeof(string), typeof(BrowserBehavior), new PropertyMetadata(HtmlPropertyChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser browser)
        {
            return (string)browser.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser browser, string value)
        {
            browser.SetValue(HtmlProperty, value);
        }

        static void HtmlPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var browser = dependencyObject as WebBrowser;
            if (browser != null && e.NewValue != null)
            {
                browser.NavigateToString((string)e.NewValue);
            }
        }
    }
}
