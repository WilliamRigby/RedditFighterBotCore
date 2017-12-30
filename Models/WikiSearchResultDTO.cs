using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditFighterBot.Models
{
    public class WikiSearchResultDTO
    {
        public string title { get; set; }

        public int size { get; set; }

        public int LevenshteinDistance { get; set; }
    }
}
