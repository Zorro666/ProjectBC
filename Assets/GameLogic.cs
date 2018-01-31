﻿using System.Collections.Generic;
using UnityEngine;
using BC;

public class GameLogic : MonoBehaviour
{
    public Color RaceLowestColour;
    public Color RaceHighestColour;
    public Color RaceFinishedColour;

    enum GameState
    {
        Initialising,
        NewGame,
        InGame,
        EndGame
    }

    enum TurnState
    {
        StartingPlayerTurn,
        PickCardFromHand,
        PickCardsFromHandToDiscard,
        PlayCardOnRace,
        FinishingRace,
        FinishingGame,
        EndingPlayerTurn
    }

    System.Random m_random;
    CupCardCubeColour[] m_cubes;
    Color[] m_cardCubeColours;
    int[] m_cubeCurrentCounts;
    int[] m_cubeStartingCounts;
    int[] m_cubeWinningCounts;
    GameState m_state;
    TurnState m_turnState;
    Race[] m_races;
    int m_frame;
    int m_cubesRemainingCount;
    int m_cubesTotalCount;
    int m_chosenHandCardCount;
    int[] m_chosenHandCards;
    bool m_playerAlreadyDiscarded;
    Player[] m_cupOwner;
    Card[] m_fullDeck;
    Queue<Card> m_drawDeck;
    Queue<Card> m_discardDeck;
    Player m_roundWinner;
    Player m_currentPlayer;
    Player m_lastRaceWinner;
    RaceLogic m_finishedRace;
    int m_maxNumCardsToSelectFromHand;

    //TODO: make a Player class and store this data per Player
    int[,] m_playerCubeCounts;
    int[] m_playerWildcardCubeCounts;
    bool[,] m_playerCups;
    Card[,] m_playerHands;

    GameUI m_gameUI;

    static public int CubeTypeCount => (int)CupCardCubeColour.Count;

    static public int PlayerCount
    {
        get { return (int)Player.Count; }
    }

    static public int RacesCount
    {
        get { return 4; }
    }

    static public int HandSize
    {
        get { return 8; }
    }

    static public int MaxCupsPerPlayer
    {
        get { return 3; }
    }

    public int GetCubesRemainingCount()
    {
        return m_cubesRemainingCount;
    }

    public void DiscardCard(Card card)
    {
        m_discardDeck.Enqueue(card);
    }

    public Color GetCardCubeColour(CupCardCubeColour cardCubeType)
    {
        return m_cardCubeColours[(int)cardCubeType];
    }

    public void AddCubeToPlayer(Player player, CupCardCubeColour cubeType)
    {
        ++m_playerCubeCounts[(int)player, (int)cubeType];
    }

    public void FinishRace(RaceLogic race)
    {
        UpdateCubeCounts();
        m_turnState = TurnState.FinishingRace;
        m_finishedRace = race;
        m_lastRaceWinner = race.Winner;
        m_frame = 0;
        UpdateStatus();
    }

    public void PlayerGenericButtonClicked()
    {
        switch (m_turnState)
        {
            case TurnState.StartingPlayerTurn:
                ExitStartingPlayerTurn();
                break;
            case TurnState.PickCardsFromHandToDiscard:
                DiscardSelectedCardsFromHand();
                break;
            case TurnState.FinishingRace:
                ExitFinishingRace();
                break;
            case TurnState.FinishingGame:
                ExitFinishingGame();
                break;
            case TurnState.EndingPlayerTurn:
                ExitEndingPlayerTurn();
                break;
            default:
                Debug.LogError("Unknown m_turnState: " + m_turnState);
                break;
        }
    }

