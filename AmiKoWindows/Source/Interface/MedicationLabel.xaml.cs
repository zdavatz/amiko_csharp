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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AmiKoWindows
{
    public partial class MedicationLabel : Page, INotifyPropertyChanged
    {
        public double lMargin = 8.0;
        public double tMargin = 3.0;
        public double rMargin = 8.0;
        public double bMargin = 3.0;

        public static readonly Regex PACKAGE_KEYWORD_RGX = new Regex(@"\[([\W\S]*)\]$", RegexOptions.Compiled);
        public static readonly Regex PACKAGE_PRICE_RGX = new Regex(@"PP\s([\d\.]+),?\s", RegexOptions.Compiled);

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private Contact _ActiveContact;
        public Contact ActiveContact {
            get { return this._ActiveContact; }
            set {
                this._ActiveContact = value;
                OnPropertyChanged("ActiveContact");
            }
        }
        private Account _ActiveAccount;
        public Account ActiveAccount {
            get { return this._ActiveAccount; }
            set {
                this._ActiveAccount = value;
                OnPropertyChanged("ActiveAccount");
            }
        }
        private Medication _Medication;
        public Medication Medication {
            get { return this._Medication; }
            set {
                this._Medication = value;
                OnPropertyChanged("Medication");
            }
        }

        private string PlaceDate;

        public MedicationLabel(string placeDate)
        {
            this.Initialized += delegate
            {
                this.DataContext = this;
            };


            InitializeComponent();

            this.PlaceDate = placeDate;

            if (PlaceDate != null && !PlaceDate.Equals(string.Empty))
                this.LabelTitle.Text = PlaceDate.Substring(0, PlaceDate.Length - 10); // default title
        }

        public void Layout()
        {
            SetTitle();

            if (ActiveContact != null)
                this.ContactInfo.Text = String.Format(
                    "{0}, {1} {2}", ActiveContact.Fullname, Properties.Resources.born, ActiveContact.Birthdate);

            Log.WriteLine("Package: {0}", Medication.Package);
            if (Medication == null || Medication.Package == null || Medication.Package.Equals(string.Empty))
                return;

            // title
            var words = Medication.Package.Split(',');
            if (words.Length > 1)
                this.MedicationTitle.Text = Medication.Package.Split(',')[0];
            else
                this.MedicationTitle.Text = Medication.Package;

            // comment
            this.Comment.Text = Medication.Comment;

            Match m;
            // keyword
            m = PACKAGE_KEYWORD_RGX.Match(Medication.Package);
            if (m != null)
                this.Keyword.Text = m.Value;
            // price
            m = PACKAGE_PRICE_RGX.Match(Medication.Package);
            if (m != null && m.Success)
                this.Price.Text = String.Format("CHF {0}", m.Groups[1]);

            UpdateLayout();
            BringIntoView();
        }

        private void SetTitle()
        {
            var title = "";

            var fullname = ActiveAccount?.Fullname;
            if (fullname != null)
                title = fullname;

            var defaultText = LabelTitle.Text;
            if (defaultText != null && !defaultText.Equals(string.Empty))
                title = String.Format("{0} - {1}", title, this.LabelTitle.Text);

            this.LabelTitle.Text = title;
        }
    }
}
