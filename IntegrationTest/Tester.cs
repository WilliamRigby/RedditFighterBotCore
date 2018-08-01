using Nito.AsyncEx;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace IntegrationTest
{
    class Tester
    {
        private const string botname = "/u/RedditFighterBot";
        private static string username;
        private static string password;
        private static string clientid;
        private static string secret;
        private static string redirect;

        public static void Main()
        {
            AsyncContext.Run(() => MainAsync());
        }

        private static async Task MainAsync()
        {
            Reddit reddit = await Authenticate();                        

            Post post = await GetPostAsync(reddit);

            List<string> comments = GetTestComments();

            await SendComments(post, comments);
        }
        
        private static async Task<Reddit> Authenticate()
        {
            await ReadPasswords();

            BotWebAgent bot = new BotWebAgent(username, password, clientid, secret, redirect)
            {
                RateLimiter = new RateLimitManager() 
                { 
                    Mode = RateLimitMode.SmallBurst 
                }
            };

            return new Reddit(bot, false);
        }

        private static async Task ReadPasswords()
        {
            string file = await File.ReadAllTextAsync(@"../RedditFighterBotCore/passwords.xml");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(file);

            XmlNode node = doc.SelectSingleNode($"/config/accounts/account[@debug=\"true\"]");

            username = node.SelectSingleNode("username").InnerText;
            clientid = node.SelectSingleNode("clientid").InnerText;
            secret = node.SelectSingleNode("secret").InnerText;                            
            password = node.SelectSingleNode("password").InnerText;
            redirect = node.SelectSingleNode("redirect").InnerText;                
        }

        private static async Task<Post> GetPostAsync(Reddit reddit)
        {
            Console.WriteLine("Post URL: ");
            Uri uri = new Uri(Console.ReadLine());
            return await reddit.GetPostAsync(uri);
        }

        private static List<string> GetTestComments()
        {
            List<string> comments = new List<string>
            {
                $"{botname} Jon Jones",
                $"{botname} Jose Aldo",
                $"{botname} Fabricio Werdum",
                $"{botname} Floyd Mayweather",
                $"{botname} Pernell Whitaker",
                $"{botname} Julio Caesar Chavez",
                $"{botname} Amir Khan",
                $"{botname} Ricky Hatton",
                $"{botname} Guillermo Rigondeaux",
                $"{botname} Anderson Silva -1",
                $"{botname} Chuck Liddell 500",
                $"{botname} Quinton Jackson 10",
                $"{botname} Rashad Evans, Wanderlei Silva",
                $"{botname} Tito Ortiz, Lyoto Machida, Daniel Cormier 30"
            };

            return comments;
        }

        private static async Task SendComments(Post post, List<string> comments)
        {
            foreach(string comment in comments)
            {
                await post.CommentAsync(comment);
            }
        }
    }
}
