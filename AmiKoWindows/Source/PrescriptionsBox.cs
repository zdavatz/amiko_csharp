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
using System.Threading.Tasks;

namespace AmiKoWindows
{
    // Prescription Manager Object
    class PrescriptionsBox
    {
        const string FILE_SUFFIX_DATE_FORMAT = "yyyy-MM-dd'T'HHmmss";

        HashSet<string> _setOfFiles = new HashSet<string>();
        HashSet<string> _setOfMedicationIds = new HashSet<string>();

        string _userDataDir;

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
            _userDataDir = Utilities.AppRoamingDataFolder();
            CheckDir(_userDataDir);
        }

        private static string CheckDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public void LoadFiles()
        {
            // TODO
        }

        public void ShowDetail()
        {
        }

        public void Renew()
        {
            this.Hash = Utilities.GenerateUUID();
            this.PlaceDate = "";
        }

        public async Task Save()
        {
            if (Hash == null)
                this.Hash = Utilities.GenerateUUID(); // new

            await Task.Run(() =>
            {
                var outputFile = String.Format("RZ_{0}", Utilities.GetLocalTimeAsString(FILE_SUFFIX_DATE_FORMAT));
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
    }
}
