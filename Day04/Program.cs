using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Day1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt").ToArray();

            // Part1
            //var points = lines.Select(line =>
            //{
            //    var wN = GetWinningNumbers(line);
            //    if (wN == 0)
            //    {
            //        return 0;
            //    }
            //    return (int)Math.Pow(2, wN - 1);
            //});

            // Part 2
            Dictionary<int, int> multiplier = new Dictionary<int, int>();
            multiplier.Add(0, 1);
            lines.Select((line, i) =>
            {
                //Console.Write(line);
                //Console.Write(" => ");
                var wN = GetWinningNumbers(line);
                //Console.Write(wN);
                //Console.Write(' ');
                var ownMultiplier = multiplier.ContainsKey(i) ? multiplier[i] : 1;
                //Console.Write(ownMultiplier);
                //Console.Write(" ");
                multiplier[i] = ownMultiplier;
                if (wN == 0)
                {
                    return 0;
                }
                for (int j = i + 1; j < i + wN + 1; j++)
                {
                    if (!multiplier.ContainsKey(j))
                    {
                        multiplier[j] = 1;
                    }
                    multiplier[j] += 1 * ownMultiplier;
                }
                //foreach (var pair in multiplier)
                //{
                //    Console.Write($"{pair.Key}:{pair.Value} ");
                //}
                //Console.WriteLine();
                return 0;
            }).Sum();


            Console.WriteLine();
            Console.WriteLine(multiplier.Values.Sum());
        }

        private static int GetWinningNumbers(string line)
        {
            int power = -1;
            var ticket = line.SkipWhile(c => c != ':').Skip(1);
            var winningNumbers = ParseNumbers(ticket.TakeWhile(c => c != '|').Aggregate("", (a, b) => $"{a}{b}"));
            var ownNumbers = ParseNumbers(ticket.SkipWhile(c => c != '|').Skip(1).Aggregate("", (a, b) => $"{a}{b}"));
            return winningNumbers.Intersect(ownNumbers).Count();

            if (power >= 0)
            {
                return (int)Math.Pow(2, power);
            }
            return 0;
        }

        private static List<int> ParseNumbers(string line)
        {
            var splitter = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return splitter.Select(c => int.Parse(c)).ToList();
        }
    }
}