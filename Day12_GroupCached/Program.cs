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
        static void Main(string[] args)
        {
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
            var cachedComputations = new Dictionary<string, long>();
            if (File.Exists("./Cached.txt"))
            {
                foreach (var line in File.ReadLines("./Cached.txt"))
                {
                    var split = line.Split("<=>");
                    cachedComputations.Add(split[0], long.Parse(split[1]));
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

            // 1 and 1 for Part 1, 1 and 5 for Part 2
            var fromRepeat = 1;
            var toRepeat = 5;
            var timeOutMillis = 300000;

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
                        Parallel.ForEach(work, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = abortToken }, (w) =>
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
                            w.Initialize(repeat);
                            if (savedComputations.ContainsKey(w.workLine))
                            {
                                w.PermCount = savedComputations[w.workLine];
                                if (repeat == toRepeat)
                                {
                                    Interlocked.Add(ref sum, w.PermCount);
                                }
                                Interlocked.Decrement(ref toWork);
                                Interlocked.Increment(ref accounted);
                                return;
                            }
                            w.Calculate(cachedComputations, cancellationToken);
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                if (repeat == toRepeat)
                                {
                                    Interlocked.Add(ref sum, w.PermCount);
                                }
                                lock (savedComputations)
                                {
                                    if (!savedComputations.ContainsKey(w.workLine))
                                    {
                                        savedComputations.Add(w.workLine, w.PermCount);
                                    }
                                }
                                Interlocked.Decrement(ref toWork);
                                Interlocked.Increment(ref accounted);
                                return;
                            }
                            if (!abortToken.IsCancellationRequested)
                            {
                                tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                                //tokenSource.CancelAfter(timeOutMillis);
                                cancellationToken = tokenSource.Token;

                                // Try divide and conquer?
                                var numQuestionMarks = w.workLine.Where(c => c == '?').Count();
                                var flipQuestionMark = numQuestionMarks / 4;
                                int flipIndex = NthIndexOf(w.workLine, '?', flipQuestionMark);

                                w.workLine = w.workLine.Substring(0, flipIndex) + '.' + w.workLine.Substring(flipIndex + 1);
                                w.Calculate(cachedComputations, cancellationToken);

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }
                                lock (cachedComputations)
                                {
                                    if (!cachedComputations.ContainsKey(w.workLine))
                                    {
                                        cachedComputations.Add(w.workLine, w.PermCount);
                                    }
                                }
                                var firstPart = w.PermCount;
                                w.PermCount = 0;
                                w.workLine = w.workLine.Substring(0, flipIndex) + '#' + w.workLine.Substring(flipIndex + 1);
                                w.Calculate(cachedComputations, cancellationToken);
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }
                                lock (cachedComputations)
                                {
                                    if (!cachedComputations.ContainsKey(w.workLine))
                                    {
                                        cachedComputations.Add(w.workLine, w.PermCount);
                                    }
                                }
                                var secondPart = w.PermCount;
                                w.workLine = w.workLine.Substring(0, flipIndex) + '?' + w.workLine.Substring(flipIndex + 1);
                                w.PermCount = firstPart + secondPart;

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    if (repeat == toRepeat)
                                    {
                                        Interlocked.Add(ref sum, w.PermCount);
                                    }
                                    lock (savedComputations)
                                    {
                                        if (!savedComputations.ContainsKey(w.workLine))
                                        {
                                            savedComputations.Add(w.workLine, w.PermCount);
                                        }
                                    }
                                    Interlocked.Decrement(ref toWork);
                                    Interlocked.Increment(ref accounted);
                                    return;
                                }
                            }
                            Interlocked.Decrement(ref toWork);
                            lock (notAccountedWriter)
                            {
                                notAccountedWriter.WriteLine(w.workLine);
                            }
                        });
                    }
                }
            }
            using (StreamWriter savedWriter = new StreamWriter("./Saved.txt"))
            {
                foreach (var pair in savedComputations)
                {
                    savedWriter.WriteLine($"{pair.Key}<=>{pair.Value}");
                }
            }
            using (StreamWriter cachedWriter = new StreamWriter("./Cached.txt"))
            {
                foreach (var pair in cachedComputations)
                {
                    cachedWriter.WriteLine($"{pair.Key}<=>{pair.Value}");
                }
            }

            timer.Stop();
            Console.WriteLine($"Accounted for: {accounted}/{maxWork}");
            Console.WriteLine($"Calculating: {timer.Elapsed}");

            Console.WriteLine(sum);
        }

        public static int NthIndexOf(string list, char element, int occurence)
        {
            var foundOccurence = 0;
            var currentIndex = -1;
            while (foundOccurence < occurence)
            {
                currentIndex = list.IndexOf(element, currentIndex + 1);
                if (currentIndex == -1)
                {
                    return -1;
                }
                foundOccurence++;
            }
            return currentIndex;
        }

        public static int NthIndexOf<T>(List<T> list, T element, int occurence)
        {
            var foundOccurence = 0;
            var currentIndex = -1;
            while (foundOccurence < occurence)
            {
                currentIndex = list.IndexOf(element, currentIndex + 1);
                if (currentIndex == -1)
                {
                    return -1;
                }
                foundOccurence++;
            }
            return currentIndex;
        }

        public class WorkPackage
        {
            public string initialLine = string.Empty;
            public string workLine = string.Empty;
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

            public void Initialize(int repeatCount)
            {
                this.PermCount = 0;
                if (repeatCount > 1)
                {
                    this.workLine = RepeatInput(initialLine, repeatCount);
                }
                else
                {
                    this.workLine = this.initialLine;
                }
            }

            public void Calculate(Dictionary<string, long> cachedComputations, CancellationToken cancellationToken)
            {
                //Console.WriteLine($"Starting work on: {this.calculatedLine}");
                (var springs, var springGroupsString) = SplitLine(workLine);
                var springGroups = springGroupsString.Split(',').Select(c => int.Parse(c)).ToList();

                var possibleGroups = springs.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();

                this.PermCount = CalculateForRemainingGroups(possibleGroups, 0, springGroups, 0, cachedComputations, cancellationToken);

                //this.PermCount = CalculateIterator(springs, 0, springGroups, 0, 0, false, springGroups.Sum() + springGroups.Count() - 1, cancellationToken);
            }

            private static long CalculateForRemainingGroups(List<string> groups, int index, List<int> springGroups, int springGroupsConsumed, Dictionary<string, long> cachedComputations, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested) { return 0L; }
                var currentGroup = groups[index];
                if (springGroupsConsumed == springGroups.Count)
                {
                    // I do not have any groups left => check if i have springs.
                    if (currentGroup.Any(c => c == '#'))
                    {
                        return 0L;
                    }
                    else
                    {
                        // I do not have springs. But a Group after me. Ask them?
                        if (index < groups.Count - 1)
                        {
                            return CalculateForRemainingGroups(groups, index + 1, springGroups, springGroupsConsumed, cachedComputations, cancellationToken);
                        }
                        return 1L;
                    }

                }
                var offset = 0;
                List<int> springGroupsToUse = new List<int>();
                if (index == groups.Count - 1)
                {
                    // I am the last group
                    // => Take all Springgroups
                    for (int curSpringGroupIndex = springGroupsConsumed; curSpringGroupIndex < springGroups.Count - 1; curSpringGroupIndex++)
                    {
                        offset++;
                        var curSpringGroup = springGroups[curSpringGroupIndex];
                        springGroupsToUse.Add(curSpringGroup);
                    }
                }

                var solution = 0L;
                for (int curSpringGroupIndex = springGroupsConsumed + offset; curSpringGroupIndex < springGroups.Count; curSpringGroupIndex++)
                {
                    var curSpringGroup = springGroups[curSpringGroupIndex];
                    springGroupsToUse.Add(curSpringGroup);
                    var neededChars = springGroupsToUse.Sum() + springGroupsToUse.Count() - 1;
                    if (neededChars > currentGroup.Length)
                    {
                        break;
                    }
                    var t = GetGroupPermutations(currentGroup, springGroupsToUse, cachedComputations, cancellationToken);
                    if (t > 0 && index < groups.Count - 1)
                    {
                        var inner = CalculateForRemainingGroups(groups, index + 1, springGroups, curSpringGroupIndex + 1, cachedComputations, cancellationToken);
                        solution += t * inner;
                    }
                    else if (t > 0)
                    {
                        // No Groups anymore, do we have springs left?
                        if (curSpringGroupIndex == springGroups.Count - 1)
                        {
                            // We checked last springGroup => all accounted for
                            solution += t;
                        }
                    }
                }

                if (currentGroup.All(c => c != '#') && index < groups.Count - 1)
                {
                    // I do not need a spring
                    solution += CalculateForRemainingGroups(groups, index + 1, springGroups, springGroupsConsumed, cachedComputations, cancellationToken);
                }

                return solution;

            }


            private static long GetGroupPermutations(string group, List<int> springGroups, Dictionary<string, long> cachedComputations, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested) { return 0L; }
                var neededChars = springGroups.Sum() + springGroups.Count() - 1;
                if (group.Length < neededChars)
                {
                    return 0L;
                }
                var key = $"{group} {springGroups.Aggregate("", (a, b) => $"{a},{b}")[1..]}";
                if (cachedComputations.ContainsKey(key))
                {
                    return cachedComputations[key];
                }
                var ret = CalculateIterator(group, 0, springGroups, 0, 0, false, neededChars, cancellationToken);
                if (cancellationToken.IsCancellationRequested) { return ret; }
                lock (cachedComputations)
                {
                    if (!cachedComputations.ContainsKey(key))
                    {
                        cachedComputations.Add(key, ret);
                    }
                }
                return ret;
            }

            private static long CalculateIterator(string springs, int index, List<int> springGroups, int nextSpringGroup, int springsLeftInGroup, bool needEmpty, int minRemainingSymbols, CancellationToken cancellationToken)
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

                var curChar = springs[index];
                if (curChar == '?')
                {
                    if (needEmpty)
                    {
                        // We need a non spring (spearator for springgroup)
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup, false, minRemainingSymbols - 1, cancellationToken);
                    }
                    if (springsLeftInGroup > 0)
                    {
                        // We are in a spring group. We need a spring
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);
                    }
                    if (nextSpringGroup >= springGroups.Count)
                    {
                        // We have no springgroups for springs left
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols, cancellationToken);
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

                    var nonSpringCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols, cancellationToken);
                    var springCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);

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

                    return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, needEmpty ? minRemainingSymbols - 1 : minRemainingSymbols, cancellationToken);
                }
                else if (curChar == '#')
                {
                    // I got a spring
                    if (springsLeftInGroup > 0)
                    {
                        // I am in a springgroup
                        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);
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
                    return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);
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
        }
    }
}