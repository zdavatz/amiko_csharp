/*
Copyright (c) 2016 Max Lungarella <cybrmx@gmail.com>

This file is part of AmiKoWindows.

AmiKoDesk is free software: you can redistribute it and/or modify
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
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class UpdateDb : INotifyPropertyChanged
    {
        #region Private Fields
        string _filename;
        long _filesize;
        #endregion

        #region Properties
        public object Errors
        {
            get;
            private set;
        }
        #endregion

        #region Class Extensions
        /**
         * Extend from List<T> and add a custom Add method to initialize T
         */
        public class TupleList<T1, T2, T3> : List<Tuple<T1, T2, T3>>
        {
            public void Add(T1 item, T2 item2, T3 item3)
            {
                Add(new Tuple<T1, T2, T3>(item, item2, item3));
            }
        }
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dependency Properties
        // Source object used for data binding
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    OnPropertyChanged("Text");
                }
            }
        }

        private double _currentProgress;
        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                if (value != _currentProgress)
                {
                    _currentProgress = value;
                    OnPropertyChanged("CurrentProgress");
                }
            }
        }

        private string _buttonContent;
        public string ButtonContent
        {
            get { return _buttonContent; }
            set
            {
                if (value != _buttonContent)
                {
                    _buttonContent = value;
                    OnPropertyChanged("ButtonContent");
                }
            }
        }

        #endregion

        #region Private Methods
        private Task StartDownloadAndExtract(string title, string url, string filename)
        {
            string filepath = Path.Combine(Utilities.AppRoamingDataFolder(), filename);
            // Starts task backed by background thread (located in thread pool)
            return Task.Run(() =>
            {
                // Download
                try
                {
                    var uri = new System.Uri(url);
                    using (WebClient wb = new WebClient())
                    {
                        wb.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                        wb.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadDataCompleted);
                        wb.DownloadFileAsync(uri, filepath);
                        while (wb.IsBusy) ;
                        _filename = filename;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to download file: {0}", filename);
                }

                // Decompress if necessary
                if (_filename.EndsWith("zip"))
                {
                    Text = string.Format("Unzipping {0}... ", _filename.Replace(".zip", ""));
                    string unzippedFilepath = filepath.Replace(".zip", "");
                    if (File.Exists(unzippedFilepath))
                        File.Delete(unzippedFilepath);
                    // Unzip
                    ZipFile.ExtractToDirectory(filepath, Utilities.AppRoamingDataFolder());
                    // Remove file
                    if (File.Exists(filepath))
                        File.Delete(filepath);
                 }

            });
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = (double)e.BytesReceived; // = double.Parse(e.BytesReceived.ToString());
            double totalBytes = (double)e.TotalBytesToReceive; // = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            Text = string.Format("Downloading database... {0:0}kB out of {1:0}kB", bytesIn*0.001, totalBytes*0.001); 
            CurrentProgress = percentage;
            _filesize = (long)totalBytes;
        }

        private void DownloadDataCompleted(object sender, AsyncCompletedEventArgs e)
        {
        }
        #endregion

        #region Public Methods
        public async Task doIt()
        {
            ButtonContent = "Cancel";

            // Default is "de"
            var listOfFiles = new TupleList<string, string, string>
                {
                    { "Report", "http://pillbox.oddb.org/amiko_report_de.html", @"amiko_report_de.html" },
                    { "Interaktionen", "http://pillbox.oddb.org/drug_interactions_csv_de.zip", @"drug_interactions_csv_de.csv.zip" },
                    { "Datenbank", "http://pillbox.oddb.org/amiko_db_full_idx_de.zip", @"amiko_db_full_idx_de.db.zip" },
                };

            await Task.Run(async () =>
            {
                // Generate list of download/unzip tasks
                var tasks = listOfFiles
                    .Select(f => StartDownloadAndExtract(f.Item1, f.Item2, f.Item3))
                    .ToList();

                foreach (Task t in tasks)
                {
                    await t;
                }
            });

            long? numArticles = 0;
            int numInteractions = 0;

            // Extract number of articles
            string filepath = Utilities.SQLiteDBPath();
            if (File.Exists(filepath))
            {
                DatabaseHelper db = new DatabaseHelper();
                await db.OpenDB(filepath);
                numArticles = await db.GetNumRecords("amikodb");
            }
            // Extract number of interactions
            filepath = Utilities.InteractionsPath();
            if (File.Exists(filepath))
            {
                string[] lines = File.ReadAllLines(filepath);
                numInteractions = lines.Length;
            }

            Text = string.Format("Neue AmiKo Datenbank mit {0} Fachinfos und {1} Interaktionen erfolgreich geladen!", numArticles, numInteractions);

            ButtonContent = "OK";
        }
        #endregion
    }
}
