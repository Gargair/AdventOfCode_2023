using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Day1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt").ToArray();

            // Part 1
            //var times = lines[0].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1);
            //var distances = lines[1].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1);
            //var races = times.Zip(distances, (time, distance) => new Race(int.Parse(time), int.Parse(distance)));
            //var numerOfWaysToWin = races.AsParallel().Select((race, i) =>
            //{
            //    //n: n >= 0 && n <= race.timeMs && (race.timeMs - n) * n >= race.distance;
            //    //=> -n^2 + n * race.timeMs - race.distance >= 0
            //    //=> n^2 - n * race.timeMs + race.distance <= 0
            //    //=> 1/2 * race.timeMs +- sqrt(1/4 * race.timeMs ^2 - race.distance)
            //    //3.5 + -sqrt(12.25 - 9);
            //    //3.5 +- sqrt(13/4)
            //    //3.5 +- 1.8
            //    //=> 1.7 | 5.3
            //    //=> 2 | 5
            //    //=> 5 - 2 + 1

            //    int min = (int)Math.Ceiling(0.5 * race.timeMs - Math.Sqrt(0.25 * Math.Pow(race.timeMs, 2) - race.distance) + 0.00001);
            //    int max = (int)Math.Floor(0.5 * race.timeMs + Math.Sqrt(0.25 * Math.Pow(race.timeMs, 2) - race.distance) - 0.00001);
            //    Console.WriteLine($"Race {i}: {min}/{max} => {max - min + 1}");
            //    return max - min + 1;
            //});


            // Part 2
            var times = lines[0].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1).Aggregate((a, b) => a + b);
            var distances = lines[1].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1).Aggregate((a, b) => a + b);
            var races = new Race[] { new Race(long.Parse(times), long.Parse(distances)) };
            var numerOfWaysToWin = races.AsParallel().Select((race, i) =>
            {
                //n: n >= 0 && n <= race.timeMs && (race.timeMs - n) * n >= race.distance;
                //=> -n^2 + n * race.timeMs - race.distance >= 0
                //=> n^2 - n * race.timeMs + race.distance <= 0
                //=> 1/2 * race.timeMs +- sqrt(1/4 * race.timeMs ^2 - race.distance)
                //3.5 + -sqrt(12.25 - 9);
                //3.5 +- sqrt(13/4)
                //3.5 +- 1.8
                //=> 1.7 | 5.3
                //=> 2 | 5
                //=> 5 - 2 + 1

                var min = (long)Math.Ceiling(0.5 * race.timeMs - Math.Sqrt(0.25 * Math.Pow(race.timeMs, 2) - race.distance) + 0.00001);
                var max = (long)Math.Floor(0.5 * race.timeMs + Math.Sqrt(0.25 * Math.Pow(race.timeMs, 2) - race.distance) - 0.00001);
                Console.WriteLine($"Race {i}: {min}/{max} => {max - min + 1}");
                return max - min + 1;
            });
            Console.WriteLine(numerOfWaysToWin.Aggregate((a, b) => a * b));
        }


    }

    class Race
    {
        public long timeMs { get; private set; }
        public long distance { get; private set; }

        public Race(long timeMs, long distance)
        {
            this.timeMs = timeMs;
            this.distance = distance;
        }
    }
}