using System.Collections.Generic;

namespace RedditFighterBot
{
    public class BingSpellCheckDTO
    {
        public int offset { get; set; }

        public string token { get; set; }

        public string type { get; set; }

        public List<Suggestion> suggestions { get; set; }
    }

    public class Suggestion
    {
        public string suggestion { get; set; }

        public float score { get; set; }
    }

    class BingError
    {
        public int statusCode { get; set; }

        public string message { get; set; }
    }
}
