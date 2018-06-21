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
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    // Prescription Manager Object
    class PrescriptionsBox
    {
        const string AMIKO_FILE_PLACE_DATE_FORMAT = "dd.MM.yyyy (HH:mm:ss)";
        const string FILE_NAME_SUFFIX_DATE_FORMAT = "yyyy-MM-dd'T'HHmmss";

        private static readonly Regex AMIKO_FILE_EXTENSION_RGX = new Regex(@"\.amk$", RegexOptions.Compiled);

        HashSet<string> _setOfFiles = new HashSet<string>();
        HashSet<string> _setOfMedicationIds = new HashSet<string>();

        string _dataDir;

        #region Public Fields
        public Contact Patient { get; set; }
        public Account Operator { get; set; }

        public string PlaceDate { get; set; }
        public string Hash { get; set; }
        public List<string> MedicationIds
        {
            get { return new List<string>(_setOfMedicationIds); }
        }

        public List<string> Files
        {
            get { return new List<string>(_setOfFiles); }
        }
        #endregion

        public PrescriptionsBox()
        {
            _dataDir = Utilities.AppRoamingDataFolder();
            EnforceDir(_dataDir);
        }

        public string GetPlaceDate()
        {
            var placeDate = "";
            if (Operator != null)
                placeDate = Utilities.ConcatWith(", ", Operator.City, Utilities.GetLocalTimeAsString(AMIKO_FILE_PLACE_DATE_FORMAT));
            return placeDate;
        }

        public void LoadFiles()
        {
            if (Patient == null)
                return;

            string userDir = Path.Combine(_dataDir, Patient.Uid);
            Log.WriteLine("userDir: {0}", userDir);
            if (EnforceDir(userDir))
            {
                string[] files = Directory.GetFiles(userDir);
                foreach (var f in files)
                {
                    if (!AMIKO_FILE_EXTENSION_RGX.IsMatch(@"\.amk$"))
                        continue;

                    Log.WriteLine("file: {0}", f);
                }
            }
        }

        public void ShowDetail()
        {
        }

        public void Renew()
        {
            this.Hash = Utilities.GenerateUUID();
            if (Operator != null)
                this.PlaceDate = GetPlaceDate();

            Log.WriteLine("PlaceDate: {0}", PlaceDate);
        }

        public async Task Save()
        {
            if (Hash == null)
                this.Hash = Utilities.GenerateUUID(); // new

            await Task.Run(() =>
            {
                var outputFile = String.Format("RZ_{0}", Utilities.GetLocalTimeAsString(FILE_NAME_SUFFIX_DATE_FORMAT));
                try
                {
                    Log.WriteLine("prescription_hash: {0}", Hash);
                    //if (File.Exists(outputFile))
                    //    File.Delete(outputFile);

                    //using (var output = File.Create(outputFile))
                    //{
                    //}
                }
                catch (IOException ex)
                {
                    Log.WriteLine(ex.Message);
                }
            });
        }

        public void AddMedication(string entry)
        {
            // TODO
        }

        public void RemoveMedication(string entry)
        {
            // TODO
        }

        public void DeleteFile(string hash)
        {

        }

        public bool Contains(string file)
        {
            return _setOfFiles.Contains(file);
        }

        private static bool EnforceDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Directory.Exists(dir);
        }
    }
}
