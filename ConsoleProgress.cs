using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerpAPI_Bot
{
    public static class ConsoleProgress
    {
        public static void DrawProgress(int current, int total, int barSize = 30)
        {
            double progress = (double)current / total;
            int filled = (int)(barSize * progress);
            string bar = "[" + new string('=', filled) + new string(' ', barSize - filled) + $"] {progress:P1}";
            Console.Write($"\r{bar}");
            if (current == total)
                Console.WriteLine();
        }
    }

}
