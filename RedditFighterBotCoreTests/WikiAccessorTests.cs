using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditFighterBot.Execution;
using RedditFighterBot.Models;
using System.Threading.Tasks;

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
        public async Task TestSearchWiki_1()
        {
            WikiAccessor wiki = new WikiAccessor();

            WikiSearchResultDTO test = await wiki.SearchWikiForFightersPage("Fabricio Werdum");

            Assert.AreEqual("", "");
        }

    }
}
