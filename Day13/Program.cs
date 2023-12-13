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
            var savedComputation = new Dictionary<string, string>();
            if (File.Exists("./Intermediate.txt"))
            {
                var intermediate = File.ReadLines("./Intermediate.txt");
                foreach (var line in intermediate)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var h = line.Split("<=>");
                        savedComputation.Add(h[0], h[1]);
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
            //lines = lines.Select(line =>
            //{
            //    var splitter = line.Split(' ');
            //    var springs = splitter[0];
            //    var springGroups = splitter[1];
            //    return $"{RepeatWithSeparator(springs, 5, '?')} {RepeatWithSeparator(springGroups, 5, ',')}";
            //});
            var currentWork = new WorkPackage();
            var work = new List<WorkPackage>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    work.Add(currentWork);
                    currentWork = new WorkPackage() { inputIndex = currentWork.inputIndex + 1 };
                }
                else
                {
                    currentWork.lines.Add(line);
                }
            }
            if (currentWork.lines.Count > 0)
            {
                work.Add(currentWork);
            }

            var maxWork = work.Count();
            var toWork = maxWork;

            var toCheck = toWork;

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
                                if (savedComputation.ContainsKey(w.inputIndex.ToString()))
                                {
                                    var result = savedComputation[w.inputIndex.ToString()];
                                    var split = result.Split(' ');
                                    w.PermCount = long.Parse(split[0]);
                                    w.IsHorizontal = bool.Parse(split[1]);
                                    Interlocked.Decrement(ref toCheck);
                                    Interlocked.Decrement(ref toWork);
                                    Interlocked.Add(ref sum, w.IsHorizontal ? 100 * w.PermCount : w.PermCount);
                                    return;
                                }
                                CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                                tokenSource.CancelAfter(100000000);
                                var cancellationToken = tokenSource.Token;
                                if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                                {
                                    Console.WriteLine("Cancellationtoken wrong");
                                }
                                w.CalculatePart2(cancellationToken);
                                Interlocked.Decrement(ref toCheck);
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    //Console.WriteLine($"{w.line}: {w.PermCount}");
                                    Interlocked.Add(ref sum, w.IsHorizontal ? 100 * w.PermCount : w.PermCount);
                                    Interlocked.Decrement(ref toWork);
                                    lock (interWriter)
                                    {
                                        interWriter.WriteLine($"{w.inputIndex.ToString()}<=>{w.PermCount} {w.IsHorizontal}");
                                    }
                                }
                                else
                                {
                                    lock (notAccountedWriter)
                                    {
                                        notAccountedWriter.WriteLine($"{w.inputIndex.ToString()}");
                                        foreach (var line in w.lines)
                                        {
                                            notAccountedWriter.WriteLine($"{line}");
                                        }
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

            foreach (var w in work)
            {
                Console.WriteLine($"{w.inputIndex} {w.IsHorizontal} {w.PermCount}");
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
            public List<string> lines = new List<string>();
            private char[,]? board = null;
            public int inputIndex = 0;
            public long PermCount = 0;
            public bool IsHorizontal = false;

            public void Calculate(CancellationToken cancellationToken)
            {
                InitializePackage();
                foreach (var hor in FindHorizontal(board, cancellationToken))
                {
                    IsHorizontal = true;
                    PermCount = hor + 1;
                    return;
                }
                foreach (var vert in FindVertical(board, cancellationToken))
                {
                    IsHorizontal = false;
                    PermCount = vert + 1;
                    return;
                }
                Console.WriteLine($"{inputIndex}: Could not find Mirror.");
            }

            public void CalculatePart2(CancellationToken cancellationToken)
            {
                Calculate(cancellationToken);
                var prevDir = IsHorizontal;
                var prevCount = PermCount;
                for (int i = 0; i < board.GetLength(0) * board.GetLength(1); i++)
                {
                    if (i > 0)
                    {
                        FlipElementAt(board, (i - 1) / board.GetLength(1), (i - 1) % board.GetLength(1));
                    }
                    FlipElementAt(board, (i) / board.GetLength(1), (i) % board.GetLength(1));
                    foreach (var hor in FindHorizontal(board, cancellationToken))
                    {
                        if (!prevDir || prevCount - 1 != hor)
                        {
                            IsHorizontal = true;
                            PermCount = hor + 1;
                            return;
                        }
                    }
                    foreach (var vert in FindVertical(board, cancellationToken))
                    {
                        if (prevDir || prevCount - 1 != vert)
                        {
                            IsHorizontal = false;
                            PermCount = vert + 1;
                            return;
                        }
                    }
                }
                Console.WriteLine($"{inputIndex}: Could not find Mirror.");
            }

            public void FlipElementAt(char[,] board, int i, int j)
            {
                if (board[i, j] == '.')
                {
                    board[i, j] = '#';
                }
                else
                {
                    board[i, j] = '.';
                }
            }

            private void InitializePackage()
            {
                board = new char[lines.Count, lines.ElementAt(0).Count()];
                for (int i = 0; i < lines.Count; i++)
                {
                    for (int j = 0; j < lines.ElementAt(0).Count(); j++)
                    {
                        board[i, j] = lines[i].ElementAt(j);
                    }
                }
            }

            private static IEnumerable<int> FindHorizontal(char[,] board, CancellationToken cancellationToken)
            {
                for (int i = 0; i < board.GetLength(0) - 1; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }
                    if (RowsIdentical(board, i, i + 1))
                    {
                        bool isMirror = true;
                        var checkLength = Math.Max(board.GetLength(0) - i - 1, i);
                        for (int j = 1; j <= checkLength; j++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                yield break;
                            }
                            if (i - j >= 0 && i + j + 1 < board.GetLength(0))
                            {
                                if (!RowsIdentical(board, i - j, i + j + 1))
                                {
                                    isMirror = false;
                                    break;
                                }
                            }
                        }
                        if (isMirror)
                        {
                            yield return i;
                            // To Skip next line it is the mirror from the opposite site
                            i++;
                        }
                    }
                }
                yield break;
            }

            private static IEnumerable<int> FindVertical(char[,] board, CancellationToken cancellationToken)
            {
                for (int i = 0; i < board.GetLength(1) - 1; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }
                    if (ColumnsIdentical(board, i, i + 1))
                    {
                        bool isMirror = true;
                        var checkLength = Math.Max(board.GetLength(1) - i - 1, i);
                        for (int j = 1; j <= checkLength; j++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                yield break;
                            }
                            if (i - j >= 0 && i + j + 1 < board.GetLength(1))
                            {
                                if (!ColumnsIdentical(board, i - j, i + j + 1))
                                {
                                    isMirror = false;
                                    break;
                                }
                            }
                        }
                        if (isMirror)
                        {
                            yield return i;
                            i++;
                        }
                    }
                }
                yield break;
            }

            private static bool RowsIdentical(char[,] board, int i, int j)
            {
                for (int a = 0; a < board.GetLength(1); a++)
                {
                    if (board[i, a] != board[j, a])
                    {
                        return false;
                    }
                }
                return true;
            }

            private static bool ColumnsIdentical(char[,] board, int i, int j)
            {
                for (int a = 0; a < board.GetLength(0); a++)
                {
                    if (board[a, i] != board[a, j])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}