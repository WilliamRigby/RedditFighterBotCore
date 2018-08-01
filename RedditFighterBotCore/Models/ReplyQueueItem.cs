using RedditSharp.Things;

namespace RedditFighterBotCore.Models
{
    public class ReplyQueueItem
    {
        public Comment Comment { get; set; }
        public string RequestLine { get; set; }
        public string Reply { get; set; }
        public int Attempts { get; set; }
    }
}
