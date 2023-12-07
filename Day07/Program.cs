using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Solution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadLines("./Input.txt").ToArray();

            // Part 1
            var cardsByCardStrengthLowestHighest = lines.Select(line => new CardSet(line)).Order(new CardStrengthComparer());
            var orderedCardsLowestHighest = cardsByCardStrengthLowestHighest.OrderBy(c => c.GetCardSetType());
            var handWinnigs = orderedCardsLowestHighest.Select((c, i) => c.bid * (i + 1));
            //foreach(var c in cardsByCardStrengthLowestHighest)
            //{
            //    Console.WriteLine(c);
            //}
            //Console.WriteLine();
            foreach (var c in orderedCardsLowestHighest)
            {
                if (c.cards.Contains('J'))
                {
                    Console.WriteLine(c);
                }
            }
            Console.WriteLine();

            Console.WriteLine(handWinnigs.Sum());

            // Part 2

        }


    }

    public class CardStrengthComparer : IComparer<CardSet>
    {
        public int Compare(CardSet? a, CardSet? b)
        {
            if (a == null || b == null) return 0;
            return a.CompareByCards(b);
        }
    }


    public class CardSet
    {
        private static Dictionary<char, int> cardStrength = new Dictionary<char, int>()
        {
            { 'A',13  },
            {'K', 12 },
            {'Q' , 11},
           // Part1 {'J', 10 },
            {'T', 9 },
            {'9', 8 },
            {'8', 7 },
            {'7', 6 },
            {'6', 5 },
            {'5', 4 },
            {'4', 3 },
            {'3', 2 },
            {'2', 1 },
            {'J', 0 } // Part2
        };
        public string cards { get; private set; }
        public long bid { get; private set; }
        private int type = 0;

        public CardSet(string line)
        {
            var t = line.Split(' ');
            cards = t[0];
            bid = long.Parse(t[1]);
        }

        public int GetCardSetType()
        {
            if (type == 0)
            {
                var t = cards.GroupBy(c => c).ToDictionary((g) => g.Key, g => g.Count());
                var j = 0;
                if (t.ContainsKey('J'))
                {
                    j = t['J'];
                    t.Remove('J');
                }
                var v = t.Values.OrderDescending();
                if (v.Count() == 0)
                {
                    type = 7;
                }
                else if (v.ElementAt(0) == 5 || v.ElementAt(0) + j == 5)
                {
                    type = 7;
                }
                else if (v.ElementAt(0) == 4 || v.ElementAt(0) + j == 4)
                {
                    type = 6;
                }
                else if ((v.ElementAt(0) == 3 || v.ElementAt(0) + j == 3) && v.ElementAt(1) == 2)
                {
                    type = 5;
                }
                else if (v.ElementAt(0) == 3 || v.ElementAt(0) + j == 3)
                {
                    type = 4;
                }
                else if ((v.ElementAt(0) == 2 || v.ElementAt(0) + j == 2) && v.ElementAt(1) == 2)
                {
                    type = 3;
                }
                else if (v.ElementAt(0) == 2 || v.ElementAt(0) + j == 2)
                {
                    type = 2;
                }
                else
                {
                    type = 1;
                }
            }
            return type;
        }

        // returns >0 when a > b; 0 when a = b; -1 when a < b
        public int CompareByCards(CardSet b)
        {
            for (int i = 0; i < 5; i++)
            {
                var charA = this.cards[i];
                var charB = b.cards[i];
                if (cardStrength[charA] > cardStrength[charB])
                {
                    return 1;
                }
                else if (cardStrength[charA] < cardStrength[charB])
                {
                    return -1;
                }
            }
            return 0;
        }

        public override string ToString()
        {
            return $"{cards} {bid} {type}";
        }
    }
}