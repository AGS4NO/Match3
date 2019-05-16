using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    public bool isGameOver
    {
        get
        {
            return mIsGameOver;
        }
        set
        {
            mIsGameOver = value;
        }
    }

    public int movesLeft = 30;
    public int scoreGoal = 10000;

    public MessageWindow messageWindow;

    public ScreenFader screenFader;

    public Sprite goalIcon;
    public Sprite loseIcon;
    public Sprite winIcon;

    public Text levelNameText;
    public Text movesLeftText;

    private Board mBoard;

    private bool mIsGameOver = false;
    private bool mIsReadyToBegin = false;
    private bool mIsReadyToReload = false;
    private bool mIsWinner = false;

    // Use this for initialization
    private void Start()
    {
        mBoard = GameObject.FindObjectOfType<Board>().GetComponent<Board>();

        Scene scene = SceneManager.GetActiveScene();

        if (levelNameText != null)
        {
            levelNameText.text = scene.name;
        }

        UpdateMoves();

        StartCoroutine("ExecuteGameLoop");
    }

    public void BeginGame()
    {
        mIsReadyToBegin = true;
    }

    public void ReloadScene()
    {
        mIsReadyToReload = true;
    }

    public void UpdateMoves()
    {
        if (movesLeftText != null)
        {
            movesLeftText.text = movesLeft.ToString();
        }
    }

    private IEnumerator ExecuteGameLoop()
    {
        yield return StartCoroutine("StartGameRoutine");
        yield return StartCoroutine("PlayGameRoutine");
        yield return StartCoroutine("WaitForBoardRoutine", 0f);
        yield return StartCoroutine("EndGameRoutine");
    }

    private IEnumerator StartGameRoutine()
    {
        if (messageWindow != null)
        {
            messageWindow.GetComponent<RectXformMover>().MoveOn();
            messageWindow.ShowMessage(goalIcon, "points goal\n" + scoreGoal.ToString(), "start");
        }

        while (!mIsReadyToBegin)
        {
            yield return null;
        }

        if (screenFader != null)
        {
            screenFader.FadeOff();
        }

        yield return new WaitForSeconds(0.5f);

        if (mBoard != null)
        {
            mBoard.SetupBoard();
        }
    }

    private IEnumerator PlayGameRoutine()
    {
        while (!mIsGameOver)
        {
            if (ScoreManager.Instance != null)
            {
                if (ScoreManager.Instance.CurrentScore >= scoreGoal)
                {
                    mIsGameOver = true;
                    mIsWinner = true;
                }
            }

            if (movesLeft == 0)
            {
                mIsGameOver = true;
                mIsWinner = false;
            }

            yield return null;
        }
    }

    private IEnumerator EndGameRoutine()
    {
        mIsReadyToReload = false;

        if (mIsWinner)
        {
            if (messageWindow != null)
            {
                messageWindow.GetComponent<RectXformMover>().MoveOn();
                messageWindow.ShowMessage(winIcon, "YOU WIN!", "OK");
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayWinSound();
            }
        }
        else
        {
            if (messageWindow != null)
            {
                messageWindow.GetComponent<RectXformMover>().MoveOn();
                messageWindow.ShowMessage(loseIcon, "YOU LOSE!", "OK");
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayLoseSound();
            }
        }

        yield return new WaitForSeconds(1f);

        if (screenFader != null)
        {
            screenFader.FadeOn();
        }

        while (!mIsReadyToReload)
        {
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator WaitForBoardRoutine(float delay = 0.5f)
    {
        if (mBoard != null)
        {
            // Wait for the board swap time delay
            yield return new WaitForSeconds(mBoard.swapTime);

            while (mBoard.isRefilling)
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(delay);
    }
}