    public void PlayerHandCardClicked(GameObject source)
    {
        if (m_state != GameState.InGame)
            return;
        if ((m_turnState != TurnState.PickCardFromHand) && (m_turnState != TurnState.PlayCardOnRace) &&
            (m_turnState != TurnState.PickCardsFromHandToDiscard))
            return;
        var playerHandSource = source.transform.parent.parent.name;
        var currentPlayerHand = m_currentPlayer + "Player";
        if (playerHandSource != currentPlayerHand)
            return;
        //Debug.Log("PlayerHandCardClicked source:" + source.name + " greatgrandparent:" + source.transform.parent.parent.name);
        var cardName = source.name;
        if (!cardName.StartsWith("Card", System.StringComparison.Ordinal))
        {
            Debug.LogError("Invalid cardName must start with 'Card' " + cardName);
            return;
        }
        var cardNumber = int.Parse(cardName.Substring(4));
        if ((cardNumber < 1) || (cardNumber > 8))
        {
            Debug.LogError("Invalid cardNumber " + cardNumber);
            return;
        }
        var oldChosenCardIndex = m_chosenHandCards[0];
        var newChosenCardIndex = cardNumber - 1;
        // Deselect card if already selected
        for (var i = 0; i < m_maxNumCardsToSelectFromHand; ++i)
        {
            if ((m_chosenHandCards[i] >= 0) && (m_chosenHandCards[i] == newChosenCardIndex))
            {
                DeSelectCard(m_currentPlayer, newChosenCardIndex);
                m_chosenHandCards[i] = -1;
                newChosenCardIndex = -1;
            }
        }
        // Single select : deselect the previous card
        if ((m_maxNumCardsToSelectFromHand == 1) && (oldChosenCardIndex >= 0))
        {
            DeSelectCard(m_currentPlayer, oldChosenCardIndex);
        }

        // Remove any -1 entries
        for (var i = 0; i < m_maxNumCardsToSelectFromHand; ++i)
        {
            if (m_chosenHandCards[i] == -1)
            {
                for (var j = i; j < m_maxNumCardsToSelectFromHand-1; ++j)
                    m_chosenHandCards[j] = m_chosenHandCards[j+1];
                m_chosenHandCards[m_maxNumCardsToSelectFromHand - 1] = -1;
            }
        }

        // Multi-select : stop when reach the limit
        if ((m_maxNumCardsToSelectFromHand > 1) && (m_chosenHandCardCount == m_maxNumCardsToSelectFromHand))
            newChosenCardIndex = -1;

        // Select new card
        if (newChosenCardIndex >= 0)
        {
            SelectCard(m_currentPlayer, newChosenCardIndex);
            // If new card is selected move the selected cards along and add to position 0
            for (var i = m_maxNumCardsToSelectFromHand-1; i > 0; --i)
                m_chosenHandCards[i] = m_chosenHandCards[i-1];
            m_chosenHandCards[0] = newChosenCardIndex;
        }

        // Count how many chosen cards
        m_chosenHandCardCount = 0;
        for (var i = 0; i < m_maxNumCardsToSelectFromHand; ++i)
        {
            if (m_chosenHandCards[i] != -1)
                ++m_chosenHandCardCount;
        }

        //Debug.Log("ChosenHandCardCount:" + m_chosenHandCardCount);
        for (var i = 0; i < m_maxNumCardsToSelectFromHand; ++i)
        {
            //Debug.Log("ChosenCards[" + i + "] " + m_chosenHandCards[i]);
        }

        //var playerIndex = (int)m_currentPlayer;
        //var card = m_playerHands[playerIndex, m_chosenHandCardIndex];
        //Debug.Log("Selected card Colour: " + card.Colour + " Value: " + card.Value);
        switch (m_turnState)
        {
            case TurnState.PickCardFromHand:
                if (newChosenCardIndex != -1)
                    m_turnState = TurnState.PlayCardOnRace;
                break;
            case TurnState.PlayCardOnRace:
                if (newChosenCardIndex == -1)
                    m_turnState = TurnState.PickCardFromHand;
                break;
            case TurnState.PickCardsFromHandToDiscard:
                DiscardSingleCard();
                break;
        }
        if ((m_turnState == TurnState.PickCardFromHand) || (m_turnState == TurnState.PlayCardOnRace))
        {
            ComputeWhatRacesCanBePlayedOn();
        }
        UpdateStatus();
    }

    public void PlayCardButtonClicked(GameObject source)
    {
        if (m_state != GameState.InGame)
            return;
        if (m_turnState != TurnState.PlayCardOnRace)
            return;
        var raceName = source.transform.root.gameObject.name;
        if (raceName.StartsWith("Race", System.StringComparison.Ordinal) == false)
            Debug.LogError("PlayCard invalid raceName " + raceName);
        var raceNumber = int.Parse(raceName.Substring(4));
        //Debug.Log("PlayCard " + source.name + " " + raceName + " " + raceNumber);
        if ((raceNumber < 1) || (raceNumber > 4))
        {
            Debug.LogError("PlayCard invalid raceNumber " + raceNumber);
            return;
        }
        var sideString = source.name;
        Player side = Player.Unknown;
        if (sideString.StartsWith("Left", System.StringComparison.Ordinal))
            side = Player.Left;
        else if (sideString.StartsWith("Right", System.StringComparison.Ordinal))
            side = Player.Right;
        if (side == Player.Unknown)
        {
            Debug.LogError("PlayCard invalid side " + sideString);
            return;
        }

        var playerIndex = (int)m_currentPlayer;
        var cardIndex = m_chosenHandCards[0];
        if ((cardIndex < 0) || (cardIndex >= GameLogic.HandSize))
        {
            Debug.LogError("PlayCard invalid cardIndex " + cardIndex);
            return;
        }
        var card = m_playerHands[playerIndex, cardIndex];
        bool validCard = m_races[raceNumber - 1].PlayCard(side, card, m_currentPlayer);
        if (!validCard)
        {
            m_gameUI.SetStatusText("Wrong Race. Please choose a different Race");
            return;
        }

        HideHands();
        ResetChosenHandCards();
        ResetAllPlayCardButtons();
        if (!validCard)
            DiscardCard(card);
        PlayerDrawNewCard(m_currentPlayer, cardIndex);
        if (m_finishedRace == null)
        {
            EndPlayerTurn();
        }
    }

