using System;
using System.Text;

namespace FtpClientSample
{
    internal class FtpResponse
    {
        public string[] Commands { get; set; }
        public uint ReplyCode { get; set; }
        public uint DataPort { get; set; }
        public uint FileLength { get; set; }

        internal FtpResponse(string[] commands)
        {
            if (commands == null)
            {
                throw new ArgumentNullException("commands");
            }

            Commands = commands;
            ParseCommands();
        }

        private void ParseCommands()
        {
            foreach (var command in Commands)
            {
                string codeString = command.Substring(0, 3);
                uint code;
                if (UInt32.TryParse(codeString, out code))
                {
                    ReplyCode = code;
                    switch (code)
                    {
                        case 213:
                            ParseCode213(command);
                            break;
                        case 229:
                            ParseCode229(command);
                            break;
                    }
                }
            }
        }

        private void ParseCode213(string command)
        {
            int index = command.IndexOf(" ");

            if (index < 0)
            {
                return;
            }

            uint fileLength;
            if (UInt32.TryParse(command.Substring(index + 1), out fileLength))
            {
                FileLength = fileLength;
            }
        }

        private void ParseCode229(string command)
        {
            int prefixIndex = command.IndexOf("|||");

            if (prefixIndex < 0)
            {
                return;
            }

            int postfixIndex = command.IndexOf("|", prefixIndex + 3);

            if (prefixIndex < 0)
            {
                return;
            }

            uint port;
            if (UInt32.TryParse(command.Substring(prefixIndex + 3, postfixIndex - prefixIndex - 3), out port))
            {
                DataPort = port;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var command in Commands)
            {
                builder.AppendLine(command);
            }
            return builder.ToString();
        }
    }
}