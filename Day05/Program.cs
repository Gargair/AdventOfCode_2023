using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Day1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt");

            var maps = new Dictionary<string, List<MapEntry>>();
            var seedNumbers = new List<long[]>();
            List<MapEntry>? currentMap = null;

            lines.Select((line, i) =>
            {
                if (i == 0)
                {
                    seedNumbers = line.Split(' ').Skip(1).Select(n => long.Parse(n)).Chunk(2).ToList();
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    currentMap = null;
                }
                else if (currentMap == null)
                {
                    maps.Add(line, new List<MapEntry>());
                    currentMap = maps[line];
                }
                else
                {
                    var numbers = line.Split(' ').Select(n => long.Parse(n)).ToArray();
                    currentMap.Add(new MapEntry()
                    {
                        DestinationStart = numbers[0],
                        SourceStart = numbers[1],
                        Length = numbers[2]
                    });
                }

                return 0;
            }).Sum();

            // Part1
            //var seeds = seedNumbers.Select(seedNumber =>
            //{
            //    var s = new PlantingSeed(seedNumber);
            //    var seedToSoil = maps["seed-to-soil map:"];
            //    s.Soil = useMap(seedNumber, seedToSoil);
            //    var soilToFert = maps["soil-to-fertilizer map:"];
            //    s.Fertilizer = useMap(s.Soil, soilToFert);
            //    var fertToWat = maps["fertilizer-to-water map:"];
            //    s.Water = useMap(s.Fertilizer, fertToWat);
            //    var watToLight = maps["water-to-light map:"];
            //    s.Light = useMap(s.Water, watToLight);
            //    var lightToTemp = maps["light-to-temperature map:"];
            //    s.Temperature = useMap(s.Light, lightToTemp);
            //    var tempToHum = maps["temperature-to-humidity map:"];
            //    s.Humidity = useMap(s.Temperature, tempToHum);
            //    var humToLoc = maps["humidity-to-location map:"];
            //    s.Location = useMap(s.Humidity, humToLoc);

            //    return s;
            //});

            //Console.WriteLine();
            //Console.WriteLine(seeds.MinBy(s => s.Location)?.Location);

            // Part 2
            long numberOfSeeds = 0;
            var seeds = seedNumbers.AsParallel().SelectMany((seedRange, r) =>
            {
                var seedStart = seedRange[0];
                var seedLength = seedRange[1];
                var seeds = new List<long>();
                for (long i = 0; i < seedLength; i++)
                {
                    seeds.Add(seedStart + i);
                }
                numberOfSeeds += seedLength;
                return seeds;
            }).Select((seedNumber, r) =>
            {
                var s = new PlantingSeed(seedNumber);
                var seedToSoil = maps["seed-to-soil map:"];
                s.Soil = useMap(seedNumber, seedToSoil);
                var soilToFert = maps["soil-to-fertilizer map:"];
                s.Fertilizer = useMap(s.Soil, soilToFert);
                var fertToWat = maps["fertilizer-to-water map:"];
                s.Water = useMap(s.Fertilizer, fertToWat);
                var watToLight = maps["water-to-light map:"];
                s.Light = useMap(s.Water, watToLight);
                var lightToTemp = maps["light-to-temperature map:"];
                s.Temperature = useMap(s.Light, lightToTemp);
                var tempToHum = maps["temperature-to-humidity map:"];
                s.Humidity = useMap(s.Temperature, tempToHum);
                var humToLoc = maps["humidity-to-location map:"];
                s.Location = useMap(s.Humidity, humToLoc);

                return s;
            });

            Console.WriteLine();
            Console.WriteLine(seeds.MinBy(s => s.Location)?.Location);
        }

        private static long useMap(long input, List<MapEntry> map)
        {
            foreach (var entry in map)
            {
                var offset = input - entry.SourceStart;
                if (offset >= 0 && offset < entry.Length)
                {
                    return entry.DestinationStart + offset;
                }
            }
            return input;
        }
    }

    internal class PlantingSeed
    {
        public long SeedNumber { get; private set; }

        public long Soil { get; set; }
        public long Fertilizer { get; set; }
        public long Water { get; set; }
        public long Light { get; set; }
        public long Temperature { get; set; }
        public long Humidity { get; set; }
        public long Location { get; set; }

        public PlantingSeed(long SeedNumber)
        {
            this.SeedNumber = SeedNumber;
        }
    }

    internal class MapEntry
    {
        public long DestinationStart;
        public long SourceStart;
        public long Length;
    }
}