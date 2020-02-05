using System;
using System.IO;
using System.Net;

namespace WindowsFormsApp2
{
    public enum HttpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }
    public enum AuthenticationType
    {
        BASIC
    }
    class Rest
    {
        public string endpoint { get; set; }
        public HttpVerb httpMethod { get; set; }
        public string token { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public AuthenticationType authType { get; set; }
        public string body { get; set; }


        public string MakeRequest()
        {
            var strResponseValue = String.Empty;
            var request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = httpMethod.ToString();
            HttpWebResponse response = null;
          //    string AuthHeader = System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(APIKEY));
            if(token != String.Empty)
            {
	            request.Headers.Add("X-Auth-Token", token);
            }

            if( (request.Method == "POST" || request.Method == "PUT") && body != String.Empty)
            {
                request.ContentType = "application/json";
                //request.Headers.Add("x-api-key", "W71Y8LB5hwFG+dza");
                //MessageBox.Show(request.ContentType);
                using (var swJsonPayload = new StreamWriter(request.GetRequestStream()))       //write 'body' into 'request'
                {
                    swJsonPayload.Write(body);
                    swJsonPayload.Close();
                }
            }

            try
            {
                response = (HttpWebResponse)request.GetResponse();                                      //make the request

                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                strResponseValue = reader.ReadToEnd();
                            }
                        }
                    }
            }
            catch (Exception e)
            {
                if (e.Message.ToString().Contains("500"))
                {
	                strResponseValue = "Error: Wrong API KEY" ;
                }
                else
                {
	                strResponseValue = "Error:" + e.Message.ToString();
                }
            }
            finally
            {
                if(response != null)
                {
                    ((IDisposable)response).Dispose();
                }
            }
                return strResponseValue;
        }
        
    }
}