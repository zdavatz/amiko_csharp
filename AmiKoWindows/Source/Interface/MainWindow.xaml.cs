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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Navigation;

using Microsoft.Win32;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using MahApps.Metro.Controls;

namespace AmiKoWindows
{
    using ControlExtensions;
    using MahApps.Metro;
    using System.Threading;

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

        private DataTransferManager _dataTransferManager;

        static bool _willNavigate = false;

        UIState _uiState;

        MainSqlDb _sqlDb;
        FullTextDb _fullTextDb;
        PatientDb _patientDb;

        FachInfo _fachInfo;
        FullTextSearch _fullTextSearch;
        InteractionsCart _interactions;
        PrescriptionsTray _prescriptions;
        StatusBarHelper _statusBarHelper;

        string _selectedFullTextSearchKey;
        bool _search = false; // lock

        FrameworkElement _browser;
        FrameworkElement _manager;

        private ContextMenu _contextMenu = null;
        private bool _fileNameListInDrag = false;
        private bool _hasFile = false;

        // current (selected section)
        private string _currentSection = null;

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

            // Initialize Patient (In-App Address Book) DB
            _patientDb = new PatientDb();
            _patientDb.Init();

            // Initialize prescriptions container
            _prescriptions = new PrescriptionsTray();
            _prescriptions.LoadFiles();

            _statusBarHelper = new StatusBarHelper();

            this.Spinner.Spin = false;

            // Set initial state
            SetState(UIState.State.Compendium);

            // Set browser emulation mode. Thx Microsoft for these stupid hacks!!
            SetBrowserEmulationMode();
            
            ReloadColors();

