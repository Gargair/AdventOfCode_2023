using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace Solution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var lines = File.ReadLines("./Input.txt");
            var savedComputation = new Dictionary<string, ulong>();
            if (File.Exists("./Intermediate.txt"))
            {
                var intermediate = File.ReadLines("./Intermediate.txt");
                foreach (var line in intermediate)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var h = line.Split("<=>");
                        savedComputation.Add(h[0], ulong.Parse(h[1]));
                    }
                }
            }
            if (File.Exists("./NotAccounted.txt"))
            {
                File.Delete("./NotAccounted.txt");
            }
            var sum = 0UL;
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

                using (var context = Context.CreateDefault())
                {
                    using (var accelerator = context.CreateCudaAccelerator(0))
                    {
                        var Starter = accelerator.LoadAutoGroupedStreamKernel<Index1D, SpecializedValue<ulong>, ArrayView<byte>, ArrayView<byte>, ArrayView<ulong>>(TestCombinations2);
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
                                            tokenSource.CancelAfter(10000);
                                            var cancellationToken = tokenSource.Token;
                                            if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                                            {
                                                Console.WriteLine("Cancellationtoken wrong");
                                            }
                                            w.CalculateGraphic(accelerator, Starter, 2000000000, cancellationToken);
                                            Interlocked.Decrement(ref toCheck);
                                            if (!cancellationToken.IsCancellationRequested && w.PermCount > 0)
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
                }
            }

            timer.Stop();
            Console.WriteLine($"Accounted for: {maxWork - toWork}/{maxWork}");
            Console.WriteLine($"Calculating: {timer.ElapsedMilliseconds}");

            Console.WriteLine(sum);
        }


        static void TestCombinations2(Index1D index, SpecializedValue<ulong> offset, ArrayView<byte> springs, ArrayView<byte> springGroups, ArrayView<ulong> solutions)
        {
            int groupNumber = 0;
            int currentGroup = 0;
            ulong currentQuestionMark = 1;
            ulong combination = offset + ((uint)index.X);
            for (int i = 0; i < springs.Length; i++)
            {
                int c = springs[i];
                if (c == 63)
                {
                    if ((combination & currentQuestionMark) != 0)
                    {
                        c = 35;
                    }
                    else
                    {
                        c = 46;
                    }
                    currentQuestionMark *= 2;
                }
                if (c == 46)
                {
                    if (currentGroup > 0)
                    {
                        if (groupNumber >= springGroups.Length)
                        {
                            return;
                        }
                        if (springGroups[groupNumber] != currentGroup)
                        {
                            return;
                        }
                        groupNumber++;
                        currentGroup = 0;
                    }
                }
                else if (c == 35)
                {
                    currentGroup++;
                }
            }
            if (currentGroup > 0)
            {
                if (groupNumber >= springGroups.Length)
                {
                    return;
                }
                if (springGroups[groupNumber] != currentGroup)
                {
                    return;
                }
                groupNumber++;
            }
            if (groupNumber != springGroups.Length)
            {
                return;
            }
            Atomic.Add(ref solutions[0], 1);
        }





        //static void TestCombinations(Index1D index, SpecializedValue<long> offset, ArrayView<byte> springs, ArrayView<byte> springGroups, ArrayView<byte> solutions)
        //{
        //    int groupNumber = 0;
        //    int currentGroup = 0;
        //    long currentQuestionMark = 1;
        //    long combination = offset + index;
        //    for (int i = 0; i < springs.Length; i++)
        //    {
        //        int c = springs[i];
        //        if (c == 63)
        //        {
        //            if ((combination & currentQuestionMark) != 0)
        //            {
        //                c = 35;
        //            }
        //            else
        //            {
        //                c = 46;
        //            }
        //            currentQuestionMark *= 2;
        //        }
        //        if (c == 46)
        //        {
        //            if (currentGroup > 0)
        //            {
        //                if (groupNumber >= springGroups.Length)
        //                {
        //                    solutions[index] = 0;
        //                    return;
        //                }
        //                if (springGroups[groupNumber] != currentGroup)
        //                {
        //                    solutions[index] = 0;
        //                    return;
        //                }
        //                groupNumber++;
        //                currentGroup = 0;
        //            }
        //        }
        //        else if (c == 35)
        //        {
        //            currentGroup++;
        //        }
        //    }
        //    if (currentGroup > 0)
        //    {
        //        if (groupNumber >= springGroups.Length)
        //        {
        //            solutions[index] = 0;
        //            return;
        //        }
        //        if (springGroups[groupNumber] != currentGroup)
        //        {
        //            solutions[index] = 0;
        //            return;
        //        }
        //        groupNumber++;
        //        currentGroup = 0;
        //    }
        //    if (groupNumber != springGroups.Length)
        //    {
        //        solutions[index] = 0;
        //        return;
        //    }
        //    solutions[index] = 1;
        //}

        public static string RepeatWithSeparator(string i, int count, char separator)
        {
            return Enumerable.Range(0, count).Select(_ => i).Aggregate("", (a, b) => $"{a}{separator}{b}")[1..];
        }

        public class WorkPackage
        {
            public string line = string.Empty;
            public ulong PermCount = 0;

            private static Regex BuildRegex(IEnumerable<byte> springGroups, double timeOutMillis = 2000)
            {
                StringBuilder rb = new StringBuilder();
                rb.Append("^[.?]*");
                rb.AppendJoin("[.?]+", springGroups.Select((s, i) => $"(?'{i + 1}'[#?]{{{s}}})"));
                rb.Append("[.?]*$");
                var regExPattern = rb.ToString();

                return new Regex(regExPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(timeOutMillis));
            }

            public void CalculateGraphic(Accelerator accelerator, Action<Index1D, SpecializedValue<ulong>, ArrayView<byte>, ArrayView<byte>, ArrayView<ulong>> Starter, uint chunkSize, CancellationToken cancellationToken)
            {
                var splitter = line.Split(' ');
                var springs = splitter[0];
                var springGroups = splitter[1].Split(',').Select(byte.Parse);

                if (springs.All(c => c == '?'))
                {
                    ulong freeCharacters = (ulong)springs.Length - (ulong)springGroups.Select(s => (int)s).Sum();
                    var withOutZero = BinomialCoefficient(freeCharacters, (ulong)springGroups.Count());
                    var forOneZero = BinomialCoefficient(freeCharacters - 1, (ulong)springGroups.Count() - 1UL);
                    var forBothZero = BinomialCoefficient(freeCharacters - 1, (ulong)springGroups.Count() - 2UL);
                    PermCount = withOutZero + forOneZero * 2 + forBothZero;
                    return;
                }

                var regEx = BuildRegex(springGroups, 200);

                var questionMarks = springs.Select((s, i) => s == '?' ? i : -1).Where(i => i >= 0).ToList();

                CheckQuestionMarks(questionMarks, regEx, ref springs, cancellationToken);

                var numQuestionMarks = springs.Where(c => c == '?').Count();

                if (numQuestionMarks <= 64)
                {
                    PermCount = BruteForceCombinations2(accelerator, Starter, chunkSize, springs, springGroups, cancellationToken);
                    return;
                }
                else
                {
                    Console.WriteLine($"{springs}: Too much.");
                }


                // Recursive ?

            }

            private ulong BruteForceCombinations2(Accelerator accelerator, Action<Index1D, SpecializedValue<ulong>, ArrayView<byte>, ArrayView<byte>, ArrayView<ulong>> Starter, uint chunkSize, string springs, IEnumerable<byte> springGroups, CancellationToken cancellationToken)
            {
                //Console.WriteLine($"Switching to GraphicCard.");
                var numQuestionMarks = springs.Where(c => c == '?').Count();
                var numCombinations = (ulong)Math.Pow(2, numQuestionMarks);

                var springs_arr = accelerator.Allocate1D<byte>(springs.Select(c => (byte)c).ToArray());
                var springGroups_arr = accelerator.Allocate1D<byte>(springGroups.ToArray());
                var solutions_arr = accelerator.Allocate1D<ulong>(1);
                var solutions = new ulong[1];
                ulong countSolutions = 0;

                for (ulong i = 0; i < numCombinations && i >= 0; i += chunkSize)
                {
                    if (cancellationToken.IsCancellationRequested) { break; }

                    var currentChunk = (int)Math.Min(chunkSize, numCombinations - i);
                    Starter(currentChunk, SpecializedValue.New(i), springs_arr.View, springGroups_arr.View, solutions_arr.View);

                    accelerator.Synchronize();
                    solutions_arr.CopyToCPU(solutions);

                    countSolutions += solutions[0];

                    //Console.WriteLine($"{springs}: {i + currentChunk}/{numCombinations} ({countSolutions})");
                }

                springs_arr.Dispose();
                springGroups_arr.Dispose();
                solutions_arr.Dispose();

                return countSolutions;
            }

            //private long BruteForceCombinations(Accelerator accelerator, Action<Index1D, SpecializedValue<long>, ArrayView<byte>, ArrayView<byte>, ArrayView<byte>> Starter, int chunkSize, string springs, IEnumerable<byte> springGroups, CancellationToken cancellationToken)
            //{
            //    //Console.WriteLine($"Switching to GraphicCard.");
            //    var numQuestionMarks = springs.Where(c => c == '?').Count();
            //    var numCombinations = (long)Math.Pow(2, numQuestionMarks);

            //    var springs_arr = accelerator.Allocate1D<byte>(springs.Select(c => (byte)c).ToArray());
            //    var springGroups_arr = accelerator.Allocate1D<byte>(springGroups.ToArray());
            //    var solutions_arr = accelerator.Allocate1D<byte>(chunkSize);
            //    var solutions = new byte[chunkSize];
            //    long countSolutions = 0;


            //    for (long i = 0; i < numCombinations && i >= 0; i += chunkSize)
            //    {
            //        if (cancellationToken.IsCancellationRequested) { break; }

            //        var currentChunk = (int)Math.Min(chunkSize, numCombinations - i);
            //        Starter(currentChunk, SpecializedValue.New(i), springs_arr.View, springGroups_arr.View, solutions_arr.View);

            //        accelerator.Synchronize();
            //        solutions_arr.CopyToCPU(solutions);

            //        countSolutions += solutions.Where((s, i) => s == 1 && i < currentChunk).Count();

            //        //Console.WriteLine($"{springs}: {i + currentChunk}/{numCombinations} ({countSolutions})");
            //    }

            //    springs_arr.Dispose();
            //    springGroups_arr.Dispose();
            //    solutions_arr.Dispose();

            //    return countSolutions;
            //}

            private void CheckQuestionMarks(List<int> questionMarks, Regex regEx, ref string currentSpring, CancellationToken cancellationToken)
            {
                for (int i = 0; i < questionMarks.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        //Console.WriteLine($"Operation cancelled [CheckQuestionMarks] {currentSpring}");
                        return;
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
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                    }
                }
            }


            //private long CalculateIterator(string springs, int index, List<int> springGroups, int nextSpringGroup, int springsLeftInGroup, bool needEmpty, int minRemainingSymbols, CancellationToken cancellationToken)
            //{
            //    if (cancellationToken.IsCancellationRequested)
            //    {
            //        //Console.WriteLine($"Operation cancelled [CalculateIterator] {springs} {index}");
            //        return -1L;
            //    }
            //    if (index == springs.Length)
            //    {
            //        if (springsLeftInGroup == 0 && nextSpringGroup == springGroups.Count)
            //        {
            //            return 1L;
            //        }
            //        return 0L;
            //    }
            //    if (springs.Length - index < minRemainingSymbols)
            //    {
            //        return 0L;
            //    }
            //    var curChar = springs[index];
            //    if (curChar == '?')
            //    {
            //        if (needEmpty)
            //        {
            //            // We need a non spring (spearator for springgroup)
            //            return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup, false, minRemainingSymbols - 1, cancellationToken);
            //        }
            //        if (springsLeftInGroup > 0)
            //        {
            //            // We are in a spring group. We need a spring
            //            return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);
            //        }
            //        if (nextSpringGroup >= springGroups.Count)
            //        {
            //            // We have no springgroups for springs left
            //            return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols, cancellationToken);
            //        }

            //        var nonSpringCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, minRemainingSymbols, cancellationToken);
            //        var springCount = CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);

            //        return nonSpringCount + springCount;
            //    }
            //    else if (curChar == '.')
            //    {
            //        // I got a non spring
            //        if (springsLeftInGroup > 0)
            //        {
            //            // But i should get a spring => No solution
            //            return 0L;
            //        }
            //        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, 0, false, needEmpty ? minRemainingSymbols - 1 : minRemainingSymbols, cancellationToken);
            //    }
            //    else if (curChar == '#')
            //    {
            //        // I got a spring
            //        if (springsLeftInGroup > 0)
            //        {
            //            // I am in a springgroup
            //            return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup, springsLeftInGroup - 1, springsLeftInGroup == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);
            //        }
            //        // But we need a non spring (separator for springgroup)
            //        if (needEmpty)
            //        {
            //            return 0L;
            //        }

            //        // No springGroup. Check if we have one
            //        if (nextSpringGroup >= springGroups.Count)
            //        {
            //            return 0L;
            //        }
            //        return CalculateIterator(springs, index + 1, springGroups, nextSpringGroup + 1, springGroups[nextSpringGroup] - 1, springGroups[nextSpringGroup] == 1 ? true : false, minRemainingSymbols - 1, cancellationToken);
            //    }
            //    return 0L;
            //}


            //private static IEnumerable<IEnumerable<int>> GetPermutations(int groupCount, int sum, bool isFirst)
            //{
            //    if (groupCount == 1)
            //    {
            //        yield return new List<int> { sum };
            //    }
            //    else
            //    {
            //        var start = isFirst ? 0 : 1;
            //        var end = sum - groupCount + 2;
            //        for (int current = start; current <= end; current++)
            //        {
            //            var inner = GetPermutations(groupCount - 1, sum - current, false);
            //            foreach (var g in inner)
            //            {
            //                yield return new List<int> { current }.Concat(g);
            //            }

            //        }
            //    }
            //}

            //private static long GetNumberOfValidPermutations(IEnumerable<char> currentSprings, List<int> springGroups, int currentGroup, int freeCharacters, bool isFirst, CancellationToken cancellationToken)
            //{
            //    if (cancellationToken.IsCancellationRequested)
            //    {
            //        return 0L;
            //    }
            //    //Console.WriteLine($"{String.Join("",currentSprings)} {currentGroup} {freeCharacters}");
            //    if (currentGroup == springGroups.Count)
            //    {
            //        if (freeCharacters > 0)
            //        {
            //            var toCheck = currentSprings.Take(freeCharacters);
            //            currentSprings = currentSprings.Skip(freeCharacters);
            //            if (!toCheck.All(c => c == '.' || c == '?'))
            //            {
            //                return 0L;
            //            }
            //            if (currentSprings == null || currentSprings.Count() == 0)
            //            {
            //                return 1L;
            //            }
            //            return 0L;
            //        }
            //        else
            //        {
            //            if (currentSprings == null || currentSprings.Count() == 0)
            //            {
            //                return 1L;
            //            }
            //            return 0L;
            //        }
            //    }

            //    // We have at least one group of springs left
            //    // if not isFirst the first char must be .

            //    if (currentSprings.All(c => c == '?'))
            //    {
            //        // Only Questionmarks left => All remaining permutations valid
            //        var remainingSpringGroups = springGroups.Count - currentGroup;

            //        if (!isFirst)
            //        {
            //            var withOutLastZero = BinomialCoefficient(freeCharacters - 1, remainingSpringGroups);
            //            var forLastZero = BinomialCoefficient(freeCharacters - 1, remainingSpringGroups - 1);
            //            return withOutLastZero + forLastZero;
            //        }
            //        else
            //        {
            //            var withOutZero = BinomialCoefficient(freeCharacters - 1, springGroups.Count - currentGroup);
            //            var forOneZero = BinomialCoefficient(freeCharacters - 1, springGroups.Count - currentGroup - 1);
            //            var forBothZero = BinomialCoefficient(freeCharacters - 1, springGroups.Count - currentGroup - 2);
            //            return withOutZero + forOneZero * 2 + forBothZero;
            //        }
            //    }

            //    var start = isFirst ? 0 : 1;
            //    var end = freeCharacters - (springGroups.Count - currentGroup - 1);
            //    var numPerms = 0L;
            //    try
            //    {
            //        if (isFirst)
            //        {
            //            Parallel.For(start, end + 1, new ParallelOptions() { MaxDegreeOfParallelism = 24, CancellationToken = cancellationToken }, (current) =>
            //            {
            //                var next = currentSprings;
            //                if (current > 0)
            //                {
            //                    var toCheck = next.Take(current);
            //                    next = next.Skip(current);
            //                    if (!toCheck.All(c => c == '.' || c == '?'))
            //                    {
            //                        return;
            //                    }
            //                }
            //                if (currentGroup < springGroups.Count)
            //                {
            //                    var toCheckForSpring = next.Take(springGroups[currentGroup]);
            //                    next = next.Skip(springGroups[currentGroup]);
            //                    if (!toCheckForSpring.All(c => c == '#' || c == '?'))
            //                    {
            //                        return;
            //                    }
            //                }
            //                var currentPerms = GetNumberOfValidPermutations(next, springGroups, currentGroup + 1, freeCharacters - current, false, cancellationToken);

            //                Interlocked.Add(ref numPerms, currentPerms);
            //            });
            //        }
            //        else
            //        {
            //            for (int current = start; current <= end; current++)
            //            {
            //                var next = currentSprings;
            //                if (current > 0)
            //                {
            //                    var toCheck = next.Take(current);
            //                    next = next.Skip(current);
            //                    if (!toCheck.All(c => c == '.' || c == '?'))
            //                    {
            //                        continue;
            //                    }
            //                }
            //                if (currentGroup < springGroups.Count)
            //                {
            //                    var toCheckForSpring = next.Take(springGroups[currentGroup]);
            //                    next = next.Skip(springGroups[currentGroup]);
            //                    if (!toCheckForSpring.All(c => c == '#' || c == '?'))
            //                    {
            //                        continue;
            //                    }
            //                }
            //                var currentPerms = GetNumberOfValidPermutations(next, springGroups, currentGroup + 1, freeCharacters - current, false, cancellationToken);

            //                Interlocked.Add(ref numPerms, currentPerms);
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        //Console.WriteLine(ex.Message);
            //    }
            //    return numPerms;
            //}

            public static ulong BinomialCoefficient(ulong n, ulong k)
            {
                var result = 1UL;
                for (uint i = 0; i < k; i++)
                {
                    result *= n - i;
                    result /= (i + 1);
                }
                return result;
            }
        }
    }
}