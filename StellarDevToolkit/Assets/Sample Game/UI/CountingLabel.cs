using System;
using TMPro;
using UnityEngine;

public sealed class CountingLabel : MonoBehaviour
{
    const float MinDuration = 0.45f;
    const float MaxDuration = 0.8f;
    const float UnitsPerSecond = 24f;
    const int MaxStepsPerFrame = 3;

    TextMeshProUGUI label;
    Func<int, string> format;
    int displayed;
    int target;
    float carry;

    public static CountingLabel Bind(TextMeshProUGUI label, Func<int, string> format = null)
    {
        if (label == null)
        {
            return null;
        }

        CountingLabel counting = label.GetComponent<CountingLabel>();
        if (counting == null)
        {
            counting = label.gameObject.AddComponent<CountingLabel>();
        }

        counting.label = label;
        counting.format = format ?? (value => value.ToString());
        return counting;
    }

    public void SetImmediate(int value)
    {
        displayed = value;
        target = value;
        carry = 0f;
        Apply(displayed);
    }

    public void Set(int value)
    {
        if (value <= displayed)
        {
            SetImmediate(value);
            return;
        }

        target = value;
    }

    public void Kill()
    {
        target = displayed;
        carry = 0f;
    }

    void Update()
    {
        if (displayed >= target || label == null)
        {
            return;
        }

        int remaining = target - displayed;
        float duration = Mathf.Clamp(remaining / UnitsPerSecond, MinDuration, MaxDuration);
        float interval = duration / remaining;
        carry += Time.unscaledDeltaTime;

        int steps = 0;
        while (displayed < target && carry >= interval && steps < MaxStepsPerFrame)
        {
            carry -= interval;
            displayed++;
            steps++;
        }

        if (steps > 0)
        {
            Apply(displayed);
        }
    }

    void Apply(int value)
    {
        if (label != null && format != null)
        {
            label.text = format(value);
        }
    }
}
