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
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var lines = File.ReadLines("./Input.txt");
            var savedComputation = new Dictionary<string, long>();
            if (File.Exists("./Intermediate.txt"))
            {
                var intermediate = File.ReadLines("./Intermediate.txt");
                foreach (var line in intermediate)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var h = line.Split("<=>");
                        savedComputation.Add(h[0], long.Parse(h[1]));
                    }
                }
            }
            if (File.Exists("./NotAccounted.txt"))
            {
                File.Delete("./NotAccounted.txt");
            }
            var sum = 0L;
            CancellationTokenSource threadAbort = new();
            Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                threadAbort.Cancel();
                Console.WriteLine("Cancellation requested");
            };

            var abortToken = threadAbort.Token;


            // Comment out for Part 1
            lines = lines.Select(line =>
            {
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1];
                return $"{RepeatWithSeparator(springs, 5, '?')} {RepeatWithSeparator(springGroups, 5, ',')}";
            });

            var work = lines.Select(l => new WorkPackage() { line = l });

            var maxWork = work.Count();
            var toWork = maxWork;

            var gs = work.GroupBy(w => savedComputation.ContainsKey(w.line));
            var workedGroup = gs.FirstOrDefault(g => g.Key);
            var toWorkGroup = gs.FirstOrDefault(g => !g.Key);

            if (workedGroup != null)
            {
                foreach (var w in workedGroup)
                {
                    toWork--;
                    sum += savedComputation[w.line];
                }
            }
            var toCheck = toWork;
            if (toWorkGroup != null)
            {
                work = toWorkGroup.AsEnumerable();

                using (var interWriter = new StreamWriter("./Intermediate.txt", true))
                {
                    interWriter.AutoFlush = true;
                    using (var notAccountedWriter = new StreamWriter("./NotAccounted.txt", false))
                    {
                        using (Timer checker = new((_) =>
                        {
                            Console.WriteLine($"\t{timer.ElapsedMilliseconds}\t\t{maxWork - toWork}/{maxWork}\t{toCheck}/{maxWork}");
                        }))
                        {
                            checker.Change(0, 5000);
                            try
                            {
                                var r = Parallel.ForEach(work, new ParallelOptions() { MaxDegreeOfParallelism = 1, CancellationToken = abortToken }, (w, s) =>
                                {
                                    if (abortToken.IsCancellationRequested)
                                    {
                                        Console.WriteLine("Foreach started despite abortToken set.");
                                    }
                                    CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                                    tokenSource.CancelAfter(300000);
                                    var cancellationToken = tokenSource.Token;
                                    if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                                    {
                                        Console.WriteLine("Cancellationtoken wrong");
                                    }
                                    w.CalculateByPattern(cancellationToken);
                                    Interlocked.Decrement(ref toCheck);
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        //Console.WriteLine($"{w.line}: {w.PermCount}");
                                        Interlocked.Add(ref sum, w.PermCount);
                                        Interlocked.Decrement(ref toWork);
                                        lock (interWriter)
                                        {
                                            interWriter.WriteLine($"{w.line}<=>{w.PermCount}");
                                        }
                                    }
                                    else
                                    {
                                        lock (notAccountedWriter)
                                        {
                                            notAccountedWriter.WriteLine($"{w.line}");
                                        }
                                    }
                                    //Console.WriteLine($"{toCheck}/{maxWork}\t{s.IsStopped} {s.ShouldExitCurrentIteration}");
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            Console.WriteLine("After ForEach");
                        }
                    }
                }
            }

            timer.Stop();
            Console.WriteLine($"Accounted for: {maxWork - toWork}/{maxWork}");
            Console.WriteLine($"Calculating: {timer.ElapsedMilliseconds}");

            Console.WriteLine(sum);
        }

        public static string RepeatWithSeparator(string i, int count, char separator)
        {
            return Enumerable.Range(0, count).Select(_ => i).Aggregate("", (a, b) => $"{a}{separator}{b}")[1..];
        }

        public class WorkPackage
        {
            public string line = string.Empty;
            public long PermCount = 0;

            public int GetComplexity()
            {
                return line.Where(c => c == '?').Count();
            }

            public void Calculate(CancellationToken cancellationToken)
            {
                //Console.WriteLine($"Calculate started {cancellationToken.IsCancellationRequested} {line}");
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1].Split(',').Select(c => int.Parse(c)).ToList();

                StringBuilder rb = new StringBuilder();
                rb.Append("^[.?]*");
                rb.AppendJoin("[.?]+", springGroups.Select((s, i) => $"(?'{i + 1}'[#?]{{{s}}})"));
                rb.Append("[.?]*$");
                var regExPattern = rb.ToString();

                var regEx = new Regex(regExPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(2000));

                var currentSpring = springs;

                var questionMarks = springs.Select((s, i) => s == '?' ? i : -1).Where(i => i >= 0).ToList();
                var counter = 1;
                while (counter < questionMarks.Count && counter < 3)
                {
                    CheckQuestionMarks(questionMarks, counter, regEx, ref currentSpring, cancellationToken);
                    counter++;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        //Console.WriteLine($"Operation cancelled [Calculate] {springs}");
                        return;
                    }
                }
                //CheckQuestionMarks(questionMarks, 1, regEx, ref currentSpring);
                //while (CheckQuestionMarks(questionMarks, 2, regEx, ref currentSpring)) ;

                this.PermCount = this.PermCount = CalculateIterator(currentSpring, 0, springGroups, 0, 0, false, springGroups.Sum() + springGroups.Count() - 1, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    //Console.WriteLine($"Operation cancelled [Calculate] {springs}");
                    return;
                }
                //Console.WriteLine($"{currentSpring}: {this.PermCount}");
            }

            private bool CheckQuestionMarks(List<int> questionMarks, int count, Regex regEx, ref string currentSpring, CancellationToken cancellationToken)
            {
                var removedQuestionMark = false;
                if (count == 1)
                {
                    for (int i = 0; i < questionMarks.Count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            //Console.WriteLine($"Operation cancelled [CheckQuestionMarks] {currentSpring}");
                            return false;
                        }
                        var index = questionMarks.ElementAt(i);
                        var nonSpringTest = currentSpring.Remove(index, 1).Insert(index, ".");
                        var springTest = currentSpring.Remove(index, 1).Insert(index, "#");

                        try
                        {
                            var nonSpringMatch = regEx.IsMatch(nonSpringTest);
                            var springMatch = regEx.IsMatch(springTest);

                            if (springMatch && nonSpringMatch)
                            {
                                continue;
                            }
                            else if (!springMatch && !nonSpringMatch)
                            {
                                Console.WriteLine($"No matching anymore. Should not happen?");
                            }
                            else
                            {
                                currentSpring = currentSpring.Remove(index, 1).Insert(index, springMatch ? "#" : ".");
                                questionMarks.RemoveAt(i);
                                i--;
                                removedQuestionMark = true;
                            }
                        }
                        catch (RegexMatchTimeoutException)
                        {
                            return false;
                        }
                    }
                    return removedQuestionMark;
                }

                var combinations = GetCombinations(questionMarks, count);
                foreach (var combination in combinations)
                {
                    var tempSpring = currentSpring;
                    // Combination contains indeces of questionMarks i need to check
                    if (combination.Any(qIndex => tempSpring[qIndex] != '?'))
                    {
                        // One of the questionMarks was already replaced => irrelevant combination
                        continue;
                    }

                    var combCount = combination.Count();
                    var iterations = Math.Pow(2, combCount);
                    List<int> matches = new();

                    for (int i = 0; i < iterations; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            //Console.WriteLine($"Operation cancelled [CheckQuestionMarks] {currentSpring}");
                            return false;
                        }
                        var checkSpring = currentSpring;
                        for (int j = 0; j < combCount; j++)
                        {
                            var qIndex = combination.ElementAt(j);
                            var bitChecker = (long)Math.Pow(2, j);
                            checkSpring = checkSpring.Remove(qIndex, 1).Insert(qIndex, (i & bitChecker) != 0 ? "#" : ".");
                        }
                        try
                        {
                            if (regEx.IsMatch(checkSpring))
                            {
                                matches.Add(i);
                            }
                        }
                        catch (RegexMatchTimeoutException)
                        {
                            return false;
                        }
                    }

                    if (matches.Count == 1)
                    {
                        var matchIteration = matches[0];
                        for (int j = 0; j < combCount; j++)
                        {
                            var qIndex = combination.ElementAt(j);
                            var bitChecker = (long)Math.Pow(2, j);
                            currentSpring = currentSpring.Remove(qIndex, 1).Insert(qIndex, (matchIteration & bitChecker) != 0 ? "#" : ".");
                        }
                        removedQuestionMark = true;
                    }

                }
                return removedQuestionMark;
            }

            private static IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> items, int count)
            {
                int i = 0;
                foreach (var item in items)
                {
                    if (count == 1)
                        yield return new T[] { item };
                    else
                    {
                        foreach (var result in GetCombinations(items.Skip(i + 1), count - 1))
                            yield return new T[] { item }.Concat(result);
                    }

                    ++i;
                }
            }

            private long CalculateIterator(string springs, int index, List<int> springGroups, int nextSpringGroup, int springsLeftInGroup, bool needEmpty, int minRemainingSymbols, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    //Console.WriteLine($"Operation cancelled [CalculateIterator] {springs} {index}");
                    return -1L;
                }
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

                    // No springGroup. Check if we have one
                    if (nextSpringGroup >= springGroups.Count)
                    {
                        return 0L;
                    }
                    return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);
                }
                return 0L;
            }

            public void CalculateByPattern(CancellationToken cancellationToken)
            {
                //Console.WriteLine($"Starting with {line}");
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1].Split(',').Select(c => int.Parse(c)).ToList();

                var numNeededCharacters = springGroups.Sum();
                var freeCharacters = springs.Length - numNeededCharacters;

                this.PermCount += GetNumberOfValidPermutations(springs, springGroups, 0, freeCharacters, true, cancellationToken);
            }

            private static IEnumerable<IEnumerable<int>> GetPermutations(int groupCount, int sum, bool isFirst)
            {
                if (groupCount == 1)
                {
                    yield return new List<int> { sum };
                }
                else
                {
                    var start = isFirst ? 0 : 1;
                    var end = sum - groupCount + 2;
                    for (int current = start; current <= end; current++)
                    {
                        var inner = GetPermutations(groupCount - 1, sum - current, false);
                        foreach (var g in inner)
                        {
                            yield return new List<int> { current }.Concat(g);
                        }

                    }
                }

            }

            private static long GetNumberOfValidPermutations(IEnumerable<char> currentSprings, List<int> springGroups, int currentGroup, int freeCharacters, bool isFirst, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return 0L;
                }
                //Console.WriteLine($"{String.Join("",currentSprings)} {currentGroup} {freeCharacters}");
                if (currentGroup == springGroups.Count)
                {
                    if (freeCharacters > 0)
                    {
                        var toCheck = currentSprings.Take(freeCharacters);
                        currentSprings = currentSprings.Skip(freeCharacters);
                        if (!toCheck.All(c => c == '.' || c == '?'))
                        {
                            return 0L;
                        }
                        if (currentSprings == null || currentSprings.Count() == 0)
                        {
                            return 1L;
                        }
                        return 0L;
                    }
                    else
                    {
                        if (currentSprings == null || currentSprings.Count() == 0)
                        {
                            return 1L;
                        }
                        return 0L;
                    }
                }

                // We have at least one group of springs left
                // if not isFirst the first char must be .

                if (currentSprings.All(c => c == '?'))
                {
                    // Only Questionmarks left => All remaining permutations valid
                    var remainingSpringGroups = springGroups.Count - currentGroup;

                    if (!isFirst)
                    {
                        var withOutLastZero = BinomialCoefficient(freeCharacters - 1, remainingSpringGroups);
                        var forLastZero = BinomialCoefficient(freeCharacters - 1, remainingSpringGroups - 1);
                        return withOutLastZero + forLastZero;
                    }
                    else
                    {
                        var withOutZero = BinomialCoefficient(freeCharacters - 1, springGroups.Count - currentGroup);
                        var forOneZero = BinomialCoefficient(freeCharacters - 1, springGroups.Count - currentGroup - 1);
                        var forBothZero = BinomialCoefficient(freeCharacters - 1, springGroups.Count - currentGroup - 2);
                        return withOutZero + forOneZero * 2 + forBothZero;
                    }
                }

                var start = isFirst ? 0 : 1;
                var end = freeCharacters - (springGroups.Count - currentGroup - 1);
                var numPerms = 0L;
                try
                {
                    if (isFirst)
                    {
                        Parallel.For(start, end + 1, new ParallelOptions() { MaxDegreeOfParallelism = 24, CancellationToken = cancellationToken }, (current) =>
                        {
                            var next = currentSprings;
                            if (current > 0)
                            {
                                var toCheck = next.Take(current);
                                next = next.Skip(current);
                                if (!toCheck.All(c => c == '.' || c == '?'))
                                {
                                    return;
                                }
                            }
                            if (currentGroup < springGroups.Count)
                            {
                                var toCheckForSpring = next.Take(springGroups[currentGroup]);
                                next = next.Skip(springGroups[currentGroup]);
                                if (!toCheckForSpring.All(c => c == '#' || c == '?'))
                                {
                                    return;
                                }
                            }
                            var currentPerms = GetNumberOfValidPermutations(next, springGroups, currentGroup + 1, freeCharacters - current, false, cancellationToken);

                            Interlocked.Add(ref numPerms, currentPerms);
                        });
                    }
                    else
                    {
                        for (int current = start; current <= end; current++)
                        {
                            var next = currentSprings;
                            if (current > 0)
                            {
                                var toCheck = next.Take(current);
                                next = next.Skip(current);
                                if (!toCheck.All(c => c == '.' || c == '?'))
                                {
                                    continue;
                                }
                            }
                            if (currentGroup < springGroups.Count)
                            {
                                var toCheckForSpring = next.Take(springGroups[currentGroup]);
                                next = next.Skip(springGroups[currentGroup]);
                                if (!toCheckForSpring.All(c => c == '#' || c == '?'))
                                {
                                    continue;
                                }
                            }
                            var currentPerms = GetNumberOfValidPermutations(next, springGroups, currentGroup + 1, freeCharacters - current, false, cancellationToken);

                            Interlocked.Add(ref numPerms, currentPerms);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.Message);
                }
                return numPerms;
            }

            public static long BinomialCoefficient(long n, long k)
            {
                var result = 1L;
                for (int i = 0; i < k; i++)
                {
                    result *= n - i;
                    result /= (i + 1);
                }
                return result;
                //var top = 1L;
                //var bottom = 1L;
                //for (long i = n; i > n - k; i--)
                //{
                //    top *= i;
                //}
                //for (long i = 1; i <= k; i++)
                //{
                //    bottom *= i;
                //}

                //return top / bottom;
            }
        }
    }
}