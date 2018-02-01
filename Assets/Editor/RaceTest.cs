using NUnit.Framework;
using BC;

public class RaceTest
{
    int NumCubesInBag;
    int FinishRaceCallCount;
    RaceLogic m_RaceLogic;
    Player RaceWinner;
    CupCardCubeColour[] m_Cubes;
    int CurrentCubeIndex;

    int GetCubesRemainingInBag()
    {
        return NumCubesInBag;
    }

    CupCardCubeColour NextCube()
    {
        if (m_Cubes == null)
            return CupCardCubeColour.Red;
        return m_Cubes[CurrentCubeIndex++];
    }

    void AddCubeToPlayer(Player player, CupCardCubeColour cubeType)
    {
    }

    void DiscardCard(Card card)
    {
    }

    void FinishRace(RaceLogic race)
    {
        FinishRaceCallCount += 1;
        RaceWinner = race.Winner;
    }

    void CreateRace(int numberOfCubes)
    {
        CurrentCubeIndex = 0;
        m_Cubes = null;
        FinishRaceCallCount = 0;
        m_RaceLogic = new RaceLogic();
        RaceUI raceUI = null;
        m_RaceLogic.Initialise(numberOfCubes, raceUI,
                             GetCubesRemainingInBag,
                             NextCube,
                             AddCubeToPlayer,
                             DiscardCard,
                             FinishRace);
    }

    void AddCardsToRace(Player side, Card[] cards, int numberOfCards)
    {
        for (int i = 0; i < numberOfCards; ++i)
        {
            Assert.That(m_RaceLogic.PlayCard(side, cards[i], side), Is.True);
        }
    }

    void CompleteTiedTestRace(int numberOfCubes, bool leftLaysFirst = true)
    {
        Card[] leftCards = new Card[numberOfCubes];
        Card[] rightCards = new Card[numberOfCubes];
        Assert.That(numberOfCubes, Is.GreaterThan(1), "A tied race needs at least two cubes");

        if (numberOfCubes == 2)
        {
            // Left : Red 1 + Blue 13
            leftCards[0] = new Card(CupCardCubeColour.Red, 1);
            leftCards[1] = new Card(CupCardCubeColour.Blue, 13);
            // Right : Red 13 + Blue 1
            rightCards[0] = new Card(CupCardCubeColour.Red, 13);
            rightCards[1] = new Card(CupCardCubeColour.Blue, 1);
        }

        if (numberOfCubes == 3)
        {
            // Left : Red 1 + Blue 7 + Green 13
            leftCards[0] = new Card(CupCardCubeColour.Red, 1);
            leftCards[1] = new Card(CupCardCubeColour.Blue, 7);
            leftCards[2] = new Card(CupCardCubeColour.Green, 13);
            // Left : Red 7 + Blue 13 + Green 1
            rightCards[0] = new Card(CupCardCubeColour.Red, 7);
            rightCards[1] = new Card(CupCardCubeColour.Blue, 13);
            rightCards[2] = new Card(CupCardCubeColour.Green, 1);
        }

        if (numberOfCubes == 4)
        {
            // Left : Red 1 + Blue 3 + Green 7 + Yellow 13
            leftCards[0] = new Card(CupCardCubeColour.Red, 1);
            leftCards[1] = new Card(CupCardCubeColour.Blue, 3);
            leftCards[2] = new Card(CupCardCubeColour.Green, 7);
            leftCards[3] = new Card(CupCardCubeColour.Yellow, 13);
            // Left : Red 3 + Blue 7 + Green 13 + Yellow 1
            rightCards[0] = new Card(CupCardCubeColour.Red, 3);
            rightCards[1] = new Card(CupCardCubeColour.Blue, 7);
            rightCards[2] = new Card(CupCardCubeColour.Green, 13);
            rightCards[3] = new Card(CupCardCubeColour.Yellow, 1);
        }

        if (leftLaysFirst)
        {
            AddCardsToRace(Player.Left, leftCards, numberOfCubes);
        }
        AddCardsToRace(Player.Right, rightCards, numberOfCubes);
        if (!leftLaysFirst)
        {
            AddCardsToRace(Player.Left, leftCards, numberOfCubes);
        }
    }

    void CompleteTestRace(int numberOfCubes)
    {
        Card[] leftCards = new Card[numberOfCubes];
        Card[] rightCards = new Card[numberOfCubes];
        for (int c = 0; c < numberOfCubes; ++c)
        {
            leftCards[c] = new Card(CupCardCubeColour.Red, c + 1);
            rightCards[c] = new Card(CupCardCubeColour.Red, 13 - c);
        }
        AddCardsToRace(Player.Left, leftCards, numberOfCubes);
        AddCardsToRace(Player.Right, rightCards, numberOfCubes);
    }

