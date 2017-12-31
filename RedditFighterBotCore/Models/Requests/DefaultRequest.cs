using RedditSharp.Things;
using System.Collections.Generic;

namespace RedditFighterBot
{
    public class DefaultRequest : IRequest
    {
        public DefaultRequest(List<string> fighter)
        {
            IsPartialTable = false;
            RequestSize = 5;
            FighterNames = fighter;
        }

        public int RequestSize { get; set; }

        public bool IsPartialTable { get; set; }

        public List<string> FighterNames { get; set; }
    }
}
