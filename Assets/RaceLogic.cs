using System;
using BC;
using UnityEngine;

public class RaceLogic
{
    public delegate void AddCubeToPlayerDelegate(Player winner, CupCardCubeColour cube);

    public delegate int CubesRemainingInBagDelegate();

    public delegate void DiscardCardDelegate(Card card);

    public delegate void FinishRaceDelegate(RaceLogic race);

    public delegate CupCardCubeColour NextCubeDelegate();

    AddCubeToPlayerDelegate m_AddCubeToPlayer;

    Card[,] m_Cards;
    int[] m_CardsPlayed;
    int[,] m_CardsRemaining;
    CupCardCubeColour[] m_Cubes;
    CubesRemainingInBagDelegate m_CubesRemainingInBag;
    DiscardCardDelegate m_DiscardCard;
    FinishRaceDelegate m_FinishRace;

    NextCubeDelegate m_NextCube;
    RaceUI m_RaceUI;

    public RaceState State { get; private set; }
    public int NumberOfCubes { get; private set; }
    public Player Winner { get; private set; }

    public Card GetPlayedCard(Player side, int i)
    {
        return m_Cards[(int)side, i];
    }

    public CupCardCubeColour GetCube(int i)
    {
        return m_Cubes[i];
    }

    public bool CanPlayCard(Card card)
    {
        var cardColour = (int)card.Colour;
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
            if (m_CardsRemaining[playerIndex, cardColour] > 0)
                return true;
        return false;
    }

