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

        private static readonly string notSaved = String.Format("({0})", Properties.Resources.unsaved);

        private static readonly Regex AMIKO_FILE_EXTENSION_RGX = new Regex(
            String.Format(@"{0}\z", AMIKO_FILE_SUFFIX), RegexOptions.Compiled);

        string _dataDir;

        #region Public Fields
        public string PlaceDate { get; set; }
        public string Hash { get; set; }

        public string ActiveFileName { get; set; }
        public string ActiveFilePath { get; set; }
        public bool IsPreview = false;

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
        private HashSet<FileItem> _Files = new HashSet<FileItem>();
        public List<FileItem> Files
        {
            get { return new List<FileItem>(_Files); }
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
        private FileItemsObservableCollection _fileNameListItems = new FileItemsObservableCollection();
        public FileItemsObservableCollection FileNameListItems
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
            var currentPath = "";
            if (this.Hash != null && this.PlaceDate != null)
            {
                currentPath = GetFilePathByPlaceDate(this.PlaceDate);

                if (asRewriting &&
                    currentPath != null && File.Exists(currentPath))
                    File.Delete(currentPath);
            }

            var outputPath = "";
            Log.WriteLine("IsPreview: {0}", IsPreview);
            Log.WriteLine("currentPath: {0}", currentPath);
            if (IsPreview && currentPath != null && !currentPath.Equals(string.Empty))
                outputPath = currentPath;
            else
            {
                this.Hash = Utilities.GenerateUUID(); // always as new here (on save as new and rewriting both)
                this.PlaceDate = GeneratePlaceDate();

                outputPath = GetFilePathByDateString(Utilities.GetLocalTimeAsString(FILE_NAME_SUFFIX_DATE_FORMAT));
            }

            if (this.Hash == null)
                return;

            await Task.Run(() =>
            {
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
                    this.ActiveFileName = AMIKO_FILE_EXTENSION_RGX.Replace(Path.GetFileName(outputPath), "");
                    this.ActiveFilePath = outputPath;
                    this.IsPreview = false;
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
                    var path = FindFilePathByNameFor(name, ActiveContact);
                    if (path == null)
                        return;

                    if (ActiveFilePath != null && !path.Equals(ActiveFilePath))
                        return;

                    var item = _Files.First(f => {
                        return (f.Name != null && f.Name.Equals(name) &&
                                f.Path != null && f.Path.Equals(path));
                    });
                    if (item != null)
                        _Files.Remove(item);

                    if (ActiveFileName != null && ActiveFileName.Equals(name))
                    {
                        this.ActiveFileName = null;
                        this.ActiveFilePath = null;
                    }
                    this.Hash = null;

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
            var path = FindFilePathByNameFor(name, ActiveContact);
            if (path == null)
                return;

            string json = Utilities.Base64Decode(File.ReadAllText(path)) ?? "{}";
            DeserializeCurrentData(json);

            // TODO set contact/account (here)

            this.ActiveFileName = name;
            this.ActiveFilePath = path;
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
                        name = AMIKO_FILE_EXTENSION_RGX.Replace(name, "");

                        var item = new FileItem() {
                            Name = name,
                            Hash = ReadHash(path),
                            Path = path,
                        };
                        _Files.Add(item);
                    }
                }
            }
            UpdateFileNameList();
        }

        // Loads .amk file in outside of the space of this app. Returns true if the loading succeeds.
        public bool PreviewFile(string path)
        {
            if (!File.Exists(path))
                return false;

            var name = AMIKO_FILE_EXTENSION_RGX.Replace(Path.GetFileName(path), "");

            // same file exists for active contact
            var currentPath = FindFilePathByNameAndHashFor(name, Hash, ActiveContact);
            if (currentPath != null)
                return false;

            // open original file
            string rawInput = File.ReadAllText(path);
            string json = Utilities.Base64Decode(rawInput) ?? "{}";

            var presenter = DeserializeJson(json);
            if (presenter == null || presenter.patient == null)
                return false;

            var hash = presenter.prescription_hash;
            if (hash == null || hash.Equals(string.Empty))
                return false;

            var existingPath = FindFilePathByNameAndHashFor(name, hash, presenter.Contact);
            if (existingPath != null)
                return false; // exists

            Renew();

            this._Medications = new HashSet<Medication>(presenter.MedicationsList);
            UpdateMedicationList();

            this.ActiveFileName = String.Format("{0} {1}", name, notSaved);
            // TODO: use Inbox
            this.ActiveFilePath = path;
            this.IsPreview = true;

            if ((GetFilePathByName(name)) == null)
            {  // this prescription is not for active contact
                _Files.Clear();
            }
            else
            {  // new prescription for active contact

            }

            this.ActiveContact = presenter.Contact;
            this.ActiveAccount = presenter.Account;

            var item = new FileItem() {
                Name = ActiveFileName,
                Hash = hash,
                Path = ActiveFilePath,
            };

            _Files.Add(item);
            UpdateFileNameList();

            this.Hash = hash;
            this.PlaceDate = presenter.place_date;

            return true;
        }

        public string FindFilePathByNameFor(string name, Contact contact)
        {
            var path = GetFilePathByNameFor(name, contact);
            if (path == null || !File.Exists(path))
                return null;
            return path;
        }

        // strict file finder method with check using `prescription_hash` (hash)
        public string FindFilePathByNameAndHashFor(string name, string hash, Contact contact)
        {
            if (hash == null || hash.Equals(string.Empty))
                return null;

            var path = GetFilePathByNameFor(name, contact);
            if (path == null || !File.Exists(path))
                return null;

            var hashInFile = ReadHash(path);
            if (hashInFile == null || !hashInFile.Equals(hash))
                return null;

            return path;
        }

        #region Private Utilities
        private string ReadHash(string path)
        {
            if (path == null || !File.Exists(path))
                return null;

            string json = Utilities.Base64Decode(File.ReadAllText(path)) ?? "{}";
            // get hash only for valid file
            var presenter = DeserializeJson(json);
            if (presenter == null || presenter.prescription_hash == null)
                return null;

            return presenter.prescription_hash;
        }

        // e.g. RZ_2018-06-29T204622 (without ext)
        private string GetFilePathByNameFor(string name, Contact contact)
        {
            string path = null;
            if (contact == null || contact.Uid == null || contact.Uid.Equals(string.Empty))
                return path;

            string userDir = Path.Combine(_dataDir, contact.Uid);
            if (!EnforceDir(userDir))
                return path;
            else
            {
                path = String.Format("{0}.amk", Path.Combine(userDir, name));
                Log.WriteLine("path: {0}", path);
                return path;
            }
        }

        private string GetFilePathByName(string name)
        {
            return GetFilePathByNameFor(name, ActiveContact);
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

        private PrescriptionJSONPresenter DeserializeJson(string json)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<PrescriptionJSONPresenter>(json);
        }

        // Restores json data into current properties
        private void DeserializeCurrentData(string json)
        {
            var presenter = DeserializeJson(json);
            if (ActiveContact == null || presenter == null || presenter.patient == null)
                return;

            // TODO more validations
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
