using DnsClient;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace SuperEmailValidator
{
    public class SuperEmailValidator
    {
        private static IPEndPoint _ipEndPoint = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
        private static LookupClient _lookupClient = new LookupClient(_ipEndPoint);

        public static bool IsEmailValid
        (
            string email,
            bool validateRegex = true,
            bool validateDisposable = true,
            bool validateDomain = true
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

            return true;
        }

        private static IEnumerable<string> SplitToLines(string input)
        {
            if (input == null)
            {
                yield break;
            }

            using (System.IO.StringReader reader = new System.IO.StringReader(input))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}