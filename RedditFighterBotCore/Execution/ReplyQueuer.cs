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

        public static async Task DequeueLoop()
        {
            while(true)
            {
                int delay = await Dequeue();
                
                await Task.Delay(delay > 0 ? delay : 10000);
            }            
        }

        private static async Task<int> Dequeue()
        {
            if(queue.Count == 0)
            {
                return 0;
            }
            
            int delay = await AttemptReply(queue.Peek());

            return delay;
        }

        private static async Task<int> AttemptReply(ReplyQueueItem item)
        {
            int delay = 0;

            try
            {
                Comment comment = await item.Comment.ReplyAsync(item.Reply);
                Logger.LogMessage("Reply successful!");
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
