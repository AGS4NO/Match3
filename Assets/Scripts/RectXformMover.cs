using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RectXformMover : MonoBehaviour
{
    public Vector3 startPosition;
    public Vector3 onScreenPosition;
    public Vector3 endPosition;

    public float timeToMove = 1f;

    private RectTransform mRectXform;

    private bool mIsMoving = false;

    private void Awake()
    {
        mRectXform = GetComponent<RectTransform>();
    }

    private void Move(Vector3 startPos, Vector3 endPos, float timeToMove)
    {
        if (!mIsMoving)
        {
            StartCoroutine(MoveRoutine(startPos, endPos, timeToMove));
        }
    }

    public void MoveOn()
    {
        Move(startPosition, onScreenPosition, timeToMove);
    }

    public void MoveOff()
    {
        Move(onScreenPosition, endPosition, timeToMove);
    }

    private IEnumerator MoveRoutine(Vector3 startPos, Vector3 endPos, float timeToMove)
    {
        if (mRectXform != null)
        {
            mRectXform.anchoredPosition = startPos;
        }

        bool reachedDestination = false;

        float elapsedTime = 0f;

        mIsMoving = true;

        while (!reachedDestination)
        {
            if (Vector3.Distance(mRectXform.anchoredPosition, endPos) < 0.01f)
            {
                reachedDestination = true;
                break;
            }

            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp(elapsedTime / timeToMove, 0f, 1f);

            t = t * t * t * (t * (t * 6 - 15) + 10);

            if (mRectXform != null)
            {
                mRectXform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            }

            yield return null;
        }

        mIsMoving = false;
    }
}