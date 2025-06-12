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
    public VisualTreeAsset _chip;
    [Header("Layout")] public Vector2Int _size;
    public int _rightAdditionalTiles;
    public int _bottomAdditionalTiles;
    [Range(0, 1f)] public float _chipPercentage = 0.5f; // Percentage of the tile size for the chip

    [Header("Betting"), Range(0, 1000), SerializeField]
    private int initialBetAmount = 10;

    private int currentBetAmount;

    [SerializeReference] private Bet bet = new Bet();
    [Header("Colors")] public Color green = new Color(0, .5f, 0);


    private Label evText;
    private Label varText;
    private Label betText;
    private VisualElement _bellParent;
    private float _cellSize = -1f;
    private TemplateContainer _zero;
    const string chipName = "Chip on ";


    void Start()
    {
        currentBetAmount = initialBetAmount;
        var board = _root.rootVisualElement.Q<VisualElement>("Board");
        evText = _root.rootVisualElement.Q<Label>("EV");
        varText = _root.rootVisualElement.Q<Label>("Var");
        betText = _root.rootVisualElement.Q<Label>("Bet");
        _bellParent = _root.rootVisualElement.Q<VisualElement>("Graph");
        _bell.Root = _bellParent;
        //+ 1 is the zero on the left
        float w = 100f / (_size.x + 1 + _rightAdditionalTiles);
        float h = 100f / (_size.y + _bottomAdditionalTiles);
        _zero = NewBetTile(w, 100f, 0, 0, green, new RouletteData(0, 35));
        board.Add(_zero);
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

        var font = FontSize("2 to 1") * .6f;
        for (int j = 0; j < _size.y; j++)
        {
            var lineBet = NewBetTile(w, h, (_size.x + 1) * w, j * h, green,
                new RouletteData(Enumerable.Range(0, 12).Select(s => j + 1 + s * 3), "2 to 1", 2), font);
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
                callback = (t) =>
                {
                    currentBetAmount *= 2;
                    Debug.Log($"Current bet amount increased to: {currentBetAmount}");
                };
            }
            else
            {
                data = "Clear";
                callback = (t) =>
                {
                    currentBetAmount = initialBetAmount;
                    bet.ClearBets();
                    foreach (var chip in board.Query<VisualElement>(chipName).Build())
                    {
                        chip.style.visibility = Visibility.Hidden;
                        chip.RemoveFromHierarchy();
                    }

                    BetUpdated();
                };
            }

            var button = NewHelperTile(w, h, (_size.x + 1) * w, j * h, Color.grey, callback, data,
                FontSize("Clear") * .5f);
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
        float size = FontSize(subCollums.Select(s => s.label));
        for (int i = 0; i < subCollums.Length; i++)
        {
            var wLarge = w * _size.x / subCollums.Length;
            var columnBet = NewBetTile(wLarge, h, i * wLarge + w, row * h, green,
                subCollums[i], size);
            root.Add(columnBet);
        }
    }

    private static float FontSize(params string[] names)
    {
        return FontSize(names.AsEnumerable());
    }

    private static float FontSize(IEnumerable<string> names)
    {
        float size = Mathf.Min(50f, 50f * 4f / names.Max(s => s.Length));
        return size;
    }

    public delegate void CallbackDelegate(TemplateContainer tile);

    private TemplateContainer NewBetTile(float w, float h, float left, float top, Color color, RouletteData data,
        float fontSize = 50f,
        CallbackDelegate callback = null)
    {
        callback += t => OnTileClicked(data, t);
        var label = data.label;
        var tile = NewHelperTile(w, h, left, top, color, callback, label, fontSize);
        return tile;
    }

    private TemplateContainer NewHelperTile(float w, float h, float left, float top, Color color,
        CallbackDelegate callback, string label, float fontSize = 50f)
    {
        var t = _tile.Instantiate();
        t.style.position = Position.Absolute;
        t.style.width = Length.Percent(w);
        t.style.height = Length.Percent(h);
        t.style.left = Length.Percent(left);
        t.style.top = Length.Percent(top);
        t.name = "Tile " + label;
        var button = t.Q<Button>();
        button.style.backgroundColor = color;
        button.style.fontSize = fontSize;
        button.text = label;
        button.clicked += () => callback?.Invoke(t);
        return t;
    }

    private void OnTileClicked(RouletteData data, TemplateContainer tile)
    {
        data.betAmount = currentBetAmount;
        bet.AddBet(data);
        BetUpdated();
        UpdateChip(data, tile);
    }

    private void UpdateChip(RouletteData data, TemplateContainer tile)
    {
        var chip = tile.Q<VisualElement>(chipName);
        if (chip == null)
        {
            if (_cellSize <= 0f)
                _cellSize = _zero.resolvedStyle.width;
            chip = _chip.Instantiate();
            chip.style.position = Position.Absolute;
            var size = _cellSize * _chipPercentage;
            chip.style.left = (tile.resolvedStyle.width - size) / 2;
            chip.style.width = size;
            chip.style.top = (tile.resolvedStyle.height - size) / 2;
            chip.style.height = size;
            chip.Q<Label>("Amount").text = "0";
            //Forward back to parent
            chip.Q<Button>().clicked += () => { OnTileClicked(data, tile); };
            tile.Add(chip);
        }

        chip.visible = true;
        chip.style.visibility = Visibility.Visible;
        var chipLabel = chip.Q<Label>("Amount");
        var parsed = int.Parse(chipLabel.text);
        chipLabel.text = (parsed + data.betAmount).ToString();
        chip.name = chipName; //+ tile.name;//Same as looked for in query for later find
    }

    private void BetUpdated()
    {
        bet.ProcessBets(out float EV, out float deviation, out float totalBet);
        Debug.Log($"EV: {EV}, Variance: {deviation}, Total Bet: {totalBet}");
        evText.text = $"Average win: {EV:F3}$";
        varText.text = $"Dev: {deviation:F3}$";
        betText.text = $"Bet: {totalBet}$";
        if (totalBet > 0)
            _bell.UpdateCurve(EV, deviation, totalBet);
        else
            _bell.Reset();
        _bellParent.MarkDirtyRepaint();
    }
}