using RedditSharp.Things;
using System.Collections.Generic;

namespace RedditFighterBot
{
    public class DefaultRequest : IRequest
    {
        public int RequestSize { get; private set; }

        public bool IsPartialTable { get; private set; }

        public List<string> FighterNames { get; private set; }

        public DefaultRequest(List<string> fighter)
        {
            IsPartialTable = false;
            RequestSize = 5;
            FighterNames = fighter;
        }        
    }
}
