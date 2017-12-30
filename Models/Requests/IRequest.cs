using RedditSharp.Things;
using System.Collections.Generic;

namespace RedditFighterBot
{
    public interface IRequest
    {
        int RequestSize { get; set; }

        bool IsPartialTable { get; set; }

        List<string> FighterNames { get; set; }

    }
}
