using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using static Solution.Program;
using static System.Net.Mime.MediaTypeNames;

namespace Solution
{
    internal class Program
    {
        static void CheckShit()
        {
            var savedComputations = new Dictionary<string, long>();
            if (File.Exists("./Saved.txt"))
            {
                foreach (var line in File.ReadLines("./Saved.txt"))
                {
                    var split = line.Split("<=>");
                    savedComputations.Add(split[0], long.Parse(split[1]));
                }
            }
            var checkComputations = new Dictionary<string, long>();
            if (File.Exists("./Saved_ForCheck.txt"))
            {
                foreach (var line in File.ReadLines("./Saved_ForCheck.txt"))
                {
                    var split = line.Split("<=>");
                    checkComputations.Add(split[0], long.Parse(split[1]));
                }
            }
            var lines = File.ReadLines("./Input.txt");
            foreach (var line in lines)
            {
                if (!checkComputations.ContainsKey(line))
                {
                    Console.WriteLine($"Not found in check: {line}");
                    continue;
                }
                if (!savedComputations.ContainsKey(line))
                {
                    Console.WriteLine($"Not found in saved: {line}");
                    continue;
                }
                var savedValue = savedComputations[line];
                var checkValue = checkComputations[line];

                if (checkValue != savedValue)
                {
                    Console.WriteLine($"Difference for: {line} {savedValue}!={checkValue}");
                }

            }
        }
        static void Main(string[] args)
        {
            //CheckShit();
            //return;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var lines = File.ReadLines("./Input.txt");

            var savedComputations = new Dictionary<string, long>();
            if (File.Exists("./Saved.txt"))
            {
                foreach (var line in File.ReadLines("./Saved.txt"))
                {
                    var split = line.Split("<=>");
                    savedComputations.Add(split[0], long.Parse(split[1]));
                }
            }
            if (File.Exists("./NotAccounted.txt"))
            {
                File.Delete("./NotAccounted.txt");
            }

            CancellationTokenSource threadAbort = new();
            Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                threadAbort.Cancel();
                Console.WriteLine("Cancellation requested");
            };

            var abortToken = threadAbort.Token;

            // Part 1
            //lines = lines.Select(line =>
            //{
            //    var splitter = line.Split(' ');
            //    var springs = splitter[0];
            //    var springGroups = splitter[1];
            //    return $"{RepeatWithSeparator(springs, 5, '?')} {RepeatWithSeparator(springGroups, 5, ',')}";
            //});

            var fromRepeat = 4;
            var toRepeat = 4;
            var timeOutMillis = 600000;

            var work = lines.Select(l => new WorkPackage() { initialLine = l });
            var maxWork = work.Count() * (toRepeat - fromRepeat + 1);
            var accounted = 0L;
            var toWork = maxWork;
            var sum = 0L;

