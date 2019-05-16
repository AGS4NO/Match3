using System.Collections;
using UnityEngine;

public enum TileType
{
    Breakable,
    Normal,
    Obstacle
}

[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour
{
    public int breakableValue;
    public int xIndex;
    public int yIndex;

    public Color normalColor;
    public Sprite[] breakableSprites;
    public TileType tileType = TileType.Normal;

    private Board mBoard;
    private SpriteRenderer mSpriteRenderer;

    private void Awake()
    {
        mSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public void BreakTile()
    {
        if (tileType != TileType.Breakable)
        {
            return;
        }

        StartCoroutine(BreakTileRoutine());
    }

    private IEnumerator BreakTileRoutine()
    {
        breakableValue = Mathf.Clamp(breakableValue--, 0, breakableValue);

        yield return new WaitForSeconds(0.25f);

        if (breakableSprites[breakableValue] != null)
        {
            mSpriteRenderer.sprite = breakableSprites[breakableValue];
        }

        if (breakableValue == 0)
        {
            tileType = TileType.Normal;
            mSpriteRenderer.color = normalColor;
        }
    }

    public void Initialize(int x, int y, Board board)
    {
        xIndex = x;
        yIndex = y;
        mBoard = board;

        if (tileType == TileType.Breakable)
        {
            if (breakableSprites[breakableValue] != null)
            {
                mSpriteRenderer.sprite = breakableSprites[breakableValue];
            }
        }
    }

    private void OnMouseDown()
    {
        if (mBoard != null)
        {
            mBoard.ClickTile(this);
        }
    }

    private void OnMouseEnter()
    {
        if (mBoard != null)
        {
            mBoard.DragToTile(this);
        }
    }

    private void OnMouseUp()
    {
        if (mBoard != null)
        {
            mBoard.ReleaseTile();
        }
    }
}