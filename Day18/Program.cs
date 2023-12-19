using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

            var tracer = new ConsoleTracer(timer);

            var abortToken = threadAbort.Token;
            var res = 0L;

            using (Timer checker = new((_) =>
            {
                //Console.WriteLine($"\t{timer.ElapsedMilliseconds}");
            }))
            {
                checker.Change(0, 5000);
                if (abortToken.IsCancellationRequested)
                {
                    Console.WriteLine("Foreach started despite abortToken set.");
                }
                CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                //tokenSource.CancelAfter(10000);
                var cancellationToken = tokenSource.Token;
                if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellationtoken wrong");
                }
                res = Calculate(lines, tracer, cancellationToken);
            }
            timer.Stop();
            Console.WriteLine($"Calculating: {timer.Elapsed}");

            Console.WriteLine(res);
        }

        public enum Direction
        {
            Up = 1,
            Right = 2,
            Bottom = 4,
            Left = 8,
            Unknown = 16
        }

        public static long Calculate(IEnumerable<string> lines, ITracer tracer, CancellationToken cancellationToken)
        {
            var path = GetPath(lines.Select(l =>
            {
                var ins = l.Split(' ');
                Direction d = GetDirection(ins[0]);
                var length = long.Parse(ins[1]);
                var color = ins[2];
                return (d, length, color);
            })).ToList();
            var minX = path.MinBy(e => e.Item1).Item1;
            var minY = path.MinBy(e => e.Item2).Item2;
            var maxX = path.MaxBy(e => e.Item1).Item1;
            var maxY = path.MaxBy(e => e.Item2).Item2;

            using (var sw = new StreamWriter("./Path.txt"))
            {
                foreach (var p in path)
                {
                    sw.WriteLine(p.ToString());
                }
            }

            var count = 0L;
            var linesDone = 0L;

            using (Timer checker = new((_) =>
            {
                tracer.WriteLine($"{linesDone,10}/{maxX - minX + 1}: {count}");
            }))
            {
                checker.Change(0, 5000);
                Parallel.For(minX, maxX + 1, new ParallelOptions() { MaxDegreeOfParallelism = 16, CancellationToken = cancellationToken }, (x) =>
                {
                    var t = CalcLine(x, path);
                    Interlocked.Add(ref count, t);
                    Interlocked.Increment(ref linesDone);
                });
            }

            //for (long x = minX; x < maxX; x++)
            //{
            //    tracer.WriteLine($"Line: {x,5} ({minX}=>{maxX}) {count}");
            //    for (long y = minY; y < maxY; y++)
            //    {
            //        if (IntersectWithPath(x, y, path))
            //        {
            //            isInside = !isInside;
            //        }
            //        if (isInside)
            //        {
            //            count++;
            //        }
            //    }
            //}

            //var offsetX = 0L;
            //var offsetY = 0L;
            //if (minX < 0)
            //{
            //    offsetX = Math.Abs(minX);
            //}
            //if (minY < 0)
            //{
            //    offsetY = Math.Abs(minY);
            //}
            //(long x, long y) offset = (offsetX, offsetY);
            //(long x, long y) boardMax = (maxX + offset.x + 1, maxY + offset.y + 1);

            //var numBoardX = (int)Math.Ceiling(boardMax.x / (double)MaxBoardSize);
            //var numBoardY = (int)Math.Ceiling(boardMax.y / (double)MaxBoardSize);

            //tracer.WriteLine($"Board size: {numBoardX}/{numBoardY}");

            //var boards = new BoardMeta[numBoardX, numBoardY];

            //for (int i = 0; i < numBoardX; i++)
            //{
            //    for (int j = 0; j < numBoardY; j++)
            //    {
            //        var boardMeta = new BoardMeta();
            //        var boardSizeX = (i + 1) * MaxBoardSize > boardMax.x ? boardMax.x - i * MaxBoardSize : MaxBoardSize;
            //        var boardSizeY = (j + 1) * MaxBoardSize > boardMax.y ? boardMax.y - j * MaxBoardSize : MaxBoardSize;

            //        if (i == 0)
            //        {
            //            for (long a = 0; a < boardSizeY; a++)
            //            {
            //                boardMeta.outSideTop.Add(a);
            //            }
            //        }
            //        if (i + 1 == numBoardX)
            //        {
            //            for (long a = 0; a < boardSizeY; a++)
            //            {
            //                boardMeta.outSideBottom.Add(a);
            //            }
            //        }
            //        if (j == 0)
            //        {
            //            for (long a = 0; a < boardSizeX; a++)
            //            {
            //                boardMeta.outSideLeft.Add(a);
            //            }
            //        }
            //        if (j + 1 == numBoardY)
            //        {
            //            for (long a = 0; a < boardSizeX; a++)
            //            {
            //                boardMeta.outSideRight.Add(a);
            //            }
            //        }
            //        boards[i, j] = boardMeta;
            //    }
            //}

            //var toDo = true;
            //var round = 1L;
            //while (toDo)
            //{
            //    for (int i = 0; i < numBoardX; i++)
            //    {
            //        for (int j = 0; j < numBoardY; j++)
            //        {
            //            var boardMeta = boards[i, j];
            //            if (boardMeta.isClean)
            //            {
            //                continue;
            //            }
            //            tracer.Write($"Round {round,3}: Checking {i,3} ({numBoardX,3})/{j,3} ({numBoardY,3}): ");
            //            var board = GetBoardAt((i, j), boardMax, path, offset);
            //            tracer.Write("Got Board ");
            //            CheckBoard(board, (i, j), boardMeta, boards, tracer);
            //            tracer.WriteLine($"Done");
            //        }
            //    }
            //    toDo = false;
            //    var countDone = 0L;
            //    for (int i = 0; i < numBoardX && !toDo; i++)
            //    {
            //        for (int j = 0; j < numBoardY; j++)
            //        {
            //            var boardMeta = boards[i, j];
            //            if (!boardMeta.isClean)
            //            {
            //                toDo = true;
            //                countDone++;
            //            }
            //        }
            //    }
            //    tracer.WriteLine($"Round {round++,3}: {countDone}/{numBoardX * numBoardY}");
            //}

            //long countOutside = 0L;

            //for (int i = 0; i < numBoardX; i++)
            //{
            //    for (int j = 0; j < numBoardY; j++)
            //    {
            //        var boardMeta = boards[i, j];
            //        countOutside += boardMeta.numOutside;
            //    }
            //}

            //long count = boardMax.x * boardMax.y - countOutside;

            return count;

        }

        private static long CalcLine(long x, long minY, long maxY, List<(long, long)> path)
        {
            bool isInside = false;
            long count = 0L;
            for (long y = minY; y < maxY; y++)
            {
                if (IntersectWithPath(x, y, path))
                {
                    isInside = !isInside;
                }
                if (isInside)
                {
                    count++;
                }
            }
            return count;
        }

        private static Direction GetUpBottomDirection(Direction d)
        {
            if ((d & Direction.Up) != 0)
            {
                return Direction.Up;
            }
            if ((d & Direction.Bottom) != 0)
            {
                return Direction.Bottom;
            }
            throw new Exception($"Direction without Up/Bottom. {d}");
        }

        private static long CalcLine(long x, List<(long, long)> path)
        {
            var p = GetPathElements(x, path);
            //if (p.Count % 2 != 0)
            //{
            //    throw new Exception($"Invalid Pathelements at {x}: {p.Count}");
            //}
            var count = 0L;
            bool isInside = false;
            for (int i = 0; i < p.Count; i++)
            {
                if (i == 0)
                {
                    if (p[i].Item2 == 0)
                    {
                        isInside = !isInside;
                    }
                    continue;
                }
                var last = p[i - 1];
                var el = p[i];
                if (last.Item2 != 0 && last.Item2 == el.Item2) // Last and current are same horizontal => Part of path
                {
                    var diff = el.Item1 - last.Item1;
                    count += diff;
                    var newDir = GetUpBottomDirection(el.Item3);
                    var lastDir = GetUpBottomDirection(last.Item3);
                    if(isInside)
                    {
                        count--;
                    }
                    if (newDir == lastDir)
                    {
                        isInside = !isInside;
                    }
                    if (!isInside) // Why when inside?
                    {
                        count ++;
                    }
                }
                else
                {
                    if (isInside)
                    {
                        var diff = el.Item1 - last.Item1 + 1;
                        count += diff;
                    }
                    if (el.Item2 == 0)
                    {
                        isInside = !isInside;
                    }
                }
            }
            if (isInside)
            {
                throw new Exception($"Finished with Inside: {x}");
            }
            //return p.Chunk(2).Select(t => t[1] - t[0] + 1).Sum();
            return count;
        }

        //private static List<long> GetPathElements(long x, List<(long, long)> path)
        private static List<(long, int, Direction)> GetPathElements(long x, List<(long, long)> path)
        {
            (long, long)? current = null;
            //List<(long, Direction)> result = new List<(long, Direction)>();
            List<(long, int, Direction)> result = new();
            int numHorizontal = 1;
            foreach (var edge in path)
            {
                if (current == null)
                {
                    current = edge;
                    continue;
                }
                if (current.Value.Item1 == edge.Item1)
                {
                    // Same x coord for both => running horizontal
                    if (current.Value.Item1 == x)
                    {
                        result.Add((edge.Item2, numHorizontal, current.Value.Item2 < edge.Item2 ? Direction.Right : Direction.Left));
                        result.Add((current.Value.Item2, numHorizontal++, current.Value.Item2 < edge.Item2 ? Direction.Right : Direction.Left));
                        //result.Add((edge.Item2, true));
                        //result.Add((current.Value.Item2, true));
                    }
                }
                else if (current.Value.Item2 == edge.Item2)
                {
                    // Same y coord for both => running vertical
                    if (current.Value.Item1 >= x && edge.Item1 <= x || edge.Item1 >= x && current.Value.Item1 <= x)
                    {
                        result.Add((edge.Item2, 0, current.Value.Item1 < edge.Item1 ? Direction.Up : Direction.Bottom));
                        //result.Add((edge.Item2, false));
                    }
                }

                current = edge;
            }
            result = result.OrderBy(c => c.Item1).ToList();
            //if (x == 4259966)
            //{
            //    using (var sw = new StreamWriter("./WrongPath.txt"))
            //    {
            //        foreach (var p in result)
            //        {
            //            sw.WriteLine(p.ToString());
            //        }
            //    }
            //}
            for (int i = 0; i < result.Count - 1; i++)
            {
                if (result[i].Item1 == result[i + 1].Item1)
                {
                    result[i] = (result[i].Item1, result[i].Item2 + result[i + 1].Item2, result[i].Item3 | result[i + 1].Item3);
                    result.RemoveAt(i + 1);
                }
            }
            //if (x == 4259966)
            //{
            //    using (var sw = new StreamWriter("./WrongPathFixed.txt"))
            //    {
            //        foreach (var p in result)
            //        {
            //            sw.WriteLine(p.ToString());
            //        }
            //    }
            //}
            //return result.Select(c => c.Item1).ToList();
            return result;
        }

        private static bool IntersectWithPath(long x, long y, List<(long, long)> path)
        {
            var current = (0L, 0L);
            foreach (var edge in path)
            {
                if (current.Item1 == edge.Item1)
                {
                    if (current.Item2 >= y && edge.Item2 <= y || edge.Item2 >= y && current.Item2 <= y)
                    {
                        return true;
                    }
                }
                if (current.Item2 == edge.Item2)
                {
                    if (current.Item1 >= y && edge.Item1 <= x || edge.Item1 >= x && current.Item1 <= x)
                    {
                        return true;
                    }
                }

                current = edge;
            }
            return false;
        }

        private static void PrintBoard(bool[,] board, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using var sw = new StreamWriter(path);
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    sw.Write(board[x, y] ? '#' : '.');
                }
                sw.WriteLine();
            }
        }

        private static void CheckBoard(bool[,] board, (int x, int y) boardIndex, BoardMeta currentBoardMeta, BoardMeta[,] boardMeta, ITracer tracer)
        {
            bool hasPath = false;
            for (int x = 0; x < board.GetLength(0) && !hasPath; x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y])
                    {
                        hasPath = true;
                    }
                }
            }
            var isOutside = new bool[board.GetLength(0), board.GetLength(1)];
            if (!hasPath)
            {
                if (currentBoardMeta.outSideTop.Count > 0 || currentBoardMeta.outSideBottom.Count > 0 || currentBoardMeta.outSideLeft.Count > 0 || currentBoardMeta.outSideRight.Count > 0)
                {
                    tracer.Write("All Outside ");
                    // No Path and i have outside edge => everything outside
                    currentBoardMeta.numOutside = board.GetLength(0) * board.GetLength(1);
                    PropagateAllOutside((board.GetLength(0), board.GetLength(1)), boardIndex, currentBoardMeta, boardMeta);
                }
                else
                {
                    tracer.Write("All Inside ");
                    // No path and no outisde edge => all within
                    currentBoardMeta.numOutside = 0;
                    PropagateAllInside(boardIndex, currentBoardMeta, boardMeta);
                }
            }
            else
            {
                foreach (var y in currentBoardMeta.outSideTop)
                {
                    MarkOutside(isOutside, board, (0, y));
                }
                foreach (var y in currentBoardMeta.outSideBottom)
                {
                    MarkOutside(isOutside, board, (board.GetLength(0) - 1, y));
                }
                foreach (var x in currentBoardMeta.outSideLeft)
                {
                    MarkOutside(isOutside, board, (x, 0));
                }
                foreach (var x in currentBoardMeta.outSideRight)
                {
                    MarkOutside(isOutside, board, (x, board.GetLength(1) - 1));
                }
                tracer.Write("Marked Outside ");
                var count = 0L;
                for (int x = 0; x < isOutside.GetLength(0); x++)
                {
                    for (int y = 0; y < isOutside.GetLength(1); y++)
                    {
                        if (isOutside[x, y])
                        {
                            count++;
                        }
                    }
                }
                tracer.Write("Counted Outside ");
                currentBoardMeta.numOutside = count;
                PropagateOutside(isOutside, boardIndex, currentBoardMeta, boardMeta);
            }
            currentBoardMeta.isClean = true;
        }

        private static void PropagateOutside(bool[,] isOutside, (int x, int y) boardIndex, BoardMeta currentBoardMeta, BoardMeta[,] boardMeta)
        {
            currentBoardMeta.outSideTop = new();
            currentBoardMeta.outSideBottom = new();
            currentBoardMeta.outSideLeft = new();
            currentBoardMeta.outSideRight = new();
            for (int x = 0; x < isOutside.GetLength(0); x++)
            {
                if (isOutside[x, 0])
                {
                    currentBoardMeta.outSideLeft.Add(x);
                }
                if (isOutside[x, isOutside.GetLength(1) - 1])
                {
                    currentBoardMeta.outSideRight.Add(x);
                }
            }
            for (int y = 0; y < isOutside.GetLength(1); y++)
            {
                if (isOutside[0, y])
                {
                    currentBoardMeta.outSideTop.Add(y);
                }
                if (isOutside[isOutside.GetLength(0) - 1, y])
                {
                    currentBoardMeta.outSideBottom.Add(y);
                }
            }
            if (boardIndex.x > 0)
            {
                var topMeta = boardMeta[boardIndex.x - 1, boardIndex.y];
                if (currentBoardMeta.outSideTop.Except(topMeta.outSideBottom).Count() > 0)
                {
                    topMeta.outSideBottom = currentBoardMeta.outSideTop.Union(topMeta.outSideBottom).ToList();
                    topMeta.isClean = false;
                }
            }
            if (boardIndex.x + 1 < boardMeta.GetLength(0))
            {
                var bottomMeta = boardMeta[boardIndex.x + 1, boardIndex.y];
                if (currentBoardMeta.outSideBottom.Except(bottomMeta.outSideTop).Count() > 0)
                {
                    bottomMeta.outSideTop = currentBoardMeta.outSideBottom.Union(bottomMeta.outSideTop).ToList();
                    bottomMeta.isClean = false;
                }
            }
            if (boardIndex.y > 0)
            {
                var leftMeta = boardMeta[boardIndex.x, boardIndex.y - 1];
                if (currentBoardMeta.outSideLeft.Except(leftMeta.outSideRight).Count() > 0)
                {
                    leftMeta.outSideRight = currentBoardMeta.outSideLeft.Union(leftMeta.outSideRight).ToList();
                    leftMeta.isClean = false;
                }
            }
            if (boardIndex.y + 1 < boardMeta.GetLength(1))
            {
                var rightMeta = boardMeta[boardIndex.x, boardIndex.y + 1];
                if (currentBoardMeta.outSideRight.Except(rightMeta.outSideLeft).Count() > 0)
                {
                    rightMeta.outSideLeft = currentBoardMeta.outSideRight.Union(rightMeta.outSideLeft).ToList();
                    rightMeta.isClean = false;
                }
            }
        }

        private static void PropagateAllInside((int x, int y) boardIndex, BoardMeta currentBoardMeta, BoardMeta[,] boardMeta)
        {
            currentBoardMeta.outSideTop.Clear();
            currentBoardMeta.outSideBottom.Clear();
            currentBoardMeta.outSideLeft.Clear();
            currentBoardMeta.outSideRight.Clear();
            if (boardIndex.x > 0)
            {
                var topMeta = boardMeta[boardIndex.x - 1, boardIndex.y];
                if (topMeta.outSideBottom.Count > 0)
                {
                    // Why?
                    topMeta.outSideBottom.Clear();
                    topMeta.isClean = false;
                }
            }
            if (boardIndex.x + 1 < boardMeta.GetLength(0))
            {
                var bottomMeta = boardMeta[boardIndex.x + 1, boardIndex.y];
                if (bottomMeta.outSideTop.Count > 0)
                {
                    bottomMeta.outSideTop.Clear();
                    bottomMeta.isClean = false;
                }
            }
            if (boardIndex.y > 0)
            {
                var leftMeta = boardMeta[boardIndex.x, boardIndex.y - 1];
                if (leftMeta.outSideRight.Count > 0)
                {
                    leftMeta.outSideRight.Clear();
                    leftMeta.isClean = false;
                }
            }
            if (boardIndex.y + 1 < boardMeta.GetLength(1))
            {
                var rightMeta = boardMeta[boardIndex.x, boardIndex.y + 1];
                if (rightMeta.outSideLeft.Count > 0)
                {
                    rightMeta.outSideLeft.Clear();
                    rightMeta.isClean = false;
                }
            }
        }

        private static void PropagateAllOutside((int maxX, int maxY) size, (int x, int y) boardIndex, BoardMeta currentBoardMeta, BoardMeta[,] boardMeta)
        {
            currentBoardMeta.outSideTop = new();
            currentBoardMeta.outSideBottom = new();
            currentBoardMeta.outSideLeft = new();
            currentBoardMeta.outSideRight = new();
            for (int x = 0; x < size.maxX; x++)
            {
                currentBoardMeta.outSideLeft.Add(x);
                currentBoardMeta.outSideRight.Add(x);
            }
            for (int y = 0; y < size.maxY; y++)
            {
                currentBoardMeta.outSideTop.Add(y);
                currentBoardMeta.outSideBottom.Add(y);
            }
            if (boardIndex.x > 0)
            {
                var topMeta = boardMeta[boardIndex.x - 1, boardIndex.y];
                if (currentBoardMeta.outSideTop.Except(topMeta.outSideBottom).Count() > 0)
                {
                    topMeta.outSideBottom = currentBoardMeta.outSideTop.Union(topMeta.outSideBottom).ToList();
                    topMeta.isClean = false;
                }
            }
            if (boardIndex.x + 1 < boardMeta.GetLength(0))
            {
                var bottomMeta = boardMeta[boardIndex.x + 1, boardIndex.y];
                if (currentBoardMeta.outSideBottom.Except(bottomMeta.outSideTop).Count() > 0)
                {
                    bottomMeta.outSideTop = currentBoardMeta.outSideBottom.Union(bottomMeta.outSideTop).ToList();
                    bottomMeta.isClean = false;
                }
            }
            if (boardIndex.y > 0)
            {
                var leftMeta = boardMeta[boardIndex.x, boardIndex.y - 1];
                if (currentBoardMeta.outSideLeft.Except(leftMeta.outSideRight).Count() > 0)
                {
                    leftMeta.outSideRight = currentBoardMeta.outSideLeft.Union(leftMeta.outSideRight).ToList();
                    leftMeta.isClean = false;
                }
            }
            if (boardIndex.y + 1 < boardMeta.GetLength(1))
            {
                var rightMeta = boardMeta[boardIndex.x, boardIndex.y + 1];
                if (currentBoardMeta.outSideRight.Except(rightMeta.outSideLeft).Count() > 0)
                {
                    rightMeta.outSideLeft = currentBoardMeta.outSideRight.Union(rightMeta.outSideLeft).ToList();
                    rightMeta.isClean = false;
                }
            }
        }

        private static void MarkOutside(bool[,] isOutside, bool[,] path, (long x, long y) start)
        {
            var queue = new Queue<(long x, long y)>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (isOutside[current.x, current.y])
                {
                    continue;
                }
                if (!path[current.x, current.y])
                {
                    isOutside[current.x, current.y] = true;
                    if (current.x > 0 && !isOutside[current.x - 1, current.y] && !path[current.x - 1, current.y])
                    {
                        queue.Enqueue((current.x - 1, current.y));
                    }
                    if (current.x < path.GetLength(0) - 1 && !isOutside[current.x + 1, current.y] && !path[current.x + 1, current.y])
                    {
                        queue.Enqueue((current.x + 1, current.y));
                    }
                    if (current.y > 0 && !isOutside[current.x, current.y - 1] && !path[current.x, current.y - 1])
                    {
                        queue.Enqueue((current.x, current.y - 1));
                    }
                    if (current.y < path.GetLength(1) - 1 && !isOutside[current.x, current.y + 1] && !path[current.x, current.y + 1])
                    {
                        queue.Enqueue((current.x, current.y + 1));
                    }
                }
            }
        }

        private static Direction GetDirection(string input)
        {
            if (input == "R")
            {
                return Direction.Right;
            }
            else if (input == "D")
            {
                return Direction.Bottom;
            }
            else if (input == "U")
            {
                return Direction.Up;
            }
            else if (input == "L")
            {
                return Direction.Left;
            }
            return Direction.Unknown;
        }

        public static List<(long, long)> GetPath(IEnumerable<(Direction, long, string)> instructions)
        {
            // Part 1
            //List<(long, long)> result = new();
            //var current = (0L, 0L);
            //result.Add(current);
            //foreach (var item in instructions)
            //{
            //    if (item.Item1 == Direction.North)
            //    {
            //        current = (current.Item1 - item.Item2, current.Item2);
            //    }
            //    else if (item.Item1 == Direction.East)
            //    {
            //        current = (current.Item1, current.Item2 + item.Item2);
            //    }
            //    else if (item.Item1 == Direction.South)
            //    {
            //        current = (current.Item1 + item.Item2, current.Item2);
            //    }
            //    else if (item.Item1 == Direction.West)
            //    {
            //        current = (current.Item1, current.Item2 - item.Item2);
            //    }
            //    result.Add(current);
            //}
            //return result;

            // Part 2
            List<(long, long)> result = new();
            var current = (0L, 0L);
            result.Add(current);
            var realInstructions = instructions.Select(ins =>
            {
                var ls = ins.Item3.Substring(2, 5);
                var d = ins.Item3.Substring(7, 1);
                var l = Convert.ToInt64(ls, 16);
                return (d == "0" ? Direction.Right : d == "1" ? Direction.Bottom : d == "2" ? Direction.Left : d == "3" ? Direction.Up : Direction.Unknown, l, ins.Item3);
            });
            foreach (var item in realInstructions)
            {
                if (item.Item1 == Direction.Up)
                {
                    current = (current.Item1 - item.Item2, current.Item2);
                }
                else if (item.Item1 == Direction.Right)
                {
                    current = (current.Item1, current.Item2 + item.Item2);
                }
                else if (item.Item1 == Direction.Bottom)
                {
                    current = (current.Item1 + item.Item2, current.Item2);
                }
                else if (item.Item1 == Direction.Left)
                {
                    current = (current.Item1, current.Item2 - item.Item2);
                }
                result.Add(current);
            }
            return result;
        }

        // not over 60000
        private const int MaxBoardSize = 65000;

        private static bool[,] GetBoardAt((int x, int y) boardIndex, (long x, long y) boardMax, List<(long, long)> path, (long x, long y) offset)
        {
            var boardSizeX = (boardIndex.x + 1) * MaxBoardSize > boardMax.x ? boardMax.x - boardIndex.x * MaxBoardSize : MaxBoardSize;
            var boardSizeY = (boardIndex.y + 1) * MaxBoardSize > boardMax.y ? boardMax.y - boardIndex.y * MaxBoardSize : MaxBoardSize;

            var result = new bool[boardSizeX, boardSizeY];
            var originalMinX = boardIndex.x * MaxBoardSize;
            var originalMinY = boardIndex.y * MaxBoardSize;
            var originalMaxX = originalMinX + boardSizeX;
            var originalMaxY = originalMinY + boardSizeY;

            (long X, long Y) current = (0L, 0L);

            foreach (var edge in path)
            {
                for (long x = current.X; x <= edge.Item1; x++)
                {
                    var originalX = x + offset.x;
                    if (originalX >= originalMinX && originalX < originalMaxX)
                    {
                        for (long y = current.Y; y <= edge.Item2; y++)
                        {
                            var originalY = y + offset.y;
                            if (originalY >= originalMinY && originalY < originalMaxY)
                            {
                                result[originalX - originalMinX, originalY - originalMinY] = true;
                            }
                        }
                    }
                }
                for (long x = current.X; x >= edge.Item1; x--)
                {
                    var originalX = x + offset.x;
                    if (originalX >= originalMinX && originalX < originalMaxX)
                    {
                        for (long y = current.Y; y >= edge.Item2; y--)
                        {
                            var originalY = y + offset.y;
                            if (originalY >= originalMinY && originalY < originalMaxY)
                            {
                                result[originalX - originalMinX, originalY - originalMinY] = true;
                            }
                        }
                    }
                }
                current = edge;
            }

            return result;
        }


    }
    internal class BoardMeta
    {
        public long numOutside = 0L;
        public bool isClean = false;
        public List<long> outSideTop = new();
        public List<long> outSideLeft = new();
        public List<long> outSideRight = new();
        public List<long> outSideBottom = new();
    }

    internal class ConsoleTracer : ITracer
    {
        private Stopwatch watch;
        private bool withTime = true;
        public ConsoleTracer(Stopwatch watch)
        {
            this.watch = watch;
        }

        public void Write(string input)
        {
            if (withTime)
            {
                withTime = false;
                Console.Write($"{watch.Elapsed}: {input}");
            }
            else
            {
                Console.Write(input);
            }
        }

        public void WriteLine(string input)
        {
            withTime = true;
            Console.WriteLine($"{watch.Elapsed}: {input}");
        }
    }

    internal interface ITracer
    {
        public void Write(string input);
        public void WriteLine(string input);
    }
}