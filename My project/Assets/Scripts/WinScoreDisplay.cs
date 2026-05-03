using TMPro;
using UnityEngine;

public class WinScoreDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject successUI;
    [SerializeField] private TMP_Text core;
    [SerializeField] private TMP_Text B_score;

    [Header("Behavior")]
    [SerializeField] private bool refreshWhenSuccessUIAppears = true;

    private bool hasDisplayed;

    private void OnEnable()
    {
        if (!refreshWhenSuccessUIAppears || IsSuccessUIVisible())
        {
            Refresh();
        }
    }

    private void Update()
    {
        if (hasDisplayed || !refreshWhenSuccessUIAppears || !IsSuccessUIVisible())
        {
            return;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (core != null)
        {
            core.text = ScoreSystem.SavedCurrentScore.ToString();
        }

        if (B_score != null)
        {
            B_score.text = ScoreSystem.SavedBestScore.ToString();
        }

        hasDisplayed = true;
    }

    private bool IsSuccessUIVisible()
    {
        return successUI == null || successUI.activeInHierarchy;
    }
}
