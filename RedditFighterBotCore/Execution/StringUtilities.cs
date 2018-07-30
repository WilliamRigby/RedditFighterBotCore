using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RedditFighterBot.Execution
{
    public static class StringUtilities
    {
        private static Regex RESregex = new Regex("\\[.*\\]");
        
        public static string GetRequestStringFromComment(string body)
        {
            string[] commentLines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string request = "";
            for (int i = 0; i < commentLines.Length; i++)
            {
                if (commentLines[i].Contains("u/") == true)
                {
                    request = commentLines[i];
                    break;
                }
            }

            return request;
        }

        public static bool DetermineIfPartial(string request)
        {
            string[] words = request.Split(' ');

            string s = words[words.Length - 1];

            bool isNumeric = int.TryParse(s, out int n);

            return isNumeric;
        }

        public static string RemoveRESUpvoteNumbers(string request)
        {
            string result = RESregex.Replace(request, "");

            return result.Trim();
        }

        public static string RemoveBotName(string body, string botname)
        {
            body = body.Replace("/u/", "");
            body = body.Replace("u/", "");
            body = body.Replace(botname, "");

            return body.Trim();
        }

        public static int GetUserRequestSize(string body)
        {
            string[] words = body.Split(' ');

            string s = words[words.Length - 1];

            bool isNumeric = int.TryParse(s, out int n);

            if (isNumeric == true)
            {
                return n;
            }
            else
            {
                return -1;
            }
        }

        public static List<string> GetFighters(string text)
        {
            string[] fighters = text.Split(',');

            List<string> list = new List<string>();

            for (int i = 0; i < fighters.Length; i++)
            {
                fighters[i] = fighters[i].Trim();

                list.Add(fighters[i]);
            }

            return list;
        }        

        public static string RemoveNumbers(string request)
        {
            string temp = "";

            foreach (char c in request)
            {
                if (char.IsDigit(c) == false && c != '-')
                {
                    temp += c;
                }
            }

            char[] charsToTrim = { '_', ' ' };

            temp = temp.Trim(charsToTrim);

            return temp;
        }

        public static int LevenshteinDistance(char[] s, char[] t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i < d.GetLength(0); i++)
            {
                for (int j = 0; j < d.GetLength(1); j++)
                {
                    d[i, j] = 0;
                }
            }

            for (int i = 1; i < s.Length + 1; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 1; j < t.Length + 1; j++)
            {
                d[0, j] = j;
            }

            for (int i = 1; i < d.GetLength(0); i++)
            {
                for (int j = 1; j < d.GetLength(1); j++)
                {
                    int subCost = s[i - 1] == t[j - 1] ? subCost = 0 : subCost = 1;

                    d[i, j] = Math.Min(d[i - 1, j] + 1,              // deletion
                              Math.Min(d[i, j - 1] + 1,              // insertion
                                       d[i - 1, j - 1] + subCost));  // substitution);
                }
            }

            return d[s.Length, t.Length];
        }

    }
}
