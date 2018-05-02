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
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class DatabaseHelper
    {
        private SQLiteConnection _db;
        private string _dbPath;

        #region Public Methods
        public bool IsOpen()
        {
            return _db.State == System.Data.ConnectionState.Open;
        }

        public async Task<SQLiteConnection> OpenDB(string dbPath)
        {
            if (_db != null)
                return _db;

            _dbPath = dbPath;

            await Task.Run(() =>
            {
                if (File.Exists(dbPath))
                {
                    _db = new SQLiteConnection("Data Source=" + dbPath);
                    _db.Open();
                }
                while (_db.State != System.Data.ConnectionState.Open) ;
                return _db;
            });
            return null;
        }

        public SQLiteConnection ReOpenIfNecessary()
        {
            if (_db.State != System.Data.ConnectionState.Open)
                OpenDB(_dbPath).Wait();
            return _db;
        }

        public void CloseDB()
        {
            _db.Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public SQLiteCommand Command()
        {
            return new SQLiteCommand(_db);
        }

        public SQLiteCommand Command(string text)
        {
            return new SQLiteCommand(text, _db);
        }

        public async Task<long?> GetNumRecords(string table)
        {
            long? numRecords = 0;

            await Task.Run(() =>
            {
                using (SQLiteCommand com = new SQLiteCommand("SELECT COUNT(*) FROM " + table, _db))
                {
                    numRecords = com.ExecuteScalar() as long?;
                }
            });

            return numRecords;
        }
        #endregion
    }
}
