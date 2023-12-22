using ILGPU;
using ILGPU.IR.Values;
using ILGPU.Runtime;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Solution
{
    internal partial class Program
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
            List<Brick> bricks = new();
            Regex BlockMatcher = BlockMatcherRegex();
            Regex TupleMatcher = TupleMatcherRegex();
            var counter = 0;
            var highestX = 0;
            var highestY = 0;
            foreach (var line in lines)
            {
                var blockData = BlockMatcher.Match(line);
                if (blockData.Success)
                {
                    var start = blockData.Groups["start"].Value;
                    var end = blockData.Groups["end"].Value;
                    var startData = TupleMatcher.Match(start);
                    var brick = new Brick()
                    {
                        Name = ((char)(counter++ + 'A')).ToString()
                    };
                    if (startData.Success)
                    {
                        var startX = int.Parse(startData.Groups["x"].Value);
                        var startY = int.Parse(startData.Groups["y"].Value);
                        var startZ = int.Parse(startData.Groups["z"].Value);
                        brick.Start = (startX, startY, startZ);
                        brick.InitialStart = (startX, startY, startZ);
                        if (startX > highestX)
                        {
                            highestX = startX;
                        }
                        if (startY > highestY)
                        {
                            highestY = startY;
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to parse input: {line}");
                    }
                    var endData = TupleMatcher.Match(end);
                    if (endData.Success)
                    {
                        var endX = int.Parse(endData.Groups["x"].Value);
                        var endY = int.Parse(endData.Groups["y"].Value);
                        var endZ = int.Parse(endData.Groups["z"].Value);
                        brick.End = (endX, endY, endZ);
                        brick.InitialEnd = (endX, endY, endZ);
                        if (endX > highestX)
                        {
                            highestX = endX;
                        }
                        if (endY > highestY)
                        {
                            highestY = endY;
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to parse input: {line}");
                    }
                    bricks.Add(brick);
                    if (brick.Start.x != brick.End.x && brick.Start.y != brick.End.y)
                    {
                        Console.WriteLine($"Multi Dimension Block: {line}");
                    }
                    else if (brick.Start.y != brick.End.y && brick.Start.z != brick.End.z)
                    {
                        Console.WriteLine($"Multi Dimension Block: {line}");
                    }
                    else if (brick.Start.x != brick.End.x && brick.Start.z != brick.End.z)
                    {
                        Console.WriteLine($"Multi Dimension Block: {line}");
                    }
                    if (brick.Start.z == 0 || brick.End.z == 0)
                    {
                        Console.WriteLine($"Invalid Block: {line}");
                    }

                }
                else
                {
                    throw new Exception($"Failed to parse input: {line}");
                }
            }

            Console.WriteLine($"Dimension Ground: {highestX}/{highestY}");

            //var tracer = new ConsoleTracer(timer);
            //var tracer = new VoidTracer();
            using var tracer = new FileTracer("./Output.txt");

            var abortToken = threadAbort.Token;
            var solution = 0L;

            using (Timer checker = new((_) =>
            {
                //Console.WriteLine($"{timer.Elapsed} Alive");
            }))
            {
                checker.Change(0, 5000);
                //Parallel.ForEach(parts, (part) =>
                //{
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
                var res = Calculate2(bricks, tracer, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    Interlocked.Add(ref solution, res);
                }
                //});
            }
            timer.Stop();
            Console.WriteLine($"Calculating: {timer.Elapsed}");

            Console.WriteLine(solution);

            foreach (var brick in bricks)
            {
                if (brick.Start.x != brick.InitialStart.x || brick.Start.y != brick.InitialStart.y)
                {
                    Console.WriteLine($"Modified Start {brick}");
                }
                if (brick.End.x != brick.InitialEnd.x || brick.End.y != brick.InitialEnd.y)
                {
                    Console.WriteLine($"Modified End {brick}");
                }
                if (brick.Start.z > brick.InitialStart.z || brick.End.z > brick.InitialEnd.z)
                {
                    Console.WriteLine($"Moved Brick upwards {brick}");
                }
            }

        }

        public static long Calculate(List<Brick> bricks, ITracer tracer, CancellationToken cancellationToken)
        {
            var maxX = bricks.Select(b => Math.Max(b.Start.x, b.End.x)).Max();
            var maxY = bricks.Select(b => Math.Max(b.Start.y, b.End.y)).Max();
            PriorityQueue<Brick, int> queue = new();
            var layedBricks = new Dictionary<(int x, int y, int z), Brick>();
            foreach (var brick in bricks)
            {
                IterateOverBrickXYZ(brick, (b, x, y, z) =>
                {
                    layedBricks.Add((x, y, z), b);
                });
                queue.Enqueue(brick, Math.Min(brick.Start.z, brick.End.z));
            }
            var zLevels = new int[maxX + 1, maxY + 1];

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                tracer.WriteLine($"Dropping {current}");
                DropBrick(current, zLevels, layedBricks);
                tracer.WriteLine($"Dropped to {current}");
                IterateOverBrickXY(current, (b, x, y) =>
                {
                    var z = Math.Min(b.Start.z, b.End.z) - 1;
                    if (layedBricks.TryGetValue((x, y, z), out Brick lowerBrick))
                    {
                        if (!lowerBrick.Supporting.Contains(b))
                        {
                            lowerBrick.Supporting.Add(b);
                        }
                        if (!b.SupportedBy.Contains(lowerBrick))
                        {
                            b.SupportedBy.Add(lowerBrick);
                        }
                    }
                });
            }

            var count = 0L;
            foreach (var brick in bricks)
            {
                //if(brick.Start.z == 0 | brick.End.z == 0)
                //{
                //    Console.WriteLine($"Invalid Position after dropping");
                //}
                //if(brick.Supporting.Count == 0)
                //{
                //    count++;
                //    continue;
                //}
                if (brick.Supporting.Any(b => b.Supporting.Intersect(brick.Supporting).Count() > 1))
                {
                    tracer.WriteLine($"Brick supporting a supported by: {brick}");
                }
                if (brick.SupportedBy.Count() == 0 && Math.Min(brick.Start.z, brick.End.z) > 1)
                {
                    tracer.WriteLine($"Flying: {brick}");
                }
                if (brick.Supporting.All(b => b.SupportedBy.Count > 1))
                {
                    count++;
                }
            }

            //tracer.WriteLine("");
            //tracer.WriteLine("By x/z");
            //for (int z = 6; z > 0; z--)
            //{
            //    for (int x = 0; x < 3; x++)
            //    {
            //        var br = layedBricks.Where(p => p.Key.x == x && p.Key.z == z);
            //        if (br.Count() == 0)
            //        {
            //            tracer.Write(".");
            //        }
            //        else
            //        {
            //            var names = br.Select(b => b.Value.Name).Distinct();
            //            if (names.Count() == 1)
            //            {
            //                tracer.Write(names.First());
            //            }
            //            else
            //            {
            //                tracer.Write("?");
            //            }
            //        }
            //    }
            //    tracer.WriteLine("");
            //}
            //tracer.WriteLine("");
            //tracer.WriteLine("By y/z");
            //for (int z = 6; z > 0; z--)
            //{
            //    for (int y = 0; y < 3; y++)
            //    {
            //        var br = layedBricks.Where(p => p.Key.y == y && p.Key.z == z);
            //        if (br.Count() == 0)
            //        {
            //            tracer.Write(".");
            //        }
            //        else
            //        {
            //            var names = br.Select(b => b.Value.Name).Distinct();
            //            if (names.Count() == 1)
            //            {
            //                tracer.Write(names.First());
            //            }
            //            else
            //            {
            //                tracer.Write("?");
            //            }
            //        }
            //    }
            //    tracer.WriteLine("");
            //}

            return count;
        }

        public static long Calculate2(List<Brick> bricks, ITracer tracer, CancellationToken cancellationToken)
        {
            var maxX = bricks.Select(b => Math.Max(b.Start.x, b.End.x)).Max();
            var maxY = bricks.Select(b => Math.Max(b.Start.y, b.End.y)).Max();
            PriorityQueue<Brick, int> queue = new();
            var layedBricks = new Dictionary<(int x, int y, int z), Brick>();
            foreach (var brick in bricks)
            {
                IterateOverBrickXYZ(brick, (b, x, y, z) =>
                {
                    layedBricks.Add((x, y, z), b);
                });
                queue.Enqueue(brick, Math.Min(brick.Start.z, brick.End.z));
            }
            var zLevels = new int[maxX + 1, maxY + 1];

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                tracer.WriteLine($"Dropping {current}");
                DropBrick(current, zLevels, layedBricks);
                tracer.WriteLine($"Dropped to {current}");
                IterateOverBrickXY(current, (b, x, y) =>
                {
                    var z = Math.Min(b.Start.z, b.End.z) - 1;
                    if (layedBricks.TryGetValue((x, y, z), out Brick lowerBrick))
                    {
                        if (!lowerBrick.Supporting.Contains(b))
                        {
                            lowerBrick.Supporting.Add(b);
                        }
                        if (!b.SupportedBy.Contains(lowerBrick))
                        {
                            b.SupportedBy.Add(lowerBrick);
                        }
                    }
                });
            }

            return bricks.Select(DetermineFallBlockAmount).Sum();

            //var count = 0L;
            //foreach (var brick in bricks)
            //{
            //    if (brick.Supporting.Any(b => b.Supporting.Intersect(brick.Supporting).Count() > 1))
            //    {
            //        tracer.WriteLine($"Brick supporting a supported by: {brick}");
            //    }
            //    if (brick.SupportedBy.Count() == 0 && Math.Min(brick.Start.z, brick.End.z) > 1)
            //    {
            //        tracer.WriteLine($"Flying: {brick}");
            //    }
            //    if (brick.Supporting.All(b => b.SupportedBy.Count > 1))
            //    {
            //        count++;
            //    }
            //}

            //return count;
        }

        public static long DetermineFallBlockAmount(Brick brick)
        {
            var count = 0L;
            List<Brick> falling = new()
            {
                brick
            };
            PriorityQueue<Brick, int> queue = new();
            queue.Enqueue(brick, 0);
            while(queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach(var upper in current.Supporting)
                {
                    if(!upper.SupportedBy.Except(falling).Any() && !falling.Contains(upper))
                    {
                        count++;
                        falling.Add(upper);
                        queue.Enqueue(upper, Math.Min(upper.Start.z, upper.End.z));
                    }
                }
            }

            return count;
        }


        public static void DropBrick(Brick brick, int[,] zLevels, Dictionary<(int x, int y, int z), Brick> layedBricks)
        {
            var minZ = FindZLevel(brick, zLevels);
            IterateOverBrickXYZ(brick, (b, x, y, z) =>
            {
                layedBricks.Remove((x, y, z));
            });
            if (brick.Start.z == brick.End.z)
            {
                brick.Start.z = minZ + 1;
                brick.End.z = minZ + 1;
                IterateOverBrickXY(brick, (b, x, y) =>
                {
                    layedBricks.Add((x, y, b.Start.z), b);
                    zLevels[x, y] = b.Start.z;
                });
            }
            else
            {
                if (brick.Start.z < brick.End.z)
                {
                    var diff = brick.End.z - brick.Start.z;
                    brick.Start.z = minZ + 1;
                    brick.End.z = brick.Start.z + diff;
                    zLevels[brick.Start.x, brick.Start.y] = brick.End.z;
                }
                else
                {
                    var diff = brick.Start.z - brick.End.z;
                    brick.End.z = minZ + 1;
                    brick.Start.z = brick.End.z + diff;
                    zLevels[brick.Start.x, brick.Start.y] = brick.Start.z;
                }
                IterateOverBrickXYZ(brick, (b, x, y, z) =>
                {
                    layedBricks.Add((x, y, z), b);
                });
            }
        }

        public static int FindZLevel(Brick brick, int[,] zLevels)
        {
            var minZ = 0;
            IterateOverBrickXY(brick, (b, x, y) =>
            {
                var savedZ = zLevels[x, y];
                if (savedZ > minZ)
                {
                    minZ = savedZ;
                }
            });
            return minZ;
        }

        public static void IterateOverBrickXY(Brick brick, Action<Brick, int, int> action)
        {
            var startX = Math.Min(brick.Start.x, brick.End.x);
            var endX = Math.Max(brick.Start.x, brick.End.x);
            var startY = Math.Min(brick.Start.y, brick.End.y);
            var endY = Math.Max(brick.Start.y, brick.End.y);
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    action(brick, x, y);
                }
            }
        }

        public static void IterateOverBrickXYZ(Brick brick, Action<Brick, int, int, int> action)
        {
            var startX = Math.Min(brick.Start.x, brick.End.x);
            var endX = Math.Max(brick.Start.x, brick.End.x);
            var startY = Math.Min(brick.Start.y, brick.End.y);
            var endY = Math.Max(brick.Start.y, brick.End.y);
            var startZ = Math.Min(brick.Start.z, brick.End.z);
            var endZ = Math.Max(brick.Start.z, brick.End.z);
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        action(brick, x, y, z);
                    }
                }
            }
        }

        [GeneratedRegex("^(?'start'[\\d,]+)~(?'end'[\\d,]+)$", RegexOptions.Compiled)]
        private static partial Regex BlockMatcherRegex();
        [GeneratedRegex("^(?'x'\\d+),(?'y'\\d+),(?'z'\\d+)$", RegexOptions.Compiled)]
        private static partial Regex TupleMatcherRegex();
    }

    internal class Brick
    {
        public string Name = string.Empty;
        public (int x, int y, int z) Start;
        public (int x, int y, int z) End;
        public (int x, int y, int z) InitialStart;
        public (int x, int y, int z) InitialEnd;
        public List<Brick> Supporting = new();
        public List<Brick> SupportedBy = new();

        public override string ToString()
        {
            return $"{Start.x},{Start.y},{Start.z}~{End.x},{End.y},{End.z}:{InitialStart.x},{InitialStart.y},{InitialStart.z}~{InitialEnd.x},{InitialEnd.y},{InitialEnd.z}";
        }
    }

    internal class GraphiCalculator
    {
        public static long Calculate(byte[,] board, (int x, int y) start, int numSteps, ITracer tracer, CancellationToken cancellationToken)
        {
            using (var context = ILGPU.Context.CreateDefault())
            {
                var device = context.GetPreferredDevice(false);
                var acc = device.CreateAccelerator(context);


                var CalcCells = acc.LoadAutoGroupedStreamKernel<Index1D, ArrayView<ILGPU.Util.Int2>, ArrayView2D<byte, Stride2D.DenseX>, ArrayView2D<byte, Stride2D.DenseX>>(MakeStepForCells);

                var gBoard = acc.Allocate2DDenseX<byte>(new LongIndex2D(board.GetLength(0), board.GetLength(1)));
                gBoard.CopyFromCPU(board);

                List<ILGPU.Util.Int2> alreadyVisited = new();
                long partSolution = 1L;
                List<ILGPU.Util.Int2> currentVisited = new();
                currentVisited.Add(new ILGPU.Util.Int2(start.x, start.y));
                alreadyVisited.Add(new ILGPU.Util.Int2(start.x, start.y));

                for (int i = 0; i < numSteps; i++)
                {
                    if (i % 100 == 0)
                    {
                        tracer.WriteLine($"Step: {i,8} {partSolution} ({currentVisited.Count})");
                    }
                    var gCells = acc.Allocate1D<ILGPU.Util.Int2>(currentVisited.Count);
                    var gResults = acc.Allocate2DDenseX<byte>(new LongIndex2D(currentVisited.Count, 4));

                    gCells.CopyFromCPU(currentVisited.ToArray());


                    CalcCells(currentVisited.Count, gCells.View, gBoard.View, gResults.View);

                    acc.Synchronize();

                    byte[,] results = new byte[currentVisited.Count, 4];

                    gResults.CopyToCPU(results);

                    List<ILGPU.Util.Int2> nextVisited = new();
                    for (int a = 0; a < currentVisited.Count; a++)
                    {
                        var current = currentVisited[a];

                        if (results[a, 0] == 1)
                        {
                            var cell = new ILGPU.Util.Int2(current.X - 1, current.Y);
                            if (!nextVisited.Contains(cell) && !alreadyVisited.Contains(cell))
                            {
                                alreadyVisited.Add(cell);
                                nextVisited.Add(cell);
                                if (i % 2 == 1)
                                {
                                    partSolution++;
                                }
                            }
                        }
                        if (results[a, 1] == 1)
                        {
                            var cell = new ILGPU.Util.Int2(current.X + 1, current.Y);
                            if (!nextVisited.Contains(cell) && !alreadyVisited.Contains(cell))
                            {
                                alreadyVisited.Add(cell);
                                nextVisited.Add(cell);
                                if (i % 2 == 1)
                                {
                                    partSolution++;
                                }
                            }
                        }
                        if (results[a, 2] == 1)
                        {
                            var cell = new ILGPU.Util.Int2(current.X, current.Y - 1);
                            if (!nextVisited.Contains(cell) && !alreadyVisited.Contains(cell))
                            {
                                alreadyVisited.Add(cell);
                                nextVisited.Add(cell);
                                if (i % 2 == 1)
                                {
                                    partSolution++;
                                }
                            }
                        }
                        if (results[a, 3] == 1)
                        {
                            var cell = new ILGPU.Util.Int2(current.X, current.Y + 1);
                            if (!nextVisited.Contains(cell) && !alreadyVisited.Contains(cell))
                            {
                                alreadyVisited.Add(cell);
                                nextVisited.Add(cell);
                                if (i % 2 == 1)
                                {
                                    partSolution++;
                                }
                            }
                        }
                    }

                    currentVisited = nextVisited;
                }

                return partSolution;

            }
        }



        private readonly static Index2D OffsetTop = new Index2D(-1, 0);
        private readonly static Index2D OffsetBottom = new Index2D(1, 0);
        private readonly static Index2D OffsetLeft = new Index2D(0, -1);
        private readonly static Index2D OffsetRight = new Index2D(0, 1);

        private static void MakeStepForCells(Index1D index, ArrayView<ILGPU.Util.Int2> coords, ArrayView2D<byte, Stride2D.DenseX> board, ArrayView2D<byte, Stride2D.DenseX> results)
        {
            var currentCell = new Index2D(coords[index].X, coords[index].Y);
            //Interop.WriteLine("MakeStepForCells: {0} {1}/{2}", index, currentCell.X, currentCell.Y);
            var top = currentCell.Add(OffsetTop);
            results[new Index2D(index, 0)] = (byte)(1 - IsRockAt(board, top));
            var bottom = currentCell.Add(OffsetBottom);
            results[new Index2D(index, 1)] = (byte)(1 - IsRockAt(board, bottom));
            var left = currentCell.Add(OffsetLeft);
            results[new Index2D(index, 2)] = (byte)(1 - IsRockAt(board, left));
            var right = currentCell.Add(OffsetRight);
            results[new Index2D(index, 3)] = (byte)(1 - IsRockAt(board, right));

        }
        private static byte IsRockAt(ArrayView2D<byte, Stride2D.DenseX> board, Index2D cell)
        {
            var x = cell.X;
            var y = cell.Y;
            while (x < 0)
            {
                x += (int)board.Extent.X;
            }
            if (x >= board.Extent.X)
            {
                x %= (int)board.Extent.X;
            }
            while (y < 0)
            {
                y += (int)board.Extent.Y;
            }
            if (y >= board.Extent.Y)
            {
                y %= (int)board.Extent.Y;
            }
            //Interop.Write("\t{0}/{1} => {2}/{3} =>", cell.X, cell.Y, x, y);
            //Interop.WriteLine("{0}", board[new Index2D(x, y)]);
            return board[new Index2D(x, y)];
        }
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

    internal class VoidTracer : ITracer
    {
        public void Write(string input) { }
        public void WriteLine(string input) { }
    }

    internal class FileTracer : ITracer, IDisposable
    {
        private StreamWriter sw;
        public FileTracer(string path)
        {
            this.sw = new StreamWriter(path);
        }

        public void Write(string input)
        {
            sw.Write(input);
        }

        public void WriteLine(string input)
        {
            sw.WriteLine(input);
        }

        public void Dispose()
        {
            sw.Dispose();
        }
    }

    internal interface ITracer
    {
        public void Write(string input);
        public void WriteLine(string input);
    }
}