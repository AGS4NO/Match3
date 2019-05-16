using System.Collections;
using UnityEngine;

public enum MatchValue
{
    Blue,
    Cyan,
    Green,
    Indigo,
    Magenta,
    None,
    Orange,
    Purple,
    Red,
    Teal,
    Yellow,
    Wild
}

public class GamePiece : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    public AudioClip clearSound;

    // Interpolation type enumerator
    public enum InterpolationType
    {
        EaseIn,
        EaseOut,
        Linear,
        SmoothStep,
        SmootherStep
    };

    public InterpolationType interpolation = InterpolationType.SmootherStep;

    public MatchValue matchValue;

    public int scoreValue = 20;

    private Board mBoard;

    private bool mIsMoving = false;

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public void ChangeColor(GamePiece pieceToMatch)
    {
        SpriteRenderer rendererToChange = GetComponent<SpriteRenderer>();

        Color colorToMatch = Color.clear;

        if (pieceToMatch != null)
        {
            SpriteRenderer rendererToMatch = pieceToMatch.GetComponent<SpriteRenderer>();

            if (rendererToMatch != null && rendererToChange != null)
            {
                rendererToChange.color = rendererToMatch.color;
            }

            matchValue = pieceToMatch.matchValue;
        }
    }

    public void Initialize(Board board)
    {
        mBoard = board;
    }

    // Move a game piece
    public void Move(int destX, int destY, float timeToMove)
    {
        if (!mIsMoving)
        {
            StartCoroutine(MoveRoutine(new Vector3(destX, destY, 0), timeToMove));
        }
    }

    // Coroutine to move a game piece fluidly during timeToMove
    private IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
    {
        Vector3 startPosition = transform.position;

        bool reachedDestination = false;

        float elapsedTime = 0f;

        mIsMoving = true;

        while (!reachedDestination)
        {
            if (Vector3.Distance(transform.position, destination) < 0.01f)
            {
                reachedDestination = true;

                if (mBoard != null)
                {
                    mBoard.PlaceGamePiece(this, (int)destination.x, (int)destination.y);
                }

                break;
            }

            // Track the running time
            elapsedTime += Time.deltaTime;

            // Calculate the Lerp value
            float t = Mathf.Clamp(elapsedTime / timeToMove, 0f, 1f);

            // Interpolation switch
            switch (interpolation)
            {
                case InterpolationType.EaseIn:
                    t = 1 - Mathf.Cos(t * Mathf.PI * 0.5f);
                    break;

                case InterpolationType.EaseOut:
                    t = Mathf.Sin(t * Mathf.PI * 0.5f);
                    break;

                case InterpolationType.Linear:
                    break;

                case InterpolationType.SmoothStep:
                    t = t * t * (3 - 2 * t);
                    break;

                case InterpolationType.SmootherStep:
                    t = t * t * t * (t * (t * 6 - 15) + 10);
                    break;
            }

            // Move the game piece
            transform.position = Vector3.Lerp(startPosition, destination, t);

            // Wait until next frame
            yield return null;
        }

        mIsMoving = false;
    }

    public void ScorePoints(int multiplier = 1, int bonus = 0)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue * multiplier + bonus);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClipAtPoint(clearSound, Vector3.zero, SoundManager.Instance.fxVolume);
        }
    }

    // Set game piece coordinates
    public void SetCoordinates(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }
}