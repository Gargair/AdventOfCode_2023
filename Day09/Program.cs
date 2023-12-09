using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Solution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt").ToArray();

            // Part 1
            var lineValues = lines.Select(line =>
            {
                string[] splitted = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                long[] input = splitted.Select(x => long.Parse(x)).ToArray();
                List<long> lastNumbers = new List<long>();
                lastNumbers.Add(input[input.Length - 1]);
                while (input.Any(x => x != 0))
                {
                    long[] output;
                    ReduceInput(input, out output);
                    lastNumbers.Add(output[output.Length - 1]);
                    input = output;
                }
                return lastNumbers.Sum();
            });

            //Console.WriteLine(lineValues.Aggregate("", (a, b) => $"{a} {b}"));

            Console.WriteLine("Part 1");
            Console.WriteLine(lineValues.Sum());


            // Part 2
            var lineValuesP2 = lines.Select(line =>
            {
                string[] splitted = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                long[] input = splitted.Select(x => long.Parse(x)).ToArray();
                List<long> firstNumbers = new List<long>();
                firstNumbers.Add(input[0]);
                while (input.Any(x => x != 0))
                {
                    long[] output;
                    ReduceInput(input, out output);
                    firstNumbers.Add(output[0]);
                    input = output;
                }
                firstNumbers.Reverse();
                return firstNumbers.Aggregate(0L, (a, b) => b - a);
            });

            //Console.WriteLine(lineValuesP2.Aggregate("", (a, b) => $"{a} {b}"));

            Console.WriteLine("Part 2");
            Console.WriteLine(lineValuesP2.Sum());
        }

        static void ReduceInput(long[] input, out long[] output)
        {
            output = new long[input.Length - 1];
            for (int i = 0; i < input.Length - 1; i++)
            {
                output[i] = input[i + 1] - input[i];
            }
        }
    }
}