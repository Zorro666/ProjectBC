using NUnit.Framework;
using BC;

public class RaceTest
{
    int NumCubesInBag;
    int FinishRaceCallCount;
    RaceLogic m_raceLogic;
    Player RaceWinner;

    int GetCubesRemainingInBag()
    {
        return NumCubesInBag;
    }

    CupCardCubeColour NextCube()
    {
        return CupCardCubeColour.Red;
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
        FinishRaceCallCount = 0;
        m_raceLogic = new RaceLogic();
        RaceUI raceUI = null;
        m_raceLogic.Initialise(numberOfCubes, raceUI,
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
            Assert.That(m_raceLogic.PlayCard(side, cards[i], side), Is.True);
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
        m_raceLogic.NewGame();
        if ((numberOfCubes % 2) == 0)
        {
            CompleteTestRace(numberOfCubes);
            FinishRaceCallCount = 0;
            m_raceLogic.StartRace();
        }
    }

    void CreateHighestRace(int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_raceLogic.NewGame();
        if ((numberOfCubes % 2) == 1)
        {
            CompleteTestRace(numberOfCubes);
            FinishRaceCallCount = 0;
            m_raceLogic.StartRace();
        }
    }

	[Test]
	public void InitialiseSetsNumbersOfCubes([Values(1, 2, 3, 4)] int numberOfCubes) 
    {
        CreateRace(numberOfCubes);
        Assert.That(m_raceLogic.NumberOfCubes, Is.EqualTo(numberOfCubes));
	}

    [Test]
    public void InitialiseSetsWinnerToUnkown([Values(1, 2, 3, 4)] int numberOfCubes) 
    {
        CreateRace(numberOfCubes);
        Assert.That(m_raceLogic.Winner, Is.EqualTo(Player.Unknown));
    }

    [Test]
    public void TheFirstOddRaceIsLowest([Values(1, 3)] int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_raceLogic.NewGame();
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    [Test]
    public void TheFirstEvenRaceIsHighest([Values(2, 4)] int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_raceLogic.NewGame();
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void StartRaceSetsRaceToFinishedWhenNotEnoughCubesInBag([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        NumCubesInBag = 0;
        CreateRace(numberOfCubes);
        m_raceLogic.StartRace();
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Finished));
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
        m_raceLogic.NewGame();

        RaceState expectedState = RaceState.Finished;
        expectedState = ToggleState(startingState);
        Assert.That(m_raceLogic.State, Is.EqualTo(startingState));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(expectedState));

        FinishRaceCallCount = 0;
        startingState = expectedState;
        expectedState = ToggleState(startingState);
        m_raceLogic.StartRace();
        Assert.That(m_raceLogic.State, Is.EqualTo(startingState));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void FinishRaceMakesLowestRaceBecomeHighest([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateLowestRace(numberOfCubes);
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Lowest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void FinishRaceMakesHighestRaceBecomeLowest([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateHighestRace(numberOfCubes);
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Highest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    [Test]
    public void HighestPlayerWinsHighestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateHighestRace(numberOfCubes);
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Highest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(RaceWinner, Is.EqualTo(Player.Right));
    }

    [Test]
    public void LowestPlayerWinsLowestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateLowestRace(numberOfCubes);
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Lowest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(RaceWinner, Is.EqualTo(Player.Left));
    }

    [Test]
    public void CurrentPlayerWinsLowestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateLowestRace(numberOfCubes);
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Lowest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(RaceWinner, Is.EqualTo(Player.Left));
    }

    [Test]
    public void CurrentPlayerWinsHighestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateHighestRace(numberOfCubes);
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Highest));
        CompleteTestRace(numberOfCubes);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(RaceWinner, Is.EqualTo(Player.Right));
    }
    //TODO: Tests 
    // The winner when completing a race : current player in a tie (Highest or Lowest)
    // CanPlayCard
    // PlayCard : true and false cases
}