using System;
using System.Collections.Generic;
using System.Linq;

public enum Suit { Черви, Бубны, Крести, Пики }

public class Card
{
    public Suit Suit { get; set; }
    public int Value { get; set; } // 2-14 (Ace high)
    public int Points { get; set; } //Points for scoring

    public Card(Suit suit, int value, int points)
    {
        Suit = suit;
        Value = value;
        Points = points;
    }

    public override string ToString()
    {
        //Для подсчёта комбинаций через очки
        string valueStr = Value switch
        {
            11 => "Валет",
            12 => "Дама",
            13 => "Король",
            14 => "Туз",
            _ => Value.ToString()
        };
        return $"{valueStr} {Suit} (Очки: {Points})";
    }
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;
        Card other = (Card)obj;
        return Suit == other.Suit && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Suit, Value);
    }
}

public class StackNode
{
    public Card Card { get; set; }
    public StackNode Next { get; set; }

    public StackNode(Card card)
    {
        Card = card;
        Next = null;
    }
}

public class CardStack
{
    private StackNode top;

    public void Push(Card card)
    {
        StackNode newNode = new StackNode(card);
        newNode.Next = top;
        top = newNode;
    }

    public Card Pop()
    {
        if (top == null) throw new InvalidOperationException("Stack is empty.");
        Card card = top.Card;
        top = top.Next;
        return card;
    }

    public Card Peek()
    {
        if (top == null) throw new InvalidOperationException("Stack is empty.");
        return top.Card;
    }

    public void InsertAt(int index, Card card)
    {
        if (index < 0) throw new ArgumentOutOfRangeException("Index must be non-negative.");

        if (index == 0)
        {
            Push(card);
            return;
        }

        StackNode current = top;
        StackNode previous = null;
        for (int i = 0; i < index - 1 && current != null; i++)
        {
            previous = current;
            current = current.Next;
        }

        if (current == null) throw new ArgumentOutOfRangeException("Index exceeds the stack size.");

        StackNode newNode = new StackNode(card);
        newNode.Next = current.Next;
        current.Next = newNode;
    }

    public Card RemoveAt(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException("Index must be non-negative.");

        if (index == 0)
        {
            return Pop();
        }

        StackNode current = top;
        StackNode previous = null;

        for (int i = 0; i < index && current != null; i++)
        {
            previous = current;
            current = current.Next;
        }

        if (current == null) throw new ArgumentOutOfRangeException("Index exceeds the stack size.");

        previous.Next = current.Next;
        return current.Card;
    }

    public void PrintStack()
    {
        StackNode current = top;
        while (current != null)
        {
            Console.WriteLine(current.Card);
            current = current.Next;
        }
    }

    public List<Card> ToList()
    {
        List<Card> list = new List<Card>();
        StackNode current = top;
        while (current != null)
        {
            list.Add(current.Card);
            current = current.Next;
        }
        return list;
    }
}

public class Desk
{
    private List<Card> cards;
    private List<Card> cardsTable;

