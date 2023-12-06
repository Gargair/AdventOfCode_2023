using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Day1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt");

            // Part1
            //var maxCubes = new Dictionary<string, int>();
            //maxCubes.Add("blue", 14);
            //maxCubes.Add("red", 12);
            //maxCubes.Add("green", 13);
            //var numbers = lines.Select(line =>
            //{
            //    return CheckIfPlayIsPossible(line, maxCubes);
            //});

            // Part 2
            var numbers = lines.Select(line =>
            {
                return GetPowerOfGame(line);
            });

            Console.WriteLine(numbers.Sum());
        }

        private static int CheckIfPlayIsPossible(string line, Dictionary<string, int> maxCubes)
        {
            var splitter = line.IndexOf(':');
            var gameNumber = int.Parse(line.Substring(4, splitter - 4));
            var game = line.Substring(splitter + 1);
            var rounds = game.Split(';');
            foreach (var round in rounds)
            {
                var cubes = round.Split(',');
                foreach (var cub in cubes)
                {
                    var t = cub.Trim().Split(' ');
                    var count = int.Parse(t[0]);
                    if (maxCubes.ContainsKey(t[1]))
                    {
                        if (count > maxCubes[t[1]])
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unknown cube {t[1]} found");
                        return 0;
                    }
                }
            }
            return gameNumber;

        }

        private static int GetPowerOfGame(string line)
        {
            var splitter = line.IndexOf(':');
            var gameNumber = int.Parse(line.Substring(4, splitter - 4));
            var game = line.Substring(splitter + 1);
            var rounds = game.Split(';');
            var minCubes = new Dictionary<string, int>();
            foreach (var round in rounds)
            {
                var cubes = round.Split(',');
                foreach (var cub in cubes)
                {
                    var t = cub.Trim().Split(' ');
                    var count = int.Parse(t[0]);
                    if (!minCubes.ContainsKey(t[1]))
                    {
                        minCubes.Add(t[1], count);
                    }
                    else if (count > minCubes[t[1]])
                    {
                        minCubes[t[1]] = count;
                    }
                }
            }
            return minCubes.Values.Aggregate((a, b) => a * b);
        }

        private static char FindNumber(string line, int i)
        {
            char c = line[i];
            if ((byte)c >= 48 && (byte)c <= 57)
            {
                return c;
            }
            else if (i < line.Length - 2 && line.Skip(i).Take(3).Aggregate("", (a, b) => $"{a}{b}") == "one")
            {
                return '1';
            }
            else if (i < line.Length - 2 && line.Skip(i).Take(3).Aggregate("", (a, b) => $"{a}{b}") == "two")
            {
                return '2';
            }
            else if (i < line.Length - 4 && line.Skip(i).Take(5).Aggregate("", (a, b) => $"{a}{b}") == "three")
            {
                return '3';
            }
            else if (i < line.Length - 3 && line.Skip(i).Take(4).Aggregate("", (a, b) => $"{a}{b}") == "four")
            {
                return '4';
            }
            else if (i < line.Length - 3 && line.Skip(i).Take(4).Aggregate("", (a, b) => $"{a}{b}") == "five")
            {
                return '5';
            }
            else if (i < line.Length - 2 && line.Skip(i).Take(3).Aggregate("", (a, b) => $"{a}{b}") == "six")
            {
                return '6';
            }
            else if (i < line.Length - 4 && line.Skip(i).Take(5).Aggregate("", (a, b) => $"{a}{b}") == "seven")
            {
                return '7';
            }
            else if (i < line.Length - 4 && line.Skip(i).Take(5).Aggregate("", (a, b) => $"{a}{b}") == "eight")
            {
                return '8';
            }
            else if (i < line.Length - 3 && line.Skip(i).Take(4).Aggregate("", (a, b) => $"{a}{b}") == "nine")
            {
                return '9';
            }
            else
            {
                return ' ';
            }
        }
    }
}