    public void ClaimCupButtonClicked(GameObject source)
    {
        if (m_state != GameState.InGame)
            return;
        var cupName = source.name;
        Debug.Log("ClaimCup:" + source.name + " by " + m_currentPlayer);
        var cupType = CupCardCubeColour.Count;
        for (CupCardCubeColour c = CupCardCubeColour.Grey; c < CupCardCubeColour.Count; ++c)
        {
            if (c.ToString() == cupName)
            {
                cupType = c;
                break;
            }
        }
        if (cupType == CupCardCubeColour.Count)
        {
            Debug.LogError("ClaimCup:" + cupName + " unknown cupType");
            return;
        }
        var cupIndex = (int)cupType;
        var playerIndex = (int)m_currentPlayer;
        var cubeCountToWin = m_cubeWinningCounts[cupIndex];
        if (m_playerCubeCounts[playerIndex, cupIndex] >= cubeCountToWin)
        {
            Debug.LogError("ClaimCup:" + cupName + " don't need wildcards to win");
            return;
        }
        if ((m_playerCubeCounts[playerIndex, cupIndex] + m_playerWildcardCubeCounts[playerIndex]) < cubeCountToWin)
        {
            Debug.LogError("ClaimCup:" + cupName + " can't win the cup");
            return;
        }
        var numWildCardsNeeded = cubeCountToWin - m_playerCubeCounts[playerIndex, cupIndex];
        AwardCupToPlayer(m_currentPlayer, cupType);
        m_playerCubeCounts[playerIndex, cupIndex] = 0;
        m_playerWildcardCubeCounts[playerIndex] -= numWildCardsNeeded;
        UpdateCubeCounts();
        ComputeHasGameEnded();
    }

    int CubesTotalCount
    {
        get { return m_cubesTotalCount; }
        set { m_cubesTotalCount = value; }
    }

    void PlayerDrawNewCard(Player player, int cardIndex)
    {
        if (m_drawDeck.Count == 0)
            StartDrawDeckFromDiscardDeck();
        ReplacePlayerCardInHand(player, cardIndex);
    }

    bool ComputeHasGameEnded()
    {
        for (Player player = Player.Left; player < Player.Count; ++player)
        {
            if (HasPlayerWon(player))
            {
                m_roundWinner = player;
                m_turnState = TurnState.FinishingGame;
                UpdateStatus();
                return true;
            }
        }
        return false;
    }

    void ExitFinishingRace()
    {
        m_gameUI.HidePlayerGenericButtons();
        if (ComputeHasGameEnded())
            return;

        m_frame = 0;
        m_state = GameState.InGame;
        m_finishedRace.StartRace();
        m_finishedRace = null;
        EndPlayerTurn();
    }

    void ExitFinishingGame()
    {
        m_gameUI.HidePlayerGenericButtons();
        m_state = GameState.Initialising;
    }

    void ResetChosenHandCards()
    {
        m_chosenHandCardCount = 0;
        for (var i = 0; i < 4; ++i)
            m_chosenHandCards[i] = -1;
    }

    void ExitStartingPlayerTurn()
    {
        ResetChosenHandCards();

        m_gameUI.HidePlayerGenericButtons();
        ShowHand(m_currentPlayer);
        if (PlayerCanPlayCardOnARace())
        {
            m_turnState = TurnState.PickCardFromHand;
            m_maxNumCardsToSelectFromHand = 1;
        }
        else
        {
            m_maxNumCardsToSelectFromHand = 4;
            m_turnState = TurnState.PickCardsFromHandToDiscard;
            SetPlayerGenericButtonText("Discard " + m_chosenHandCardCount + " Cards from Hand");
            ShowPlayerGenericButton();
        }
        UpdateStatus();
    }

    void DiscardSingleCard()
    {
        SetPlayerGenericButtonText("Discard " + m_chosenHandCardCount + " Cards from Hand");
        ShowPlayerGenericButton();
        m_turnState = TurnState.PickCardsFromHandToDiscard;
        UpdateStatus();
    }

