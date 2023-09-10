using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiKoWindows.Source.HINClient
{
    public class ADSwissSAMLResponse
    {
        public string URL;
        public string EPDAutURL;

        public static ADSwissSAMLResponse FromResponseJSON(string jsonStr)
        {
            ADSwissSAMLResponseJSONPresenter o = JsonConvert.DeserializeObject<ADSwissSAMLResponseJSONPresenter>(jsonStr);
            var r = new ADSwissSAMLResponse();
            r.URL = o.url;
            r.EPDAutURL = o.epdAuthUrl;
            return r;
        }
    }
    internal class ADSwissSAMLResponseJSONPresenter
    {
        public string url;
        public string epdAuthUrl;
    }
}
