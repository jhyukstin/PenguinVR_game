using TMPro;
using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    private const string BEST_SCORE_KEY = "BEST_SCORE";

    [Header("Score Settings")]
    [SerializeField] private float maxScore = 1000f;
    [SerializeField] private float fullScoreTime = 10f;
    [SerializeField] private float lambda = 0.04f;
    [SerializeField] private int scoreStep = 10;

    [Header("Score UI")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Test")]
    [SerializeField] private bool testOnStart = true;
    [SerializeField] private float testTime = 23f;

    public int CurrentScore { get; private set; }
    public int BestScore => PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);

    private void Start()
    {
        UpdateScoreUI();

        if (testOnStart)
        {
            SubmitScore(testTime);

            Debug.Log("Clear Time: " + testTime + "s");
            Debug.Log("Score: " + CurrentScore);
            Debug.Log("Best Score: " + BestScore);
        }
    }

    public int CalculateScore(float timeSeconds)
    {
        if (timeSeconds <= fullScoreTime)
        {
            return Mathf.RoundToInt(maxScore);
        }

        float rawScore = maxScore * Mathf.Exp(-lambda * (timeSeconds - fullScoreTime));

        // 1의 자리 버림: 594 -> 590
        int finalScore = ((int)rawScore / scoreStep) * scoreStep;

        return Mathf.Max(0, finalScore);
    }

    public bool SubmitScore(float timeSeconds)
    {
        CurrentScore = CalculateScore(timeSeconds);

        if (CurrentScore > BestScore)
        {
            PlayerPrefs.SetInt(BEST_SCORE_KEY, CurrentScore);
            PlayerPrefs.Save();

            Debug.Log("NEW BEST SCORE!");
            UpdateScoreUI();
            return true;
        }

        UpdateScoreUI();
        return false;
    }

    public void ResetBestScore()
    {
        PlayerPrefs.DeleteKey(BEST_SCORE_KEY);
        PlayerPrefs.Save();

        Debug.Log("Best Score Reset");
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = CurrentScore.ToString();
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = BestScore.ToString();
        }
    }
}
