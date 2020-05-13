using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class GoogleSyncManager
    {
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

        public async Task<UserCredential> LoginGoogle()
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                this.GoogleSecrets(),
                new[] { DriveService.Scope.DriveAppdata },
                "user",
                CancellationToken.None,
                this.DataStore());
            return credential;
        }

        public Task LogoutGoogle()
        {
            return DataStore().DeleteAsync<TokenResponse>("user");
        }

        public async Task TryGoogleAsync()
        {
            try {
                var uc = await this.LoginGoogle();
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = uc,
                    ApplicationName = "Amiko Desitin",
                });
                FilesResource.ListRequest req = service.Files.List();
                req.Spaces = "appDataFolder";
                IList<Google.Apis.Drive.v3.Data.File> files = req.Execute()
                    .Files;
                Console.WriteLine("Files:" + files.ToString());
            }
            catch (Exception e)
            {
                Log.WriteLine(e.ToString());
            }
        }
    }
}
