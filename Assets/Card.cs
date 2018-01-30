using BC;

public
class Card {
public
  CupCardCubeColour Colour {
    get;
  private
    set;
  }
public
  int Value {
    get;
  private
    set;
  }

public
  Card(CupCardCubeColour colour, int value) {
    Colour = colour;
    Value = value;
  }
}