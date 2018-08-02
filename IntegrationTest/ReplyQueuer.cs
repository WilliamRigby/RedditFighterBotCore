using IntegrationTest.Models;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntegrationTest
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

        public static async Task<int> AttemptAllReply(Post post)
        {
            if (queue.Count == 0)
            {
                return -1;
            }

            return await Dequeue(queue.Peek(), post);
        }

        private static async Task<int> Dequeue(ReplyQueueItem item, Post post)
        {
            int delay = 0;

            try
            {
                Comment comment = await post.CommentAsync(item.Reply);
                queue.Dequeue();
                await Task.Delay(10000);
            }
            catch (RateLimitException rate)
            {
                item.Attempts++;

                if (item.Attempts < 4)
                {
                    delay = Convert.ToInt32(rate.TimeToReset.TotalMilliseconds);
                }
                else
                {
                    queue.Dequeue();
                }
            }
            catch (Exception)
            {
                queue.Dequeue();
            }

            return delay;
        }
    }
}
