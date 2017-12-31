using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedditFighterBot;
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
        public void GetRequestStringFromComment0()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test\n/u/RedditFighterBot rjj 10");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void GetRequestStringFromComment1()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test\n\n/u/RedditFighterBot rjj 10");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void GetRequestStringFromComment2()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test\n\n/u/RedditFighterBot rjj 10\n\nlol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void GetRequestStringFromComment3()
        {
            string test = StringUtilities.GetRequestStringFromComment("lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test lol this is a test \n\n/u/RedditFighterBot rjj 10");
            Assert.AreEqual("/u/RedditFighterBot rjj 10", test);
        }

        [TestMethod()]
        public void DetermineIfPartialTest1()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 4");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void DetermineIfPartialTest2()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 4789");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void DetermineIfPartialTest3()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/R3edditFighterBot rjj 4");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void DetermineIfPartialTest4()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 4 5");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void DetermineIfPartialTest5()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 442");
            Assert.IsTrue(test);
        }

        [TestMethod()]
        public void DetermineIfPartialTest6()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj 44 asdf");
            Assert.IsFalse(test);
        }

        [TestMethod()]
        public void DetermineIfPartialTest7()
        {
            bool test = StringUtilities.DetermineIfPartial("/u/RedditFighterBot rjj");
            Assert.IsFalse(test);
        }


        [TestMethod()]
        public void GetRequestSizeTest1()
        {
            int test = StringUtilities.GetUserRequestSize("/u/RedditFighterBot rjj 20");
            Assert.AreEqual(20, test);
        }

        [TestMethod()]
        public void GetRequestSizeTest2()
        {
            int test = StringUtilities.GetUserRequestSize("/u/RedditFighterBot rjj 2 3");
            Assert.AreEqual(3, test);
        }

        [TestMethod()]
        public void GetRequestSizeTest3()
        {
            int test = StringUtilities.GetUserRequestSize("/u/RedditFighterBot rjj 20 asdf");
            Assert.AreEqual(-1, test);
        }

        [TestMethod()]
        public void RemoveBotNameTest1()
        {
            string test = StringUtilities.RemoveBotName("/u/redditfighterbot rjj", "redditfighterbot");
            Assert.IsFalse(test.Contains("/u/redditfighterbot"));
        }

        [TestMethod()]
        public void RemoveBotNameTest2()
        {
            string test = StringUtilities.RemoveBotName("/u/redditfighterbot rjj 10", "redditfighterbot");
            Assert.IsFalse(test.Contains("/u/RedditFighterBot"));
        }

        [TestMethod()]
        public void GetFightersTest1()
        {
            List<string> test = StringUtilities.GetFighters("rjj, jcc, Floyd Mayweather");
            Assert.AreEqual(3, test.Count);
        }      
        
        [TestMethod()]
        public void RemoveNumbersTest1()
        {
            string test = StringUtilities.RemoveNumbers("andrei arlovski 20");
            Assert.AreEqual("andrei arlovski", test);
        }

        [TestMethod()]
        public void RemoveNumbersTest2()
        {
            string test = StringUtilities.RemoveNumbers("andrei arlovski");
            Assert.AreEqual("andrei arlovski", test);
        }

        [TestMethod()]
        public void RemoveNumbersTest3()
        {
            string test = StringUtilities.RemoveNumbers("andrei arlovski -9");
            Assert.AreEqual("andrei arlovski", test);
        }

        [TestMethod()]
        public void LevenshteinDistanceTest1()
        {
            Assert.AreEqual(0, StringUtilities.LevenshteinDistance("cat".ToCharArray(), "cat".ToCharArray()));
        }

        [TestMethod()]
        public void LevenshteinDistanceTest2()
        {
            Assert.AreEqual(1, StringUtilities.LevenshteinDistance("cat".ToCharArray(), "catt".ToCharArray()));
        }

        [TestMethod()]
        public void LevenshteinDistanceTest3()
        {
            Assert.AreEqual(1, StringUtilities.LevenshteinDistance("cat".ToCharArray(), "ca".ToCharArray()));
        }
    }
}
