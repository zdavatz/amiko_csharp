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

using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AmiKoWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        UIState _uiState;
        MainSqlDb _sqlDb;
        FachInfo _fachInfo;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize state machine
            _uiState = new UIState();
            // Set data context
            this.SearchTextBox.DataContext = _uiState;
            // Set state machine
            _uiState.SetState(UIState.State.Compendium);
            _uiState.SetQuery(UIState.Query.Title);

            // Initialize SQLite DB
            _sqlDb = new MainSqlDb();
            _sqlDb.StartSQLite();
            // Set data context
            this.SearchResult.DataContext = _sqlDb;
            this.SectionTitles.DataContext = _sqlDb;

            // Initialize expert info browser frame
            _fachInfo = new FachInfo();
            // Set data context
            this.Browser.DataContext = _fachInfo;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            /*
            Uri iconUri = new Uri("./images/desitin_icon.ico", UriKind.RelativeOrAbsolute); //make sure your path is correct, and the icon set as Resource
            this.Icon = BitmapFrame.Create(iconUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            */
        }

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // ... Get control that raised this event.
            var textBox = sender as TextBox;
            // ... Change Window Title.
            string text = textBox.Text;
            if (text.Length > 0)
            {
                // Change the data context of the status bar
                this.StatusBar.DataContext = _sqlDb;
                string searchQueryType = _uiState.SearchQueryType();
                _sqlDb?.Search(searchQueryType, text);
            }
        }

        /**
         * Little hack to deselect the currently selected item in list. This is necessary
         * to circumnavigate the SelectionChanged event which is only fired when the currently
         * selected item is different from the previous one.
         */
        private void OnSearchResultChildPreviewMouseDown(object sender, RoutedEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(sender as ListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                item.IsSelected = false;
            }
        }

        /**
         * This event handler is called when the user selects the title in the search result.
         */
        static long? _searchSelectionItemId = 0;
        private async void OnSearchItemSelection(object sender, SelectionChangedEventArgs e)
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
                        string html = await _sqlDb.GetFachInfoFromId(_searchSelectionItemId);
                        // Load html in browser window
                        _fachInfo.ShowHtml(html);
                    }

                    sw.Stop();
                    Console.WriteLine("Item " + _searchSelectionItemId + " -> " + sw.ElapsedMilliseconds + "ms");
                }
            }
        }

        /**
         * This event handler is called when the user selects a package
         */
        static long? _searchSelectionChildItemId = 0;
        private async void OnSearchChildItemSelection(object sender, SelectionChangedEventArgs e)
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
                        string html = await _sqlDb.GetFachInfoFromId(_searchSelectionChildItemId);  
                        // Load html in browser window
                        _fachInfo.ShowHtml(html);
                    }

                    sw.Stop();
                    Console.WriteLine("ChildItem " + _searchSelectionChildItemId + "/" + selection.Id + " -> " + sw.ElapsedMilliseconds + "ms");
                }
            }
        }

        private void StateButtonClick(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            if (source.Equals("Compendium"))
            {
                _uiState.SetState(UIState.State.Compendium);
            }
            else if (source.Equals("Favorites"))
            {
                _uiState.SetState(UIState.State.Favorites);
            }
            else if (source.Equals("Interactions"))
            {
                _uiState.SetState(UIState.State.Interactions);
            }
        }

        private void QueryButtonClick(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            if (source.Name.Equals("Title"))
            {
                this.SearchTextBox.DataContext = _uiState;
                _uiState.SetQuery(UIState.Query.Title);
                _sqlDb.Sort("title");
            }
            else if (source.Name.Equals("Author"))
            {
                this.SearchTextBox.DataContext = _uiState;
                _uiState.SetQuery(UIState.Query.Author);
                _sqlDb.Sort("author");
            }
            else if (source.Name.Equals("AtcCode"))
            {
                this.SearchTextBox.DataContext = _uiState;
                _uiState.SetQuery(UIState.Query.AtcCode);
                _sqlDb.Sort("atc");
            }
            else if (source.Name.Equals("RegNr"))
            {
                this.SearchTextBox.DataContext = _uiState;
                _uiState.SetQuery(UIState.Query.Regnr);
                _sqlDb.Sort("regnr");
            }
            else if (source.Name.Equals("Application"))
            {
                this.SearchTextBox.DataContext = _uiState;
                _uiState.SetQuery(UIState.Query.Application);
                _sqlDb.Sort("application");
            }
            else if (source.Name.Equals("Update"))
            {
                UpdateDb update = new UpdateDb();
                // Changed data context
                // this.ProgressBar.DataContext = update;
                update.doIt();
            }
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
