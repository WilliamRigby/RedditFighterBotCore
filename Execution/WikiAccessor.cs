using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RedditFighterBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RedditFighterBot
{
    public class WikiAccessor
    {

        public static StringBuilder Builder { get; set; }
        public static int Request_Size { get; set; }
        private static int Row_Count { get; set; }
        private static string Fighter_Type { get; set; }

        public static WikiSearchResultDTO SearchWiki(string fighter)
        {
            JObject json = new JObject();
            
            var myRequest = WebRequest.CreateHttp("https://en.wikipedia.org/w/api.php?action=query&list=search&format=json&srprop=size&srsearch=" + fighter + " fighter");
            myRequest.Method = "GET";
            myRequest.UserAgent = "WebRequestDemo";
            using (var theResponse = myRequest.GetResponse())
            {
                var dataStream = theResponse.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string objResponse = reader.ReadToEnd();

                json = JObject.Parse(objResponse.ToString());

                dataStream.Close();
                theResponse.Close();
            }
            

            // get JSON result objects into a list
            JEnumerable<JToken> results = json["query"]["search"].Children();

            // serialize JSON results into .NET objects
            List<WikiSearchResultDTO> searchResults = new List<WikiSearchResultDTO>();
            foreach (JToken result in results)
            {
                // JToken.ToObject is a helper method that uses JsonSerializer internally
                WikiSearchResultDTO searchResult = result.ToObject<WikiSearchResultDTO>();
                searchResults.Add(searchResult);
            }
            
            if (searchResults.Count > 0)
            {
                foreach (WikiSearchResultDTO result in searchResults)
                {
                    if (result.title.ToLower().Contains("(fighter)"))
                    {
                        result.LevenshteinDistance = StringUtilities.LevenshteinDistance((fighter.ToLower() + " (fighter)").ToCharArray(), result.title.ToLower().ToCharArray());
                    }
                    else
                    {
                        result.LevenshteinDistance = StringUtilities.LevenshteinDistance(fighter.ToLower().ToCharArray(), result.title.ToLower().ToCharArray());
                    }
                }

                searchResults.Sort((x, y) => x.LevenshteinDistance.CompareTo(y.LevenshteinDistance));

                int ShortestDistance = 0;

                List<WikiSearchResultDTO> ties = new List<WikiSearchResultDTO>();

                for (int i = 0; i < searchResults.Count; i++)
                {
                    if (i == 0) { ShortestDistance = searchResults[i].LevenshteinDistance; }

                    if (searchResults[i].LevenshteinDistance == ShortestDistance)
                    {
                        ties.Add(searchResults[i]);
                    }
                }

                for (int i = 0; i < ties.Count; i++)
                {
                    if (ties[i].title.Contains("(fighter)"))
                    {
                        return ties[i];
                    }
                }

                return ties[0];
            }
            else
            {
                return null;
            }
        }

        public static int GetIndex(string fighter)
        {
            JToken json = new JObject();

            
            /* this gets the json for the page table of contents */
            var myRequest = WebRequest.CreateHttp("https://en.wikipedia.org/w/api.php?format=json&action=parse&prop=sections&page=" + fighter);
            myRequest.Method = "GET";
            myRequest.UserAgent = "WebRequestDemo";
            using (var theResponse = myRequest.GetResponse())
            {
                var dataStream = theResponse.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string objResponse = reader.ReadToEnd();

                json = (JToken)JObject.Parse(objResponse.ToString());

                dataStream.Close();
                theResponse.Close();
            }

            JEnumerable<JToken> sections = json["parse"]["sections"].Children();

            // serialize JSON results into .NET objects
            List<WikiTableOfContentDTO> TOC = new List<WikiTableOfContentDTO>();
            foreach (JToken result in sections)
            {
                // JToken.ToObject is a helper method that uses JsonSerializer internally
                WikiTableOfContentDTO index = result.ToObject<WikiTableOfContentDTO>();
                TOC.Add(index);
            }
            
            foreach (WikiTableOfContentDTO section in TOC)
            {
                if (section.anchor.ToLower().Contains("martial_arts_record") == true)
                {
                    Fighter_Type = "mma";
                    return section.index;
                }
            }

            foreach (WikiTableOfContentDTO section in TOC)
            {
                if (section.anchor.ToLower() == "professional_boxing_record" || section.anchor.ToString().ToLower() == "professional_record")
                {
                    Fighter_Type = "boxing";
                    return section.index;
                }
            }

            //if we don't find the index, then return magic number -1 as error code
            return -1;
        }
        
        

        /* this method goes out to the wikipedia api and tries to get the fighter's table
         * on wikipedia the fighter's table is html, but has a consistent formatting  
         * we take advantage of that consistent pattern to be able to parse the table */
        public static void GetEntireTable(string fighter, int index)
        {
            /* keep track of the number of rows which have been created, 
               so that we won't let the comment exceed the reddit limit */
            Row_Count = 1;

            HtmlNodeCollection tables = GetHTMLTables(fighter, index);

            if (tables.Count > 1)
            {
                if (Fighter_Type == "mma")
                {
                    string url = "https://en.wikipedia.org/wiki/" + fighter.Replace(' ', '_') + "#Mixed_martial_arts_record";
                    url = url.Replace("(", "%28");
                    url = url.Replace(")", "%29");
                    Builder.Append("###[" + fighter + "](" + url + ")\n\n");
                    GetOverallRecordTable(tables[0]);
                    Builder.Append(" Res. | Record | Opponent | Type | Date | Rd. | Time\n-----|-----|-----|-----|-----|-----|-----|-----");
                    GetMmaDetailedRecordTable(tables[1]);
                }
                else if (Fighter_Type == "boxing")
                {
                    string url = "https://en.wikipedia.org/wiki/" + fighter.Replace(' ', '_') + "#Professional_boxing_record";
                    url = url.Replace("(", "%28");
                    url = url.Replace(")", "%29");
                    Builder.Append("###[" + fighter + "](" + url + ")\n\n");
                    GetOverallRecordTable(tables[0]);
                    Builder.Append(" Res. | Record | Opponent | Type | Rd., Time | Date\n-----|-----|-----|-----|-----|-----");
                    GetBoxingDetailedRecordTable(tables[1]);
                }
            }
            else if (tables.Count == 1)
            {
                string url = "https://en.wikipedia.org/wiki/" + fighter.Replace(' ', '_') + "#Professional_record";
                url = url.Replace("(", "%28");
                url = url.Replace(")", "%29");
                Builder.Append("###[" + fighter + "](" + url + ")\n\n");
                GetDetailedRecordTableWithTopRecord(tables[0]);
            }

            Builder.Append("\n\n");
        }

        private static HtmlNodeCollection GetHTMLTables(string fighter, int index)
        {
            /* this url will direct to the html for the given fighter */
            JToken json = new JObject();
            
            /* this gets the json for the page table of contents */
            var myRequest = WebRequest.CreateHttp("https://en.wikipedia.org/w/api.php?format=json&action=parse&prop=text&page=" + fighter + "&section=" + index);
            myRequest.Method = "GET";
            myRequest.UserAgent = "WebRequestDemo";

            using (var theResponse = myRequest.GetResponse())
            {
                var dataStream = theResponse.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string objResponse = reader.ReadToEnd();

                json = (JToken)JObject.Parse(objResponse.ToString());

                dataStream.Close();
                theResponse.Close();
            }

            /* get the html which describes the table */
            string s = json.SelectToken("parse.text").ToString();

            HtmlDocument html_table = new HtmlDocument();
            html_table.LoadHtml(s);

            return html_table.DocumentNode.SelectNodes("//table");
        }

        private static void GetOverallRecordTable(HtmlNode node)
        {
            string _total = "";
            string _wins = "";
            string _losses = "";

            foreach (HtmlNode row in node.SelectNodes("tr"))
            {
                foreach (HtmlNode cell in row.SelectNodes("th|td"))
                {
                    if (cell.InnerText.ToLower().Contains("&#160;fights") == true)
                    {
                        _total = cell.InnerText.ToLower().Replace("&#160;fights", "");
                    }
                    else if (cell.InnerText.ToLower().Contains("&#160;fight") == true)
                    {
                        _total = cell.InnerText.ToLower().Replace("&#160;fight", "");
                    }
                    else if (cell.InnerText.ToLower().Contains("&#160;matches") == true)
                    {
                        _total = cell.InnerText.ToLower().Replace("&#160;matches", "");
                    }
                    else if (cell.InnerText.ToLower().Contains("&#160;match") == true)
                    {
                        _total = cell.InnerText.ToLower().Replace("&#160;match", "");
                    }
                    else if (cell.InnerText.ToLower().Contains(" wins") == true)
                    {
                        _wins = cell.InnerText.ToLower().Replace(" wins", "");
                    }
                    else if (cell.InnerText.ToLower().Contains(" win") == true)
                    {
                        _wins = cell.InnerText.ToLower().Replace(" win", "");
                    }
                    else if (cell.InnerText.ToLower().Contains(" losses") == true)
                    {
                        _losses = cell.InnerText.ToLower().Replace(" losses", "");
                    }
                    else if (cell.InnerText.ToLower().Contains(" loss") == true)
                    {
                        _losses = cell.InnerText.ToLower().Replace(" loss", "");
                    }
                }
            }

            
            int total = Int32.Parse(_total);
            int wins = Int32.Parse(_wins);
            int losses = Int32.Parse(_losses);

            int draws = 0;

            if (total - wins - losses != 0)
            {
                draws = total - wins - losses;
            }

            Builder.Append("###Overall Record: " + wins + " - " + losses + " - " + draws + "\n\n");
            Row_Count++;
        }


        private static void GetBoxingDetailedRecordTable(HtmlNode node)
        {

            int cell_count = 1;

            foreach (HtmlNode row in node.SelectNodes("tr"))
            {
                bool shouldBreakout = BreakoutLogic(Row_Count);

                if (shouldBreakout == true)
                {
                    break;
                }

                /* the first row has all the column labels of the wiki table, and we don't want those
                 * we are going to parse through the list, and select some of the data that we want, but not all
                 * thus we just want to throw this row away */
                if (Row_Count != 2)
                {

                    cell_count = 1;
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {

                        /* this conditional is supposed to correspond to the 'result' comlumn of the table
                           sometimes is just an index column though, so throw that away */
                        if (cell_count == 1)
                        {
                            if (cell.InnerText.ToLower() == "win" || cell.InnerText.ToLower() == "loss" ||
                                cell.InnerText.ToLower() == "draw" || cell.InnerText.ToLower() == "nc")
                            {
                                Builder.Append("\n" + cell.InnerText);
                            }
                        }
                        /* if it's the 'result' column, we want a newline char to precede it */
                        else if (cell_count == 2)
                        {
                            if (cell.InnerText.ToLower() == "win" || cell.InnerText.ToLower() == "loss" ||
                                cell.InnerText.ToLower() == "draw" || cell.InnerText.ToLower() == "nc")
                            {
                                Builder.Append("\n" + cell.InnerText);
                            }
                            else if (cell.InnerText.ToLower() == "n/a")
                            {
                                Row_Count--;
                                break;
                            }
                            else
                            {
                                Builder.Append(" | " + cell.InnerText);
                            }
                        }
                        else if (cell_count == 3 || cell_count == 4 || cell_count == 5 || cell_count == 6)
                        {
                            Builder.Append(" | " + cell.InnerText);
                        }
                        else if (cell_count == 7)
                        {
                            /* we want the date of the fight, so if we can parse this string as a date, then add it to the table */
                            try
                            {
                                DateTime t = DateTime.Parse(cell.InnerText);
                                Builder.Append(" | " + cell.InnerText);
                            }
                            catch (FormatException)
                            {

                            }
                        }

                        cell_count++;
                    }
                }

                Row_Count++;
            }
        }


        private static void GetMmaDetailedRecordTable(HtmlNode node)
        {

            int cell_count = 1;

            foreach (HtmlNode row in node.SelectNodes("tr"))
            {
                bool shouldBreakout = BreakoutLogic(Row_Count);

                if (shouldBreakout == true)
                {
                    break;
                }

                /* the first row has all the column labels of the wiki table, and we don't want those
                 * we are going to parse through the list, and select some of the data that we want, but not all
                 * thus we just want to throw this row away */
                if (Row_Count != 2)
                {

                    cell_count = 1;
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {

                        /* this conditional is supposed to correspond to the 'result' comlumn of the table
                           sometimes is just an index column though, so throw that away */
                        if (cell_count == 1)
                        {
                            if (cell.InnerText.ToLower() == "win" || cell.InnerText.ToLower() == "loss" ||
                                cell.InnerText.ToLower() == "draw" || cell.InnerText.ToLower() == "nc")
                            {
                                Builder.Append("\n" + cell.InnerText);
                            }
                        }
                        /* if it's the 'result' column, we want a newline char to precede it */
                        else if (cell_count == 2)
                        {
                            if (cell.InnerText.ToLower() == "win" || cell.InnerText.ToLower() == "loss" ||
                                cell.InnerText.ToLower() == "draw" || cell.InnerText.ToLower() == "nc")
                            {
                                Builder.Append("\n" + cell.InnerText);
                            }
                            else if (cell.InnerText.ToLower() == "n/a")
                            {
                                Row_Count--;
                                break;
                            }
                            else
                            {
                                Builder.Append(" | " + cell.InnerText);
                            }
                        }
                        else if (cell_count == 3 || cell_count == 4 || cell_count == 7 || cell_count == 8)
                        {
                            Builder.Append(" | " + cell.InnerText);
                        }
                        else if (cell_count == 6)
                        {

                            HtmlNodeCollection collection = cell.SelectNodes("span");

                            foreach (HtmlNode span in collection)
                            {
                                /* we want the date of the fight, so if we can parse this string as a date, then add it to the table */
                                try
                                {
                                    DateTime t = DateTime.Parse(span.InnerText);
                                    Builder.Append(" | " + span.InnerText);
                                }
                                catch (FormatException)
                                {

                                }
                            }
                        }

                        cell_count++;
                    }
                }

                Row_Count++;
            }
        }

        private static void GetDetailedRecordTableWithTopRecord(HtmlNode node)
        {

            int cell_count = 1;

            foreach (HtmlNode row in node.SelectNodes("tr"))
            {
                bool shouldBreakout = BreakoutLogic(Row_Count);

                if (shouldBreakout == true)
                {
                    break;
                }

                /* the second row has all the column labels of the wiki table, and we don't want those
                 * we are going to parse through the list, and select some of the data that we want, but not all
                 * thus we just want to throw this row away */
                if (Row_Count == 1)
                {
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {
                        if (cell.InnerText.ToLower().Contains("win") || cell.InnerText.ToLower().Contains("loss") || cell.InnerText.ToLower().Contains("draw"))
                        {
                            Builder.Append(cell.InnerText + "\n\nRes. | Record | Opponent | Type | Rd., Time | Date\n-----|--------|------------|------|-----------|------");
                        }
                    }
                }
                else if (Row_Count != 2)
                {

                    cell_count = 1;
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {

                        /* this conditional is supposed to correspond to the 'result' comlumn of the table
                           sometimes is just an index column though, so throw that away */
                        if (cell_count == 1)
                        {
                            if (cell.InnerText.ToLower() == "win" || cell.InnerText.ToLower() == "loss" ||
                                cell.InnerText.ToLower() == "draw" || cell.InnerText.ToLower() == "nc")
                            {
                                Builder.Append("\n" + cell.InnerText);
                            }
                        }
                        /* if it's the 'result' column, we want a newline char to precede it */
                        else if (cell_count == 2)
                        {
                            if (cell.InnerText.ToLower() == "win" || cell.InnerText.ToLower() == "loss" ||
                                cell.InnerText.ToLower() == "draw" || cell.InnerText.ToLower() == "nc")
                            {
                                Builder.Append("\n" + cell.InnerText);
                            }
                            else if (cell.InnerText.ToLower() == "n/a")
                            {
                                Row_Count--;
                                break;
                            }
                            else
                            {
                                Builder.Append(" | " + cell.InnerText);
                            }
                        }
                        else if (cell_count == 3 || cell_count == 4 || cell_count == 5 || cell_count == 6)
                        {
                            Builder.Append(" | " + cell.InnerText);
                        }
                        else if (cell_count == 7)
                        {
                            /* we want the date of the fight, so if we can parse this string as a date, then add it to the table */
                            try
                            {
                                DateTime.Parse(cell.InnerText);
                                Builder.Append(" | " + cell.InnerText);
                            }
                            catch (FormatException)
                            {

                            }
                        }

                        cell_count++;
                    }
                }

                Row_Count++;
            }
        }

        private static bool BreakoutLogic(int rowCount)
        {
            bool shouldBreakout = false;

            /* some fighter's have so many fights that the comment would exceed the limit that reddit imposes */
            if (Request_Size <= 0)
            {
                if (Request_Size == -999)
                {
                    if (rowCount > 150)
                    {
                        shouldBreakout = true;
                    }
                }
                else
                {
                    Console.WriteLine("error: user specified negative number");
                    shouldBreakout = true;
                }
            }
            else
            {
                if (rowCount > 150 || rowCount > Request_Size + 2)
                {
                    shouldBreakout = true;
                }
            }

            return shouldBreakout;
        }

    }
}