    RaceState ToggleState(RaceState state)
    {
        if (state == RaceState.Lowest)
            return RaceState.Highest;
        if (state == RaceState.Highest)
            return RaceState.Lowest;
        return RaceState.Highest;
    }

    void CreateLowestRace(int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        if ((numberOfCubes % 2) == 0)
        {
            CompleteTestRace(numberOfCubes);
            FinishRaceCallCount = 0;
            m_RaceLogic.StartRace();
        }
    }

    void CreateHighestRace(int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        if ((numberOfCubes % 2) == 1)
        {
            CompleteTestRace(numberOfCubes);
            FinishRaceCallCount = 0;
            m_RaceLogic.StartRace();
        }
    }

    void CreateTiedRace(int numberOfCubes, RaceState raceType)
    {
        Assert.That(numberOfCubes, Is.GreaterThan(1), "A tied race needs at least two cubes");
        NumCubesInBag = 10;
        if (raceType == RaceState.Lowest)
        {
            CreateHighestRace(numberOfCubes);
        }
        else
        {
            CreateLowestRace(numberOfCubes);
        }
        CompleteTestRace(numberOfCubes);
        m_Cubes = new CupCardCubeColour[4];
        m_Cubes[0] = CupCardCubeColour.Red;
        m_Cubes[1] = CupCardCubeColour.Blue;
        m_Cubes[2] = CupCardCubeColour.Green;
        m_Cubes[3] = CupCardCubeColour.Yellow;
        CurrentCubeIndex = 0;
        FinishRaceCallCount = 0;
        m_RaceLogic.StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
    }

    [Test]
    public void InitialiseSetsNumbersOfCubes([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateRace(numberOfCubes);
        Assert.That(m_RaceLogic.NumberOfCubes, Is.EqualTo(numberOfCubes));
    }

    [Test]
    public void InitialiseSetsWinnerToUnkown([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateRace(numberOfCubes);
        Assert.That(m_RaceLogic.Winner, Is.EqualTo(Player.Unknown));
    }

    [Test]
    public void TheFirstOddRaceIsLowest([Values(1, 3)] int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    [Test]
    public void TheFirstEvenRaceIsHighest([Values(2, 4)] int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void StartRaceSetsRaceToFinishedWhenNotEnoughCubesInBag([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        NumCubesInBag = 0;
        CreateRace(numberOfCubes);
        m_RaceLogic.StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Finished));
    }

    [Test]
    public void StartingAFinishedRaceTogglesState([Values(RaceState.Lowest, RaceState.Highest)] RaceState startingState)
    {
        int numberOfCubes = 0;
        if (startingState == RaceState.Lowest)
        {
            numberOfCubes = 1;
        }
        else if (startingState == RaceState.Highest)
        {
            numberOfCubes = 2;
        }
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();

        RaceState expectedState = RaceState.Finished;
        expectedState = ToggleState(startingState);
        Assert.That(m_RaceLogic.State, Is.EqualTo(startingState));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_RaceLogic.State, Is.EqualTo(expectedState));

        FinishRaceCallCount = 0;
        startingState = expectedState;
        expectedState = ToggleState(startingState);
        m_RaceLogic.StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(startingState));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_RaceLogic.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void FinishRaceMakesLowestRaceBecomeHighest([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateLowestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void FinishRaceMakesHighestRaceBecomeLowest([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateHighestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    [Test]
    public void HighestPlayerWinsHighestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateHighestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(RaceWinner, Is.EqualTo(Player.Right));
    }

    [Test]
    public void LowestPlayerWinsLowestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateLowestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(RaceWinner, Is.EqualTo(Player.Left));
    }

    // The winner when completing a race : current player in a tie (Highest or Lowest)
    [Test]
    public void CurrentPlayerWinsTiedRace([Values(2, 3, 4)] int numberOfCubes, 
                                          [Values(Player.Left, Player.Right)] Player startPlayer, 
                                          [Values(RaceState.Lowest, RaceState.Highest)] RaceState raceType)
    {
        CreateTiedRace(numberOfCubes, raceType);
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        bool leftLaysFirst = (startPlayer == Player.Left) ? true : false;
        CompleteTiedTestRace(numberOfCubes, leftLaysFirst);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        if (leftLaysFirst)
            Assert.That(RaceWinner, Is.EqualTo(Player.Right));
        else
            Assert.That(RaceWinner, Is.EqualTo(Player.Left));
    }

    //TODO: Tests 
    // CanPlayCard
    // PlayCard : true and false cases
    [Test]
    public void CanPlayCardReturnsFalseIfSideIsFull([Values(1, 2, 3, 4)] int numberOfCubes, [Values(Player.Left, Player.Right)] Player side)
    {
        Card card = new Card(CupCardCubeColour.Red, 1);
        Assert.That(m_RaceLogic.CanPlayCard(card), Is.False);
    }
}