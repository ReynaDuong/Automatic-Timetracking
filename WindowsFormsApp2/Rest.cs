using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public enum httpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }
    public enum AuthenticationType
    {
        Basic
    }
    class Rest
    {
        public string endpoint { get; set; }
        public httpVerb httpMethod { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public AuthenticationType AuthType { get; set; }
        public string body { get; set; }


        public string MakeRequest()
        {
            string StrResponseValue = String.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = httpMethod.ToString();
            HttpWebResponse response = null;
          //    string AuthHeader = System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(APIKEY));
            if(Token != String.Empty)
                request.Headers.Add("X-Auth-Token", Token);

            if( (request.Method == "POST" || request.Method == "PUT") && body != String.Empty)
            {
                request.ContentType = "application/json";
                //request.Headers.Add("x-api-key", "W71Y8LB5hwFG+dza");
                //MessageBox.Show(request.ContentType);
                using (StreamWriter swJSONPayload = new StreamWriter(request.GetRequestStream()))       //write 'body' into 'request'
                {
                    swJSONPayload.Write(body);
                    swJSONPayload.Close();
                }
            }

            try
            {
                response = (HttpWebResponse)request.GetResponse();                                      //make the request

                    using (Stream ResponseStream = response.GetResponseStream())
                    {
                        if (ResponseStream != null)
                        {
                            using (StreamReader Reader = new StreamReader(ResponseStream))
                            {
                                StrResponseValue = Reader.ReadToEnd();
                            }
                        }
                    }
            }
            catch (Exception e)
            {
                if (e.Message.ToString().Contains("500"))
                    StrResponseValue = "Error: Wrong API KEY" ;
                else
                StrResponseValue = "Error:" + e.Message.ToString();

            }
            finally
            {
                if(response != null)
                {
                    ((IDisposable)response).Dispose();
                }
            }
                return StrResponseValue;
        }
        
    }
}