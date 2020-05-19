using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Requests;
using System.Web.Script.Serialization;

namespace AmiKoWindows
{
    class GoogleSyncManager
    {
        public static readonly GoogleSyncManager Instance = new GoogleSyncManager();

        private GoogleSyncManager()
        { }

        private ClientSecrets GoogleSecrets()
        {
            var c = new ClientSecrets();
            c.ClientId = "";
            c.ClientSecret = "";
            return c;
        }

        private FileDataStore DataStore()
        {
            return new FileDataStore("GoogleDataStore");
        }

        public async Task<Boolean> IsGoogleLoggedInAsync()
        {
            var fds = this.DataStore();
            var result = await fds.GetAsync<TokenResponse>("user");
            return result != null;
        }

        public async Task<UserCredential> Login()
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                this.GoogleSecrets(),
                new[] { DriveService.Scope.DriveAppdata },
                "user",
                CancellationToken.None,
                this.DataStore());
            return credential;
        }

        public Task Logout()
        {
            return DataStore().ClearAsync();
        }

        public async Task<DriveService> GetDriveInstance()
        {
            var uc = await this.Login();
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = uc,
                ApplicationName = "Amiko Desitin",
            });
            return service;
        }

        private void ReportStatus(string str)
        { // TODO
        }

        #region Sync Logic
        static string FILE_FIELDS = "id, name, version, parents, mimeType, modifiedTime, size, properties";

        public async Task Synchronise()
        {
            Log.WriteLine("Start syncing");
            this.ReportStatus("Starting sync");
            List<File> remoteFiles = await this.ListRemoteFilesAndFolders();
            if (remoteFiles == null)
            {
                // Error during listing all files, better stop, otherwise we will wrongly delete files
                return;
            }
            List<IO.FileInfo> localFiles = this.ListLocalFilesAndFolders();
            Dictionary<string, File> remoteFilesMap = this.RemoteFilesToMap(remoteFiles);
            Dictionary<string, File> remotePatientsMap = this.ExtractPatientsFromFilesMap(remoteFilesMap);
            Dictionary<string, IO.FileInfo> localFilesMap = this.LocalFilesToMap(localFiles);
            Dictionary<string, long> localVersions = this.LocalFileVersionMap();
            Dictionary<string, long> patientVersions = this.ExtractPatientsFromFilesMap(localVersions);

            //PatientDBAdapter db = new PatientDBAdapter(this);
            //Map<string, Date> localPatientTimestamps = this.localPatientTimestamps(db);
            var localPatientTimestamps = new Dictionary<string, DateTime>();

            Log.WriteLine("remoteFiles " + remoteFiles.ToString());
            Log.WriteLine("localFiles " + localFiles.ToString());
            Log.WriteLine("remoteFilesMap " + remoteFilesMap.ToString());
            Log.WriteLine("localFilesMap " + localFilesMap.ToString());
            Log.WriteLine("localVersions " + localVersions.ToString());
            Log.WriteLine("remotePatientsMap " + remotePatientsMap.ToString());
            Log.WriteLine("patientVersions " + patientVersions.ToString());

            SyncPlan sp = new SyncPlan(
                    new IO.FileInfo(Utilities.AppRoamingDataFolder()),
                    await GetDriveInstance(),
                    localFilesMap,
                    localVersions,
                    remoteFilesMap,
                    patientVersions,
                    localPatientTimestamps,
                    remotePatientsMap
            );

            Dictionary<string, long> newVersionMap = await sp.Execute();
            //db.close();
            this.SaveLocalFileVersionMap(newVersionMap);
            this.ReportStatus("Finished sync");
            Log.WriteLine("End syncing");
        }

        private async Task<List<File>> ListRemoteFilesAndFolders()
        {
            var files = new List<File>();
            DriveService drive = await GetDriveInstance();
            string pageToken = null;

            do
            {
                var list = drive.Files.List();
                list.Spaces = "appDataFolder";
                list.Fields = "nextPageToken, files(" + FILE_FIELDS + ")";
                var fileList = await list.ExecuteAsync();
                files.AddRange(fileList.Files);
                pageToken = fileList.NextPageToken;
            } while (pageToken != null);
            return files;
        }

        protected bool ShouldSyncPath(string path)
        {
            var syncFolder = IO.Path.Combine(Utilities.AppRoamingDataFolder(), "googleSync");
            if (path.StartsWith(syncFolder))
            {
                return false;
            }
            var extension = IO.Path.GetExtension(path);
            if (extension.Equals(".csv") // interaction files
                || extension.Equals(".db") // patient db or main db
                || extension.Equals(".html") // report
                || extension.Equals(".zip") // temp file during update
                )
            {
                return false;
            }
            return true;
        }

        protected List<IO.FileInfo> ListLocalFilesAndFolders()
        {
            // TODO: create a fake file for doctor?
            var allFiles = new List<IO.FileInfo>();
            var filesDir = new IO.DirectoryInfo(Utilities.AppRoamingDataFolder());
            //IO.FileInfo filesDir = this.getFilesDir();
            var pendingFolders = new List<IO.DirectoryInfo>();
            pendingFolders.Add(filesDir);

            while (pendingFolders.Count > 0)
            {
                var currentFolder = pendingFolders[0];
                IO.FileInfo[] children = currentFolder.GetFiles();
                foreach (var child in children)
                {
                    if (!ShouldSyncPath(child.FullName))
                    {
                        // Skip "googleSync" folder which is used to store sync and creds info
                        continue;
                    }
                    allFiles.Add(child);
                }
                IO.DirectoryInfo[] childDirs = currentFolder.GetDirectories();
                foreach (var child in childDirs)
                {
                    allFiles.Add(new IO.FileInfo(child.FullName));
                    if (ShouldSyncPath(child.FullName))
                    {
                        pendingFolders.Add(new IO.DirectoryInfo(child.FullName));
                    }
                }
                pendingFolders.RemoveAt(0);
            }
            return allFiles;
        }

        /**
         * Convert remote files into a ([path] -> File) map
         * e.g. { "amk/111/abc.amk": File }
         *
         * @param files
         * @return the files map
         */
        private Dictionary<string, File> RemoteFilesToMap(List<File> files)
        {
            var fileMap = new Dictionary<string, File>();
            var idMap = new Dictionary<string, File>();

            foreach (File file in files)
            {
                idMap[file.Id] = file;
            }

            foreach (string key in idMap.Keys)
            {
                File file = idMap[key];
                var parents = new List<string>();
                string fileId = key;
                while (fileId != null)
                {
                    try
                    {
                        File thisFile = idMap[fileId];
                        string parent = thisFile.Parents.First();
                        if (parent != null)
                        {
                            // For files at root level, the parent id is a folder that we cannot access.
                            // Which would trigger an exception here:
                            File parentFile = idMap[parent];
                            parents.Insert(0, parentFile.Name);
                        }
                        fileId = parent;
                    }
                    catch (Exception e)
                    {
                        fileId = null;
                    }
                }
                parents.Add(file.Name);
                string fullPath = string.Join(IO.Path.DirectorySeparatorChar.ToString(), parents);
                fileMap[fullPath] = file;
            }
            return fileMap;
        }

        /**
         * Modify the given map, move the "patient entries" in the map to the new returned map
         * @param filesMap
         * @return
         */
        private Dictionary<string, T> ExtractPatientsFromFilesMap<T>(Dictionary<string, T> filesMap)
        {
            var map = new Dictionary<string, T>();
            foreach (string path in filesMap.Keys)
            {
                if (path.StartsWith("patients/"))
                {
                    map[path.Replace("patients/", "")] = filesMap[path];
                    filesMap.Remove(path);
                }
            }
            return map;
        }

        private Dictionary<string, DateTime> LocalPatientTimestamps(/*PatientDBAdapter db*/)
        {
            // TODO
            //Dictionary<string, string> strMap = db.getAllTimestamps();
            var map = new Dictionary<string, DateTime>();
            /*for (string uid : strMap.keySet())
            {
                Date timeStamp = Utilities.dateFromTimestring(strMap.get(uid));
                if (timeStamp != null)
                {
                    map.put(uid, timeStamp);
                }
            }*/
            return map;
        }

        private Dictionary<string, IO.FileInfo> LocalFilesToMap(List<IO.FileInfo> files)
        {
            var filesDir = new IO.DirectoryInfo(Utilities.AppRoamingDataFolder());
            var fileMap = new Dictionary<string, IO.FileInfo>();
            foreach (IO.FileInfo file in files)
            {
                string relPath = Utilities.MakeRelativePath(filesDir.FullName, file.FullName);
                fileMap[relPath] = file;
            }
            return fileMap;
        }

        private Dictionary<string, long> LocalFileVersionMap()
        {
            IO.FileInfo versionFile = new IO.FileInfo(IO.Path.Combine(Utilities.AppRoamingDataFolder(), "googleSync", "versions.json"));
            if (!versionFile.Exists)
            {
                return new Dictionary<string, long>();
            }
            string str = IO.File.ReadAllText(versionFile.FullName);
            var serializer = new JavaScriptSerializer();
            var dict = serializer.Deserialize<Dictionary<string, long>>(str);
            return dict;
        }

        private void SaveLocalFileVersionMap(Dictionary<string, long> map)
        {
            IO.FileInfo versionFile = new IO.FileInfo(IO.Path.Combine(Utilities.AppRoamingDataFolder(), "googleSync", "versions.json"));
            IO.DirectoryInfo parent = versionFile.Directory;
            if (!parent.Exists)
            {
                parent.Create();
            }
            var serializer = new JavaScriptSerializer();
            var str = serializer.Serialize(map);
            IO.File.WriteAllText(versionFile.FullName, str);
        }

        protected void ReportUpdatedFile(IO.FileInfo file)
        {
            // TODO
        }

        protected void ReportUpdatedPatient(string uid)
        {
            // TODO
        }

        public static string LastSynced()
        {
            IO.FileInfo versionFile = new IO.FileInfo(IO.Path.Combine(Utilities.AppRoamingDataFolder(), "googleSync", "versions.json"));
            if (!versionFile.Exists)
            {
                return "None";
            }
            // TODO
            //SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm.ss");
            //return sdf.format(new Date(versionFile.lastModified()));
            return "";
        }

        class SyncPlan
        {
            public IO.FileInfo filesDir;
            public DriveService driveService;

            private Dictionary<string, IO.FileInfo> localFilesMap;
            private Dictionary<string, long> localVersionMap;
            private Dictionary<string, File> remoteFilesMap;
            private Dictionary<string, long> patientVersions;
            private Dictionary<string, DateTime> localPatientTimestamps;
            private Dictionary<string, File> remotePatientsMap;

            public HashSet<string> pathsToCreate;
            // { path: remote file id }
            public Dictionary<string, string> pathsToUpdate;
            // { path: remote file id }
            public Dictionary<string, string> pathsToDownload;
            // set of path
            public HashSet<string> localFilesToDelete;
            // set of path
            public HashSet<string> remoteFilesToDelete;

            public HashSet<string> patientsToCreate; // uid
            public Dictionary<string, File> patientsToUpdate; // uid : remote file
            public Dictionary<string, File> patientsToDownload; // uid : remote file
            public HashSet<string> localPatientsToDelete; // uid
            public Dictionary<string, File> remotePatientsToDelete;
            //private PatientDBAdapter patientDB;

            public SyncPlan(IO.FileInfo filesDir,
                     DriveService driveService,
                     Dictionary<string, IO.FileInfo> localFilesMap,
                     Dictionary<string, long> localVersionMap,
                     Dictionary<string, File> remoteFilesMap,
                     Dictionary<string, long> patientVersions,
                     Dictionary<string, DateTime> localPatientTimestamps,
                     Dictionary<string, File> remotePatientsMap
                     //PatientDBAdapter patientDB
                     )
            {
                this.filesDir = filesDir;
                this.driveService = driveService;
                this.localFilesMap = localFilesMap;
                this.localVersionMap = localVersionMap;
                this.remoteFilesMap = remoteFilesMap;
                this.patientVersions = patientVersions;
                this.localPatientTimestamps = localPatientTimestamps;
                this.remotePatientsMap = remotePatientsMap;
                //this.patientDB = patientDB;

                PrepareFiles();
                PreparePatients();
            }

            void ReportStatus(string str)
            { // TODO
                Log.WriteLine(str);
            }

            void PrepareFiles()
            {
                pathsToCreate = new HashSet<string>();
                pathsToUpdate = new Dictionary<string, string>();
                pathsToDownload = new Dictionary<string, string>();
                localFilesToDelete = new HashSet<string>();
                remoteFilesToDelete = new HashSet<string>();

                pathsToCreate.UnionWith(localFilesMap.Keys);
                pathsToCreate.ExceptWith(localVersionMap.Keys);
                pathsToCreate.ExceptWith(remoteFilesMap.Keys);

                foreach (string path in localVersionMap.Keys)
                {
                    bool localHasFile = localFilesMap.ContainsKey(path);
                    bool remoteHasFile = remoteFilesMap.ContainsKey(path);

                    if (localHasFile && remoteHasFile)
                    {
                        long localVersion = localVersionMap[path];
                        IO.FileInfo localFile = localFilesMap[path];
                        File remoteFile = remoteFilesMap[path];
                        long? remoteVerion = remoteFile.Version;

                        if (localFile.Attributes.HasFlag(IO.FileAttributes.Directory) || remoteFile.MimeType.Equals("application/vnd.google-apps.folder"))
                        {
                            continue;
                        }
                        if (remoteVerion.Equals(localVersion))
                        {
                            DateTime localModified = localFile.LastWriteTime;
                            DateTime? remoteModified = remoteFile.ModifiedTime;
                            if (localModified > remoteModified)
                            {
                                pathsToUpdate[path] = remoteFile.Id;
                            }
                        }
                        else if (remoteVerion > localVersion)
                        {
                            pathsToDownload[path] = remoteFile.Id;
                        }
                    }
                    if (!localHasFile && remoteHasFile)
                    {
                        File remoteFile = remoteFilesMap[path];
                        if (!remoteFile.MimeType.Equals("application/vnd.google-apps.folder"))
                        {
                            remoteFilesToDelete.Add(path);
                        }
                    }
                    if (localHasFile && !remoteHasFile)
                    {
                        localFilesToDelete.Add(path);
                    }
                }

                foreach (string path in remoteFilesMap.Keys)
                {
                    File remoteFile = remoteFilesMap[path];
                    if (remoteFile.MimeType.Equals("application/vnd.google-apps.folder"))
                    {
                        continue;
                    }
                    if (localVersionMap.ContainsKey(path))
                    {
                        // We already handled this in the above loop
                        continue;
                    }
                    if (localFilesMap.ContainsKey(path))
                    {
                        // File exist both on server and local, but has no local version
                        IO.FileInfo localFile = localFilesMap[path];
                        var localModified = localFile.LastWriteTime;
                        var remoteModified = remoteFile.ModifiedTime;
                        if (localModified > remoteModified)
                        {
                            pathsToUpdate[path] = remoteFile.Id;
                        }
                        else if (localModified < remoteModified)
                        {
                            pathsToDownload[path] = remoteFile.Id;
                        }
                    }
                    else
                    {
                        pathsToDownload[path] = remoteFile.Id;
                    }
                }

                Log.WriteLine("pathsToCreate: " + pathsToCreate.ToString());
                Log.WriteLine("pathsToUpdate: " + pathsToUpdate.ToString());
                Log.WriteLine("pathsToDownload: " + pathsToDownload.ToString());
                Log.WriteLine("localFilesToDelete: " + localFilesToDelete.ToString());
                Log.WriteLine("remoteFilesToDelete: " + remoteFilesToDelete.ToString());
                Log.WriteLine("pathsToCreate: " + pathsToCreate.ToString());
            }

            void PreparePatients()
            {
                patientsToCreate = new HashSet<string>();
                patientsToUpdate = new Dictionary<string, File>();
                patientsToDownload = new Dictionary<string, File>();
                localPatientsToDelete = new HashSet<string>();
                remotePatientsToDelete = new Dictionary<string, File>();
                return;

                patientsToCreate.UnionWith(localPatientTimestamps.Keys);
                patientsToCreate.ExceptWith(patientVersions.Keys);
                patientsToCreate.ExceptWith(remotePatientsMap.Keys);

                foreach (string uid in patientVersions.Keys)
                {
                    bool localHasPatient = localPatientTimestamps.ContainsKey(uid);
                    bool remoteHasPatient = remotePatientsMap.ContainsKey(uid);

                    if (localHasPatient && remoteHasPatient)
                    {
                        long localVersion = patientVersions[uid];
                        DateTime localTimestamp = localPatientTimestamps[uid];
                        File remoteFile = remotePatientsMap[uid];
                        var remoteVerion = remoteFile.Version;

                        if (remoteVerion.Equals(localVersion))
                        {
                            var remoteModified = remoteFile.ModifiedTime;
                            if (localTimestamp > remoteModified)
                            {
                                patientsToUpdate[uid] = remoteFile;
                            }
                        }
                        else if (remoteVerion > localVersion)
                        {
                            patientsToDownload[uid] = remoteFile;
                        }
                    }
                    if (!localHasPatient && remoteHasPatient)
                    {
                        remotePatientsToDelete[uid] = remotePatientsMap[uid];
                    }
                    if (localHasPatient && !remoteHasPatient)
                    {
                        localPatientsToDelete.Add(uid);
                    }
                }

                foreach (string uid in remotePatientsMap.Keys)
                {
                    if (patientVersions.ContainsKey(uid))
                    {
                        // We already handled this in the above loop
                        continue;
                    }
                    if (localPatientTimestamps.ContainsKey(uid))
                    {
                        // File exist both on server and local, but has no local version
                        DateTime timestamp = localPatientTimestamps[uid];
                        File remoteFile = remotePatientsMap[uid];
                        var remoteModified = remoteFile.ModifiedTime;
                        if (timestamp > remoteModified)
                        {
                            patientsToUpdate[uid] = remoteFile;
                        }
                        else if (remoteModified > timestamp)
                        {
                            patientsToDownload[uid] = remoteFile;
                        }
                    }
                    else
                    {
                        File remoteFile = remotePatientsMap[uid];
                        patientsToDownload[uid] = remoteFile;
                    }
                }

                Log.WriteLine("patientsToCreate " + patientsToCreate.ToString());
                Log.WriteLine("patientsToUpdate " + patientsToUpdate.ToString());
                Log.WriteLine("patientsToDownload " + patientsToDownload.ToString());
                Log.WriteLine("localPatientsToDelete " + localPatientsToDelete.ToString());
                Log.WriteLine("remotePatientsToDelete " + remotePatientsToDelete.ToString());
            }

            public async Task CreateFolders() 
            {
                while (true)
                {
                    List<IO.FileInfo> allFoldersToCreate = this.pathsToCreate
                        .Select(path => new IO.FileInfo(IO.Path.Combine(this.filesDir.FullName, path)))
                        .Where(f => f.Attributes.HasFlag(IO.FileAttributes.Directory))
                        .ToList();
                    if (allFoldersToCreate.Count == 0)
                    {
                        break;
                    }

                    if (driveService == null) return;
                    BatchRequest batch = new BatchRequest(driveService);

                    foreach (IO.FileInfo folderToCreate in allFoldersToCreate)
                    {
                        string relativePath = Utilities.MakeRelativePath(this.filesDir.FullName, folderToCreate.FullName);
                        var parentFile = folderToCreate.Directory;
                        bool atRoot = parentFile.FullName.Equals(this.filesDir.FullName);
                        string parentRelativeToFilesDir = Utilities.MakeRelativePath(this.filesDir.FullName, parentFile.FullName);
                        File remoteParent = this.remoteFilesMap.ContainsKey(parentRelativeToFilesDir) ? this.remoteFilesMap[parentRelativeToFilesDir] : null;
                        if (!atRoot && remoteParent == null)
                        {
                            // parent is not created yet, skip this
                            continue;
                        }
                        File fileMetadata = new File();
                        fileMetadata.Name = folderToCreate.Name;
                        fileMetadata.MimeType = "application/vnd.google-apps.folder";
                        if (atRoot)
                        {
                            fileMetadata.Parents = new List<string> { "appDataFolder" };
                        }
                        else
                        {
                            fileMetadata.Parents = new List<string> { remoteParent.Id };
                        }
                        var req = driveService.Files.Create(fileMetadata);
                        req.Fields = FILE_FIELDS;
                        batch.Queue<File>(req, (result, error, index, message) =>
                        {
                            Log.WriteLine("Created folder");
                            Log.WriteLine("File name: " + result.Name);
                            Log.WriteLine("File ID: " + result.Id);
                            pathsToCreate.Remove(relativePath);
                            remoteFilesMap[relativePath] = result;
                        });
                    }
                    if (batch.Count > 0)
                    {
                        await batch.ExecuteAsync();
                    }
                    Log.WriteLine("batch finished");
                }
            }

            public async Task CreateFiles()
            {
                if (driveService == null) return;
                var toRemove = new HashSet<string>();
                int i = 0;
                foreach (string pathToCreate in pathsToCreate)
                {
                    IO.FileInfo localFile = new IO.FileInfo(IO.Path.Combine(this.filesDir.FullName, pathToCreate));
                    IO.DirectoryInfo parentFile = localFile.Directory;
                    bool atRoot = parentFile.FullName.Equals(this.filesDir.FullName);
                    string parentRelativeToFilesDir = Utilities.MakeRelativePath(this.filesDir.FullName, parentFile.FullName);
                    File remoteParent = this.remoteFilesMap.ContainsKey(parentRelativeToFilesDir) ? this.remoteFilesMap[parentRelativeToFilesDir] : null;

                    if (!atRoot && !remoteParent.MimeType.Equals("application/vnd.google-apps.folder"))
                    {
                        Log.WriteLine("remoteParent is not a folder?");
                    }

                    File fileMetadata = new File();
                    fileMetadata.Name = localFile.Name;
                    fileMetadata.ModifiedTime = localFile.LastWriteTime;
                    if (atRoot)
                    {
                        fileMetadata.Parents = new List<string> { "appDataFolder" };
                    }
                    else
                    {
                        fileMetadata.Parents = new List<string> { remoteParent.Id };
                    }

                    ReportStatus("Uploading new files (" + i + "/" + pathsToCreate.Count + ")");
                    using (var fileStream = localFile.OpenRead())
                    {
                        var req = driveService.Files
                            .Create(fileMetadata, fileStream, "application/octet-stream");
                        req.Fields = FILE_FIELDS;
                        var result = await req.UploadAsync();
                        var resultFile = req.ResponseBody;
                        Log.WriteLine("Created file");
                        Log.WriteLine("File name: " + resultFile.Name);
                        Log.WriteLine("File ID: " + resultFile.Id);
                        toRemove.Add(pathToCreate);
                        remoteFilesMap[pathToCreate] = resultFile;
                    }

                    i++;
                }
                pathsToCreate.ExceptWith(toRemove);
            }

            public async Task UpdateFiles()
            {
                if (driveService == null) return;
                int i = 0;
                var toRemove = new HashSet<string>();
                foreach (string path in this.pathsToUpdate.Keys)
                {
                    string fileId = this.pathsToUpdate[path];
                    IO.FileInfo localFile = new IO.FileInfo(IO.Path.Combine(this.filesDir.FullName, path));

                    File fileMetadata = new File();
                    fileMetadata.Name = localFile.Name;
                    fileMetadata.ModifiedTime = localFile.LastWriteTime;

                    ReportStatus("Updating files (" + i + "/" + pathsToUpdate.Count + ")");
                    using (var fileStream = localFile.OpenRead())
                    {
                        var req = driveService.Files
                            .Update(fileMetadata, fileId, fileStream, "application/octet-stream");
                        req.Fields = FILE_FIELDS;
                        var progress = await req.UploadAsync();
                        if (progress.Status == Google.Apis.Upload.UploadStatus.Completed)
                        {
                            var result = req.ResponseBody;
                            Log.WriteLine(result.ToString());
                            Log.WriteLine("Updated file");
                            Log.WriteLine("File name: " + result.Name);
                            Log.WriteLine("File ID: " + result.Id);

                            toRemove.Add(path);
                            remoteFilesMap[path] = result;
                        }
                    }
                    i++;
                }
                foreach (string x in toRemove)
                {
                    this.pathsToUpdate.Remove(x);
                }
            }

            public async Task DeleteFiles()
            {
                if (driveService == null) return;
                var batch = new BatchRequest(driveService);
                ReportStatus("Deleting files...");
                foreach (string path in this.remoteFilesToDelete)
                {
                    File remoteFile = this.remoteFilesMap[path];
                    var req = driveService.Files.Delete(remoteFile.Id);
                    batch.Queue<string>(req, (result, error, index, message) =>
                    {
                        Log.WriteLine("Deleted file");
                        Log.WriteLine("fileId: " + result);
                        remoteFilesToDelete.Remove(path);
                        remoteFilesMap.Remove(path);
                    });
                }
                if (batch.Count > 0) {
                    await batch.ExecuteAsync();
                }
            }

            public async Task DownloadRemoteFiles()
            {
                if (driveService == null) return;
                int i = 0;
                foreach (string path in this.pathsToDownload.Keys) {
                    string fileId = this.pathsToDownload[path];
                    File remoteFile = this.remoteFilesMap[path];
                    IO.FileInfo localFile = new IO.FileInfo(IO.Path.Combine(this.filesDir.FullName, path));
                    var parent = localFile.Directory;
                    if (!parent.Exists)
                    {
                        parent.Create();
                    }
                    if (remoteFile.Size != null && remoteFile.Size == 0)
                    {
                        continue;
                    }
                    ReportStatus("Downloading files (" + i + "/" + pathsToDownload.Count + ")");

                    using (var fileStream = localFile.OpenWrite())
                    {
                        var req = driveService.Files.Get(fileId);
                        req.Fields = FILE_FIELDS;
                        await req.DownloadAsync(fileStream);
                        // TODO
                        // ReportUpdatedFile(localFile);
                    }
                    try
                    {
                        localFile.LastWriteTime = (DateTime)remoteFile.ModifiedTime;
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(e.ToString());
                    }
                    i++;
                }
            }

            public void DeleteLocalFiles()
            {
                int i = 0;
                foreach (string path in this.localFilesToDelete)
                {
                    ReportStatus("Deleting files (" + i + "/" + localFilesToDelete.Count + ")");
                    IO.FileInfo file = new IO.FileInfo(IO.Path.Combine(this.filesDir.FullName, path));
                    file.Delete();
                    // TODO: ReportUpdatedFile(file);
                    i++;
                }
            }

            private async Task CreatePatientFolder()
            {
                if (remoteFilesMap.ContainsKey("patients")) return;
                ReportStatus("Preparing patients");
                File fileMetadata = new File();
                fileMetadata.Name = "patients";
                fileMetadata.MimeType = "application/vnd.google-apps.folder";
                fileMetadata.Parents = new List<string> { "appDataFolder" };
                var req = driveService.Files.Create(fileMetadata);
                req.Fields = FILE_FIELDS;
                var file = await req.ExecuteAsync();
                remoteFilesMap["patients"] = file;
            }

            private async Task CreatePatients(BatchRequest batch)
            {
                if (driveService == null) return;
                File patientFolder = remoteFilesMap.ContainsKey("patients") ? remoteFilesMap["patients"] : null;
                if (patientFolder == null)
                {
                    Log.WriteLine("Cannot find patients folder");
                    return;
                }
                List<Contact> patients = new List<Contact>(); // TODO: this.patientDB.getPatientsWithUids(this.patientsToCreate);
                foreach (Contact patient in patients)
                {
                    File fileMetadata = new File();
                    fileMetadata.Name = patient.Id.ToString();
                    fileMetadata.Properties = new Dictionary<string, string>(); // TODO;
                    fileMetadata.MimeType = "application/octet-stream";
                    fileMetadata.Parents = new List<string> { patientFolder.Id };
                    
                    var req = driveService.Files.Create(fileMetadata);
                    req.Fields = FILE_FIELDS;
                    batch.Queue<File>(req, (result, error, index, message) =>
                    {
                        Log.WriteLine("Created patient " + patient.Id.ToString());
                        Log.WriteLine("File ID: " + result.Id);
                        patientsToCreate.Remove(patient.Id.ToString());
                        remotePatientsMap[patient.Id.ToString()] = result;
                    });
                }
            }

            private void UpdatePatients(BatchRequest batch)
            {
                if (driveService == null) return;
                List<Contact> patients = new List<Contact>();
                // TODO: this.patientDB.getPatientsWithUids(this.patientsToUpdate.keySet());
                foreach (Contact patient in patients) {
                    File file = this.patientsToUpdate[patient.Id.ToString()];
                    File fileMetadata = new File();
                    fileMetadata.Name = patient.Id.ToString();
                    fileMetadata.Properties = new Dictionary<string, string>(); // TODO: patient.toMap

                    var req = driveService.Files.Update(fileMetadata, file.Id);
                    req.Fields = FILE_FIELDS;
                    batch.Queue<File>(req, (result, error, index, message) =>
                    {
                        Log.WriteLine("Updated files");
                        Log.WriteLine("File name: " + result.Name);
                        Log.WriteLine("File ID: " + result.Id);
                        patientsToUpdate.Remove(patient.Id.ToString());
                        remotePatientsMap[patient.Id.ToString()] = result;
                    });
                }
            }

            private void DeletePatients(BatchRequest batch)
            {
                if (driveService == null) return;
                foreach (string uid in this.remotePatientsToDelete.Keys) {
                    File file = this.remotePatientsToDelete[uid];
                    var req = driveService.Files.Delete(file.Id);
                    batch.Queue<string>(req, (content, error, index, message) =>
                    {
                        Log.WriteLine("Deleted remote patient");
                        Log.WriteLine("fileId: " + file.Id);
                        remotePatientsToDelete.Remove(uid);
                        remotePatientsMap.Remove(uid);
                    });
                }
            }

            private async Task DownloadRemotePatients()
            {
                foreach (string uid in this.patientsToDownload.Keys)
                {
                    File file = this.patientsToDownload[uid];
                    // TODO: this.patientDB.upsertRecordByUid(new Patient(file.getProperties()));
                    // TODO: reportUpdatedPatient(uid);
                }
            }

            private async Task DeleteLocalPatients()
            {
                foreach (string uid in this.localPatientsToDelete)
                {
                    // TODO: this.patientDB.deletePatientWithUid(uid);
                    this.patientVersions.Remove(uid);
                    // TODO: reportUpdatedPatient(uid);
                }
            }

            private async Task<Dictionary<string, long>> SyncFiles()
            {
                ReportStatus("Preparing folders...");
                await this.CreateFolders();
                Log.WriteLine("new remote map after creating folder " + remoteFilesMap.ToString());
                await this.CreateFiles();
                await this.UpdateFiles();
                await this.DeleteFiles();
                Log.WriteLine("new remote map after uploading " + remoteFilesMap.ToString());
                await this.DownloadRemoteFiles();
                this.DeleteLocalFiles();
                var newVersionMap = this.RemoteFilesToVersionMap(this.remoteFilesMap);
                return newVersionMap;
            }

            private async Task<Dictionary<string, long>> SyncPatients()
            {
                var batch = new BatchRequest(driveService);
                await this.CreatePatientFolder();
                Log.WriteLine("new remote map after creating patient folder " + remoteFilesMap.ToString());

                ReportStatus("Syncing patients");
                await this.CreatePatients(batch);
                this.UpdatePatients(batch);
                this.DeletePatients(batch);
                if (batch.Count > 0) {
                    await batch.ExecuteAsync();
                }
                Log.WriteLine("new remote map after uploading patients " + remotePatientsMap.ToString());
                await this.DownloadRemotePatients();
                await this.DeleteLocalPatients();
                var newVersionMap = this.RemoteFilesToVersionMap(this.remotePatientsMap);
                return newVersionMap;
            }

            public async Task<Dictionary<string, long>> Execute()
            {
                Dictionary<string, long> fileVersions = await this.SyncFiles();
                Dictionary<string, long> patientVersions = await this.SyncPatients();

                var result = new Dictionary<string, long>(fileVersions);
                foreach (string uid in patientVersions.Keys)
                {
                    result[IO.Path.Combine("patients", uid)] = patientVersions[uid];
                }
                return result;
            }

            private Dictionary<string, long> RemoteFilesToVersionMap(Dictionary<string, File> files)
            {
                var map = new Dictionary<string, long>();
                foreach (string key in files.Keys)
                {
                    File file = files[key];
                    map[key] = file.Version.GetValueOrDefault(0);
                }
                return map;
            }
        }
        #endregion
    }
}
