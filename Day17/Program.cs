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
            var min = long.MaxValue;
            CancellationTokenSource threadAbort = new();
            Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                threadAbort.Cancel();
                Console.WriteLine("Cancellation requested");
            };

            var abortToken = threadAbort.Token;

            byte[,] inputBoard = new byte[lines.Count(), lines.ElementAt(0).Length];


            int i = 0;
            int j = 0;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    j = 0;
                    foreach (var c in line)
                    {
                        inputBoard[i, j] = (byte)(c - '0');
                        j++;
                    }
                }
                i++;
            }

            bool[,] visited = new bool[inputBoard.GetLength(0), inputBoard.GetLength(1)];

            using (Timer checker = new((_) =>
            {
                Console.WriteLine($"\t{timer.Elapsed}\t\t{min}");
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
                min = Calculate(inputBoard, 4, 10, cancellationToken);
            }
            timer.Stop();
            Console.WriteLine($"Calculating: {timer.Elapsed}");

            Console.WriteLine(min);
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
            Up,
            Right,
            Bottom,
            Left
        }

        public static int Calculate(byte[,] board, int minDirectionLength, int maxDirectionLength, CancellationToken cancellationToken)
        {
            var g = BuildGraph(board, minDirectionLength, maxDirectionLength);
            CalcDijkstraa(g.start);
            var endNodes = g.nodes[(board.GetLength(0) - 1, board.GetLength(1) - 1)];
            return endNodes.Select(n => n.distance).Min() ?? 0;
            //CalculateDijkstra(board, (0, 0), out int?[,] distances, 3, cancellationToken);
            //globalMin = distances[board.GetLength(0) - 1, board.GetLength(1) - 1]!.Value;
            //bool[,] visited = new bool[board.GetLength(0), board.GetLength(1)];
            //FindEasiestPath(board, visited, 0, ref globalMin, (0, 0), Direction.North, 2, 3);
            //FindEasiestPath(board, visited, 0, ref globalMin, (0, 1), Direction.East, 1, 3);
            //FindEasiestPath(board, visited, 0, ref globalMin, (0, 1), Direction.South, 1, 3);
            //FindEasiestPath(board, visited, 0, ref globalMin, (1, 0), Direction.South, 1, 3);
            //FindEasiestPath(board, visited, 0, ref globalMin, (1, 0), Direction.East, 1, 3);
        }

        public static void CalculateDijkstra(byte[,] board, (byte x, byte y) start, out int?[,] distances, byte maxDirectionLength, CancellationToken cancellationToken)
        {
            distances = new int?[board.GetLength(0), board.GetLength(1)];
            (int, int, List<(Direction, int)>)?[,] vorgaenger = new (int, int, List<(Direction, int)>)?[board.GetLength(0), board.GetLength(1)];
            bool[,] calculated = new bool[board.GetLength(0), board.GetLength(1)];
            distances[start.Item1, start.Item2] = 0;
            //var toDo = new PriorityQueue<(byte, byte, Direction, int), int>();
            var toDo = new PriorityQueue<(int, int), int>();
            toDo.Enqueue((start.x, start.y), 0);
            //toDo.Enqueue((start.x, start.y, Direction.North, 0), 0);
            vorgaenger[start.Item1, start.Item2] = (0, 0, new List<(Direction, int)>() { (Direction.Left, 0) });
            //(byte x, byte y, Direction d)[,] vorgaenger = new (byte x, byte y, Direction d)[board.GetLength(0), board.GetLength(1)];
            //bool[,,] added = new bool[board.GetLength(0), board.GetLength(1), 4];

            while (toDo.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var current = toDo.Dequeue();
                if (calculated[current.Item1, current.Item2])
                {
                    Console.WriteLine($"popped calculated from queue????? {current.Item1}/{current.Item2}");
                    continue;
                }
                calculated[current.Item1, current.Item2] = true;
                var arrivedFrom = vorgaenger[current.Item1, current.Item2];
                if (!arrivedFrom.HasValue)
                {
                    throw new NullReferenceException($"Processing {current.Item1}/{current.Item2} without predecessor.");
                }
                CheckDirection(board, distances, calculated, vorgaenger, toDo, current, (current.Item1 - 1, current.Item2), Direction.Up, maxDirectionLength);
                CheckDirection(board, distances, calculated, vorgaenger, toDo, current, (current.Item1, current.Item2 + 1), Direction.Right, maxDirectionLength);
                CheckDirection(board, distances, calculated, vorgaenger, toDo, current, (current.Item1 + 1, current.Item2), Direction.Bottom, maxDirectionLength);
                CheckDirection(board, distances, calculated, vorgaenger, toDo, current, (current.Item1, current.Item2 - 1), Direction.Left, maxDirectionLength);
                //CheckDirection2(board, distances, calculated, added, vorgaenger, toDo, current, Direction.North, maxDirectionLength); ;
                //CheckDirection2(board, distances, calculated, added, vorgaenger, toDo, current, Direction.East, maxDirectionLength);
                //CheckDirection2(board, distances, calculated, added, vorgaenger, toDo, current, Direction.South, maxDirectionLength);
                //CheckDirection2(board, distances, calculated, added, vorgaenger, toDo, current, Direction.West, maxDirectionLength);
            }
        }

        private static void CheckDirection(byte[,] board, int?[,] distances, bool[,] calculated, (int, int, List<(Direction, int)>)?[,] vorgaenger, PriorityQueue<(int, int), int> toDo, (int, int) current, (int, int) target, Direction targetDirection, int maxDirectionLength)
        {
            var currentDistance = distances[current.Item1, current.Item2]!.Value;
            if (target.Item1 >= 0 && target.Item1 < board.GetLength(0) && target.Item2 >= 0 && target.Item2 < board.GetLength(1))
            {
                if (calculated[target.Item1, target.Item2])
                {
                    return;
                }
                var arrivedFrom = vorgaenger[current.Item1, current.Item2];
                if (!arrivedFrom.HasValue)
                {
                    throw new NullReferenceException($"Processing {current.Item1}/{current.Item2} without predecessor.");
                }

                var newDistance = currentDistance + board[target.Item1, target.Item2];
                var oldDistance = distances[target.Item1, target.Item2];
                var directionOffset = arrivedFrom.Value.Item3.Select(t => t.Item1 != targetDirection ? 0 : t.Item2).Min();
                bool canGo = directionOffset < maxDirectionLength;
                if (canGo)
                {
                    if (!oldDistance.HasValue || newDistance < oldDistance.Value)
                    {
                        distances[target.Item1, target.Item2] = newDistance;
                        List<(Direction, int)> incomings = new List<(Direction, int)>();
                        incomings.Add((targetDirection, directionOffset + 1));
                        vorgaenger[target.Item1, target.Item2] = (current.Item1, current.Item2, incomings);
                        //Console.WriteLine($"Queued {target.Item1}/{target.Item2}");
                        toDo.Enqueue((target.Item1, target.Item2), newDistance);
                    }
                    else if (newDistance == oldDistance.Value)
                    {
                        //Console.WriteLine($"Multiple predecessors for {target.Item1}/{target.Item2}");
                        var temp = vorgaenger[target.Item1, target.Item2];
                        temp!.Value.Item3.Add((targetDirection, directionOffset + 1));
                    }
                }
            }

        }

        private static (int x, int y) GetOffsetVector(Direction d)
        {
            if (d == Direction.Up)
            {
                return (-1, 0);
            }
            if (d == Direction.Bottom)
            {
                return (1, 0);
            }
            if (d == Direction.Left)
            {
                return (0, -1);
            }
            if (d == Direction.Right)
            {
                return (0, 1);
            }
            throw new ArgumentException("Invalid direction");
        }

        private static void FindEasiestPath(byte[,] board, bool[,] visited, long currentMin, ref long currentGlobalMin, (int x, int y) current, Direction targetDirection, int remainingDistance, int maxTargetDistance)
        {
            if (current.x == board.GetLength(0) - 1 && current.y == board.GetLength(1) - 1)
            {
                if (currentMin < currentGlobalMin)
                {
                    currentGlobalMin = currentMin;
                }
                return;
            }
            if (visited[current.x, current.y])
            {
                return;
            }
            if (currentMin >= currentGlobalMin)
            {
                return;
            }
            visited[current.x, current.y] = true;
            if (current.x > 0 && (targetDirection != Direction.Up || remainingDistance > 0))
            {
                (int x, int y) nextCell = (current.x - 1, current.y);
                var nextValue = board[nextCell.x, nextCell.y];
                var nextDirection = Direction.Up;
                var nextRemaining = targetDirection == nextDirection ? remainingDistance - 1 : maxTargetDistance - 1;
                FindEasiestPath(board, visited, currentMin + nextValue, ref currentGlobalMin, nextCell, nextDirection, nextRemaining, maxTargetDistance);
            }
            if (current.y > 0 && (targetDirection != Direction.Left || remainingDistance > 0))
            {
                (int x, int y) nextCell = (current.x, current.y - 1);
                var nextValue = board[nextCell.x, nextCell.y];
                var nextDirection = Direction.Left;
                var nextRemaining = targetDirection == nextDirection ? remainingDistance - 1 : maxTargetDistance - 1;
                FindEasiestPath(board, visited, currentMin + nextValue, ref currentGlobalMin, nextCell, nextDirection, nextRemaining, maxTargetDistance);
            }
            if (current.x + 1 < board.GetLength(0) && (targetDirection != Direction.Bottom || remainingDistance > 0))
            {
                (int x, int y) nextCell = (current.x + 1, current.y);
                var nextValue = board[nextCell.x, nextCell.y];
                var nextDirection = Direction.Bottom;
                var nextRemaining = targetDirection == nextDirection ? remainingDistance - 1 : maxTargetDistance - 1;
                FindEasiestPath(board, visited, currentMin + nextValue, ref currentGlobalMin, nextCell, nextDirection, nextRemaining, maxTargetDistance);
            }
            if (current.y + 1 < board.GetLength(1) && (targetDirection != Direction.Right || remainingDistance > 0))
            {
                (int x, int y) nextCell = (current.x, current.y + 1);
                var nextValue = board[nextCell.x, nextCell.y];
                var nextDirection = Direction.Right;
                var nextRemaining = targetDirection == nextDirection ? remainingDistance - 1 : maxTargetDistance - 1;
                FindEasiestPath(board, visited, currentMin + nextValue, ref currentGlobalMin, nextCell, nextDirection, nextRemaining, maxTargetDistance);
            }
            visited[current.x, current.y] = false;
        }




        public static void CalcDijkstraa(GraphNode start)
        {
            PriorityQueue<GraphNode, int> queue = new PriorityQueue<GraphNode, int>();
            queue.Enqueue(start, 0);
            start.distance = 0;
            while (queue.Count > 0)
            {
                GraphNode node = queue.Dequeue();
                if (node.visited)
                {
                    continue;
                }
                node.visited = true;
                if (!node.distance.HasValue)
                {
                    throw new Exception($"Found node to be processed without distance set.");
                }
                foreach (var next in node.next)
                {
                    if (!next.visited)
                    {
                        var newDistance = node.distance.Value + next.cost;
                        if (!next.distance.HasValue || next.distance.Value > newDistance)
                        {
                            next.distance = newDistance;
                            next.predecessor = node;
                            queue.Enqueue(next, newDistance);
                        }
                    }
                }
            }
        }

        public static (Dictionary<(int x, int y), List<GraphNode>> nodes, GraphNode start) BuildGraph(byte[,] board, int minDirectionLength, int maxDirectionLength)
        {
            var nodes = BuildNodes(board, minDirectionLength, maxDirectionLength);
            ConnectNodes((board.GetLength(0), board.GetLength(1)), nodes, minDirectionLength, maxDirectionLength);
            var startNode = new GraphNode() { cost = 0 };
            for (int i = minDirectionLength; i <= maxDirectionLength; i++)
            {
                if (i < board.GetLength(1))
                {
                    var targetNodes = nodes[(0, i)];
                    var targetRightNode = targetNodes.Where(n => n.pathDirection == Direction.Right && n.directionLength == i).FirstOrDefault();
                    if (targetRightNode == null)
                    {
                        throw new Exception($"Did not find expected target node: ({0},{i}): Right, {i}");
                    }
                    startNode.next.Add(targetRightNode);
                }
                if (i < board.GetLength(0))
                {
                    var targetNodes = nodes[(i, 0)];
                    var targetBottomNode = targetNodes.Where(n => n.pathDirection == Direction.Bottom && n.directionLength == i).FirstOrDefault();
                    if (targetBottomNode == null)
                    {
                        throw new Exception($"Did not find expected target node: ({i},{0}): Bottom, {i}");
                    }
                    startNode.next.Add(targetBottomNode);
                }
            }
            return (nodes, startNode);
        }

        public static void ConnectNodes((int x, int y) size, Dictionary<(int x, int y), List<GraphNode>> nodes, int minDirectionLength, int maxDirectionLength)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var currentNodes = nodes[(x, y)];
                    foreach (var curNode in currentNodes)
                    {
                        if (curNode.pathDirection == Direction.Up || curNode.pathDirection == Direction.Bottom)
                        {
                            var maxLeft = Math.Min(y, maxDirectionLength);
                            for (int i = minDirectionLength; i <= maxLeft; i++)
                            {
                                var targetNodes = nodes[(x, y - i)];
                                var targetLeftNode = targetNodes.Where(n => n.pathDirection == Direction.Left && n.directionLength == i).FirstOrDefault();
                                if (targetLeftNode == null)
                                {
                                    throw new Exception($"Did not find expected target node: ({x},{y - i}): Left, {i}");
                                }
                                curNode.next.Add(targetLeftNode);
                            }
                            var maxRight = Math.Min(size.y - 1 - y, maxDirectionLength);
                            for (int i = minDirectionLength; i <= maxRight; i++)
                            {
                                var targetNodes = nodes[(x, y + i)];
                                var targetRightNode = targetNodes.Where(n => n.pathDirection == Direction.Right && n.directionLength == i).FirstOrDefault();
                                if (targetRightNode == null)
                                {
                                    throw new Exception($"Did not find expected target node: ({x},{y + i}): Right, {i}");
                                }
                                curNode.next.Add(targetRightNode);
                            }
                        }
                        if (curNode.pathDirection == Direction.Left || curNode.pathDirection == Direction.Right)
                        {
                            var maxTop = Math.Min(x, maxDirectionLength);
                            for (int i = minDirectionLength; i <= maxTop; i++)
                            {
                                var targetNodes = nodes[(x - i, y)];
                                var targetTopNode = targetNodes.Where(n => n.pathDirection == Direction.Up && n.directionLength == i).FirstOrDefault();
                                if (targetTopNode == null)
                                {
                                    throw new Exception($"Did not find expected target node: ({x - i},{y}): Top, {i}");
                                }
                                curNode.next.Add(targetTopNode);
                            }
                            var maxBottom = Math.Min(size.x - 1 - x, maxDirectionLength);
                            for (int i = minDirectionLength; i <= maxBottom; i++)
                            {
                                var targetNodes = nodes[(x + i, y)];
                                var targetBottomNode = targetNodes.Where(n => n.pathDirection == Direction.Bottom && n.directionLength == i).FirstOrDefault();
                                if (targetBottomNode == null)
                                {
                                    throw new Exception($"Did not find expected target node: ({x + i},{y}): Bottom, {i}");
                                }
                                curNode.next.Add(targetBottomNode);
                            }
                        }
                    }
                }
            }
        }

        public static Dictionary<(int x, int y), List<GraphNode>> BuildNodes(byte[,] board, int minDirectionLength, int maxDirectionLength)
        {
            var result = new Dictionary<(int x, int y), List<GraphNode>>();
            Parallel.For(0, board.GetLength(0), x =>
            {
                Parallel.For(0, board.GetLength(1), y =>
                {
                    var r = BuildNodesForCell(board, (x, y), minDirectionLength, maxDirectionLength);
                    lock (result)
                    {
                        result.Add((x, y), r);
                    }
                });
            });
            return result;
        }

        public static List<GraphNode> BuildNodesForCell(byte[,] board, (int x, int y) cell, int minDirectionLength, int maxDirectionLength)
        {
            var result = new List<GraphNode>();
            var maxTop = Math.Min(cell.x, maxDirectionLength);
            var cost = 0;
            for (int i = 1; i <= maxTop; i++)
            {
                cost += board[cell.x + 1 - i, cell.y];
                if (i >= minDirectionLength)
                {
                    result.Add(new GraphNode() { cost = cost, pathDirection = Direction.Bottom, directionLength = i });
                }
            }
            var maxBottom = Math.Min(board.GetLength(0) - 1 - cell.x, maxDirectionLength);
            cost = 0;
            for (int i = 1; i <= maxBottom; i++)
            {
                cost += board[cell.x - 1 + i, cell.y];
                if (i >= minDirectionLength)
                {
                    result.Add(new GraphNode() { cost = cost, pathDirection = Direction.Up, directionLength = i });
                }
            }
            var maxLeft = Math.Min(cell.y, maxDirectionLength);
            cost = 0;
            for (int i = 1; i <= maxLeft; i++)
            {
                cost += board[cell.x, cell.y + 1 - i];
                if (i >= minDirectionLength)
                {
                    result.Add(new GraphNode() { cost = cost, pathDirection = Direction.Right, directionLength = i });
                }
            }
            var maxRight = Math.Min(board.GetLength(1) - 1 - cell.y, maxDirectionLength);
            cost = 0;
            for (int i = 1; i <= maxRight; i++)
            {
                cost += board[cell.x, cell.y - 1 + i];
                if (i >= minDirectionLength)
                {
                    result.Add(new GraphNode() { cost = cost, pathDirection = Direction.Left, directionLength = i });
                }
            }
            return result;
        }
    }

    internal class GraphNode
    {
        public int cost;
        public int? distance;
        public bool visited = false;
        public Direction pathDirection;
        public int directionLength;
        public List<GraphNode> next = new List<GraphNode>();
        public GraphNode? predecessor = null;
    }
}