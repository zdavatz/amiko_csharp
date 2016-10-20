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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class UpdateDb : INotifyPropertyChanged
    {
        string _filename;
        long _filesize;

        public object Errors
        {
            get;
            private set;
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Source object used for data binding
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

        public async void doIt()
        {
            string app_folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Default is "de"
            var list_of_files = new TupleList<string, string, string>
            {
                { "Report", "http://pillbox.oddb.org/amiko_report_de.html", app_folder + @"\dbs\amiko_report_de.html" },
                { "Interaktionen", "http://pillbox.oddb.org/drug_interactions_csv_de.zip", app_folder + @"\dbs\drug_interactions_csv_de.csv.zip" },
                { "Datenbank", "http://pillbox.oddb.org/amiko_db_full_idx_de.zip", app_folder + @"\dbs\amiko_db_full_idx_de.db.zip" },
            };

            // Generate list of tasks
            var tasks = list_of_files
                .Select(f => StartDownload(f.Item1, f.Item2, f.Item3))
                .ToList();

            foreach (Task t in tasks)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                await t;
                sw.Stop();
                Console.WriteLine("File {0}, time = {1}ms", _filename, sw.ElapsedMilliseconds);
            }
            Console.WriteLine("Download finished.");
        }

        private Task StartDownload(string title, string url, string filename)
        {
            // Starts task backed by background thread (located in thread pool)
            return Task.Run(() =>
            {
                try
                {
                    var uri = new System.Uri(url);
                    using (WebClient wb = new WebClient())
                    {
                        wb.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                        wb.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadDataCompleted);
                        wb.DownloadFileAsync(uri, filename);
                        while (wb.IsBusy) ;
                        _filename = filename;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to download file: {0}", filename);
                }
            });
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = (double)e.BytesReceived; // = double.Parse(e.BytesReceived.ToString());
            double totalBytes = (double)e.TotalBytesToReceive; // = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            CurrentProgress = percentage;
            _filesize = (long)totalBytes;
        }

        private void DownloadDataCompleted(object sender, AsyncCompletedEventArgs e)
        {
        }
    }
}
