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
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    public class PatientDb : INotifyPropertyChanged
    {
        #region Constants
        const string DATABASE_TABLE = "patients";

        // NOTE:
        // The schema should have consistency with macOS Version.
        //
        // See also below (v3.4.1),
        // https://github.com/zdavatz/amiko-osx/blob/8910324a74970d4b7e2b170fb000dbdda934451c/MLPatientDBAdapter.m#L87
        const string KEY_ID = "_id";
        const string KEY_TIME_STAMP = "time_stamp";
        const string KEY_UID = "uid";
        const string KEY_GIVEN_NAME = "given_name";
        const string KEY_FAMILY_NAME = "family_name";
        const string KEY_ADDRESS = "address";
        const string KEY_CITY = "city";
        const string KEY_ZIP = "zip";
        const string KEY_COUNTRY = "country";
        const string KEY_BIRTHDATE = "birthdate";
        const string KEY_GENDER = "gender";
        const string KEY_WEIGHT_KG = "weight_kg";
        const string KEY_HEIGHT_CM = "height_cm";
        const string KEY_PHONE = "phone";
        const string KEY_EMAIL = "email";

        private static readonly string[] DATABASE_COLUMNS = {
            KEY_ID,
            KEY_TIME_STAMP, KEY_UID,
            KEY_GIVEN_NAME, KEY_FAMILY_NAME, KEY_ADDRESS, KEY_CITY, KEY_ZIP, KEY_COUNTRY,
            KEY_BIRTHDATE, KEY_GENDER, KEY_WEIGHT_KG, KEY_HEIGHT_CM,
            KEY_PHONE, KEY_EMAIL
        };

        private static readonly string DATABASE_SCHEMA = String.Format(@"
            CREATE TABLE {0} (
                {1} INTEGER PRIMARY KEY AUTOINCREMENT,
                {2} TEXT NOT NULL,
                {3} TEXT NOT NULL,
                {4} TEXT,
                {5} TEXT,
                {6} TEXT,
                {7} INTEGER,
                {8} INTEGER,
                {9} INTEGER,
                {10} TEXT,
                {11} TEXT,
                {12} TEXT,
                {13} TEXT,
                {14} TEXT,
                {15} TEXT
            );",
            new string[] {DATABASE_TABLE}.Concat(DATABASE_COLUMNS).ToArray()
        );
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
            _db = new DatabaseHelper();

            if (!File.Exists(dbPath))
                await _db.CreateDB(dbPath, DATABASE_SCHEMA);
                await _db.OpenDB(dbPath);

                if (_db.IsOpen())
                {
                    long? numContacts = await _db.GetNumRecords(DATABASE_TABLE);
                    Log.WriteLine(">> OK: Opened sqlite db with {0} items located in {1}", numContacts, dbPath);
                }
                else
                {
                    // Cannot open patient sqlite database!
                    // Todo: generate friendly message (msgbox...)
                    Log.WriteLine(">> ERR: Unable to open sqlite db located in {0}", dbPath);
                }
        }

        public void Close()
        {
            if (_db != null)
                _db.CloseDB();
        }

        public Contact InitContact(Dictionary<string, string> values) {
            var contact = new Contact();
            var columns = DATABASE_COLUMNS.Where(k => k != KEY_ID && k != KEY_TIME_STAMP).ToArray();

            foreach (string name in columns)
            {
                string text = "";
                if (values.TryGetValue(name, out text))
                {
                    string propName = Utilities.ConvertSnakeCaseToTitleCase(name);
                    contact[propName] = text;
                }
            }
            // TODO
            contact["TimeStamp"] = "";
            return contact;
        }

        public bool SaveContact(Contact contact)
        {
            if (contact.Uid != null && !contact.Uid.Equals(string.Empty))
            { // update
                var cmd = _db.Command(
                    String.Format(@"SELECT {0} FROM {1} WHERE uid = '{2}' LIMIT 1;",
                        KEY_ID, DATABASE_TABLE, contact.Uid));
                var existingId = cmd.ExecuteScalar() as int?;
                if (existingId == null)
                    return false;

                // TODO
                return true;
            }
            else
            { // insert
                contact.Uid = contact.GenerateUid();
                var columns = DATABASE_COLUMNS.Where(k => k != KEY_ID).ToArray();
                var cmd = _db.Command(
                    String.Format(@"INSERT INTO {0} ({1}) VALUES ({2});",
                        DATABASE_TABLE,
                        String.Join(",", columns),
                        contact.Flatten(columns)
                    ));
                cmd.ExecuteNonQuery();
                return true;
            }
        }

        public async Task<bool> DeleteContact(long id)
        {
            bool result = false;
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        cmd.CommandText = String.Format(
                            @"DELETE FROM {0} WHERE {1} = '{2}';",
                            DATABASE_TABLE,
                            KEY_ID, id
                        );
                        // TODO
                        // check result
                        cmd.ExecuteNonQuery();
                        result = true;
                    }
                }
            });
            return result;
        }

        public async void UpdateSearchResults()
        {
            SearchResultItems.Clear();

            _foundContacts = await LoadAllContacts();
            SearchResultItems.AddRange(_foundContacts);
        }


        public async Task<List<Contact>> LoadAllContacts()
        {
            List<Contact> contacts = new List<Contact>();
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SQLiteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        cmd.CommandText = String.Format(
                            @"SELECT {0} FROM {1};", "*", DATABASE_TABLE);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                contacts.Add(CursorToContact(reader));
                        }
                    }
                }
            });
            Log.WriteLine("contacs.Count: {0}", contacts.Count);
            return contacts;
        }

        public bool ValidateField(string fieldName, string text)
        {
            if (fieldName == null)
                return false;

            // TODO
            // consider more appropriate limitations (length, formats etc.)
            int maxLength = 255;

            // required
            if (fieldName.Equals(KEY_GIVEN_NAME))
                return text != string.Empty && text.Length < maxLength;
            else if (fieldName.Equals(KEY_FAMILY_NAME))
                return text != string.Empty && text.Length < maxLength;
            else if (fieldName.Equals(KEY_ADDRESS))
                return text != string.Empty && text.Length < maxLength;
            else if (fieldName.Equals(KEY_CITY))
                return text != string.Empty && text.Length < maxLength;
            else if (fieldName.Equals(KEY_ZIP))
                return text != string.Empty && text.Length < maxLength;
            else if (fieldName.Equals(KEY_BIRTHDATE))
                return text != string.Empty && text.Length < maxLength;
            else if (fieldName.Equals(KEY_GENDER))
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Utilities.AppCultureInfoName());
                string[] values = {Properties.Resources.female, Properties.Resources.male,};
                return text != string.Empty && values.Contains(text);
            }

            // optional
            if (fieldName.Equals(KEY_COUNTRY))
                return text.Length < maxLength;
            else if (fieldName.Equals(KEY_WEIGHT_KG))
                return text.Length < maxLength;
            else if (fieldName.Equals(KEY_HEIGHT_CM))
                return text.Length < maxLength;
            else if (fieldName.Equals(KEY_PHONE))
                return text.Length < maxLength;
            else if (fieldName.Equals(KEY_EMAIL))
                return text.Length < maxLength;

            return false;
        }
        #endregion

        private Contact CursorToContact(SQLiteDataReader reader)
        {
            Contact contact = new Contact();

            contact.Id = reader[KEY_ID] as long?;
            contact.TimeStamp = reader[KEY_TIME_STAMP] as string;
            contact.Uid = reader[KEY_UID] as string;
            contact.GivenName = reader[KEY_GIVEN_NAME] as string;
            contact.FamilyName = reader[KEY_FAMILY_NAME] as string;
            contact.Address = reader[KEY_ADDRESS] as string;
            contact.Zip = reader[KEY_ZIP] as string;
            contact.Country = reader[KEY_COUNTRY] as string;
            contact.Birthdate = reader[KEY_BIRTHDATE] as string;

            var gender = reader[KEY_GENDER] as int?;
            if (gender != null)
                contact._Gender = gender.Value;

            var weightKg = reader[KEY_WEIGHT_KG] as int?;
            if (weightKg != null)
                contact._WeightKg = weightKg.Value;

            var heightCm = reader[KEY_HEIGHT_CM] as int?;
            if (heightCm != null)
                contact._HeightCm = heightCm.Value;

            contact.Phone = reader[KEY_PHONE] as string;
            contact.Email = reader[KEY_EMAIL] as string;
            return contact;
        }
    }
}
