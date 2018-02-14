using System;

namespace BC
{
    public enum CupCardCubeColour
    {
        Grey = 0,
        Blue,
        Green,
        Yellow,
        Red,
        Count = Red + 1,
        Invalid
    }

    public enum Player
    {
        Left = 0,
        Right,
        Count = Right + 1,
        Unknown
    }

    public enum RaceState
    {
        Lowest,
        Highest,
        Finished
    }
}
