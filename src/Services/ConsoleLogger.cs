#pragma warning disable 1591

using Shinden.Logger;
using System;

namespace Sanakan.Services
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
