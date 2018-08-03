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

        public static void EnqueueItems(List<ReplyQueueItem> items)
        {
            foreach(var item in items)
            {
                queue.Enqueue(item);
            }            
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
                Logger.LogMessage($"Reply sent for request: {item.RequestLine}");
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
                    queue.Dequeue();
                    Logger.LogMessage($"Threw away request: {item.RequestLine}{Environment.NewLine}{rate.StackTrace}");
                }
            }
            catch(Exception e)
            {
                queue.Dequeue();
                Logger.LogMessage($"Threw away request: {item.RequestLine}{Environment.NewLine}{e.StackTrace}");
            }

            return delay;
        }
    }
}
