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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;


namespace AmiKoWindows
{
    using ControlExtensions;

    /// <summary>
    /// View controls for doctor's (operator) profile and signature.
    /// </summary>
    public partial class ProfileCardControl : UserControl, INotifyPropertyChanged
    {
        // visible fields
        static string[] profileFields = {
            "Title", "GivenName", "FamilyName", "Address", "City", "Zip", "Phone", "Email",
            "Picture",
        };

        #region Private Fields
        MainWindow _mainWindow;
        MahApps.Metro.Controls.Flyout _parent;
        #endregion

        #region Public Fields
        private Operator _CurrentEntry;
        public Operator CurrentEntry {
            get { return _CurrentEntry; }
            set {
                _CurrentEntry = value;
                OnPropertyChanged("CurrentEntry");
            }
        }

        private string _PictureFile;
        public string PictureFile {
            get { return _PictureFile; }
            set {
                _PictureFile = value;
                OnPropertyChanged("PictureFile");
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

        public ProfileCardControl()
        {
            this.Initialized += delegate
            {
                // This block is called after InitializeComponent
                this.DataContext = this;
            };

            if (Properties.Settings.Default.Operator == null)
                Properties.Settings.Default.Operator = new Operator();
            else
                Properties.Settings.Default.Operator.Reload();

            InitializeComponent();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(e.ToString());

            this.CurrentEntry = Properties.Settings.Default.Operator;


            var path = Utilities.OperatorPictureFilePath();
            if (!File.Exists(path))
                SetUserDefaultPicture(path);
            else
                this.PictureFile = path;

            if (this.PictureFile == null || this.PictureFile.Equals(string.Empty) || !File.Exists(this.PictureFile))
                EnableDeletePictureButton(false);
            else
                LoadPicture();
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _parent = this.Parent as MahApps.Metro.Controls.Flyout;
            _parent.AreAnimationsEnabled = false;

            var isVisible = e.NewValue as bool?;
            if (isVisible != null && isVisible.Value)
                _mainWindow = Window.GetWindow(_parent.Parent) as AmiKoWindows.MainWindow;
            else
                _mainWindow = null;
        }

        #region Actions
        private void Title_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void GivenName_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void FamilyName_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Address_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void City_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Zip_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Phone_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void Email_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            ValidateField(box);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);
            var valid = ValidateFields();

            if (valid)
            {
                Properties.Settings.Default.Operator = this.CurrentEntry;
                Properties.Settings.Default.Operator.Save();
                Properties.Settings.Default.Save();
            }

            if (_parent != null)
                _parent.IsOpen = !valid;
        }

        private void SelectPictureButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = String.Format(
                "{0} | {1}", "Image Files (*.gif, *.jpg, *.jpeg, *.png)", "*.gif; *.jpg; *.jpeg; *.png");
            dialog.DefaultExt = ".png";
            var result = dialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    this.PictureFile = ImportPicture(dialog.FileName, Utilities.OperatorPictureFilePath());
                    LoadPicture();
                    ValidateField(this.Picture);
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }
        }

        private void DeletePictureButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(this.PictureFile))
                File.Delete(this.PictureFile);

            this.PictureFile = null;
            ValidateField(this.Picture);

            LoadPicture();
        }
        #endregion

        // Returns dictionary contains key (propertyName) and value
        private Dictionary<string, string> GetProfileValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string field in profileFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                if (element is TextBox)
                {
                    var box = element as TextBox;
                    if (box != null)
                        values.Add(box.Name, box.Text);
                }
                else if (element is Image)
                {
                    var img = element as Image;
                    if (img != null)
                        values.Add(img.Name, img.Source.ToString());
                }
            }
            return values;
        }

        // Returns the input is valid or not
        private bool ValidateField(FrameworkElement element)
        {
            bool hasError = false;

            if (element == null)
                return hasError;

            if (element is TextBox)
            {
                var box = element as TextBox;
                // Check text using Operator's validation method
                hasError = !Operator.ValidateProperty(box.Name, box.Text);
                this.FeedbackField<TextBox>(box, hasError);
            }
            else if (element is Image)
            {
                var img = element as Image;
                if (img == null)
                    hasError = true;
                else
                {
                    if (img.Source == null || this.PictureFile == null || this.PictureFile.Equals(string.Empty) ||
                        !img.Source.ToString().Contains(Path.GetFileName(this.PictureFile)) || !File.Exists(this.PictureFile))
                        hasError = true;

                    this.FeedbackField<Image>(img, hasError);
                }
            }
            else
                hasError = true; // unknown

            return !hasError;
        }

        private bool ValidateFields()
        {
            bool hasError = false;
            foreach (string field in profileFields)
            {
                var element = this.FindName(field) as FrameworkElement;
                var result = ValidateField(element);
                if (!hasError)
                    hasError = !result;
            }
            Log.WriteLine("hasError: {0}", hasError);

            ShowMessage(hasError);
            return !hasError;
        }

        private void LoadPicture()
        {
            try
            {
                if (this.PictureFile != null && !this.PictureFile.Equals(string.Empty) && File.Exists(this.PictureFile))
                {
                    var source = new BitmapImage();
                    source.BeginInit();
                    source.CacheOption = BitmapCacheOption.OnLoad;
                    source.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    source.UriSource = new Uri(this.PictureFile, UriKind.Absolute);
                    source.EndInit();
                    this.Picture.Source = source;

                    EnableDeletePictureButton(true);
                }
                else
                    throw new IOException(String.Format("{0} does not exist", this.PictureFile));
            }
            catch (IOException e)
            {
                Log.WriteLine(e.Message);
                this.Picture.Source = DependencyProperty.UnsetValue as System.Windows.Media.ImageSource;
                EnableDeletePictureButton(false);
            }
        }

        private void ShowMessage(bool hasError)
        {
            this.FeedbackMessage(this.SaveProfileFailureMessage, hasError);
        }

        // Returns imported output file path
        private string ImportPicture(string inputFile, string outputFile)
        {
            try
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);

                using (var input = File.OpenRead(inputFile))
                using (var output = File.Create(outputFile))
                {
                    Utilities.ResizeImageFileAsPng(input, output, 200, 200);
                }
                return outputFile;
            }
            catch (IOException ex)
            {
                Log.WriteLine(ex.Message);
                return null;
            }
        }

        // Copy/Set current system user's avatar as default picture
        private void SetUserDefaultPicture(string outputFile)
        {
            // `username = null` means current user
            string avatarFile = Utilities.GetUserAvatarFilePath(null);
            if (File.Exists(avatarFile))
                this.PictureFile = ImportPicture(avatarFile, outputFile);
        }

        private void EnableDeletePictureButton(bool isEnabled)
        {
            this.DeletePictureButton.IsEnabled = isEnabled;
            var image = this.DeletePictureButton.Content as FontAwesome.WPF.ImageAwesome;
            if (image != null)
                if (isEnabled)
                    image.Foreground = Brushes.Black;
                else
                    image.Foreground = Brushes.LightGray;
        }
    }
}
