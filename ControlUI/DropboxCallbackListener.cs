using System;
using System.Net;
using System.Threading;
using DropNet;
using DropNet.Models;

namespace ControlUI
{
    public class DropboxCallbackListener
    {
        public ManualResetEvent Done { get; set; }
        private readonly DropNetClient _client;
        const string ResponseString = "<HTML><BODY>You can close the page now</BODY></HTML>"; // a very basic response to show to a user upon successful authorization
        public UserLogin UserLogin;

        public DropboxCallbackListener(DropNetClient dropNetclient)
        {
            Done = new ManualResetEvent(false);
            _client = dropNetclient;
        }

        public static void ProcessDropboxCallback(object parameters)
        {
            var p = parameters as DropboxCallbackListener;
            if (p == null) throw new ArgumentNullException("parameters");
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add("http://127.0.0.1:64646/callback/");
                listener.Start();                
                var context = listener.GetContext();// this call would block until request is made                
                p.UserLogin = p._client.GetAccessToken();

                var response = context.Response;                                
                var buffer = System.Text.Encoding.UTF8.GetBytes(ResponseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                using (var output = response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();//must close the output stream.
                }                
                listener.Stop();
            }
            p.Done.Set();// set the event so waiting thread knows we're done
        }
    }
}
