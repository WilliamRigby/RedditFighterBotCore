using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RedditFighterBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RedditFighterBot.Execution
{
    public class WikiAccessor
    {
        private const int MAXROWS = 150;
        private StringBuilder Builder { get; set; }
        private int RowCount { get; set; }
        private string FighterType { get; set; }                        

        public WikiAccessor()
        {            
            Builder = new StringBuilder();
        }

        public async Task<WikiSearchResultDTO> SearchWikiForFightersPage(string fighter)
        {
            JObject json = await CreateWebRequest("https://en.wikipedia.org/w/api.php?action=query&list=search&format=json&srprop=size&srsearch=" + fighter + " fighter");
            
            JEnumerable<JToken> results = json["query"]["search"].Children();
            
            List<WikiSearchResultDTO> searchResults = new List<WikiSearchResultDTO>();
            foreach (JToken result in results)
            {
                WikiSearchResultDTO searchResult = result.ToObject<WikiSearchResultDTO>();
                searchResults.Add(searchResult);
            }           
            
            return PickSearchResult(fighter, searchResults);
        }

        private WikiSearchResultDTO PickSearchResult(string fighter, List<WikiSearchResultDTO> searchResults)
        {
            if (searchResults.Count == 0)
            {
                return null;
            }
            
            foreach (WikiSearchResultDTO result in searchResults)
            {
                if (result.title.ToLower().Contains("(fighter)"))
                {
                    result.LevenshteinDistance = StringUtilities.LevenshteinDistance(($"{fighter.ToLower()} (fighter)").ToCharArray(), result.title.ToLower().ToCharArray());
                }
                else
                {
                    result.LevenshteinDistance = StringUtilities.LevenshteinDistance(fighter.ToLower().ToCharArray(), result.title.ToLower().ToCharArray());
                }
            }

            searchResults.Sort((x, y) => x.LevenshteinDistance.CompareTo(y.LevenshteinDistance));

            int shortestDistance = 0;

            List<WikiSearchResultDTO> ties = new List<WikiSearchResultDTO>();

            for (int i = 0; i < searchResults.Count; i++)
            {
                if (i == 0) { shortestDistance = searchResults[i].LevenshteinDistance; }

                if (searchResults[i].LevenshteinDistance == shortestDistance)
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

        public async Task<int> GetIndexOfRecordTableInTableOfContents(string fighter)
        {
            JToken json = await CreateWebRequest("https://en.wikipedia.org/w/api.php?format=json&action=parse&prop=sections&page=" + fighter);
            
            JEnumerable<JToken> sections = json["parse"]["sections"].Children();
            
            List<WikiTableOfContentDTO> TOC = new List<WikiTableOfContentDTO>();
            foreach (JToken result in sections)
            {
                WikiTableOfContentDTO index = result.ToObject<WikiTableOfContentDTO>();
                TOC.Add(index);
            }

            return ParseTableOfContents(TOC);
        }       
        
        /* this method goes out to the wikipedia api and tries to get the fighter's table
         * on wikipedia the fighter's table is html, but has a consistent formatting  
         * we take advantage of that consistent pattern to be able to parse the table */
        public async Task<string> GetEntireTable(int requestSize, string fighter, int index)
        {
            RowCount = 1;

            HtmlNodeCollection tables = await GetHTMLTables(fighter, index);
            string url = "https://en.wikipedia.org/wiki/" + fighter.Replace(' ', '_').Replace("(", "%28").Replace(")", "%29");

            if (tables.Count > 1)
            {   
                if (FighterType == "mma")
                {
                    url += "#Mixed_martial_arts_record";
                    Builder.Append("###[" + fighter + "](" + url + ")\n\n");
                    GetOverallRecordTable(tables[0]);
                    Builder.Append(" Res. | Record | Opponent | Type | Date | Rd. | Time\n-----|-----|-----|-----|-----|-----|-----|-----");
                    GetMmaDetailedRecordTable(requestSize, tables[1]);
                }
                else if (FighterType == "boxing")
                {
                    url += "#Professional_boxing_record";
                    Builder.Append("###[" + fighter + "](" + url + ")\n\n");
                    GetOverallRecordTable(tables[0]);
                    Builder.Append(" Res. | Record | Opponent | Type | Rd., Time | Date\n-----|-----|-----|-----|-----|-----");
                    GetBoxingDetailedRecordTable(requestSize, tables[1]);
                }
            }
            else if (tables.Count == 1)
            {
                url += "#Professional_record";
                Builder.Append("###[" + fighter + "](" + url + ")\n\n");
                GetDetailedRecordTableWithTopRecord(requestSize, tables[0]);
            }

            Builder.Append("\n\n");
            
            return Builder.ToString();
        }

        private int ParseTableOfContents(List<WikiTableOfContentDTO> TOC)
        {
            foreach (WikiTableOfContentDTO section in TOC)
            {
                if (section.anchor.ToLower().Contains("martial_arts_record") == true)
                {
                    FighterType = "mma";
                    return section.index;
                }
            }

            foreach (WikiTableOfContentDTO section in TOC)
            {
                if (section.anchor.ToLower() == "professional_boxing_record" || section.anchor.ToString().ToLower() == "professional_record")
                {
                    FighterType = "boxing";
                    return section.index;
                }
            }

            //if we don't find the index, then return magic number -1 as error code
            return -1;
        }

        private async Task<HtmlNodeCollection> GetHTMLTables(string fighter, int index)
        {
            /* this url will direct to the html for the given fighter */
            JToken json = await CreateWebRequest("https://en.wikipedia.org/w/api.php?format=json&action=parse&prop=text&page=" + fighter + "&section=" + index);
            
            /* get the html which describes the table */
            string s = json.SelectToken("parse.text").ToString();

            HtmlDocument html_table = new HtmlDocument();
            html_table.LoadHtml(s);

            return html_table.DocumentNode.SelectNodes("//table");
        }

        private void GetOverallRecordTable(HtmlNode node)
        {
            string _total = "";
            string _wins = "";
            string _losses = "";

            foreach (HtmlNode row in node.SelectSingleNode("tbody").SelectNodes("tr"))
            {
                foreach (HtmlNode cell in row.SelectNodes("th|td"))
                {
                    if (GetNodeInnerText(cell).ToLower().Contains("&#160;fights") == true)
                    {
                        _total = GetNodeInnerText(cell).ToLower().Replace("&#160;fights", "");
                    }
                    else if (GetNodeInnerText(cell).ToLower().Contains("&#160;fight") == true)
                    {
                        _total = GetNodeInnerText(cell).ToLower().Replace("&#160;fight", "");
                    }
                    else if (GetNodeInnerText(cell).ToLower().Contains("&#160;matches") == true)
                    {
                        _total = GetNodeInnerText(cell).ToLower().Replace("&#160;matches", "");
                    }
                    else if (GetNodeInnerText(cell).ToLower().Contains("&#160;match") == true)
                    {
                        _total = GetNodeInnerText(cell).ToLower().Replace("&#160;match", "");
                    }
                    else if (GetNodeInnerText(cell).ToLower().Contains(" wins") == true)
                    {
                        _wins = GetNodeInnerText(cell).ToLower().Replace(" wins", "");
                    }
                    else if (GetNodeInnerText(cell).ToLower().Contains(" win") == true)
                    {
                        _wins = GetNodeInnerText(cell).ToLower().Replace(" win", "");
                    }
                    else if (GetNodeInnerText(cell).ToLower().Contains(" losses") == true)
                    {
                        _losses = GetNodeInnerText(cell).ToLower().Replace(" losses", "");
                    }
                    else if (GetNodeInnerText(cell).ToLower().Contains(" loss") == true)
                    {
                        _losses = GetNodeInnerText(cell).ToLower().Replace(" loss", "");
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
            RowCount++;
        }

        private void GetBoxingDetailedRecordTable(int requestSize, HtmlNode node)
        {
            int cellCount = 1;

            foreach (HtmlNode row in node.SelectSingleNode("tbody").SelectNodes("tr"))
            {
                if (BreakoutLogic(requestSize, RowCount) == true)
                {
                    break;
                }

                /* the first row has all the column labels of the wiki table, and we don't want those
                 * we are going to parse through the list, and select some of the data that we want, but not all
                 * thus we just want to throw this row away */
                if (RowCount != 2)
                {
                    cellCount = 1;
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {
                        /* this conditional is supposed to correspond to the 'result' comlumn of the table
                           sometimes is just an index column though, so throw that away */
                        if (cellCount == 1)
                        {
                            if (GetNodeInnerText(cell).ToLower() == "win" || GetNodeInnerText(cell).ToLower() == "loss" ||
                                GetNodeInnerText(cell).ToLower() == "draw" || GetNodeInnerText(cell).ToLower() == "nc")
                            {
                                Builder.Append("\n" + GetNodeInnerText(cell));
                            }
                        }
                        /* if it's the 'result' column, we want a newline char to precede it */
                        else if (cellCount == 2)
                        {
                            if (GetNodeInnerText(cell).ToLower() == "win" || GetNodeInnerText(cell).ToLower() == "loss" ||
                                GetNodeInnerText(cell).ToLower() == "draw" || GetNodeInnerText(cell).ToLower() == "nc")
                            {
                                Builder.Append("\n" + GetNodeInnerText(cell));
                            }
                            else if (GetNodeInnerText(cell).ToLower() == "n/a")
                            {
                                RowCount--;
                                break;
                            }
                            else
                            {
                                Builder.Append(" | " + GetNodeInnerText(cell));
                            }
                        }
                        else if (cellCount == 3 || cellCount == 4 || cellCount == 5 || cellCount == 6)
                        {
                            Builder.Append(" | " + GetNodeInnerText(cell));
                        }
                        else if (cellCount == 7)
                        {
                            /* we want the date of the fight, so if we can parse this string as a date, then add it to the table */
                            try
                            {
                                DateTime t = DateTime.Parse(GetNodeInnerText(cell));
                                Builder.Append(" | " + GetNodeInnerText(cell));
                            }
                            catch (FormatException)
                            {

                            }
                        }

                        cellCount++;
                    }
                }

                RowCount++;
            }
        }

        private void GetMmaDetailedRecordTable(int requestSize, HtmlNode node)
        {
            int cellCount = 1;

            foreach (HtmlNode row in node.SelectSingleNode("tbody").SelectNodes("tr"))
            {
                if (BreakoutLogic(requestSize, RowCount) == true)
                {
                    break;
                }

                /* the first row has all the column labels of the wiki table, and we don't want those
                 * we are going to parse through the list, and select some of the data that we want, but not all
                 * thus we just want to throw this row away */
                if (RowCount != 2)
                {
                    cellCount = 1;
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {

                        /* this conditional is supposed to correspond to the 'result' comlumn of the table
                           sometimes is just an index column though, so throw that away */
                        if (cellCount == 1)
                        {
                            if (GetNodeInnerText(cell).ToLower() == "win" || GetNodeInnerText(cell).ToLower() == "loss" ||
                                GetNodeInnerText(cell).ToLower() == "draw" || GetNodeInnerText(cell).ToLower() == "nc")
                            {
                                Builder.Append("\n" + GetNodeInnerText(cell));
                            }
                        }
                        /* if it's the 'result' column, we want a newline char to precede it */
                        else if (cellCount == 2)
                        {
                            if (GetNodeInnerText(cell).ToLower() == "win" || GetNodeInnerText(cell).ToLower() == "loss" ||
                                GetNodeInnerText(cell).ToLower() == "draw" || GetNodeInnerText(cell).ToLower() == "nc")
                            {
                                Builder.Append("\n" + GetNodeInnerText(cell));
                            }
                            else if (GetNodeInnerText(cell).ToLower() == "n/a")
                            {
                                RowCount--;
                                break;
                            }
                            else
                            {
                                Builder.Append(" | " + GetNodeInnerText(cell));
                            }
                        }
                        else if (cellCount == 3 || cellCount == 4 || cellCount == 7 || cellCount == 8)
                        {
                            Builder.Append(" | " + GetNodeInnerText(cell));
                        }
                        else if (cellCount == 6)
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

                        cellCount++;
                    }
                }

                RowCount++;
            }
        }

        private void GetDetailedRecordTableWithTopRecord(int requestSize, HtmlNode node)
        {
            int cellCount = 1;

            foreach (HtmlNode row in node.SelectSingleNode("tbody").SelectNodes("tr"))
            {
                if (BreakoutLogic(requestSize, RowCount) == true)
                {
                    break;
                }

                /* the second row has all the column labels of the wiki table, and we don't want those
                 * we are going to parse through the list, and select some of the data that we want, but not all
                 * thus we just want to throw this row away */
                if (RowCount == 1)
                {
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {
                        if (GetNodeInnerText(cell).ToLower().Contains("win") || GetNodeInnerText(cell).ToLower().Contains("loss") || GetNodeInnerText(cell).ToLower().Contains("draw"))
                        {
                            Builder.Append(GetNodeInnerText(cell) + "\n\nRes. | Record | Opponent | Type | Rd., Time | Date\n-----|--------|------------|------|-----------|------");
                        }
                    }
                }
                else if (RowCount != 2)
                {

                    cellCount = 1;
                    /* for each of the row's cells (i.e. columns) */
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {

                        /* this conditional is supposed to correspond to the 'result' comlumn of the table
                           sometimes is just an index column though, so throw that away */
                        if (cellCount == 1)
                        {
                            if (GetNodeInnerText(cell).ToLower() == "win" || GetNodeInnerText(cell).ToLower() == "loss" ||
                                GetNodeInnerText(cell).ToLower() == "draw" || GetNodeInnerText(cell).ToLower() == "nc")
                            {
                                Builder.Append("\n" + GetNodeInnerText(cell));
                            }
                        }
                        /* if it's the 'result' column, we want a newline char to precede it */
                        else if (cellCount == 2)
                        {
                            if (GetNodeInnerText(cell).ToLower() == "win" || GetNodeInnerText(cell).ToLower() == "loss" ||
                                GetNodeInnerText(cell).ToLower() == "draw" || GetNodeInnerText(cell).ToLower() == "nc")
                            {
                                Builder.Append("\n" + GetNodeInnerText(cell));
                            }
                            else if (GetNodeInnerText(cell).ToLower() == "n/a")
                            {
                                RowCount--;
                                break;
                            }
                            else
                            {
                                Builder.Append(" | " + GetNodeInnerText(cell));
                            }
                        }
                        else if (cellCount == 3 || cellCount == 4 || cellCount == 5 || cellCount == 6)
                        {
                            Builder.Append(" | " + GetNodeInnerText(cell));
                        }
                        else if (cellCount == 7)
                        {
                            /* we want the date of the fight, so if we can parse this string as a date, then add it to the table */
                            try
                            {
                                DateTime.Parse(GetNodeInnerText(cell));
                                Builder.Append(" | " + GetNodeInnerText(cell));
                            }
                            catch (FormatException)
                            {

                            }
                        }

                        cellCount++;
                    }
                }

                RowCount++;
            }
        }

        private async Task<JObject> CreateWebRequest(string url)
        {
            JObject json = new JObject();

            var request = WebRequest.CreateHttp(url);
            request.Method = "GET";

            using (var response = await request.GetResponseAsync())
            {
                var stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);

                string text = await reader.ReadToEndAsync();

                json = JObject.Parse(text);

                stream.Close();
                response.Close();
            }

            return json;
        }

        private bool BreakoutLogic(int requestSize, int rowCount)
        {
            /* some fighter's have so many fights that the comment would exceed the limit that reddit imposes */            
            return (rowCount > MAXROWS || rowCount > requestSize + 2) ? true : false;            
        }

        private string GetNodeInnerText(HtmlNode cell)
        {
            return cell.InnerText.Replace("\\n", "");
        }

    }
}
