using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
            var workflows = new List<Workflow>();
            var parts = new List<Part>();
            bool getWorkflows = true;
            var opt = new JsonNodeOptions();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    getWorkflows = false;
                    continue;
                }
                if (getWorkflows)
                {
                    workflows.Add(new Workflow(line));
                }
                else
                {
                    parts.Add(new Part(line));
                }
            }
            var workflowLookup = workflows.ToDictionary(w => w.Name);

            var tracer = new ConsoleTracer(timer);

            var abortToken = threadAbort.Token;
            var solution = 0L;

            using (Timer checker = new((_) =>
            {
                //Console.WriteLine($"\t{timer.ElapsedMilliseconds}");
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
                tokenSource.CancelAfter(10000);
                var cancellationToken = tokenSource.Token;
                if (abortToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellationtoken wrong");
                }
                var res = Calculate2(workflowLookup, tracer, cancellationToken);
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

        public static long Calculate(Part part, Dictionary<string, Workflow> workflowLookup, ITracer tracer, CancellationToken cancellationToken)
        {
            var curWorkflow = "in";
            while (curWorkflow != "A" && curWorkflow != "R")
            {
                var w = workflowLookup[curWorkflow];
                curWorkflow = w.ApplyWorkflow(part);
            }
            if (curWorkflow == "A")
            {
                return part.GetRating();
            }
            return 0L;
        }

        public static long Calculate2(Dictionary<string, Workflow> workflowLookup, ITracer tracer, CancellationToken cancellationToken)
        {
            var queue = new Queue<PartGroup>();
            var results = new List<PartGroup>();
            queue.Enqueue(new PartGroup() { nextWorkflow = "in", attrs = new Dictionary<char, (int min, int max)>() { { 'x', (1, 4000) }, { 'm', (1, 4000) }, { 'a', (1, 4000) }, { 's', (1, 4000) } } });
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var w = workflowLookup[cur.nextWorkflow];
                foreach (var next in w.ApplyWorkflow(cur))
                {
                    if (next.nextWorkflow == "A")
                    {
                        results.Add(next);
                    }
                    else if (next.nextWorkflow == "R")
                    {
                        continue;
                    }
                    else
                    {
                        queue.Enqueue(next);
                    }
                }
            }
            return results.Select(r => r.attrs.Values.Select(v => v.max - v.min + 1).Aggregate(1L, (a, b) => a * b)).Sum();
        }
    }

    internal class Workflow
    {
        private readonly Regex regex = new Regex("^(?'name'\\w+){(?'conditions'.+)}$", RegexOptions.Compiled);
        public string Name;
        private List<WorkflowCondition> conditions;

        public Workflow(string input)
        {
            var match = regex.Match(input);
            if (!match.Success)
            {
                throw new ArgumentException("Workflow in invalid format");
            }
            Name = match.Groups["name"].Value;
            var cond = match.Groups["conditions"].Value;
            var conds = cond.Split(',');
            conditions = new List<WorkflowCondition>();
            foreach (var c in conds)
            {
                conditions.Add(new WorkflowCondition(c));
            }
        }

        public string ApplyWorkflow(Part part)
        {
            foreach (var cond in conditions)
            {
                if (cond.Matches(part))
                {
                    return cond.GetOutput();
                }
            }
            throw new ArgumentException($"Did not match conditions on workflow {Name}");
        }

        public IEnumerable<PartGroup> ApplyWorkflow(PartGroup group)
        {
            var groupToCopy = group.Copy();
            foreach (var cond in conditions)
            {
                if (cond.op == 't')
                {
                    groupToCopy.nextWorkflow = cond.output;
                    yield return groupToCopy;
                    yield break;
                }
                var currentAttr = groupToCopy.attrs[cond.attr];
                if (cond.op == '<')
                {
                    if (currentAttr.min >= cond.val)
                    {
                        // No matching here
                        continue;
                    }
                    // min < val! 
                    var toReturn = groupToCopy.Copy();
                    toReturn.attrs[cond.attr] = (currentAttr.min, cond.val - 1);
                    toReturn.nextWorkflow = cond.output;
                    yield return toReturn;
                    groupToCopy.attrs[cond.attr] = (cond.val, currentAttr.max);
                }
                else if (cond.op == '>')
                {
                    if (currentAttr.max <= cond.val)
                    {
                        // No matching here
                        continue;
                    }
                    // max > val! 
                    var toReturn = groupToCopy.Copy();
                    toReturn.attrs[cond.attr] = (cond.val + 1, currentAttr.max);
                    toReturn.nextWorkflow = cond.output;
                    yield return toReturn;
                    groupToCopy.attrs[cond.attr] = (currentAttr.min, cond.val);
                }
            }
        }
    }

    internal class WorkflowCondition
    {
        private readonly Regex regex = new Regex("^(?'attr'[xmas])(?'op'[<>])(?'val'\\d+):(?'out'\\w+)$", RegexOptions.Compiled);
        public char attr;
        public char op;
        public int val;
        public string output;

        public WorkflowCondition(string cond)
        {
            if (cond.IndexOf(":") >= 0)
            {
                var match = regex.Match(cond);
                if (!match.Success)
                {
                    throw new ArgumentException("Condition not in valid format");
                }
                attr = match.Groups["attr"].Value[0];
                op = match.Groups["op"].Value[0];
                val = int.Parse(match.Groups["val"].Value);
                output = match.Groups["out"].Value;
            }
            else
            {
                op = 't';
                output = cond;
            }
        }

        private int GetPartValue(Part part)
        {
            if (attr == 'x')
            {
                return part.x;
            }
            if (attr == 'm')
            {
                return part.m;
            }
            if (attr == 'a')
            {
                return part.a;
            }
            if (attr == 's')
            {
                return part.s;
            }
            throw new ArgumentException($"Unknown attr {attr}");
        }

        public bool Matches(Part part)
        {
            if (op == 't')
            {
                return true;
            }
            var partVal = GetPartValue(part);
            if (partVal < val && op == '<')
            {
                return true;
            }
            else if (partVal > val && op == '>')
            {
                return true;
            }
            return false;
        }

        public string GetOutput()
        {

            return output;
        }
    }

    internal class Part
    {
        private readonly Regex regex = new Regex("^{x=(?'x'\\d+),m=(?'m'\\d+),a=(?'a'\\d+),s=(?'s'\\d+)}$", RegexOptions.Compiled);
        public int x;
        public int m;
        public int a;
        public int s;

        public Part(string input)
        {
            var match = regex.Match(input);
            if (!match.Success)
            {
                throw new ArgumentException($"No valid part: {input}");
            }
            x = int.Parse(match.Groups["x"].Value);
            m = int.Parse(match.Groups["m"].Value);
            a = int.Parse(match.Groups["a"].Value);
            s = int.Parse(match.Groups["s"].Value);
        }

        public long GetRating()
        {
            return x + m + a + s;
        }
    }

    internal class PartGroup
    {
        public Dictionary<char, (int min, int max)> attrs;
        public string nextWorkflow;

        public PartGroup Copy()
        {
            var c = new PartGroup()
            {
                attrs = new Dictionary<char, (int min, int max)>(),
                nextWorkflow = nextWorkflow
            };
            foreach (var p in attrs)
            {
                c.attrs.Add(p.Key, p.Value);
            }
            return c;
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

    internal interface ITracer
    {
        public void Write(string input);
        public void WriteLine(string input);
    }
}