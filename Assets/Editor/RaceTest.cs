﻿using System;
using BC;
using NUnit.Framework;

public class RaceTest
{
    int m_CurrentCubeIndex;
    int m_FinishRaceCallCount;
    CupCardCubeColour[] m_Cubes;
    RaceLogic m_RaceLogic;
    int m_NumCubesInBag;
    Player m_RaceWinner;

    int GetCubesRemainingInBag()
    {
        return m_NumCubesInBag;
    }

    CupCardCubeColour NextCube()
    {
        if (m_Cubes == null)
            return CupCardCubeColour.Red;
        return m_Cubes[m_CurrentCubeIndex++];
    }

    void AddCubeToPlayer(Player player, CupCardCubeColour cubeType) { }

    void DiscardCard(Card card) { }

    void FinishRace(RaceLogic race)
    {
        m_FinishRaceCallCount += 1;
        m_RaceWinner = race.Winner;
    }

    void CreateRace(int numberOfCubes)
    {
        m_CurrentCubeIndex = 0;
        m_Cubes = null;
        m_FinishRaceCallCount = 0;
        m_RaceLogic = new RaceLogic();
        m_RaceLogic.Initialise(numberOfCubes, null,
            GetCubesRemainingInBag,
            NextCube,
            AddCubeToPlayer,
            DiscardCard,
            FinishRace);
    }

    void PrepareRaceOfSpecificType(int numberOfCubes, RaceState raceType)
    {
        if (raceType == RaceState.Lowest)
            CreateHighestRace(numberOfCubes);
        else
            CreateLowestRace(numberOfCubes);
        CompleteTestRace(numberOfCubes);
    }

    void AddCardsToRace(Player side, Card[] cards, int numberOfCards)
    {
        for (var i = 0; i < numberOfCards; ++i) Assert.That(m_RaceLogic.PlayCard(side, cards[i], side), Is.True);
    }

    void CompleteTiedTestRace(int numberOfCubes, bool leftLaysFirst = true)
    {
        var leftCards = new Card [numberOfCubes];
        var rightCards = new Card [numberOfCubes];
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

        if (leftLaysFirst) AddCardsToRace(Player.Left, leftCards, numberOfCubes);
        AddCardsToRace(Player.Right, rightCards, numberOfCubes);
        if (!leftLaysFirst) AddCardsToRace(Player.Left, leftCards, numberOfCubes);
    }

    void CompleteTestRace(int numberOfCubes)
    {
        var leftCards = new Card [numberOfCubes];
        var rightCards = new Card [numberOfCubes];
        for (var c = 0; c < numberOfCubes; ++c)
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
        m_NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        if (numberOfCubes % 2 == 0)
        {
            CompleteTestRace(numberOfCubes);
            StartRace();
        }
    }

    void CreateHighestRace(int numberOfCubes)
    {
        m_NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        if (numberOfCubes % 2 == 1)
        {
            CompleteTestRace(numberOfCubes);
            StartRace();
        }
    }

    void CreateTiedRace(int numberOfCubes, RaceState raceType)
    {
        Assert.That(numberOfCubes, Is.GreaterThan(1), "A tied race needs at least two cubes");
        m_NumCubesInBag = 10;
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        m_Cubes = new CupCardCubeColour [4];
        m_Cubes[0] = CupCardCubeColour.Red;
        m_Cubes[1] = CupCardCubeColour.Blue;
        m_Cubes[2] = CupCardCubeColour.Green;
        m_Cubes[3] = CupCardCubeColour.Yellow;
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
    }

