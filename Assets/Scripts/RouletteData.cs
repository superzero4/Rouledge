using System.Collections.Generic;
using System.Linq;

public struct RouletteData
{
    public static int[] order = new int[]
    {
        0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22,
        18, 29, 7, 28, 12, 35, 3, 26
    };

    public HashSet<int> numbers;
    public float betAmount;
    public float payoff;
    public string label;


    public RouletteData(IEnumerable<int> numbers, string label, int payoff, int betAmount = 1)
    {
        this.payoff = payoff;
        this.betAmount = betAmount;
        this.numbers = numbers.ToHashSet();
        this.label = label;
    }

    public RouletteData(int number, int payoff, int betAmount = 1) : this(new HashSet<int>() { number },
        number.ToString(), payoff, betAmount)
    {
    }

    public bool Contains(int val)
    {
        return numbers.Contains(val);
    }
}