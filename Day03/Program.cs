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
            //var sum = 0;

            //for (int i = 0; i < lines.Length; i++)
            //{
            //    var current = 0;
            //    if (i == 0)
            //    {
            //        current = CheckLine(lines[i], null, lines[i + 1]);
            //    }
            //    else if (i == lines.Length - 1)
            //    {
            //        current = CheckLine(lines[i], lines[i - 1], null);
            //    }
            //    else
            //    {
            //        current = CheckLine(lines[i], lines[i - 1], lines[i + 1]);
            //    }
            //    Console.WriteLine($"{i}: {current}");
            //    sum += current;
            //}

            // Part 2
            var sum = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var current = 0;
                if (i == 0)
                {
                    current = CheckForGear(lines[i], null, lines[i + 1]);
                }
                else if (i == lines.Length - 1)
                {
                    current = CheckForGear(lines[i], lines[i - 1], null);
                }
                else
                {
                    current = CheckForGear(lines[i], lines[i - 1], lines[i + 1]);
                }
                Console.WriteLine($"{i}: {current}");
                sum += current;
            }

            Console.WriteLine();
            Console.WriteLine(sum);
        }


        private static int CheckLine(string currentLine, string? lastLine, string? nextLine)
        {
            //Console.WriteLine($"   LastLine: {lastLine}");
            //Console.WriteLine($"CurrentLine: {currentLine}");
            //Console.WriteLine($"   NextLine: {nextLine}");
            var lineSum = 0;
            var i = 0;
            while (i < currentLine.Length)
            {
                var c = currentLine[i];
                if (c != '.')
                {
                    string currentNumber = currentLine.Skip(i).TakeWhile(c => (byte)c >= 48 && (byte)c <= 57).Aggregate("", (a, b) => $"{a}{b}");
                    if (currentNumber != string.Empty)
                    {
                        //Console.WriteLine($"{currentNumber}");

                        //Console.WriteLine($"Part LastLine: {lastLine?.Skip(i > 0 ? i - 1 : i).Take(currentNumber.Length + 1).Aggregate("", (a, b) => $"{a}{b}")}");
                        //Console.WriteLine($"Part NextLine: {nextLine?.Skip(i > 0 ? i - 1 : i).Take(currentNumber.Length + 1).Aggregate("", (a, b) => $"{a}{b}")}");
                        //Console.WriteLine($"Part Symbols LastLine: {lastLine?.Skip(i > 0 ? i - 1 : i).Take(i > 0 ? currentNumber.Length + 2 : currentNumber.Length + 1).Where(c => c != '.' && !((byte)c >= 48 && (byte)c <= 57)).Count()}");
                        //Console.WriteLine($"Part Symbols NextLine: {nextLine?.Skip(i > 0 ? i - 1 : i).Take(i > 0 ? currentNumber.Length + 2 : currentNumber.Length + 1).Where(c => c != '.' && !((byte)c >= 48 && (byte)c <= 57)).Count()}");

                        if (lastLine != null && lastLine.Skip(i > 0 ? i - 1 : i).Take(i > 0 ? currentNumber.Length + 2 : currentNumber.Length + 1).Where(c => c != '.' && !((byte)c >= 48 && (byte)c <= 57)).Count() > 0)
                        {
                            //Console.WriteLine("Found in LastLine");
                            lineSum += int.Parse(currentNumber);
                        }
                        else if (nextLine != null && nextLine.Skip(i > 0 ? i - 1 : i).Take(i > 0 ? currentNumber.Length + 2 : currentNumber.Length + 1).Where(c => c != '.' && !((byte)c >= 48 && (byte)c <= 57)).Count() > 0)
                        {
                            //Console.WriteLine("Found in NextLine");
                            lineSum += int.Parse(currentNumber);
                        }
                        else if (i > 0 && currentLine[i - 1] != '.')
                        {
                            //Console.WriteLine("Found in CurrentLine (prev)");
                            lineSum += int.Parse(currentNumber);
                        }
                        else if (i + currentNumber.Length < currentLine.Length && currentLine[i + currentNumber.Length] != '.')
                        {
                            //Console.WriteLine("Found in CurrentLine (next)");
                            lineSum += int.Parse(currentNumber);
                        }
                        i += currentNumber.Length - 1;
                    }
                }
                i++;
            }
            return lineSum;
        }

        private static int CheckForGear(string currentLine, string? lastLine, string? nextLine)
        {
            Console.WriteLine($"   LastLine: {lastLine}");
            Console.WriteLine($"CurrentLine: {currentLine}");
            Console.WriteLine($"   NextLine: {nextLine}");
            var lineSum = 0;
            var i = 0;
            while (i < currentLine.Length)
            {
                var c = currentLine[i];
                if (c == '*')
                {
                    var numbers = new List<int>();
                    if (i > 0 && (byte)currentLine[i - 1] >= 48 && (byte)currentLine[i - 1] <= 57)
                    {
                        var prevNumber = currentLine.Take(i).Reverse().TakeWhile(c => (byte)c >= 48 && (byte)c <= 57).Reverse().Aggregate("", (a, b) => $"{a}{b}");
                        Console.WriteLine($"PrevNumber: {prevNumber}");
                        numbers.Add(int.Parse(prevNumber));
                    }
                    if (i + 1 < currentLine.Length && (byte)currentLine[i + 1] >= 48 && (byte)currentLine[i + 1] <= 57)
                    {
                        var nextNumber = currentLine.Skip(i + 1).TakeWhile(c => (byte)c >= 48 && (byte)c <= 57).Aggregate("", (a, b) => $"{a}{b}");
                        Console.WriteLine($"NextNumber: {nextNumber}");
                        numbers.Add(int.Parse(nextNumber));
                    }
                    if (lastLine != null)
                    {
                        var lastNumbers = CheckLineForNumbers(lastLine, i);
                        numbers.AddRange(lastNumbers);
                    }
                    if(nextLine!= null)
                    {
                        var nextNumbers = CheckLineForNumbers(nextLine, i);
                        numbers.AddRange(nextNumbers);
                    }

                    if (numbers.Count == 2)
                    {
                        lineSum += numbers.Aggregate((a, b) => a * b);
                    }
                }
                i++;
            }
            return lineSum;
        }

        private static List<int> CheckLineForNumbers(string currentLine, int overlappingIndex)
        {
            var numbers = new List<int>();
            var i = 0;

            while (i < currentLine.Length)
            {
                var c = currentLine[i];
                if (c != '.')
                {
                    string currentNumber = currentLine.Skip(i).TakeWhile(c => (byte)c >= 48 && (byte)c <= 57).Aggregate("", (a, b) => $"{a}{b}");
                    if (currentNumber != string.Empty)
                    {
                        if (i - 1 <= overlappingIndex && overlappingIndex <= i + currentNumber.Length)
                        {
                            numbers.Add(int.Parse(currentNumber));
                        }
                        i += currentNumber.Length - 1;
                    }
                }
                i++;
            }

            return numbers;
        }
    }
}