    void StartRace()
    {
        m_CurrentCubeIndex = 0;
        m_FinishRaceCallCount = 0;
        m_RaceLogic.StartRace();
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
        m_NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    [Test]
    public void TheFirstEvenRaceIsHighest([Values(2, 4)] int numberOfCubes)
    {
        m_NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void StartRaceSetsRaceToFinishedWhenNotEnoughCubesInBag([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        m_NumCubesInBag = 0;
        CreateRace(numberOfCubes);
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Finished));
    }

    [Test]
    public void StartingAFinishedRaceTogglesState([Values(RaceState.Lowest, RaceState.Highest)]
        RaceState startingState)
    {
        var numberOfCubes = 0;
        if (startingState == RaceState.Lowest)
            numberOfCubes = 1;
        else if (startingState == RaceState.Highest) numberOfCubes = 2;
        m_NumCubesInBag = 10;
        CreateRace(numberOfCubes);
        m_RaceLogic.NewGame();

        var expectedState = ToggleState(startingState);
        Assert.That(m_RaceLogic.State, Is.EqualTo(startingState));
        CompleteTestRace(numberOfCubes);
        Assert.That(m_FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(expectedState));

        m_FinishRaceCallCount = 0;
        startingState = expectedState;
        expectedState = ToggleState(startingState);
        Assert.That(m_RaceLogic.State, Is.EqualTo(startingState));
        CompleteTestRace(numberOfCubes);
        Assert.That(m_FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void StartRaceMakesLowestRaceBecomeHighest([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateLowestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
        CompleteTestRace(numberOfCubes);
        Assert.That(m_FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
    }

    [Test]
    public void StartRaceMakesHighestRaceBecomeLowest([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateHighestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
        CompleteTestRace(numberOfCubes);
        Assert.That(m_FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
    }

    [Test]
    public void HighestPlayerWinsHighestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateHighestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Highest));
        CompleteTestRace(numberOfCubes);
        Assert.That(m_FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_RaceWinner, Is.EqualTo(Player.Right));
    }

    [Test]
    public void LowestPlayerWinsLowestRace([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateLowestRace(numberOfCubes);
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Lowest));
        CompleteTestRace(numberOfCubes);
        Assert.That(m_FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        Assert.That(m_RaceWinner, Is.EqualTo(Player.Left));
    }

    [Test]
    public void CurrentPlayerWinsTiedRace([Values(2, 3, 4)] int numberOfCubes,
        [Values(Player.Left, Player.Right)] Player startPlayer,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType)
    {
        CreateTiedRace(numberOfCubes, raceType);
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        var leftLaysFirst = (startPlayer == Player.Left);
        CompleteTiedTestRace(numberOfCubes, leftLaysFirst);
        Assert.That(m_FinishRaceCallCount, Is.EqualTo(1), "Finish Race was not called once");
        if (leftLaysFirst)
            Assert.That(m_RaceWinner, Is.EqualTo(Player.Right));
        else
            Assert.That(m_RaceWinner, Is.EqualTo(Player.Left));
    }

    [Test]
    public void CanPlayCardReturnsFalseIfRaceIsFinished([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        CreateRace(numberOfCubes);
        var card = new Card(CupCardCubeColour.Red, 1);
        Assert.That(m_RaceLogic.CanPlayCard(card), Is.False);
    }

    [Test]
    public void CanPlayCardReturnsFalseIfColourNotOnRace([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        var card = new Card(CupCardCubeColour.Grey, 1);
        Assert.That(m_RaceLogic.CanPlayCard(card), Is.False);
    }

    [Test]
    public void CanPlayCardReturnsFalseIfColourNotAvailable([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType,
        [Values(Player.Left, Player.Right)] Player currentPlayer)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        m_Cubes = new CupCardCubeColour [4];
        m_Cubes[0] = CupCardCubeColour.Red;
        m_Cubes[1] = CupCardCubeColour.Blue;
        m_Cubes[2] = CupCardCubeColour.Green;
        m_Cubes[3] = CupCardCubeColour.Yellow;
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        m_RaceLogic.PlayCard(Player.Left, new Card(CupCardCubeColour.Red, 1), currentPlayer);
        m_RaceLogic.PlayCard(Player.Right, new Card(CupCardCubeColour.Red, 2), currentPlayer);
        Assert.That(m_RaceLogic.CanPlayCard(new Card(CupCardCubeColour.Red, 3)), Is.False);
    }

    [Test]
    public void CanPlayCardReturnsTrueIfColourIsAvailable([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        m_Cubes = new CupCardCubeColour [4];
        m_Cubes[0] = CupCardCubeColour.Red;
        m_Cubes[1] = CupCardCubeColour.Blue;
        m_Cubes[2] = CupCardCubeColour.Green;
        m_Cubes[3] = CupCardCubeColour.Yellow;
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        Assert.That(m_RaceLogic.CanPlayCard(new Card(CupCardCubeColour.Red, 1)), Is.True);
    }

    [Test]
    public void PlayCardReturnsFalseIfSideIsFull([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType,
        [Values(Player.Left, Player.Right)] Player side,
        [Values(Player.Left, Player.Right)] Player currentPlayer)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        for (var c = 0; c < numberOfCubes; ++c) Assert.That(m_RaceLogic.PlayCard(side, new Card(CupCardCubeColour.Red, c + 1), Player.Left), Is.True);
        Assert.That(m_RaceLogic.PlayCard(side, new Card(CupCardCubeColour.Red, 13), Player.Left), Is.False);
    }

    [Test]
    public void PlayCardReturnsFalseIfColourNotInRace([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType,
        [Values(Player.Left, Player.Right)] Player side,
        [Values(Player.Left, Player.Right)] Player currentPlayer)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        Assert.That(m_RaceLogic.PlayCard(side, new Card(CupCardCubeColour.Grey, 13), currentPlayer), Is.False);
    }

    [Test]
    public void PlayCardReturnsFalseIfColourNotAvailable([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType,
        [Values(Player.Left, Player.Right)] Player side,
        [Values(Player.Left, Player.Right)] Player currentPlayer)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        for (var c = 0; c < numberOfCubes; ++c) Assert.That(m_RaceLogic.PlayCard(side, new Card(CupCardCubeColour.Red, c + 1), currentPlayer), Is.True);
        Assert.That(m_RaceLogic.PlayCard(side, new Card(CupCardCubeColour.Red, 13), currentPlayer), Is.False);
    }

    [Test]
    public void PlayCardReturnsTrueIfColourIsAvailable([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType,
        [Values(Player.Left, Player.Right)] Player side,
        [Values(Player.Left, Player.Right)] Player currentPlayer)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        m_Cubes = new CupCardCubeColour [4];
        m_Cubes[0] = CupCardCubeColour.Red;
        m_Cubes[1] = CupCardCubeColour.Blue;
        m_Cubes[2] = CupCardCubeColour.Green;
        m_Cubes[3] = CupCardCubeColour.Yellow;
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        for (var c = 0; c < numberOfCubes; ++c) Assert.That(m_RaceLogic.PlayCard(side, new Card(m_Cubes[c], 1), currentPlayer), Is.True);
    }

    [Test]
    public void GetCubeReturnsCubesAddedToRace([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        m_Cubes = new CupCardCubeColour [4];
        m_Cubes[0] = CupCardCubeColour.Red;
        m_Cubes[1] = CupCardCubeColour.Blue;
        m_Cubes[2] = CupCardCubeColour.Green;
        m_Cubes[3] = CupCardCubeColour.Yellow;
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        for (var c = 0; c < numberOfCubes; ++c) Assert.That(m_RaceLogic.GetCube(c), Is.EqualTo(m_Cubes[c]));
    }

    [Test]
    public void GetCubeReturnsInvalidIfRaceIsFinished([Values(1, 2, 3, 4)] int numberOfCubes)
    {
        m_NumCubesInBag = 0;
        CreateRace(numberOfCubes);
        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(RaceState.Finished));
        for (var c = 0; c < numberOfCubes; ++c) Assert.That(m_RaceLogic.GetCube(c), Is.EqualTo(CupCardCubeColour.Invalid));
    }

    [Test]
    public void GetPlayedCardReturnsNullIfCardNotPlayed([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType,
        [Values(Player.Left, Player.Right)] Player side)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        StartRace();
        for (var c = 0; c < numberOfCubes; ++c) Assert.That(m_RaceLogic.GetPlayedCard(side, c), Is.Null);
    }

    [Test]
    public void GetPlayedCardReturnsCardsAddedToRace([Values(1, 2, 3, 4)] int numberOfCubes,
        [Values(RaceState.Lowest, RaceState.Highest)]
        RaceState raceType)
    {
        PrepareRaceOfSpecificType(numberOfCubes, raceType);
        m_Cubes = new CupCardCubeColour [4];
        m_Cubes[0] = CupCardCubeColour.Red;
        m_Cubes[1] = CupCardCubeColour.Blue;
        m_Cubes[2] = CupCardCubeColour.Green;
        m_Cubes[3] = CupCardCubeColour.Yellow;
        var leftCards = new Card [numberOfCubes];
        var rightCards = new Card [numberOfCubes];
        leftCards[0] = new Card(CupCardCubeColour.Red, 1);
        rightCards[0] = new Card(CupCardCubeColour.Red, 13);
        if (numberOfCubes > 1)
        {
            leftCards[1] = new Card(CupCardCubeColour.Blue, 13);
            rightCards[1] = new Card(CupCardCubeColour.Blue, 1);
        }

        if (numberOfCubes > 2)
        {
            leftCards[2] = new Card(CupCardCubeColour.Green, 13);
            rightCards[2] = new Card(CupCardCubeColour.Green, 1);
        }

        if (numberOfCubes > 3)
        {
            leftCards[3] = new Card(CupCardCubeColour.Yellow, 13);
            rightCards[3] = new Card(CupCardCubeColour.Yellow, 1);
        }

        StartRace();
        Assert.That(m_RaceLogic.State, Is.EqualTo(raceType));
        for (var c = 0; c < numberOfCubes; ++c)
        {
            Assert.That(m_RaceLogic.PlayCard(Player.Left, leftCards[c], Player.Left), Is.True);
            Assert.That(m_RaceLogic.PlayCard(Player.Right, rightCards[c], Player.Left), Is.True);
        }

        for (var c = 0; c < numberOfCubes; ++c)
        {
            Assert.That(m_RaceLogic.GetPlayedCard(Player.Left, c), Is.EqualTo(leftCards[c]));
            Assert.That(m_RaceLogic.GetPlayedCard(Player.Right, c), Is.EqualTo(rightCards[c]));
        }
    }
}
