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

            CancellationTokenSource threadAbort = new();
            Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                threadAbort.Cancel();
                Console.WriteLine("Cancellation requested");
            };

            var abortToken = threadAbort.Token;


            var currentWork = new WorkPackage();
            currentWork.board = new char[lines.Count(), lines.ElementAt(0).Length];

            int i = 0;
            int j = 0;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    j = 0;
                    foreach (var c in line)
                    {
                        currentWork.board[i, j] = c;
                        j++;
                    }
                }
                i++;
            }

            try
            {
                if (abortToken.IsCancellationRequested)
                {
                    Console.WriteLine("Foreach started despite abortToken set.");
                }
                CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                tokenSource.CancelAfter(100000000);
                var cancellationToken = tokenSource.Token;
                if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellationtoken wrong");
                }
                currentWork.CalculatePart2(true, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine(currentWork.Points);



            timer.Stop();
            Console.WriteLine($"Calculating: {timer.ElapsedMilliseconds}");
        }

        public static string RepeatWithSeparator(string i, int count, char separator)
        {
            return Enumerable.Range(0, count).Select(_ => i).Aggregate("", (a, b) => $"{a}{separator}{b}")[1..];
        }

        public class WorkPackage
        {
            public char[,]? board = null;
            public long Points = 0L;

            public void Calculate(bool withParallel, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                PrintBoard(board);
                Console.WriteLine();
                PushNorth(withParallel);
                PrintBoard(board);
                Points = CalculateWeight();
            }

            public void CalculatePart2(bool withParallel, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                PrintBoard(board);
                Console.WriteLine();
                List<Tuple<int, int>>[] rockPositions = new List<Tuple<int, int>>[1000000000];
                for (int i = 0; i < 1000000000; i++)
                {
                    if (i % 100000 == 0)
                    {
                        Console.WriteLine($"{i,10}/{1000000000}");
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    Cycle(withParallel);

                    var currentPos = GetRockPositions();
                    rockPositions[i] = currentPos;

                    for (int t = 0; t < i; t++)
                    {
                        var oldPosition = rockPositions[t];
                        if (oldPosition.Except(currentPos).Count() == 0 && currentPos.Except(oldPosition).Count() == 0)
                        {
                            var cycleLength = i - t;
                            var itersLeft = 1000000000 - i;
                            var fullCyclesPossible = itersLeft / cycleLength;
                            var itersInCycle = itersLeft - fullCyclesPossible * cycleLength;
                            Console.WriteLine($"Found cycle at {i} starting on {t}: Length {cycleLength}. Iterations left: {itersLeft}. Skipping {fullCyclesPossible} cycles.");
                            Console.WriteLine($"Iterations in cycle left: {itersInCycle}");
                            var endIteration = rockPositions[t + itersInCycle - 1];
                            Points = endIteration.Select(tup => board.GetLength(0) - tup.Item1).Sum();
                            return;
                        }
                    }

                }
                Points = CalculateWeight();
            }

            public void Cycle(bool withParallel)
            {
                PushNorth(withParallel);
                //PrintBoard(board);
                //Console.WriteLine();
                PushWest(withParallel);
                //PrintBoard(board);
                //Console.WriteLine();
                PushSouth(withParallel);
                //PrintBoard(board);
                //Console.WriteLine();
                PushEast(withParallel);
            }

            public long CalculateWeight()
            {
                var sum = 0L;
                for (int i = 0; i < board.GetLength(0); i++)
                {
                    var curPoints = board.GetLength(0) - i;
                    for (int j = 0; j < board.GetLength(1); j++)
                    {
                        if (board[i, j] == 'O')
                        {
                            sum += curPoints;
                        }
                    }
                }
                return sum;
            }

            private static ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 4 };

            private void PushNorth(bool withParallel)
            {
                if (withParallel)
                {
                    Parallel.For(0, board.GetLength(1), parallelOptions, (j) =>
                    {
                        var lastFixed = -1;
                        for (int i = 0; i < board.GetLength(0); i++)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (i != lastFixed + 1)
                                {
                                    board[lastFixed + 1, j] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed++;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = i;
                            }
                        }
                    });
                }
                else
                {
                    for (int j = 0; j < board.GetLength(1); j++)
                    {
                        var lastFixed = -1;
                        for (int i = 0; i < board.GetLength(0); i++)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (i != lastFixed + 1)
                                {
                                    board[lastFixed + 1, j] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed++;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = i;
                            }
                        }
                    }
                }
            }

            private void PushWest(bool withParallel)
            {
                if (withParallel)
                {
                    Parallel.For(0, board.GetLength(0), parallelOptions, i =>
                    {
                        var lastFixed = -1;
                        for (int j = 0; j < board.GetLength(1); j++)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (j != lastFixed + 1)
                                {
                                    board[i, lastFixed + 1] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed++;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = j;
                            }
                        }
                    });
                }
                else
                {
                    for (int i = 0; i < board.GetLength(0); i++)
                    {
                        var lastFixed = -1;
                        for (int j = 0; j < board.GetLength(1); j++)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (j != lastFixed + 1)
                                {
                                    board[i, lastFixed + 1] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed++;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = j;
                            }
                        }
                    }
                }
            }

            private void PushSouth(bool withParallel)
            {
                if (withParallel)
                {
                    Parallel.For(0, board.GetLength(1), parallelOptions, j =>
                    {
                        var lastFixed = board.GetLength(0);
                        for (int i = board.GetLength(0) - 1; i >= 0; i--)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (i != lastFixed - 1)
                                {
                                    board[lastFixed - 1, j] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed--;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = i;
                            }
                        }
                    });
                }
                else
                {
                    for (int j = 0; j < board.GetLength(1); j++)
                    {

                        var lastFixed = board.GetLength(0);
                        for (int i = board.GetLength(0) - 1; i >= 0; i--)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (i != lastFixed - 1)
                                {
                                    board[lastFixed - 1, j] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed--;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = i;
                            }
                        }
                    }
                }
            }

            private void PushEast(bool withParallel)
            {
                if (withParallel)
                {
                    Parallel.For(0, board.GetLength(0), parallelOptions, i =>
                    {
                        var lastFixed = board.GetLength(1);
                        for (int j = board.GetLength(1) - 1; j >= 0; j--)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (j != lastFixed - 1)
                                {
                                    board[i, lastFixed - 1] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed--;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = j;
                            }
                        }
                    });
                }
                else
                {
                    for (int i = 0; i < board.GetLength(0); i++)
                    {
                        var lastFixed = board.GetLength(1);
                        for (int j = board.GetLength(1) - 1; j >= 0; j--)
                        {
                            if (board[i, j] == 'O')
                            {
                                if (j != lastFixed - 1)
                                {
                                    board[i, lastFixed - 1] = 'O';
                                    board[i, j] = '.';
                                }
                                lastFixed--;
                            }
                            else if (board[i, j] == '#')
                            {
                                lastFixed = j;
                            }
                        }
                    }
                }
            }

            private static void PrintBoard(char[,] board)
            {
                for (int i = 0; i < board.GetLength(0); i++)
                {
                    for (int j = 0; j < board.GetLength(1); j++)
                    {
                        Console.Write(board[i, j]);

                    }
                    Console.WriteLine();
                }
            }

            public List<Tuple<int, int>> GetRockPositions()
            {
                var r = new List<Tuple<int, int>>();
                for (int i = 0; i < board.GetLength(0); i++)
                {
                    for (int j = 0; j < board.GetLength(1); j++)
                    {
                        if (board[i, j] == 'O')
                        {
                            r.Add(new Tuple<int, int>(i, j));
                        }
                    }
                }
                return r;
            }
        }
    }
}