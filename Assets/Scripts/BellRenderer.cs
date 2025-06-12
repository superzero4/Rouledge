using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class BellRenderer : MonoBehaviour
{
    [Range(-1, 1f)] public float center;

    [Range(0, 500f)] public float deviation;
    public float totalBet;
    public Vector2 scale;
    public Vector3 offset;
    [Range(0, 5f)] public float width = 1f;
    [Range(0, 10000)] public int resolution = 100;
    [Range(0f, 10f)] public float xSampleArea = 4;
    public Color positive;
    public Color negative;
    public LineRenderer lineRenderer;
    public LineRenderer axis;
    private VisualElement _root;
    private bool dirty = true;

    public VisualElement Root
    {
        get { return _root; }
        set
        {
            _root = value;
            _root.generateVisualContent += OnGenerateVisualContent;
        }
    }

    private void OnValidate()
    {
        UpdateCurve();
    }

    public void UpdateCurve()
    {
        UpdateCurve(center, deviation);
    }


    public void UpdateCurve(float center, float deviation, float totalBet = 0f)
    {
        this.center = center;
        this.deviation = deviation;
        this.totalBet = totalBet;
        _root?.MarkDirtyRepaint();
        dirty = true;
        return;
        //lineRenderer.positionCount = resolution + 1;
        //lineRenderer.widthMultiplier = width;
        ////scale = new Vector2(1f / deviation, 1f);
        //SetLine(lineRenderer, Curve(center, deviation));
        //SetLine(axis, axisPoints );
    }

    private float deviationScale => xSampleArea * deviation;

    private IEnumerable<Vector3> Curve()
    {
        scale.x = 1f / deviationScale;
        return Enumerable.Range(0, resolution + 1).Select(i =>
        {
            float normalized = (i / (float)resolution) * 2 - 1;
            float x = deviationScale * normalized;
            float y = Mathf.Exp(-Mathf.Pow(x - center, 2) / (2 * Mathf.Pow(deviation, 2)));
            return new Vector3(x, y, 0);
        });
    }

    public void SetLine(LineRenderer line, IEnumerable<Vector3> points)
    {
        var arr = points.Select(p =>
                transform.parent.localToWorldMatrix.MultiplyPoint3x4(offset + new Vector3(p.x * scale.x, p.y * scale.y,
                    0)))
            .ToArray();
        line.positionCount = arr.Length;
        line.SetPositions(arr);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="painter"></param>
    /// <param name="points">In the [-1,-1]x[0,1] draw space</param>
    public void SetLine(Painter2D painter, IEnumerable<Vector3> points, Vector3 scale, bool fill = false)
    {
        //Debug.Log($"OnGenerateVisualContent: w={w}, h={h}");
        var w = _root.worldBound.width;
        var h = _root.worldBound.height;
        var arr = points.Select(point =>
            offset + new Vector3(((point.x * scale.x + 1) / 2f) * w, (1 - point.y) * scale.y * h, 0)).ToArray();
        painter.BeginPath();
        painter.MoveTo(arr[0]);
        foreach (var point in arr.Skip(1))
        {
            painter.LineTo(point);
        }

        painter.MoveTo(arr[0]);
        painter.ClosePath();
        if (fill)
        {
            painter.Fill();
        }
        else
        {
            painter.Stroke();
        }
    }

    public void Reset()
    {
        UpdateCurve(0, 1);
    }

    public void OnGenerateVisualContent(MeshGenerationContext obj)
    {
        if (!dirty)
            return;
        var paint = obj.painter2D;
        paint.lineCap = LineCap.Round;
        paint.lineJoin = LineJoin.Bevel;
        paint.lineWidth = width;

        paint.strokeColor = Color.white;
        paint.fillColor = Color.gray;
        var curv = Curve().ToList();
        SetLine(paint, curv, scale, false);
        Vector3 breakPoint = Vector3.zero;
        breakPoint.x = totalBet;
        Debug.Log(" totalBet: " + totalBet + " => breakPoint: " + breakPoint.x);
        int selection = (int)(curv.Count * (1 + breakPoint.x / deviationScale) / 2);
        var half1 = curv.Take(selection);
        var half2 = curv.Skip(selection);
        paint.fillColor = negative;
        SetLine(paint, half1.Prepend(deviationScale * Vector3.left).Append(breakPoint), scale,
            true);
        paint.fillColor = positive;
        SetLine(paint, half2.Prepend(breakPoint).Append(deviationScale * Vector3.right), scale, true);

        paint.strokeColor = Color.black;
        paint.lineCap = LineCap.Butt;
        paint.lineJoin = LineJoin.Round;
        Vector3[] axisPoints =
        {
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 1f, 0)
        };
        SetLine(paint, axisPoints, Vector3.one);

        _root.Q<Label>().style.left = Length.Percent(100 * (center / deviationScale + 1) / 2);
    }
}