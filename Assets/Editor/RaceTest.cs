using NUnit.Framework;
using BC;

public class RaceTest 
{
    int NumCubesInBag;
    int FinishRaceCallCount;
    RaceLogic m_raceLogic;

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

    void FinishRace(Player winner, RaceLogic race)
    {
        FinishRaceCallCount += 1;
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

    void CompleteARace(int numberOfCubes, RaceState startingState)
    {
        Assert.That(m_raceLogic.State, Is.EqualTo(startingState));

        // Finish the race by laying cards on it
        Player currentPlayer = Player.Left;
        int cardValue = 1;
        for (int i = 0; i < numberOfCubes; ++i)
        {
            for (int side = 0; side < GameLogic.PlayerCount; ++side)
            {
                Card card = new Card(CupCardCubeColour.Red, cardValue);
                cardValue += 1;
                currentPlayer = (Player)side;
                Assert.That(m_raceLogic.PlayCard(currentPlayer, card, currentPlayer), Is.True);
            }
        }
    }

    RaceState ToggleState(RaceState state)
    {
        if (state == RaceState.Lowest)
            return RaceState.Highest;
        if (state == RaceState.Highest)
            return RaceState.Lowest;
        return RaceState.Highest;
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
        CompleteARace(numberOfCubes, startingState);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(expectedState));

        FinishRaceCallCount = 0;
        startingState = expectedState;
        expectedState = ToggleState(startingState);
        m_raceLogic.StartRace();
        CompleteARace(numberOfCubes, startingState);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void FinishRaceMakesLowestRaceBecomeHighest([Values(1, 3)] int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_raceLogic.NewGame();
        CompleteARace(numberOfCubes, RaceState.Lowest);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void FinishRaceMakesHighestRaceBecomeLowest([Values(2, 4)] int numberOfCubes)
    {
        NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_raceLogic.NewGame();
        CompleteARace(numberOfCubes, RaceState.Highest);
        Assert.That(FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_raceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    //TODO: Tests 
    // The winner when completing a race : Highest, Lowest, current player in a tie (Highest or Lowest)
    // CanPlayCard
    // PlayCard : true and false cases
}