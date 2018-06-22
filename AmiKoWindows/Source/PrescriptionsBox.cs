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
    // Prescription Manager Object which knows active (opened) prescription.
    //
    // ```
    // # properties for active prescription
    // Contact Patient
    // Account Operotar
    // List<Medication> Medications
    // ```
    class PrescriptionsBox
    {
        const string AMIKO_FILE_PLACE_DATE_FORMAT = "dd.MM.yyyy (HH:mm:ss)";
        const string FILE_NAME_SUFFIX_DATE_FORMAT = "yyyy-MM-dd'T'HHmmss";

        private static readonly Regex AMIKO_FILE_EXTENSION_RGX = new Regex(@"\.amk$", RegexOptions.Compiled);

        string _dataDir;

        #region Public Fields
        #region for active prescription
        public string PlaceDate { get; set; }
        public string Hash { get; set; }

        public Contact Patient { get; set; }
        public Account Operator { get; set; }

        private HashSet<Medication> _Medications = new HashSet<Medication>();
        public List<Medication> Medications
        {
            get { return new List<Medication>(_Medications); }
        }
        #endregion

        #region for prescription manager
        private HashSet<string> _Files = new HashSet<string>();
        public List<string> Files
        {
            get { return new List<string>(_Files); }
        }
        #endregion
        #endregion

        #region Dependency Properties
        private ItemsObservableCollection _medicationListItems = new ItemsObservableCollection();
        public ItemsObservableCollection MedicationListItems
        {
            get { return _medicationListItems; }
            private set
            {
                if (value != _medicationListItems)
                    _medicationListItems = value;
            }
        }
        #endregion

        public PrescriptionsBox()
        {
            _dataDir = Utilities.AppRoamingDataFolder();
            EnforceDir(_dataDir);
        }

        public void UpdateMedicationList()
        {
            MedicationListItems.Clear();
            MedicationListItems.AddRange(Medications);
        }

        public void ShowDetail()
        {
        }

        public void Renew()
        {
            _Medications.Clear();
            UpdateMedicationList();

            this.Hash = Utilities.GenerateUUID();
            this.PlaceDate = "";
        }

        public async Task Save()
        {
            this.PlaceDate = GeneratePlaceDate();
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

        public void AddMedication(Medication medication)
        {
            _Medications.Add(medication);
            UpdateMedicationList();
        }

        public void RemoveMedication(Medication medication)
        {
            _Medications.Remove(medication);
            UpdateMedicationList();
        }

        public bool Contains(Medication medication)
        {
            return _Medications.Contains(medication);
        }

        public void LoadFiles()
        {
            if (Patient == null)
                return;

            this.Hash = null;

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

        public void DeleteFile(string hash)
        {

        }

        private string GeneratePlaceDate()
        {
            if (Operator != null)
                return Utilities.ConcatWith(", ", Operator.City, Utilities.GetLocalTimeAsString(AMIKO_FILE_PLACE_DATE_FORMAT));
            return "";
        }

        private static bool EnforceDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Directory.Exists(dir);
        }
    }
}
