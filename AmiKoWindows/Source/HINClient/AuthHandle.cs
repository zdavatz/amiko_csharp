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
            return a;
        }
    }

    class AuthHandleJSONPresenter
    {
        public string token { get; set; }
        public DateTime expires_at { get; set; }
        
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
                return a;
            }
            set
            {
                this.token = value.Token;
                this.expires_at = value.ExpiresAt;
            }
        }
    }

    class AuthHandleResponseJSONPresenter
    {
        public string authHandle { get; set; }
    }
}
