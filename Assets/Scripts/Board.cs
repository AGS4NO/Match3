using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoardDeadlock))]
public class Board : MonoBehaviour
{
    public int borderSize;

    public int fillYOffset = 10;
    public float fillMoveTime = 0.5f;

    public int height;
    public int width;

    [Range(0, 1)]
    public float chanceForCollectible = 0.1f;
    public int collectibleCount = 0;
    public int maxCollectibles = 3;

    public bool isRefilling = false;

    public float swapTime = 0.5f;

    public GameObject[] gamePiecePrefabs;
    public GameObject[] collectiblePrefabs;
    public GameObject colorBombPrefab;

    public GameObject[] adjacentBombPrefabs;
    public GameObject[] columnBombPrefabs;
    public GameObject[] rowBombPrefabs;

    public GameObject tileNormalPrefab;
    public GameObject tileObstaclePrefab;

    public StartingObject[] startingTiles;
    public StartingObject[] startingGamePieces;

    [System.Serializable]
    public class StartingObject
    {
        public GameObject prefab;
        public int x;
        public int y;
        public int z;
    }

    private bool mPlayerInputEnabled = true;
    private int  mScoreMultiplier = 0;

    private BoardDeadlock mBoardDeadlock;

    private GamePiece[,] mAllGamePieces;
    
    private GameObject mClickedTileBomb;
    private GameObject mTargetTileBomb;

    private ParticleManager mParticleManager;

    private Tile[,] mAllTiles;
    private Tile mClickedTile;
    private Tile mTargetTile;

