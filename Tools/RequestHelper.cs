using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace Tools
{
    public class FormFile
    {
        public string Name { get; set; }

        public string ContentType { get; set; }

        public string FilePath { get; set; }

        public Stream Stream { get; set; }
    }

    public class RequestHelper
    {
        public static HttpWebResponse PostMultipart(string url, Dictionary<string, object> parameters)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            request.Credentials = System.Net.CredentialCache.DefaultCredentials;

            if (parameters != null && parameters.Count > 0)
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    foreach (KeyValuePair<string, object> pair in parameters)
                    {
                        requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                        if (pair.Value is FormFile)
                        {
                            FormFile file = pair.Value as FormFile;
                            string header = "Content-Disposition: form-data; name=\"" + pair.Key + "\"; filename=\"" + file.Name + "\"\r\nContent-Type: " + file.ContentType + "\r\n\r\n";
                            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(header);
                            requestStream.Write(bytes, 0, bytes.Length);
                            
                            byte[] buffer = new byte[4196];
                            int bytesRead;
                            if (file.Stream == null)
                            {
                                // upload from file
                                using (FileStream fileStream = File.OpenRead(file.FilePath))
                                {
                                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                        requestStream.Write(buffer, 0, bytesRead);
                                    fileStream.Close();
                                }
                            }
                            else
                            {
                                // upload from given stream
                                while ((bytesRead = file.Stream.Read(buffer, 0, buffer.Length)) != 0)
                                    requestStream.Write(buffer, 0, bytesRead);
                            }
                        }
                        else
                        {
                            string data = "Content-Disposition: form-data; name=\"" + pair.Key + "\"\r\n\r\n" + pair.Value;
                            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
                            requestStream.Write(bytes, 0, bytes.Length);
                        }
                    }

                    byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                    requestStream.Write(trailer, 0, trailer.Length);
                    requestStream.Close();
                }
            }
            return request.GetResponse() as HttpWebResponse;
        }

        public static HttpWebResponse Post(string url, Dictionary<string, object> parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.KeepAlive = true;
            request.Credentials = System.Net.CredentialCache.DefaultCredentials;

            if (parameters != null && parameters.Count > 0)
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    var data = new StringBuilder();
                    foreach (KeyValuePair<string, object> pair in parameters)
                    {
                        data.AppendFormat("{0}={1}&",pair.Key,pair.Value);
                    }
                    data.Remove(data.Length-1, 1);
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data.ToString());
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();
                }
            }
            return request.GetResponse() as HttpWebResponse;
        }
    }
}
