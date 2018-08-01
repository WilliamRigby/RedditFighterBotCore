using RedditFighterBotCore.Models;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedditFighterBotCore.Execution
{
    public static class ReplyQueuer
    {
        private static Queue<ReplyQueueItem> queue;

        static ReplyQueuer()
        {
            queue = new Queue<ReplyQueueItem>();              
        }

        public static void EnqueueItem(ReplyQueueItem item)
        {
            queue.Enqueue(item);
        }

        public static async Task<int> AttemptReply()
        {
            if(queue.Count == 0)
            {
                return 0;
            }
            
            return await Dequeue(queue.Peek());            
        }

        private static async Task<int> Dequeue(ReplyQueueItem item)
        {
            int delay = 0;

            try
            {
                Comment comment = await item.Comment.ReplyAsync(item.Reply);
                Logger.LogMessage("Reply successful!");
                queue.Dequeue();
            }
            catch(RateLimitException rate)
            {
                item.Attempts++;

                if(item.Attempts < 4)
                {
                    delay = Convert.ToInt32(rate.TimeToReset.TotalMilliseconds);
                }
                else
                {
                    queue.Dequeue(); // if it's failed more than 3 times, throw it away
                }
            }

            return delay;
        }
    }
}
