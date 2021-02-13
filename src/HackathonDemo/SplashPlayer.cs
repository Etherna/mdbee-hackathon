using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.HackathonDemo
{
    class SplashPlayer
    {
        public static async Task PlayAsync(int playTime, int stopTime)
        {
            /* Using try/catch for avoid a crash if frame files are not found. */
            try
            {
                // Load frames.
                var frameFiles = Directory.GetFiles(@"Resources\Splash");
                var frames = frameFiles.Select(ff => File.ReadLines(ff));

                // Draw frames.
                var timePerFrame = playTime / frames.Count();
                Console.Clear();
                foreach (var frame in frames)
                {
                    Console.CursorTop = 0;
                    Console.CursorLeft = 0;
                    foreach (var line in frame)
                    {
                        Console.CursorTop++;
                        Console.CursorLeft = 0;

                        Console.Write(line);
                    }

                    await Task.Delay(timePerFrame);
                }

                // Pause.
                await Task.Delay(stopTime);
            }
            catch { }
        }
    }
}