    public Desk()
    {
        cards = new List<Card>();
        CreateDesk();
    }

public void CreateDesk()
    {
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            for (int value = 2; value <= 14; value++) // Туз 14 очков
            {
                int points = value > 10 ? 10 : value;
                cards.Add(new Card(suit, value, points));
            }
        }
    }

    public void PrintDesk()
    {
        foreach (Card card in cards)
        {
            Console.WriteLine(card);
        }
    }

    public void DealToTable(CardStack player1Hand, CardStack player2Hand, int numberOfCards = 5)
    {
        // Карты на стол
        cardsTable = new List<Card>();
        for (int i = 0; i < 5; i++)
        {
            cardsTable.Add(DrawCard());
        }

        Console.WriteLine("Карты на столе:");
        PrintCards(cardsTable);

        List<Card> player1HandList = player1Hand.ToList();
        List<Card> player2HandList = player2Hand.ToList();
        List<Card> player1CombinedHand = player1HandList.Concat(cardsTable).ToList();
        List<Card> player2CombinedHand = player2HandList.Concat(cardsTable).ToList();

        (HandRank player1Rank, List<int> player1Kickers) = HandEvaluator.EvaluateHand(player1CombinedHand);
        (HandRank player2Rank, List<int> player2Kickers) = HandEvaluator.EvaluateHand(player2CombinedHand);


        // Если у игрока нет комбинации, просто выводится комбинация стола, будет исправлено в следующем обновлении
        Console.WriteLine($"Рука игрока 1 ({string.Join(", ", player1HandList)}) + стол: {player1Rank}");
        Console.WriteLine($"Рука игрока 2 ({string.Join(", ", player2HandList)}) + стол: {player2Rank}");

        int winner = CompareHands(player1Rank, player1Kickers, player2Rank, player2Kickers);

        if (winner == 1) Console.WriteLine("Победил игрок 1!");
        else if (winner == 2) Console.WriteLine("Победил игрок 2!");
        else Console.WriteLine("Ничья!");
    }

    private void PrintCards(List<Card> cardsToPrint)
    {
        foreach (var card in cardsToPrint)
        {
            Console.WriteLine(card);
        }
    }

    public void Shuffle()
    {
        Random rand = new Random();
        int n = cards.Count;

        while (n > 1)
        {
            int k = rand.Next(n--);
            Card temp = cards[n];
            cards[n] = cards[k];
            cards[k] = temp;
        }
    }

    public Card DrawCard()
    {
        if (cards.Count == 0) throw new InvalidOperationException("No cards left in the deck.");

        Card drawnCard = cards[0];
        cards.RemoveAt(0);
        return drawnCard;
    }

    public void ReturnCard(Card card)
    {
        cards.Add(card);
    }

    private int CompareHands(HandRank rank1, List<int> kickers1, HandRank rank2, List<int> kickers2)
    {
        if (rank1 > rank2) return 1;
        if (rank2 > rank1) return 2;
        // Проверка на победу
        return CompareKickers(kickers1, kickers2);
    }

    private int CompareKickers(List<int> kickers1, List<int> kickers2)
    {
        for (int i = 0; i < Math.Min(kickers1.Count, kickers2.Count); i++)
        {
            if (kickers1[i] > kickers2[i]) return 1;
            if (kickers2[i] > kickers1[i]) return 2;
        }
        return 0; 
    }
}

public enum HandRank
{
    СтаршаяКарта,
    ОднаПара,
    ДвеПары,
    Сет,
    Стрит,
    Флеш,
    ФуллХаус,
    Каре,
    СтритФлеш,
    ФлешРояль
}

public class HandEvaluator
{
    public static (HandRank Rank, List<int> Kickers) EvaluateHand(List<Card> hand)
    {
        hand = hand.OrderByDescending(c => c.Value).ToList();

        if (IsRoyalFlush(hand)) return (HandRank.ФлешРояль, new List<int>());
        if (IsStraightFlush(hand)) return (HandRank.СтритФлеш, new List<int> { hand[0].Value });
        if (IsFourOfAKind(hand, out var fourOfAKindValue)) return (HandRank.Каре, GetKickers(hand, fourOfAKindValue));
        if (IsFullHouse(hand, out var threeOfAKindValue, out var pairValue)) return (HandRank.ФуллХаус, new List<int> { threeOfAKindValue, pairValue });
        if (IsFlush(hand)) return (HandRank.Флеш, hand.Select(c => c.Value).ToList());
        if (IsStraight(hand)) return (HandRank.Стрит, new List<int> { hand[0].Value });
        if (IsThreeOfAKind(hand, out var threeOfAKindValue2)) return (HandRank.Сет, GetKickers(hand, threeOfAKindValue2));
        if (IsTwoPair(hand, out var pairValue1, out var pairValue2)) return (HandRank.ДвеПары, GetKickers(hand, pairValue1, pairValue2));
        if (IsOnePair(hand, out var pairValue3)) return (HandRank.ОднаПара, GetKickers(hand, pairValue3));

        return (HandRank.СтаршаяКарта, hand.Select(c => c.Value).ToList());
    }

    // Помощь для корректной работы
    private static List<int> GetKickers(List<Card> hand, params int[] valuesToRemove)
    {
        return hand.Select(c => c.Value)
                   .Where(v => !valuesToRemove.Contains(v))
                   .OrderByDescending(v => v)
                   .ToList();
    }


