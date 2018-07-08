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
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AmiKoWindows
{
    public partial class Prescription : Page, INotifyPropertyChanged
    {
        public double lMargin = 72.0;
        public double tMargin = 52.0;
        public double rMargin = 72.0;
        public double bMargin = 52.0;

        public double MedicationListBoxMaxHeight = 600;

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dependency Properties
        private CommentItemsObservableCollection _medicationListItems = new CommentItemsObservableCollection();
        public CommentItemsObservableCollection MedicationListItems
        {
            get { return _medicationListItems; }
            private set
            {
                if (value != _medicationListItems)
                    _medicationListItems = value;
            }
        }
        #endregion

        public Contact ActiveContact { get; set; }
        public Account ActiveAccount { get; set; }
        public List<Medication> Medications { get; set; }

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public Prescription(string filename, string placeDate)
        {
            this.Initialized += delegate
            {
                this.DataContext = this;
            };

            InitializeComponent();

            this.Main.Margin = new Thickness(lMargin, tMargin, rMargin, bMargin);

            this.FileName.Text = filename;
            this.PlaceDate.Text = placeDate;
        }

        public Medication PopMedication()
        {
            if (Medications == null || Medications.Count < 1)
                return null;

            var i = Medications.Count - 1;
            Medication medication = Medications[i];
            Medications.RemoveAt(i);
            return medication;
        }

        public void UpdateMedicationList()
        {
            MedicationListItems.Clear();
            MedicationListItems.AddRange(Medications);
        }

        public void Number()
        {
            this.PageText.Text = String.Format(
                Properties.Resources.page, PageNumber, TotalPages);
        }

        public void SetAccountPicture()
        {
            try
            {
                Image image = this.AccountPicture as Image;
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

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Log.WriteLine(e.ToString());
        }

        private void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Log.WriteLine(e.ToString());
        }
    }
}