            UserPreferenceChangedEventHandler e3 = (o, e) =>
            {
                ReloadColors();
            };
            SystemEvents.UserPreferenceChanged += e3;
        }

        #region WndProc Support
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        // Gets .amk file path via WndProc Message from DoubleClick  etc.
        // See also App.xaml.cs.
        //
        // * https://web.archive.org/web/20091019124817/http://www.steverands.com/2009/03/19/custom-wndproc-wpf-apps/
        // * https://boycook.wordpress.com/2008/07/29/c-win32-messaging-with-sendmessage-and-wm_copydata/
        protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == App.AMIKO_MSG)
            {
                Log.WriteLine("AMIKO_MSG: {0}", App.AMIKO_MSG);
                try {
                    var dat = Marshal.PtrToStructure(lparam, typeof(App.AMIKO_DAT));
                    if (dat != null && dat is App.AMIKO_DAT)
                        OpenFile(((App.AMIKO_DAT)dat).msg);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.Message);
                }
            }
            return IntPtr.Zero;
        }
        #endregion

        #region Public Methods
        // This is a public endpoint for the booting of application with file (path)
        public void OpenFile(string path)
        {
            Log.WriteLine("path: {0}", path);
            if (path == null || path.Equals(string.Empty) || !File.Exists(path))
                return;

            this.DataContext = new ViewType("Form");
            SwitchViewContext();

            this._hasFile = true;

            Prescriptions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ImportFile(path);
        }

        public void BringToFront()
        {
            if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
            {
                this.WindowState = WindowState.Normal;
                this.Show();
            }

            this.Activate();
            this.Focus();
            Keyboard.ClearFocus();
        }

        public void SetState(UIState.State state)
        {
            _uiState.SetState(state);

            if (state == UIState.State.Compendium)
            {
                SetDataContext(state);
                this.Favorites.IsChecked = false;
                this.Interactions.IsChecked = false;
                this.Prescriptions.IsChecked = false;
                this.Compendium.IsChecked = true;
            }
            else if (state == UIState.State.Favorites)
            {
                SetDataContext(state);
                this.Compendium.IsChecked = false;
                this.Interactions.IsChecked = false;
                this.Prescriptions.IsChecked = false;
                this.Favorites.IsChecked = true;
            }
            else if (state == UIState.State.Interactions)
            {
                SetDataContext(state);
                this.Compendium.IsChecked = false;
                this.Favorites.IsChecked = false;
                this.Prescriptions.IsChecked = false;
                this.Interactions.IsChecked = true;

                _interactions.ShowBasket();
            }
            else if (state == UIState.State.Prescriptions)
            {
                SetDataContext(state);
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

        // Injects javascript into the current browser
        public void InjectJS(string jsCode)
        {
            var browser = GetView() as WebBrowser;
            if (browser != null)
            {
                Log.WriteLine("browser.DataContext: {0}", browser.DataContext);
                Log.WriteLine("jsCode: {0}", jsCode);
                browser.InvokeScript("execScript", new Object[] { jsCode, "JavaScript" });
            }
        }

        public string SelectedFullTextSearchKey()
        {
            int idx = _selectedFullTextSearchKey.IndexOf("("); // Remove parentheses
            if (idx > -1)
                return _selectedFullTextSearchKey.Substring(0, idx).Trim();

            return "";
        }

        // For Fulltext Search. This keeps search results by fulltext query, but
        // enables show fachinfo document in browser.
        public void BringFachinfoIntoView()
        {
            this.SearchResult.DataContext = _fullTextDb;
            var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
            if (box != null)
                box.DataContext = _fullTextSearch;

            // set only document body as fachinfo
            var browser = GetView() as WebBrowser;
            if (browser != null)
            {
                browser.DataContext = _fachInfo;
                browser.ObjectForScripting = _fachInfo;
            }
        }

        #region Fill Utilities
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
            LoadAccountPicture();
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
        #endregion

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

        private void SwitchViewContext()
        {
            var viewType = DataContext as ViewType;
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

        private FrameworkElement GetView()
        {
            FrameworkElement element = null;

            var viewType = DataContext as ViewType;
            if (viewType.Mode.Equals("Form"))
                element = _manager;
            else
                element = _browser;

            return element;
        }

        private void SetDataContext(UIState.State state)
        {
            if (StatusBar.DataContext == null) {
                this.StatusBar.DataContext = _statusBarHelper;
            }

            if (state == UIState.State.Compendium)
            {
                this.DataContext = new ViewType("Html");
                SwitchViewContext();

                var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
                if (box != null)
                    box.DataContext = _fachInfo;

                var text = SearchFieldText();
                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                {
                    this.SearchResult.DataContext = _sqlDb;

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

                var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
                if (box != null)
                    box.DataContext = _fachInfo;

                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                {
                    this.SearchResult.DataContext = _sqlDb;

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

                var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
                if (box != null)
                    box.DataContext = _interactions;

                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                {
                    this.SearchResult.DataContext = _sqlDb;

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

                    FillAccountFields();
                    FillPlaceDate();
                    EnableButton("NewPrescriptionButton", true);
                    EnableButton("SendPrescriptionButton", false);

                    if (Account.IsSet() &&
                        ActiveAccount != null &&
                        ActiveContact != null && (_prescriptions.HasChange || !_prescriptions.IsActivePrescriptionPersisted))
                        EnableButton("SavePrescriptionButton", true);

                    EnableButton("CheckInteractionButton", (_prescriptions.Medications.Count > 0));
                }

                var box = GetElementIn("FileNameList", RightArea) as ListBox;
                if (box != null)
                    box.DataContext = _prescriptions;

                var medicationList = GetElementIn("MedicationList", MainArea) as ListBox;
                if (medicationList != null)
                    medicationList.DataContext = _prescriptions;

                var grid = GetView() as Grid;
                if (grid != null)
                    grid.DataContext = _prescriptions;

                if (_uiState.FullTextQueryEnabled)
                    SetFullTextSearchDataContext();
                else
                    this.SearchResult.DataContext = _sqlDb;
            }
        }

        private void SetFullTextSearchDataContext()
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

        private string SearchFieldText()
        {
            return SearchTextBox.Text;
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

        private void ReloadColors()
        {
            Colors.ReloadColors();
            if (Colors.IsLightMode())
            {
                ThemeManager.ChangeAppStyle(Application.Current,
                        ThemeManager.GetAccent("Steel"),
                        ThemeManager.GetAppTheme("BaseLight"));
                this.CompendiumIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/aips32x32_gray.png", UriKind.RelativeOrAbsolute));
                this.FavoritesIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/favorites32x32_gray.png", UriKind.RelativeOrAbsolute));
                this.InteractionsIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/interactions32x32_gray.png", UriKind.RelativeOrAbsolute));
                this.PrescriptionsIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/prescriptions64x64.png", UriKind.RelativeOrAbsolute));
            } else
            {
                ThemeManager.ChangeAppStyle(Application.Current,
                        ThemeManager.GetAccent("Steel"),
                        ThemeManager.GetAppTheme("BaseDark"));
                this.CompendiumIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/aips32x32_light.png", UriKind.RelativeOrAbsolute));
                this.FavoritesIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/favorites32x32_light.png", UriKind.RelativeOrAbsolute));
                this.InteractionsIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/interactions32x32_light.png", UriKind.RelativeOrAbsolute));
                this.PrescriptionsIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/img/prescriptions64x64_light.png", UriKind.RelativeOrAbsolute));
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _statusBarHelper.IsConnectedToInternet();

            Account.MigrateFromOldSettings();
            if (Account.IsSet())
            {
                this.ActiveAccount = Account.Read();
                _prescriptions.ActiveAccount = ActiveAccount;
            }

            Log.WriteLine("_hasFile: {0}", _hasFile);
            if (!_hasFile)
            {
                this.DataContext = new ViewType("Html");
                SwitchViewContext();
                Compendium.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            SmartCard smartcard = SmartCard.Instance;
            smartcard.ReceivedCardResult += ReceivedCardResult;
            smartcard.Start();

            patchScrollBarColor();

            _uiState.SetQuery(UIState.Query.Title);
            this.SearchTextBoxWaterMark = _uiState.SearchTextBoxWaterMark;
            TitleQuerySelectButton.Focus();
            this.TitleQuerySelectButton.IsChecked = true;
            await _sqlDb?.Search(_uiState, "");
        }

        private void patchScrollBarColor()
        {
            ScrollViewer sv = (ScrollViewer)VisualTreeHelper.GetChild(this.SearchResult, 0);
            sv.ApplyTemplate();
            ScrollBar s = sv.Template.FindName("PART_VerticalScrollBar", sv) as ScrollBar;
            var myResourceDictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.Scrollbars.xaml", UriKind.RelativeOrAbsolute)
            };
            var myStyle = myResourceDictionary["MetroScrollBar"] as Style;
            s.Style = myStyle;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            var textBox = sender as TextBox;
            // Change Window Title.
            string text = textBox.Text;
            if (text.Length > 0)
            {
                this._search = true;
                // Change the data context of the status bar
                Stopwatch sw = new Stopwatch();
                sw.Start();
                SetSpinnerEnabled(true);

                long numResults = 0;
                UIState.State state = _uiState.GetState();
                Log.WriteLine("state: {0}", state);
                if (_uiState.FullTextQueryEnabled) {
                    if (state == UIState.State.Favorites)
                        numResults = await _fullTextDb?.Filter(_uiState, text);
                    else
                        numResults = await _fullTextDb?.Search(_uiState, text);
                } else {
                    // NOTE:
                    // This may need also to update search results count for
                    // favorites. Which package is favorite is saved in text
                    // file, thus is would need more work in xaml. For now,
                    // it shows same count with Compendium Search in status bar.
                    if (state == UIState.State.Favorites)
                        numResults = await _sqlDb?.Filter(_uiState, text);
                    else
                        numResults = await _sqlDb?.Search(_uiState, text);
                }

                SetSpinnerEnabled(false);
                sw.Stop();
                double elapsedTime = sw.ElapsedMilliseconds / 1000.0;
                _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numResults, elapsedTime));
                await Task.Delay(1200);
                this._search = false;
            }
            else
                ResetSearchInDelay(400);
        }

        private async void ResetSearchInDelay(int delay = -1)
        {
            Log.WriteLine("delay: {0}", delay);

            if (delay > 1)
                await Task.Delay(delay);

            var text = this.SearchFieldText();
            Log.WriteLine("text: {0}", text);
            Log.WriteLine("search: {0}", _search);
            if (!_search)
            {
                // Change the data context of the status bar
                Stopwatch sw = new Stopwatch();
                sw.Start();
                SetSpinnerEnabled(true);

                long numResults = 0;
                Log.WriteLine("FullTextQueryEnabled: {0}", _uiState.FullTextQueryEnabled);
                if (_uiState.FullTextQueryEnabled)
                {
                    var state = _uiState.GetState();
                    _fullTextDb.ClearFoundEntries();
                    if (state == UIState.State.Favorites && (text == null || text.Equals(string.Empty)))
                    {
                        await _fullTextDb.RetrieveFavorites();
                        _fullTextDb.UpdateSearchResults(_uiState);
                    }
                    numResults = _fullTextDb.GetCount();
                }
                else
                    numResults = await _sqlDb?.Search(_uiState, text);

                SetSpinnerEnabled(false);
                sw.Stop();
                double elapsedTime = sw.ElapsedMilliseconds / 1000.0;
                _statusBarHelper.UpdateDatabaseSearchText(new Tuple<long, double>(numResults, elapsedTime));
            }
        }

        // Listens to click events in search box
        private void OnSearchTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            this.SearchTextBox.Text = "";
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
            var li = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (li != null)
                li.IsSelected = false;

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
                            if (listOfArticles != null && entry.RegChaptersDict != null)
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
        private void ToggleContextMenu(FrameworkElement block)
        {
            var menu = block?.ContextMenu;
            if (menu != null)
            {
                // check other menu is open or not
                if (_contextMenu == null || !_contextMenu.IsOpen)
                {
                    menu.PlacementTarget = block;
                    menu.IsOpen = true;
                    _contextMenu = menu;
                }
                else if (_contextMenu.IsOpen)
                {   // don't open
                    _contextMenu.IsOpen = false;
                    _contextMenu = null;
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
                    ToggleContextMenu(sender as FrameworkElement);

                e.Handled = true;
            }
        }

        private void SearchChildItemContextMenu_Closing(object sender, ContextMenuEventArgs e)
        {
            _contextMenu = null;
            e.Handled = true;
        }

        private async void SearchChildItemContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            _contextMenu = null;
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
                        Prescriptions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }

                    if (Account.IsSet() &&
                        ActiveContact != null && (ActiveAccount != null && ActiveAccount.Fullname != null && ActiveAccount.Fullname.Length > 0))
                    {
                        EnableButton("SavePrescriptionButton", true);
                        EnableButton("SendPrescriptionButton", false);
                    }

                    EnableButton("CheckInteractionButton", true);
                    EnableButton("PrintPrescriptionButton", false);
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

                var li = this.FindVisualAncestor<ListBoxItem>(box);
                var item = li.Content as CommentItem;
                if (item != null)
                {
                    var index = item.Id;
                    var added = _prescriptions.AddMedicationCommentAtIndex(index, text);

                    if (added && Account.IsSet() &&
                        ActiveContact != null && (ActiveAccount != null && ActiveAccount.Fullname != null && ActiveAccount.Fullname.Length > 0))
                    {
                        EnableButton("SavePrescriptionButton", true);
                        EnableButton("SendPrescriptionButton", false);
                        EnableButton("PrintPrescriptionButton", false);
                    }
                    e.Handled = true;
                }
            }
        }

        private void PrintMedicationLabelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            var button = sender as Button;
            var li = this.FindVisualAncestor<ListBoxItem>(button);
            var item = li.Content as CommentItem;
            if (item != null && item.Id != null)
            {
                var medication = _prescriptions.Medications[(int)item.Id.Value];
                var label = new MedicationLabel(_prescriptions.PlaceDate)
                {
                    ActiveContact = ActiveContact,
                    ActiveAccount = ActiveAccount,
                    Medication = medication
                };
                Printer.PrintMedicationLabel(label);
            }
        }

        private void DeleteMedicationButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            var button = sender as Button;
            var li = this.FindVisualAncestor<ListBoxItem>(button);
            var item = li.Content as CommentItem;
            if (item != null)
            {
                var index = item.Id;
                _prescriptions.RemoveMedicationAtIndex(index);
                EnableButton("SavePrescriptionButton", (_prescriptions.Medications.Count > 0));
                EnableButton("SendPrescriptionButton", false);
            }

            EnableButton("CheckInteractionButton", _prescriptions.Medications.Count > 0);
            EnableButton("PrintPrescriptionButton", false);
        }

        #region SectionTitleList EventHandlers
        private void SectionTitleList_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            SetActiveSectionAsSelected();
        }

        // Event handler called when the user selects a section title, injects javascript into Browser window
        private void SectionTitleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            ListBox box = sender as ListBox;
            if (box?.Items.Count > 0)
            {
                var item = box.SelectedItem as TitleItem;
                if (item != null && item.Id != null)
                {
                    _currentSection = item.Id;
                    if (!_uiState.FullTextQueryEnabled)
                    {
                        // Inject javascript to move to anchor
                        string jsCode = "" +
                        "var elem = document.getElementById('" + item.Id + "');" +
                        "if (elem != null && typeof elem != 'undefined') { elem.scrollIntoView(true); }";
                        InjectJS(jsCode);
                    }
                    else
                    {
                        // Back to fulltext search result
                        var browser = GetView() as WebBrowser;
                        if (browser != null)
                        {
                            browser.DataContext = _fullTextSearch;
                            browser.ObjectForScripting = _fachInfo;
                        }
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
        private void FileNameList_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            SetActiveFileAsSelected();
        }

        private async void FileNameContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            if (_fileNameListInDrag)
                return;

            var item = (sender as MenuItem)?.DataContext as FileItem;
            bool result = await DeleteFileItem(item);
            if (result)
                e.Handled = true;
        }

        private async Task<bool> DeleteFileItem(FileItem item)
        {
            if (item != null && item.IsValid)
            {
                var dialog = Utilities.MessageDialog(
                    Properties.Resources.msgPrescriptionDeleteConfirmation, "", "OKCancel");
                dialog.ShowDialog();

                var result = dialog.MessageBoxResult;
                if (result == MessageBoxResult.OK)
                {
                    await _prescriptions.DeleteFile(item.Path);
                    _prescriptions.Renew();
                    _prescriptions.LoadFiles();
                    EnableButton("CheckInteractionButton", false);
                    EnableButton("SavePrescriptionButton", false);
                    EnableButton("SendPrescriptionButton", false);

                    FillPlaceDate();
                    return true;
                }
            }
            return false;
        }

        private void FileNameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            var box = sender as ListBox;
            if (box?.Items.Count > 0)
            {
                var item = box.SelectedItem as FileItem;
                if (item != null && item.IsValid)
                {
                    _prescriptions.Hash = item.Hash;

                    if (!_prescriptions.HasChange)
                        _prescriptions.ReadFile(item.Path);

                    ActiveContact = _prescriptions.ActiveContact;
                    ActiveAccount = _prescriptions.ActiveAccount;

                    FillContactFields();
                    FillAccountFields();
                    FillPlaceDate();

                    EnableButton("CheckInteractionButton", _prescriptions.Medications.Count > 0);
                    EnableButton("SavePrescriptionButton", (Account.IsSet() && (_prescriptions.HasChange || _prescriptions.IsPreview)));

                    var saved = (!_prescriptions.HasChange && _prescriptions.IsActivePrescriptionPersisted);
                    EnableButton("SendPrescriptionButton", saved);
                    EnableButton("PrintPrescriptionButton", saved);
                    e.Handled = true;
                }
            }
        }

        private async void FileNameList_KeyDown(object sender, KeyEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            e.Handled = false;

            if (_fileNameListInDrag)
                return;

            var box = GetElementIn("FileNameList", RightArea) as ListBox;
            if (box == null || !object.ReferenceEquals(sender, box))
                return;

            if (e.IsDown && e.Key == Key.Back && box.Items.Count > 0)
            {
                var item = box.SelectedItem as FileItem;
                bool result = await DeleteFileItem(item);
                if (result)
                    e.Handled = true;
            }
        }

        // NOTE:
        // User must start drag while keeping mouse button down.
        // See also PreviewMouseLeftButtonDown/Up
        private void FileNameList_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = false;

            if (!_fileNameListInDrag)
                return;

            Log.WriteLine(sender.GetType().Name);

            _fileNameListInDrag = false;

            ListBox box = sender as ListBox;
            if (box == null)
                return;

            var element = e.OriginalSource as FrameworkElement;
            if (element == null || element.DataContext == null)
                return;

            var li = box.ItemContainerGenerator.ContainerFromItem(element.DataContext) as ListBoxItem;
            if (li == null)
                return;

            var item = li.Content as FileItem;
            if (item == null)
                return;

            string[] paths = new string[1];
            var path = _prescriptions.FindFilePathByNameFor(item.Name, ActiveContact);
            if (path != null)
            {
                paths[0] = path;
                DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, paths), DragDropEffects.Copy);
            }
            e.Handled = true;
        }

        private void FileNameList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            _fileNameListInDrag = true;

            e.Handled = false;
        }

        private void FileNameList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            _fileNameListInDrag = false;

            e.Handled = false;
        }

        private void FileNameList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
        }

        private void FileNameList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void FileNameList_DragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        // Handle drop of files from explorer etc. (import)
        private void FileNameList_Drop(object sender, DragEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            if (_fileNameListInDrag)
                return;

            EnableButton("SavePrescriptionButton", false);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string [])e.Data.GetData(DataFormats.FileDrop);
                if (paths.Length < 1)
                    return;

                // treat only first file (same as macOS)
                var path = paths[0];
                Log.WriteLine("path: {0}", path);

                ImportFile(path);
                e.Handled = true;
            }
            e.Handled = false;
        }
        #endregion

        private async void ImportFile(string path)
        {
            // disable accidental drop
            var amikoDir = Utilities.PrescriptionsPath();
            var inboxDir = Utilities.GetInboxPath();
            if (path.Contains(inboxDir) || path.Contains(amikoDir))
                return;

            // filepath in inbox/amiko (new or existing one if exists)
            string filepath = _prescriptions.CopyFile(path);
            Log.WriteLine("filepath: {0}", filepath);
            if (filepath == null)
                return;

            Xceed.Wpf.Toolkit.MessageBox dialog = null;
            var filename = Path.GetFileName(filepath);

            var result = await _prescriptions.ImportFile(filepath);
            if (result == PrescriptionsTray.Result.Invalid)
            {
                dialog = Utilities.MessageDialog(
                    Properties.Resources.msgPrescriptionImportFailure, "", "OK");
                dialog.ShowDialog();
                return;
            }
            else if (result == PrescriptionsTray.Result.Found)
            {
                this.ActiveContact = _prescriptions.ActiveContact;
                this.ActiveAccount = _prescriptions.ActiveAccount;

                dialog = Utilities.MessageDialog(
                    String.Format(Properties.Resources.msgPrescriptionFileFound, filename), "", "OK");
            }
            else if (result == PrescriptionsTray.Result.Ok)
            {
                // NOTE:
                // The timing of validations is little bit late... But
                // PrescriptionsTray does not know _patientDb and _sqlDb. Tuhs let's do here :'(

                Contact contactInFile = _prescriptions.ActiveContact;
                if (contactInFile == null || contactInFile.Uid == null || contactInFile.Uid.Equals(string.Empty))
                {
                    _prescriptions.Renew();
                    return;
                }

                string uid = contactInFile.Uid;

                // NOTE:
                // Uid (in file) may wrong one from iOS and macOS (they have old
                // implementation)
                contactInFile.Birthdate = AddressBookControl.FormatBirthdate(contactInFile.Birthdate);
                string validUid = contactInFile.GenerateUid();
                if (!validUid.Equals(uid))
                {
                    uid = validUid;
                    contactInFile.Uid = validUid;
                }

                // validate contact
                if (!_patientDb.ValidateContact(contactInFile))
                {
                    _prescriptions.Renew();
                    return;
                }

                Contact contact = await _patientDb.GetContactByUid(uid);
                if (PrescriptionsTray.AutoSavingMode)
                {
                    // save/update contact from this .amk here. assume contact in .amk file as always new.
                    // (same as iOS and macOS version)
                    if (contact != null)
                    {   // update
                        await _patientDb.UpdateContact(contactInFile);
                        await _patientDb.LoadAllContacts();
                        contact = await _patientDb.GetContactByUid(uid);
                    }
                    else
                    {   // save as new
                        contactInFile.TimeStamp = Utilities.GetLocalTimeAsString(Contact.TIME_STAMP_DATE_FORMAT);
                        long? newId = await _patientDb.InsertContact(contactInFile);
                        if (newId != null && newId.Value > 0)
                            contactInFile.Id = newId;
                        contact = contactInFile;
                    }
                }

                this.ActiveContact = contact;
                _prescriptions.ActiveContact = ActiveContact;

                // TODO validate account
                this.ActiveAccount = _prescriptions.ActiveAccount;

                // TODO validate medications

                dialog = Utilities.MessageDialog(
                    String.Format(Properties.Resources.msgPrescriptionImportSuccess, filename), "", "OK");
            }

            EnableButton("CheckInteractionButton", _prescriptions.Medications.Count > 0);
            EnableButton("SavePrescriptionButton", Account.IsSet());
            EnableButton("SendPrescriptionButton", false);

            FillContactFields();
            FillAccountFields();
            FillPlaceDate();

            // update current entry and contacts list in addressbook
            var book = AddressBook.Content as AddressBookControl;
            if (book != null)
                book.Select(ActiveContact);

            // NOTE:
            // This method call of `SetActiveFileAsSelected` behaves strangely only here.
            // The assignment to `SelectedIndex` in this method clears
            // `ActiveContact.Id` value. whyyy? by GC? ... :'(
            var nowId = ActiveContact.Id;
            SetActiveFileAsSelected();
            ActiveContact.Id = nowId;

            if (dialog != null)
                dialog.ShowDialog();
        }

        private void SetActiveFileAsSelected()
        {
            var box = GetElementIn("FileNameList", RightArea) as ListBox;
            if (box != null)
            {
                for (var i = 0; i < box.Items.Count; i++)
                {
                    var item = box.Items[i] as FileItem;
                    if (item != null && item.IsValid &&
                        item.Name.Equals(_prescriptions.ActiveFileName) && item.Hash.Equals(_prescriptions.Hash))
                    {
                        box.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void SetActiveSectionAsSelected()
        {
            var box = GetElementIn("SectionTitleList", RightArea) as ListBox;
            if (box != null)
            {
                if (_currentSection == null)
                    return;

                for (var i = 0; i < box.Items.Count; i++)
                {
                    var item = box.Items[i] as TitleItem;
                    if (item != null && _currentSection.Equals(item.Id))
                    {
                        box.SelectedIndex = i;
                        break;
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
                viewType = DataContext as ViewType;
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
                    viewType = DataContext as ViewType;
                    Prescriptions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
            else if (name.Equals("Settings"))
            {
                ViewType viewType = DataContext as ViewType;
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
                    viewType = DataContext as ViewType;
                    Prescriptions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                viewType.HasSettings = true;
                this.DataContext = viewType;
                this.FlyoutMenu.IsOpen = false;
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

            Log.WriteLine(source.GetType().Name);
            var state = source.Name;

            if (state.Equals("Compendium"))
                SetState(UIState.State.Compendium);
            else if (state.Equals("Favorites"))
                SetState(UIState.State.Favorites);
            else if (state.Equals("Interactions"))
                SetState(UIState.State.Interactions);
            else if (state.Equals("Prescriptions"))
                SetState(UIState.State.Prescriptions);

            ResetSearchInDelay(-1);
        }

        private void QuerySelectButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            UIState.Query query = UIState.QueryBySourceName(source.Name.Replace("QuerySelectButton", ""));

            var q = _uiState.GetQuery(); // current
            if ((query == UIState.Query.Fulltext && query != q) ||
                (q == UIState.Query.Fulltext && query != q))
            {
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

            SearchTextBox.Focus();
            this.SearchTextBoxWaterMark = _uiState.SearchTextBoxWaterMark;

            UIState.State state = _uiState.GetState();
            Log.WriteLine("state: {0}", state);
            Log.WriteLine("query: {0}", query);

            _uiState.SetQuery(query);
            if (query == UIState.Query.Fulltext)
            {
                // only change data context (keep state)
                SetDataContext(state);
                SetFullTextSearchDataContext();

                if (state == UIState.State.Favorites)
                    ResetSearchInDelay(-1);
            }
            else {
                SetState(state);
                _sqlDb.UpdateSearchResults(_uiState);
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

        private void ReceivedCardResult(object sender, SmartCard.Result r) {
            this.Invoke(new Action(() => ReceivedCardResultMainThread(r)));
        }
        private async void ReceivedCardResultMainThread(SmartCard.Result r)
        {
            var book = AddressBook.Content as AddressBookControl;
            Contact existingContact = null;
            if (book != null)
            {
                existingContact = await book.ReceivedCardResult(r);
            }

            if (existingContact != null)
            {
                this.DataContext = new ViewType("Form", false);
                this.ActiveContact = existingContact;
                _prescriptions.ActiveContact = ActiveContact;
                _prescriptions.LoadFiles();
            } else {
                this.DataContext = new ViewType("Form", true);
            }
            FillContactFields();
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
            viewType = DataContext as ViewType;
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

            var box = GetElementIn("FileNameList", RightArea) as ListBox;
            if (box?.Items.Count > 0)
                box.SelectedIndex = -1;

            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);
            _prescriptions.Renew();

            // reload current entries
            var book = AddressBook.Content as AddressBookControl;
            this.ActiveContact = book?.CurrentEntry;
            _prescriptions.ActiveContact = ActiveContact;

            var card = ProfileCard.Content as ProfileCardControl;
            this.ActiveAccount = card?.CurrentEntry;
            this.ActiveAccount.Signature = null; // reset
            _prescriptions.ActiveAccount = ActiveAccount;

            FillContactFields();
            FillAccountFields();
            FillPlaceDate();

            EnableButton("CheckInteractionButton", false);
            EnableButton("SavePrescriptionButton", false);
            EnableButton("SendPrescriptionButton", false);
            e.Handled = true;
        }

        private async void CheckInteractionButton_Click(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            if (_prescriptions.Medications == null && _prescriptions.Medications.Count > 0)
                return;

            _interactions.RemoveAllArticles();
            string[] eancodes = _prescriptions.Medications.Select(m => m.Eancode).ToArray();
            List<Article> articles = await _sqlDb.FindArticlesByEans(eancodes);
            foreach (var a in articles)
            {
                if (a?.Id != null)
                    _interactions.AddArticle(a);
            }
            _interactions.ShowBasket();

            Interactions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
                // reset account (use always primary account)
                var card = ProfileCard.Content as ProfileCardControl;
                this.ActiveAccount = card?.CurrentEntry;
                _prescriptions.ActiveAccount = ActiveAccount;

                await _prescriptions.Save(asRewriting);

                Keyboard.ClearFocus();
                _prescriptions.LoadFiles();

                SetActiveFileAsSelected();

                FillPlaceDate();
                EnableButton("SavePrescriptionButton", false);
                EnableButton("SendPrescriptionButton", true);
            }
            e.Handled = true;
        }

        private async void SendPrescriptionButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null)
                return;

            if (e.ChangedButton == MouseButton.Left)
            {
                Log.WriteLine(source.Name);

                if (_prescriptions.ActiveFilePath != null)
                {
                    var filepath = _prescriptions.PickFile(_prescriptions.ActiveFilePath);
                    Log.WriteLine("filepath: {0}", filepath);
                    try
                    {
                        this.ShareFilePathOrThrow(filepath);
                    } catch (TypeLoadException exception)
                    {
                        Log.WriteLine("Cannot use ShareUtility {0}", exception);

                        // Use older API
                        MAPI mapi = new MAPI();
                        mapi.AddAttachment(filepath);
                        string title = Utilities.GetMailSubject(
                            ActiveContact.Fullname, ActiveContact.Birthdate, ActiveAccount.Fullname
                        );
                        mapi.SendMailPopup(title, Utilities.GetMailBody());
                    }
                }
            }
        }

        private void ShareFilePathOrThrow(string amkFilePath)
        {
            var s = new ShareUtility(amkFilePath, ActiveAccount, ActiveContact);
            s.Share();
        }

        private void PrintPrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            var prescription = new Prescription(_prescriptions.ActiveFileName, _prescriptions.PlaceDate)
            {
                ActiveContact = ActiveContact,
                ActiveAccount = ActiveAccount,
                Medications = _prescriptions.Medications,
                PageNumber = 1
            };
            Printer.PrintPrescription(prescription);
            e.Handled = true;
        }

        private void AddressBookControl_ClosingFinished(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as MahApps.Metro.Controls.Flyout;
            if (source == null)
                return;

            Log.WriteLine(source.Name);

            if (ActiveContact == null)
            {
                _prescriptions.ActiveContact = null;
                _prescriptions.Renew();
                _prescriptions.LoadFiles();
            }
            else
            {  // doesn't renew here (keep current medications)
                Log.WriteLine("ActiveContact.Uid: {0}", ActiveContact.Uid);
                _prescriptions.Hash = Utilities.GenerateUUID();
                _prescriptions.PlaceDate = null;
                _prescriptions.ActiveContact = ActiveContact;
                _prescriptions.LoadFiles();
            }

            // reset account
            var card = ProfileCard.Content as ProfileCardControl;
            this.ActiveAccount = card?.CurrentEntry;
            _prescriptions.ActiveAccount = ActiveAccount;

            FillContactFields();
            FillAccountFields();
            FillPlaceDate();

            var hasMedications = (_prescriptions.Medications.Count > 0);
            EnableButton("CheckInteractionButton", hasMedications);

            EnableButton("SendPrescriptionButton", false);
            if (ActiveAccount != null && ActiveContact != null)
            {
                if (hasMedications && Account.IsSet())
                    EnableButton("SavePrescriptionButton", true);
                else
                {
                    _prescriptions.HasChange = false;
                    EnableButton("SavePrescriptionButton", false);
                }
                EnableButton("PrintPrescriptionButton", false);
            }

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
                if (_prescriptions.ActiveFileName == null)
                {
                    _prescriptions.ActiveAccount = ActiveAccount;
                    FillAccountFields();
                }
                FillPlaceDate();

                var button = GetElementIn("OpenProfileCardButton", MainArea) as Button;
                if (button != null)
                    button.Visibility = Visibility.Collapsed;

                EnableButton("NewPrescriptionButton", true);
            }

            // Re:enable animations for next time
            source.AreAnimationsEnabled = true;
            e.Handled = true;
        }

        private void LoadAccountPicture()
        {
            try
            {
                Image image = GetElementIn("AccountPicture", MainArea) as Image;

                var signature = ActiveAccount?.Signature;
                Log.WriteLine("signature (length): {0}", signature.Length);
                if (signature != null && !signature.Equals(string.Empty))
                {
                    byte[] bytes = Convert.FromBase64String(signature);
                    using (MemoryStream m = new MemoryStream(bytes))
                    {
                        image.Source = BitmapFrame.Create(
                            m, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
                else
                {
                    if (image != null && Account.IsSet() && ActiveAccount != null)
                        Utilities.LoadPictureInto(image, ActiveAccount.PictureFile);
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is NotSupportedException || ex is NullReferenceException)
                    Log.WriteLine(ex.Message);
                else
                    throw ex;
            }
        }

        private void EnableButton(string name, bool isEnabled)
        {
            Log.WriteLine("name: {0}, isEnabled: {1}", name, isEnabled);
            if (name != null && name.Equals("PrintPrescriptionButton"))
            {
                this.PrintPrescriptionButton.IsEnabled = isEnabled;
                this.PrintPrescriptionButton.Cursor = isEnabled ? Cursors.Hand : Cursors.No;
                this.PrintPrescription.Foreground = isEnabled ? Brushes.Gray : Brushes.LightGray;
            }
            else
            {
                Button button = GetElementIn(name, MainArea) as Button;
                if (button != null)
                {
                    button.IsEnabled = isEnabled;
                    button.Cursor = isEnabled ? Cursors.Hand : Cursors.No;
                }
            }
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


	[ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDataTransferManagerInterOp
	{
		[PreserveSig]
		uint GetForWindow([In] IntPtr appWindow, [In] ref Guid riid, [Out] out DataTransferManager pDataTransferManager);

		[PreserveSig]
		uint ShowShareUIForWindow(IntPtr appWindow);
    }
}
