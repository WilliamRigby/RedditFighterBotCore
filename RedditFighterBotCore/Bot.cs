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
using log4net;
using System.Configuration;

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
        private static readonly string debug;
        private static readonly ILog logger;
        
        static Bot()
        {
            debug = ConfigurationManager.AppSettings["isDebugMode"];
            logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }        

        public static int Main(string[] args)
        {
            return AsyncContext.Run(() => MainAsync(args));
        }

        private static async Task<int> MainAsync(string[] args)
        {
            var logRepo = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepo, new FileInfo("app.config"));

            LogMessage("Application Start");

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

                LogMessage("Logged in...");
                LogMessage("About to enter Timer controlled infinite loop");
            }
            catch (Exception e)
            {
                LogMessage(e.Message);
                return 1;
            }
            
            return await Loop();
        }

        private static async Task ReadPasswords()
        {
            string file = await File.ReadAllTextAsync(@"passwords.xml");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(file);

            XmlNode node = doc.SelectSingleNode($"/config/accounts/account[@debug={debug}]");

            username = node.SelectSingleNode("username").InnerText;
            clientid = node.SelectSingleNode("clientid").InnerText;
            secret = node.SelectSingleNode("secret").InnerText;                            
            password = node.SelectSingleNode("password").InnerText;
            redirect = node.SelectSingleNode("redirect").InnerText;                
        }       
        
        private static async Task<int> Loop()
        {
            while(true)
            {            
                try
                {
                    Listing<Thing> list = reddit.User.GetUnreadMessages();

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

                                await HandleComment(comment);
                            }
                        }
                    }               
                }
                catch (Exception e)
                {
                    LogMessage(e.Message);
                }

                await Task.Delay(10000);
            }
        }

        private static async Task HandlePrivateMessage(PrivateMessage pm)
        {
            await pm.SetAsReadAsync();

            LogMessage("Got a PM");
            LogMessage($"Private Message Received{Environment.NewLine}{Environment.NewLine}Subject: {pm.Subject}{Environment.NewLine}{Environment.NewLine}Body: {pm.Body}");
        }

        private static async Task HandleComment(Comment comment)
        {
            await comment.SetAsReadAsync();

            LogMessage($"Request received: {comment.Body}");

            string requestLine = StringUtilities.GetRequestStringFromComment(comment.Body);

            //if we cannot find a line of the comment that contains the bot's name, then continue
            //this implies that this was probably just a regular comment reply, and not a request
            if (requestLine == null || requestLine == "")
            {
                throw new Exception($"request string invalid: {requestLine}");
            }

            requestLine = StringUtilities.RemoveBotName(requestLine.ToLower(), username.ToLower());
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

            if (result != string.Empty)
            {
                SendReply(comment, $"{result}\n\n^(I am a bot. This post was requested by {comment.AuthorName})");
            }
            else
            {
                throw new Exception("Reply attempt failed due to being unable to find the record section index in the ToC");
            }
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
                    LogMessage($"Null returned from SearchWiki(fighter) for fighter: {fighter}");
                    LogMessage(ex.Message);
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

        private static async void SendReply(Comment comment, string reply, int attemptCounter = 1)
        {
            try
            {
                Comment task = await comment.ReplyAsync(reply);
                LogMessage("Reply successful!");                                
            }
            catch (RateLimitException rate)
            {
                if(attemptCounter < 4)
                {
                    LogMessage(rate.Message);
                    await Task.Delay(Convert.ToInt32(rate.TimeToReset.TotalMilliseconds));
                    SendReply(comment, reply, ++attemptCounter);
                }
                else
                {
                    throw;
                }                
            }
        }        

        private static void LogMessage(string message)
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            logger.Debug($"[{time}]  {message}");
            Console.WriteLine(message);
        }
    }
}