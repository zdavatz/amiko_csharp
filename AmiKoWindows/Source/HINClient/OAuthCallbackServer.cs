using ABI.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AmiKoWindows.Source.HINClient
{
    public class OAuthCallbackServer
    {
        public delegate void CloseCallback();

        CloseCallback callback;
        public OAuthCallbackServer(CloseCallback callback)
        {
            this.callback = callback;
            this.StartServer();
        }

        private HttpListener? listener = null;
        public async Task StartServer()
        {
            if (listener != null) return;
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:23822/");
            listener.Start();

            listener.BeginGetContext(new AsyncCallback(HandleRequest), listener);
        }

        public void StopServer()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
        }

        public void HandleRequest(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            Debug.WriteLine(request.Url?.AbsolutePath);
            Debug.WriteLine(request.Url?.Query);
            var queryParam = HttpUtility.ParseQueryString(request.Url.Query);
            var code = queryParam.Get("code");
            var state = queryParam.Get("state");

            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = "OK! Please go back to the app.";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            response.Close();

            if (code != null && state != null)
            {
                GetAccessTokenFromCodeAndClose(code, state);
            }
            else
            {
                // Keep the server running so we can handle the next request
                listener.BeginGetContext(new AsyncCallback(HandleRequest), listener);
            }
        }

        private async Task GetAccessTokenFromCodeAndClose(string code, string state)
        {
            var tokens = await HINClient.FetchAccessTokenWithAuthCode(code, state);
            if (state == HINClient.SDSApplicationName)
            {
                HINSettingsManager.Instance.SDSAccessToken = tokens;
            } else if (state == HINClient.ADSwissApplicationName) {
                HINSettingsManager.Instance.ADSwissAccessToken = tokens;
            }
            StopServer();
            this.callback();

            var profile = await HINClient.FetchSDSProfile(tokens);
            var account = Account.Read() ?? new Account();
            profile.MergeToAccount(account);
            account.Save();
        }
    }
}