    void DiscardSelectedCardsFromHand()
    {
        var playerIndex = (int)m_currentPlayer;
        for (var i = 0; i < m_chosenHandCardCount; ++i)
        {
            var cardIndex = m_chosenHandCards[i];
            if (cardIndex < 0)
            {
                Debug.LogError("Discard[" + i + "] Invalid cardIndex " + cardIndex);
                continue;
            }
            var card = m_playerHands[playerIndex, cardIndex];
            Debug.Log("Discard[" + i + "] Card Colour " + card.Colour + " Value " + card.Value);
            DiscardCard(card);
            PlayerDrawNewCard(m_currentPlayer, cardIndex);
            m_chosenHandCards[i] = -1;
        }
        m_chosenHandCardCount = 0;

        //check if player can still play a card and let them play it
        Debug.Log("PlayerAlreadyDiscarded " + m_playerAlreadyDiscarded);
        if (m_playerAlreadyDiscarded)
        {
            EndPlayerTurn();
        }
        else
        {
            m_playerAlreadyDiscarded = true;
            ExitStartingPlayerTurn();
        }
    }

    bool PlayerCanPlayCardOnARace()
    {
        var playerIndex = (int)m_currentPlayer;
        bool canPlayCard = false;
        for (var cardIndex = 0; cardIndex < GameLogic.HandSize; ++cardIndex)
        {
            var card = m_playerHands[playerIndex, cardIndex];
            foreach (var race in m_races)
                canPlayCard |= race.CanPlayCard(card);
        }
        return canPlayCard;
    }

    void ResetAllPlayCardButtons()
    {
        foreach (var race in m_races)
            race.ResetPlayCardButtons();
    }

    void ComputeWhatRacesCanBePlayedOn()
    {
        var cardIndex = m_chosenHandCards[0];
        if (cardIndex < 0)
        {
            ResetAllPlayCardButtons();
            return;
        }
        var card = m_playerHands[(int)m_currentPlayer, cardIndex];
        foreach (var race in m_races)
            race.SetPlayCardButtons(card);
    }

    public CupCardCubeColour NextCube()
    {
        if (m_cubesRemainingCount > 0)
        {
            m_cubesRemainingCount -= 1;
        }
        var cubeType = m_cubes[m_cubesRemainingCount];
        var cubeTypeIndex = (int)cubeType;
        m_cubeCurrentCounts[cubeTypeIndex] -= 1;
        if (m_cubeCurrentCounts[cubeTypeIndex] < 0)
        {
            Debug.LogError("Negative cubeCounts " + cubeType);
        }
        return m_cubes[m_cubesRemainingCount];
    }

    void Initialise()
    {
        m_gameUI = GetComponent<GameUI>();
        if (m_gameUI == null)
            Debug.LogError("Can't find 'GameUI' Component");

        var gamelogic = GetComponent<GameLogic>();
        if (gamelogic == null)
            Debug.LogError("Can't find 'GameLogic' Component");

        for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            m_cubeCurrentCounts[cubeType] = m_cubeStartingCounts[cubeType];

        CreateFullDeck();
        var cubeIndex = 0;
        for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
        {
            var count = m_cubeCurrentCounts[cubeType];
            for (var i = 0; i < count; ++i)
                m_cubes[cubeIndex++] = (CupCardCubeColour)cubeType;
        }
        if (CubesTotalCount != cubeIndex)
            Debug.LogError("cubes count not matching " + CubesTotalCount + " != " + cubeIndex);

        for (var i = 0; i < RacesCount; ++i)
        {
            var raceIndex = i + 1;
            var raceName = "/Race" + raceIndex;
            var raceGO = GameObject.Find(raceName);
            if (raceGO == null)
                Debug.LogError("Can't find Race[" + i + "] GameObject '" + raceName + "'");
            m_races[i] = raceGO.GetComponent<Race>();
            if (m_races[i] == null)
                Debug.LogError("Can't find Race[" + i + "] Component");
        }
        foreach (var race in m_races)
            race.Initialise(gamelogic);

        m_gameUI.Initialise();
    }

    void ResetUnclaimedCups()
    {
        for (var cupIndex = 0; cupIndex < GameLogic.CubeTypeCount; ++cupIndex)
        {
            if (!IsCupWon((CupCardCubeColour)cupIndex))
            {
                m_gameUI.SetCupStatus(cupIndex, true);
            }
            m_gameUI.SetCupInteractible(cupIndex, false);
        }
    }

    void NewGame()
    {
        ResetUnclaimedCups();
        m_gameUI.HidePlayerGenericButtons();
        CreateDrawDeck();
        ShuffleDrawDeck();
        DealHands();
        m_cubesRemainingCount = CubesTotalCount;
        ShuffleCubes();
        foreach (var race in m_races)
            race.NewGame();
        m_frame = 0;
        m_finishedRace = null;
        m_lastRaceWinner = Player.Unknown;
        ResetChosenHandCards();
        m_currentPlayer = (Player)m_random.Next(GameLogic.PlayerCount);
        m_roundWinner = Player.Unknown;

        HideHands();
        for (var player = 0; player < GameLogic.PlayerCount; ++player)
        {
            for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            {
                m_gameUI.SetPlayerCubeCountColour(player, cubeType, GetCardCubeColour((CupCardCubeColour)cubeType));
                m_playerCubeCounts[player, cubeType] = 0;
            }
            for (var cupIndex = 0; cupIndex < GameLogic.MaxCupsPerPlayer; ++cupIndex)
            {
                m_gameUI.SetPlayerCupActive(player, cupIndex, false);
                m_playerCups[player, cupIndex] = false;
            }
            m_playerWildcardCubeCounts[player] = 0;
        }
        UpdateCubeCounts();
        for (var cupType = 0; cupType < GameLogic.CubeTypeCount; ++cupType)
            m_cupOwner[cupType] = Player.Unknown;
        StartPlayerTurn();
    }

