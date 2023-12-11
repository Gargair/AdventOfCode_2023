using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Solution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt");

            // Part 1
            //var universe = lines.Select(l => l.ToArray()).ToArray();
            //List<Tuple<long, long>> galaxies = new();

            //foreach (var line in universe)
            //{
            //    Console.WriteLine(line);
            //}
            //Console.WriteLine();
            //var expUn = ExpandedUnivserse(universe, 1000000 - 1);
            //for (long i = 0; i < expUn.GetLongLength(0); i++)
            //{
            //    for (long j = 0; j < expUn.GetLongLength(1); j++)
            //    {
            //        if (expUn[i, j] == '#')
            //        {
            //            galaxies.Add(Tuple.Create(i, j));
            //        }
            //        //Console.Write(expUn[i, j]);
            //    }
            //    //Console.WriteLine();
            //}
            //Console.WriteLine();
            //var steps = galaxies.SelectMany((g, i) =>
            //{
            //    return galaxies.Where((_, j) => i < j).Select(h => Math.Abs(g.Item1 - h.Item1) + Math.Abs(g.Item2 - h.Item2));
            //});

            ////foreach(var g in steps)
            ////{
            ////    Console.WriteLine(g);
            ////}

            //Console.WriteLine();
            //Console.WriteLine(steps.Sum());

            // Part 2
            var universe = lines.Select(l => l.ToArray()).ToArray();
            List<Tuple<long, long>> galaxies = new();

            foreach (var line in universe)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine();

            for (long i = 0; i < universe.Length; i++)
            {
                for (long j = 0; j < universe[0].Length; j++)
                {
                    if (universe[i][j] == '#')
                    {
                        galaxies.Add(Tuple.Create(i, j));
                    }
                }
            }

            galaxies = ExpandedUnivserse(universe, galaxies, 1000000 - 1);

            Console.WriteLine();
            var steps = galaxies.SelectMany((g, i) =>
            {
                return galaxies.Where((_, j) => i < j).Select(h => Math.Abs(g.Item1 - h.Item1) + Math.Abs(g.Item2 - h.Item2));
            });

            //foreach(var g in steps)
            //{
            //    Console.WriteLine(g);
            //}

            Console.WriteLine();
            Console.WriteLine(steps.Sum());
        }

        public static char[,] ExpandedUnivserse(char[][] universe, long expansionCount)
        {
            List<long> emptyRows = new List<long>();
            List<long> emptyColumns = new List<long>();

            for (int i = 0; i < universe.Length; i++)
            {
                var curLine = universe[i];
                if (curLine.All(c => c == '.'))
                {
                    emptyRows.Add(i);
                }
            }
            for (int j = 0; j < universe[0].Length; j++)
            {
                if (universe.Select(l => l[j]).All(c => c == '.'))
                {
                    emptyColumns.Add(j);
                }
            }
            char[,] expUn = new char[universe.Length + emptyRows.Count * expansionCount, universe[0].Length + emptyColumns.Count * expansionCount];
            var x = 0L;
            var currentExpansion = 0L;
            bool hasSecond = false;
            for (int i = 0; i < universe.Length; i++)
            {
                var y = 0L;
                for (int j = 0; j < universe.Length; j++)
                {
                    expUn[i + x, j + y] = universe[i][j];
                    if (emptyColumns.Contains(j))
                    {
                        for (int a = 1; a <= expansionCount; a++)
                        {
                            expUn[i + x, j + y + a] = universe[i][j];
                        }
                        y += expansionCount;
                    }
                }
                if (emptyRows.Contains(i) && !hasSecond)
                {
                    currentExpansion = expansionCount;
                    hasSecond = true;
                }
                else if (currentExpansion == 0)
                {
                    hasSecond = false;
                }
                if (currentExpansion > 0)
                {
                    x++;
                    i--;
                    currentExpansion--;
                }
                //else
                //{
                //    hasSecond = false;
                //}
            }
            return expUn;
        }

        public static List<Tuple<long, long>> ExpandedUnivserse(char[][] universe, List<Tuple<long, long>> galaxies, long expansionCount)
        {
            List<long> emptyRows = new List<long>();
            List<long> emptyColumns = new List<long>();

            for (int i = 0; i < universe.Length; i++)
            {
                var curLine = universe[i];
                if (curLine.All(c => c == '.'))
                {
                    emptyRows.Add(i);
                }
            }
            for (int j = 0; j < universe[0].Length; j++)
            {
                if (universe.Select(l => l[j]).All(c => c == '.'))
                {
                    emptyColumns.Add(j);
                }
            }
            List<Tuple<long, long>> expGal = new();

            foreach (var gal in galaxies)
            {
                var newX = gal.Item1;
                var newY = gal.Item2;
                foreach (var r in emptyRows)
                {
                    if (gal.Item1 > r)
                    {
                        newX += expansionCount;
                    }
                }
                foreach (var c in emptyColumns)
                {
                    if (gal.Item2 > c)
                    {
                        newY += expansionCount;
                    }
                }
                expGal.Add(Tuple.Create(newX, newY));
            }

            return expGal;
        }
    }
}