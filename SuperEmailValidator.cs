using DnsClient;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SuperEmailValidator
{
    public class SuperEmailValidator
    {
        private static IPEndPoint _ipEndPoint = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
        private static LookupClient _lookupClient = new LookupClient(_ipEndPoint);
        private static string[] _outlookDomains = new string[] { "hotmail.", "live.", "outlook.", "passport.", "windowslive.", "msn." };

        public static bool IsEmailValid
        (
            string email,
            bool validateRegex = true,
            bool validateDisposable = true,
            bool validateDomain = true,
            bool validateGmail = true,
            bool validateOutlook = true
        )
        {
            if (validateRegex && !Regex.IsMatch(email,
                @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"))
            {
                return false;
            }

            string emailDomain = email.Split('@')[1];

            if (validateDisposable)
            {
                foreach (string domain in SplitToLines(Properties.Resources.disposable_mail_domain_list))
                {
                    if (emailDomain.ToLower().Equals(domain.ToLower()))
                    {
                        return false;
                    }
                }
            }

            if (validateDomain)
            {
                try
                {
                    IDnsQueryResponse theQuery = _lookupClient.Query(emailDomain, QueryType.ANY);

                    if (theQuery.Answers.Count == 0)
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            if (validateGmail)
            {
                if (emailDomain.ToLower().Trim().Equals("gmail.com"))
                {
                    if (!IsGmailValid(email))
                    {
                        return false;
                    }
                }
            }

            if (validateOutlook)
            {
                foreach (string outlookDomain in _outlookDomains)
                {
                    if (emailDomain.ToLower().Trim().StartsWith(outlookDomain))
                    {
                        if (!IsOutlookValid(email))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static IEnumerable<string> SplitToLines(string input)
        {
            if (input == null)
            {
                yield break;
            }

            using (StringReader reader = new StringReader(input))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        private static bool IsSmtpEmailValid(string smtpServer, string emailAddress)
        {
            try
            {
                TcpClient tClient = new TcpClient("gmail-smtp-in.l.google.com", 25);
                string CRLF = "\r\n";
                byte[] dataBuffer;
                string ResponseString;
                NetworkStream netStream = tClient.GetStream();
                StreamReader reader = new StreamReader(netStream);
                ResponseString = reader.ReadLine();

                dataBuffer = BytesFromString("HELO Hi" + CRLF);
                netStream.Write(dataBuffer, 0, dataBuffer.Length);
                ResponseString = reader.ReadLine();
                dataBuffer = BytesFromString("MAIL FROM:<expobotofficial@gmail.com>" + CRLF);
                netStream.Write(dataBuffer, 0, dataBuffer.Length);
                ResponseString = reader.ReadLine();

                dataBuffer = BytesFromString($"RCPT TO:<{emailAddress}>" + CRLF);
                netStream.Write(dataBuffer, 0, dataBuffer.Length);
                ResponseString = reader.ReadLine();
                int responseCode = GetResponseCode(ResponseString);

                if (responseCode == 550)
                {
                    return false;
                }

                dataBuffer = BytesFromString("QUITE" + CRLF);
                netStream.Write(dataBuffer, 0, dataBuffer.Length);
                tClient.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsGmailValid(string emailAddress)
        {
            return IsSmtpEmailValid("gmail-smtp-in.l.google.com", emailAddress);
        }

        private static bool IsOutlookValid(string emailAddress)
        {
            return IsSmtpEmailValid("outlook-com.olc.protection.outlook.com", emailAddress);
        }

        private static byte[] BytesFromString(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        private static int GetResponseCode(string ResponseString)
        {
            return int.Parse(ResponseString.Substring(0, 3));
        }
    }
}