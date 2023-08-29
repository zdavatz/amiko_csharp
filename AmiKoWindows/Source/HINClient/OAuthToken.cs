using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AmiKoWindows.Source.HINClient
{
    class OAuthTokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? HINId { get; set; }

        public string ToJSON()
        {
            string str = JsonConvert.SerializeObject(new OAuthTokenJSONPresenter(this), Formatting.Indented);
            return str;
        }

        public static OAuthTokens FromJSON(string jsonStr)
        {
            OAuthTokenJSONPresenter o = JsonConvert.DeserializeObject<OAuthTokenJSONPresenter>(jsonStr);
            return o.OAuthTokens;
        }

        public static OAuthTokens FromResponseJSON(string jsonStr)
        {
            OAuthTokenResponseJSONPresenter o = JsonConvert.DeserializeObject<OAuthTokenResponseJSONPresenter>(jsonStr);
            var tokens = new OAuthTokens();
            tokens.AccessToken = o.access_token;
            tokens.RefreshToken = o.refresh_token;
            tokens.ExpiresAt = DateTime.Now.AddSeconds(o.expires_in);
            tokens.HINId = o.hin_id;
            return tokens;
        }
    }

    class OAuthTokenJSONPresenter
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public DateTime expires_at { get; set; }
        public string? hin_id { get; set; }

        public OAuthTokenJSONPresenter(OAuthTokens tokens)
        {
            this.OAuthTokens = tokens;
        }
        public OAuthTokenJSONPresenter()
        {
            // pass (for deserialization)
        }
        [JsonIgnore]
        public OAuthTokens OAuthTokens
        {
            get
            {
                var o = new OAuthTokens();
                o.AccessToken = this.access_token;
                o.RefreshToken = this.refresh_token;
                o.ExpiresAt = this.expires_at;
                o.HINId = this.hin_id;
                return o;
            }
            set
            {
                var tokens = value;
                this.access_token = tokens.AccessToken;
                this.refresh_token = tokens.RefreshToken;
                this.expires_at = tokens.ExpiresAt;
                this.hin_id = tokens.HINId;
            }
        }
    }

    class OAuthTokenResponseJSONPresenter
    {
        public string access_token;
        public string refresh_token;
        public double expires_in;
        public string? hin_id;
    }
}
