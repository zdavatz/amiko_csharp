using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AmiKoWindows.Source.HINClient
{
    class AuthHandle
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastUsedAt { get; set; }

        public bool IsExpired()
        {
            if (this.ExpiresAt.CompareTo(DateTime.Now) <= 0 ||
                // Expires after 2 hours of idle
                this.LastUsedAt.AddHours(2).CompareTo(DateTime.Now) <= 0)
            {
                return true;
            }
            return false;
        }

        public string ToJSON()
        {
            string str = JsonConvert.SerializeObject(new AuthHandleJSONPresenter(this), Formatting.Indented);
            return str;
        }

        public static AuthHandle FromJSON(string jsonStr)
        {
            AuthHandleJSONPresenter o = JsonConvert.DeserializeObject<AuthHandleJSONPresenter>(jsonStr);
            return o.AuthHandle;
        }

        public static AuthHandle FromResponseJSON(string jsonStr)
        {
            AuthHandleResponseJSONPresenter o = JsonConvert.DeserializeObject<AuthHandleResponseJSONPresenter>(jsonStr);
            var a = new AuthHandle();
            a.ExpiresAt = DateTime.Now.AddHours(12);
            a.Token = o.authHandle;
            a.LastUsedAt = DateTime.Now;
            return a;
        }
    }

    class AuthHandleJSONPresenter
    {
        public string token { get; set; }
        public DateTime expires_at { get; set; }
        public DateTime last_used_at { get; set; }

        // for serialisation
        public AuthHandleJSONPresenter() { }


        public AuthHandleJSONPresenter(AuthHandle a)
        {
            this.AuthHandle = a;
        }

        [JsonIgnore]
        public AuthHandle AuthHandle
        {
            get
            {
                var a = new AuthHandle();
                a.Token = this.token;
                a.ExpiresAt = this.expires_at;
                a.LastUsedAt = this.last_used_at;
                return a;
            }
            set
            {
                this.token = value.Token;
                this.expires_at = value.ExpiresAt;
                this.last_used_at = value.LastUsedAt;
            }
        }
    }

    class AuthHandleResponseJSONPresenter
    {
        public string authHandle { get; set; }
    }
}
