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

        public bool DownloadingCompleted
        {
            get;
            set;
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
        /// <summary>
        /// This function is used to check specified file being used or not
        /// </summary>
        /// <param name="file">FileInfo of required file</param>
        /// <returns>If that specified file is being processed 
        /// or not found is return true</returns>
        private static Boolean IsFileLocked(string filename)
        {
            FileInfo file = new FileInfo(filename);
            FileStream stream = null;

            try
            {
                // Don't change FileAccess to ReadWrite, because if a file is in readOnly, it fails.
                stream = file.Open
                (
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None
                );
            }
            catch (IOException)
            {
                // the file is unavailable because it is:
                // still being written to or being processed by another thread
                // or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        private void StartDownloadAndExtract(string title, string url, string filename)
        {
            string filepath = Path.Combine(Utilities.AppRoamingDataFolder(), filename);
            _filename = filename;

            // Starts task backed by background thread (located in thread pool)
            // Download
            try
            {
                var uri = new Uri(url);
                using (WebClient wb = new WebClient())
                {
                    wb.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                    wb.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadDataCompleted);
                    wb.DownloadFileAsync(uri, filepath);
                    while (wb.IsBusy) ;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to download file: {0}", filename);
            }

            if (DownloadingCompleted)
                return;

            // Decompress if necessary
            if (_filename.EndsWith("zip"))
            {
                Text = string.Format("Unzipping {0}... ", _filename.Replace(".zip", ""));
                string unzippedFilepath = filepath.Replace(".zip", "");
                // Remove old zip file if it exists
                if (File.Exists(unzippedFilepath) && !IsFileLocked(unzippedFilepath))
                {
                    File.Delete(unzippedFilepath);
                }
                // Unzip to app roaming folder
                ZipFile.ExtractToDirectory(filepath, Utilities.AppRoamingDataFolder());
                // Remove file
                if (File.Exists(filepath) && !IsFileLocked(filepath))
                {
                    File.Delete(filepath);
                }
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = (double)e.BytesReceived; // = double.Parse(e.BytesReceived.ToString());
            double totalBytes = (double)e.TotalBytesToReceive; // = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            Text = string.Format("Downloading {0}... {1:0}kB out of {2:0}kB", _filename.Replace(".zip", ""), bytesIn *0.001, totalBytes*0.001);
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
                    { "Hauptdatenbank", "http://pillbox.oddb.org/amiko_db_full_idx_de.zip", @"amiko_db_full_idx_de.db.zip" },
                    { "Volltext-Suchbegriffe", "http://pillbox.oddb.org/amiko_frequency_de.db.zip", @"amiko_frequency_de.db.zip" }
                };

            if (Utilities.AppLanguage().Equals("fr"))
            {
                listOfFiles = new TupleList<string, string, string>
                {
                    { "Report", "http://pillbox.oddb.org/amiko_report_fr.html", @"amiko_report_fr.html" },
                    { "Interactions", "http://pillbox.oddb.org/drug_interactions_csv_fr.zip", @"drug_interactions_csv_fr.csv.zip" },
                    { "Base des données principale", "http://pillbox.oddb.org/amiko_db_full_idx_fr.zip", @"amiko_db_full_idx_fr.db.zip" },
                    { "Mots-clés", "http://pillbox.oddb.org/amiko_frequency_fr.db.zip", @"amiko_frequency_fr.db.zip" }
                };
            }

            DownloadingCompleted = false;
            await Task.Run(() =>
            {
                // Generate list of download/unzip tasks
                var actions = new List<Action>();
                foreach (var f in listOfFiles)
                {
                    actions.Add(new Action(() => { StartDownloadAndExtract(f.Item1, f.Item2, f.Item3); }));      
                }
                foreach (Action a in actions)
                {
                    a();
                }
            });

            long? numArticles = 0;
            long? numSearchTerms = 0;
            int numInteractions = 0;

            // Extract number of articles
            string filepath = Utilities.SQLiteDBPath();
            if (File.Exists(filepath))
            {
                if (!DownloadingCompleted)
                {
                    DatabaseHelper db = new DatabaseHelper();
                    await db.OpenDB(filepath);
                    numArticles = await db.GetNumRecords("amikodb");
                    db.CloseDB();
                }
            }
            // Extract number of fulltext search terms
            filepath = Utilities.FrequencyDBPath();
            if (File.Exists(filepath))
            {
                if (!DownloadingCompleted)
                {
                    DatabaseHelper db = new DatabaseHelper();
                    await db.OpenDB(filepath);
                    numSearchTerms = await db.GetNumRecords("frequency");
                    db.CloseDB();
                }
            }
            // Extract number of interactions
            filepath = Utilities.InteractionsPath();
            if (File.Exists(filepath))
            {
                string[] lines = File.ReadAllLines(filepath);
                numInteractions = lines.Length;
            }

            if (Utilities.AppLanguage().Equals("de"))
                Text = string.Format("Neue AmiKo Datenbank mit:\n- {0} Fachinfos\n- {1} Suchbegriffen\n- {2} Interaktionen\n erfolgreich geladen!"
                    , numArticles, numSearchTerms, numInteractions);
            else if (Utilities.AppLanguage().Equals("fr"))
                Text = string.Format("Nouvelle base de données avec:\n- {0} notes infopro\n- {1} mot-clés\n- {2} interactions\n chargée avec succès!"
                    , numArticles, numSearchTerms, numInteractions);

            DownloadingCompleted = true;
            ButtonContent = "OK";
        }
        #endregion
    }
}
