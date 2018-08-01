using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditFighterBot.Execution;
using System.Collections.Generic;

namespace RedditFighterBotCoreTests
{
    [TestClass()]
    public class StringUtilitiesTests
    {

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {

        }

        [TestMethod()]
        public void TestGetRequestStringFromComment_1()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test\n/u/RedditFighterBot rjj 10");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void TestGetRequestStringFromComment_2()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test\n\n/u/RedditFighterBot rjj 10");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void TestGetRequestStringFromComment_3()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test\n\n/u/RedditFighterBot rjj 10\n\nlol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void TestGetRequestStringFromComment_4()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test \n\n/u/RedditFighterBot rjj 10");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void TestDetermineIfPartialTest_1()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 4");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void TestDetermineIfPartialTest_2()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 4789");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void TestDetermineIfPartialTest_3()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/R3edditFighterBot rjj 4");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void TestDetermineIfPartialTest_4()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 4 5");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void TestDetermineIfPartialTest_5()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 442");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void TestDetermineIfPartialTest_6()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 44 asdf");
            Assert.IsFalse(test);
        }

        [TestMethod()]
        public void TestDetermineIfPartialTest_7()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj");
            Assert.IsFalse(test);
        }


        [TestMethod()]
        public void TestGetRequestSizeTest_1()
        {
            int test = StringUtilities.GetUserRequestSize("/u/RedditFighterBot rjj 20");
            Assert.AreEqual(20, test);
        }

        [TestMethod()]
        public void TestGetRequestSizeTest_2()
        {
            int test = StringUtilities.GetUserRequestSize("/u/RedditFighterBot rjj 2 3");
            Assert.AreEqual(3, test);
        }

        [TestMethod()]
        public void TestGetRequestSizeTest_3()
        {
            int test = StringUtilities.GetUserRequestSize("/u/RedditFighterBot rjj 20 asdf");
            Assert.AreEqual(-1, test);
        }

        [TestMethod()]
        public void TestRemoveBotNameTest_1()
        {
            string test = StringUtilities.RemoveBotName("/u/redditfighterbot rjj", "redditfighterbot");
            Assert.IsFalse(test.Contains("/u/redditfighterbot"));
        }

        [TestMethod()]
        public void TestRemoveBotNameTest_2()
        {
            string test = StringUtilities.RemoveBotName("/u/redditfighterbot rjj 10", "redditfighterbot");
            Assert.IsFalse(test.Contains("/u/RedditFighterBot"));
        }

        [TestMethod()]
        public void TestGetFightersTest_1()
        {
            List<string> test = StringUtilities.GetFighters("rjj, jcc, Floyd Mayweather");
            Assert.AreEqual(3, test.Count);
        }      
        
        [TestMethod()]
        public void TestRemoveNumbersTest_1()
        {
            string test = StringUtilities.RemoveNumbers("andrei arlovski 20");
            Assert.AreEqual("andrei arlovski", test);
        }

        [TestMethod()]
        public void TestRemoveNumbersTest_2()
        {
            string test = StringUtilities.RemoveNumbers("andrei arlovski");
            Assert.AreEqual("andrei arlovski", test);
        }

        [TestMethod()]
        public void TestRemoveNumbersTest_3()
        {
            string test = StringUtilities.RemoveNumbers("andrei arlovski -9");
            Assert.AreEqual("andrei arlovski", test);
        }

        [TestMethod()]
        public void TestLevenshteinDistanceTest_1()
        {
            Assert.AreEqual(0, StringUtilities.LevenshteinDistance("cat".ToCharArray(), "cat".ToCharArray()));
        }

        [TestMethod()]
        public void TestLevenshteinDistanceTest_2()
        {
            Assert.AreEqual(1, StringUtilities.LevenshteinDistance("cat".ToCharArray(), "catt".ToCharArray()));
        }

        [TestMethod()]
        public void TestLevenshteinDistanceTest_3()
        {
            Assert.AreEqual(1, StringUtilities.LevenshteinDistance("cat".ToCharArray(), "ca".ToCharArray()));
        }
    }
}