            using (StreamWriter notAccountedWriter = new StreamWriter("./NotAccounted.txt", new FileStreamOptions() { Mode = FileMode.OpenOrCreate, Access = FileAccess.Write, Options = FileOptions.SequentialScan, Share = FileShare.Read }))
            {
                using (Timer checker = new Timer((_) => Console.WriteLine($"{timer.ElapsedMilliseconds,10:n0}\t\t{accounted}/{maxWork}\t\t{toWork}/{maxWork}")))
                {
                    checker.Change(0, 5000);
                    for (int repeat = fromRepeat; repeat <= toRepeat; repeat++)
                    {
                        Parallel.ForEach(work, new ParallelOptions() { MaxDegreeOfParallelism = 16, CancellationToken = abortToken }, (w) =>
                        {
                            if (abortToken.IsCancellationRequested)
                            {
                                Console.WriteLine("Foreach started despite abortToken set.");
                                return;
                            }
                            CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                            tokenSource.CancelAfter(timeOutMillis);
                            var cancellationToken = tokenSource.Token;
                            if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                            {
                                Console.WriteLine("Cancellationtoken wrong");
                                return;
                            }
                            w.Initialize(repeat, savedComputations);
                            if (savedComputations.ContainsKey(w.calculatedLine))
                            {
                                w.PermCount = savedComputations[w.calculatedLine];
                                if (repeat == toRepeat)
                                {
                                    Interlocked.Add(ref sum, w.PermCount);
                                }
                                Interlocked.Decrement(ref toWork);
                                Interlocked.Increment(ref accounted);
                                return;
                            }
                            w.Calculate(savedComputations, cancellationToken);
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                if (repeat == toRepeat)
                                {
                                    Interlocked.Add(ref sum, w.PermCount);
                                }
                                lock (savedComputations)
                                {
                                    if (!savedComputations.ContainsKey(w.calculatedLine))
                                    {
                                        savedComputations.Add(w.calculatedLine, w.PermCount);
                                    }
                                }
                                if (w.workLine.StartsWith('?'))
                                {
                                    var ww = new WorkPackage();
                                    (var oldSpring, var oldGroups) = WorkPackage.SplitLine(w.workLine);
                                    var newSpring = string.Concat('#', oldSpring[1..]);
                                    var newGroups = oldGroups.Split(',').Select(int.Parse).ToList();
                                    if (newGroups[0] > 1)
                                    {
                                        newGroups[0]--;
                                        ww.workLine = string.Concat(newSpring, ' ', newGroups.Aggregate("", (a, b) => $"{a},{b}")[1..]);
                                        ww.calculatedLine = ww.workLine;
                                        CancellationTokenSource tokenSourceInner = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                                        tokenSourceInner.CancelAfter(timeOutMillis);
                                        cancellationToken = tokenSourceInner.Token;
                                        if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                                        {
                                            Console.WriteLine("Cancellationtoken wrong");
                                            return;
                                        }
                                        if (!savedComputations.ContainsKey(ww.calculatedLine))
                                        {
                                            ww.Calculate(savedComputations, cancellationToken);
                                            if (!cancellationToken.IsCancellationRequested)
                                            {
                                                lock (savedComputations)
                                                {
                                                    if (!savedComputations.ContainsKey(ww.calculatedLine))
                                                    {
                                                        savedComputations.Add(ww.calculatedLine, ww.PermCount);
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }

                                Interlocked.Decrement(ref toWork);
                                Interlocked.Increment(ref accounted);
                            }
                            else
                            {
                                Interlocked.Decrement(ref toWork);
                                lock (notAccountedWriter)
                                {
                                    notAccountedWriter.WriteLine(w.calculatedLine);
                                }
                            }
                        });
                    }
                }
            }
            using (StreamWriter savedWriter = new StreamWriter("./Saved.txt", new FileStreamOptions() { Mode = FileMode.OpenOrCreate, Access = FileAccess.Write, Options = FileOptions.SequentialScan, Share = FileShare.Read }))
            {
                foreach (var pair in savedComputations)
                {
                    savedWriter.WriteLine($"{pair.Key}<=>{pair.Value}");
                }
            }

            timer.Stop();
            Console.WriteLine($"Accounted for: {accounted}/{maxWork}");
            Console.WriteLine($"Calculating: {timer.ElapsedMilliseconds}");

            Console.WriteLine(sum);
        }

        public class WorkPackage
        {
            public string initialLine = string.Empty;
            public string workLine = string.Empty;
            public string calculatedLine = string.Empty;
            public long PermCount = 0;

            public static (string spring, string springGroups) SplitLine(string line)
            {
                var splitter = line.Split(' ');
                return (splitter[0], splitter[1]);
            }

            private static string RepeatWithSeparator(string i, int count, char separator)
            {
                return Enumerable.Range(0, count).Select(_ => i).Aggregate("", (a, b) => $"{a}{separator}{b}")[1..];
            }

            private static string RepeatInput(string i, int count)
            {
                (var springs, var springGroups) = SplitLine(i);
                return $"{RepeatWithSeparator(springs, count, '?')} {RepeatWithSeparator(springGroups, count, ',')}";
            }

            public void Initialize(int repeatCount, Dictionary<string, long> savedComputations)
            {
                this.PermCount = 0;
                if (repeatCount > 1)
                {
                    this.workLine = RepeatInput(initialLine, repeatCount);
                    this.calculatedLine = this.workLine;
                }
                else
                {
                    this.workLine = this.initialLine;
                    this.calculatedLine = this.workLine;
                }
            }

            public void Calculate(Dictionary<string, long> savedComputations, CancellationToken cancellationToken)
            {
                //Console.WriteLine($"Starting work on: {this.calculatedLine}");
                (var springs, var springGroupsString) = SplitLine(workLine);
                var springGroups = springGroupsString.Split(',').Select(c => int.Parse(c)).ToList();

                this.PermCount += CalculateIterator(springs, 0, springGroups, 0, 0, false, springGroups.Sum() + springGroups.Count() - 1, savedComputations, cancellationToken);
            }

            private static long CalculateIterator(string springs, int index, List<int> springGroups, int nextSpringGroup, int springsLeftInGroup, bool needEmpty, int minRemainingSymbols, Dictionary<string, long> savedComputations, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested) { return 0L; }
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
                if (nextSpringGroup == springGroups.Count - 1 && springsLeftInGroup == 0 && !needEmpty)
                {
                    // I am at last springGroup and currently nothing open. => Make quick check
                    return LayoutLastSpringGroup(springs, index, springGroups[nextSpringGroup]);
                }

                long check;
                if (IsAlreadyKnown(springs, index, springGroups, nextSpringGroup, springsLeftInGroup, savedComputations, out check))
                {
                    return check;
                }

                var curChar = springs[index];
                if (curChar == '?')
                {
                    if (needEmpty)
                    {
                        // We need a non spring (spearator for springgroup)
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup, false, minRemainingSymbols - 1, savedComputations, cancellationToken);
                    }
                    if (springsLeftInGroup > 0)
                    {
                        // We are in a spring group. We need a spring
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1, savedComputations, cancellationToken);
                    }
                    if (nextSpringGroup >= springGroups.Count)
                    {
                        // We have no springgroups for springs left
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols, savedComputations, cancellationToken);
                    }

                    if (springs.Skip(index).All(c => c == '?'))
                    {
                        // Only Questionmarks left => All remaining permutations valid
                        var remainingSpringGroups = springGroups.Count - nextSpringGroup;
                        var remainingSprings = springs.Length - index;
                        var numNeededCharacters = springGroups.Skip(nextSpringGroup).Sum();
                        var freeCharacters = remainingSprings - numNeededCharacters;

                        if (remainingSpringGroups == 0)
                        {
                            return 1L;
                        }
                        if (freeCharacters > 0)
                        {
                            if (remainingSpringGroups == 1)
                            {
                                return BinomialCoefficient(freeCharacters + remainingSpringGroups, remainingSpringGroups);
                            }
                            var withOutZero = BinomialCoefficient(freeCharacters - 1, remainingSpringGroups);
                            var forOneZero = BinomialCoefficient(freeCharacters - 1, remainingSpringGroups - 1);
                            var forBothZero = BinomialCoefficient(freeCharacters - 1, remainingSpringGroups - 2);
                            return withOutZero + forOneZero * 2 + forBothZero;
                        }
                        return 1L;

                    }

                    var nonSpringCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols, savedComputations, cancellationToken);
                    var springCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1, savedComputations, cancellationToken);

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

