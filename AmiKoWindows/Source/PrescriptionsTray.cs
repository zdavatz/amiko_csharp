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
using Newtonsoft.Json;
using System.Windows;
using Newtonsoft.Json;

namespace AmiKoWindows
{
    // Prescription Manager Object which knows active (opened) prescription.
    class PrescriptionsTray
    {
        const string AMIKO_FILE_PREFIX = "RZ_";
        const string AMIKO_FILE_SUFFIX = ".amk";
        const string AMIKO_FILE_CREATED_AT_FORMAT = "yyyy-MM-dd'T'HHmmss";
        const string AMIKO_FILE_PLACE_DATE_FORMAT = "dd.MM.yyyy (HH:mm:ss)";

        public static readonly bool AutoSavingMode = true;

        private static readonly string notSaved = String.Format("({0})", Properties.Resources.unsaved);

        private static readonly Regex AMIKO_FILE_PREFIX_RGX = new Regex(
            String.Format(@"\A{0}", AMIKO_FILE_PREFIX), RegexOptions.Compiled);
        private static readonly Regex AMIKO_FILE_SUFFIX_RGX = new Regex(
            String.Format(@"{0}\z", AMIKO_FILE_SUFFIX), RegexOptions.Compiled);

        string _amikoDir;
        string _inboxDir;
        string _outboxDir;

        #region Public Fields
        public enum Result
        {
            Invalid, Found, Ok
        }

        public string PlaceDate { get; set; }
        public string Hash { get; set; }

        public string ActiveFileName { get; set; }
        public string ActiveFilePath { get; set; }
        public bool IsPreview = false;
        public bool HasChange = false;

        public Contact ActiveContact { get; set; }
        public Account ActiveAccount { get; set; }

