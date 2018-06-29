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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Win32;

using MahApps.Metro.Controls;

namespace AmiKoWindows
{
    using ControlExtensions;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        class ExtendedDataContext
        {
            public Network Network { get; set; }
            public MainSqlDb MainSqlDb { get; set; }
        }

        static bool _willNavigate = false;

        UIState _uiState;
        MainSqlDb _sqlDb;
        FullTextDb _fullTextDb;
        FachInfo _fachInfo;
        FullTextSearch _fullTextSearch;
        InteractionsCart _interactions;
        PrescriptionsBox _prescriptions;
        StatusBarHelper _statusBarHelper;
        string _selectedFullTextSearchKey;

        FrameworkElement _browser;
        FrameworkElement _manager;

        private ContextMenu _searchResultContextMenu = null;

        #region Public Fields
        private string _SearchTextBoxWaterMark;
        public string SearchTextBoxWaterMark
        {
            get { return _SearchTextBoxWaterMark; }
            set {
                _SearchTextBoxWaterMark = value;
                OnPropertyChanged("SearchTextBoxWaterMark");
            }
        }

        private Contact _ActiveContact;
        public Contact ActiveContact
        {
            get { return _ActiveContact; }
            set
            {
                _ActiveContact = value;
                OnPropertyChanged("ActiveContact");
            }
        }

        private Account _ActiveAccount;
        public Account ActiveAccount
        {
            get { return _ActiveAccount; }
            set
            {
                _ActiveAccount = value;
                OnPropertyChanged("ActiveAccount");
            }
        }
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Initialize state machine
            _uiState = new UIState();
            this.SearchTextBox.DataContext = _uiState;
            // Set state machine
            _uiState.SetState(UIState.State.Compendium);
            _uiState.SetQuery(UIState.Query.Title);
            this.SearchTextBoxWaterMark = _uiState.SearchTextBoxWaterMark;

            // Initialize Main SQLite DB
            _sqlDb = new MainSqlDb();
            _sqlDb.Init();

            // Initialize Fulltext DB
            _fullTextDb = new FullTextDb();
            _fullTextDb.Init();
            _fullTextSearch = new FullTextSearch();

            // Initialize expert info browser frame
            _fachInfo = new FachInfo(this, _sqlDb);

            // Initialize interactions cart
            _interactions = new InteractionsCart();
            _interactions.LoadFiles();

            // Initialize prescriptions container
            _prescriptions = new PrescriptionsBox();
            _prescriptions.LoadFiles();

            _statusBarHelper = new StatusBarHelper();

            this.Spinner.Spin = false;

            // Set initial state
            SetState(UIState.State.Compendium);

