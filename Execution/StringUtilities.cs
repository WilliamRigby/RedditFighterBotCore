using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RedditFighterBot
{
    public static class StringUtilities
    {
        private static Regex RESregex = new Regex("\\[.*\\]");

        private static Dictionary<char, char> CharMappings = new Dictionary<char, char>()
        {
            {'ã', 'a'},
            {'á', 'a'},
            {'à', 'a'},
            {'â', 'a'},
            {'ç', 'c'},
            {'é', 'e'},
            {'ê', 'e'},
            {'í', 'i'},
            {'ó', 'o'},
            {'õ', 'o'},
            {'ô', 'o'},
            {'ú', 'u'},
            {'ü', 'u'},
            {'ñ', 'n'},
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns>line of comment for request</returns>
        public static string GetRequestStringFromComment(string body)
        {

            //first edit the string and extract parameters
            string[] comment_lines = body.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string fighters_string = "";
            for (int i = 0; i < comment_lines.Length; i++)
            {
                if (comment_lines[i].Contains("u/") == true)
                {
                    fighters_string = comment_lines[i];
                }
            }

            return fighters_string;
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

        /*this method attempts to take commonly user inputted fighter names and resolve them to more correct ones */
        public static string FighterNameFixes(string fighter)
        {
            if (fighter.ToLower() == "julio cesar chavez" || fighter.ToLower() == "chavez")
            {
                return "Julio César Chávez";
            }
            else if (fighter.ToLower() == "julio cesar chavez jr." || fighter.ToLower() == "julio cesar chavez jr"
                      || fighter.ToLower() == "julio cesar chavez, jr" || fighter.ToLower() == "julio cesar chavez jr.")
            {
                return "Julio César Chávez Jr.";
            }
            else if (fighter.ToLower() == "roy jones jr" || fighter.ToLower() == "roy jones, jr"
                            || fighter.ToLower() == "roy jones jr." || fighter.ToLower() == "rjj")
            {
                return "Roy_Jones_Jr.";
            }
            else if (fighter.ToLower() == "hopkins")
            {
                return "Bernard Hopkins";
            }
            else if (fighter.ToLower() == "sergio martinez" || fighter.ToLower() == "sergio martínez")
            {
                return "Sergio_Martínez_(boxer)";
            }
            else if (fighter.ToLower() == "juan manuel marquez" || fighter.ToLower() == "juan marquez" || fighter.ToLower() == "marquez")
            {
                return "Juan_Manuel_Márquez";
            }
            else if (fighter.ToLower() == "sugar ray robinson" || fighter.ToLower() == "srr")
            {
                return "Sugar_Ray_Robinson";
            }
            else
            {
                return fighter;
            }
        }

        public static List<string> ReplaceNonEnglishChars(List<string> fighters)
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                foreach (var pair in CharMappings)
                {
                    fighters[i] = fighters[i].Replace(pair.Key, pair.Value);
                }
            }

            return fighters;
        }

        public static string RemoveNumbers(string request)
        {
            string temp = "";

            foreach (char c in request)
            {
                if (char.IsDigit(c) == false)
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