        public bool IsActivePrescriptionPersisted
        {
            get {
                // has valid properties && file saved?
                if (!IsPreview &&
                    (PlaceDate != null && !PlaceDate.Equals(string.Empty)) &&
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

        public PrescriptionsTray()
        {
            _amikoDir = Utilities.PrescriptionsPath();
            _inboxDir = Utilities.GetInboxPath();
            _outboxDir = Utilities.GetOutboxPath();
            Utilities.EnforceDir(_amikoDir);

            GoogleSyncManager.Instance.Progress.ProgressChanged += (_sender, progress) =>
            {
                if (progress is SyncProgressFile)
                {
                    var p = progress as SyncProgressFile;
                    if (p.File.Extension.Equals(AMIKO_FILE_SUFFIX))
                    {
                        LoadFiles();
                    }
                }
            };
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

                if (asRewriting && !IsPreview &&
                    currentPath != null && File.Exists(currentPath))
                    File.Delete(currentPath);
            }

            var outputPath = "";
            Log.WriteLine("IsPreview: {0}", IsPreview);

            if (IsPreview)
            {   // preview of file in inbox
                if (currentPath != null && !currentPath.Equals(string.Empty))
                {
                    outputPath = currentPath;

                    if (ActiveFilePath != null &&
                        ActiveFilePath.Contains(_inboxDir) && File.Exists(ActiveFilePath))
                        File.Delete(ActiveFilePath);
                }
            }
            else
            {
                this.Hash = Utilities.GenerateUUID(); // always as new here (on save as new and rewriting both)
                this.PlaceDate = GeneratePlaceDate();

                outputPath = GetFilePathByDateString(Utilities.GetLocalTimeAsString(AMIKO_FILE_CREATED_AT_FORMAT));
            }

            if (this.Hash == null)
                return;

            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(outputPath))
                        File.Delete(outputPath);

                    // save json with current active values
                    string json = Utilities.Base64Encode(SerializeCurrentData()) ?? "";
                    using (var output = File.Create(outputPath))
                    {
                        byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                        output.Write(bytes, 0, bytes.Length);
                    }
                    this.ActiveFileName = AMIKO_FILE_SUFFIX_RGX.Replace(Path.GetFileName(outputPath), "");
                    this.ActiveFilePath = outputPath;
                    this.IsPreview = false;
                    this.HasChange = false;
                }
                catch (IOException ex)
                {
                    Log.WriteLine(ex.Message);
                }
            });
        }

        // takes (file) name argument without ext
        public async Task DeleteFile(string fullpath)
        {
            if (ActiveContact == null || ActiveContact.Uid == null ||
                ActiveAccount == null)
                return;

            // NOTE:
            // These directories may be not found, if user has samething
            // issue in HD, but not sure.
            if (_amikoDir == null || _inboxDir == null)
                return;

            await Task.Run(() =>
            {
                string aDir = Path.Combine(_amikoDir, ActiveContact.Uid);
                string iDir = Path.Combine(_inboxDir, ActiveContact.Uid);

                if (Utilities.EnforceDir(iDir) && Utilities.EnforceDir(iDir))
                {
                    var path = fullpath;
                    var name = AMIKO_FILE_SUFFIX_RGX.Replace(Path.GetFileName(fullpath), "");

                    if (!path.Contains(_inboxDir))
                        path = FindFilePathByNameFor(name, ActiveContact);

                    if (path == null)
                        return;

                    if (ActiveFilePath != null && !path.Equals(ActiveFilePath))
                        return;

                    FileItem item = null;
                    try {
                        item = _Files.First(f => {
                            return (f.Name != null && f.Name.Equals(name) &&
                                    f.Path != null && f.Path.Equals(path));
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        // unexpected
                        Log.WriteLine(ex.Message);
                    }

                    if (item != null)
                        _Files.Remove(item);

                    if (ActiveFileName != null && ActiveFileName.Equals(name))
                    {
                        this.ActiveFileName = null;
                        this.ActiveFilePath = null;
                    }
                    this.Hash = null;

                    if (File.Exists(path))
                        File.Delete(path);
                }
            });
        }

        public void AddMedication(Medication medication)
        {
            if (medication != null)
            {
                _Medications.Add(medication);
                if (this.IsPreview)
                {
                    this.IsPreview = false;
                    this.PlaceDate = GeneratePlaceDate();
                }
                this.HasChange = true;
                UpdateMedicationList();
            }
        }

        public void RemoveMedication(Medication medication)
        {
            if (medication != null)
            {
                _Medications.Remove(medication);
                if (this.IsPreview)
                {
                    this.IsPreview = false;
                    this.PlaceDate = GeneratePlaceDate();
                }
                this.HasChange = true;
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

            if (this.IsPreview)
            {
                this.IsPreview = false;
                this.PlaceDate = GeneratePlaceDate();
            }

            this.HasChange = true;
            medication.Comment = comment;
            return true;
        }

        public void LoadFiles()
        {
            _Files.Clear();

            if (ActiveContact != null)
            {
                string aDir = Path.Combine(_amikoDir, ActiveContact.Uid);
                string iDir = Path.Combine(_inboxDir, ActiveContact.Uid);

                var items = new List<FileItem>()
                    .Concat(LoadFilesAsItems(aDir))
                    .Concat(LoadFilesAsItems(iDir))
                    .ToList();

                foreach (var item in items.OrderByDescending(f => f.Name).ToArray())
                {
                    if (item.Path.Contains(_inboxDir))
                        item.Name = String.Format("{0} {1}", item.Name, notSaved);

                    _Files.Add(item);
                }
            }
            UpdateFileNameList();
        }

        // Read file. The file must be in inbox or activecontact's directory
        public void ReadFile(string fullpath)
        {
            ReadFileFor(fullpath, ActiveContact);
        }

        public void ReadFileFor(string fullpath, Contact contact)
        {
            var path = fullpath;
            var name = AMIKO_FILE_SUFFIX_RGX.Replace(Path.GetFileName(path), "");

            if (path.Contains(_inboxDir) && path.Contains(contact.Uid))
                this.IsPreview = true;
            else
            {
                this.IsPreview = false;
                path = FindFilePathByNameFor(name, contact);
            }

            if (path == null)
                return;

            string json = Utilities.Base64Decode(File.ReadAllText(path)) ?? "{}";
            DeserializeCurrentData(json, contact.Uid);

            if (path.Contains(_inboxDir))
                name = String.Format("{0} {1}", name, notSaved);

            this.ActiveFileName = name;
            this.ActiveFilePath = path;
            UpdateMedicationList();
        }

        // Copies file if it it valid. Returns path.
        public string CopyFile(string path)
        {
            var filename = Path.GetFileName(path);

            // check existence of the file and its name format
            if (!File.Exists(path) ||
                !AMIKO_FILE_PREFIX_RGX.IsMatch(filename) || !AMIKO_FILE_SUFFIX_RGX.IsMatch(filename))
                return null;

            var name = filename;
            name = AMIKO_FILE_PREFIX_RGX.Replace(name, "");
            name = AMIKO_FILE_SUFFIX_RGX.Replace(name, "");

            DateTime dt = DateTime.ParseExact(name, AMIKO_FILE_CREATED_AT_FORMAT, CultureInfo.InvariantCulture);
            if (!name.Equals(dt.ToString(AMIKO_FILE_CREATED_AT_FORMAT)))
                return null;

            // uid is in file
            string uid = ReadUid(path);
            if (uid == null || uid.Equals(string.Empty))
                return null; // invalid content

            string destDir = null;
            string destPath = null;

            // amiko
            destDir = Path.Combine(_amikoDir, uid);
            if (!Utilities.EnforceDir(destDir))
                return null;

            destPath = Path.Combine(destDir, filename);
            Log.WriteLine("destPath: {0}", destPath);
            if (File.Exists(destPath))
                return destPath;

            // inbox
            destDir = Path.Combine(_inboxDir, uid);
            if (!Utilities.EnforceDir(destDir))
                return null;

            destPath = Path.Combine(destDir, filename);
            Log.WriteLine("destPath: {0}", destPath);
            if (File.Exists(destPath))
                return destPath;

            File.Copy(path, destPath);
            if (!File.Exists(destPath))
                return null;

            return destPath;
        }

        // Copies file into sharable space (outbox)
        public string PickFile(string path)
        {
            var filename = Path.GetFileName(path);
            var destPath = Path.Combine(_outboxDir, filename);

            // always overwrite (if previous file exists)
            FileInfo info = new FileInfo(path);
            info.CopyTo(destPath, true);

            if (!File.Exists(destPath))
                return null;

            return destPath;
        }

        // Import new .amk file in inbox, or read existing file from amiko. It
        // depends to save or not to `autoSavingMode`. Returns enum result ("Invalid"/"Found"/"ok").
        public async Task<Result> ImportFile(string path)
        {
            if ((!path.Contains(_inboxDir) && !path.Contains(_amikoDir)) || !File.Exists(path))
                return Result.Invalid;

            var name = AMIKO_FILE_SUFFIX_RGX.Replace(Path.GetFileName(path), "");

            // same file exists for active contact with check of hash value
            var currentPath = FindFilePathByNameAndHashFor(name, Hash, ActiveContact);
            Log.WriteLine("currentPath: {0}", currentPath);
            if (currentPath != null)
            {
                ReadFile(currentPath);
                LoadFiles();
                return Result.Found;
            }

            // load all fields from the file
            PrescriptionJSONPresenter presenter = null;
            string hash = null;
            string placeDate = null;
            Contact contact = null;
            try {
                string rawInput = File.ReadAllText(path);
                string json = Utilities.Base64Decode(rawInput) ?? "{}";

                presenter = DeserializeJson(json);
                if (presenter == null || presenter.patient == null)
                    return Result.Invalid;

                hash = presenter.prescription_hash;
                if (hash == null || hash.Equals(string.Empty))
                    return Result.Invalid;

                placeDate = presenter.place_date;
                if (placeDate == null || placeDate.Equals(string.Empty))
                    return Result.Invalid;

                // NOTE:
                // Uid (in file) may wrong one from iOS and macOS (they have old
                // implementation)
                contact = presenter.Contact;
                contact.Uid = Utilities.FixedUidOf(contact);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
                return Result.Invalid;
            }

            // same file exists for another contact
            var existingPath = FindFilePathByNameAndHashFor(name, hash, contact);
            Log.WriteLine("existingPath: {0}", existingPath);
            if (existingPath != null)
            {
                ReadFileFor(existingPath, contact);
                LoadFiles();
                return Result.Found;
            }

            this._Medications = new HashSet<Medication>(presenter.MedicationsList);
            UpdateMedicationList();

            if (!AutoSavingMode)
                name = String.Format("{0} {1}", name, notSaved);

            this.ActiveFileName = name;
            this.ActiveFilePath = path;

            this.ActiveContact = contact;
            this.ActiveAccount = presenter.Account;

            this.Hash = hash;
            this.PlaceDate = placeDate;

            this.IsPreview = true;

            if (AutoSavingMode)
                await Save(false);

            LoadFiles();
            return Result.Ok;
        }

        #region Public Utility Methods
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
        #endregion

        #region Private Utility Methods
        private List<FileItem> LoadFilesAsItems(string dir)
        {
            List<FileItem> items = new List<FileItem>();

            if (Utilities.EnforceDir(dir))
            {
                string[] files = Directory.GetFiles(dir).OrderByDescending(f => f).ToArray();
                foreach (var path in files)
                {
                    var name = Path.GetFileName(path);
                    if (!AMIKO_FILE_PREFIX_RGX.IsMatch(name) || !AMIKO_FILE_SUFFIX_RGX.IsMatch(name))
                        continue;

                    name = AMIKO_FILE_SUFFIX_RGX.Replace(name, "");
                    items.Add(new FileItem() {
                        Name = name,
                        Hash = ReadHash(path),
                        Path = path,
                    });
                }
            }
            return items;
        }

        private string ReadUid(string path)
        {
            if (path == null || !File.Exists(path))
                return null;

            string json = Utilities.Base64Decode(File.ReadAllText(path)) ?? "{}";
            // get uid only for valid file
            var presenter = DeserializeJson(json);
            if (presenter == null || presenter.Contact == null)
                return null;

            // NOTE:
            // Uid (in file) may wrong one from iOS and macOS (they have old
            // implementation)
            Contact contact = presenter.Contact;
            return Utilities.FixedUidOf(contact);
        }

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

        #region Private File Path Utility Methods (_amikoDir)
        // e.g. RZ_2018-06-29T204622 (without ext)
        private string GetFilePathByNameFor(string name, Contact contact)
        {
            string path = null;
            if (contact == null || contact.Uid == null || contact.Uid.Equals(string.Empty))
                return path;

            string amkDir = Path.Combine(_amikoDir, contact.Uid);
            if (!Utilities.EnforceDir(amkDir))
                return path;
            else
            {
                path = String.Format("{0}.amk", Path.Combine(amkDir, name));
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
            Log.WriteLine("placeDate: {0}", placeDate);
            string path = null;
            if (placeDate == null || placeDate.Equals(string.Empty))
                return path;

            string savedAt = null;
            if (placeDate.Contains(","))
                savedAt = placeDate.Substring(placeDate.LastIndexOf(',') + 2, AMIKO_FILE_PLACE_DATE_FORMAT.Length);
            else
                savedAt = placeDate;
            Log.WriteLine("savedAt: {0}", savedAt);

            if (savedAt != null && !savedAt.Equals(string.Empty))
            {   // place_date -> .amk filename
                DateTime dt = DateTime.ParseExact(savedAt, AMIKO_FILE_PLACE_DATE_FORMAT, CultureInfo.InvariantCulture);
                path = GetFilePathByDateString(dt.ToString(AMIKO_FILE_CREATED_AT_FORMAT));
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

            return JsonConvert.SerializeObject(presenter, Formatting.None);
        }

        private PrescriptionJSONPresenter DeserializeJson(string json)
        {
            return JsonConvert.DeserializeObject<PrescriptionJSONPresenter>(json);
        }

        // Restores json data into current properties
        private void DeserializeCurrentData(string json, string uid)
        {
            var presenter = DeserializeJson(json);
            if (uid == null || presenter == null || presenter.patient == null)
                return;

            // NOTE:
            // Uid (in file) may wrong one from iOS and macOS (they have old
            // implementation)
            Contact contact = presenter.Contact;
            contact.Uid = Utilities.FixedUidOf(contact);

            this.Hash = presenter.prescription_hash;
            this.PlaceDate = presenter.place_date;

            this.ActiveAccount = presenter.Account;
            this.ActiveContact = contact;

            this._Medications = new HashSet<Medication>(presenter.MedicationsList);
        }

        private string GeneratePlaceDate()
        {
            if (ActiveAccount != null)
                return Utilities.ConcatWith(", ", ActiveAccount.City, Utilities.GetLocalTimeAsString(AMIKO_FILE_PLACE_DATE_FORMAT));
            return "";
        }
        #endregion
    }
}
