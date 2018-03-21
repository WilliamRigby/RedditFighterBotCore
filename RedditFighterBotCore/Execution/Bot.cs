
using log4net;
using Newtonsoft.Json.Linq;
using RedditFighterBot.Models;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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

            Console.WriteLine("Application Start");
            logger.Debug("Application Start");

            try
            {
                ReadPasswords();

                reddit = new Reddit(new BotWebAgent(username, password, clientid, secret, redirect), false);
                reddit.InitOrUpdateUserAsync().Wait();

                logger.Debug("Logged in...");
                Console.WriteLine("Logged in...");
                logger.Debug("About to enter Timer controlled infinite loop");
                Console.WriteLine("About to enter Timer controlled infinite loop");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                logger.Debug(e);
                System.Environment.Exit(1);
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


        private static void Authenticate()
        {
            try
            {
                HttpWebRequest http = (HttpWebRequest)WebRequest.Create("https://www.reddit.com/api/v1/access_token");

                http.Credentials = new NetworkCredential(clientid, secret);
                http.Method = "POST";
                http.ContentType = "application/x-www-form-urlencoded";

                //write the post data to the request
                Stream stream = http.GetRequestStreamAsync().Result;
                byte[] data = Encoding.ASCII.GetBytes("grant_type=password&username=" + WebUtility.UrlEncode(username) + "&password=" + WebUtility.UrlEncode(password));
                stream.Write(data, 0, data.Length);
                

                //send the request and get the response
                WebResponse response = http.GetResponseAsync().Result;
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                JObject json = JObject.Parse(responseString);

                JToken result = json;

                RedditOauthResponseDTO oauth = result.ToObject<RedditOauthResponseDTO>();

                reddit = new Reddit(oauth.access_token);
                reddit.InitOrUpdateUserAsync().Wait();

                response.Dispose();
                stream.FlushAsync();
            }
            catch (Exception e)
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
                    while (enumerator.MoveNext().Result)
                    {
                        Thing thing = enumerator.Current;

                        //if this Thing is a PM
                        if (thing.Kind == "t4")
                        {
                            PrivateMessage pm = ((PrivateMessage)thing);

                            pm.SetAsReadAsync();

                            logger.Debug("Got a PM");
                            logger.Debug(pm.Subject + " - " + pm.Body);
                        }
                        //if this Thing is a comment
                        else if (thing.Kind == "t1")
                        {
                            Comment comment = ((Comment)thing);

                            //mark this comment as 'read' so we don't process any comment more than once
                            comment.SetAsReadAsync();

                            //log the comment body obvs
                            Console.WriteLine("Reuqest received: " + comment.Body);
                            logger.Debug("Reuqest received: " + comment.Body);

                            //get the line of the comment body which is the actual request
                            string request_string = StringUtilities.GetRequestStringFromComment(comment.Body);

                            
                            //if we cannot find a line of the comment that contains the bot's name, then continue
                            //this implies that this was probably just a regular comment reply, and not a request
                            if (request_string == null || request_string == "")
                            {
                                logger.Debug("request string invalid: " + request_string);
                                continue;
                            }

                            //go ahead and remove the bot name from the string
                            request_string = StringUtilities.RemoveBotName(request_string.ToLower(), username.ToLower());

                            //remove RES vote sum for the bot (so user can copy and paste a previous request, and those nums won't cause an error)
                            request_string = StringUtilities.RemoveRESUpvoteNumbers(request_string);

                            //get the user specified request size
                            int request_size = StringUtilities.GetUserRequestSize(request_string);

                            //remove the user specified size numbers
                            request_string = StringUtilities.RemoveNumbers(request_string);

                            //get the fighter name(s) as an array
                            List<string> fighters = StringUtilities.GetFighters(request_string);

                            //replace the nonenglish chars in the request, which might mess up the bing search
                            //fighters = StringUtilities.ReplaceNonEnglishChars(fighters);

                            //fix the user input using the Bing spell check api
                            //List<string> bing_checked_fighters = BingSpellCheck(fighters);

                            //reinitialize the stringbuilder back to a new object
                            WikiAccessor.Builder = new System.Text.StringBuilder();

                            // now search via the wiki api for pages with similar names
                            // this helps to correct locale errors
                            // i.e. roman gonzalez vs Román González (boxer)
                            List<string> wiki_checked_fighters = new List<string>();
                            foreach (string fighter in fighters)
                            {
                                WikiSearchResultDTO test = WikiAccessor.SearchWiki(fighter);
                                                                
                                try
                                {
                                    wiki_checked_fighters.Add(test.title);
                                }
                                catch (NullReferenceException ex)
                                {
                                    logger.Debug("Null returned from SearchWiki(fighter) for fighter: " + fighter);
                                    logger.Debug(ex.Message);
                                    continue;
                                }
                            }

                            //if we threw an exception for every wiki search, and thus we have no wiki pages, then just break out of this garbage request
                            if (wiki_checked_fighters.Count == 0)
                            {
                                continue;
                            }

                            //create the request object
                            IRequest request = null;
                            if (request_size != -1)
                            {
                                request = new SpecifiedRequest(wiki_checked_fighters, request_size);
                            }
                            else
                            {
                                request = new DefaultRequest(wiki_checked_fighters);
                            }

                            WikiAccessor.Request_Size = request.RequestSize;

                            //iteratively create the reply string
                            foreach (string fighter in request.FighterNames)
                            {
                                
                                int index = WikiAccessor.GetIndex(fighter);                                

                                if (index != -1)
                                {
                                    WikiAccessor.GetEntireTable(fighter, index);
                                }
                            }
                            

                            //create the reply object                        
                            if (WikiAccessor.Builder != null && WikiAccessor.Builder.ToString() != "")
                            {
                                //send the reply
                                SendReply(comment, WikiAccessor.Builder.ToString() + "\n\n^(I am a bot. This post was requested by " + comment.AuthorName + ")\n\n[^(Usage / FAQ)](http://redditfighterbotwebapp.azurewebsites.net/)");
                            }
                            else
                            {
                                logger.Debug("Reply attempt failed due to empty StringBuilder");
                            }
                        }
                    }
                }               
            }            
            catch(RedditSharp.RedditHttpException e)
            {
                logger.Debug(e);
                try
                {
                    ReadPasswords();
                    Authenticate();
                }
                catch(Exception ex)
                {
                    logger.Debug(ex);
                    System.Environment.Exit(1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                logger.Debug(e);                
            }

            timer.Change(10000, Timeout.Infinite);
        }


        private static void SendReply(Comment comment, string reply)
        {
            try
            {
                comment.ReplyAsync(reply).Wait();
                logger.Debug("Reply successful!");
            }
            catch (RateLimitException rate)
            {
                logger.Debug(rate.Message);
                Thread.Sleep(45000);
                SendReply(comment, reply);
            }
            catch (Exception e)
            {
                logger.Debug(e);
            }
        }
    }
}