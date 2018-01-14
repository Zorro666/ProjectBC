using System;

namespace BC
{
    public enum CardCubeColour
    {
        Grey = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Red = 4,
        Count = CardCubeColour.Red + 1
    }

    public enum Player
    {
        First = 0,
        Left = Player.First,
        Right = 1,
        Count = 2,
        Unknown = 2
    }
}