    private static bool IsRoyalFlush(List<Card> hand) => IsFlush(hand) && IsStraight(hand) && hand[0].Value == 14;

    private static bool IsStraightFlush(List<Card> hand) => IsFlush(hand) && IsStraight(hand);

    private static bool IsFourOfAKind(List<Card> hand, out int fourOfAKindValue)
    {
        var groups = hand.GroupBy(c => c.Value).Select(g => new { Value = g.Key, Count = g.Count() }).ToList();
        fourOfAKindValue = groups.FirstOrDefault(g => g.Count == 4)?.Value ?? 0;
        return groups.Any(g => g.Count == 4);
    }

    private static bool IsFullHouse(List<Card> hand, out int threeOfAKindValue, out int pairValue)
    {
        var groups = hand.GroupBy(c => c.Value).Select(g => new { Value = g.Key, Count = g.Count() }).ToList();
        threeOfAKindValue = groups.FirstOrDefault(g => g.Count == 3)?.Value ?? 0;
        pairValue = groups.FirstOrDefault(g => g.Count == 2)?.Value ?? 0;
        return groups.Any(g => g.Count == 3) && groups.Any(g => g.Count == 2);
    }

    private static bool IsFlush(List<Card> hand) => hand.All(c => c.Suit == hand[0].Suit);

    private static bool IsStraight(List<Card> hand)
    {
        bool isAceLowStraight = hand[0].Value == 14 && hand[1].Value == 5 && hand[2].Value == 4 && hand[3].Value == 3 && hand[4].Value == 2;
        if (isAceLowStraight) return true;

        for (int i = 1; i < hand.Count; i++)
        {
            if (hand[i].Value != hand[i - 1].Value - 1) return false;
        }
        return true;
    }


    private static bool IsThreeOfAKind(List<Card> hand, out int threeOfAKindValue)
    {
        var groups = hand.GroupBy(c => c.Value).Select(g => new { Value = g.Key, Count = g.Count() }).ToList();
        threeOfAKindValue = groups.FirstOrDefault(g => g.Count == 3)?.Value ?? 0;
        return groups.Any(g => g.Count == 3);
    }

    private static bool IsTwoPair(List<Card> hand, out int pairValue1, out int pairValue2)
    {
        var groups = hand.GroupBy(c => c.Value).Select(g => new { Value = g.Key, Count = g.Count() }).ToList();
        var pairs = groups.Where(g => g.Count == 2).Select(g => g.Value).OrderByDescending(v => v).ToList();
        pairValue1 = pairs.Count >= 2 ? pairs[0] : 0;
        pairValue2 = pairs.Count >= 2 ? pairs[1] : 0;
        return pairs.Count >= 2;
    }

private static bool IsOnePair(List<Card> hand, out int pairValue)
    {
        var groups = hand.GroupBy(c => c.Value).Select(g => new { Value = g.Key, Count = g.Count() }).ToList();
        pairValue = groups.FirstOrDefault(g => g.Count == 2)?.Value ?? 0;
        return groups.Any(g => g.Count == 2);
    }
}

public class Program
{
    public static bool End { get; set; } = false;
    public static void Main(string[] args)
    {

        while (End == false)
        {
            Desk deck = new Desk();
            deck.Shuffle();

            // Пример создания рук игроков
            CardStack player1Hand = new CardStack();
            CardStack player2Hand = new CardStack();

            // Игроки берут по 2 карты
            for (int i = 0; i < 2; i++) 
            {
                player1Hand.Push(deck.DrawCard());
                player2Hand.Push(deck.DrawCard());
            }

            Console.WriteLine("Ваша рука:");
            player1Hand.PrintStack();

            Console.WriteLine("Ваш выбор:");
            Console.WriteLine("1. Продолжить");
            Console.WriteLine("2. Пас");
            int.TryParse(Console.ReadLine(), out int number);

            if (number == 1)
            {
                deck.DealToTable(player1Hand, player2Hand);
            }
            else if (number == 2)
            {
                Console.WriteLine("Вы лох");
            }

            Console.WriteLine("Продолжить игру?");
            Console.WriteLine("1. Да");
            Console.WriteLine("2. Нет");
            int.TryParse(Console.ReadLine(), out int n);

            if (n == 2)
            {
                End = true;
                break;
            }

        }

    }
}