    bool IsCupWon(CupCardCubeColour cupColour)
    {
        return (m_cupOwner[(int)cupColour] != Player.Unknown);
    }

    void AwardCupToPlayer(Player player, CupCardCubeColour cupColour)
    {
        var playerIndex = (int)player;
        var cupIndex = (int)cupColour;
        if (IsCupWon(cupColour))
        {
            Debug.LogError("Cup has already been won " + cupColour + " by " + m_cupOwner[cupIndex]);
        }
        m_playerCups[playerIndex, cupIndex] = true;
        m_cupOwner[cupIndex] = player;

        m_gameUI.SetCupStatus(cupIndex, false);

        var cupDisplayIndex = NumCupsPlayerHasWon(player) - 1;
        if (cupDisplayIndex < GameLogic.MaxCupsPerPlayer)
        {
            var cubeCountToWin = m_cubeWinningCounts[cupIndex];
            var colour = GetCardCubeColour(cupColour);
            m_gameUI.SetPlayerCupActive(playerIndex, cupDisplayIndex, true);
            m_gameUI.SetPlayerCup(playerIndex, cupDisplayIndex, cupColour, cubeCountToWin, colour);
        }
    }

    int NumCupsPlayerHasWon(Player player)
    {
        var cupsWonCount = 0;
        for (var cupType = 0; cupType < GameLogic.CubeTypeCount; ++cupType)
            cupsWonCount += (m_playerCups[(int)player, (int)cupType] == true) ? 1 : 0;
        return cupsWonCount;
    }

    bool HasPlayerWon(Player player)
    {
        return (NumCupsPlayerHasWon(player) >= 3);
    }

    void UpdateCubeCounts()
    {
        for (Player player = Player.Left; player < Player.Count; ++player)
            AwardCupsToPlayer(player);

        // Do wildcards after cups are awarded 
        for (Player player = Player.Left; player < Player.Count; ++player)
            UpdateWildcardCubesForPlayer(player);

        for (Player player = Player.Left; player < Player.Count; ++player)
            UpdateCubeCountsUIForPlayer(player);

        ResetUnclaimedCups();
        var playerIndex = (int)m_currentPlayer;
        var wildcardCount = m_playerWildcardCubeCounts[playerIndex];
        for (var cubeIndex = 0; cubeIndex < GameLogic.CubeTypeCount; ++cubeIndex)
        {
            if (!IsCupWon((CupCardCubeColour)cubeIndex))
            {
                var cubeValue = m_playerCubeCounts[playerIndex, cubeIndex];
                var cubeCountToWin = m_cubeWinningCounts[cubeIndex];
                if (cubeValue + wildcardCount >= cubeCountToWin)
                {
                    m_gameUI.SetCupInteractible(cubeIndex, true);
                }
            }
        }
    }

    void AwardCupsToPlayer(Player player)
    {
        var playerIndex = (int)player;
        for (var cupIndex = 0; cupIndex < GameLogic.CubeTypeCount; ++cupIndex)
        {
            var cubeValue = m_playerCubeCounts[playerIndex, cupIndex];
            var cubeCountToWin = m_cubeWinningCounts[cupIndex];
            CupCardCubeColour cubeType = (CupCardCubeColour)cupIndex;
            if (cubeValue >= cubeCountToWin)
            {
                AwardCupToPlayer(player, cubeType);
                m_playerCubeCounts[playerIndex, cupIndex] -= cubeCountToWin;
            }
        }
    }

    void UpdateWildcardCubesForPlayer(Player player)
    {
        var playerIndex = (int)player;
        for (var cubeIndex = 0; cubeIndex < GameLogic.CubeTypeCount; ++cubeIndex)
        {
            CupCardCubeColour cubeType = (CupCardCubeColour)cubeIndex;
            if (IsCupWon(cubeType))
            {
                var cubeValue = m_playerCubeCounts[playerIndex, cubeIndex];
                var numWildcardCubes = cubeValue / 3;
                m_playerWildcardCubeCounts[playerIndex] += numWildcardCubes;
                cubeValue -= (numWildcardCubes * 3);
                m_playerCubeCounts[playerIndex, cubeIndex] = cubeValue;
            }
        }
    }