    // Use this for initialization
    private void Start()
    {
        mAllTiles = new Tile[width, height];
        mAllGamePieces = new GamePiece[width, height];
        mParticleManager = GameObject.FindWithTag("ParticleManager").GetComponent<ParticleManager>();
        mBoardDeadlock = GetComponent<BoardDeadlock>();
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void ActivateBomb(GameObject bomb)
    {
        int x = (int)bomb.transform.position.x;
        int y = (int)bomb.transform.position.y;

        if (IsWithinBounds(x, y))
        {
            mAllGamePieces[x, y] = bomb.GetComponent<GamePiece>();
        }
    }

    // Break a tile at given coordinates
    private void BreakTileAt(int x, int y)
    {
        Tile tileToBreak = mAllTiles[x, y];

        if (tileToBreak != null && tileToBreak.tileType == TileType.Breakable)
        {
            if (mParticleManager != null)
            {
                mParticleManager.BreakTileFXAt(tileToBreak.breakableValue, x, y, 0);
            }

            tileToBreak.BreakTile();
        }
    }

    // Overload
    private void BreakTileAt(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                BreakTileAt(piece.xIndex, piece.yIndex);
            }
        }
    }

    // Checks if adding a collectible object is valid based on the ruleset
    private bool CanAddCollectible()
    {
        return (UnityEngine.Random.Range(0f, 1f) <= chanceForCollectible && collectiblePrefabs.Length > 0 && collectibleCount < maxCollectibles);
    }

    // Clear game piece objects with valid matches and collapse the remaining game object pieces
    private IEnumerator ClearAndCollapseRoutine(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        List<GamePiece> matches = new List<GamePiece>();

        yield return new WaitForSeconds(0.1f);

        bool isFinished = false;

        while (!isFinished)
        {
            List<GamePiece> bombedPieces = GetBombedPieces(gamePieces);
            gamePieces = gamePieces.Union(bombedPieces).ToList();

            bombedPieces = GetBombedPieces(gamePieces);
            gamePieces = gamePieces.Union(bombedPieces).ToList();

            List<GamePiece> collectedPieces = FindCollectiblesAt(0, true);

            List<GamePiece> allCollectibles = FindAllCollectibles();
            List<GamePiece> blockers = gamePieces.Intersect(allCollectibles).ToList();
            collectedPieces = collectedPieces.Union(blockers).ToList();
            collectibleCount -= collectedPieces.Count;

            gamePieces = gamePieces.Union(collectedPieces).ToList();

            List<int> columnsToCollapse = GetColumns(gamePieces);

            // Clear the list of game piece objects including game piece objects affected by bombs
            ClearPieceAt(gamePieces, bombedPieces);
            // Break tiles that may exist under a game piece
            BreakTileAt(gamePieces);

            if (mClickedTileBomb != null)
            {
                ActivateBomb(mClickedTileBomb);
                mClickedTileBomb = null;
            }

            if (mTargetTileBomb != null)
            {
                ActivateBomb(mTargetTileBomb);
                mTargetTileBomb = null;
            }

            yield return new WaitForSeconds(0.1f);

            movingPieces = CollapseColumn(columnsToCollapse);

            while (!IsCollapsed(movingPieces))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            matches = FindMatchesAt(movingPieces);
            collectedPieces = FindCollectiblesAt(0, true);

            matches = matches.Union(collectedPieces).ToList();

            if (matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else
            {
                mScoreMultiplier++;

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBonusSound();
                }

                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }
        }

        yield return null;
    }

    // Clear game piece objects and refill empty tiles with new game piece objects
    private void ClearAndRefillBoard(List<GamePiece> gamePieces)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gamePieces));
    }

    private IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces)
    {
        mPlayerInputEnabled = false;

        isRefilling = true;

        List<GamePiece> matches = gamePieces;

        mScoreMultiplier = 0;

        do
        {
            mScoreMultiplier++;

            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            yield return null;

            yield return StartCoroutine(RefillBoardRoutine());

            matches = FindAllMatches();

            yield return new WaitForSeconds(0.1f);
        }
        while (matches.Count != 0);

        if (mBoardDeadlock.IsDeadlocked(mAllGamePieces, 3))
        {
            yield return new WaitForSeconds(1f);

            ClearBoard();

            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(RefillBoardRoutine());
        }

        mPlayerInputEnabled = true;

        isRefilling = false;
    }

    // Clear all game piece objects from the board
    private void ClearBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                ClearPieceAt(i, j);

                if (mParticleManager != null)
                {
                    mParticleManager.ClearPieceFXAt(i, j);
                }
            }
        }
    }

    // Clear a game piece from the given coordinates
    private void ClearPieceAt(int x, int y)
    {
        GamePiece pieceToClear = mAllGamePieces[x, y];

        if (pieceToClear != null)
        {
            mAllGamePieces[x, y] = null;
            Destroy(pieceToClear.gameObject);
        }

        //HighlightTileOff(x, y);
    }

    // Overload to pass a list of game piece objects to the clear method
    private void ClearPieceAt(List<GamePiece> gamePieces, List<GamePiece> bombedPieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                ClearPieceAt(piece.xIndex, piece.yIndex);
                piece.ScorePoints(mScoreMultiplier);

                if (mParticleManager != null)
                {
                    if (bombedPieces.Contains(piece))
                    {
                        mParticleManager.BombFXAt(piece.xIndex, piece.yIndex);
                    }
                    else
                    {
                        mParticleManager.ClearPieceFXAt(piece.xIndex, piece.yIndex);
                    }
                }
            }
        }
    }

    // Determine the source tile
    public void ClickTile(Tile tile)
    {
        if (mClickedTile == null)
        {
            mClickedTile = tile;
            Debug.Log("Source Tile: " + tile.name);
        }
    }

    // Collapse a column after clearing game piece objects
    private List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();

        for (int i = 0; i < height - 1; i++)
        {
            if (mAllGamePieces[column, i] == null && mAllTiles[column, i].tileType != TileType.Obstacle)
            {
                for (int j = i + 1; j < height; j++)
                {
                    if (mAllGamePieces[column, j] != null)
                    {
                        mAllGamePieces[column, j].Move(column, i, collapseTime * (j - i));
                        mAllGamePieces[column, i] = mAllGamePieces[column, j];
                        mAllGamePieces[column, i].SetCoordinates(column, i);

                        if (!movingPieces.Contains(mAllGamePieces[column, i]))
                        {
                            movingPieces.Add(mAllGamePieces[column, i]);
                        }

                        mAllGamePieces[column, j] = null;

                        break;
                    }
                }
            }
        }

        return movingPieces;
    }

    // Overload for collapse column method that accepts a list of game piece objects
    private List<GamePiece> CollapseColumn(List<GamePiece> gamePieces, float collapseTime = 0.1f)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        List<int> columnsToCollapse = GetColumns(gamePieces);

        foreach (int column in columnsToCollapse)
        {
            movingPieces = movingPieces.Union(CollapseColumn(column)).ToList();
        }

        return movingPieces;
    }

    // Overload for collapse column method that accepts a list of columns
    private List<GamePiece> CollapseColumn(List<int> columnsToCollapse)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();

        foreach (int column in columnsToCollapse)
        {
            movingPieces = movingPieces.Union(CollapseColumn(column)).ToList();
        }

        return movingPieces;
    }

    private GameObject CreateBomb(GameObject prefab, int x, int y)
    {
        if (prefab != null && IsWithinBounds(x, y))
        {
            GameObject bomb = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity) as GameObject;
            bomb.GetComponent<Bomb>().Initialize(this);
            bomb.GetComponent<Bomb>().SetCoordinates(x, y);
            bomb.transform.parent = transform;
            return bomb;
        }

        return null;
    }

    private void CreateGamePiece(GameObject prefab, int x, int y, int falseYOffset = 0, float moveTime = 0.1f)
    {
        if (prefab != null && IsWithinBounds(x, y))
        {
            prefab.GetComponent<GamePiece>().Initialize(this);
            PlaceGamePiece(prefab.GetComponent<GamePiece>(), x, y);

            if (falseYOffset != 0)
            {
                prefab.transform.position = new Vector3(x, y + falseYOffset, 0);
                prefab.GetComponent<GamePiece>().Move(x, y, moveTime);
            }

            prefab.transform.parent = transform;
        }
    }

    private void CreateTile(GameObject prefab, int x, int y, int z = 0)
    {
        if (prefab != null && IsWithinBounds(x, y))
        {
            GameObject tile = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity) as GameObject;

            tile.name = "Tile (" + x + "," + y + ")";

            tile.transform.parent = transform;

            mAllTiles[x, y] = tile.GetComponent<Tile>();

            mAllTiles[x, y].Initialize(x, y, this);
        }
    }

    // Determine the source tile
    public void DragToTile(Tile tile)
    {
        if (mClickedTile != null && IsAdjacent(tile, mClickedTile))
        {
            mTargetTile = tile;
            Debug.Log("Target Tile: " + tile.name);
        }
    }

    // Fill the board with random game piece objects
    // Generates no valid matches inside of the empty tile coordinates
    private void FillBoard(int falseYOffset = 0, float moveTime = 0.01f)
    {
        int maxIterations = 100;
        int iterations = 0;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (mAllGamePieces[i, j] == null && mAllTiles[i, j].tileType != TileType.Obstacle)
                {
                    if (j == height - 1 && CanAddCollectible())
                    {
                        FillRandomCollectibleAt(i, j, falseYOffset, moveTime);
                        collectibleCount++;
                    }
                    else
                    {
                        FillRandomGamePieceAt(i, j, falseYOffset, moveTime);

                        iterations = 0;

                        while (HasMatchOnFill(i, j))
                        {
                            ClearPieceAt(i, j);
                            FillRandomGamePieceAt(i, j, falseYOffset, moveTime);

                            iterations++;

                            if (iterations > maxIterations)
                            {
                                Debug.Log("Max game piece placement iterations performed on tile: " + i + "," + j + "!");
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    // Fill a tile with a random collectible object
    private GamePiece FillRandomCollectibleAt(int x, int y, int falseYOffset = 0, float moveTime = 0.1f)
    {
        if (IsWithinBounds(x, y))
        {
            GameObject randomPiece = Instantiate(GetRandomCollectible(), Vector3.zero, Quaternion.identity) as GameObject;

            CreateGamePiece(randomPiece, x, y, falseYOffset, moveTime);

            return randomPiece.GetComponent<GamePiece>();
        }

        return null;
    }

    // Fill a tile with a random game piece object
    private GamePiece FillRandomGamePieceAt(int x, int y, int falseYOffset = 0, float moveTime = 0.1f)
    {
        if (IsWithinBounds(x, y))
        {
            GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity) as GameObject;

            CreateGamePiece(randomPiece, x, y, falseYOffset, moveTime);

            return randomPiece.GetComponent<GamePiece>();
        }

        return null;
    }

    // Find consecutive matching pieces
    private List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();

        GamePiece startPiece = null;

        if (IsWithinBounds(startX, startY))
        {
            startPiece = mAllGamePieces[startX, startY];
        }

        if (startPiece != null)
        {
            matches.Add(startPiece);
        }
        else
        {
            return null;
        }

        int nextX;
        int nextY;

        int maxValue = (width > height) ? width : height;

        for (int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if (!IsWithinBounds(nextX, nextY))
            {
                break;
            }

            GamePiece nextPiece = mAllGamePieces[nextX, nextY];

            if (nextPiece == null)
            {
                break;
            }
            else
            {
                if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece) && nextPiece.matchValue != MatchValue.None)
                {
                    matches.Add(nextPiece);
                }
                else
                {
                    break;
                }
            }
        }
        if (matches.Count >= minLength)
        {
            return matches;
        }

        return null;
    }

    private List<GamePiece> FindAllMatches()
    {
        List<GamePiece> combinedMatches = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                List<GamePiece> matches = FindMatchesAt(i, j);

                combinedMatches = combinedMatches.Union(matches).ToList();
            }
        }

        return combinedMatches;
    }

    private List<GamePiece> FindAllMatchValues(MatchValue mValue)
    {
        List<GamePiece> foundPieces = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (mAllGamePieces[i, j].matchValue == mValue)
                {
                    foundPieces.Add(mAllGamePieces[i, j]);
                }
            }
        }

        return foundPieces;
    }

    private List<GamePiece> FindAllCollectibles()
    {
        List<GamePiece> foundCollectibles = new List<GamePiece>();

        for (int i = 0; i < height; i++)
        {
            List<GamePiece> collectibleRow = FindCollectiblesAt(i);

            foundCollectibles = foundCollectibles.Union(collectibleRow).ToList();
        }

        return foundCollectibles;
    }

    // Find game piece objects in a row
    private List<GamePiece> FindCollectiblesAt(int row, bool clearedAtBottomOnly = false)
    {
        List<GamePiece> foundCollectibles = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            if (mAllGamePieces[i, row] != null)
            {
                Collectible collectibleComponent = mAllGamePieces[i, row].GetComponent<Collectible>();

                if (collectibleComponent != null)
                {
                    if (!clearedAtBottomOnly || (clearedAtBottomOnly && collectibleComponent.clearedAtBottom))
                    {
                        foundCollectibles.Add(mAllGamePieces[i, row]);
                    }
                }
            }
        }

        return foundCollectibles;
    }

    private GameObject FindGamePieceByMatchValue(GameObject[] gamePiecePrefabs, MatchValue matchValue)
    {
        if (matchValue == MatchValue.None)
        {
            return null;
        }

        foreach (GameObject go in gamePiecePrefabs)
        {
            GamePiece piece = go.GetComponent<GamePiece>();

            if (piece != null)
            {
                if (piece.matchValue == matchValue)
                {
                    return go;
                }
            }
        }

        return null;
    }

    // Find matches for specified game piece
    private List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<GamePiece> hMatches = FindHorizontalMatches(x, y, minLength);
        List<GamePiece> vMatches = FindVerticalMatches(x, y, minLength);

        if (hMatches == null)
        {
            hMatches = new List<GamePiece>();
        }

        if (vMatches == null)
        {
            vMatches = new List<GamePiece>();
        }

        var combinedMatches = hMatches.Union(vMatches).ToList();

        return combinedMatches;
    }

    // Overload method to find matches using a list of game piece objects
    private List<GamePiece> FindMatchesAt(List<GamePiece> gamePieces, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();

        foreach (GamePiece piece in gamePieces)
        {
            matches = matches.Union(FindMatchesAt(piece.xIndex, piece.yIndex, minLength)).ToList();
        }

        return matches;
    }

    private MatchValue FindMatchValue(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                return piece.matchValue;
            }
        }

        return MatchValue.None;
    }

    // Find horizontal matches
    private List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);
        List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);

        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }

        if (rightMatches == null)
        {
            rightMatches = new List<GamePiece>();
        }

        var combinedMatches = leftMatches.Union(rightMatches).ToList();

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }

    // Find vertical matches
    private List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);
        List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);

        if (downwardMatches == null)
        {
            downwardMatches = new List<GamePiece>();
        }

        if (upwardMatches == null)
        {
            upwardMatches = new List<GamePiece>();
        }

        var combinedMatches = downwardMatches.Union(upwardMatches).ToList();

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }

    // Return a list of game piece objects that are adjacent to a given coordinate
    private List<GamePiece> GetAdjacentPieces(int x, int y, int offset = 1)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();

        for (int i = x - offset; i <= x + offset; i++)
        {
            for (int j = y - offset; j <= y + offset; j++)
            {
                if (IsWithinBounds(i, j))
                {
                    gamePieces.Add(mAllGamePieces[i, j]);
                }
            }
        }

        return gamePieces;
    }

    // Return a list of game piece objects affected by a bomb
    private List<GamePiece> GetBombedPieces(List<GamePiece> gamePieces)
    {
        List<GamePiece> allPiecesToClear = new List<GamePiece>();

        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                List<GamePiece> piecesToClear = new List<GamePiece>();

                Bomb bomb = piece.GetComponent<Bomb>();

                if (bomb != null)
                {
                    switch (bomb.bombType)
                    {
                        case BombType.Adjacent:
                            piecesToClear = GetAdjacentPieces(bomb.xIndex, bomb.yIndex, 1);
                            break;

                        case BombType.Color:
                            break;

                        case BombType.Column:
                            piecesToClear = GetColumnPieces(bomb.xIndex);
                            break;

                        case BombType.Row:
                            piecesToClear = GetRowPieces(bomb.yIndex);
                            break;
                    }

                    allPiecesToClear = allPiecesToClear.Union(piecesToClear).ToList();
                    allPiecesToClear = RemoveCollectibles(allPiecesToClear);
                }
            }
        }

        return allPiecesToClear;
    }

    // Return a list of game piece objects in a specific column
    private List<GamePiece> GetColumnPieces(int column)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();

        for (int i = 0; i < height; i++)
        {
            if (mAllGamePieces[column, i] != null)
            {
                gamePieces.Add(mAllGamePieces[column, i]);
            }
        }

        return gamePieces;
    }

    // Return a list of game piece objects in a specific row
    private List<GamePiece> GetRowPieces(int row)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            if (mAllGamePieces[i, row] != null)
            {
                gamePieces.Add(mAllGamePieces[i, row]);
            }
        }

        return gamePieces;
    }

    // Return a list of columns from the game board
    private List<int> GetColumns(List<GamePiece> gamePieces)
    {
        List<int> columns = new List<int>();

        foreach (GamePiece piece in gamePieces)
        {
            if (!columns.Contains(piece.xIndex))
            {
                columns.Add(piece.xIndex);
            }
        }

        return columns;
    }

    // Get a random game object prefab
    private GameObject GetRandomObject(GameObject[] objectArray)
    {
        int randomIndex = UnityEngine.Random.Range(0, objectArray.Length);

        if (objectArray[randomIndex] == null)
        {
            Debug.LogWarning("BOARD.GetRandomObject at index " + randomIndex + " does not contain a valid GameObject!");
        }

        return objectArray[randomIndex];
    }

    // Get a random collectible object from the array
    private GameObject GetRandomCollectible()
    {
        return GetRandomObject(collectiblePrefabs);
    }

    // Get a random game piece object from the array
    private GameObject GetRandomGamePiece()
    {
        return GetRandomObject(gamePiecePrefabs);
    }

    // Check for matches on fill
    private bool HasMatchOnFill(int x, int y, int minLength = 3)
    {
        List<GamePiece> downwardMatches = FindMatches(x, y, new Vector2(0, -1), minLength);
        List<GamePiece> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minLength);

        if (downwardMatches == null)
        {
            downwardMatches = new List<GamePiece>();
        }

        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }

        return (downwardMatches.Count > 0 || leftMatches.Count > 0);
    }

    // Highlight game piece matches for the entire board
    private void HighlightMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
            }
        }
    }

    // Highlight valid matches for a given coordinate
    private void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);

        List<GamePiece> combinedMatches = FindMatchesAt(x, y);

        if (combinedMatches.Count > 0)
        {
            foreach (GamePiece piece in combinedMatches)
            {
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    // Highlight a single tile object
    private void HighlightPieces(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    // Disable tile highlight
    private void HighlightTileOff(int x, int y)
    {
        if (mAllTiles[x, y].tileType != TileType.Breakable)
        {
            SpriteRenderer spriteRenderer = mAllTiles[x, y].GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
        }
    }

    // Enable tile highlight
    private void HighlightTileOn(int x, int y, Color color)
    {
        if (mAllTiles[x, y].tileType != TileType.Breakable)
        {
            SpriteRenderer spriteRenderer = mAllTiles[x, y].GetComponent<SpriteRenderer>();
            spriteRenderer.color = color;
        }
    }

    // Generate bomb objects for game board
    public GameObject InitializeBomb(int x, int y, Vector2 swapDirection, List<GamePiece> gamePieces)
    {
        GameObject bomb = null;
        MatchValue matchValue = MatchValue.None;

        if (gamePieces != null)
        {
            matchValue = FindMatchValue(gamePieces);
        }

        if (gamePieces.Count >= 5 && matchValue != MatchValue.None)
        {
            if (IsCornerMatch(gamePieces))
            {
                GameObject adjacentBomb = FindGamePieceByMatchValue(adjacentBombPrefabs, matchValue);

                if (adjacentBomb != null)
                {
                    bomb = CreateBomb(adjacentBomb, x, y);
                }
            }
            else
            {
                if (colorBombPrefab != null)
                {
                    bomb = CreateBomb(colorBombPrefab, x, y);
                }
            }
        }
        else if (gamePieces.Count == 4 && matchValue != MatchValue.None)
        {
            if (swapDirection.x != 0)
            {
                GameObject rowBomb = FindGamePieceByMatchValue(rowBombPrefabs, matchValue);

                if (rowBomb != null)
                {
                    bomb = CreateBomb(rowBomb, x, y);
                }
            }
            else
            {
                GameObject columnBomb = FindGamePieceByMatchValue(columnBombPrefabs, matchValue);

                if (columnBomb != null)
                {
                    bomb = CreateBomb(columnBomb, x, y);
                }
            }
        }

        return bomb;
    }

    // Set main camera orthographic size based on board size
    private void InitializeCamera()
    {
        Camera.main.transform.position = new Vector3((float)(width - 1) / 2f, (float)(height - 1) / 2f, -10f);

        float aspectRatio = (float)Screen.width / (float)Screen.height;

        float verticalSize = (float)height / 2f + (float)borderSize;

        float horizontalSize = ((float)width / 2f + (float)borderSize) / aspectRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }

    // Generate game pieces for game board
    private void InitializeGamePieces()
    {
        foreach (StartingObject startingGamePiece in startingGamePieces)
        {
            if (startingGamePiece != null)
            {
                GameObject piece = Instantiate(startingGamePiece.prefab, new Vector3(startingGamePiece.x, startingGamePiece.y, 0), Quaternion.identity) as GameObject;

                CreateGamePiece(piece, startingGamePiece.x, startingGamePiece.y, fillYOffset, fillMoveTime);
            }
        }
    }

    // Generate tiles for game board
    private void InitializeTiles()
    {
        foreach (StartingObject startingTile in startingTiles)
        {
            if (startingTile != null)
            {
                CreateTile(startingTile.prefab, startingTile.x, startingTile.y, startingTile.z);
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (mAllTiles[i, j] == null)
                {
                    CreateTile(tileNormalPrefab, i, j);
                }
            }
        }
    }

    // Check if two tiles are adjacent
    private bool IsAdjacent(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
        {
            return true;
        }

        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex)
        {
            return true;
        }

        return false;
    }

    private bool IsCollapsed(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                if (piece.transform.position.y - (float)piece.yIndex > 0.001f)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsColorBomb(GamePiece gamePiece)
    {
        Bomb bomb = gamePiece.GetComponent<Bomb>();

        if (bomb != null)
        {
            return (bomb.bombType == BombType.Color);
        }

        return false;
    }

    private bool IsCornerMatch(List<GamePiece> gamePieces)
    {
        bool horizontal = false;
        bool vertical = false;

        int xStart = -1;
        int yStart = -1;

        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                if (xStart == -1 || yStart == -1)
                {
                    xStart = piece.xIndex;
                    yStart = piece.yIndex;
                    continue;
                }

                if (piece.xIndex != xStart && piece.yIndex == yStart)
                {
                    horizontal = true;
                }

                if (piece.xIndex == xStart && piece.yIndex != yStart)
                {
                    vertical = true;
                }
            }
        }

        return (horizontal && vertical);
    }

    // Check if a coordinate exists on the board
    private bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    // Place a game piece onto the board
    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        if (gamePiece == null)
        {
            Debug.LogWarning("Board: Invalid GamePiece!");
            return;
        }

        gamePiece.transform.position = new Vector3(x, y, 0);
        gamePiece.transform.rotation = Quaternion.identity;

        if (IsWithinBounds(x, y))
        {
            mAllGamePieces[x, y] = gamePiece;
        }

        gamePiece.SetCoordinates(x, y);
    }

    private IEnumerator RefillBoardRoutine()
    {
        FillBoard(fillYOffset, fillMoveTime);
        yield return null;
    }

    public void ReleaseTile()
    {
        if (mClickedTile != null && mTargetTile != null)
        {
            SwitchTiles(mClickedTile, mTargetTile);
        }

        mClickedTile = null;
        mTargetTile = null;
    }

    // Remove collectible game objects from a list
    private List<GamePiece> RemoveCollectibles(List<GamePiece> bombedPieces)
    {
        List<GamePiece> collectiblePieces = FindAllCollectibles();
        List<GamePiece> piecesToRemove = new List<GamePiece>();

        foreach (GamePiece piece in collectiblePieces)
        {
            Collectible collectibleComponent = piece.GetComponent<Collectible>();

            if (collectibleComponent != null)
            {
                if (!collectibleComponent.clearedByBomb)
                {
                    piecesToRemove.Add(piece);
                }
            }
        }

        return bombedPieces.Except(piecesToRemove).ToList();
    }

    // Setup board invoked by game manager singleton
    public void SetupBoard()
    {
        InitializeTiles();
        InitializeGamePieces();

        List<GamePiece> startingCollectibles = FindAllCollectibles();
        collectibleCount = startingCollectibles.Count;

        InitializeCamera();

        FillBoard(fillYOffset, fillMoveTime);
    }

    private void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    private IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        if (mPlayerInputEnabled && !GameManager.Instance.isGameOver)
        {
            GamePiece clickedPiece = mAllGamePieces[clickedTile.xIndex, clickedTile.yIndex];
            GamePiece targetPiece = mAllGamePieces[targetTile.xIndex, targetTile.yIndex];

            if (clickedPiece != null && targetPiece != null)
            {
                // Swap the position of two tiles
                clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);

                // Wait for the swap time before evaluation
                yield return new WaitForSeconds(swapTime);

                // Check for matches in the new position
                List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
                List<GamePiece> targetPieceMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

                List<GamePiece> colorMatches = new List<GamePiece>();

                // Color bomb logic to populate colorMatches list
                if (IsColorBomb(clickedPiece) && !IsColorBomb(targetPiece))
                {
                    clickedPiece.matchValue = targetPiece.matchValue;
                    colorMatches = FindAllMatchValues(clickedPiece.matchValue);
                }
                else if (!IsColorBomb(clickedPiece) && IsColorBomb(targetPiece))
                {
                    targetPiece.matchValue = clickedPiece.matchValue;
                    colorMatches = FindAllMatchValues(targetPiece.matchValue);
                }
                else if (IsColorBomb(clickedPiece) && IsColorBomb(targetPiece))
                {
                    foreach (GamePiece piece in mAllGamePieces)
                    {
                        if (!colorMatches.Contains(piece))
                        {
                            colorMatches.Add(piece);
                        }
                    }
                }

                // Return pieces to original coordinates if there are no valid matches
                if (targetPieceMatches.Count == 0 && clickedPieceMatches.Count == 0 && colorMatches.Count == 0)
                {
                    clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                    targetPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                }
                // Clear pieces for valid matches
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.movesLeft--;
                        GameManager.Instance.UpdateMoves();
                    }

                    // Wait for the swap time before evaluation
                    yield return new WaitForSeconds(swapTime);

                    Vector2 swapDirection = new Vector2(targetTile.xIndex - clickedTile.xIndex, targetTile.yIndex - clickedTile.yIndex);

                    mClickedTileBomb = InitializeBomb(clickedTile.xIndex, clickedTile.yIndex, swapDirection, clickedPieceMatches);
                    mTargetTileBomb = InitializeBomb(targetTile.xIndex, targetTile.yIndex, swapDirection, targetPieceMatches);

                    if (mClickedTileBomb != null && targetPiece != null)
                    {
                        GamePiece clickedBombPiece = mClickedTileBomb.GetComponent<GamePiece>();

                        if (!IsColorBomb(clickedBombPiece))
                        {
                            clickedBombPiece.ChangeColor(targetPiece);
                        }
                    }

                    if (mTargetTileBomb != null && clickedPiece != null)
                    {
                        GamePiece targetBombPiece = mTargetTileBomb.GetComponent<GamePiece>();

                        if (!IsColorBomb(targetBombPiece))
                        {
                            targetBombPiece.ChangeColor(clickedPiece);
                        }
                    }

                    ClearAndRefillBoard(clickedPieceMatches.Union(targetPieceMatches).ToList().Union(colorMatches).ToList());
                }
            }
        }
    }
}