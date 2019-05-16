public class Collectible : GamePiece
{
    public bool clearedByBomb = false;
    public bool clearedAtBottom = true;

    // Use this for initialization
    private void Start()
    {
        matchValue = MatchValue.None;
    }

    // Update is called once per frame
    private void Update()
    {
    }
}