    void UpdateCubeCountsUIForPlayer(Player player)
    {
        var playerIndex = (int)player;
        for (var cubeIndex = 0; cubeIndex < GameLogic.CubeTypeCount; ++cubeIndex)
        {
            var cubeValue = m_playerCubeCounts[playerIndex, cubeIndex];
            m_gameUI.SetPlayerCubeCountValue(playerIndex, cubeIndex, cubeValue);
        }
        var wildcardCubeCount = m_playerWildcardCubeCounts[playerIndex];
        m_gameUI.SetPlayerWildcardCubeCountValue(playerIndex, wildcardCubeCount);
    }

    void InGame()
    {
        switch (m_turnState)
        {
            case TurnState.FinishingRace:
                FinishingRace();
                break;
            case TurnState.FinishingGame:
                FinishingGame();
                break;
        }
        ++m_frame;
        if (m_frame == 30)
        {
            m_frame = 0;
        }
        if (!Validate())
            Debug.LogError("Validation failed!");
    }

    void EndGame()
    {
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
    }

    void FinishingRace()
    {
        ++m_frame;
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
    }

    void FinishingGame()
    {
        ++m_frame;
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
    }

    void StartPlayerTurn()
    {
        //DEBUG:DEBUG
        //m_playerWildcardCubeCounts[(int)m_currentPlayer] += m_random.Next(-1, 2);
        //if (m_playerWildcardCubeCounts[(int)m_currentPlayer] < 0)
            //m_playerWildcardCubeCounts[(int)m_currentPlayer] = 0;
        //DEBUG:DEBUG
        UpdateCubeCounts();

        m_maxNumCardsToSelectFromHand = 1;
        m_playerAlreadyDiscarded = false;
        ResetChosenHandCards();
        m_turnState = TurnState.StartingPlayerTurn;
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
        ResetAllPlayCardButtons();
        UpdateStatus();
    }

    void ShowPlayerGenericButton()
    {
        m_gameUI.ShowPlayerGenericButton((int)m_currentPlayer);
    }

    bool NeedEndPlayerTurn()
    {
        for (var cubeIndex = 0; cubeIndex < GameLogic.CubeTypeCount; ++cubeIndex)
        {
            if (!IsCupWon((CupCardCubeColour)cubeIndex))
            {
                //TODO: this should be tracked in logic not UI
                if (m_gameUI.IsCupInteractible(cubeIndex))
                    return true;
            }
        }
        return false;
    }

    void EndPlayerTurn()
    {
        if (NeedEndPlayerTurn())
        {
            m_turnState = TurnState.EndingPlayerTurn;
            SetPlayerGenericButtonText("Continue");
            ShowPlayerGenericButton();
            UpdateStatus();
        }
        else
        {
            ExitEndingPlayerTurn();
        }
    }

    void ExitEndingPlayerTurn()
    {
        m_gameUI.HidePlayerGenericButtons();
        HideHands();
        if (m_lastRaceWinner != Player.Unknown)
            m_currentPlayer = m_lastRaceWinner;

        m_currentPlayer++;
        if (m_currentPlayer == Player.Count)
            m_currentPlayer = Player.Left;
        m_lastRaceWinner = Player.Unknown;
        StartPlayerTurn();
    }

    void UpdateStatus()
    {
        switch (m_turnState)
        {
            case TurnState.StartingPlayerTurn:
                m_gameUI.SetStatusText(m_currentPlayer + " Player: Press Continue to Start Turn");
                break;
            case TurnState.PickCardFromHand:
                m_gameUI.SetStatusText("Choose a Card to Play");
                break;
            case TurnState.PlayCardOnRace:
                m_gameUI.SetStatusText("Choose a Race to Play on");
                break;
            case TurnState.PickCardsFromHandToDiscard:
                m_gameUI.SetStatusText("No card can be Played. Select Cards to Discard");
                break;
            case TurnState.FinishingRace:
                m_gameUI.SetStatusText(m_lastRaceWinner + " Player Won the Race");
                break;
            case TurnState.FinishingGame:
                m_gameUI.SetStatusText(m_roundWinner + " Player Won the Game");
                break;
            case TurnState.EndingPlayerTurn:
                m_gameUI.SetStatusText(m_currentPlayer + " Player: Press Continue to Finish Turn");
                break;

        }
    }

