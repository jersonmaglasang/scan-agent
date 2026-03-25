using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScanAgent
{
    public class WebSocketServer : IDisposable
    {
        private TcpListener listener;
        private List<TcpClient> clients;
        private Thread listenerThread;
        private bool isRunning;
        private int port;
        private string currentSessionWebhookUrl;

        public string CurrentSessionWebhookUrl
        {
            get { return currentSessionWebhookUrl; }
        }

        public WebSocketServer(int serverPort)
        {
            port = serverPort;
            clients = new List<TcpClient>();
            currentSessionWebhookUrl = string.Empty;
        }

        public void Start()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;

                listenerThread = new Thread(ListenForClients);
                listenerThread.IsBackground = true;
                listenerThread.Start();

                Logger.Instance.Log("WebSocket server listening on port " + port);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Failed to start WebSocket server: " + ex.Message);
            }
        }

        public void Stop()
        {
            isRunning = false;

            if (listener != null)
            {
                listener.Stop();
            }

            lock (clients)
            {
                foreach (TcpClient client in clients)
                {
                    try
                    {
                        client.Close();
                    }
                    catch { }
                }
                clients.Clear();
            }

            Logger.Instance.Log("WebSocket server stopped");
        }

        private void ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    if (listener.Pending())
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                        clientThread.IsBackground = true;
                        clientThread.Start(client);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Logger.Instance.Log("Error accepting client: " + ex.Message);
                    }
                }
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = null;

            try
            {
                stream = client.GetStream();

                // Perform WebSocket handshake
                if (!PerformHandshake(stream))
                {
                    client.Close();
                    return;
                }

                lock (clients)
                {
                    clients.Add(client);
                }

                Logger.Instance.Log("New WebSocket client connected");

                // Send connected message
                SendMessage(client, CreateMessage("connected", new Dictionary<string, object>
                {
                    { "status", "connected" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                }));

                // Read messages from client
                while (isRunning && client.Connected)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }

                    string message = DecodeWebSocketFrame(buffer, bytesRead);
                    if (!string.IsNullOrEmpty(message))
                    {
                        HandleMessage(client, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("WebSocket client error: " + ex.Message);
            }
            finally
            {
                lock (clients)
                {
                    clients.Remove(client);
                }

                if (stream != null)
                {
                    stream.Close();
                }

                client.Close();
                Logger.Instance.Log("WebSocket client disconnected");
            }
        }

        private bool PerformHandshake(NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Extract WebSocket key
                Match keyMatch = Regex.Match(request, "Sec-WebSocket-Key: (.*)");
                if (!keyMatch.Success)
                {
                    return false;
                }

                string key = keyMatch.Groups[1].Value.Trim();
                string acceptKey = ComputeWebSocketAcceptKey(key);

                // Send handshake response
                StringBuilder response = new StringBuilder();
                response.AppendLine("HTTP/1.1 101 Switching Protocols");
                response.AppendLine("Upgrade: websocket");
                response.AppendLine("Connection: Upgrade");
                response.AppendLine("Sec-WebSocket-Accept: " + acceptKey);
                response.AppendLine();

                byte[] responseBytes = Encoding.UTF8.GetBytes(response.ToString());
                stream.Write(responseBytes, 0, responseBytes.Length);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string ComputeWebSocketAcceptKey(string key)
        {
            string combined = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hash);
        }

        private string DecodeWebSocketFrame(byte[] buffer, int length)
        {
            try
            {
                if (length < 2) return null;

                bool isMasked = (buffer[1] & 0x80) != 0;
                int payloadLength = buffer[1] & 0x7F;
                int offset = 2;

                if (payloadLength == 126)
                {
                    payloadLength = (buffer[2] << 8) | buffer[3];
                    offset = 4;
                }
                else if (payloadLength == 127)
                {
                    offset = 10; // Skip 8-byte length
                }

                byte[] mask = new byte[4];
                if (isMasked)
                {
                    Array.Copy(buffer, offset, mask, 0, 4);
                    offset += 4;
                }

                byte[] payload = new byte[payloadLength];
                for (int i = 0; i < payloadLength; i++)
                {
                    if (isMasked)
                    {
                        payload[i] = (byte)(buffer[offset + i] ^ mask[i % 4]);
                    }
                    else
                    {
                        payload[i] = buffer[offset + i];
                    }
                }

                return Encoding.UTF8.GetString(payload);
            }
            catch
            {
                return null;
            }
        }

        private void HandleMessage(TcpClient client, string message)
        {
            try
            {
                // Simple JSON parsing for .NET 3.5
                if (message.Contains("\"type\""))
                {
                    if (message.Contains("\"start_scan\""))
                    {
                        // Extract webhook URL from message
                        Match match = Regex.Match(message, "\"webHookUrl\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success)
                        {
                            currentSessionWebhookUrl = match.Groups[1].Value;
                            Logger.Instance.Log("Scan session started with webhook: " + currentSessionWebhookUrl);

                            SendMessage(client, CreateMessage("scan_session_started", new Dictionary<string, object>
                            {
                                { "status", "started" },
                                { "webHookUrl", currentSessionWebhookUrl }
                            }));
                        }
                    }
                    else if (message.Contains("\"end_scan\""))
                    {
                        currentSessionWebhookUrl = string.Empty;
                        Logger.Instance.Log("Scan session ended");

                        SendMessage(client, CreateMessage("scan_session_ended", new Dictionary<string, object>
                        {
                            { "status", "ended" }
                        }));
                    }
                    else if (message.Contains("\"get_logs\""))
                    {
                        string logs = Logger.Instance.GetTodayLogs();
                        SendMessage(client, CreateMessage("logs_response", new Dictionary<string, object>
                        {
                            { "logs", logs },
                            { "timestamp", DateTime.UtcNow.ToString("o") }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Error handling message: " + ex.Message);
            }
        }

        private void SendMessage(TcpClient client, string message)
        {
            try
            {
                if (client == null || !client.Connected) return;

                byte[] payload = Encoding.UTF8.GetBytes(message);
                byte[] frame = EncodeWebSocketFrame(payload);

                NetworkStream stream = client.GetStream();
                stream.Write(frame, 0, frame.Length);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Error sending message: " + ex.Message);
            }
        }

        private byte[] EncodeWebSocketFrame(byte[] payload)
        {
            int payloadLength = payload.Length;
            int frameSize;
            int payloadStartIndex;

            if (payloadLength < 126)
            {
                frameSize = 2 + payloadLength;
                payloadStartIndex = 2;
            }
            else if (payloadLength < 65536)
            {
                frameSize = 4 + payloadLength;
                payloadStartIndex = 4;
            }
            else
            {
                frameSize = 10 + payloadLength;
                payloadStartIndex = 10;
            }

            byte[] frame = new byte[frameSize];
            frame[0] = 0x81; // Text frame, FIN bit set

            if (payloadLength < 126)
            {
                frame[1] = (byte)payloadLength;
            }
            else if (payloadLength < 65536)
            {
                frame[1] = 126;
                frame[2] = (byte)(payloadLength >> 8);
                frame[3] = (byte)(payloadLength & 0xFF);
            }
            else
            {
                frame[1] = 127;
                // Write 8-byte length (simplified for .NET 3.5)
                for (int i = 0; i < 8; i++)
                {
                    frame[2 + i] = (byte)((payloadLength >> (8 * (7 - i))) & 0xFF);
                }
            }

            Array.Copy(payload, 0, frame, payloadStartIndex, payloadLength);
            return frame;
        }

        public static string EscapeJsonValue(Object value)
        {
            string str = (value ?? "").ToString();
            if (string.IsNullOrEmpty(str)) return "";

            return str
                .Replace("\\", "\\\\")   // Escape backslashes first!
                .Replace("\"", "\\\"")   // Escape double quotes
                .Replace("\b", "\\b")    // Backspace
                .Replace("\f", "\\f")    // Form feed
                .Replace("\n", "\\n")    // New line
                .Replace("\r", "\\r")    // Carriage return
                .Replace("\t", "\\t");   // Tab
        }

        private string CreateMessage(string type, Dictionary<string, object> data)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{");
            json.Append("\"type\":\"" + EscapeJsonValue(type) + "\",");
            json.Append("\"data\":{");

            bool first = true;
            foreach (KeyValuePair<string, object> kvp in data)
            {
                if (!first) json.Append(",");
                json.Append("\"" + EscapeJsonValue(kvp.Key) + "\":\"" + EscapeJsonValue(kvp.Value?.ToString()) + "\"");
                first = false;
            }

            json.Append("}}");
            return json.ToString().Trim();
        }

        public void BroadcastFileDetected(string filePath, string fileName)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["filePath"] = filePath;
            data["fileName"] = fileName;
            data["timestamp"] = DateTime.UtcNow.ToString("o");

            Broadcast(CreateMessage("file_detected", data));
        }

        public void BroadcastFileUploaded(string filePath, string fileName, string s3Url)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["filePath"] = filePath;
            data["fileName"] = fileName;
            data["s3BucketUrl"] = s3Url ?? string.Empty;
            data["timestamp"] = DateTime.UtcNow.ToString("o");

            Broadcast(CreateMessage("file_uploaded", data));
        }

        private void Broadcast(string message)
        {
            lock (clients)
            {
                foreach (TcpClient client in clients)
                {
                    SendMessage(client, message);
                }
            }
        }

        public void BroadcastMessage(Dictionary<string, object> message)
        {
            string jsonMessage = CreateMessageFromDictionary(message);
            Broadcast(jsonMessage);
        }

        private string CreateMessageFromDictionary(Dictionary<string, object> message)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{");

            bool first = true;
            foreach (KeyValuePair<string, object> kvp in message)
            {
                if (!first) json.Append(",");

                json.Append("\"" + kvp.Key + "\":");

                if (kvp.Value is Dictionary<string, object>)
                {
                    json.Append(CreateMessageFromDictionary((Dictionary<string, object>)kvp.Value));
                }
                else
                {
                    json.Append("\"" + kvp.Value + "\"");
                }

                first = false;
            }

            json.Append("}");
            return json.ToString();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

