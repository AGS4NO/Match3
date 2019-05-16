using System.Collections;
using UnityEngine.UI;

public class ScoreManager : Singleton<ScoreManager>
{
    private int mCurrentScore = 0;
    private int mCounterValue = 0;
    private int mIncrement = 5;

    public int CurrentScore
    {
        get
        {
            return mCurrentScore;
        }
    }

    public Text scoreText;

    // Use this for initialization
    private void Start()
    {
        UpdateScoreText(mCurrentScore);
    }

    public void AddScore(int value)
    {
        mCurrentScore += value;
        StartCoroutine(CountScoreRoutine());
    }

    private IEnumerator CountScoreRoutine()
    {
        int iterations = 0;

        while (mCounterValue < mCurrentScore && iterations < 100000)
        {
            mCounterValue += mIncrement;
            UpdateScoreText(mCounterValue);
            iterations++;
            yield return null;
        }

        mCounterValue = mCurrentScore;
        UpdateScoreText(mCurrentScore);
    }

    public void UpdateScoreText(int scoreValue)
    {
        if (scoreText != null)
        {
            scoreText.text = scoreValue.ToString();
        }
    }
}