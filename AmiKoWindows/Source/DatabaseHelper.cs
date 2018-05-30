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
        private SQLiteConnection _conn;
        private string _dbPath;

        #region Public Methods
        public bool IsOpen()
        {
            return _conn.State == System.Data.ConnectionState.Open;
        }

        public async Task CreateDB(string dbPath, string dbSchema)
        {
            if (_conn != null)
                CloseDB();

            _dbPath = dbPath;

            SQLiteConnection.CreateFile(dbPath);
            await OpenDB(_dbPath);

            if (IsOpen()) {
                SQLiteCommand cmd = Command(dbSchema);
                cmd.ExecuteNonQuery();
            }
            CloseDB();
        }

        public async Task<SQLiteConnection> OpenDB(string dbPath)
        {
            if (_conn != null)
                return _conn;

            _dbPath = dbPath;

            await Task.Run(() =>
            {
                if (File.Exists(dbPath))
                {
                    _conn = new SQLiteConnection("Data Source=" + dbPath);
                    _conn.Open();
                }
                while (!IsOpen());
                return _conn;
            });
            return null;
        }

        public SQLiteConnection ReOpenIfNecessary()
        {
            if (!IsOpen())
                OpenDB(_dbPath).Wait();
            return _conn;
        }

        public void CloseDB()
        {
            _conn.Close();
            _conn = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public SQLiteCommand Command()
        {
            return new SQLiteCommand(_conn);
        }

        public SQLiteCommand Command(string text)
        {
            return new SQLiteCommand(text, _conn);
        }

        public async Task<long?> GetNumRecords(string table)
        {
            long? numRecords = 0;

            await Task.Run(() =>
            {
                using (SQLiteCommand cmd = Command("SELECT COUNT(*) FROM " + table))
                {
                    numRecords = cmd.ExecuteScalar() as long?;
                }
            });

            return numRecords;
        }
        #endregion
    }
}
