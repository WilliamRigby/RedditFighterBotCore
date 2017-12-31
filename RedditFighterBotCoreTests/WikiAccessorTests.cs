using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditFighterBot;
using RedditFighterBot.Models;

namespace RedditFighterBotCoreTests
{
    [TestClass()]
    public class WikiAccessorTests
    {

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {

        }

        [TestMethod()]
        public void SearchWiki1()
        {
            WikiSearchResultDTO test = WikiAccessor.SearchWiki("Fabricio Werdum");
            Assert.AreEqual("", "");
        }

    }
}
