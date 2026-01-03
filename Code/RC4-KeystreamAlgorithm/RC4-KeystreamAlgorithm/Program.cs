using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RC4_KeystreamAlgorithm
{

    enum Alphabet
    {
        None = 0,
        A = 1,
        B = 2,
        C = 3,
        D = 4,
        E = 5,
        F = 6,
        G = 7,
        H = 8,
        I = 9,
        J = 10,
        K = 11,
        L = 12,
        M = 13,
        N = 14,
        O = 15,
        P = 16,
        Q = 17,
        R = 18,
        S = 19,
        T = 20,
        U = 21,
        V = 22,
        W = 23,
        X = 24,
        Y = 25,
        Z = 26
    }
    enum Suit
    {
        Club = 1,
        Diamond = 2,
        Heart = 3,
        Speads = 4
    }

    enum CardNumber
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }
    class Card
    {
        public Suit Suit { get; set; }
        public CardNumber CardNumber { get; set; }
    }

    class Deck
    {
        public Deck()
        {
            Reset();
        }

        public List<Card> Cards { get; set; }

        public void Reset()
        {
            Cards = Enumerable.Range(1, 4)
                .SelectMany(s => Enumerable.Range(1, 13)
                                    .Select(c => new Card()
                                    {
                                        Suit = (Suit)s,
                                        CardNumber = (CardNumber)c
                                    }
                                            )
                            )
                   .ToList();
        }

        public void Shuffle()
        {
            Cards = Cards.OrderBy(c => Guid.NewGuid())
                         .ToList();
        }

        public Card TakeCard()
        {
            var card = Cards[Cards.Count - 1];
            Cards.RemoveAt(Cards.Count - 1);

            return card;
        }

        public IEnumerable<Card> TakeCards(int numberOfCards)
        {
            var cards = Cards.Take(numberOfCards);

            var takeCards = cards as Card[] ?? cards.ToArray();
            Cards.RemoveAll(takeCards.Contains);

            return takeCards;
        }

        public (List<Card> Reds, List<Card> Blacks) RedAndBlack(Deck deck)
        {
            List<Card> reds = new List<Card>();
            List<Card> blacks = new List<Card>();

            foreach(var card in deck.Cards)
            {
                switch (card.Suit) 
                {
                    case Suit.Heart:
                    case Suit.Diamond:
                        reds.Add(card);
                        break;
                    case Suit.Club:
                    case Suit.Speads:
                        blacks.Add(card);
                        break;

                    
                }

            }
            return (reds,blacks);
        }

        public Deck DeckFromRedAndBlack((List<Card> Reds, List<Card> Blacks) split)
        {
            Deck deck = new Deck();
            deck.Cards.Clear();
            for (int i = 0; i < split.Reds.Count; i++)
            {
                if (i < split.Blacks.Count)
                    deck.Cards.Add(split.Blacks[i]);

                if (i < split.Reds.Count)
                    deck.Cards.Add(split.Reds[i]);

            }
            return deck;
        }

        public int FindCard(Suit suit, CardNumber cardNumber)
        {
            return Cards.FindIndex(c=>c.Suit == suit && c.CardNumber == cardNumber);
        }
        public void SwapCards(int index1, int index2)
        {
            if (index1 < 0 || index1 >= Cards.Count || index2 < 0 || index2 >= Cards.Count)
                throw new ArgumentOutOfRangeException("Index invalid");

            (Cards[index1], Cards[index2]) = (Cards[index2], Cards[index1]);
        }


    }

    static class LookUpTable
    {
        private static readonly Dictionary<int, Card> valueToBlackCard;

        static LookUpTable()
        {
            valueToBlackCard = new Dictionary<int, Card>();

            // Spades 1–13
            for (int i = 1; i <= 13; i++)
            {
                valueToBlackCard[i] = new Card
                {
                    Suit = Suit.Speads,
                    CardNumber = (CardNumber)i
                };
            }

            // Clubs 14–26
            for (int i = 14; i <= 26; i++)
            {
                valueToBlackCard[i] = new Card
                {
                    Suit = Suit.Club,
                    CardNumber = (CardNumber)(i - 13) 
                };
            }
        }

        public static Card GetBlackCard(int value)
        {
            if (valueToBlackCard.TryGetValue(value, out var card))
            {
                return card;
            }
            throw new ArgumentOutOfRangeException(nameof(value), "Valoarea trebuie să fie între 1 și 26");
        }
    }

    internal class Program
    {
        public static List<int> Text2Numbers(string text)
        {
            return text
                .ToUpper()
                .Where(char.IsLetter)
                .Select(c => (int)((Alphabet)(c - 'A' + 1)))
                .ToList();

        }
        public static string Numbers2Text(List<int> numbers)
        {
           
            return string.Concat(numbers.Select(n =>
            {
                if (n < 1 || n > 26)
                    throw new ArgumentOutOfRangeException(nameof(numbers), "Numerele trebuie să fie între 1 și 26");

 
                return (char)('A' + n - 1);
            }));
        }

        public static List<int> CountWordsLength(string text)
        {
            var words = text.Split(new[] {' ', '\t', '\n'} , StringSplitOptions.RemoveEmptyEntries);
            return words.Select(w => w.Length).ToList();
        }

        public List<int> KeystreamAlgorithm(Deck deck, List<int> text2numbers, bool encrypt)
        {   List<int> list = new List<int>();
            int end = deck.Cards.Count() -1;
            
            for (int i =0; i< text2numbers.Count; i++){
                int item = text2numbers[i];
                int sum = (int)deck.Cards[1].CardNumber + (int)deck.Cards[end].CardNumber;
                int number = sum % 26;
                if (number == 0) number = 26;
                Card Blackcard = LookUpTable.GetBlackCard(number);

                int RedCardAboveIndex = deck.FindCard(Blackcard.Suit, Blackcard.CardNumber) + 1;

                if (RedCardAboveIndex >= deck.Cards.Count)
                    RedCardAboveIndex = deck.Cards.Count - 1;

                sum = (int)deck.Cards[RedCardAboveIndex].CardNumber + (int)deck.Cards[end].CardNumber;
                number = sum % 26;
                int newNumber;
                if (encrypt)
                {
                    newNumber = (number + item) % 26;
                }
                else
                {
                    newNumber = (item - number + 26) % 26;
                }
              

                list.Add(newNumber);

                deck.SwapCards(RedCardAboveIndex, end);

                Card card = deck.TakeCard();

                deck.Cards.Add(card);
                card = deck.TakeCard();
                deck.Cards.Add(card);
            }
            

            return list;
        }
        public string Encript(Deck deck, string text )
        {
            List<int> text2numbers = Text2Numbers(text);
            return Numbers2Text(KeystreamAlgorithm(deck, text2numbers,true));
        }

        public string Dencript(Deck deck, string encriptedString)
        {
            List<int> numbersFromEncriptedString = Text2Numbers(encriptedString);           
            return Numbers2Text(KeystreamAlgorithm(deck, numbersFromEncriptedString, false));

        }

        public static string SplitWordsByLengths(string text, List<int> wordLengths)
        {
            int index = 0;
            List<string> words = new List<string>();

            foreach (int length in wordLengths)
            {
                if (index + length > text.Length)
                    throw new ArgumentException("Text prea scurt pentru lungimile date");

                string word = text.Substring(index, length);
                words.Add(word);
                index += length;
            }

            return string.Join(" ", words);
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            Deck key = new Deck();
            key.Shuffle();

            string text = "HELLO WORLD";
            List<int> wordLengths = CountWordsLength(text);

            string encryptedText = p.Encript(key, text);
            Console.WriteLine("Encrypted: " + encryptedText);

            string decryptedText = p.Dencript(key, encryptedText);
            string decryptedWithSpaces = SplitWordsByLengths(decryptedText, wordLengths);
            Console.WriteLine("Decrypted: " + decryptedWithSpaces);
        }
    }
}
