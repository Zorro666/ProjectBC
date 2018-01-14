public class Card
{
    public BC.CardCubeColour Colour
    {
        get { return m_Colour; }
    } 
    public int Value
    {
        get { return m_Value; }
    }

    readonly BC.CardCubeColour m_Colour;
    readonly int m_Value;

    public Card(BC.CardCubeColour colour, int value)
    {
        m_Colour = colour;
        m_Value = value;
    }
}
