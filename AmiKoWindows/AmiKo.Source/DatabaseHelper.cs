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
