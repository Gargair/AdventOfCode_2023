using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
            var modules = new List<IModule>();

            foreach (var line in lines)
            {
                if (line[0] == '%')
                {
                    modules.Add(new FlipFlopModule(line.Substring(1)));
                }
                else if (line[0] == '&')
                {
                    modules.Add(new ConjunctionModule(line.Substring(1)));
                }
                else
                {
                    modules.Add(new BroadcasterModule(line));
                }
            }
            var modulesLookup = modules.ToDictionary(w => w.Name);
            foreach (var module in modules)
            {
                foreach (var dest in module.GetDestionationModules())
                {
                    if (modulesLookup.ContainsKey(dest))
                    {
                        var destMod = modulesLookup[dest];
                        destMod.RegisterInput(module.Name);
                    }
                }
            }

            //var removed = false;
            //do
            //{
            //    removed = false;
            //    for (int i = 0; i < modules.Count; i++)
            //    {
            //        var module = modules[i];
            //        if (module is ConjunctionModule con && con.NumInputs == 1)
            //        {
            //            Console.WriteLine($"Removing {module.Name}");
            //            var prevModule = modulesLookup[con.lastPulse.Keys.ElementAt(0)];
            //            prevModule.InvertOutput = true;
            //            prevModule.SetDestinationModules(con.GetDestionationModules());
            //            modules.RemoveAt(i);
            //            i--;
            //            modulesLookup.Remove(module.Name);
            //            removed = true;
            //        }
            //    }
            //} while (removed);

            //using (var sw = new StreamWriter("./Shortened.txt", false))
            //{
            //    foreach (var module in modules)
            //    {
            //        if (module.InvertOutput)
            //        {
            //            sw.Write('!');
            //        }
            //        if (module is FlipFlopModule)
            //        {
            //            sw.Write('%');
            //        }
            //        else if (module is ConjunctionModule)
            //        {
            //            sw.Write('&');
            //        }
            //        sw.Write(module.Name);
            //        sw.Write(" -> ");
            //        sw.WriteLine(string.Join(", ", module.GetDestionationModules()));
            //    }
            //}

            // from bt: 3923 Until HighPulse
            // from rc: 4091 Until HighPulse
            // from qs: 4001 Until HighPulse
            // from qt: 3847 Until HighPulse

            long z1 = kgv(3923, 4091);
            long z2 = kgv(4001, 3847);
            Console.WriteLine($"{z1} {z2}");
            Console.WriteLine(kgv(z1, z2));
            var tracer = new ConsoleTracer(timer);
            //var tracer = new VoidTracer();

            var abortToken = threadAbort.Token;
            var solution = 0L;

            using (Timer checker = new((_) =>
            {
                Console.WriteLine($"{timer.Elapsed} Alive");
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
                var res = Calculate2(modulesLookup, tracer, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    Interlocked.Add(ref solution, res);
                }
                //});
            }
            timer.Stop();
            Console.WriteLine($"Calculating: {timer.Elapsed}");

            Console.WriteLine(solution);
        }

        public static long Calculate(Dictionary<string, IModule> moduleLookup, ITracer tracer, CancellationToken cancellationToken)
        {
            var counter = 0;
            long OuterHighPulses = 0L;
            long OuterLowPulses = 0L;
            do
            {
                var r = PressButton(moduleLookup, tracer, cancellationToken);
                counter++;
                OuterHighPulses += r.Where(s => s.isHighPulse).Count();
                OuterLowPulses += r.Where(s => !s.isHighPulse).Count();
            } while (counter < 1000 && moduleLookup.Values.Any(m => !m.HasInitialState()));
            if (counter < 1000)
            {
                var numIterationsLeft = 1000 / counter;
                foreach (var m in moduleLookup.Values)
                {
                    m.HighPulses *= numIterationsLeft;
                    m.LowPulses *= numIterationsLeft;
                }
                OuterHighPulses *= numIterationsLeft;
                OuterLowPulses *= numIterationsLeft;
                counter += (numIterationsLeft - 1) * counter;
            }
            while (counter < 1000)
            {
                var r = PressButton(moduleLookup, tracer, cancellationToken);
                counter++;
                OuterHighPulses += r.Where(s => s.isHighPulse).Count();
                OuterLowPulses += r.Where(s => !s.isHighPulse).Count();
            }
            var globalHighPulses = OuterHighPulses + moduleLookup.Values.Select(m => m.HighPulses).Sum();
            var globalLowPulses = OuterLowPulses + moduleLookup.Values.Select(m => m.LowPulses).Sum();
            tracer.WriteLine($"Low: {globalLowPulses}, High: {globalHighPulses}");
            return globalHighPulses * globalLowPulses;
        }

        public static long Calculate2(Dictionary<string, IModule> moduleLookup, ITracer tracer, CancellationToken cancellationToken)
        {
            var counter = 0L;
            while (true)
            {
                var r = PressButton(moduleLookup, tracer, cancellationToken);
                counter++;
                if (counter % 10000000 == 0)
                {
                    tracer.WriteLine(counter.ToString());
                }
                if (r.Any(s => s.outName == "jz" && s.isHighPulse))
                {
                    return counter;
                }
                if (moduleLookup.Values.All(m => m.HasInitialState()))
                {
                    throw new Exception("Got initial state without jz");
                }
            }
        }

        private static List<(string outName, bool isHighPulse)> PressButton(Dictionary<string, IModule> moduleLookup, ITracer tracer, CancellationToken cancellationToken)
        {
            List<(string outName, bool isHighPulse)> outerPulses = new List<(string outName, bool isHighPulse)>();
            Queue<(string sender, string module, bool isHighPulse)> queue = new();
            queue.Enqueue(("button", "broadcaster", false));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                //tracer.WriteLine($"{current.sender} -{current.isHighPulse}-> {current.module}");
                if (moduleLookup.ContainsKey(current.module))
                {
                    var curMod = moduleLookup[current.module];

                    var newPulse = curMod.ConsumePulse(current.sender, current.isHighPulse);
                    if (newPulse.HasValue)
                    {
                        if (curMod.InvertOutput)
                        {
                            newPulse = !newPulse.Value;
                        }
                        var dests = curMod.GetDestionationModules();
                        foreach (var dest in dests)
                        {
                            queue.Enqueue((curMod.Name, dest, newPulse.Value));
                        }
                    }
                }
                else
                {
                    outerPulses.Add((current.module, current.isHighPulse));
                }
            }
            return outerPulses;
        }

        public static long ggT(long _z1, long _z2)
        {
            long z1 = _z1;
            long z2 = _z2;
            while (z1 % z2 != 0)
            {
                long tmp = z1 % z2;
                z1 = z2;
                z2 = tmp;
            }
            return z2;
        }
        public static long kgv(long z1, long z2)
        {
            return (z1 * z2) / ggT(z1, z2);
        }
    }

    internal interface IModule
    {
        public bool InvertOutput { get; set; }
        public long LowPulses { get; set; }
        public long HighPulses { get; set; }
        public int NumInputs { get; set; }
        public string Name { get; set; }
        public void RegisterInput(string sender);
        public bool? ConsumePulse(string sender, bool isHighPulse);
        public IEnumerable<string> GetDestionationModules();
        public void SetDestinationModules(IEnumerable<string> destinationModules);
        public bool HasInitialState();
    }

    internal class FlipFlopModule : IModule
    {
        public bool InvertOutput { get; set; } = false;
        public long LowPulses { get; set; }
        public long HighPulses { get; set; }
        public int NumInputs { get; set; }
        public string Name { get; set; }
        private bool isOn = false;
        private List<string> destinationModules = new List<string>();

        public FlipFlopModule(string input)
        {
            var t = input.Split("->", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Name = t[0];
            destinationModules = t[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        public void RegisterInput(string sender) { NumInputs++; }
        public bool? ConsumePulse(string sender, bool isHighPulse)
        {
            if (isHighPulse)
            {
                HighPulses++;
            }
            else
            {
                LowPulses++;
            }
            if (!isHighPulse)
            {
                isOn = !isOn;
                return isOn;
            }
            return null;
        }

        public IEnumerable<string> GetDestionationModules()
        {
            return destinationModules;
        }

        public void SetDestinationModules(IEnumerable<string> destinationModules)
        {
            this.destinationModules = destinationModules.ToList();
        }

        public bool HasInitialState()
        {
            return !isOn;
        }
    }

    internal class ConjunctionModule : IModule
    {
        public bool InvertOutput { get; set; } = false;
        public long LowPulses { get; set; }
        public long HighPulses { get; set; }
        public int NumInputs { get; set; }
        public string Name { get; set; }
        public Dictionary<string, bool> lastPulse = new Dictionary<string, bool>();
        private List<string> destinationModules = new List<string>();

        public ConjunctionModule(string input)
        {
            var t = input.Split("->", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Name = t[0];
            destinationModules = t[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        public void RegisterInput(string sender)
        {
            NumInputs++;
            lastPulse.Add(sender, false);
        }

        public bool? ConsumePulse(string sender, bool isHighPulse)
        {
            if (isHighPulse)
            {
                HighPulses++;
            }
            else
            {
                LowPulses++;
            }
            if (lastPulse.ContainsKey(sender))
            {
                lastPulse[sender] = isHighPulse;
            }
            else
            {
                throw new Exception($"Got Pulse from not known sender me: {Name} sender: {sender}");
            }
            return !lastPulse.Values.All(c => c);
        }

        public IEnumerable<string> GetDestionationModules()
        {
            return destinationModules;
        }

        public void SetDestinationModules(IEnumerable<string> destinationModules)
        {
            this.destinationModules = destinationModules.ToList();
        }

        public bool HasInitialState()
        {
            return !lastPulse.Values.Any(c => c);
        }
    }

    internal class BroadcasterModule : IModule
    {
        public bool InvertOutput { get; set; } = false;
        public long LowPulses { get; set; }
        public long HighPulses { get; set; }
        public int NumInputs { get; set; }
        public string Name { get; set; }
        private List<string> destinationModules = new List<string>();

        public BroadcasterModule(string input)
        {
            var t = input.Split("->", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Name = t[0];
            destinationModules = t[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        public void RegisterInput(string sender) { NumInputs++; }
        public bool? ConsumePulse(string sender, bool isHighPulse)
        {
            if (isHighPulse)
            {
                HighPulses++;
            }
            else
            {
                LowPulses++;
            }
            return isHighPulse;
        }

        public IEnumerable<string> GetDestionationModules()
        {
            return destinationModules;
        }

        public void SetDestinationModules(IEnumerable<string> destinationModules)
        {
            this.destinationModules = destinationModules.ToList();
        }

        public bool HasInitialState()
        {
            return true;
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

    internal interface ITracer
    {
        public void Write(string input);
        public void WriteLine(string input);
    }
}