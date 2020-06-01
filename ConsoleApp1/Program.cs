using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var runTime = new DateTime(2020, 5, 31, 13, 0, 0);
            Console.WriteLine(runTime.ToBinary().ToString());
            Console.ReadLine();
        }
    }
}
