using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Solution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt");
            var board = lines.Select(line => line.ToArray()).ToArray();
            long[]? startingPos = null;

            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    if (board[i][j] == 'S')
                    {
                        startingPos = new long[] { i, j };
                    }
                }
            }

            if (startingPos == null)
            {
                Console.WriteLine("No Starting Pos");
                return;
            }

            var directionMapping = new Dictionary<char, char>
            {
                { 'N', 'S' },
                { 'S', 'N' },
                { 'W', 'E' },
                { 'E', 'W' },
                { '\0', '\0' }
            };
            List<long> pathLengths = new();

            // Part 1
            //if (startingPos[0] > 0)
            //{
            //    var northLength = CheckForCycle(board, new long[] { startingPos[0] - 1, startingPos[1] }, 'S', directionMapping);
            //    Console.WriteLine($"North: {northLength}");
            //    if (northLength.HasValue)
            //    {
            //        pathLengths.Add(northLength.Value);
            //    }
            //}
            //if (startingPos[0] < board.Length - 1)
            //{
            //    var southLength = CheckForCycle(board, new long[] { startingPos[0] + 1, startingPos[1] }, 'N', directionMapping);
            //    Console.WriteLine($"South: {southLength}");
            //    if (southLength.HasValue)
            //    {
            //        pathLengths.Add(southLength.Value);
            //    }
            //}
            //if (startingPos[1] > 0)
            //{
            //    var westLength = CheckForCycle(board, new long[] { startingPos[0], startingPos[1] - 1 }, 'E', directionMapping);
            //    Console.WriteLine($"West: {westLength}");
            //    if (westLength.HasValue)
            //    {
            //        pathLengths.Add(westLength.Value);
            //    }
            //}
            //if (startingPos[1] < board.Length - 1)
            //{
            //    var eastLength = CheckForCycle(board, new long[] { startingPos[0], startingPos[1] + 1 }, 'W', directionMapping);
            //    Console.WriteLine($"East: {eastLength}");
            //    if (eastLength.HasValue)
            //    {
            //        pathLengths.Add(eastLength.Value);
            //    }
            //}
            //var maxLength = pathLengths.Max();
            //Console.WriteLine($"Max: {maxLength}");


            // Part 2
            //var wPath = GetPath(board, new long[] { startingPos[0], startingPos[1] - 1, -1, 0 }, 'E', directionMapping);
            var wPath = GetPath(board, new long[] { startingPos[0], startingPos[1] - 1, (char)Direction.South, (char)Direction.East }, 'E', directionMapping);

            if (wPath == null)
            {
                Console.WriteLine("Found no Path");
                return;
            }

            var pipeBoard = board.Select(l => l.Select(c => '.').ToArray()).ToArray();
            //var pathBoard = board.Select(l => l.Select(c => new char[] { '.', '.' }).ToArray()).ToArray();
            pipeBoard[startingPos[0]][startingPos[1]] = '*';
            //pathBoard[startingPos[0]][startingPos[1]] = new char[] { (char)Direction.South, (char)Direction.East };
            foreach (var pos in wPath)
            {
                pipeBoard[pos[0]][pos[1]] = '*';
                //pathBoard[pos[0]][pos[1]] = new char[] { (char)pos[2], (char)pos[3] };
            }

            //for (int i = 0; i < pipeBoard.Length; i++)
            //{
            //    FillBoard(pipeBoard, new long[] { i, 0 }, 'O', '.');
            //    FillBoard(pipeBoard, new long[] { i, pipeBoard[0].Length - 1 }, 'O', '.');
            //}
            //for (int j = 0; j < pipeBoard[0].Length; j++)
            //{
            //    FillBoard(pipeBoard, new long[] { 0, j }, 'O', '.');
            //    FillBoard(pipeBoard, new long[] { pipeBoard.Length - 1, j }, 'O', '.');
            //}

            var checkBoard = PrepareCheckBoard(pipeBoard, startingPos, wPath);

            for (int i = 0; i < checkBoard.Length; i++)
            {
                FillBoard(checkBoard, new long[] { i, 0 }, 'O', '.');
                FillBoard(checkBoard, new long[] { i, checkBoard[0].Length - 1 }, 'O', '.');
            }
            for (int j = 0; j < checkBoard[0].Length; j++)
            {
                FillBoard(checkBoard, new long[] { 0, j }, 'O', '.');
                FillBoard(checkBoard, new long[] { checkBoard.Length - 1, j }, 'O', '.');
            }

            for (int i = 0; i < checkBoard.Length; i++)
            {
                for (int j = 0; j < checkBoard[0].Length; j++)
                {
                    if (checkBoard[i][j] == '.')
                    {
                        FillBoard(checkBoard, new long[] { i, j }, 'I', '.');
                    }
                }
            }

            foreach (var line in checkBoard)
            {
                foreach (var pipe in line)
                {
                    Console.Write(pipe);
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            var checkPipeBoard = checkBoard.Where((_, i) => i % 2 == 0).Select(l => l.Where((_,i) => i % 2 == 0).ToArray()).ToArray();

            var countPoints = 0;
            foreach (var line in checkPipeBoard)
            {
                foreach (var pipe in line)
                {
                    Console.Write(pipe);
                    if (pipe == 'I')
                    {
                        countPoints++;
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine(countPoints.ToString());


            //var count = 0;
            //for(int i = 0; i < checkBoard.Length; i+= 2)
            //{
            //    for(int j = 0; j < checkBoard[0].Length; j+= 2)
            //    {
            //        if (checkBoard[i][j] == '.')
            //        {
            //            count++;
            //        }
            //    }
            //}
            //Console.WriteLine(count);
            //foreach (var line in pipeBoard)
            //{
            //    foreach (var pipe in line)
            //    {
            //        Console.Write(pipe);
            //        if (pipe == 'I')
            //        {
            //            countPoints++;
            //        }
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine(countPoints.ToString());

            //for (int i = 0; i < pipeBoard.Length; i++)
            //{
            //    for (int j = 0; j < pipeBoard[0].Length; j++)
            //    {
            //        if (pipeBoard[i][j] == '.')
            //        {
            //            if (IsOutside(pipeBoard, pathBoard, i, j))
            //            {
            //                FillBoard(pipeBoard, new long[] { i, j }, 'O', '.');
            //            }
            //            else
            //            {
            //                FillBoard(pipeBoard, new long[] { i, j }, 'I', '.');
            //            }
            //        }
            //    }
            //}

            //for (int i = 0; i < pipeBoard.Length; i++)
            //{
            //    for (int j = 0; j < pipeBoard[0].Length; j++)
            //    {
            //        if (pipeBoard[i][j] == '.')
            //        {
            //            var left = 0;
            //            var right = 0;
            //            for (int a = 0; a < pipeBoard[0].Length; a++)
            //            {
            //                if (pipeBoard[i][a] == '*')
            //                {
            //                    if (a < j)
            //                    {
            //                        if (pathBoard[i][a][1] == (char)Direction.North)
            //                        {
            //                            left++;
            //                        }
            //                        else if (pathBoard[i][a] == (char)Direction.South)
            //                        {
            //                            left--;
            //                        }
            //                    }
            //                    else if (a > j)
            //                    {
            //                        if (pathBoard[i][a] == (char)Direction.South)
            //                        {
            //                            right++;
            //                        }
            //                        else if (pathBoard[i][a] == (char)Direction.North)
            //                        {
            //                            right--;
            //                        }
            //                    }
            //                }
            //            }
            //            var top = 0;
            //            var bottom = 0;
            //            for (int b = 0; b < pipeBoard.Length; b++)
            //            {
            //                if (pipeBoard[b][j] == '*')
            //                {
            //                    if (b < i)
            //                    {
            //                        if (pathBoard[b][j] == (char)Direction.East)
            //                        {
            //                            top++;
            //                        }
            //                        else if (pathBoard[b][j] == (char)Direction.West)
            //                        {
            //                            top--;
            //                        }

            //                    }
            //                    else if (b > i)
            //                    {
            //                        if (pathBoard[b][j] == (char)Direction.West)
            //                        {
            //                            bottom++;
            //                        }
            //                        else if (pathBoard[b][j] == (char)Direction.East)
            //                        {
            //                            bottom--;
            //                        }
            //                    }
            //                }
            //            }
            //            if (left < 0) { left *= -1; }
            //            if (right < 0) { right *= -1; }
            //            if (top < 0) { top *= -1; }
            //            if (bottom < 0) { bottom *= -1; }
            //            pipeBoard[i][j] = Math.Min(Math.Min(left, right), Math.Min(top, bottom)) == 0 ? 'O' : 'I';
            //        }
            //    }
            //}

            //for (int i = 0; i < pipeBoard.Length; i++)
            //{
            //    for (int j = 0; j < pipeBoard[0].Length; j++)
            //    {
            //        if (pipeBoard[i][j] == 'I')
            //        {
            //            FillBoard(pipeBoard, new long[] { i + 1, j }, 'I', 'O');
            //            FillBoard(pipeBoard, new long[] { i, j + 1 }, 'I', 'O');
            //            FillBoard(pipeBoard, new long[] { i - 1, j }, 'I', 'O');
            //            FillBoard(pipeBoard, new long[] { i, j - 1 }, 'I', 'O');
            //        }
            //    }
            //}

            //FillBoard(pipeBoard, new long[] { 0, 0 }, 'O', 'I');
            //FillBoard(pipeBoard, new long[] { 0, pipeBoard[0].Length - 1 }, 'O', 'I');

            //var countPoints = 0;

            //foreach (var line in pipeBoard)
            //{
            //    foreach (var pipe in line)
            //    {
            //        Console.Write(pipe);
            //        if (pipe == 'I')
            //        {
            //            countPoints++;
            //        }
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine(countPoints.ToString());

            //foreach (var line in pathBoard)
            //{
            //    foreach (var pipe in line)
            //    {
            //        Console.Write(pipe[1]);
            //    }
            //    Console.WriteLine();
            //}

        }

        public enum Direction
        {
            North = 'N',
            South = 'S',
            West = 'W',
            East = 'E'
        };

        public static long[]? GetNext(char[][]? board, long[]? pos, char InDirection, out char OutDirection)
        {
            if (board == null || pos == null)
            {
                OutDirection = '\0';
                return null;
            }
            var x = pos[0];
            var y = pos[1];
            if (x < 0 || y < 0)
            {
                OutDirection = '\0';
                return null;
            }
            if (x >= board.Length || y >= board[0].Length)
            {
                OutDirection = '\0';
                return null;
            }
            char currentChar = board[x][y];
            if (currentChar == '|')
            {
                if (InDirection == 'N')
                {
                    OutDirection = 'S';
                    return new long[] { x + 1, y };
                }
                else if (InDirection == 'S')
                {
                    OutDirection = 'N';
                    return new long[] { x - 1, y };
                }
                else
                {
                    OutDirection = '\0';
                    return null;
                }
            }
            else if (currentChar == '-')
            {
                if (InDirection == 'E')
                {
                    OutDirection = 'W';
                    return new long[] { x, y - 1 };
                }
                else if (InDirection == 'W')
                {
                    OutDirection = 'E';
                    return new long[] { x, y + 1 };
                }
                else
                {
                    OutDirection = '\0';
                    return null;
                }
            }
            else if (currentChar == 'L')
            {
                if (InDirection == 'N')
                {
                    OutDirection = 'E';
                    return new long[] { x, y + 1 };
                }
                else if (InDirection == 'E')
                {
                    OutDirection = 'N';
                    return new long[] { x - 1, y };
                }
                else
                {
                    OutDirection = '\0';
                    return null;
                }
            }
            else if (currentChar == 'J')
            {
                if (InDirection == 'N')
                {
                    OutDirection = 'W';
                    return new long[] { x, y - 1 };
                }
                else if (InDirection == 'W')
                {
                    OutDirection = 'N';
                    return new long[] { x - 1, y };
                }
                else
                {
                    OutDirection = '\0';
                    return null;
                }
            }
            else if (currentChar == '7')
            {
                if (InDirection == 'W')
                {
                    OutDirection = 'S';
                    return new long[] { x + 1, y };
                }
                else if (InDirection == 'S')
                {
                    OutDirection = 'W';
                    return new long[] { x, y - 1 };
                }
                else
                {
                    OutDirection = '\0';
                    return null;
                }
            }
            else if (currentChar == 'F')
            {
                if (InDirection == 'E')
                {
                    OutDirection = 'S';
                    return new long[] { x + 1, y };
                }
                else if (InDirection == 'S')
                {
                    OutDirection = 'E';
                    return new long[] { x, y + 1 };
                }
                else
                {
                    OutDirection = '\0';
                    return null;
                }
            }
            else
            {
                OutDirection = '\0';
                return null;
            }

        }

        public static long? CheckForCycle(char[][] board, long[] curPos, char curDirection, Dictionary<char, char> directionMapping)
        {
            long curLength = 1;
            while (curPos != null)
            {
                char nextDirection;
                long[]? NextPos = GetNext(board, curPos, curDirection, out nextDirection);
                nextDirection = directionMapping[nextDirection];
                if (NextPos == null)
                {
                    char curChar = board[curPos[0]][curPos[1]];
                    if (curChar == 'S')
                    {
                        if (curLength > 1)
                        {
                            return curLength;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    curPos = NextPos;
                    curDirection = nextDirection;
                    curLength += 1;
                }
            }
            return null;
        }

        public static List<long[]>? GetPath(char[][] board, long[] curPos, char curDirection, Dictionary<char, char> directionMapping)
        {
            List<long[]> path = new List<long[]>();
            while (curPos != null)
            {
                char nextDirection;
                long[]? NextPos = GetNext(board, curPos, curDirection, out nextDirection);
                if (NextPos == null)
                {
                    char curChar = board[curPos[0]][curPos[1]];
                    if (curChar == 'S')
                    {
                        return path;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    path.Add(new long[] { curPos[0], curPos[1], curDirection, nextDirection });
                    nextDirection = directionMapping[nextDirection];
                    curPos = NextPos;
                    curDirection = nextDirection;
                }
            }
            return null;
        }

        public static void FillBoard(char[][] board, long[] pos, char filler, char overwritten)
        {
            if (pos[0] < 0 || pos[1] < 0 || pos[0] >= board.Length || pos[1] >= board[0].Length)
            {
                return;
            }
            Queue<long[]> queue = new Queue<long[]>();
            queue.Enqueue(pos);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var curChar = board[cur[0]][cur[1]];
                if (curChar == overwritten)
                {
                    board[cur[0]][cur[1]] = filler;
                    if (cur[0] > 0)
                    {
                        queue.Enqueue(new long[] { cur[0] - 1, cur[1] });
                    }
                    if (cur[0] < board.Length - 1)
                    {
                        queue.Enqueue(new long[] { cur[0] + 1, cur[1] });
                    }
                    if (cur[1] > 0)
                    {
                        queue.Enqueue(new long[] { cur[0], cur[1] - 1 });
                    }
                    if (cur[1] < board[0].Length - 1)
                    {
                        queue.Enqueue(new long[] { cur[0], cur[1] + 1 });
                    }
                }

            }
        }

        public static char[][] PrepareCheckBoard(char[][] board, long[] startingPos, List<long[]> path)
        {
            var checkBoard = board.Select(line => line.SelectMany(c => new char[] { c, '.' }).ToArray()).SelectMany(line =>
            {
                return new char[][]
                {
                    line,
                    line.Select(c => '.').ToArray()
                };
            }).ToArray();

            long[] current = startingPos;

            for (int i = 0; i < path.Count; i++)
            {
                var next = path[i];
                var diffX = next[0] - current[0];
                var diffY = next[1] - current[1];
                checkBoard[current[0] * 2 + diffX][current[1] * 2 + diffY] = 'B';
                current = next;
            }
            var diffStartX = startingPos[0] - current[0];
            var diffStartY = startingPos[1] - current[1];
            checkBoard[current[0] * 2 + diffStartX][current[1] * 2 + diffStartY] = 'B';
            return checkBoard;
        }

        //public static bool IsOutside(char[][] checkBoard, char[][][] pathBoard, long x, long y)
        //{
        //    if (x == 0 || y == 0 || x == checkBoard.Length - 1 || y == checkBoard[0].Length - 1)
        //    {
        //        return true;
        //    }




        //}

        //public static bool IsOutSideIterator(char[][] board, long x, long y)
        //{

        //}
    }
}