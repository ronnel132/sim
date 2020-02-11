using System;
using NUnit.Framework;

namespace sim
{
    class Program
    {

        public static int add(int x, int y) {
          return x + y;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    [TestFixture]
    class SampleTest {
      [TestCase(1, 2, 3)]
      [TestCase(4, 5, 9)]
      public void Return_Sum(int x, int y, int z) {
        Assert.AreEqual(Program.add(x, y), z);
      }
    }
}
