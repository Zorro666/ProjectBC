namespace BC
{
    public enum CupCardCubeColour
    {
        Grey = 0,
        Blue,
        Green,
        Yellow,
        Red,
        Count = Red + 1
    }

    public enum Player
    {
        Unknown = -1,
        Left = 0,
        Right,
        Count = Right + 1
    }

    public enum RaceState
    {
        Lowest,
        Highest,
        Finished
    }
}