    void CreateFullDeck()
    {
        int[][] allCardValues = new int[GameLogic.CubeTypeCount][];
        allCardValues[(int)CupCardCubeColour.Grey] = new int[] { 1, 4, 7, 10, 13 };
        allCardValues[(int)CupCardCubeColour.Blue] = new int[] { 1, 3, 5, 7, 9, 11, 13 };
        allCardValues[(int)CupCardCubeColour.Green] = new int[] { 1, 2, 4, 6, 7, 8, 10, 12, 13 };
        allCardValues[(int)CupCardCubeColour.Yellow] = new int[] { 1, 2, 3, 5, 6, 7, 8, 9, 11, 12, 13 };
        allCardValues[(int)CupCardCubeColour.Red] = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        var cardIndex = 0;
        for (var colour = 0; colour < GameLogic.CubeTypeCount; ++colour)
        {
            int[] cardValues = allCardValues[colour];
            if (m_cubeCurrentCounts[colour] != cardValues.Length)
                Debug.LogError((CupCardCubeColour)colour + " CardValues length does not match cubeCounts");
            foreach (var v in cardValues)
            {
                m_fullDeck[cardIndex] = new Card((CupCardCubeColour)colour, v);
                ++cardIndex;
            }
        }
    }

    void CreateDrawDeck()
    {
        m_drawDeck.Clear();
        m_discardDeck.Clear();
        m_drawDeck = new Queue<Card>(m_fullDeck);
    }

    void StartDrawDeckFromDiscardDeck()
    {
        if (m_discardDeck.Count == 0)
            Debug.LogError("Zero sized discard deck");
        m_drawDeck = m_discardDeck;
        ShuffleDrawDeck();
        Debug.Log("New Draw Deck");
        m_discardDeck.Clear();
        if (m_drawDeck.Count == 0)
            Debug.LogError("Zero sized draw deck");
    }

    void ShuffleDrawDeck()
    {
        Card[] drawDeck = m_drawDeck.ToArray();
        for (var i = 0; i < drawDeck.Length; ++i)
        {
            var newIndex = m_random.Next(0, drawDeck.Length);
            var temp = drawDeck[newIndex];
            drawDeck[newIndex] = drawDeck[i];
            drawDeck[i] = temp;
        }
        m_drawDeck = new Queue<Card>(drawDeck);
    }

    Card DrawCard()
    {
        var card = m_drawDeck.Dequeue();
        return card;
    }

    void ReplacePlayerCardInHand(Player player, int cardIndex)
    {
        Card card = DrawCard();
        if (card == null)
            Debug.LogError("null Card from Draw Deck");
        m_playerHands[(int)player, cardIndex] = card;
        UpdatePlayerCardUI(player, cardIndex);
    }

    void DealHands()
    {
        for (var player = 0; player < GameLogic.PlayerCount; ++player)
        {
            for (var i = 0; i < GameLogic.HandSize; ++i)
            {
                ReplacePlayerCardInHand((Player)player, i);
            }
        }
    }

    void ShowHand(Player player)
    {
        m_gameUI.SetPlayerHandActive((int)player, true);
    }

    void HideHands()
    {
        for (var player = 0; player < GameLogic.PlayerCount; ++player)
            HideHand((Player)player);
    }

    void HideHand(Player player)
    {
        m_gameUI.SetPlayerHandActive((int)player, false);
    }

    void UpdatePlayerCardUI(Player player, int cardIndex)
    {
        var playerIndex = (int)player;
        Card card = m_playerHands[playerIndex, cardIndex];
        var colour = GetCardCubeColour(card.Colour);

        m_gameUI.SetPlayerCard(playerIndex, cardIndex, card, colour);
        m_gameUI.SetPlayerCardHighlighted(playerIndex, cardIndex, false);
    }

    void ShuffleCubes()
    {
        for (var i = 0; i < m_cubes.Length; ++i)
        {
            var newIndex = m_random.Next(0, m_cubes.Length);
            var temp = m_cubes[newIndex];
            m_cubes[newIndex] = m_cubes[i];
            m_cubes[i] = temp;
        }
    }

