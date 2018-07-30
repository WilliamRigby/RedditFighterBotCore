using RedditSharp.Things;
using System.Collections.Generic;

namespace RedditFighterBot
{
    public interface IRequest
    {
        int RequestSize { get; }

        bool IsPartialTable { get; }

        List<string> FighterNames { get; }
    }
}
