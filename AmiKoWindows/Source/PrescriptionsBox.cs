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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AmiKoWindows
{
    // Prescription Manager Object which knows active (opened) prescription.
    class PrescriptionsBox
    {
        const string AMIKO_FILE_PREFIX = "RZ_";
        const string AMIKO_FILE_SUFFIX = ".amk";
        const string AMIKO_FILE_PLACE_DATE_FORMAT = "dd.MM.yyyy (HH:mm:ss)";
        const string FILE_NAME_SUFFIX_DATE_FORMAT = "yyyy-MM-dd'T'HHmmss";

        private static readonly Regex AMIKO_FILE_EXTENSION_RGX = new Regex(
            String.Format(@"{0}\z", AMIKO_FILE_SUFFIX), RegexOptions.Compiled);

        string _dataDir;

        #region Public Fields
        public string PlaceDate { get; set; }
        public string Hash { get; set; }

        public Contact ActiveContact { get; set; }
        public Account ActiveAccount { get; set; }

        public bool IsActivePrescriptionPersisted
        {
            get {
                // has valid properties && file saved?
                if ((PlaceDate != null && !PlaceDate.Equals(string.Empty)) &&
                    (Hash != null && !Hash.Equals(string.Empty)) && Medications.Count > 0)
                {
                    var path = GetFilePathByPlaceDate(PlaceDate);
                    return path != null && File.Exists(path);
                }
                return false;
            }
        }

        private HashSet<Medication> _Medications = new HashSet<Medication>();
        public List<Medication> Medications
        {
            get { return new List<Medication>(_Medications); }
        }

        #region Prescription File Manager
        private HashSet<TitleItem> _Files = new HashSet<TitleItem>();
        public List<TitleItem> Files
        {
            get { return new List<TitleItem>(_Files); }
        }
        #endregion
        #endregion

        #region Dependency Properties
        private CommentItemsObservableCollection _medicationListItems = new CommentItemsObservableCollection();
        public CommentItemsObservableCollection MedicationListItems
        {
            get { return _medicationListItems; }
            private set
            {
                if (value != _medicationListItems)
                    _medicationListItems = value;
            }
        }

        #region Prescription File Manager
        private TitleItemsObservableCollection _fileNameListItems = new TitleItemsObservableCollection();
        public TitleItemsObservableCollection FileNameListItems
        {
            get { return _fileNameListItems; }
            private set
            {
                if (value != _fileNameListItems)
                    _fileNameListItems = value;
            }
        }
        #endregion
        #endregion

        public async static Task DeleteAllPrescriptions(string uid)
        {
            await Task.Run(() =>
            {
                string userDir = Path.Combine(Utilities.AppRoamingDataFolder(), uid);
                if (EnforceDir(userDir))
                {
                    if (!Directory.Exists(userDir))
                        return;

                    var info = new DirectoryInfo(userDir);
                    foreach (FileInfo file in info.GetFiles())
                        file.Delete();

                    info.Delete(true);
                }
            });
        }

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

        public void UpdateFileNameList()
        {
            FileNameListItems.Clear();
            FileNameListItems.AddRange(Files); // string[]
        }

        public void Renew()
        {
            _Medications.Clear();
            UpdateMedicationList();

            this.Hash = Utilities.GenerateUUID();
            this.PlaceDate = null;
        }

        public async Task Save(bool asRewriting)
        {
            if (this.Hash != null && this.PlaceDate != null && asRewriting)
            {
                var currentPath = GetFilePathByPlaceDate(this.PlaceDate);
                if (currentPath != null && File.Exists(currentPath))
                    File.Delete(currentPath);
            }

            this.Hash = Utilities.GenerateUUID(); // always as new here (on save as new and rewriting both)
            this.PlaceDate = GeneratePlaceDate();

            if (this.Hash == null)
                return;

            await Task.Run(() =>
            {
                var outputPath = GetFilePathByDateString(Utilities.GetLocalTimeAsString(FILE_NAME_SUFFIX_DATE_FORMAT));
                try
                {
                    if (File.Exists(outputPath))
                        File.Delete(outputPath);

                    string json = Utilities.Base64Encode(SerializeCurrentData()) ?? "";
                    using (var output = File.Create(outputPath))
                    {
                        byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                        output.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (IOException ex)
                {
                    Log.WriteLine(ex.Message);
                }
            });
        }

        // takes (file) name argument without ext
        public async Task DeleteFile(string name)
        {
            if (ActiveContact == null || ActiveAccount == null)
                return;

            await Task.Run(() =>
            {
                string userDir = Path.Combine(_dataDir, ActiveContact.Uid);
                if (EnforceDir(userDir))
                {
                    var path = FindFilePathByName(name);
                    if (path == null)
                        return;

                    var item = new TitleItem() { Id = name, Title = name };
                    _Files.Remove(item);

                    File.Delete(path);
                }
            });
        }

        public void AddMedication(Medication medication)
        {
            if (medication != null)
            {
                _Medications.Add(medication);
                UpdateMedicationList();
            }
        }

        public void RemoveMedication(Medication medication)
        {
            if (medication != null)
            {
                _Medications.Remove(medication);
                UpdateMedicationList();
            }
        }

        public void RemoveMedicationAtIndex(long? index)
        {
            if (index == null)
                return;

            var i = Convert.ToInt32(index);
            var medication = _Medications.ElementAt(i);
            RemoveMedication(medication);
        }

        public bool AddMedicationCommentAtIndex(long? index, string comment)
        {
            if (index == null)
                return false;

            var i = Convert.ToInt32(index);
            var medication = _Medications.ElementAt(i);
            var currentComment = (medication.Comment != null ? medication.Comment : "");

            if (currentComment.Equals(comment))
                return false;

            medication.Comment = comment;
            return true;
        }

        public void LoadFile(string name)
        {
            var path = FindFilePathByName(name);
            if (path == null)
                return;

            string json = Utilities.Base64Decode(File.ReadAllText(path)) ?? "{}";
            DeserializeJson(json);
            UpdateMedicationList();
        }

        public void LoadFiles()
        {
            _Files.Clear();

            if (ActiveContact != null)
            {
                string userDir = Path.Combine(_dataDir, ActiveContact.Uid);
                if (EnforceDir(userDir))
                {
                    string[] files = Directory.GetFiles(userDir).OrderByDescending(f => f).ToArray();
                    foreach (var path in files)
                    {
                        var name = Path.GetFileName(path);
                        if (!AMIKO_FILE_EXTENSION_RGX.IsMatch(name))
                            continue;
                        var item = new TitleItem() {
                            Id = name, Title = AMIKO_FILE_EXTENSION_RGX.Replace(name, "")
                        };
                        _Files.Add(item);
                    }
                }
            }
            UpdateFileNameList();
        }

        public string FindFilePathByName(string name)
        {
            var path = GetFilePathByName(name);
            if (!File.Exists(path))
                return null;
            return path;
        }

        #region Private Path Utilities
        // e.g. RZ_2018-06-29T204622 (without ext)
        private string GetFilePathByName(string name)
        {
            string path = null;
            if (ActiveContact == null)
                return path;

            string userDir = Path.Combine(_dataDir, ActiveContact.Uid);
            if (!EnforceDir(userDir))
                return path;
            else
            {
                path = String.Format("{0}.amk", Path.Combine(userDir, name));
                Log.WriteLine("path: {0}", path);
                return path;
            }
        }

        private string GetFilePathByDateString(string dateString)
        {
            if (dateString == null || dateString.Equals(string.Empty))
                return null;

            var name = String.Format("RZ_{0}", dateString);
            return GetFilePathByName(name);
        }

        public string GetFilePathByPlaceDate(string placeDate)
        {
            string path = null;
            if (placeDate == null || placeDate.Equals(string.Empty))
                return path;

            var savedAt = placeDate.Substring(placeDate.LastIndexOf(',') + 2, AMIKO_FILE_PLACE_DATE_FORMAT.Length);
            Log.WriteLine("savedAt: {0}", savedAt);
            if (savedAt != null && !savedAt.Equals(string.Empty))
            {   // place_date -> .amk filename
                DateTime dt = DateTime.ParseExact(savedAt, AMIKO_FILE_PLACE_DATE_FORMAT, CultureInfo.InvariantCulture);
                path = GetFilePathByDateString(dt.ToString(FILE_NAME_SUFFIX_DATE_FORMAT));
            }
            return path;
        }
        #endregion

        // Returns json as string
        private string SerializeCurrentData()
        {
            var presenter = new PrescriptionJSONPresenter(Hash, PlaceDate);
            presenter.Account = ActiveAccount;
            presenter.Contact = ActiveContact;
            presenter.MedicationsList = Medications;

            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(presenter);
        }

        // Restores properties from json file
        private void DeserializeJson(string json)
        {
            var serializer = new JavaScriptSerializer();
            var presenter = serializer.Deserialize<PrescriptionJSONPresenter>(json);

            if (ActiveContact == null || presenter.patient == null)
                return;

            if (ActiveContact.Uid.Equals(presenter.patient.patient_id))
            {
                this.Hash = presenter.prescription_hash;
                this.PlaceDate = presenter.place_date;

                // TODO
                // How to handle properties are different than *active*
                // Account and Contact here?
                //this.ActiveContact = presenter.Contact;
                //this.ActiveAccount = presenter.Account;

                this._Medications = new HashSet<Medication>(presenter.MedicationsList);
            }
        }

        private string GeneratePlaceDate()
        {
            if (ActiveAccount != null)
                return Utilities.ConcatWith(", ", ActiveAccount.City, Utilities.GetLocalTimeAsString(AMIKO_FILE_PLACE_DATE_FORMAT));
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