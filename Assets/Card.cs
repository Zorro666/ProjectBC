using System;
using BC;

public class Card
{
    public Card(CupCardCubeColour colour, int value)
    {
        Colour = colour;
        Value = value;
    }

    public CupCardCubeColour Colour { get; }
    public int Value { get; }
}