                    var toDoGroups = springGroups.Skip(nextSpringGroup);
                    var toDoSpring = springs.Substring(index + 1);
                    var leftOverKey = $"{toDoSpring} {toDoGroups.Aggregate("", (a, b) => $"{a},{b}")}";
                    if (savedComputations.ContainsKey(leftOverKey))
                    {
                        return savedComputations[leftOverKey];
                    }

                    return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, needEmpty ? minRemainingSymbols - 1 : minRemainingSymbols, savedComputations, cancellationToken);
                }
                else if (curChar == '#')
                {
                    // I got a spring
                    if (springsLeftInGroup > 0)
                    {
                        // I am in a springgroup
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1, savedComputations, cancellationToken);
                    }
                    // But we need a non spring (separator for springgroup)
                    if (needEmpty)
                    {
                        return 0L;
                    }

                    // No springGroup. Check if we have one left
                    if (nextSpringGroup >= springGroups.Count)
                    {
                        return 0L;
                    }
                    return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1, savedComputations, cancellationToken);
                }
                return 0L;
            }

            private static long BinomialCoefficient(long n, long k)
            {
                var result = 1L;
                for (int i = 0; i < k; i++)
                {
                    result *= n - i;
                    result /= (i + 1);
                }
                return result;
            }

            private static long LayoutLastSpringGroup(string springs, int index, int springGroupLength)
            {
                var remaining = springs.Substring(index);
                var layoutGroups = remaining.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var groupsWithSprings = layoutGroups.Where(g => g.IndexOf('#') >= 0);
                var numGroupsWithSpring = groupsWithSprings.Count();
                if (numGroupsWithSpring > 1)
                {
                    // We are looking for a single group of springs. But we have at least two groups with at least one spring separated by '.'
                    return 0L;
                }
                if (numGroupsWithSpring == 1)
                {
                    // We have one group with at least one spring. => We need to lay it out here
                    var groupWithSpring = groupsWithSprings.ElementAt(0);


                    if (groupWithSpring.Length < springGroupLength)
                    {
                        return 0L;
                    }

                    var springIndexes = groupWithSpring.Select((c, i) => c == '#' ? i : -1).Where(i => i >= 0);
                    
                    var earliest = springIndexes.Min();
                    var latest = springIndexes.Max();
                    var length = latest - earliest + 1;
                    
                    if(length > springGroupLength)
                    {
                        return 0L;
                    }
                    if (length == springGroupLength)
                    {
                        return 1L;
                    }
                    var free = springGroupLength - length;
                    var leftAvailable = earliest;
                    var rightAvailable = groupWithSpring.Length - latest - 1;
                    var maxPossible = groupWithSpring.Length - springGroupLength;
                    var combs = Math.Min(Math.Min(free, maxPossible), Math.Min(leftAvailable, rightAvailable)) + 1;
                    return combs;
                }
                else
                {
                    // We have no remaining fixed springs. => The only thing in here are '?' for each group
                    var combinations = layoutGroups.Select(g =>
                    {
                        if (g.Length < springGroupLength)
                        {
                            return 0L;
                        }
                        return g.Length - springGroupLength + 1;
                        //return BinomialCoefficient(g.Length - springGroupLength, 1);
                    });
                    return combinations.Sum();
                }
            }

            private static bool IsAlreadyKnown(string springs, int index, List<int> springGroups, int nextSpringGroup, int springsLeftInGroup, Dictionary<string, long> savedComputations, out long Result)
            {
                var remainingSpring = springs.Substring(index);
                var springGroup = springGroups.Skip(nextSpringGroup).Aggregate("", (a, b) => $"{a},{b}")[1..];
                var extraGroup = springsLeftInGroup > 0 ? $"{springsLeftInGroup}," : "";
                var correctSpring = springsLeftInGroup > 0 ? $"#{remainingSpring.Substring(1)}" : remainingSpring;
                var lineKey = $"{correctSpring} {extraGroup}{springGroup}";
                if (savedComputations.ContainsKey(lineKey))
                {
                    Result = savedComputations[lineKey];
                    return true;
                }
                Result = 0;
                return false;
            }
        }
    }
}