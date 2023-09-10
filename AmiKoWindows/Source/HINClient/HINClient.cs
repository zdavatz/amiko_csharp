using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
        static public string HINDomainForADSwiss()
        {
#if DEBUG
            return "oauth2.ci-prep.adswiss.hin.ch";
#else
            return "oauth2.ci.adswiss.hin.ch";
#endif
        }

        static public string CertifactionDomain()
        {
#if DEBUG
            return HINClientCredentials.CertifactionTestServer;
#else
            return HINClientCredentials.CertifactionServer;
#endif
        }

        static public string OAuthCallbackURL()
        {
            return "http://localhost:23822/callback";
        }
        static public async Task<OAuthTokens> FetchAccessTokenWithAuthCode(string authCode, string state)
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
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint),
                Method = HttpMethod.Post,
                Content = content,
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" }
                }
            };

            var result = await client.SendAsync(request);
            var responseStr = await result.Content.ReadAsStringAsync();
            var app = state == HINClient.SDSApplicationName ? OAuthTokens.Application.SDS : OAuthTokens.Application.ADSwiss;
            var tokens = OAuthTokens.FromResponseJSON(responseStr, app);
            return tokens;
        }

        static public async Task<OAuthTokens> RenewTokensIfNeeded(OAuthTokens tokens)
        {
            if (tokens.ExpiresAt > DateTime.UtcNow)
            {
                return tokens;
            }
            var client = new HttpClient();
            var endpoint = "https://oauth2.hin.ch/REST/v1/OAuth/GetAccessToken";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("redirect_uri", OAuthCallbackURL()),
                new KeyValuePair<string, string>("refresh_token", tokens.RefreshToken),
                new KeyValuePair<string, string>("client_id", HINClientCredentials.ClientId),
                new KeyValuePair<string, string>("client_secret", HINClientCredentials.ClientSecret),
            });
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint),
                Method = HttpMethod.Post,
                Content = content,
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" }
                }
            };

            var result = await client.SendAsync(request);
            var responseStr = await result.Content.ReadAsStringAsync();
            var newTokens = OAuthTokens.FromResponseJSON(responseStr, tokens.App);
            switch (tokens.App)
            {
                case OAuthTokens.Application.SDS:
                    HINSettingsManager.Instance.SDSAccessToken = newTokens;
                    break;
                case OAuthTokens.Application.ADSwiss:
                    HINSettingsManager.Instance.ADSwissAccessToken = newTokens;
                    break;
            }
            return newTokens;
        }

        static public async Task<SDSProfileResponse> FetchSDSProfile(OAuthTokens token)
        {
            token = await RenewTokensIfNeeded(token);
            var client = new HttpClient();
            var endpoint = "https://oauth2.sds.hin.ch/api/public/v1/self/";
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint),
                Method = HttpMethod.Get,
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {token.AccessToken}" },
                }
            };

            var result = await client.SendAsync(request);
            var responseStr = await result.Content.ReadAsStringAsync();
            SDSProfileResponse profile = SDSProfileResponse.FromResponseJSON(responseStr);
            return profile;
        }

        static public async Task<ADSwissSAMLResponse> FetchADSwissSAML(OAuthTokens token)
        {
            token = await RenewTokensIfNeeded(token);
            var client = new HttpClient();
            var endpoint = String.Format("https://{0}/authService/EPDAuth?targetUrl={1}&style=redirect", HINDomainForADSwiss(), OAuthCallbackURL());
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint),
                Method = HttpMethod.Post,
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {token.AccessToken}" },
                }
            };

            var result = await client.SendAsync(request);
            var responseStr = await result.Content.ReadAsStringAsync();
            ADSwissSAMLResponse saml = ADSwissSAMLResponse.FromResponseJSON(responseStr);
            return saml;
        }
    }
}