    public void ResetPlayCardButtons()
    {
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            var cardIndex = m_CardsPlayed[playerIndex];
            if (cardIndex < NumberOfCubes)
                if (m_RaceUI)
                    m_RaceUI.SetPlayCardButtons(playerIndex, cardIndex, false);
        }
    }

    public void SetPlayCardButtons(Card card)
    {
        var cardColour = (int)card.Colour;
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            var cardIndex = m_CardsPlayed[playerIndex];
            var setValue = false;
            var active = false;
            if (m_CardsRemaining[playerIndex, cardColour] > 0)
            {
                active = true;
                setValue = true;
            }
            else if (cardIndex < NumberOfCubes)
            {
                setValue = true;
            }

            if (setValue)
                if (m_RaceUI)
                {
                    m_RaceUI.SetPlayedCardToCard(playerIndex, cardIndex, card);
                    m_RaceUI.SetPlayCardButtons(playerIndex, cardIndex, active);
                }
        }
    }

    public void StartRace()
    {
        if (State == RaceState.Lowest)
            State = RaceState.Highest;
        else if (State == RaceState.Highest)
            State = RaceState.Lowest;

        for (var i = 0; i < m_CardsPlayed.Length; ++i)
            m_CardsPlayed[i] = 0;

        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
            {
                if (m_Cards[playerIndex, cardIndex] != null)
                    m_DiscardCard(m_Cards[playerIndex, cardIndex]);
                m_Cards[playerIndex, cardIndex] = null;
                if (m_RaceUI) m_RaceUI.SetPlayCardButtons(playerIndex, cardIndex, false);
            }

            for (var colour = 0; colour < GameLogic.CubeTypeCount; ++colour)
                m_CardsRemaining[playerIndex, colour] = 0;
        }

        if (m_CubesRemainingInBag() < NumberOfCubes)
            State = RaceState.Finished;

        if (State == RaceState.Finished)
        {
            if (m_RaceUI) m_RaceUI.SetFinished();
            for (var i = 0; i < NumberOfCubes; i++) m_Cubes[i] = CupCardCubeColour.Invalid;
            return;
        }

        for (var i = 0; i < NumberOfCubes; i++)
        {
            var cubeColour = m_NextCube();
            m_Cubes[i] = cubeColour;
            var colour = (int)cubeColour;
            if (m_RaceUI) m_RaceUI.SetCube(i, cubeColour);
            for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
                ++m_CardsRemaining[playerIndex, colour];
        }

        if (m_RaceUI) m_RaceUI.StartRace(State);
    }

    Player ComputeWinner(Player currentPlayer)
    {
        var maxScorePlayer = Player.Unknown;
        var maxScoreValue = -1;
        var minScorePlayer = Player.Unknown;
        var minScoreValue = 9999;
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            var score = 0;
            var player = (Player)playerIndex;
            for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
                score += m_Cards[playerIndex, cardIndex].Value;

            if (score == maxScoreValue) maxScorePlayer = currentPlayer;

            if (score > maxScoreValue)
            {
                maxScoreValue = score;
                maxScorePlayer = player;
            }

            if (score == minScoreValue) minScorePlayer = currentPlayer;

            if (score < minScoreValue)
            {
                minScoreValue = score;
                minScorePlayer = player;
            }
        }

        //Debug.Log ("max " + maxScorePlayer + " " + maxScoreValue + " CurrentPlayer:" + currentPlayer);
        //Debug.Log ("min " + minScorePlayer + " " + minScoreValue + " CurrentPlayer:" + currentPlayer);
        if (State == RaceState.Lowest)
            return minScorePlayer;
        if (State == RaceState.Highest)
            return maxScorePlayer;
        Debug.LogError("m_state is not Lowest or Highest");
        return Player.Unknown;
    }

    void FinishRace(Player currentPlayer)
    {
        Winner = ComputeWinner(currentPlayer);

        //Debug.Log (State + " Player " + Winner + " won");
        for (var c = 0; c < NumberOfCubes; ++c)
        {
            var colour = m_Cubes[c];
            m_AddCubeToPlayer(Winner, colour);
            m_Cubes[c] = CupCardCubeColour.Invalid;
        }

        m_FinishRace(this);
    }

    public void Initialise(int numberOfCubes, RaceUI raceUI,
        CubesRemainingInBagDelegate cubesRemainingInBag,
        NextCubeDelegate nextCube,
        AddCubeToPlayerDelegate addCubeToPlayer,
        DiscardCardDelegate discardCard,
        FinishRaceDelegate finishRace)
    {
        m_CubesRemainingInBag = cubesRemainingInBag;
        m_NextCube = nextCube;
        m_AddCubeToPlayer = addCubeToPlayer;
        m_DiscardCard = discardCard;
        m_FinishRace = finishRace;

        m_RaceUI = raceUI;
        NumberOfCubes = numberOfCubes;
        State = RaceState.Finished;
        Winner = Player.Unknown;
        m_Cards = new Card [GameLogic.PlayerCount, NumberOfCubes];
        m_CardsPlayed = new int [GameLogic.PlayerCount];
        m_CardsRemaining = new int [GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_Cubes = new CupCardCubeColour [NumberOfCubes];
    }

    public void NewGame()
    {
        // StartRace will flip the state
        if (NumberOfCubes % 2 == 1)
            State = RaceState.Highest;
        else
            State = RaceState.Lowest;
        StartRace();
    }

    public bool PlayCard(Player side, Card card, Player currentPlayer)
    {
        //Debug.Log(Name + " PlayCard " + side + " " + card.Colour + " " + card.Value);
        if (State == RaceState.Finished) return false;
        var sideIndex = (int)side;
        var cardIndex = m_CardsPlayed[sideIndex];
        if (cardIndex == NumberOfCubes) return false;
        var cardColour = (int)card.Colour;
        if (m_CardsRemaining[sideIndex, cardColour] == 0) return false;
        --m_CardsRemaining[sideIndex, cardColour];
        ++m_CardsPlayed[sideIndex];
        if (m_RaceUI)
        {
            m_RaceUI.SetPlayedCardToCard(sideIndex, cardIndex, card);
            m_RaceUI.SetPlayCardButtons(sideIndex, cardIndex, true);
            m_RaceUI.SetPlayCardButtonInteractable(sideIndex, cardIndex, false);
        }

        m_Cards[sideIndex, cardIndex] = card;

        var raceFinished = true;
        foreach (var cardsPlayed in m_CardsPlayed)
            if (cardsPlayed != NumberOfCubes)
                raceFinished = false;
        if (raceFinished)
            FinishRace(currentPlayer);
        return true;
    }
}
