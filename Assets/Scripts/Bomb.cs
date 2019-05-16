public enum BombType
{
    Adjacent,
    Color,
    Column,
    None,
    Row
}

public class Bomb : GamePiece
{
    public BombType bombType;
}