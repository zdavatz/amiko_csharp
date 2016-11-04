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
        FachInfo _fachInfo;
        InteractionsCart _interactions;
        StatusBarHelper _statusBarHelper;

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
            this.SearchResult.DataContext = _sqlDb;

            // Initialize expert info browser frame
            _fachInfo = new FachInfo();
            this.Browser.DataContext = _fachInfo;
            this.SectionTitles.DataContext = _fachInfo;

            // Initialize interactions cart
            _interactions = new InteractionsCart();
            this.Browser.ObjectForScripting = _interactions;
            _interactions.LoadFiles();

            _statusBarHelper = new StatusBarHelper();
            this.StatusBar.DataContext = _statusBarHelper;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            /*
            Uri iconUri = new Uri("./images/desitin_icon.ico", UriKind.RelativeOrAbsolute); //make sure your path is correct, and the icon set as Resource
            this.Icon = BitmapFrame.Create(iconUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            */
            _statusBarHelper.IsConnectedToInternet();
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
                long numArticles = await _sqlDb?.Search(_uiState, text);
                sw.Stop();
                double elapsedTime = sw.ElapsedMilliseconds / 1000.0;

                _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numArticles, elapsedTime));
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
            sw.Start();
            long numArticles = await _sqlDb?.Search(_uiState, "");
            sw.Stop();
            double elapsedTime = sw.ElapsedMilliseconds / 1000.0;
            _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numArticles, elapsedTime));
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

                    Item selection = selectedItem as Item;
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
                            _interactions.AddArticle(a);
                            _interactions.ShowBasket();
                        }
                    }

                    sw.Stop();
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
                // Inject javascript to move to anchor
                if (sectionTitle.Id != null)
                {
                    var jsCode = "document.getElementById('" + sectionTitle.Id + "').scrollIntoView(true);";
                    this.Browser.InvokeScript("execScript", new Object[] { jsCode, "JavaScript" });
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
                Article article = await _sqlDb.GetArticleWithId(item.Id);
                _sqlDb.UpdateFavorites(article);
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
                ProgressDialog progressDialog = new ProgressDialog();
                progressDialog.UpdateDbAsync();
                progressDialog.ShowDialog();
                // Re-init db
                 _sqlDb.Init();
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

            if (source.Name.Equals("Compendium"))
            {
                _uiState.SetState(UIState.State.Compendium);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Browser.DataContext = _fachInfo;
                this.SectionTitles.DataContext = _fachInfo;
                this.Compendium.IsChecked = true;
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = false;
            }
            else if (source.Name.Equals("Favorites"))
            {
                _uiState.SetState(UIState.State.Favorites);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Browser.DataContext = _fachInfo;
                this.SectionTitles.DataContext = _fachInfo;
                this.Compendium.IsChecked = false;
                this.Favorites.IsChecked = true;
                this.Interactions.IsChecked = false;
            }
            else if (source.Name.Equals("Interactions"))
            {
                _uiState.SetState(UIState.State.Interactions);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Browser.DataContext = _interactions;
                this.SectionTitles.DataContext = _interactions;
                this.Compendium.IsChecked = false;
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = true;
                _interactions.ShowBasket();
            }
        }

        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            this.SearchTextBox.DataContext = _uiState;

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

            _sqlDb.UpdateSearchResults(_uiState);
        }
    }

    /// <summary>
    /// Register depency property for browser
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
