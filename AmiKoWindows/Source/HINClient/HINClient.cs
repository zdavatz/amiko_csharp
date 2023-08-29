using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace AmiKoWindows.Source.HINClient
{
    class HINClient
    {
        static public string SDSApplicationName = "hin_sds";
        static public string ADSwissApplicationName
        {
            get
            {
#if DEBUG
                return "ADSwiss_CI-Test";
#else
                return "ADSwiss_CI";
#endif
            }
        }

        static public string SDSOAuthURL()
        {
            return String.Format(
                "https://apps.hin.ch/REST/v1/OAuth/GetAuthCode/{0}?response_type=code&client_id={1}&redirect_uri={2}&state={3}",
                SDSApplicationName,
                HINClientCredentials.ClientId,
                OAuthCallbackURL(),
                SDSApplicationName
                );
        }
        static public string ADSwissAuthURL()
        {
            return String.Format(
                "https://apps.hin.ch/REST/v1/OAuth/GetAuthCode/{0}?response_type=code&client_id={1}&redirect_uri={2}&state={3}",
                ADSwissApplicationName,
                HINClientCredentials.ClientId,
                OAuthCallbackURL(),
                ADSwissApplicationName
                );
        }
        static public string OAuthCallbackURL()
        {
            return "http://localhost:23822/callback";
        }
        static public async Task<OAuthTokens> FetchAccessTokenWithAuthCode(string authCode)
        {
            var client = new HttpClient();
            var endpoint = "https://oauth2.hin.ch/REST/v1/OAuth/GetAccessToken";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", OAuthCallbackURL()),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("client_id", HINClientCredentials.ClientId),
                new KeyValuePair<string, string>("client_secret", HINClientCredentials.ClientSecret),
            });

            var result = await client.PostAsync(endpoint, content);
            var responseStr = await result.Content.ReadAsStringAsync();
            var tokens = OAuthTokens.FromResponseJSON(responseStr);
            return tokens;
        }
    }
}
