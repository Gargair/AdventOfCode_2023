using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using static Solution.Program;
using static System.Net.Mime.MediaTypeNames;

namespace Solution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt");

            // Part 1
            Stopwatch timer = new Stopwatch();
            timer.Start();
            lines = lines.Select(line =>
            {
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1];
                return $"{RepeatWithSeparator(springs, 5, '?')} {RepeatWithSeparator(springGroups, 5, ',')}";
            });

            var work = lines.Select(l => new WorkPackage() { line = l }).OrderByDescending(w => w.GetComplexity());
            var maxWork = work.Count();
            var toWork = maxWork;
            var sum = 0L;

            using (Timer checker = new Timer((_) => Console.WriteLine($"\t{timer.ElapsedMilliseconds}\t\t{maxWork - toWork}/{maxWork}")))
            {
                checker.Change(0, 5000);
                Parallel.ForEach(work, parallelOptions, (w) =>
                {
                    w.CalculateDepthFirst();
                    //Console.WriteLine($"{w.line}: {w.PermCount}");
                    Interlocked.Add(ref sum, w.PermCount);
                    Interlocked.Decrement(ref toWork);
                });
            }

            timer.Stop();
            Console.WriteLine($"Calculating: {timer.ElapsedMilliseconds}");

            Console.WriteLine(sum);
        }

        public static string RepeatWithSeparator(string i, int count, char separator)
        {
            return Enumerable.Range(0, count).Select(_ => i).Aggregate("", (a, b) => $"{a}{separator}{b}")[1..];
        }

        public class Perm
        {
            public int nextSpringGroup = 0;
            public long springsLeftInGroup = 0;
            public bool needEmpty = false;
            public long minRemainingSymbols = 0;

            public Perm() { }
        }

        public static ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 + 1 };

        public class WorkPackage
        {
            public string line = string.Empty;
            public long PermCount = 0;

            public int GetComplexity()
            {
                return line.Where(c => c == '?').Count();
            }

            public void Calculate()
            {
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1].Split(',').Select(c => int.Parse(c));
                var inputLength = springs.Length;

                ConcurrentBag<Perm> validPermutations = new();

                //List<Perm> validPermutations = new();

                if (springs[0] == '?')
                {
                    validPermutations.Add(new Perm()
                    {
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 1
                    });
                    validPermutations.Add(new Perm()
                    {
                        nextSpringGroup = 1,
                        springsLeftInGroup = springGroups.ElementAt(0) - 1,
                        needEmpty = springGroups.ElementAt(0) - 1 == 0,
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 2
                    });
                }
                else if (springs[0] == '.')
                {
                    validPermutations.Add(new Perm()
                    {
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 1
                    });
                }
                else if (springs[0] == '#')
                {
                    validPermutations.Add(new Perm()
                    {
                        nextSpringGroup = 1,
                        springsLeftInGroup = springGroups.ElementAt(0) - 1,
                        needEmpty = springGroups.ElementAt(0) - 1 == 0,
                        minRemainingSymbols = springGroups.Sum() + springGroups.Count() - 2
                    });
                }

                for (int i = 1; i < springs.Length; i++)
                {
                    var currSpring = springs[i];
                    ConcurrentBag<Perm> newPermutations = new ConcurrentBag<Perm>();
                    Parallel.ForEach(validPermutations, parallelOptions, perm =>
                    {
                        if (inputLength - i < perm.minRemainingSymbols)
                        {
                            return;
                        }
                        if (currSpring == '?')
                        {
                            if (perm.needEmpty)
                            {
                                perm.needEmpty = false;
                                perm.minRemainingSymbols--;
                                newPermutations.Add(perm);
                                return;
                            }
                            if (perm.springsLeftInGroup > 0)
                            {
                                perm.springsLeftInGroup--;
                                perm.minRemainingSymbols--;
                                if (perm.springsLeftInGroup == 0)
                                {
                                    perm.needEmpty = true;
                                }
                                newPermutations.Add(perm);
                                return;

                            }
                            if (perm.nextSpringGroup >= springGroups.Count())
                            {
                                newPermutations.Add(perm);
                                return;
                            }
                            var pWH = new Perm()
                            {
                                nextSpringGroup = perm.nextSpringGroup,
                                needEmpty = perm.needEmpty,
                                springsLeftInGroup = perm.springsLeftInGroup,
                                minRemainingSymbols = perm.minRemainingSymbols
                            };

                            pWH.springsLeftInGroup = springGroups.ElementAt(pWH.nextSpringGroup) - 1;
                            pWH.nextSpringGroup++;
                            pWH.minRemainingSymbols--;
                            if (pWH.springsLeftInGroup == 0)
                            {
                                pWH.needEmpty = true;
                            }

                            newPermutations.Add(perm);
                            newPermutations.Add(pWH);
                        }
                        else if (currSpring == '.')
                        {
                            if (perm.springsLeftInGroup > 0)
                            {
                                return;
                            }
                            if (perm.needEmpty)
                            {
                                perm.needEmpty = false;
                                perm.minRemainingSymbols--;
                            }
                            newPermutations.Add(perm);
                        }
                        else if (currSpring == '#')
                        {
                            if (perm.needEmpty)
                            {
                                return;
                            }
                            perm.minRemainingSymbols--;
                            if (perm.springsLeftInGroup > 0)
                            {
                                perm.springsLeftInGroup--;
                                if (perm.springsLeftInGroup == 0)
                                {
                                    perm.needEmpty = true;
                                }

                                newPermutations.Add(perm);
                                return;
                            }
                            if (perm.nextSpringGroup >= springGroups.Count())
                            {
                                return;
                            }
                            perm.springsLeftInGroup = springGroups.ElementAt(perm.nextSpringGroup) - 1;
                            perm.nextSpringGroup++;
                            if (perm.springsLeftInGroup == 0)
                            {
                                perm.needEmpty = true;
                            }

                            newPermutations.Add(perm);
                        }
                    });
                    validPermutations = newPermutations;
                }
                this.PermCount = validPermutations.Where(p => p.nextSpringGroup == springGroups.Count()).Count();
            }

            public void CalculateDepthFirst()
            {
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1].Split(',').Select(c => int.Parse(c)).ToList();

                this.PermCount = CalculateIterator(springs, 0, springGroups, 0, 0, false, springGroups.Sum() + springGroups.Count() - 1);
            }

            private long CalculateIterator(string springs, int index, List<int> springGroups, int nextSpringGroup, int springsLeftInGroup, bool needEmpty, int minRemainingSymbols)
            {
                if (index == springs.Length)
                {
                    if (springsLeftInGroup == 0 && nextSpringGroup == springGroups.Count)
                    {
                        return 1L;
                    }
                    return 0L;
                }
                if (springs.Length - index < minRemainingSymbols)
                {
                    return 0L;
                }
                var curChar = springs[index];
                if (curChar == '?')
                {
                    if (needEmpty)
                    {
                        // We need a non spring (spearator for springgroup)
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup, false, minRemainingSymbols - 1);
                    }
                    if (springsLeftInGroup > 0)
                    {
                        // We are in a spring group. We need a spring
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1);
                    }
                    if (nextSpringGroup >= springGroups.Count)
                    {
                        // We have no springgroups for springs left
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols);
                    }

                    var nonSpringCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols);
                    var springCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1);

                    return nonSpringCount + springCount;
                }
                else if (curChar == '.')
                {
                    // I got a non spring
                    if (springsLeftInGroup > 0)
                    {
                        // But i should get a spring => No solution
                        return 0L;
                    }
                    return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, needEmpty ? minRemainingSymbols - 1 : minRemainingSymbols);
                }
                else if (curChar == '#')
                {
                    // I got a spring
                    if (springsLeftInGroup > 0)
                    {
                        // I am in a springgroup
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1);
                    }
                    // But we need a non spring (separator for springgroup)
                    if(needEmpty)
                    {
                        return 0L;
                    }

                    // No springGroup. Check if we have one
                    if (nextSpringGroup >= springGroups.Count)
                    {
                        return 0L;
                    }
                    return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1);
                }
                return 0L;
            }
        }
    }
}