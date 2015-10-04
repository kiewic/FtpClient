using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace FtpClientSample
{
    // FtpClient class is not thread safe.
    internal class FtpClient : IDisposable
    {
        private StreamSocket controlStreamSocket;
        private StreamSocket dataStreamSocket;
        private HostName hostName;

        private DataReader reader;
        private DataWriter writer;
        private AutoResetEvent loadCompleteEvent;
        private Task readTask;
        private List<string> readCommands;
        private string messageLeft;

        internal async Task ConnectAsync(
            HostName hostName,
            string serviceName,
            string user,
            string password)
        {
            if (controlStreamSocket != null)
            {
                throw new InvalidOperationException("Control connection already started.");
            }

            this.hostName = hostName;

            controlStreamSocket = new StreamSocket();
            await controlStreamSocket.ConnectAsync(hostName, serviceName);

            reader = new DataReader(controlStreamSocket.InputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;

            writer = new DataWriter(controlStreamSocket.OutputStream);

            readCommands = new List<string>();
            loadCompleteEvent = new AutoResetEvent(false);
            readTask = InfiniteReadAsync();

            FtpResponse response;
            response = await GetResponseAsync();
            VerifyResponse(response, 220);

            response = await UserAsync(user);
            VerifyResponse(response, 331);

            response = await PassAsync(password);
            VerifyResponse(response, 230);
        }

        internal async Task UploadAsync(
            string filePath,
            byte[] fileContent)
        {
            if (controlStreamSocket == null)
            {
                throw new InvalidOperationException("Call ConnectAsync() first.");
            }

            FtpResponse response;
            response = await TypeAsync("I");
            VerifyResponse(response, 200);

            response = await EpsvAsync();
            VerifyResponse(response, 229);

            await OpenDataConnectionAsync(response.DataPort);

            response = await StorAsync(filePath);
            VerifyResponse(response, new uint[] { 125, 150 });

            await WriteAndCloseAsync(fileContent.AsBuffer());

            response = await GetResponseAsync();
            VerifyResponse(response, 226);
        }

        internal async Task<byte[]> DownloadAsync(
            string filePath)
        {
            if (controlStreamSocket == null)
            {
                throw new InvalidOperationException("Call ConnectAsync() first.");
            }

            FtpResponse response;
            response = await TypeAsync("I");
            VerifyResponse(response, 200);

            response = await EpsvAsync();
            VerifyResponse(response, 229);

            await OpenDataConnectionAsync(response.DataPort);

            response = await SizeAsync(filePath);
            VerifyResponse(response, 213);
            uint fileLength = response.FileLength;

            response = await RetrAsync(filePath);
            VerifyResponse(response, new uint[] { 125, 150 });

            IBuffer buffer = await ReadAndCloseAsync(fileLength);
            Debug.WriteLine(buffer.Length);

            return buffer.ToArray();
        }

        internal async Task OpenDataConnectionAsync(uint port)
        {
            if (dataStreamSocket != null)
            {
                throw new InvalidOperationException("Data connection already started.");
            }

            dataStreamSocket = new StreamSocket();
            await dataStreamSocket.ConnectAsync(hostName, port.ToString());
        }

        internal Task<FtpResponse> UserAsync(string user)
        {
            string command = String.Format("USER {0}\r\n", user);
            return SendCommandAndGetResponseAsync(command);
        }

        internal Task<FtpResponse> PassAsync(string password)
        {
            string command = String.Format("PASS {0}\r\n", password);
            return SendCommandAndGetResponseAsync(command);
        }

        internal Task<FtpResponse> TypeAsync(string type)
        {
            string command = string.Format("TYPE {0}\r\n", type);
            return SendCommandAndGetResponseAsync(command);
        }

        internal Task<FtpResponse> EpsvAsync()
        {
            string command = string.Format("EPSV\r\n");
            return SendCommandAndGetResponseAsync(command);
        }

        internal Task<FtpResponse> SizeAsync(string fileName)
        {
            string command = string.Format("SIZE {0}\r\n", fileName);
            return SendCommandAndGetResponseAsync(command);
        }

        internal Task<FtpResponse> RetrAsync(string fileName)
        {
            string command = string.Format("RETR {0}\r\n", fileName);
            return SendCommandAndGetResponseAsync(command);
        }

        internal Task<FtpResponse> StorAsync(string fileName)
        {
            string command = string.Format("STOR {0}\r\n", fileName);
            return SendCommandAndGetResponseAsync(command);
        }

        internal async Task<IBuffer> ReadAndCloseAsync(uint dataLength)
        {
            IBuffer buffer = new Windows.Storage.Streams.Buffer(dataLength);

            await dataStreamSocket.InputStream.ReadAsync(buffer, dataLength, InputStreamOptions.None);

            dataStreamSocket.Dispose();
            dataStreamSocket = null;

            return buffer;
        }

        internal async Task<uint> WriteAndCloseAsync(IBuffer buffer)
        {
            uint bytesWritten = await dataStreamSocket.OutputStream.WriteAsync(buffer);

            dataStreamSocket.Dispose();
            dataStreamSocket = null;

            return bytesWritten;
        }

        internal Task QuitAsync()
        {
            string command = String.Format("QUIT \r\n");
            return SendCommandAsync(command);
        }

        internal async Task<FtpResponse> SendCommandAndGetResponseAsync(string command)
        {
            loadCompleteEvent.Reset();

            await SendCommandAsync(command);

            return await GetResponseAsync();
        }

        internal async Task SendCommandAsync(string command)
        {
            writer.WriteString(command);
            uint bytesWritten = await writer.StoreAsync();
        }

        internal Task<FtpResponse> GetResponseAsync()
        {
            return Task.Run(() =>
            {
                // Wait for one DataReader.LoadAsync() to complete.
                loadCompleteEvent.WaitOne();

                FtpResponse response = new FtpResponse(readCommands.ToArray());
                readCommands = new List<string>();
                return response;
            });
        }
        internal static void VerifyResponse(FtpResponse response, uint expectedReplyCode)
        {
            VerifyResponse(response, new uint[] { expectedReplyCode });
        }

        internal static void VerifyResponse(FtpResponse response, uint[] expectedReplyCodes)
        {
            foreach (uint expectedReplyCode in expectedReplyCodes)
            {
                if (expectedReplyCode == response.ReplyCode)
                {
                    return;
                }
            }

            throw new Exception(String.Format(
                "FTP: Expected reply code was {0}, however the server replied: {1}",
                JoinRetryCodes(expectedReplyCodes),
                response.ToString().Trim()));
        }

        private static string JoinRetryCodes(uint[] values)
        {
            StringBuilder builder = new StringBuilder();

            foreach (uint value in values)
            {
                if (builder.Length != 0)
                {
                    builder.Append(" or ");
                }
                builder.Append(value.ToString());
            }

            return builder.ToString();
        }

        private async Task InfiniteReadAsync()
        {
            uint bytesRead = 1;
            while (bytesRead > 0)
            {
                bytesRead = await reader.LoadAsync(1000);
                while (reader.UnconsumedBufferLength > 0)
                {
                    string message = reader.ReadString(reader.UnconsumedBufferLength);
                    ProcessLoad(message);
                }

                loadCompleteEvent.Set();
            }
        }

        private void ProcessLoad(string message)
        {
            message = messageLeft + message;
            int index = message.IndexOf("\r\n");

            while (index >= 0)
            {
                string command = message.Substring(0, index);
                readCommands.Add(command);

                message = message.Substring(index + 2);
                index = message.IndexOf("\r\n");
            }

            messageLeft = message;
        }

        public void Dispose()
        {
            if (dataStreamSocket != null)
            {
                dataStreamSocket.Dispose();
                dataStreamSocket = null;
            }

            if (controlStreamSocket != null)
            {
                controlStreamSocket.Dispose();
                controlStreamSocket = null;
            }
        }
    }
}
