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
            //var instructions = lines[0];

            //var dict = new Dictionary<string, string[]>();

            //foreach (var lin in lines.Skip(2))
            //{
            //    Regex r = new Regex("(?'from'\\w{3}) = \\((?'left'\\w{3}), (?'right'\\w{3})\\)");
            //    var m = r.Match(lin);
            //    if (m.Success)
            //    {
            //        dict.Add(m.Groups["from"].Value, new string[] { m.Groups["left"].Value, m.Groups["right"].Value });
            //    }
            //    else
            //    {
            //        Console.WriteLine($"Non matched line: {lin}");
            //    }
            //}

            //var count = 0;
            //var currentNode = "AAA";

            //while (currentNode != "ZZZ")
            //{
            //    foreach (var inst in instructions)
            //    {
            //        Console.Write($"{currentNode} => ");
            //        var curNext = dict[currentNode];
            //        if (inst == 'L')
            //        {
            //            count++;
            //            currentNode = curNext[0];
            //        }
            //        else if (inst == 'R')
            //        {
            //            count++;
            //            currentNode = curNext[1];
            //        }
            //        else
            //        {
            //            Console.WriteLine($"Unknown instruction: {inst}");
            //        }
            //        if (currentNode == "ZZZ")
            //        {
            //            Console.WriteLine(currentNode);
            //            break;
            //        }
            //    }
            //}

            //Console.WriteLine();
            //Console.WriteLine(count);

            // Part 2
            var instructions = lines[0];

            var dict = new Dictionary<string, string[]>();
            var startNodes = new List<string>();

            foreach (var lin in lines.Skip(2))
            {
                Regex r = new Regex("(?'from'\\w{3}) = \\((?'left'\\w{3}), (?'right'\\w{3})\\)");
                var m = r.Match(lin);
                if (m.Success)
                {
                    dict.Add(m.Groups["from"].Value, new string[] { m.Groups["left"].Value, m.Groups["right"].Value });
                    if (m.Groups["from"].Value.EndsWith("A"))
                    {
                        startNodes.Add(m.Groups["from"].Value);
                    }
                }
                else
                {
                    Console.WriteLine($"Non matched line: {lin}");
                }
            }

            Console.WriteLine($"Number of Nodes: {dict.Keys.Count}");
            Console.WriteLine($"Number of StartNodes: {startNodes.Count}");

            for (int i = 0; i < startNodes.Count; i++)
            {
                var currentNode = startNodes[i];
                var count = 0;
                var runs = 5;
                Console.Write($"{currentNode} => ");
                while (runs > 0)
                {
                    runs--;
                    bool start = true;
                    while (!currentNode.EndsWith("Z") || start)
                    {
                        start = false;
                        foreach (var inst in instructions)
                        {
                            count++;

                            var curNext = dict[currentNode];
                            if (inst == 'L')
                            {
                                currentNode = curNext[0];
                            }
                            else if (inst == 'R')
                            {
                                currentNode = curNext[1];
                            }
                            else
                            {
                                Console.WriteLine($"Unknown instruction: {inst}");
                            }

                            if (currentNode.EndsWith("Z"))
                            {
                                if (runs == 0)
                                {
                                    break;
                                }
                                else
                                {
                                    Console.Write($"{currentNode}: {count} => ");
                                    runs--;
                                    start = true;
                                }
                            }
                        }
                    }
                    Console.Write($"{currentNode}: {count} => ");
                }
                Console.WriteLine();
            }

            // 17873
            // 19631
            // 17287
            // 12599
            // 21389
            // 20803
            //Console.WriteLine();
            //Console.WriteLine(count);
            long z1 = kgv(17873, 19631);
            long z2 = kgv(17287, 12599);
            long z3 = kgv(21389, 20803);
            Console.WriteLine($"{z1} {z2} {z3}");
            Console.WriteLine(kgv(kgv(z1, z2), z3));
        }

        public static long ggT(long _z1, long _z2)
        {
            long z1 = _z1;
            long z2 = _z2;
            long tmp = 0;
            long ggt = 0;
            while (z1 % z2 != 0)
            {
                tmp = z1 % z2;
                z1 = z2;
                z2 = tmp;
            }
            ggt = z2;
            return ggt;
        }
        public static long kgv(long z1, long z2)
        {
            return (z1 * z2) / ggT(z1, z2);
        }
    }
}