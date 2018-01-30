using NUnit.Framework;
using BC;

public class RaceTest 
{
    int m_numCubesInBag;

    int GetCubesRemainingInBag()
    {
        return m_numCubesInBag;
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
    }

    RaceLogic CreateRace(int numberOfCubes)
    {
        RaceLogic raceLogic = new RaceLogic();
        RaceUI raceUI = null;
        raceLogic.Initialise(numberOfCubes, raceUI, 
                             GetCubesRemainingInBag, 
                             NextCube,
                             AddCubeToPlayer,
                             DiscardCard,
                             FinishRace);
        return raceLogic;
    }

	[Test]
	public void InitialiseSetsNumbersOfCubes([Values(1, 2, 3, 4)] int numberOfCubes) 
    {
        var raceLogic = CreateRace(numberOfCubes);
        Assert.That(raceLogic.NumberOfCubes, Is.EqualTo(numberOfCubes));
	}

    [Test]
    public void InitialiseSetsWinnerToUnkown([Values(1, 2, 3, 4)] int numberOfCubes) 
    {
        var raceLogic = CreateRace(numberOfCubes);
        Assert.That(raceLogic.Winner, Is.EqualTo(Player.Unknown));
    }

    [Test]
    public void TheFirstOddRacesAreLowest([Values(1, 3)] int numberOfCubes)
    {
        m_numCubesInBag = 10;
        var raceLogic = CreateRace(numberOfCubes);
        raceLogic.NewGame();
        Assert.That(raceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    [Test]
    public void TheFirstEvenRacesAreHighest([Values(2, 4)] int numberOfCubes)
    {
        m_numCubesInBag = 10;
        var raceLogic = CreateRace(numberOfCubes);
        raceLogic.NewGame();
        Assert.That(raceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void StartRaceSetsRaceToFinishedWhenNotEnoughCubesInBag([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        m_numCubesInBag = 0;
        var raceLogic = CreateRace(numberOfCubes);
        raceLogic.StartRace();
        Assert.That(raceLogic.State, Is.EqualTo(RaceState.Finished));
    }

    // Test
    // Need to make these as delegates in RaceLogic 
    //void AddCubeToPlayer(Player winner, CupCardCubeColour cubeColour);
    //void DiscardCard(Card card);
    //void FinishRace(Player winner, RaceLogic race);

    //Player Race.ComputeWinner()
    //Need to setup m_cards
    //public bool PlayCard(BC.Player player, Card card, BC.Player currentPlayer)
    // Need to setup m_cardsPlayed & m_cardsRemaining : to test return false cases
}