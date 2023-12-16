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
            //if (File.Exists("./Intermediate.txt"))
            //{
            //    var intermediate = File.ReadLines("./Intermediate.txt");
            //    foreach (var line in intermediate)
            //    {
            //        if (!string.IsNullOrWhiteSpace(line))
            //        {
            //            var h = line.Split("<=>");
            //            savedComputation.Add(h[0], h[1]);
            //        }
            //    }
            //}
            //if (File.Exists("./NotAccounted.txt"))
            //{
            //    File.Delete("./NotAccounted.txt");
            //}
            var sum = 0L;
            CancellationTokenSource threadAbort = new();
            Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                threadAbort.Cancel();
                Console.WriteLine("Cancellation requested");
            };

            var abortToken = threadAbort.Token;

            char[,] inputBoard = new char[lines.Count(), lines.ElementAt(0).Length];


            int i = 0;
            int j = 0;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    j = 0;
                    foreach (var c in line)
                    {
                        inputBoard[i, j] = c;
                        j++;
                    }
                }
                i++;
            }
            List<WorkPackage> work = new List<WorkPackage>();

            for (int a = 0; a < inputBoard.GetLength(0); a++)
            {
                var copyWork = new WorkPackage();
                copyWork.board = CopyBoard(inputBoard);
                copyWork.startAt = Tuple.Create(a, -1, Direction.East);
                work.Add(copyWork);
                copyWork = new WorkPackage();
                copyWork.board = CopyBoard(inputBoard);
                copyWork.startAt = Tuple.Create(a, inputBoard.GetLength(1), Direction.West);
                work.Add(copyWork);
            }
            for (int a = 0; a < inputBoard.GetLength(1); a++)
            {
                var copyWork = new WorkPackage();
                copyWork.board = CopyBoard(inputBoard);
                copyWork.startAt = Tuple.Create(-1, a, Direction.South);
                work.Add(copyWork);
                copyWork = new WorkPackage();
                copyWork.board = CopyBoard(inputBoard);
                copyWork.startAt = Tuple.Create(inputBoard.GetLength(0), a, Direction.North);
                work.Add(copyWork);
            }


            var maxWork = work.Count();
            var toWork = maxWork;
            var toCheck = toWork;

            using (Timer checker = new((_) =>
            {
                Console.WriteLine($"\t{timer.ElapsedMilliseconds}\t\t{maxWork - toWork}/{maxWork}\t{toCheck}/{maxWork}");
            }))
            {
                checker.Change(0, 5000);
                try
                {
                    var r = Parallel.ForEach(work, new ParallelOptions() { MaxDegreeOfParallelism = 12, CancellationToken = abortToken }, (w, s) =>
                    {
                        if (abortToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Foreach started despite abortToken set.");
                        }
                        CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                        //tokenSource.CancelAfter(100000000);
                        var cancellationToken = tokenSource.Token;
                        if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Cancellationtoken wrong");
                        }
                        w.Calculate(cancellationToken);
                        Interlocked.Decrement(ref toCheck);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Interlocked.Decrement(ref toWork);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.WriteLine("After ForEach");
            }

            var max = work.Select(w => w.Points).Max();

            timer.Stop();
            Console.WriteLine($"Calculating: {timer.ElapsedMilliseconds}");

            Console.WriteLine(max);
        }

        public static char[,] CopyBoard(char[,] board)
        {
            var copyBoard = new char[board.GetLength(0), board.GetLength(1)];
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    copyBoard[i, j] = board[i, j];
                }
            }
            return copyBoard;
        }

        public enum Direction
        {
            North,
            East,
            South,
            West
        }

        public class WorkPackage
        {
            public char[,] board;
            public bool[,] energized;
            public Tuple<int, int, Direction> startAt;
            public Queue<Tuple<int, int, Direction>> currentBeams = new Queue<Tuple<int, int, Direction>>();
            public List<Tuple<int, int, Direction>> check = new List<Tuple<int, int, Direction>>();
            public long Points = 0;

            public void Calculate(CancellationToken cancellationToken)
            {
                Points = 0;
                energized = new bool[board.GetLength(0), board.GetLength(1)];
                currentBeams.Enqueue(startAt);
                //energized[startAt.Item1, startAt.Item2] = true;
                while (currentBeams.Count > 0)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    var currentBeam = currentBeams.Dequeue();
                    if (check.Contains(currentBeam))
                    {
                        continue;
                    }
                    check.Add(currentBeam);
                    //for (int i = 0; i < energized.GetLength(0); i++)
                    //{
                    //    for (int j = 0; j < energized.GetLength(1); j++)
                    //    {
                    //        Console.Write(energized[i, j] ? '#' : '.');
                    //    }
                    //    Console.WriteLine();
                    //}
                    //Console.WriteLine();
                    (var posi, var posj) = GetPosToCheck(currentBeam);
                    if (posi >= 0 && posi < board.GetLength(0) && posj >= 0 && posj < board.GetLength(1))
                    {
                        energized[posi, posj] = true;
                        if (board[posi, posj] == '.')
                        {
                            currentBeams.Enqueue(Tuple.Create(posi, posj, currentBeam.Item3));
                        }
                        else if (board[posi, posj] == '|')
                        {
                            if (currentBeam.Item3 == Direction.North || currentBeam.Item3 == Direction.South)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, currentBeam.Item3));
                            }
                            else if (currentBeam.Item3 == Direction.East || currentBeam.Item3 == Direction.West)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.North));
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.South));
                            }
                        }
                        else if (board[posi, posj] == '-')
                        {
                            if (currentBeam.Item3 == Direction.East || currentBeam.Item3 == Direction.West)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, currentBeam.Item3));
                            }
                            else if (currentBeam.Item3 == Direction.North || currentBeam.Item3 == Direction.South)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.East));
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.West));
                            }
                        }
                        else if (board[posi, posj] == '/')
                        {
                            if (currentBeam.Item3 == Direction.North)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.East));
                            }
                            else if (currentBeam.Item3 == Direction.East)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.North));
                            }
                            else if (currentBeam.Item3 == Direction.South)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.West));
                            }
                            else if (currentBeam.Item3 == Direction.West)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.South));
                            }
                        }
                        else if (board[posi, posj] == '\\')
                        {
                            if (currentBeam.Item3 == Direction.North)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.West));
                            }
                            else if (currentBeam.Item3 == Direction.East)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.South));
                            }
                            else if (currentBeam.Item3 == Direction.South)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.East));
                            }
                            else if (currentBeam.Item3 == Direction.West)
                            {
                                currentBeams.Enqueue(Tuple.Create(posi, posj, Direction.North));
                            }
                        }
                    }
                }
                //for (int i = 0; i < energized.GetLength(0); i++)
                //{
                //    for (int j = 0; j < energized.GetLength(1); j++)
                //    {
                //        Console.Write(energized[i, j] ? '#' : '.');
                //    }
                //    Console.WriteLine();
                //}
                //Console.WriteLine();
                foreach (var tile in energized)
                {
                    if (tile)
                    {
                        Points++;
                    }
                }
            }

            public static (int, int) GetPosToCheck(Tuple<int, int, Direction> input)
            {
                switch (input.Item3)
                {
                    case Direction.North:
                        return (input.Item1 - 1, input.Item2);
                    case Direction.East:
                        return (input.Item1, input.Item2 + 1);
                    case Direction.South:
                        return (input.Item1 + 1, input.Item2);
                    case Direction.West:
                        return (input.Item1, input.Item2 - 1);
                    default:
                        return (input.Item1, input.Item2);
                }
            }

            public void CalculatePart2(CancellationToken cancellationToken)
            {

            }
        }
    }
}