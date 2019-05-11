using System;
using AkunaTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AkunaUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var engine = new OrderMatchingEngine();

            engine.Parse("BUY GFD 1000 10 order1");
            engine.Parse("SELL GFD 900 10 order2");
            Assert.AreEqual("TRADE order1 1000 10 order2 900 10",
                engine.DebugOutput[engine.DebugOutput.Count-1], "Case0010");

            engine.Reset();

            engine.Parse("SELL GFD 900 10 order2");
            engine.Parse("BUY GFD 1000 10 order1");
            Assert.AreEqual("TRADE order2 900 10 order1 1000 10",
                engine.DebugOutput[engine.DebugOutput.Count - 1], "Case0020");

            engine.Reset();

            engine.Parse("BUY GFD 1000 10 order1");
            engine.Parse("BUY GFD 1000 10 order2");
            engine.Parse("SELL GFD 900 20 order3");
            Assert.AreEqual("TRADE order1 1000 10 order3 900 10",
                engine.DebugOutput[engine.DebugOutput.Count - 2], "Case0030");
            Assert.AreEqual("TRADE order2 1000 10 order3 900 10",
                engine.DebugOutput[engine.DebugOutput.Count - 1], "Case0040");

            engine.Reset();

            engine.Parse("BUY GFD 950 10 order1");
            engine.Parse("BUY GFD 1000 15 order2");
            engine.Parse("SELL GFD 900 20 order3");
            Assert.AreEqual("TRADE order2 1000 15 order3 900 15",
                engine.DebugOutput[engine.DebugOutput.Count - 2], "Case0050");
            Assert.AreEqual("TRADE order1 950 5 order3 900 5",
                engine.DebugOutput[engine.DebugOutput.Count - 1], "Case0060");

            engine.Reset();

            engine.Parse("BUY GFD 1000 10 order1");
            engine.Parse("BUY GFD 1000 10 order2");
            engine.Parse("MODIFY order1 BUY 1000 20");
            engine.Parse("SELL GFD 900 20 order3");
            Assert.AreEqual("TRADE order2 1000 10 order3 900 10",
                engine.DebugOutput[engine.DebugOutput.Count - 2], "Case0070");
            Assert.AreEqual("TRADE order1 1000 10 order3 900 10",
                engine.DebugOutput[engine.DebugOutput.Count - 1], "Case0080");

            engine.Reset();

            engine.Parse("BUY GFD 1000 10 order1");
            engine.Parse("BUY GFD 1001 20 order2");
            engine.Parse("PRINT");
            Assert.AreEqual("1001 20",
                engine.DebugOutput[engine.DebugOutput.Count - 2], "Case0090");
            Assert.AreEqual("1000 10",
                engine.DebugOutput[engine.DebugOutput.Count - 1], "Case0100");


            engine.Reset();

            engine.Parse("BUY GFD 1000 10 order1");
            engine.Parse("SELL GFD 900 20 order2");
            engine.Parse("PRINT");
            Assert.AreEqual("TRADE order1 1000 10 order2 900 10",
                engine.DebugOutput[engine.DebugOutput.Count - 4], "Case0110");
            Assert.AreEqual("900 10",
                engine.DebugOutput[engine.DebugOutput.Count - 2], "Case0120");


            engine.Reset();

            engine.Parse("BUY GFD 1000 10 order1");
            engine.Parse("BUY GFD 1010 10 order2");
            engine.Parse("SELL GFD 1000 15 order3");
            Assert.AreEqual("TRADE order2 1010 10 order3 1000 10",
                engine.DebugOutput[engine.DebugOutput.Count - 2], "Case0130");
            Assert.AreEqual("TRADE order1 1000 5 order3 1000 5",
                engine.DebugOutput[engine.DebugOutput.Count - 1], "Case0140");

        }

        [TestMethod]
        public void TestMethod2Boundries()
        {
            var engine = new OrderMatchingEngine();

            engine.Parse("BUY GFD 1000 0 order1");
            engine.Parse("SELL GFD 900 10 order2");
            engine.Parse("PRINT");
            Assert.AreEqual("BUY:",
                engine.DebugOutput[engine.DebugOutput.Count - 1], "Case0500");

            //engine.Reset();
        }

        }
}
