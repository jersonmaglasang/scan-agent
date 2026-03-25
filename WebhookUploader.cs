using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace ScanAgent
{
    public delegate void UploadCallback(bool success, string response, string error);

    public static class WebhookUploader
    {
        public static void UploadFile(string filePath, string webhookUrl, UploadCallback callback)
        {
            try
            {
                var fileStream = File.OpenRead(filePath);
                var streamContent = new StreamContent(fileStream);
                var formData = new MultipartFormDataContent();
                formData.Add(streamContent, "file", Path.GetFileName(filePath));

                using (var client = new HttpClient())
                {
                    var response = client.PostAsync(webhookUrl, formData).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        callback(true, json, null);
                    }
                    else
                    {
                        callback(true, "{}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                callback(false, null, ex.Message);
            }
        }
        public static void UploadFile1(string filePath, string webhookUrl, UploadCallback callback)
        {
            try
            { 
                var client = new WebClient();
                client.Credentials = CredentialCache.DefaultCredentials;
                var fileName = Path.GetFileName(filePath);
                var bytes = client.UploadFile(webhookUrl, "POST", filePath);
                var json = Encoding.ASCII.GetString(bytes);
                callback(true, json, null);
            }
            catch (Exception ex)
            {
                callback(false, null, ex.Message);
            }
        }

        public static void UploadFile2(string filePath, string webhookUrl, UploadCallback callback)
        {
            // Upload on background thread
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string boundary = "----Boundary" + DateTime.Now.Ticks.ToString("x");
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webhookUrl);
                    request.Method = "POST";
                    request.ContentType = "multipart/form-data; boundary=" + boundary;
                    request.KeepAlive = true;

                    using (Stream requestStream = request.GetRequestStream())
                    {
                        // Write file data
                        WriteFilePart(requestStream, boundary, filePath);

                        // Write metadata
                        WriteFormField(requestStream, boundary, "fileName", Path.GetFileName(filePath));
                        WriteFormField(requestStream, boundary, "fileSize", new FileInfo(filePath).Length.ToString());
                        WriteFormField(requestStream, boundary, "timestamp", DateTime.UtcNow.ToString("o"));

                        // Write closing boundary
                        byte[] endBoundary = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                        requestStream.Write(endBoundary, 0, endBoundary.Length);
                    }

                    // Get response
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            string responseText = reader.ReadToEnd();
                            
                            if (response.StatusCode == HttpStatusCode.OK || 
                                response.StatusCode == HttpStatusCode.Created)
                            {
                                callback(true, responseText, null);
                            }
                            else
                            {
                                callback(false, null, "HTTP " + (int)response.StatusCode + ": " + responseText);
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    string errorMessage = ex.Message;
                    if (ex.Response != null)
                    {
                        using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            errorMessage += ": " + reader.ReadToEnd();
                        }
                    }
                    callback(false, null, errorMessage);
                }
                catch (Exception ex)
                {
                    callback(false, null, ex.Message);
                }
            });
        }

        private static void WriteFilePart(Stream stream, string boundary, string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string mimeType = GetMimeType(filePath);

            StringBuilder header = new StringBuilder();
            header.AppendLine("--" + boundary);
            header.AppendLine("Content-Disposition: form-data; name=\"file\"; filename=\"" + fileName + "\"");
            header.AppendLine("Content-Type: " + mimeType);
            header.AppendLine();

            byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);

            // Write file content
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                }
            }
        }

        private static void WriteFormField(Stream stream, string boundary, string name, string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("--" + boundary);
            sb.AppendLine("Content-Disposition: form-data; name=\"" + name + "\"");
            sb.AppendLine();
            sb.Append(value);

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".tiff":
                case ".tif":
                    return "image/tiff";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "application/octet-stream";
            }
        }
    }
}

