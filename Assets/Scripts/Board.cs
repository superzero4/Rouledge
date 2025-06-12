using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class Board : MonoBehaviour
{
    [Header("References")] public BellRenderer _bell;
    public UIDocument _root;
    public VisualTreeAsset _tile;
    [Header("Layout")] public Vector2Int _size;
    public int _rightAdditionalTiles;
    public int _bottomAdditionalTiles;

    [Header("Betting")] [SerializeField, InspectorName("Initial Bet Amount"), Range(0, 1000)]
    private int currentBetAmount = 10;

    [SerializeReference] private Bet bet = new Bet();
    [Header("Colors")] public Color green = new Color(0, .5f, 0);

    
    private Label evText;
    private Label varText;
    private Label betText;
    private VisualElement _bellParent;

    void Start()
    {
        var board = _root.rootVisualElement.Q<VisualElement>("Board");
        evText = _root.rootVisualElement.Q<Label>("EV");
        varText = _root.rootVisualElement.Q<Label>("Var");
        betText = _root.rootVisualElement.Q<Label>("Bet");
        _bellParent = _root.rootVisualElement.Q<VisualElement>("Graph");
        _bell.Root = _bellParent;
        //+ 1 is the zero on the left
        float w = 100f / (_size.x + 1 + _rightAdditionalTiles);
        float h = 100f / (_size.y + _bottomAdditionalTiles);
        var zero = NewBetTile(w, 100f, 0, 0, green, new RouletteData(0, 35));
        board.Add(zero);
        int curr = 0;
        foreach (var val in RouletteData.order.Skip(1))
        {
            var j = _size.y - 1 - (val - 1) % _size.y;
            var i = (val - 1) / _size.y;
            var t = NewBetTile(w, h, (i + 1) * w, j * h, curr % 2 == 0 ? Color.red : Color.black,
                new RouletteData(val, 35));
            board.Add(t);
            curr++;
        }

        for (int j = 0; j < _size.y; j++)
        {
            var lineBet = NewBetTile(w, h, (_size.x + 1) * w, j * h, green,
                new RouletteData(Enumerable.Range(0, 12).Select(s => j + 1 + s * 3), "2 to 1", 2),
                () => Debug.Log("Line bet clicked!"));
            board.Add(lineBet);
        }

        int buttonCount = 0;
        for (int j = _size.y; j < _size.y + _bottomAdditionalTiles; j++)
        {
            string data;
            CallbackDelegate callback = null;
            if (buttonCount == 0)
            {
                data = "X2";
                callback = () =>
                {
                    currentBetAmount *= 2;
                    Debug.Log($"Current bet amount increased to: {currentBetAmount}");
                };
            }
            else
            {
                data = "Clear";
                callback = () =>
                {
                    currentBetAmount = 10;
                    bet.ClearBets();
                };
            }

            var button = NewHelperTile(w, h, (_size.x + 1) * w, j * h, Color.grey, callback, data);
            board.Add(button);
            buttonCount++;
        }

        RouletteData[] subCollums1 = new RouletteData[]
        {
            new(Enumerable.Range(1, 12), "1st 12", 2),
            new(Enumerable.Range(13, 12), "2nd 12", 2),
            new(Enumerable.Range(25, 12), "3rd 12", 2)
        };
        SubCollumns(subCollums1, w, h, _size.y, board);

        IEnumerable<int> first12 = Enumerable.Range(1, 18);
        var evens = first12.Select(x => x * 2);
        var odds = first12.Select(x => x * 2 - 1);
        RouletteData[] subColumns2 = new RouletteData[]
        {
            new(first12, "1 to 18", 1),
            new(evens, "Even", 1),
            new(odds.Select(e => RouletteData.order[e]), "Red", 1),
            new(evens.Select(e => RouletteData.order[e]), "Black", 1),
            new(odds, "Odd", 1),
            new(Enumerable.Range(19, 18), "19 to 36", 1)
        };
        SubCollumns(subColumns2, w, h, _size.y + 1, board);
        BetUpdated();
    }

    private void SubCollumns(RouletteData[] subCollums, float w, float h, int row, VisualElement root)
    {
        for (int i = 0; i < subCollums.Length; i++)
        {
            var wLarge = w * _size.x / subCollums.Length;
            var columnBet = NewBetTile(wLarge, h, i * wLarge + w, row * h, green,
                subCollums[i], () => Debug.Log("Column bet clicked!"));
            root.Add(columnBet);
        }
    }

    public delegate void CallbackDelegate();

    private TemplateContainer NewBetTile(float w, float h, float left, float top, Color color, RouletteData data,
        CallbackDelegate callback = null)
    {
        callback += () =>
        {
            data.betAmount = currentBetAmount;
            bet.AddBet(data);
            BetUpdated();
        };
        var label = data.label;
        var t = NewHelperTile(w, h, left, top, color, callback, label);
        return t;
    }

    private void BetUpdated()
    {
        bet.ProcessBets(out float EV, out float variance, out float totalBet);
        Debug.Log($"EV: {EV}, Variance: {variance}, Total Bet: {totalBet}");
        evText.text = $"EV: {EV:F3}";
        varText.text = $"Var: {variance:F3}";
        betText.text = $"Bet: {totalBet}";
        if (totalBet > 0)
            _bell.UpdateCurve(EV, Mathf.Sqrt(variance / totalBet));
        else
            _bell.Reset();
        _bellParent.MarkDirtyRepaint();
    }

    private TemplateContainer NewHelperTile(float w, float h, float left, float top, Color color,
        CallbackDelegate callback, string label)
    {
        var t = _tile.Instantiate();
        t.style.position = Position.Absolute;
        t.style.width = Length.Percent(w);
        t.style.height = Length.Percent(h);
        t.style.left = Length.Percent(left);
        t.style.top = Length.Percent(top);
        var button = t.Q<Button>();
        button.style.backgroundColor = color;
        button.style.fontSize = Length.Percent(Mathf.Min(50, 50 * 3f / label.Length));
        button.text = label;
        button.clicked += () => callback?.Invoke();
        return t;
    }
}