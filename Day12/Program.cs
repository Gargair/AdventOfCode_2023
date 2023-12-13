using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
            //var d = lines.Select(line =>
            //{
            //    //Console.WriteLine(line);
            //    var splitter = line.Split(' ');
            //    var springs = splitter[0];
            //    var springGroups = splitter[1].Split(',');
            //    StringBuilder r = new StringBuilder();
            //    r.Append("^");
            //    bool isFirst = true;
            //    foreach (var group in springGroups)
            //    {
            //        if (isFirst)
            //        {
            //            r.Append("\\.*");
            //            isFirst = false;
            //        }
            //        else
            //        {
            //            r.Append("\\.+");
            //        }
            //        var l = long.Parse(group);
            //        for (int i = 0; i < l; i++)
            //        {
            //            r.Append("#");
            //        }

            //    }
            //    r.Append("\\.*$");
            //    var regEx = new Regex(r.ToString());

            //    var questionMarks = springs.Select((s, i) => s == '?' ? i : -1).Where(i => i >= 0);

            //    var numberOfPermutations = (int)Math.Pow(2, questionMarks.Count());
            //    var permutations = Enumerable.Range(1, numberOfPermutations).Select((p) =>
            //    {
            //        var numberOfQuestionMark = 0;
            //        return springs.Select((spring, j) =>
            //        {
            //            if (spring == '?')
            //            {
            //                var div = (int)Math.Pow(2, numberOfQuestionMark);
            //                numberOfQuestionMark++;
            //                if ((p & div) == 0)
            //                {
            //                    return '.';
            //                }
            //                else
            //                {
            //                    return '#';
            //                }
            //            }

            //            return spring;
            //        }).Aggregate("", (a, b) => $"{a}{b}");
            //    });

            //    //foreach (var p in permutations)
            //    //{
            //    //    Console.WriteLine($"\t{p}");
            //    //}


            //    return permutations.Count(p => regEx.IsMatch(p));
            //});

            ////foreach (var g in d)
            ////{
            ////    Console.WriteLine(g);
            ////}

            //Console.WriteLine(d.Sum());

            // Part 2
            Stopwatch timer = new Stopwatch();
            timer.Start();
            lines = lines.AsParallel().Select(line =>
            {
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1];
                return $"{RepeatWithSeparator(springs, 5, '?')} {RepeatWithSeparator(springGroups, 5, ',')}";
            });
            var d = lines.AsParallel().Select(line =>
            {
                //Console.WriteLine($"{line}: ");
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1].Split(',').Select(c => int.Parse(c));

                List<Perm> validPermutations = new();

                if (springs[0] == '?')
                {
                    validPermutations.Add(new Perm()
                    {
                        currentPermutation = ".",
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 1
                    });
                    validPermutations.Add(new Perm()
                    {
                        currentPermutation = "#",
                        currentSpringGroup = 1,
                        springsLeftInGroup = springGroups.ElementAt(0) - 1,
                        needEmpty = springGroups.ElementAt(0) - 1 == 0,
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 2
                    });
                }
                else if (springs[0] == '.')
                {
                    validPermutations.Add(new Perm()
                    {
                        currentPermutation = ".",
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 1
                    });
                }
                else if (springs[0] == '#')
                {
                    validPermutations.Add(new Perm()
                    {
                        currentPermutation = "#",
                        currentSpringGroup = 1,
                        springsLeftInGroup = springGroups.ElementAt(0) - 1,
                        needEmpty = springGroups.ElementAt(0) - 1 == 0,
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 2
                    });
                }

                for (int i = 1; i < springs.Length; i++)
                {
                    var currSpring = springs[i];
                    for (int p = 0; p < validPermutations.Count; p++)
                    {
                        var perm = validPermutations[p];
                        if (springs.Length - perm.currentPermutation.Length < perm.minRemainingSymbols)
                        {
                            validPermutations.RemoveAt(p);
                            p--;
                            continue;
                        }
                        if (currSpring == '?')
                        {
                            if (perm.needEmpty)
                            {
                                perm.needEmpty = false;
                                perm.currentPermutation += '.';
                                perm.minRemainingSymbols--;
                                continue;
                            }
                            if (perm.springsLeftInGroup > 0)
                            {
                                perm.currentPermutation += '#';
                                perm.springsLeftInGroup--;
                                perm.minRemainingSymbols--;
                                if (perm.springsLeftInGroup == 0)
                                {
                                    perm.needEmpty = true;
                                }
                                continue;
                            }
                            if (perm.currentSpringGroup >= springGroups.Count())
                            {
                                perm.currentPermutation += '.';
                                continue;
                            }
                            var pWH = new Perm()
                            {
                                currentPermutation = perm.currentPermutation,
                                currentSpringGroup = perm.currentSpringGroup,
                                needEmpty = perm.needEmpty,
                                springsLeftInGroup = perm.springsLeftInGroup,
                                minRemainingSymbols = perm.minRemainingSymbols
                            };
                            perm.currentPermutation += '.';

                            pWH.currentPermutation += '#';
                            pWH.springsLeftInGroup = springGroups.ElementAt(pWH.currentSpringGroup) - 1;
                            pWH.currentSpringGroup++;
                            pWH.minRemainingSymbols--;
                            if (pWH.springsLeftInGroup == 0)
                            {
                                pWH.needEmpty = true;
                            }
                            validPermutations.Insert(p, pWH);
                            p++;
                        }
                        else if (currSpring == '.')
                        {
                            if (perm.springsLeftInGroup > 0)
                            {
                                validPermutations.RemoveAt(p);
                                p--;
                                continue;
                            }
                            if (perm.needEmpty)
                            {
                                perm.needEmpty = false;
                                perm.minRemainingSymbols--;
                            }
                            perm.currentPermutation += currSpring;
                        }
                        else if (currSpring == '#')
                        {
                            if (perm.needEmpty)
                            {
                                validPermutations.RemoveAt(p);
                                p--;
                                continue;
                            }
                            perm.minRemainingSymbols--;
                            perm.currentPermutation += currSpring;
                            if (perm.springsLeftInGroup > 0)
                            {
                                perm.springsLeftInGroup--;
                                if (perm.springsLeftInGroup == 0)
                                {
                                    perm.needEmpty = true;
                                }
                                continue;
                            }
                            if (perm.currentSpringGroup >= springGroups.Count())
                            {
                                validPermutations.RemoveAt(p);
                                p--;
                                continue;
                            }
                            perm.springsLeftInGroup = springGroups.ElementAt(perm.currentSpringGroup) - 1;
                            perm.currentSpringGroup++;
                            if (perm.springsLeftInGroup == 0)
                            {
                                perm.needEmpty = true;
                            }
                        }
                    }
                }
                validPermutations = validPermutations.Where(p => p.currentSpringGroup == springGroups.Count()).ToList();

                //foreach (var p in validPermutations)
                //{
                //    Console.WriteLine($"\t{p}");
                //}
                //Console.WriteLine($"\t{validPermutations.Count()}");

                return validPermutations.Count;
            });
            timer.Stop();
            Console.WriteLine($"Setting up Linq: {timer.ElapsedMilliseconds}");
            
            timer.Restart();
            var r = d.Sum();
            timer.Stop();
            Console.WriteLine($"Calculating: {timer.ElapsedMilliseconds}");

            //foreach (var g in d)
            //{
            //    Console.WriteLine(g);
            //}
            Console.WriteLine();

            Console.WriteLine(r);
        }

        public static string RepeatWithSeparator(string i, int count, char separator)
        {
            return Enumerable.Range(0, count).Select(_ => i).Aggregate("", (a, b) => $"{a}{separator}{b}")[1..];
        }

        public class Perm
        {
            public int currentSpringGroup = 0;
            public long springsLeftInGroup = 0;
            public bool needEmpty = false;
            public string currentPermutation = "";
            public long minRemainingSymbols = 0;

            public Perm() { }

            public override string ToString()
            {
                return currentPermutation;
            }
        }
    }
}