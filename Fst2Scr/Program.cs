using System;
using System.IO;

namespace Fst2Scr
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var inStream = args.Length < 1
                ? Console.OpenStandardInput()
                : new FileStream(args[0], FileMode.Open);

            var outStream = args.Length < 2
                ? Console.OpenStandardOutput()
                : new FileStream(args[1], FileMode.OpenOrCreate);

            Fst2Scr.Convert(inStream, outStream);

            inStream.Close();
            outStream.Close();
        }
    }
}