    void Awake()
    {
        m_random = new System.Random();
        m_cubeCurrentCounts = new int[GameLogic.CubeTypeCount];
        m_cubeStartingCounts = new int[GameLogic.CubeTypeCount];
        m_cubeStartingCounts[(int)CupCardCubeColour.Grey] = 5;
        m_cubeStartingCounts[(int)CupCardCubeColour.Blue] = 7;
        m_cubeStartingCounts[(int)CupCardCubeColour.Green] = 9;
        m_cubeStartingCounts[(int)CupCardCubeColour.Yellow] = 11;
        m_cubeStartingCounts[(int)CupCardCubeColour.Red] = 13;

        m_cubeWinningCounts = new int[GameLogic.CubeTypeCount];
        for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            m_cubeWinningCounts[cubeType] = (m_cubeStartingCounts[cubeType] + 1) / 2;

        var numCubesTotal = 0;
        foreach (var count in m_cubeStartingCounts)
            numCubesTotal += count;
        CubesTotalCount = numCubesTotal;

        m_cardCubeColours = new Color[GameLogic.CubeTypeCount];
        m_cardCubeColours[0] = Color.grey;
        m_cardCubeColours[1] = Color.blue;
        m_cardCubeColours[2] = Color.green;
        m_cardCubeColours[3] = Color.yellow;
        m_cardCubeColours[4] = Color.red;

        m_cubes = new CupCardCubeColour[CubesTotalCount];
        m_races = new Race[GameLogic.RacesCount];
        m_state = GameState.Initialising;

        m_fullDeck = new Card[CubesTotalCount];
        m_drawDeck = new Queue<Card>();
        m_discardDeck = new Queue<Card>();
        m_currentPlayer = Player.Unknown;

        m_chosenHandCards = new int[4];

        m_playerCubeCounts = new int[GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_playerWildcardCubeCounts = new int[GameLogic.PlayerCount];
        m_playerHands = new Card[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCups = new bool[GameLogic.PlayerCount, GameLogic.CubeTypeCount];

        m_cupOwner = new Player[GameLogic.CubeTypeCount];
    }

    void Start()
    {
    }

    bool Validate()
    {
        bool allOk = true;
        //TODO:allOk &= ValidateCards();
        allOk &= ValidateCups();
        allOk &= ValidateCubes();
        return allOk;
    }

    bool ValidateCards()
    {
        //TODO: Verify every card is in draw deck or discard deck or player hand or played on a race
        return false;
    }

    bool ValidateCups()
    {
        // Verify each cup has only been won by one player or by no players
        bool allOk = true;
        for (var cupType = 0; cupType < GameLogic.CubeTypeCount; ++cupType)
        {
            var cupWinCount = 0;
            for (var player = 0; player < GameLogic.PlayerCount; ++player)
                cupWinCount += (m_playerCups[player, cupType] == true) ? 1 : 0;
            if (cupWinCount > 1)
            {
                Debug.LogError("Cup " + (CupCardCubeColour)cupType + " Invalid cupWinCount " + cupWinCount);
                allOk = false;
            }
        }
        return allOk;
    }

    bool ValidateCubes()
    {
        // Verify count of cubes left to play is correct
        int[] cubeCounts = new int[GameLogic.CubeTypeCount];
        bool allOk = true;
        for (var i = 0; i < m_cubesRemainingCount; ++i)
        {
            var cube = m_cubes[i];
            switch (cube)
            {
                case CupCardCubeColour.Grey:
                    cubeCounts[(int)cube]++;
                    break;
                case CupCardCubeColour.Blue:
                    cubeCounts[(int)cube]++;
                    break;
                case CupCardCubeColour.Green:
                    cubeCounts[(int)cube]++;
                    break;
                case CupCardCubeColour.Yellow:
                    cubeCounts[(int)cube]++;
                    break;
                case CupCardCubeColour.Red:
                    cubeCounts[(int)cube]++;
                    break;
                default:
                    allOk = false;
                    Debug.LogError("Unknown cube value " + cube);
                    break;
            }
        }
        for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
        {
            if (m_cubeCurrentCounts[cubeType] != cubeCounts[cubeType])
            {
                allOk = false;
                Debug.LogError("Current Cube count is incorrect " + (CupCardCubeColour)cubeType + " " + cubeCounts[cubeType]);
            }
        }
        //TODO: count cubes in bag + races + players must match the totals
        //TODO: validate player cube counts and cups won and cubes remaining and wildcards
        return allOk;
    }

    void SetPlayerGenericButtonText(string text)
    {
        var playerStart = (int)m_currentPlayer;
        var playerEnd = playerStart + 1;
        if (m_currentPlayer == Player.Unknown)
        {
            playerStart = (int)Player.Left;
            playerEnd = GameLogic.PlayerCount;
        }
        for (var p = playerStart; p < playerEnd; ++p)
        {
            m_gameUI.SetPlayerGenericButtonText(p, text);
        }
    }

    void DeSelectCard(Player player, int cardIndex)
    {
        if (cardIndex < 0)
        {
            Debug.LogError("Invalid cardIndex " + cardIndex);
            return;
        }
        m_gameUI.SetPlayerCardHighlighted((int)player, cardIndex, false);
    }

    void SelectCard(Player player, int cardIndex)
    {
        if (cardIndex < 0)
        {
            Debug.LogError("Invalid cardIndex " + cardIndex);
            return;
        }
        m_gameUI.SetPlayerCardHighlighted((int)player, cardIndex, true);
    }

    void Update() 
    {
        switch (m_state)
        {
            case GameState.Initialising:
                Initialise();
                m_state = GameState.NewGame;
                break;
            case GameState.NewGame:
                NewGame();
                m_state = GameState.InGame;
                break;
            case GameState.InGame:
                InGame();
                break;
            case GameState.EndGame:
                EndGame();
                break;
        }
    }
}