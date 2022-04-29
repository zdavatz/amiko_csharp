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
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    public class PatientDb : INotifyPropertyChanged
    {
        #region Constants
        const string DATABASE_TABLE = "patients";

        // NOTE:
        // The field names in schema should have consistency with macOS/iOS Version.
        //
        // See also links below:
        // * https://github.com/zdavatz/amiko-osx/blob/8910324a74970d4b7e2b170fb000dbdda934451c/MLPatientDBAdapter.m#L87
        // * https://github.com/zdavatz/AmiKo-iOS/blob/49470597fb11a020206aa2051e7c629ac118be0b/AmiKoDesitin/MLPatientDBAdapter.m#L54
        const string KEY_ID = "_id";
        public const string KEY_TIME_STAMP = "time_stamp";
        public const string KEY_UID = "uid";

        public const string KEY_GIVEN_NAME = "given_name";
        public const string KEY_FAMILY_NAME = "family_name";
        public const string KEY_ADDRESS = "address";
        public const string KEY_CITY = "city";
        public const string KEY_ZIP = "zip";
        public const string KEY_COUNTRY = "country";
        public const string KEY_BIRTHDATE = "birthdate";
        public const string KEY_GENDER = "gender";
        public const string KEY_WEIGHT_KG = "weight_kg";
        public const string KEY_HEIGHT_CM = "height_cm";
        public const string KEY_PHONE = "phone";
        public const string KEY_EMAIL = "email";

        public static readonly Regex BIRTHDATE_NONDEVIDER_RGX = new Regex(@"[\-\/]", RegexOptions.Compiled);
        public static readonly Regex BIRTHDATE_ZEROPADDED_RGX = new Regex(@"(\A|\.)0*", RegexOptions.Compiled);
        public static readonly Regex BIRTHDATE_DATEFORMAT_RGX = new Regex(@"\d{2}\.\d{2}\.\d{4}", RegexOptions.Compiled);
        public static readonly Regex BIRTHDATE_NONZEROPAD_RGX = new Regex(@"(\A|\.)([1-9]{1})(?=\.)", RegexOptions.Compiled);

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
                {7} TEXT,
                {8} TEXT,
                {9} TEXT,
                {10} TEXT,
                {11} INTEGER,
                {12} REAL,
                {13} REAL,
                {14} TEXT,
                {15} TEXT
            );",
            new string[] {DATABASE_TABLE}.Concat(DATABASE_COLUMNS).ToArray()
        );

        private static readonly string SORT_KEYS = String.Format(
                @"{0}, {1}, {2}", KEY_GIVEN_NAME, KEY_FAMILY_NAME, KEY_ID);
        #endregion

        #region Private Fields
        private DatabaseHelper _db;
        private List<Contact> _foundContacts = new List<Contact>();

        private bool _onMemory = false; // csv or db
        #endregion

        public long Count {
            get {
                if (_foundContacts != null)
                    return _foundContacts.Count;
                return 0;
            }
        }

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
        private ItemsObservableCollection _contactListItems = new ItemsObservableCollection();
        public ItemsObservableCollection ContactListItems
        {
            get { return _contactListItems; }
            private set
            {
                if (value != _contactListItems)
                    _contactListItems = value;
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
                await LoadAllContacts();
                Log.WriteLine(">> OK: Opened patient db with {0} items located in {1}", Count, dbPath);
            }
            else
            {
                // Cannot open patient sqlite database!
                // Todo: generate friendly message (msgbox...)
                Log.WriteLine(">> ERR: Unable to open patient db located in {0}", dbPath);
            }
        }

        public void Close()
        {
            if (_db != null)
                _db.CloseDB();
        }

        public async Task<long> Search(string text, List<Contact> contacts)
        {
            _foundContacts.Clear();

            Log.WriteLine("_onMemory: {0}", _onMemory);
            if (_onMemory)
            {
                if (text == null || text.Equals(string.Empty))
                    _foundContacts = contacts;
                else
                    _foundContacts = await FindContactsByText(text, contacts);
            }

            UpdateContactList(_foundContacts);

            return Count;
        }

        public async Task<long> Search(string text)
        {
            _foundContacts.Clear();

            if (text == null || text.Equals(string.Empty))
                _foundContacts = await GetAllContacts();
            else
                _foundContacts = await FindContactsByText(text);

            UpdateContactList();

            return Count;
        }

        public Contact InitContact(Dictionary<string, string> values) {
            var contact = new Contact();
            var columns = DATABASE_COLUMNS.Where(k => k != KEY_ID && k != KEY_TIME_STAMP).ToArray();

            foreach (string name in columns)
            {
                string text = "";
                if (values.TryGetValue(name, out text))
                {
                    string propertyName = Utilities.ConvertSnakeCaseToTitleCase(name);
                    contact[propertyName] = text;
                }
            }
            // TODO
            contact["TimeStamp"] = "";
            return contact;
        }

        // Returns operations succeed or not
        public async Task<bool> UpsertContactByUid(Contact contact)
        {
            bool result = false;
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        string q;

                        q = String.Format(@"SELECT {0} FROM {1} WHERE {2} = @uid LIMIT 1;",
                            KEY_ID, DATABASE_TABLE, KEY_UID);
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        cmd.Parameters.AddWithValue("@uid", contact.Uid);
                        var existingId = cmd.ExecuteScalar() as long?;
                        if (existingId == null)
                        {
                            string[] columnNames = DATABASE_COLUMNS.Where(
                                k => k != KEY_ID).ToArray();
                            var parameters = columnNames.Select(c =>
                                String.Format("${0}", c)).ToArray();
                            q = String.Format(@"INSERT INTO {0} ({1}) VALUES ({2});",
                                DATABASE_TABLE, String.Join(",", columnNames), String.Join(",", parameters));
                            //Log.WriteLine("Query: {0}", q);
                            cmd.CommandText = q;
                            foreach (var item in contact.ToParameters(columnNames))
                                cmd.Parameters.AddWithValue(item.Key, item.Value);

                            cmd.ExecuteNonQuery();

                            return;
                        }
                        else
                        {
                            string[] columnNames = DATABASE_COLUMNS.Where(
                                k => k != KEY_ID && k != KEY_UID).ToArray();

                            var parameterPairs = columnNames.Select(c =>
                                String.Format("{0} = @{1}", c, c)).ToArray();
                            q = String.Format(@"UPDATE {0} SET {1} WHERE {2} = @uid;",
                                DATABASE_TABLE, String.Join(",", parameterPairs), KEY_UID);
                            Log.WriteLine("Query: {0}", q);
                            cmd.CommandText = q;
                            foreach (var item in contact.ToParameters(columnNames))
                                cmd.Parameters.AddWithValue(item.Key, item.Value);

                            cmd.Parameters.AddWithValue("@uid", contact.Uid);
                            int rows = cmd.ExecuteNonQuery();
                            if (rows == 1)
                                result = true;
                        }
                    }
                }
            });
            return result;
        }

        // Returns operations succeed or not
        public async Task<bool> UpdateContact(Contact contact)
        {
            bool result = false;
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        string q;

                        q = String.Format(@"SELECT {0} FROM {1} WHERE {2} = @uid LIMIT 1;",
                            KEY_ID, DATABASE_TABLE, KEY_UID);
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        cmd.Parameters.AddWithValue("@uid", contact.Uid);
                        var existingId = cmd.ExecuteScalar() as long?;
                        if (existingId == null)
                        {
                            result = false;
                            return;
                        }

                        contact.TimeStamp = Utilities.GetLocalTimeAsString(Contact.TIME_STAMP_DATE_FORMAT);
                        string[] columnNames = DATABASE_COLUMNS.Where(
                            k => k != KEY_ID && k != KEY_UID).ToArray();

                        var parameterPairs = columnNames.Select(c =>
                            String.Format("{0} = @{1}", c, c)).ToArray();
                        q = String.Format(@"UPDATE {0} SET {1} WHERE {2} = @id;",
                            DATABASE_TABLE, String.Join(",", parameterPairs), KEY_ID);
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        foreach (var item in contact.ToParameters(columnNames))
                            cmd.Parameters.AddWithValue(item.Key, item.Value);

                        cmd.Parameters.AddWithValue("@id", existingId.Value);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 1)
                            result = true;
                    }
                }
            });
            return result;
        }

        public async Task UpdateContactUid(Contact contact)
        {
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        string q = String.Format(@"UPDATE {0} SET {1} = @newUid WHERE {2} = @id;",
                            DATABASE_TABLE, KEY_UID, KEY_ID);
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;

                        cmd.Parameters.AddWithValue("@id", contact.Id);
                        cmd.Parameters.AddWithValue("@newUid", contact.Uid);
                        int rows = cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        // Returns inserted new id, if insert succeeds
        public async Task<long?> InsertContact(Contact contact)
        {
            long? id = null;
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        string q;

                        contact.Uid = contact.GenerateUid();

                        q = String.Format(@"SELECT {0} FROM {1} WHERE {2} = @uid LIMIT 1;",
                            KEY_ID, DATABASE_TABLE, KEY_UID);
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        cmd.Parameters.AddWithValue("@uid", contact.Uid);
                        var existingId = cmd.ExecuteScalar() as long?;
                        if (existingId != null)
                        {
                            // already exists
                            id = null;
                            return;
                        }

                        contact.TimeStamp = Utilities.GetLocalTimeAsString(Contact.TIME_STAMP_DATE_FORMAT);
                        string[] columnNames = DATABASE_COLUMNS.Where(
                            k => k != KEY_ID).ToArray();
                        var parameters = columnNames.Select(c =>
                            String.Format("${0}", c)).ToArray();
                        q = String.Format(@"INSERT INTO {0} ({1}) VALUES ({2});",
                            DATABASE_TABLE, String.Join(",", columnNames), String.Join(",", parameters));
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        foreach (var item in contact.ToParameters(columnNames))
                            cmd.Parameters.AddWithValue(item.Key, item.Value);

                        int rows = cmd.ExecuteNonQuery();
                        //Log.WriteLine("rows: {0}", rows);
                        if (rows == 1)
                        {
                            long insertedId = GetLastInsertId();
                            //Log.WriteLine("insertedId: {0}", insertedId);
                            if (insertedId > 0)
                                id = insertedId;
                        }
                    }
                }
            });
            return id;
        }

        public async Task<bool> DeleteContact(long id)
        {
            bool result = false;
            await Task.Run(() =>
            {
                if (!_onMemory && _db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        var q = String.Format(
                            @"DELETE FROM {0} WHERE {1} = @id;",
                            DATABASE_TABLE, KEY_ID
                        );
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        cmd.Parameters.AddWithValue("@id", id);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 1)
                            result = true;
                    }
                }
            });
            return result;
        }

        public async Task<bool> DeleteContactByUid(string uid)
        {
            bool result = false;
            await Task.Run(() =>
            {
                if (!_onMemory && _db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        var q = String.Format(
                            @"DELETE FROM {0} WHERE {1} = @uid;",
                            DATABASE_TABLE, KEY_UID
                        );
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        cmd.Parameters.AddWithValue("@uid", uid);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 1)
                            result = true;
                    }
                }
            });
            return result;
        }

        public bool ValidateContact(Contact contact)
        {
            string[] columnNames = DATABASE_COLUMNS.Where(
                k => k != KEY_ID && k != KEY_UID).ToArray();

            bool hasError = false;
            foreach (var item in contact.ToParameters(columnNames))
            {
                var columnName = item.Key.Replace("@", "");
                if (!ValidateField(columnName, item.Value))
                {
                    hasError = true;
                    break;
                }
            }
            return hasError;
        }

        public void UpdateContactList()
        {
            _onMemory = false;
            ContactListItems.Clear();
            ContactListItems.AddRange(_foundContacts);
        }

        public void UpdateContactList(List<Contact> contacts)
        {
            _onMemory = true;
            _foundContacts = contacts;
            ContactListItems.Clear();
            ContactListItems.AddRange(contacts);
        }

        public async Task<long> LoadAllContacts()
        {
            _onMemory = false;
            _foundContacts.Clear();
            _foundContacts = await GetAllContacts();

            return Count;
        }

        public async Task<Contact> GetContactById(long? id)
        {
            Contact contact = null;

            if (id == null)
                return contact;

            await Task.Run(() =>
            {
                if (_onMemory && _foundContacts != null)
                {
                    try {
                        contact = _foundContacts.Where(c => {
                            return (c.Id == id);
                        }).Single();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // not found
                        Log.WriteLine(ex.Message);
                    }
                }
                else if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        var q = String.Format(
                            @"SELECT * FROM {0} WHERE {1} = @id LIMIT 1;",
                            DATABASE_TABLE, KEY_ID);
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        cmd.Parameters.AddWithValue("@id", id);
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                contact = CursorToContact(reader);
                        }
                    }
                }
            });
            return contact;
        }

        public async Task<Contact> GetContactByUid(string uid)
        {
            Contact contact = null;

            if (uid == null || uid.Equals(string.Empty))
                return contact;

            await Task.Run(() =>
            {
                if (_onMemory && _foundContacts != null)
                {
                    try {
                        contact = _foundContacts.Where(c => {
                            return (c.Uid != null && c.Uid.Equals(uid));
                        }).Single();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // not found
                        Log.WriteLine(ex.Message);
                    }
                }
                else if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        var q = String.Format(
                            @"SELECT * FROM {0} WHERE {1} = @uid LIMIT 1;",
                            DATABASE_TABLE, KEY_UID);
                        cmd.CommandText = q;
                        cmd.Parameters.AddWithValue("@uid", uid);
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                contact = CursorToContact(reader);
                        }
                    }
                }
            });
            return contact;
        }

        public async Task<List<Contact>> GetContactsByUids(List<string> uids)
        {
            List<Contact> contacts = new List<Contact>();

            if (uids == null || uids.Count == 0)
                return contacts;

            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        var uidsStr = "(" + String.Join(",", uids.Select(s=> "'" + s + "'")) + ")";
                        var q = String.Format(
                            @"SELECT * FROM {0} WHERE {1} IN {2} LIMIT 1;",
                            DATABASE_TABLE, KEY_UID, uidsStr);
                        cmd.CommandText = q;
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var contact = CursorToContact(reader);
                                contacts.Add(contact);
                            }
                        }
                    }
                }
            });
            return contacts;
        }

        public async Task<List<Contact>> GetAllContacts()
        {
            List<Contact> contacts = new List<Contact>();
            await Task.Run(() =>
            {
                if (_onMemory && _foundContacts != null)
                    contacts = _foundContacts;
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        var q = String.Format(
                            @"SELECT * FROM {0} ORDER BY {1};", DATABASE_TABLE, SORT_KEYS);
                        //Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;
                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                contacts.Add(CursorToContact(reader));
                        }
                    }
                }
            });
            return contacts;
        }

        public async Task<List<Contact>> FindContactsByText(string text, List<Contact> targets)
        {
            List<Contact> contacts = new List<Contact>();
            await Task.Run(() =>
            {
                if (targets != null)
                {
                    try {
                        if (text == null || text.Equals(string.Empty))
                            contacts = targets;
                        else
                        {
                            var t = text.ToLower();
                            contacts = targets.Where(c => {
                                return (c != null &&
                                    ((c.GivenName != null && c.GivenName.ToLower().Contains(t)) ||
                                     (c.FamilyName != null && c.FamilyName.ToLower().Contains(t)) ||
                                     (c.City != null && c.City.ToLower().Contains(t)) ||
                                     (c.Zip != null && c.Zip.ToLower().Contains(t)))
                                );
                            }).ToList();
                        }
                    }
                    catch (InvalidOperationException ex)
                    {   // not found
                        Log.WriteLine(ex.Message);
                    }
                }
            });
            Log.WriteLine("contacts.Length (csv): {0}", contacts.Count);
            return contacts;
        }

        public async Task<List<Contact>> FindContactsByText(string text)
        {
            List<Contact> contacts = new List<Contact>();
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();

                        string[] texts = text.Split(' ').Where(t => !t.Equals("")).Distinct().ToArray();
                        var conditions = BuildSearchConditionsForTexts(texts);
                        var q = String.Format(
                            @"SELECT * FROM {0} WHERE {1} ORDER BY {2};", DATABASE_TABLE, conditions, SORT_KEYS);
                        Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;

                        for (var i = 0; i < texts.Length; i++)
                        {
                            cmd.Parameters.AddWithValue(String.Format("@t{0}", i),
                                String.Format("%{0}%", texts[i]));
                        }

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                contacts.Add(CursorToContact(reader));
                        }
                    }
                }
            });
            Log.WriteLine("contacts.Length (db): {0}", contacts.Count);
            return contacts;
        }

        public async Task<Dictionary<string, DateTime>> GetTimestampsOfContacts()
        {
            var contacts = new Dictionary<string, DateTime>();
            await Task.Run(() =>
            {
                if (_db.IsOpen())
                {
                    using (SqliteCommand cmd = _db.Command())
                    {
                        _db.ReOpenIfNecessary();
                        
                        var q = String.Format(
                            @"SELECT {0}, {1} FROM {2};", KEY_UID, KEY_TIME_STAMP, DATABASE_TABLE);
                        Log.WriteLine("Query: {0}", q);
                        cmd.CommandText = q;

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var uid = reader[KEY_UID] as string;
                                var stringValue = reader[KEY_TIME_STAMP] as string;
                                DateTime timestamp;
                                try
                                {
                                    timestamp = DateTime.ParseExact(stringValue, Contact.TIME_STAMP_DATE_FORMAT, null);
                                } catch (FormatException _)
                                {
                                    timestamp = DateTime.ParseExact(stringValue, Contact.OLD_TIME_STAMP_DATE_FORMAT, null);
                                }
                                contacts[uid] = timestamp;
                            }
                        }
                    }
                }
            });
            Log.WriteLine("contacts.Length (db): {0}", contacts.Count);
            return contacts;
        }

        public bool ValidateField(string columnName, string text)
        {
            if (columnName == null)
                return false;

            // TODO
            // consider more appropriate limitations (length, formats etc.)
            int maxLength = 255;

            // required
            if (columnName.Equals(KEY_GIVEN_NAME))
                return text != string.Empty && text.Length < maxLength;
            else if (columnName.Equals(KEY_FAMILY_NAME))
                return text != string.Empty && text.Length < maxLength;
            else if (columnName.Equals(KEY_ADDRESS))
                return text != string.Empty && text.Length < maxLength;
            else if (columnName.Equals(KEY_CITY))
                return text != string.Empty && text.Length < maxLength;
            else if (columnName.Equals(KEY_ZIP))
                return text != string.Empty && text.Length < maxLength;
            else if (columnName.Equals(KEY_BIRTHDATE))
            {
                bool valid = false;
                valid = text != string.Empty && text.Length < maxLength;
                if (valid)
                {
                    MatchCollection matches;
                    matches = BIRTHDATE_NONDEVIDER_RGX.Matches(text);
                    if (matches.Count > 0)
                        valid = false;
                    else
                    {
                        // Rads zero as needed
                        text = BIRTHDATE_NONZEROPAD_RGX.Replace(text, "${1}0${2}");
                        matches = BIRTHDATE_DATEFORMAT_RGX.Matches(text);
                        if (matches.Count < 1)
                            valid = false;
                        else
                        {
                            DateTime _;
                            valid = DateTime.TryParseExact(text, "dd.MM.yyyy",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
                        }
                    }
                }
                return valid;
            }
            else if (columnName.Equals(KEY_GENDER))
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Utilities.AppCultureInfoName());
                string[] values = {Properties.Resources.female, Properties.Resources.male,};
                return text != string.Empty && values.Contains(text);
            }

            // optional
            if (columnName.Equals(KEY_COUNTRY))
                return text.Length < maxLength;
            else if (columnName.Equals(KEY_WEIGHT_KG))
                return text.Length < maxLength;
            else if (columnName.Equals(KEY_HEIGHT_CM))
                return text.Length < maxLength;
            else if (columnName.Equals(KEY_PHONE))
                return text.Length < maxLength;
            else if (columnName.Equals(KEY_EMAIL))
                // TODO
                return text.Length < maxLength;

            return false;
        }
        #endregion

        private long GetLastInsertId()
        {
            long result = -1;

            if (_db.IsOpen())
            {
                using (SqliteCommand cmd = _db.Command())
                {
                    _db.ReOpenIfNecessary();
                    var q = String.Format(
                        @"SELECT last_insert_rowid() FROM {0};",
                        DATABASE_TABLE);
                    //Log.WriteLine("Query: {0}", q);
                    cmd.CommandText = q;
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            result = (long)reader.GetInt64(0);
                    }
                }
            }
            return result;
        }

        private string BuildSearchConditionsForTexts(string[] texts)
        {
            var result = "";
            if (texts == null || texts.Length == 0)
                result += "1 = 1";
            else
            {
                string[] conditions = new string[texts.Length];
                for (var i = 0; i < texts.Length; i++)
                {
                    conditions[i] = String.Format(
                        @"({0} LIKE @t{4} OR {1} LIKE @t{4} OR {2} LIKE @t{4} OR {3} LIKE @t{4})",
                            KEY_FAMILY_NAME, KEY_GIVEN_NAME, KEY_CITY, KEY_ZIP, i);
                }
                result += String.Join(" AND ", conditions);
            }
            return result;
        }

        private Contact CursorToContact(SqliteDataReader reader)
        {
            Contact contact = new Contact();

            contact.Id = reader[KEY_ID] as long?;
            contact.TimeStamp = reader[KEY_TIME_STAMP] as string;
            contact.Uid = reader[KEY_UID] as string;

            contact.GivenName = reader[KEY_GIVEN_NAME] as string;
            contact.FamilyName = reader[KEY_FAMILY_NAME] as string;
            contact.Address = reader[KEY_ADDRESS] as string;
            contact.City = reader[KEY_CITY] as string;
            contact.Zip = reader[KEY_ZIP] as string;
            contact.Country = reader[KEY_COUNTRY] as string;
            contact.Birthdate = reader[KEY_BIRTHDATE] as string;

            var gender = reader[KEY_GENDER] as long?;
            if (gender != null)
                contact.RawGender = Convert.ToInt32(gender.Value);

            // It seems that REAL values should be casted to double once :'(
            var weightKg = reader[KEY_WEIGHT_KG] as double?;
            if (weightKg != null)
                contact.RawWeightKg = (float)weightKg.Value;
            var heightCm = reader[KEY_HEIGHT_CM] as double?;
            if (heightCm != null)
                contact.RawHeightCm = (float)heightCm.Value;

            contact.Phone = reader[KEY_PHONE] as string;
            contact.Email = reader[KEY_EMAIL] as string;
            return contact;
        }
    }
}
