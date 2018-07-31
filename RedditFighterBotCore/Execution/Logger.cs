using log4net;
using System;
using System.IO;
using System.Reflection;

namespace RedditFighterBotCore.Execution
{
    public static class Logger
    {
        private static readonly ILog logger;

        static Logger()
        {
            logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            var logRepo = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepo, new FileInfo("app.config"));
        }

        public static void LogMessage(string message)
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            logger.Debug($"[{time}]  {message}");
            Console.WriteLine(message);
        }
    }
}
