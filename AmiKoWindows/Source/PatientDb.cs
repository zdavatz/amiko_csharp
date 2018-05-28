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
using System.Data.SQLite;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    public class PatientDb : INotifyPropertyChanged
    {
        #region Constants
        const string KEY_ROWID = "_id";

        const string DATABASE_TABLE = "patientdb";
        #endregion

        #region Private Fields
        private DatabaseHelper _db;
        private List<Contact> _foundContacts = new List<Contact>();
        #endregion

        #region Event Handlers
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
        #endregion

        #region Dependency Properties
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
        #endregion

        #region Public Methods
        public async void Init()
        {
            string dbPath = Utilities.PatientDBPath();
            if (File.Exists(dbPath))
            {
                _db = new DatabaseHelper();
                await _db.OpenDB(dbPath);
                long? numContacts = await _db.GetNumRecords(DATABASE_TABLE);
                Console.Out.WriteLine(">> OK: Opened sqlite db with {0} items located in {1}", numContacts, dbPath);
            }
            else
            {
                // Cannot open patient sqlite database!
                // Todo: generate friendly message (msgbox...)
                Console.Out.WriteLine(">> ERR: Unable to open sqlite db located in {0}", dbPath);
            }
        }

        public void Close()
        {
            if (_db != null)
                _db.CloseDB();
        }

        public void UpdateSearchResults(UIState state)
        {
            SearchResultItems.Clear();
            //SearchResultItems.AddRange(state, _foundContacts);
        }
        #endregion
    }
}
