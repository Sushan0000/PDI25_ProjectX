using TMPro;
using UnityEngine;

public class EnemyTrackerHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private TMP_Text remainingText;

    [SerializeField]
    private TMP_Text killedText;

    [SerializeField]
    private TMP_Text totalText;

    [Header("Colors")]
    [SerializeField]
    private Color mainColor = new Color32(128, 249, 255, 255); // cyan

    [SerializeField]
    private Color lowColor = new Color32(255, 96, 96, 255); // bright red

    [SerializeField]
    private int lowRemainingThreshold = 5;

    private void Reset()
    {
        // auto-wire if names contain these words
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            string n = t.name.ToLower();
            if (n.Contains("remain"))
                remainingText = t;
            else if (n.Contains("kill"))
                killedText = t;
            else if (n.Contains("total"))
                totalText = t;
        }
    }

    private void OnEnable()
    {
        MechMutantEnemy.EnemyCountsChanged += Refresh;
        Refresh(); // initial fill
    }

    private void OnDisable()
    {
        MechMutantEnemy.EnemyCountsChanged -= Refresh;
    }

    private void Refresh()
    {
        int totalSpawned = MechMutantEnemy.TotalSpawned;
        int totalKilled = MechMutantEnemy.TotalKilled;
        int remaining = Mathf.Max(0, totalSpawned - totalKilled);

        Color remainingColor = remaining <= lowRemainingThreshold ? lowColor : mainColor;

        if (remainingText != null)
        {
            remainingText.color = remainingColor;
            remainingText.text = $"REMAINING: <size=150%><b>{remaining}</b></size>";
        }

        if (killedText != null)
        {
            killedText.color = mainColor;
            killedText.text = $"KILLED: {totalKilled}";
        }

        if (totalText != null)
        {
            totalText.color = mainColor;
            totalText.text = $"TOTAL: {totalSpawned}";
        }
    }

    // Optional, if you ever want to force update from a button
    public void ForceRefresh() => Refresh();
}
