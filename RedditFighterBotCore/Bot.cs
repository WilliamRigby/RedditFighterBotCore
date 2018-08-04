using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using RedditSharp;
using RedditSharp.Things;
using RedditFighterBot.Execution;
using RedditFighterBot.Models;
using Nito.AsyncEx;
using System.Configuration;
using RedditFighterBotCore.Execution;
using RedditFighterBotCore.Models;

namespace RedditFighterBot
{
    public class Bot
    {
        private static Reddit reddit;     
        private static string clientid;
        private static string secret;
        private static string username;
        private static string password;
        private static string redirect;
        private static int delay = 0;
        private static readonly string debug;
        
        static Bot()
        {
            debug = ConfigurationManager.AppSettings["isDebugMode"];
        }        

        public static void Main()
        {
            AsyncContext.Run(() => MainAsync());
        }

        private static async Task MainAsync()
        {
            try
            {
                await ReadPasswords();

                BotWebAgent bot = new BotWebAgent(username, password, clientid, secret, redirect)
                {
                    RateLimiter = new RateLimitManager() 
                    { 
                        Mode = RateLimitMode.SmallBurst 
                    }
                };

                reddit = new Reddit(bot, false);

                await reddit.InitOrUpdateUserAsync();

                Logger.LogMessage("Logged in...");
            }
            catch (Exception e)
            {
                Logger.LogMessage(e.StackTrace);
                return;
            }
            
            await Loop();
        }

        private static async Task ReadPasswords()
        {
            string file = await File.ReadAllTextAsync(@"passwords.xml");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(file);

            XmlNode node = doc.SelectSingleNode($"/config/accounts/account[@debug=\"{debug}\"]");

            username = node.SelectSingleNode("username").InnerText;
            clientid = node.SelectSingleNode("clientid").InnerText;
            secret = node.SelectSingleNode("secret").InnerText;                            
            password = node.SelectSingleNode("password").InnerText;
            redirect = node.SelectSingleNode("redirect").InnerText;                
        }       
        
        private static async Task Loop()
        {
            while(true)
            {
                try
                {
                    List<ReplyQueueItem> items = await GetBotMessages();

                    if(items.Count != 0)
                    {
                        ReplyQueuer.EnqueueItems(items);
                    }                    

                    delay = await ReplyQueuer.AttemptReply();                     

                    await Task.Delay((delay == 0) ? 10000: delay + 5000);
                }
                catch(Exception e)
                {
                    Logger.LogMessage($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                }
            }
        }

        private static async Task<List<ReplyQueueItem>> GetBotMessages()
        {        
            Listing<Thing> list = reddit.User.GetUnreadMessages();

            List<ReplyQueueItem> items = new List<ReplyQueueItem>();

            using(IAsyncEnumerator<Thing> enumerator = list.GetEnumerator())
            {
                while (await enumerator.MoveNext() == true)
                {
                    Thing thing = enumerator.Current;
                        
                    if (thing.Kind == "t4")
                    {
                        PrivateMessage pm = ((PrivateMessage)thing);

                        await HandlePrivateMessage(pm);
                    }
                    else if (thing.Kind == "t1")
                    {
                        Comment comment = ((Comment)thing);

                        ReplyQueueItem item = await HandleComment(comment);

                        if(item != null)
                        {
                            items.Add(item);
                        }                        
                    }
                }
            }

            return items;
        }

        private static async Task HandlePrivateMessage(PrivateMessage pm)
        {
            await pm.SetAsReadAsync();
            
            Logger.LogMessage($"Private Message Received{Environment.NewLine}{Environment.NewLine}Subject: {pm.Subject}{Environment.NewLine}{Environment.NewLine}Body: {pm.Body}");
        }

        private static async Task<ReplyQueueItem> HandleComment(Comment comment)
        {
            await comment.SetAsReadAsync();            

            string original = StringUtilities.GetRequestStringFromComment(comment.Body);

            //if we cannot find a line of the comment that contains the bot's name, then continue
            //this implies that this was probably just a regular comment reply, and not a request
            if (original == null || original == "")
            {
                return null;
            }

            string requestLine = StringUtilities.RemoveBotName(original.ToLower(), username.ToLower());
            requestLine = StringUtilities.RemoveRESUpvoteNumbers(requestLine);
            int requestSize = StringUtilities.GetUserRequestSize(requestLine);
            requestLine = StringUtilities.RemoveNumbers(requestLine);
            List<string> fighters = StringUtilities.GetFighters(requestLine);
            
            var wikiCheckedFighters = await GetWikiCheckedFighters(fighters);

            //if we threw an exception for every wiki search, and thus we have no wiki pages, then just break out of this garbage request
            if (wikiCheckedFighters.Count == 0)
            {
                throw new Exception($"Unable to find any fighter for request string: {requestLine}");
            }

            IRequest request = GetRequest(wikiCheckedFighters, requestSize);

            string result = await CreateTables(request);

            ReplyQueueItem item = null;
            if (result != string.Empty)
            {
                item = new ReplyQueueItem
                {
                    Comment = comment,
                    RequestLine = original,
                    Reply = $"{result}\n\n^(I am a bot. This post was requested by {comment.AuthorName})",
                    Attempts = 0
                };                
            }
            else
            {
                throw new Exception("Reply attempt failed due to being unable to find the record section index in the ToC");
            }

            return item;
        }

        private static async Task<List<string>> GetWikiCheckedFighters(List<string> fighters)
        {
            List<string> checkedFighters = new List<string>();
            WikiAccessor wikiAccessor = new WikiAccessor();

            foreach (string fighter in fighters)
            {
                WikiSearchResultDTO test = await wikiAccessor.SearchWikiForFightersPage(fighter);

                try
                {
                    checkedFighters.Add(test.title);
                }
                catch (NullReferenceException ex)
                {
                    Logger.LogMessage($"Null returned from SearchWiki(fighter) for fighter: {fighter}{Environment.NewLine}{ex.StackTrace}");
                    continue;
                }
            }

            return checkedFighters;
        }

        private static IRequest GetRequest(List<string> fighters, int requestSize)
        {
            if (requestSize != -1)
            {
                return new SpecifiedRequest(fighters, requestSize);
            }
            else
            {
                return new DefaultRequest(fighters);
            }
        }

        private static async Task<string> CreateTables(IRequest request)
        {
            WikiAccessor wikiAccessor = new WikiAccessor();
            string result = string.Empty;

            foreach (string fighter in request.FighterNames)
            {
                int index = await wikiAccessor.GetIndexOfRecordTableInTableOfContents(fighter);

                if (index != -1)
                {
                    result = await wikiAccessor.GetEntireTable(request.RequestSize, fighter, index);
                }
            }

            return result;
        }              
    }
}