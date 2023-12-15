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
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var h = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var w in h)
                    {
                        work.Add(new WorkPackage() { line = w });
                    }
                }
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
                            var r = Parallel.ForEach(work, new ParallelOptions() { MaxDegreeOfParallelism = 24, CancellationToken = abortToken }, (w, s) =>
                            {
                                if (abortToken.IsCancellationRequested)
                                {
                                    Console.WriteLine("Foreach started despite abortToken set.");
                                }
                                //lock (savedComputation)
                                //{
                                //    if (savedComputation.ContainsKey(w.line))
                                //    {
                                //        var result = savedComputation[w.line];
                                //        w.result = short.Parse(result);
                                //        Interlocked.Decrement(ref toCheck);
                                //        Interlocked.Decrement(ref toWork);
                                //        Interlocked.Add(ref sum, w.result);
                                //        return;
                                //    }
                                //}
                                CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(abortToken);
                                tokenSource.CancelAfter(1000);
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
                                    Interlocked.Add(ref sum, w.result);
                                    Interlocked.Decrement(ref toWork);

                                    //lock (savedComputation)
                                    //{
                                    //    if (!savedComputation.ContainsKey(w.line))
                                    //    {
                                    //        lock (interWriter)
                                    //        {
                                    //            interWriter.WriteLine($"{w.line}<=>{w.result}");
                                    //        }
                                    //        savedComputation.Add(w.line, w.result.ToString());
                                    //    }
                                    //}
                                }
                                else
                                {
                                    lock (notAccountedWriter)
                                    {
                                        notAccountedWriter.WriteLine($"{w.line}");
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        Console.WriteLine("After ForEach");

                        List<WorkPackage>[] boxes = new List<WorkPackage>[256];
                        for (int i = 0; i < boxes.Length; i++)
                        {
                            boxes[i] = new List<WorkPackage>();
                        }

                        foreach (var w in work)
                        {
                            var box = boxes[w.result];
                            var wIndex = box.IndexOf(w);
                            if (w.operation)
                            {
                                // to add
                                if (wIndex >= 0)
                                {
                                    box.RemoveAt(wIndex);
                                    box.Insert(wIndex, w);
                                }
                                else
                                {
                                    box.Add(w);
                                }
                            }
                            else
                            {
                                // to Remove
                                if (wIndex >= 0)
                                {
                                    box.RemoveAt(wIndex);
                                }
                            }
                        }

                        //for (int i = 0; i < boxes.Length; i++)
                        //{
                        //    var box = boxes[i];
                        //    Console.WriteLine($"Box {i}: {box.Aggregate("", (w1, w2) =>
                        //    {
                        //        return $"{w1} [{w2.label} {w2.strength}]";
                        //    })}");
                        //}

                        var boxResults = boxes.Select((box, i) =>
                        {
                            var r = 0L;
                            var boxNumber = i + 1;
                            for(int j = 0; j < box.Count; j++)
                            {
                                var w = box[j];
                                r += boxNumber * (j + 1) * w.strength;
                            }
                            return r;
                        });

                        Console.WriteLine(boxResults.Sum());
                    }
                }
            }

            //foreach(var w in work)
            //{
            //    Console.WriteLine($"{w.line}: {w.result}");
            //}

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
            public string line;
            public short result;
            public string label;
            public bool operation;
            public byte strength;

            public void Calculate(CancellationToken cancellationToken)
            {
                short r = 0;
                foreach (var c in line)
                {
                    r += (byte)c;
                    r *= 17;
                    r %= 256;
                }
                result = r;
            }

            public void CalculatePart2(CancellationToken cancellationToken)
            {
                if (line.IndexOf('=') > 0)
                {
                    var h = line.Split('=');
                    label = h[0];
                    operation = true;
                    strength = byte.Parse(h[1]);
                }
                if (line.IndexOf('-') > 0)
                {
                    label = line.Substring(0, line.Length - 1);
                    operation = false;
                    strength = 0;
                }
                short r = 0;
                foreach (var c in label)
                {
                    r += (byte)c;
                    r *= 17;
                    r %= 256;
                }
                result = r;
            }

            public override bool Equals(object? obj)
            {
                if (obj == null) { return false; }
                if (obj.GetType() != this.GetType()) { return false; }
                var other = (WorkPackage)obj;
                return other.label == this.label;
            }
        }
    }
}