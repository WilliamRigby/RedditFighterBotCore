using log4net;
using RedditFighterBot.Models;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace RedditFighterBot
{

    public class Bot
    {
        private static Reddit reddit = null;

        private static TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        private static Timer timer = null;
        private static string clientid = null;
        private static string secret = null;
        private static string username = null;
        private static string password = null;
        private static string redirect = null;
        private static readonly bool debug = true;

        private static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            var logRepo = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepo, new FileInfo("app.config"));

            LogMessage("Application Start");

            try
            {
                ReadPasswords();

                BotWebAgent bot = new BotWebAgent(username, password, clientid, secret, redirect);
                bot.RateLimiter = new RateLimitManager() { Mode = RateLimitMode.SmallBurst };

                reddit = new Reddit(bot, false);
                reddit.InitOrUpdateUserAsync().Wait();

                LogMessage("Logged in...");
                LogMessage("About to enter Timer controlled infinite loop");
            }
            catch (Exception e)
            {
                LogMessage(e.Message);
                Environment.Exit(1);
            }

            timer = new Timer(new TimerCallback(InfiniteLoopCallBack), null, 0, 0);
            System.Threading.Thread.Sleep(Timeout.Infinite);
        }

        private static void ReadPasswords()
        {
            try
            {
                string file = File.ReadAllText(@"passwords.xml");

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(file);

                XmlNodeList nodeList = doc.SelectNodes("/config/accounts/account");

                foreach (XmlNode node in nodeList)
                {
                    if(debug == false)
                    {
                        if(node.SelectSingleNode("username").InnerText.Contains("Test") == false)
                        {
                            clientid = node.SelectSingleNode("clientid").InnerText;
                            secret = node.SelectSingleNode("secret").InnerText;
                            username = node.SelectSingleNode("username").InnerText;
                            password = node.SelectSingleNode("password").InnerText;
                            redirect = node.SelectSingleNode("redirect").InnerText;
                        }                        
                    }
                    else
                    {
                        if (node.SelectSingleNode("username").InnerText.Contains("Test") == true)
                        {
                            clientid = node.SelectSingleNode("clientid").InnerText;
                            secret = node.SelectSingleNode("secret").InnerText;
                            username = node.SelectSingleNode("username").InnerText;
                            password = node.SelectSingleNode("password").InnerText;
                            redirect = node.SelectSingleNode("redirect").InnerText;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                throw e;
            }
        }        

        private static void InfiniteLoopCallBack(object o)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                Listing<Thing> list = reddit.User.GetUnreadMessages();

                using(IAsyncEnumerator<Thing> enumerator = list.GetEnumerator())
                {
                    while (enumerator.MoveNext() != null && enumerator.MoveNext().Result == true)
                    {
                        Thing thing = enumerator.Current;
                        
                        if (thing.Kind == "t4")
                        {
                            PrivateMessage pm = ((PrivateMessage)thing);

                            HandlePrivateMessage(pm);
                        }
                        else if (thing.Kind == "t1")
                        {
                            Comment comment = ((Comment)thing);

                            HandleComment(comment);
                        }
                    }
                }               
            }
            catch (Exception e)
            {
                LogMessage(e.Message);          
            }

            timer.Change(10000, Timeout.Infinite);
        }

        private static void HandlePrivateMessage(PrivateMessage pm)
        {
            pm.SetAsReadAsync();

            LogMessage("Got a PM");
            LogMessage(pm.Subject + " - " + pm.Body);
        }

        private static void HandleComment(Comment comment)
        {
            comment.SetAsReadAsync();

            LogMessage("Request received: " + comment.Body);

            string request_string = StringUtilities.GetRequestStringFromComment(comment.Body);

            //if we cannot find a line of the comment that contains the bot's name, then continue
            //this implies that this was probably just a regular comment reply, and not a request
            if (request_string == null || request_string == "")
            {
                LogMessage("request string invalid: " + request_string);
                return;
            }

            request_string = StringUtilities.RemoveBotName(request_string.ToLower(), username.ToLower());
            request_string = StringUtilities.RemoveRESUpvoteNumbers(request_string);
            int request_size = StringUtilities.GetUserRequestSize(request_string);
            request_string = StringUtilities.RemoveNumbers(request_string);
            List<string> fighters = StringUtilities.GetFighters(request_string);

            WikiAccessor.Builder = new StringBuilder();

            var wikiCheckedFighters = GetWikiCheckedFighters(fighters);

            //if we threw an exception for every wiki search, and thus we have no wiki pages, then just break out of this garbage request
            if (wikiCheckedFighters.Count == 0)
            {
                return;
            }

            IRequest request = GetRequest(wikiCheckedFighters, request_size);

            WikiAccessor.RequestSize = request.RequestSize;

            CreateTables(request);

            if (WikiAccessor.Builder != null && WikiAccessor.Builder.ToString() != "")
            {
                SendReply(comment, WikiAccessor.Builder.ToString() + "\n\n^(I am a bot. This post was requested by " + comment.AuthorName + ")\n\n[^(Usage / FAQ)](http://redditfighterbotwebapp.azurewebsites.net/)");
            }
            else
            {
                LogMessage("Reply attempt failed due to empty StringBuilder");
            }
        }

        private static List<string> GetWikiCheckedFighters(List<string> fighters)
        {
            var checkedFighters = new List<string>();

            foreach (string fighter in fighters)
            {
                WikiSearchResultDTO test = WikiAccessor.SearchWiki(fighter);

                try
                {
                    checkedFighters.Add(test.title);
                }
                catch (NullReferenceException ex)
                {
                    LogMessage("Null returned from SearchWiki(fighter) for fighter: " + fighter);
                    LogMessage(ex.Message);
                    continue;
                }
            }

            return checkedFighters;
        }

        private static IRequest GetRequest(List<string> fighters, int request_size)
        {
            if (request_size != -1)
            {
                return new SpecifiedRequest(fighters, request_size);
            }
            else
            {
                return new DefaultRequest(fighters);
            }
        }

        private static void CreateTables(IRequest request)
        {
            foreach (string fighter in request.FighterNames)
            {
                int index = WikiAccessor.GetIndex(fighter);

                if (index != -1)
                {
                    WikiAccessor.GetEntireTable(fighter, index);
                }
            }
        }

        private static async void SendReply(Comment comment, string reply)
        {
            try
            {
                Task<Comment> task = comment.ReplyAsync(reply);
                task.Wait();

                if(!task.IsFaulted)
                {
                    LogMessage("Reply successful!");
                }
                else
                {
                    throw task.Exception;
                }                
            }
            catch (RateLimitException rate)
            {
                await HandleRateLimitException(rate, comment, reply);
            }
            catch (AggregateException aggregate)
            {
                RateLimitException rate = (RateLimitException)aggregate.InnerExceptions.SingleOrDefault(e => e.GetType() == typeof(RateLimitException));

                if(rate != null)
                {
                    await HandleRateLimitException(rate, comment, reply);
                }
                else
                {
                    LogMessage(aggregate.GetBaseException().Message);
                }
            }
        }

        private static async Task HandleRateLimitException(RateLimitException rate, Comment comment, string reply)
        {
            LogMessage(rate.Message);

            await Task.Delay(Convert.ToInt32(rate.TimeToReset.TotalMilliseconds));

            SendReply(comment, reply);
        }

        private static void LogMessage(string message)
        {
            logger.Debug(message);
            Console.WriteLine(message);
        }
    }
}