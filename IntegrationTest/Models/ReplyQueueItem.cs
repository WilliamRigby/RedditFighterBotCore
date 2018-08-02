using RedditSharp.Things;

namespace IntegrationTest.Models
{
    public class ReplyQueueItem
    {
        public string Reply { get; set; }
        public int Attempts { get; set; }
    }
}
