using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using BC;

public class RaceTest 
{
    int GetCubesRemainingInBag()
    {
        return 10;
    }

    CupCardCubeColour NextCube()
    {
        return CupCardCubeColour.Red;
    }

    RaceLogic CreateRace(int numberOfCubes)
    {
        RaceLogic raceLogic = new RaceLogic();
        RaceUI raceUI = null;
        GameLogic gameLogic = null;
        raceLogic.Initialise(numberOfCubes, raceUI, 
                             GetCubesRemainingInBag, 
                             NextCube,
                             gameLogic);
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

    // Test
    // Need to make these as delegates in RaceLogic 
    //void AddCubeToPlayer(Player winner, CupCardCubeColour cubeColour);
    //void DiscardCard(Card card);
    //void FinishRace(Player winner, RaceLogic race);

    //Player Race.ComputeWinner()
    //Need to setup m_cards
    //public bool PlayCard(BC.Player player, Card card, BC.Player currentPlayer)
    // Need to setup m_cardsPlayed & m_cardsRemaining : to test return false cases

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator RaceTestWithEnumeratorPasses() 
    {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
        Assert.Fail();
	}
}
