using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiKoWindows.Source.HINClient
{
    class HINSettingsManager
    {
        public static HINSettingsManager Instance = new HINSettingsManager();

        private HINSettingsManager()
        {

        }
        public OAuthTokens? SDSAccessToken
        {
            get
            {
                var filesDir = Utilities.AppRoamingDataFolder();
                var filePath = Path.Combine(filesDir, Constants.HIN_SDS_ACCESS_TOKEN);
                if (!File.Exists(filePath)) return null;
                string jsonStr = File.ReadAllText(filePath);
                if (jsonStr == null) return null;
                return OAuthTokens.FromJSON(jsonStr);
            }
            set
            {
                var filesDir = Utilities.AppRoamingDataFolder();
                var filePath = Path.Combine(filesDir, Constants.HIN_SDS_ACCESS_TOKEN);
                if (value == null) {
                    File.Delete(filePath);
                } else
                {
                    var jsonStr = value.ToJSON();
                    File.WriteAllText(filePath, jsonStr);
                }
            }
        }

        public OAuthTokens? ADSwissAccessToken
        {
            get
            {
                var filesDir = Utilities.AppRoamingDataFolder();
                var filePath = Path.Combine(filesDir, Constants.HIN_ADSWISS_ACCESS_TOKEN);
                if (!File.Exists(filePath)) return null;
                string jsonStr = File.ReadAllText(filePath);
                if (jsonStr == null) return null;
                return OAuthTokens.FromJSON(jsonStr);
            }
            set
            {
                var filesDir = Utilities.AppRoamingDataFolder();
                var filePath = Path.Combine(filesDir, Constants.HIN_ADSWISS_ACCESS_TOKEN);
                if (value == null)
                {
                    File.Delete(filePath);
                }
                else
                {
                    var jsonStr = value.ToJSON();
                    File.WriteAllText(filePath, jsonStr);
                }
            }
        }

        public AuthHandle? ADSwissAuthHandle
        {
            get
            {
                var filesDir = Utilities.AppRoamingDataFolder();
                var filePath = Path.Combine(filesDir, Constants.HIN_ADSWISS_AUTH_HANDLE);
                if (!File.Exists(filePath)) return null;
                string jsonStr = File.ReadAllText(filePath);
                if (jsonStr == null) return null;
                var authHandle = AuthHandle.FromJSON(jsonStr);
                if (authHandle.IsExpired()) return null;
                return authHandle;
            }
            set
            {
                var filesDir = Utilities.AppRoamingDataFolder();
                var filePath = Path.Combine(filesDir, Constants.HIN_ADSWISS_AUTH_HANDLE);
                if (value == null)
                {
                    File.Delete(filePath);
                    return;
                }
                var jsonStr = value.ToJSON();
                File.WriteAllText(filePath, jsonStr);
            }
        }
    }
}