            // Set browser emulation mode. Thx Microsoft for these stupid hacks!!
            SetBrowserEmulationMode();
        }

        // Returns an element in main area after datatemplate is switched by trigger
        private FrameworkElement GetElementIn(string elementName, ContentControl area)
        {
            if (area == null || !(area is ContentControl))
                return null;

            FrameworkElement element = null;
            int n = VisualTreeHelper.GetChildrenCount(area);
            if (n == 1)
            {
                ContentPresenter presenter = VisualTreeHelper.GetChild(area, 0) as ContentPresenter;
                // Presenter's template is not applied yet, whyyyy :'(
                // https://stackoverflow.com/a/15467687
                presenter.ApplyTemplate();
                element = presenter.ContentTemplate.FindName(elementName, presenter) as FrameworkElement;
            }
            return element;
        }

        public void SwitchViewContext()
        {
            var viewType = this.DataContext as ViewType;
            if (viewType.Mode.Equals("Form"))
            {
                if (_browser != null)
                {
                    _browser.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                    _browser = null;
                }
                if (_manager == null)
                    _manager = GetElementIn("Manager", MainArea);
            } else { // Html
                if (_manager != null)
                {
                    _manager.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                    _manager = null;
                }
                if (_browser == null)
                    _browser = GetElementIn("Browser", MainArea);
            }
        }

        public FrameworkElement GetView()
        {
            FrameworkElement element = null;

            var viewType = this.DataContext as ViewType;
            if (viewType.Mode.Equals("Form"))
                element = _manager;
            else
                element = _browser;

            return element;
        }

        public async void SetState(string state)
        {
            // TODO: Fix Search result items after state changed with query Volltext -> Volltext
            if (_uiState.GetState() == UIState.State.Favorites)
                _fullTextDb.ClearFoundEntries();

            if (state.Equals("Compendium"))
                SetState(UIState.State.Compendium);
            else if (state.Equals("Favorites"))
                SetState(UIState.State.Favorites);
            else if (state.Equals("Interactions"))
                SetState(UIState.State.Interactions);
            else if (state.Equals("Prescriptions"))
                SetState(UIState.State.Prescriptions);

            if (_uiState.FullTextQueryEnabled)
                if (state.Equals("Farovites"))
                    await _fullTextDb.RetrieveFavorites();
                _fullTextDb.UpdateSearchResults(_uiState);
        }

        public void SetState(UIState.State state)
        {
            _uiState.SetState(state);

            if (state == UIState.State.Compendium)
            {
                SetDataContext(state);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = false;
                this.Prescriptions.IsChecked = false;
                this.Compendium.IsChecked = true;
            }
            else if (state == UIState.State.Favorites)
            {
                SetDataContext(state);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Compendium.IsChecked = false;
                this.Interactions.IsChecked = false;
                this.Prescriptions.IsChecked = false;
                this.Favorites.IsChecked = true;
            }
            else if (state == UIState.State.Interactions)
            {
                SetDataContext(state);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Compendium.IsChecked = false;
                this.Favorites.IsChecked = false;
                this.Prescriptions.IsChecked = false;
                this.Interactions.IsChecked = true;

                _interactions.ShowBasket();
            }
            else if (state == UIState.State.Prescriptions)
            {
                SetDataContext(state);
                _sqlDb.UpdateSearchResults(_uiState);
                this.Compendium.IsChecked = false;
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = false;
                this.Prescriptions.IsChecked = true;

                Button button = GetElementIn("OpenProfileCardButton", MainArea) as Button;
                if (button != null && !Account.IsSet())
                    button.Visibility = Visibility.Visible;

                _prescriptions.LoadFiles();
            }
        }

        public void SetDataContext(UIState.State state)
        {
            if (this.StatusBar.DataContext == null) {
                this.StatusBar.DataContext = _statusBarHelper;
            }

            if (state == UIState.State.Compendium)
            {
                this.DataContext = new ViewType("Html");
                SwitchViewContext();

                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                {
                    this.SearchResult.DataContext = _sqlDb;
                    var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
                    if (box != null)
                        box.DataContext = _fachInfo;

                    var browser = GetView() as WebBrowser;
                    if (browser != null)
                    {
                        browser.DataContext = _fachInfo;
                        browser.ObjectForScripting = _fachInfo;
                    }
                }
            }
            else if (state == UIState.State.Favorites)
            {
                this.DataContext = new ViewType("Html");
                SwitchViewContext();

                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                {
                    this.SearchResult.DataContext = _sqlDb;
                    var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
                    if (box != null)
                        box.DataContext = _fachInfo;

                    var browser = GetView() as WebBrowser;
                    if (browser != null)
                    {
                        browser.DataContext = _fachInfo;
                        browser.ObjectForScripting = _fachInfo;
                    }
                }
            }
            else if (state == UIState.State.Interactions)
            {
                this.DataContext = new ViewType("Html");
                SwitchViewContext();

                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                {
                    this.SearchResult.DataContext = _sqlDb;
                    var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
                    if (box != null)
                        box.DataContext = _interactions;

                    var browser = GetView() as WebBrowser;
                    if (browser != null)
                    {
                        browser.DataContext = _interactions;
                        browser.ObjectForScripting = _interactions;
                    }
                }
            }
            else if (state == UIState.State.Prescriptions)
            {
                this.DataContext = new ViewType("Form", false);
                SwitchViewContext();

                if (ActiveContact != null)
                    FillContactFields();

                if (ActiveAccount != null)
                {
                    var accountInfo = GetElementIn("AccountInfo", MainArea) as Grid;
                    if (accountInfo != null)
                        accountInfo.DataContext = ActiveAccount;

                    LoadAccountPicture();
                    FillPlaceDate();
                    EnableButton("NewPrescriptionButton", true);

                    if (ActiveAccount != null && ActiveContact != null && !_prescriptions.IsActivePrescriptionPersisted)
                        EnableButton("SavePrescriptionButton", true);
                }

                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                {
                    this.SearchResult.DataContext = _sqlDb;
                    var names = GetElementIn("FileNameList", RightArea) as ListBox;
                    if (names != null)
                        names.DataContext = _prescriptions;

                    var medicationList = GetElementIn("MedicationList", MainArea) as ListBox;
                    if (medicationList != null)
                        medicationList.DataContext = _prescriptions;

                    var grid = GetView() as Grid;
                    if (grid != null)
                    {
                        grid.DataContext = _prescriptions;
                    }
                }
            }
        }

        public void SetFullTextSearchDataContext()
        {
            this.SearchResult.DataContext = _fullTextDb;
            var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
            if (box != null)
                box.DataContext = _fullTextSearch;

            var browser = GetView() as WebBrowser;
            if (browser != null)
            {
                browser.DataContext = _fullTextSearch;
                browser.ObjectForScripting = _fachInfo;
            }
        }

        public string SearchFieldText()
        {
            return this.SearchTextBox.Text;
        }

        public string SelectedFullTextSearchKey()
        {
            // Remove parentheses
            int idx = _selectedFullTextSearchKey.IndexOf("(");
            return _selectedFullTextSearchKey.Substring(0, idx).Trim();
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
            _statusBarHelper.IsConnectedToInternet();
            await _sqlDb?.Search(_uiState, "");

            _uiState.SetQuery(UIState.Query.Title);
            this.SearchTextBoxWaterMark = _uiState.SearchTextBoxWaterMark;
            this.TitleQuerySelectButton.Focus();
            this.TitleQuerySelectButton.IsChecked = true;

            // TODO
            // Fix default MainArea's `ContentTemplate` (via style with triggers) loading issue
            this.Prescriptions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            this.Compendium.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            if (Account.IsSet())
            {
                this.ActiveAccount = Properties.Settings.Default.Account;
                _prescriptions.ActiveAccount = ActiveAccount;
            }

            this.DataContext = new ViewType("Html");
            SwitchViewContext();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            // Change Window Title.
            string text = textBox.Text;
            if (text.Length > 0)
            {
                // Change the data context of the status bar
                Stopwatch sw = new Stopwatch();
                sw.Start();
                long numResults = 0;
                if (_uiState.FullTextQueryEnabled)
                    numResults = await _fullTextDb?.Search(_uiState, text);
                else
                    numResults = await _sqlDb?.Search(_uiState, text);
                sw.Stop();
                double elapsedTime = sw.ElapsedMilliseconds / 1000.0;
                _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numResults, elapsedTime));
            }
        }

        // Listens to click events in search box
        private async void OnSearchTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            this.SearchTextBox.Text = "";
            // Change the data context of the status bar
            Stopwatch sw = new Stopwatch();
            long numResults = 0;
            if (!_uiState.FullTextQueryEnabled)
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
            if (!(sender is ListBox))
                return;

            ListBox listBox = sender as ListBox;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
                item.IsSelected = false;

            //e.Handled = true; don't set here
        }

        // This event handler is called when the user selects the title in the search result
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
                    if (_uiState.FullTextQueryEnabled)
                    {
                        // Store selected fulltext search...
                        _selectedFullTextSearchKey = selection.Text;
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
                            if (_uiState.IsCompendium || _uiState.IsFavorites || _uiState.IsPrescriptions)
                            {
                                Article a = await _sqlDb.GetArticleFromId(_searchSelectionItemId);
                                _fachInfo.ShowFull(a);   // Load html in browser window
                            }
                            else if (_uiState.IsInteractions)
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
                }
            }
        }

        // This event handler is called when the user selects a package
        static long? _searchSelectionChildItemId = 0;
        private async void OnSearchChildItem_Selection(object sender, SelectionChangedEventArgs e)
        {
            //Log.WriteLine(sender.GetType().Name);
            var listBox = sender as ListBox;
            if (listBox?.Items.Count > 0)
            {
                object item = listBox.SelectedItem;
                if (item?.GetType() == typeof(ChildItem))
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    SetSpinnerEnabled(true);

                    ChildItem childItem = item as ChildItem;
                    if (_searchSelectionChildItemId != childItem.Id)
                    {
                        _searchSelectionChildItemId = childItem.Id;
                        if (_searchSelectionChildItemId != null)
                        {
                            if (_uiState.IsCompendium || _uiState.IsFavorites || _uiState.IsPrescriptions)
                            {
                                Article a = await _sqlDb.GetArticleFromId(_searchSelectionChildItemId);
                                _fachInfo.ShowFull(a);   // Load html in browser window
                            }
                        }
                    }

                    SetSpinnerEnabled(false);
                    sw.Stop();
                }
            }
        }

        // Makes it same behavior as context menu on macOS Version
        private void ToggleContextMenu(TextBlock block)
        {
            var menu = block?.ContextMenu;
            if (menu != null)
            {
                // check other menu is open or not
                if (_searchResultContextMenu == null || !_searchResultContextMenu.IsOpen)
                {
                    menu.PlacementTarget = block;
                    menu.IsOpen = true;
                    _searchResultContextMenu = menu;
                }
                else if (_searchResultContextMenu.IsOpen)
                {   // don't open
                    _searchResultContextMenu.IsOpen = false;
                    _searchResultContextMenu = null;
                    menu.IsOpen = false;
                }
            }
        }

        private void SearchChildItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;  // do nothing
        }

        private void SearchChildItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // NOTE:
                // Search result of title query on Interactions tab, it seems
                // that macOS and Windows version have different child items.
                if (_uiState.GetQueryTypeAsName().Equals("title") && !_uiState.IsInteractions)
                    ToggleContextMenu(sender as TextBlock);

                e.Handled = true;
            }
        }

        private void SearchChildItemContextMenu_Closing(object sender, ContextMenuEventArgs e)
        {
            _searchResultContextMenu = null;
            e.Handled = true;
        }

        private async void SearchChildItemContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            _searchResultContextMenu = null;
            ChildItem item = (sender as MenuItem)?.DataContext as ChildItem;
            if (item != null && item.Ean != null && !item.Ean.Equals(string.Empty))
            {
                Article article = await _sqlDb.GetArticleWithId(item.Id);
                if (article != null)
                {
                    var medication = new Medication(item.Ean, article);
                    _prescriptions.AddMedication(medication);

                    if (!_uiState.IsPrescriptions)
                    {
                        SetState(UIState.State.Prescriptions);
                        this.Prescriptions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }

                    if (ActiveContact != null && ActiveAccount != null)
                        EnableButton("SavePrescriptionButton", true);

                    e.Handled = true;
                }
            }
        }

        private void MedicationCommentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = sender as TextBox;
            string text = "";
            if (box != null)
            {
                text = box.Text;

                // fix for watermark position issue
                Thickness t = box.Padding;
                t.Left = (text.Equals(string.Empty)) ? 0 : 2;
                box.Padding = t;

                var listBoxItem = this.FindVisualAncestor<ListBoxItem>(box);
                var item = listBoxItem.Content as CommentItem;
                if (item != null)
                {
                    var index = item.Id;
                    var added = _prescriptions.AddMedicationCommentAtIndex(index, text);

                    if (added && ActiveContact != null && ActiveAccount != null)
                        EnableButton("SavePrescriptionButton", true);

                    e.Handled = true;
                }
            }
        }

        private void DeleteMedicationButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            var button = sender as Button;
            var listBoxItem = this.FindVisualAncestor<ListBoxItem>(button);
            var item = listBoxItem.Content as CommentItem;
            if (item != null)
            {
                var index = item.Id;
                _prescriptions.RemoveMedicationAtIndex(index);
                EnableButton("SavePrescriptionButton", true);
            }
        }

        #region SectionTitleList EventHandlers
        // Event handler called when the user selects a section title, injects javascript into Browser window
        private void SectionTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            ListBox box = sender as ListBox;
            if (box?.Items.Count > 0)
            {
                TitleItem item = box.SelectedItem as TitleItem;
                if (item != null && item.Id != null)
                {
                    if (!_uiState.FullTextQueryEnabled)
                    {
                        // Inject javascript to move to anchor
                        string jsCode = "document.getElementById('" + item.Id + "').scrollIntoView(true);";
                        InjectJS(jsCode);
                    }
                    else
                    {
                        // Set filter
                        _fullTextSearch.Filter = item.Id;
                        // Update result table
                        _fullTextSearch.UpdateTable();
                    }
                    e.Handled = true;
                }
            }
        }
        #endregion

        #region FileNameList EventHandlers
        private async void FileNameContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            var item = (sender as MenuItem)?.DataContext as TitleItem;
            Log.WriteLine("item: {0}", item);
            if (item != null && !item.Title.Equals(string.Empty))
            {
                var file = item.Title;
                await _prescriptions.DeleteFile(file);
                _prescriptions.LoadFiles();

                var button = GetElementIn("NewPrescriptionButton", this.MainArea);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void FileName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            ListBox box = sender as ListBox;
            if (box?.Items.Count > 0)
            {
                TitleItem item = box.SelectedItem as TitleItem;
                if (item != null && item.Id != null)
                {
                    _prescriptions.LoadFile(item.Title);
                    EnableButton("SavePrescriptionButton", false);

                    FillPlaceDate();
                    e.Handled = true;
                }
            }
        }

        private async void FileName_KeyDown(object sender, KeyEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            if (e.Key == Key.Back)
            {
                var dialog = Utilities.MessageDialog(
                    Properties.Resources.msgPrescriptionDeleteConfirmation, "", "OKCancel");
                dialog.ShowDialog();

                var result = dialog.MessageBoxResult;
                if (result == MessageBoxResult.OK)
                {
                    ListBox box = sender as ListBox;
                    if (box?.Items.Count > 0)
                    {
                        TitleItem item = box.SelectedItem as TitleItem;
                        if (item != null && item.Id != null)
                        {
                            var file = item.Title;
                            await _prescriptions.DeleteFile(file);
                            _prescriptions.Renew();
                            _prescriptions.LoadFiles();
                            EnableButton("SavePrescriptionButton", false);

                            FillPlaceDate();
                            e.Handled = true;
                        }
                    }
                }
            }
            else
                e.Handled = false;
        }

        // export
        private void FileName_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            ListBox box = sender as ListBox;
            e.Handled = false;

            if (box == null)
                return;

            FrameworkElement element = e.OriginalSource as FrameworkElement;
            if (element == null)
                return;

            ListBoxItem li = (ListBoxItem)box.ItemContainerGenerator.ContainerFromItem(element.DataContext);
            if (li == null)
                return;

            var item = li.Content as TitleItem;

            string[] paths = new string[1];
            var path = _prescriptions.FindFilePathByName(item.Title);
            if (path != null)
            {
                paths[0] = path;
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, paths), DragDropEffects.Copy);
                e.Handled = true;
            }
        }

        private void FileName_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void FileName_DragOver(object sender, DragEventArgs e)
        {
            // pass
            e.Handled = true;
        }

        private void FileName_DragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        // import
        private void FileName_Drop(object sender, DragEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            var box = sender as ListBox;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // TODO
                object data = e.Data.GetData(typeof(string));
                Log.WriteLine("data: {0}", data);
            }
            e.Handled = true;
        }
        #endregion

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
                else if (item.Hash != null)
                {
                    FullTextEntry entry = await _fullTextDb.GetEntryWithHash(item.Hash);
                    _fullTextDb.UpdateFavorites(entry);
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
                var browser = GetView() as WebBrowser;
                if (browser != null)
                {
                    browser.DataContext = _fachInfo;
                    await _fachInfo.ShowReport();
                }
            }
            else if (name.Equals("AccountAddress"))
            {
                ViewType viewType;
                viewType = this.DataContext as ViewType;
                if (viewType == null)
                    return;

                if (viewType.Mode.Equals("Html"))
                {
                    // NOTE
                    // WebBrowser does not allow to put controls over on it :'(
                    // Thus, flyouts does not work on HTML Context.
                    //
                    // https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/wpf-and-win32-interoperation
                    SetState(UIState.State.Prescriptions);
                    viewType = this.DataContext as ViewType;
                    this.Prescriptions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else
                {
                    // close addressbook if it's visible.
                    viewType.HasBook = false;
                }
                viewType.HasCard = true;
                this.DataContext = viewType;

                // ProfileCard
                this.FlyoutMenu.IsOpen = false;
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

        // Tab
        private void StateButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            SetState(source.Name);
        }

        private async void QuerySelectButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            UIState.Query query = UIState.QueryBySourceName(source.Name.Replace("QuerySelectButton", ""));

            if (query != _uiState.GetQuery()) { // current query
                this.SearchTextBox.Text = "";
            }
            // uncheck other buttons
            DependencyObject parent = (sender as ToggleButton).Parent;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            ToggleButton button;
            for (int i = 0; i < count; i++)
            {
                button = null;
                button = VisualTreeHelper.GetChild(parent, i) as ToggleButton;
                if (button != null)
                    button.IsChecked = false;
            }
            (sender as ToggleButton).IsChecked = true;

            this.SearchTextBox.Focus();
            UIState.State state = _uiState.GetState();

            if (query == UIState.Query.Fulltext)
            {
                _uiState.SetQuery(UIState.Query.Fulltext);
                this.SearchTextBoxWaterMark = _uiState.SearchTextBoxWaterMark;

                // only change data context (keep state)
                SetDataContext(state);

                if (state == UIState.State.Favorites) {
                    await _fullTextDb.RetrieveFavorites();
                } else {
                    _fullTextDb.ClearFoundEntries();
                }
                _fullTextDb.UpdateSearchResults(_uiState);
            }
            else
            {
                _uiState.SetQuery(query);
                this.SearchTextBoxWaterMark = _uiState.SearchTextBoxWaterMark;
                SetState(state);
            }
        }

        private void SetBrowserEmulationMode()
        {
            var fileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            if (String.Compare(fileName, "devenv.exe", true) == 0 || String.Compare(fileName, "XDesProc.exe", true) == 0)
                return;
            UInt32 mode = 10000;
            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, mode);
        }

        private void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue(appName, (UInt32)value, RegistryValueKind.DWord);
            }
        }

        // Injects javascript into the current browser
        public void InjectJS(string jsCode)
        {
            var browser = GetView() as WebBrowser;
            if (browser != null)
                browser.InvokeScript("execScript", new Object[] { jsCode, "JavaScript" });
        }

        private void WebBrowser_Loaded(object sender, EventArgs e)
        {
            // Pass
        }

        private void WebBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            // Pass, See InjectJS
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

        private void OpenAddressBookButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            this.DataContext = new ViewType("Form", true);
            e.Handled = true;
        }

        private void OpenProfileCardButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            ViewType viewType;
            viewType = this.DataContext as ViewType;
            if (viewType == null)
                return;

            viewType.HasCard = true;
            this.DataContext = viewType;

            this.FlyoutMenu.IsOpen = false;
            e.Handled = true;
        }

        private void NewPrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();

            var box = GetElementIn("FileNameList", this.RightArea) as ListBox;
            if (box?.Items.Count > 0)
                box.SelectedIndex = -1;

            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);
            _prescriptions.Renew();

            FillPlaceDate();

            EnableButton("CheckInteractionButton", false);
            EnableButton("SavePrescriptionButton", false);
            EnableButton("SendPrescriptionButton", false);
            e.Handled = true;
        }

        private void CheckInteractionButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);
            e.Handled = true;
        }

        private async void SavePrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            bool doSave = true;
            bool asRewriting = false;
            if (_prescriptions.IsActivePrescriptionPersisted)
            {
                var dialog = Utilities.MessageDialog(
                    Properties.Resources.msgPrescriptionSavingContextConfirmation, "", "YesNoCancel");
                // Note: see Style.xaml about style of MessageBox
                dialog.YesButtonContent = Properties.Resources.rewrite;
                dialog.NoButtonContent = Properties.Resources.newPrescription;
                dialog.ShowDialog();
                var result = dialog.MessageBoxResult;

                doSave = (result == MessageBoxResult.Yes || result == MessageBoxResult.No);
                asRewriting = (result == MessageBoxResult.Yes);
            }

            if (doSave)
            {
                await _prescriptions.Save(asRewriting);

                Keyboard.ClearFocus();
                _prescriptions.LoadFiles();

                var box = GetElementIn("FileNameList", this.RightArea) as ListBox;
                if (box?.Items.Count > 0)
                    box.SelectedIndex = 0;

                FillPlaceDate();
                EnableButton("SavePrescriptionButton", false);
                EnableButton("SendPrescriptionButton", true);
            }
            e.Handled = true;
        }

        private void SendPrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            e.Handled = true;
        }

        private void AddressBookControl_ClosingFinished(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as MahApps.Metro.Controls.Flyout;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            if (_prescriptions.ActiveContact != null && ActiveContact != null &&
                _prescriptions.ActiveContact.Uid != ActiveContact.Uid) // change of contact (patient)
            {
                // doesn't renew here (keep current medications)
                _prescriptions.Hash = Utilities.GenerateUUID();
                _prescriptions.PlaceDate = null;
            }

            _prescriptions.ActiveContact = ActiveContact;
            _prescriptions.LoadFiles();
            FillContactFields();
            FillPlaceDate();

            EnableButton("SendPrescriptionButton", false);

            if (ActiveAccount != null && ActiveContact != null && _prescriptions.Medications.Count > 0)
                EnableButton("SavePrescriptionButton", true);
            else
                EnableButton("SavePrescriptionButton", false);

            // Re:enable animations for next time
            source.AreAnimationsEnabled = true;
            e.Handled = true;
        }

        private void ProfileCardControl_ClosingFinished(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as MahApps.Metro.Controls.Flyout;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            if (Account.IsSet() && ActiveAccount != null)
            {
                _prescriptions.ActiveAccount = ActiveAccount;
                LoadAccountPicture();
                FillAccountFields(); // only for initial loading (at first time)
                FillPlaceDate();

                var button = GetElementIn("OpenProfileCardButton", this.MainArea) as Button;
                if (button != null)
                    button.Visibility = Visibility.Collapsed;

                EnableButton("NewPrescriptionButton", true);
            }

            // Re:enable animations for next time
            source.AreAnimationsEnabled = true;
            e.Handled = true;
        }

        #region Fill Methods
        public void FillAccountFields()
        {
            TextBlock block = null;
            var fields = new string[] {"Fullname", "Address", "Place", "Phone", "Email"};
            foreach (var f in fields)
            {
                var key = String.Format("Account{0}", f);
                block = GetElementIn(key, MainArea) as TextBlock;
                if (block != null)
                {
                    if (ActiveAccount != null)
                        block.Text = (string)ActiveAccount.GetType().GetProperty(f).GetValue(ActiveAccount, null);
                    else
                        block.Text = "";
                    block.UpdateLayout();
                }
            }
        }

        public void FillContactFields()
        {
            TextBlock block = null;
            var fields = new string[] {"Fullname", "Address", "Place", "PersonalInfo", "Phone", "Email"};
            foreach (var f in fields)
            {
                var key = String.Format("Contact{0}", f);
                block = GetElementIn(key, MainArea) as TextBlock;
                if (block != null)
                {
                    if (ActiveContact != null)
                        block.Text = (string)ActiveContact[f];
                    else 
                        block.Text = "";
                    block.UpdateLayout();
                }
            }
        }

        public void FillPlaceDate()
        {
            var block = GetElementIn("PlaceDate", MainArea) as TextBlock;
            if (block != null)
            {
                if (ActiveContact != null && ActiveAccount != null)
                    block.Text = _prescriptions.PlaceDate;
                else 
                    block.Text = "";
                block.UpdateLayout();
            }
        }
        #endregion

        private void LoadAccountPicture()
        {
            try
            {
                Image image = GetElementIn("AccountPicture", MainArea) as Image;
                if (image != null && Account.IsSet() && ActiveAccount != null)
                    Utilities.LoadPictureInto(image, ActiveAccount.PictureFile);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is NotSupportedException)
                    Log.WriteLine(ex.Message);
                else
                    throw ex;
            }
        }

        private void EnableButton(string name, bool isEnabled)
        {
            Button button = GetElementIn(name, MainArea) as Button;
            if (button != null)
                button.IsEnabled = isEnabled;
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
            if (browser != null)
            {
                var text = (string)e.NewValue;
                if (text != null && text != string.Empty)
                    browser.NavigateToString(text);
                else
                    browser.NavigateToString(" "); // set empty document
            }
        }
    }
}
