using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Summary
{
    internal class Day06_Solution : IDaySolution
    {
        public long GetInputSize()
        {
            return Input.Split(Environment.NewLine).Length;
        }

        public long Part1()
        {
            var lines = Input.Split(Environment.NewLine);

            var times = lines[0].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1);
            var distances = lines[1].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1);
            var races = times.Zip(distances, (time, distance) => (long.Parse(time), long.Parse(distance)));
            var numerOfWaysToWin = races.AsParallel().Select((race) =>
            {
                //n: n >= 0 && n <= race.timeMs && (race.timeMs - n) * n >= race.distance;
                //=> -n^2 + n * race.timeMs - race.distance >= 0
                //=> n^2 - n * race.timeMs + race.distance <= 0
                //=> 1/2 * race.timeMs +- sqrt(1/4 * race.timeMs ^2 - race.distance)

                var min = (long)Math.Ceiling(0.5 * race.Item1 - Math.Sqrt(0.25 * Math.Pow(race.Item1, 2) - race.Item2) + 0.00001);
                var max = (long)Math.Floor(0.5 * race.Item1 + Math.Sqrt(0.25 * Math.Pow(race.Item1, 2) - race.Item2) - 0.00001);
                return max - min + 1;
            });
            return numerOfWaysToWin.Aggregate((a, b) => a * b);
        }

        public long Part2()
        {
            var lines = Input.Split(Environment.NewLine);

            var times = lines[0].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1);
            var distances = lines[1].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(1);

            var time = long.Parse(times.Aggregate(string.Concat));
            var distance = long.Parse(distances.Aggregate(string.Concat));
            var min = (long)Math.Ceiling(0.5 * time - Math.Sqrt(0.25 * Math.Pow(time, 2) - distance) + 0.00001);
            var max = (long)Math.Floor(0.5 * time + Math.Sqrt(0.25 * Math.Pow(time, 2) - distance) - 0.00001);

            return max - min + 1;
        }


        private const string Input = """
            Time:        46     80     78     66
            Distance:   214   1177   1402   1024
            """;
    }
}
