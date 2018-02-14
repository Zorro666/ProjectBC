using System;
using System.Collections.Generic;
using BC;
using UnityEngine;
using Random = System.Random;

public class GameLogic : MonoBehaviour, ISerializationCallbackReceiver
{
    Color[] m_CardCubeColours;
    int m_ChosenHandCardCount;
    int[] m_ChosenHandCards;
    int[] m_CubeCurrentCounts;
    [SerializeField]
    CupCardCubeColour[] m_Cubes;
    int m_CubesRemainingCount;
    int[] m_CubeStartingCounts;
    int[] m_CubeWinningCounts;
    Player[] m_CupOwner;
    int[,] m_CupPayment;
    Player m_CurrentPlayer;
    Queue<Card> m_DiscardDeck;
    Queue<Card> m_DrawDeck;
    RaceLogic m_FinishedRace;
    Card[] m_FullDeck;

    GameUI m_GameUI;
    Player m_LastRaceWinner;
    int m_MaxNumCardsToSelectFromHand;
    bool m_PlayerAlreadyDiscarded;

    //TODO: make a Player class and store this data per Player
    int[,] m_PlayerCubeCounts;
    bool[,] m_PlayerCups;
    Card[,] m_PlayerHands;
    int[,] m_PlayerWildcardCubeCounts;
    Race[] m_Races;

    Random m_Random;
    Player m_RoundWinner;
    GameState m_State;
    TurnState m_TurnState;
    public Color RaceFinishedColour;
    public Color RaceHighestColour;
    public Color RaceLowestColour;

    public bool RobotActive { get; private set; }
    public bool NeedEndPlayerTurn { get; private set; }

    public static int CubeTypeCount => (int)CupCardCubeColour.Count;

    public static int PlayerCount => (int)Player.Count;

    public static int RacesCount => 4;

    public static int HandSize => 8;

    public static int MaxCupsPerPlayer => 3;

    int CubesTotalCount { get; set; }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() { }

    public int GetCubesRemainingCount()
    {
        return m_CubesRemainingCount;
    }

    public void DiscardCard(Card card)
    {
        m_DiscardDeck.Enqueue(card);
    }

    public Color GetCardCubeColour(CupCardCubeColour cardCubeType)
    {
        return m_CardCubeColours[(int)cardCubeType];
    }

    public void AddCubeToPlayer(Player player, CupCardCubeColour cubeType)
    {
        ++m_PlayerCubeCounts[(int)player, (int)cubeType];
    }

    public void FinishRace(RaceLogic race)
    {
        UpdateCubeCounts();
        m_TurnState = TurnState.FinishingRace;
        m_FinishedRace = race;
        m_LastRaceWinner = race.Winner;
        UpdateStatus();
    }

    public void OnRobotButtonPressed()
    {
        RobotActive = !RobotActive;
        UpdateStatus();
    }

    public void PlayerGenericButtonClicked()
    {
        switch (m_TurnState)
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
                Debug.LogError("Unknown m_turnState: " + m_TurnState);
                break;
        }
    }

    public void PlayerHandCardClicked(GameObject source)
    {
        if (m_State != GameState.InGame)
            return;
        if (m_TurnState != TurnState.PickCardFromHand && m_TurnState != TurnState.PlayCardOnRace &&
            m_TurnState != TurnState.PickCardsFromHandToDiscard)
            return;
        var playerHandSource = source.transform.parent.parent.name;
        var currentPlayerHand = m_CurrentPlayer + "Player";
        if (playerHandSource != currentPlayerHand)
            return;

        //Debug.Log("PlayerHandCardClicked source:" + source.name + " greatgrandparent:" + source.transform.parent.parent.name);
        var cardName = source.name;
        if (!cardName.StartsWith("Card", StringComparison.Ordinal))
        {
            Debug.LogError("Invalid cardName must start with 'Card' " + cardName);
            return;
        }

        var cardNumber = int.Parse(cardName.Substring(4));
        if (cardNumber < 1 || cardNumber > 8)
        {
            Debug.LogError("Invalid cardNumber " + cardNumber);
            return;
        }

        CurrentPlayerSelectCardFromHand(cardNumber - 1);
    }

    void CurrentPlayerSelectCardFromHand(int cardNumber)
    {
        var oldChosenCardIndex = m_ChosenHandCards[0];
        var newChosenCardIndex = cardNumber;

        // Deselect card if already selected
        for (var i = 0; i < m_MaxNumCardsToSelectFromHand; ++i)
            if (m_ChosenHandCards[i] >= 0 && m_ChosenHandCards[i] == newChosenCardIndex)
            {
                DeSelectCard(m_CurrentPlayer, newChosenCardIndex);
                m_ChosenHandCards[i] = -1;
                newChosenCardIndex = -1;
            }

        // Single select : deselect the previous card
        if (m_MaxNumCardsToSelectFromHand == 1 && oldChosenCardIndex >= 0) DeSelectCard(m_CurrentPlayer, oldChosenCardIndex);

        // Remove any -1 entries
        for (var i = 0; i < m_MaxNumCardsToSelectFromHand; ++i)
            if (m_ChosenHandCards[i] == -1)
            {
                for (var j = i; j < m_MaxNumCardsToSelectFromHand - 1; ++j)
                    m_ChosenHandCards[j] = m_ChosenHandCards[j + 1];
                m_ChosenHandCards[m_MaxNumCardsToSelectFromHand - 1] = -1;
            }

        // Multi-select : stop when reach the limit
        if (m_MaxNumCardsToSelectFromHand > 1 && m_ChosenHandCardCount == m_MaxNumCardsToSelectFromHand)
            newChosenCardIndex = -1;

        // Select new card
        if (newChosenCardIndex >= 0)
        {
            SelectCard(m_CurrentPlayer, newChosenCardIndex);

            // If new card is selected move the selected cards along and add to position 0
            for (var i = m_MaxNumCardsToSelectFromHand - 1; i > 0; --i)
                m_ChosenHandCards[i] = m_ChosenHandCards[i - 1];
            m_ChosenHandCards[0] = newChosenCardIndex;
        }

        // Count how many chosen cards
        m_ChosenHandCardCount = 0;
        for (var i = 0; i < m_MaxNumCardsToSelectFromHand; ++i)
            if (m_ChosenHandCards[i] != -1)
                ++m_ChosenHandCardCount;

        //Debug.Log("ChosenHandCardCount:" + m_chosenHandCardCount);
        //for (var i = 0; i < m_MaxNumCardsToSelectFromHand; ++i)
        //{
            //Debug.Log("ChosenCards[" + i + "] " + m_chosenHandCards[i]);
        //}

        //var playerIndex = (int)m_currentPlayer;
        //var card = m_playerHands[playerIndex, m_chosenHandCardIndex];
        //Debug.Log("Selected card Colour: " + card.Colour + " Value: " + card.Value);
        switch (m_TurnState)
        {
            case TurnState.PickCardFromHand:
                if (newChosenCardIndex != -1)
                    m_TurnState = TurnState.PlayCardOnRace;
                break;
            case TurnState.PlayCardOnRace:
                if (newChosenCardIndex == -1)
                    m_TurnState = TurnState.PickCardFromHand;
                break;
            case TurnState.PickCardsFromHandToDiscard:
                DiscardSingleCard();
                break;
        }

        if (m_TurnState == TurnState.PickCardFromHand || m_TurnState == TurnState.PlayCardOnRace) ComputeWhatRacesCanBePlayedOn();

        UpdateStatus();
    }

    public void PlayCardButtonClicked(GameObject source)
    {
        if (m_State != GameState.InGame)
            return;
        if (m_TurnState != TurnState.PlayCardOnRace)
            return;
        var raceName = source.transform.root.gameObject.name;
        if (raceName.StartsWith("Race", StringComparison.Ordinal) == false)
            Debug.LogError("PlayCard invalid raceName " + raceName);
        var raceNumber = int.Parse(raceName.Substring(4));

        //Debug.Log("PlayCard " + source.name + " " + raceName + " " + raceNumber);
        if (raceNumber < 1 || raceNumber > 4)
        {
            Debug.LogError("PlayCard invalid raceNumber " + raceNumber);
            return;
        }

        var sideString = source.name;
        var side = Player.Unknown;
        if (sideString.StartsWith("Left", StringComparison.Ordinal))
            side = Player.Left;
        else if (sideString.StartsWith("Right", StringComparison.Ordinal))
            side = Player.Right;
        if (side == Player.Unknown)
        {
            Debug.LogError("PlayCard invalid side " + sideString);
            return;
        }

        var race = m_Races[raceNumber - 1];
        var validCard = PlaySelectedCardOnARace(side, race);
        if (!validCard) m_GameUI.SetStatusText("Wrong Race. Please choose a different Race");
    }

    bool PlaySelectedCardOnARace(Player side, Race race)
    {
        var playerIndex = (int)m_CurrentPlayer;
        var cardIndex = m_ChosenHandCards[0];
        if (cardIndex < 0 || cardIndex >= HandSize)
        {
            Debug.LogError("PlaySelectedCardOnARace invalid cardIndex " + cardIndex);
            return false;
        }

        var card = m_PlayerHands[playerIndex, cardIndex];
        var validCard = race.PlayCard(side, card, m_CurrentPlayer);
        if (!validCard)
            return false;

        HideHands();
        ResetChosenHandCards();
        ResetAllPlayCardButtons();
        PlayerDrawNewCard(m_CurrentPlayer, cardIndex);
        if (m_FinishedRace == null) EndPlayerTurn();

        return true;
    }

    public void ClaimCupButtonClicked(GameObject source)
    {
        if (m_State != GameState.InGame)
            return;
        var cupName = source.name;
        Debug.Log("ClaimCupButton:" + cupName + " by " + m_CurrentPlayer);
        var cupType = CupCardCubeColour.Count;
        for (var c = CupCardCubeColour.Grey; c < CupCardCubeColour.Count; ++c)
            if (c.ToString() == cupName)
            {
                cupType = c;
                break;
            }

        if (cupType == CupCardCubeColour.Count)
        {
            Debug.LogError("ClaimCupButton:" + cupName + " unknown cupType");
            return;
        }

        string reason;
        if (!CanClaimCup(cupType, out reason))
        {
            Debug.LogError("ClaimCupButton: " + cupType + " CanClaimCup failed " + reason);
            return;
        }

        ClaimCup(cupType);
    }

    bool CanClaimCup(CupCardCubeColour cupType, out string reason)
    {
        var cupIndex = (int)cupType;
        var playerIndex = (int)m_CurrentPlayer;
        var cubeCountToWin = m_CubeWinningCounts[cupIndex];
        if (IsCupWon(cupType))
        {
            reason = "cup is already owned";
            return false;
        }

        if (m_PlayerCubeCounts[playerIndex, cupIndex] >= cubeCountToWin)
        {
            reason = "don't need wildcard cubes to claim cup";
            return false;
        }

        var playerTotalWildcardCubeCount = GetPlayerWildcardCubeCount(m_CurrentPlayer);
        if (m_PlayerCubeCounts[playerIndex, cupIndex] + playerTotalWildcardCubeCount < cubeCountToWin)
        {
            reason = "can't claim cup using wildcards";
            return false;
        }

        reason = "";
        return true;
    }

    void ClaimCup(CupCardCubeColour cupType)
    {
        string reason;
        if (!CanClaimCup(cupType, out reason))
        {
            Debug.LogError("ClaimCup: " + cupType + " CanClaimCup failed " + reason);
            return;
        }

        var cupIndex = (int)cupType;
        var playerIndex = (int)m_CurrentPlayer;
        var cubeCountToWin = m_CubeWinningCounts[cupIndex];
        var numWildCardsNeeded = cubeCountToWin - m_PlayerCubeCounts[playerIndex, cupIndex];
        AwardCupToPlayer(m_CurrentPlayer, cupType);
        m_CupPayment[cupIndex, cupIndex] += m_PlayerCubeCounts[playerIndex, cupIndex];
        m_PlayerCubeCounts[playerIndex, cupIndex] = 0;
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
        {
            var wildcardCount = m_PlayerWildcardCubeCounts[playerIndex, cubeType];
            var wildcardUsed = Math.Min(wildcardCount, numWildCardsNeeded);
            m_CupPayment[cupIndex, cubeType] += wildcardUsed * 3;
            m_PlayerWildcardCubeCounts[playerIndex, cubeType] -= wildcardUsed;
            numWildCardsNeeded -= wildcardUsed;
            if (numWildCardsNeeded <= 0)
                break;
        }

        if (numWildCardsNeeded > 0) Debug.LogError("ClaimCup:" + cupType + " invalid numWildCardsNeeded:" + numWildCardsNeeded);

        UpdateCubeCounts();
        ComputeHasGameEnded();
    }

    int GetPlayerWildcardCubeCount(Player player)
    {
        var playerIndex = (int)player;
        var playerTotalWildcardCubeCount = 0;
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType) playerTotalWildcardCubeCount += m_PlayerWildcardCubeCounts[playerIndex, cubeType];

        return playerTotalWildcardCubeCount;
    }

    void PlayerDrawNewCard(Player player, int cardIndex)
    {
        if (m_DrawDeck.Count == 0)
            StartDrawDeckFromDiscardDeck();
        ReplacePlayerCardInHand(player, cardIndex);
    }

    bool ComputeHasGameEnded()
    {
        for (var player = Player.Left; player < Player.Count; ++player)
            if (HasPlayerWon(player))
            {
                m_RoundWinner = player;
                m_TurnState = TurnState.FinishingGame;
                UpdateStatus();
                return true;
            }

        return false;
    }

    void ExitFinishingRace()
    {
        m_GameUI.HidePlayerGenericButtons();
        if (ComputeHasGameEnded())
            return;

        m_State = GameState.InGame;
        m_FinishedRace.StartRace();
        m_FinishedRace = null;
        EndPlayerTurn();
    }

    void ExitFinishingGame()
    {
        m_GameUI.HidePlayerGenericButtons();
        m_State = GameState.Initialising;
    }

    void ResetChosenHandCards()
    {
        m_ChosenHandCardCount = 0;
        for (var i = 0; i < 4; ++i)
            m_ChosenHandCards[i] = -1;
    }

    void ExitStartingPlayerTurn()
    {
        ResetChosenHandCards();

        m_GameUI.HidePlayerGenericButtons();
        ShowHand(m_CurrentPlayer);
        if (PlayerCanPlayCardOnARace())
        {
            m_TurnState = TurnState.PickCardFromHand;
            m_MaxNumCardsToSelectFromHand = 1;
        }
        else
        {
            m_MaxNumCardsToSelectFromHand = 4;
            m_TurnState = TurnState.PickCardsFromHandToDiscard;
            SetPlayerGenericButtonText("Discard " + m_ChosenHandCardCount + " Cards from Hand");
            ShowPlayerGenericButton();
        }

        UpdateStatus();
    }

    void DiscardSingleCard()
    {
        SetPlayerGenericButtonText("Discard " + m_ChosenHandCardCount + " Cards from Hand");
        ShowPlayerGenericButton();
        m_TurnState = TurnState.PickCardsFromHandToDiscard;
        UpdateStatus();
    }

    void DiscardSelectedCardsFromHand()
    {
        var playerIndex = (int)m_CurrentPlayer;
        for (var i = 0; i < m_ChosenHandCardCount; ++i)
        {
            var cardIndex = m_ChosenHandCards[i];
            if (cardIndex < 0)
            {
                Debug.LogError("Discard[" + i + "] Invalid cardIndex " + cardIndex);
                continue;
            }

            var card = m_PlayerHands[playerIndex, cardIndex];
            Debug.Log("Discard[" + i + "] Card Colour " + card.Colour + " Value " + card.Value);
            DiscardCard(card);
            PlayerDrawNewCard(m_CurrentPlayer, cardIndex);
            m_ChosenHandCards[i] = -1;
        }

        m_ChosenHandCardCount = 0;

        //check if player can still play a card and let them play it
        Debug.Log("PlayerAlreadyDiscarded " + m_PlayerAlreadyDiscarded);
        if (m_PlayerAlreadyDiscarded)
        {
            EndPlayerTurn();
        }
        else
        {
            m_PlayerAlreadyDiscarded = true;
            ExitStartingPlayerTurn();
        }
    }

    bool PlayerCanPlayCardOnARace()
    {
        var playerIndex = (int)m_CurrentPlayer;
        var canPlayCard = false;
        for (var cardIndex = 0; cardIndex < HandSize; ++cardIndex)
        {
            var card = m_PlayerHands[playerIndex, cardIndex];
            foreach (var race in m_Races)
                canPlayCard |= race.CanPlayCard(card);
        }

        return canPlayCard;
    }

    void ResetAllPlayCardButtons()
    {
        foreach (var race in m_Races)
            race.ResetPlayCardButtons();
    }

    void ComputeWhatRacesCanBePlayedOn()
    {
        var cardIndex = m_ChosenHandCards[0];
        if (cardIndex < 0)
        {
            ResetAllPlayCardButtons();
            return;
        }

        var card = m_PlayerHands[(int)m_CurrentPlayer, cardIndex];
        foreach (var race in m_Races)
            race.SetPlayCardButtons(card);
    }

    public CupCardCubeColour NextCube()
    {
        if (m_CubesRemainingCount > 0) m_CubesRemainingCount -= 1;

        var cubeType = m_Cubes[m_CubesRemainingCount];
        var cubeTypeIndex = (int)cubeType;
        m_CubeCurrentCounts[cubeTypeIndex] -= 1;
        if (m_CubeCurrentCounts[cubeTypeIndex] < 0) Debug.LogError("Negative cubeCounts " + cubeType);

        return m_Cubes[m_CubesRemainingCount];
    }

    void Initialise()
    {
        m_GameUI = GetComponent<GameUI>();
        if (m_GameUI == null)
            Debug.LogError("Can't find 'GameUI' Component");

        var gamelogic = GetComponent<GameLogic>();
        if (gamelogic == null)
            Debug.LogError("Can't find 'GameLogic' Component");

        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
            m_CubeCurrentCounts[cubeType] = m_CubeStartingCounts[cubeType];

        CreateFullDeck();
        var cubeIndex = 0;
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
        {
            var count = m_CubeCurrentCounts[cubeType];
            for (var i = 0; i < count; ++i)
                m_Cubes[cubeIndex++] = (CupCardCubeColour)cubeType;
        }

        if (CubesTotalCount != cubeIndex)
            Debug.LogError("cubes count not matching " + CubesTotalCount + " != " + cubeIndex);

        for (var i = 0; i < RacesCount; ++i)
        {
            var raceIndex = i + 1;
            var raceName = "/Race" + raceIndex;
            var raceGameObject = GameObject.Find(raceName);
            if (raceGameObject == null)
                Debug.LogError("Can't find Race[" + i + "] GameObject '" + raceName + "'");
            else
                m_Races[i] = raceGameObject.GetComponent<Race>();
            if (m_Races[i] == null)
                Debug.LogError("Can't find Race[" + i + "] Component");
        }

        foreach (var race in m_Races)
            race.Initialise(gamelogic);

        m_GameUI.Initialise();
    }

    void ResetUnclaimedCups()
    {
        for (var cupIndex = 0; cupIndex < CubeTypeCount; ++cupIndex)
        {
            if (!IsCupWon((CupCardCubeColour)cupIndex)) m_GameUI.SetCupStatus(cupIndex, true);

            m_GameUI.SetCupInteractible(cupIndex, false);
            NeedEndPlayerTurn = false;
        }
    }

    void NewGame()
    {
        ResetUnclaimedCups();
        m_GameUI.HidePlayerGenericButtons();
        CreateDrawDeck();
        ShuffleDrawDeck();
        DealHands();
        m_CubesRemainingCount = CubesTotalCount;
        ShuffleCubes();
        foreach (var race in m_Races)
            race.NewGame();
        m_FinishedRace = null;
        m_LastRaceWinner = Player.Unknown;
        ResetChosenHandCards();
        m_CurrentPlayer = (Player)m_Random.Next(PlayerCount);
        m_RoundWinner = Player.Unknown;

        HideHands();
        for (var player = 0; player < PlayerCount; ++player)
        {
            for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
            {
                m_GameUI.SetPlayerCubeCountColour(player, cubeType, GetCardCubeColour((CupCardCubeColour)cubeType));
                m_PlayerCubeCounts[player, cubeType] = 0;
                m_PlayerCups[player, cubeType] = false;
                m_PlayerWildcardCubeCounts[player, cubeType] = 0;
            }

            for (var cupIndex = 0; cupIndex < MaxCupsPerPlayer; ++cupIndex) m_GameUI.SetPlayerCupActive(player, cupIndex, false);
        }

        UpdateCubeCounts();
        for (var cupType = 0; cupType < CubeTypeCount; ++cupType)
        {
            m_CupOwner[cupType] = Player.Unknown;
            for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType) m_CupPayment[cupType, cubeType] = 0;
        }

        StartPlayerTurn();
    }

    bool IsCupWon(CupCardCubeColour cupColour)
    {
        return m_CupOwner[(int)cupColour] != Player.Unknown;
    }

    void AwardCupToPlayer(Player player, CupCardCubeColour cupColour)
    {
        var playerIndex = (int)player;
        var cupIndex = (int)cupColour;
        if (IsCupWon(cupColour)) Debug.LogError("Cup has already been won " + cupColour + " by " + m_CupOwner[cupIndex]);

        if (m_PlayerCups[playerIndex, cupIndex])
            Debug.LogError("Player:" + player + " already won cup: " + cupColour);
        if (m_CupOwner[cupIndex] != Player.Unknown)
            Debug.LogError("Cup:" + cupColour + " already owned by " + m_CupOwner[cupIndex]);

        m_PlayerCups[playerIndex, cupIndex] = true;
        m_CupOwner[cupIndex] = player;

        m_GameUI.SetCupStatus(cupIndex, false);

        var cupDisplayIndex = NumCupsPlayerHasWon(player) - 1;
        if (cupDisplayIndex < MaxCupsPerPlayer)
        {
            var cubeCountToWin = m_CubeWinningCounts[cupIndex];
            var colour = GetCardCubeColour(cupColour);
            m_GameUI.SetPlayerCupActive(playerIndex, cupDisplayIndex, true);
            m_GameUI.SetPlayerCup(playerIndex, cupDisplayIndex, cupColour, cubeCountToWin, colour);
        }
    }

    int NumCupsPlayerHasWon(Player player)
    {
        var cupsWonCount = 0;
        for (var cupType = 0; cupType < CubeTypeCount; ++cupType)
            cupsWonCount += m_PlayerCups[(int)player, cupType] ? 1 : 0;
        return cupsWonCount;
    }

    bool HasPlayerWon(Player player)
    {
        return NumCupsPlayerHasWon(player) >= 3;
    }

    void UpdateCubeCounts()
    {
        for (var player = Player.Left; player < Player.Count; ++player)
            AwardCupsToPlayer(player);

        // Do wildcards after cups are awarded 
        for (var player = Player.Left; player < Player.Count; ++player)
            UpdateWildcardCubesForPlayer(player);

        for (var player = Player.Left; player < Player.Count; ++player)
            UpdateCubeCountsUIForPlayer(player);

        ResetUnclaimedCups();
        var playerIndex = (int)m_CurrentPlayer;
        var wildcardCount = GetPlayerWildcardCubeCount(m_CurrentPlayer);
        for (var cupIndex = 0; cupIndex < CubeTypeCount; ++cupIndex)
            if (!IsCupWon((CupCardCubeColour)cupIndex))
            {
                var cubeValue = m_PlayerCubeCounts[playerIndex, cupIndex];
                var cubeCountToWin = m_CubeWinningCounts[cupIndex];
                if (cubeValue + wildcardCount >= cubeCountToWin)
                {
                    m_GameUI.SetCupInteractible(cupIndex, true);
                    NeedEndPlayerTurn = true;
                }
            }
    }

    void AwardCupsToPlayer(Player player)
    {
        var playerIndex = (int)player;
        for (var cupIndex = 0; cupIndex < CubeTypeCount; ++cupIndex)
        {
            var cubeValue = m_PlayerCubeCounts[playerIndex, cupIndex];
            var cubeCountToWin = m_CubeWinningCounts[cupIndex];
            var cupType = (CupCardCubeColour)cupIndex;
            if (!IsCupWon(cupType))
                if (cubeValue >= cubeCountToWin)
                {
                    AwardCupToPlayer(player, cupType);
                    m_PlayerCubeCounts[playerIndex, cupIndex] -= cubeCountToWin;
                    m_CupPayment[cupIndex, cupIndex] += cubeCountToWin;
                }
        }
    }

    void UpdateWildcardCubesForPlayer(Player player)
    {
        var playerIndex = (int)player;
        for (var cubeIndex = 0; cubeIndex < CubeTypeCount; ++cubeIndex)
        {
            var cubeType = (CupCardCubeColour)cubeIndex;
            if (IsCupWon(cubeType))
            {
                var cubeValue = m_PlayerCubeCounts[playerIndex, cubeIndex];
                var numWildcardCubes = cubeValue / 3;
                m_PlayerWildcardCubeCounts[playerIndex, (int)cubeType] += numWildcardCubes;
                cubeValue -= numWildcardCubes * 3;
                m_PlayerCubeCounts[playerIndex, cubeIndex] = cubeValue;
            }
        }
    }

    void UpdateCubeCountsUIForPlayer(Player player)
    {
        var playerIndex = (int)player;
        for (var cubeIndex = 0; cubeIndex < CubeTypeCount; ++cubeIndex)
        {
            var cubeValue = m_PlayerCubeCounts[playerIndex, cubeIndex];
            m_GameUI.SetPlayerCubeCountValue(playerIndex, cubeIndex, cubeValue);
        }

        var wildcardCubeCount = GetPlayerWildcardCubeCount(player);
        m_GameUI.SetPlayerWildcardCubeCountValue(playerIndex, wildcardCubeCount);
    }

    void InGame()
    {
        switch (m_TurnState)
        {
            case TurnState.FinishingRace:
                FinishingRace();
                break;
            case TurnState.FinishingGame:
                FinishingGame();
                break;
        }

        if (!Validate())
        {
            Debug.LogError("Validation failed!");
            RobotActive = false;
            UpdateStatus();
        }
    }

    void EndGame()
    {
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
    }

    void FinishingRace()
    {
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
    }

    void FinishingGame()
    {
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
    }

    void StartPlayerTurn()
    {
        UpdateCubeCounts();

        m_MaxNumCardsToSelectFromHand = 1;
        m_PlayerAlreadyDiscarded = false;
        NeedEndPlayerTurn = false;
        ResetChosenHandCards();
        m_TurnState = TurnState.StartingPlayerTurn;
        SetPlayerGenericButtonText("Continue");
        ShowPlayerGenericButton();
        ResetAllPlayCardButtons();
        UpdateStatus();
    }

    void ShowPlayerGenericButton()
    {
        m_GameUI.ShowPlayerGenericButton((int)m_CurrentPlayer);
    }

    void EndPlayerTurn()
    {
        if (NeedEndPlayerTurn)
        {
            m_TurnState = TurnState.EndingPlayerTurn;
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
        m_GameUI.HidePlayerGenericButtons();
        HideHands();
        if (m_LastRaceWinner != Player.Unknown)
            m_CurrentPlayer = m_LastRaceWinner;

        m_CurrentPlayer++;
        if (m_CurrentPlayer == Player.Count)
            m_CurrentPlayer = Player.Left;
        m_LastRaceWinner = Player.Unknown;
        StartPlayerTurn();
    }

    void UpdateStatus()
    {
        switch (m_TurnState)
        {
            case TurnState.StartingPlayerTurn:
                m_GameUI.SetStatusText(m_CurrentPlayer + " Player: Press Continue to Start Turn");
                break;
            case TurnState.PickCardFromHand:
                m_GameUI.SetStatusText("Choose a Card to Play");
                break;
            case TurnState.PlayCardOnRace:
                m_GameUI.SetStatusText("Choose a Race to Play on");
                break;
            case TurnState.PickCardsFromHandToDiscard:
                m_GameUI.SetStatusText("No card can be Played. Select Cards to Discard");
                break;
            case TurnState.FinishingRace:
                m_GameUI.SetStatusText(m_LastRaceWinner + " Player Won the Race");
                break;
            case TurnState.FinishingGame:
                m_GameUI.SetStatusText(m_RoundWinner + " Player Won the Game");
                break;
            case TurnState.EndingPlayerTurn:
                m_GameUI.SetStatusText(m_CurrentPlayer + " Player: Press Continue to Finish Turn");
                break;
        }

        if (RobotActive)
            m_GameUI.SetRobotButtonText("Robot");
        else
            m_GameUI.SetRobotButtonText("Human");
    }

    void CreateFullDeck()
    {
        var allCardValues = new int [CubeTypeCount][];
        allCardValues[(int)CupCardCubeColour.Grey] = new[] { 1, 4, 7, 10, 13 };
        allCardValues[(int)CupCardCubeColour.Blue] = new[] { 1, 3, 5, 7, 9, 11, 13 };
        allCardValues[(int)CupCardCubeColour.Green] = new[] { 1, 2, 4, 6, 7, 8, 10, 12, 13 };
        allCardValues[(int)CupCardCubeColour.Yellow] = new[] { 1, 2, 3, 5, 6, 7, 8, 9, 11, 12, 13 };
        allCardValues[(int)CupCardCubeColour.Red] = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        var cardIndex = 0;
        for (var colour = 0; colour < CubeTypeCount; ++colour)
        {
            var cardValues = allCardValues[colour];
            if (m_CubeCurrentCounts[colour] != cardValues.Length)
                Debug.LogError((CupCardCubeColour)colour + " CardValues length does not match cubeCounts");
            foreach (var v in cardValues)
            {
                m_FullDeck[cardIndex] = new Card((CupCardCubeColour)colour, v);
                ++cardIndex;
            }
        }
    }

    void CreateDrawDeck()
    {
        m_DrawDeck.Clear();
        m_DiscardDeck.Clear();
        m_DrawDeck = new Queue<Card>(m_FullDeck);
    }

    void StartDrawDeckFromDiscardDeck()
    {
        if (m_DiscardDeck.Count == 0)
            Debug.LogError("Zero sized discard deck");
        m_DrawDeck = m_DiscardDeck;
        ShuffleDrawDeck();
        Debug.Log("New Draw Deck");
        m_DiscardDeck.Clear();
        if (m_DrawDeck.Count == 0)
            Debug.LogError("Zero sized draw deck");
    }

    void ShuffleDrawDeck()
    {
        var drawDeck = m_DrawDeck.ToArray();
        for (var i = 0; i < drawDeck.Length; ++i)
        {
            var newIndex = m_Random.Next(0, drawDeck.Length);
            var temp = drawDeck[newIndex];
            drawDeck[newIndex] = drawDeck[i];
            drawDeck[i] = temp;
        }

        m_DrawDeck = new Queue<Card>(drawDeck);
    }

    Card DrawCard()
    {
        var card = m_DrawDeck.Dequeue();
        return card;
    }

    void ReplacePlayerCardInHand(Player player, int cardIndex)
    {
        var card = DrawCard();
        if (card == null)
            Debug.LogError("null Card from Draw Deck");
        m_PlayerHands[(int)player, cardIndex] = card;
        UpdatePlayerCardUI(player, cardIndex);
    }

    void DealHands()
    {
        for (var player = 0; player < PlayerCount; ++player)
        for (var i = 0; i < HandSize; ++i)
            ReplacePlayerCardInHand((Player)player, i);
    }

    void ShowHand(Player player)
    {
        m_GameUI.SetPlayerHandActive((int)player, true);
    }

    void HideHands()
    {
        for (var player = 0; player < PlayerCount; ++player)
            HideHand((Player)player);
    }

    void HideHand(Player player)
    {
        m_GameUI.SetPlayerHandActive((int)player, false);
    }

    void UpdatePlayerCardUI(Player player, int cardIndex)
    {
        var playerIndex = (int)player;
        var card = m_PlayerHands[playerIndex, cardIndex];
        var colour = GetCardCubeColour(card.Colour);

        m_GameUI.SetPlayerCard(playerIndex, cardIndex, card, colour);
        m_GameUI.SetPlayerCardHighlighted(playerIndex, cardIndex, false);
    }

    void ShuffleCubes()
    {
        for (var i = 0; i < m_Cubes.Length; ++i)
        {
            var newIndex = m_Random.Next(0, m_Cubes.Length);
            var temp = m_Cubes[newIndex];
            m_Cubes[newIndex] = m_Cubes[i];
            m_Cubes[i] = temp;
        }
    }

    void Awake()
    {
        m_Random = new Random();
        m_CubeCurrentCounts = new int [CubeTypeCount];
        m_CubeStartingCounts = new int [CubeTypeCount];
        m_CubeStartingCounts[(int)CupCardCubeColour.Grey] = 5;
        m_CubeStartingCounts[(int)CupCardCubeColour.Blue] = 7;
        m_CubeStartingCounts[(int)CupCardCubeColour.Green] = 9;
        m_CubeStartingCounts[(int)CupCardCubeColour.Yellow] = 11;
        m_CubeStartingCounts[(int)CupCardCubeColour.Red] = 13;

        m_CubeWinningCounts = new int [CubeTypeCount];
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
            m_CubeWinningCounts[cubeType] = (m_CubeStartingCounts[cubeType] + 1) / 2;

        var numCubesTotal = 0;
        foreach (var count in m_CubeStartingCounts)
            numCubesTotal += count;
        CubesTotalCount = numCubesTotal;

        m_CardCubeColours = new Color [CubeTypeCount];
        m_CardCubeColours[0] = Color.grey;
        m_CardCubeColours[1] = Color.blue;
        m_CardCubeColours[2] = Color.green;
        m_CardCubeColours[3] = Color.yellow;
        m_CardCubeColours[4] = Color.red;

        m_Cubes = new CupCardCubeColour [CubesTotalCount];
        m_Races = new Race [RacesCount];
        m_State = GameState.Initialising;

        m_FullDeck = new Card [CubesTotalCount];
        m_DrawDeck = new Queue<Card>();
        m_DiscardDeck = new Queue<Card>();
        m_CurrentPlayer = Player.Unknown;

        m_ChosenHandCards = new int [4];

        m_PlayerCubeCounts = new int [PlayerCount, CubeTypeCount];
        m_PlayerWildcardCubeCounts = new int [PlayerCount, CubeTypeCount];
        m_PlayerHands = new Card [PlayerCount, HandSize];
        m_PlayerCups = new bool [PlayerCount, CubeTypeCount];

        m_CupOwner = new Player [CubeTypeCount];
        m_CupPayment = new int [CubeTypeCount, CubeTypeCount];
    }

    bool Validate()
    {
        var allOk = true;
        allOk &= ValidateCards();
        allOk &= ValidateCups();
        allOk &= ValidateCubes();
        return allOk;
    }

    bool CheckCard(int maxCardValue, Card card, int[,] cardCounts)
    {
        var cubeType = (int)card.Colour;
        var v = card.Value;
        if (v < 0 || v > maxCardValue)
        {
            Debug.LogError("Invalid card value:" + v);
            return false;
        }

        if (cardCounts[cubeType, v] != 1)
            return false;
        cardCounts[cubeType, v] -= 1;
        return true;
    }

    bool ValidateCards()
    {
        var maxCardValue = 13 + 1; // 1-based index 1 -> 13
        var cardCounts = new int [CubeTypeCount, maxCardValue];
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
        for (var v = 0; v < maxCardValue; ++v)
            cardCounts[cubeType, v] = 0;

        // Set the starting cards count to 1
        foreach (var card in m_FullDeck)
            cardCounts[(int)card.Colour, card.Value] = 1;

        // Verify every card is in draw deck or discard deck or player hand or played on a race
        // Draw deck
        foreach (var card in m_DrawDeck)
            if (!CheckCard(maxCardValue, card, cardCounts))
            {
                Debug.LogError("Draw deck card already used:" + card.Colour + " " + card.Value);
                return false;
            }

        // Discard deck
        foreach (var card in m_DiscardDeck)
            if (!CheckCard(maxCardValue, card, cardCounts))
            {
                Debug.LogError("Discard deck card already used:" + card.Colour + " " + card.Value);
                return false;
            }

        // Player hands
        for (var playerIndex = 0; playerIndex < PlayerCount; ++playerIndex)
        for (var c = 0; c < HandSize; ++c)
        {
            var card = m_PlayerHands[playerIndex, c];
            if (!CheckCard(maxCardValue, card, cardCounts))
            {
                Debug.LogError("Player: " + (Player)playerIndex + " hand card already used:" +
                    card.Colour + " " + card.Value);
                return false;
            }
        }

        // Cards played on a race
        foreach (var race in m_Races)
            for (var side = 0; side < PlayerCount; ++side)
            for (var c = 0; c < race.NumberOfCubes; ++c)
            {
                var card = race.GetPlayedCard((Player)side, c);
                if (card != null)
                    if (!CheckCard(maxCardValue, card, cardCounts))
                    {
                        Debug.LogError("Race: " + race.name +
                            " Side: " + (Player)side + " card already used:" +
                            card.Colour + " " + card.Value);
                        return false;
                    }
            }

        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
        for (var v = 0; v < maxCardValue; ++v)
            if (cardCounts[cubeType, v] != 0)
            {
                Debug.LogError("Card Validation failed invalid card count: " +
                    cardCounts[cubeType, v] + " " +
                    (CupCardCubeColour)cubeType + " " + v);
                return false;
            }

        return true;
    }

    bool ValidateCups()
    {
        // Verify each cup has only been won by one player or by no players
        var allOk = true;
        for (var cupType = 0; cupType < CubeTypeCount; ++cupType)
        {
            var cupWinCount = 0;
            for (var player = 0; player < PlayerCount; ++player)
                cupWinCount += m_PlayerCups[player, cupType] ? 1 : 0;
            if (cupWinCount > 1)
            {
                Debug.LogError("Cup " + (CupCardCubeColour)cupType + " Invalid cupWinCount " + cupWinCount);
                allOk = false;
            }
        }

        // validate cup payments cubes used to win cups : it might not be 5 greens
        for (var cupType = 0; cupType < CubeTypeCount; ++cupType)
        {
            var cup = (CupCardCubeColour)cupType;
            if (IsCupWon(cup))
            {
                var cubePaidCount = 0;
                for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
                {
                    var cubeCount = m_CupPayment[cupType, cubeType];
                    if (cubeType != cupType)
                    {
                        if (cubeCount % 3 != 0)
                        {
                            Debug.LogError("Wildcard cube count not a multiple of 3 : " + cubeCount +
                                " Cup: " + cup + " Cube: " + (CupCardCubeColour)cubeType);
                            allOk = false;
                        }

                        cubeCount /= 3;
                    }

                    cubePaidCount += cubeCount;
                }

                if (cubePaidCount != m_CubeWinningCounts[cupType])
                {
                    Debug.LogError("Cup paid amount is wrong " +
                        "Cup: " + cup + " " + cubePaidCount);
                    allOk = false;
                }
            }
        }

        return allOk;
    }

    bool UpdateCubeCounts(CupCardCubeColour cube, int[] cubeCounts)
    {
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
                Debug.LogError("Unknown cube type " + cube);
                return false;
        }

        return true;
    }

    bool ValidateCubes()
    {
        // Verify count of cubes left to play is correct
        var cubeCounts = new int [CubeTypeCount];
        var allOk = true;
        for (var i = 0; i < m_CubesRemainingCount; ++i)
        {
            var cube = m_Cubes[i];
            allOk &= UpdateCubeCounts(cube, cubeCounts);
        }

        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
            if (m_CubeCurrentCounts[cubeType] != cubeCounts[cubeType])
            {
                allOk = false;
                Debug.LogError("Current Cube count is incorrect " +
                    (CupCardCubeColour)cubeType + " " +
                    cubeCounts[cubeType] +
                    " Expected: " + m_CubeCurrentCounts[cubeType]);
            }

        // Add cubes from races
        foreach (var race in m_Races)
            for (var c = 0; c < race.NumberOfCubes; ++c)
            {
                var cube = race.GetCube(c);
                if (cube != CupCardCubeColour.Invalid)
                {
                    allOk &= UpdateCubeCounts(cube, cubeCounts);
                }
                else if (race.State != RaceState.Finished && m_TurnState != TurnState.FinishingRace && m_TurnState != TurnState.FinishingGame)
                {
                    Debug.LogError("Invalid cube from race:" + race.name + " but race is not Finished:" + race.State);
                    allOk = false;
                }
            }

        // Add cubes from players
        for (var playerIndex = 0; playerIndex < PlayerCount; ++playerIndex)
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
        {
            var cubeCount = m_PlayerCubeCounts[playerIndex, cubeType];
            cubeCounts[cubeType] += cubeCount;
        }

        // Add cubes from player wildcards
        for (var playerIndex = 0; playerIndex < PlayerCount; ++playerIndex)
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
        {
            var wildcardCount = m_PlayerWildcardCubeCounts[playerIndex, cubeType];
            cubeCounts[cubeType] += wildcardCount * 3;
        }

        // Add cubes from owned cups : a cup can be bought with different (wildcards)
        for (var cupType = 0; cupType < CubeTypeCount; ++cupType)
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
        {
            var cubeCount = m_CupPayment[cupType, cubeType];
            cubeCounts[cubeType] += cubeCount;
        }

        // cubes from bag + races + players (including wildcards) + owned cups 
        // must match the starting cube counts
        for (var cubeType = 0; cubeType < CubeTypeCount; ++cubeType)
            if (m_CubeStartingCounts[cubeType] != cubeCounts[cubeType])
            {
                Debug.LogError("Total Cube count is incorrect " +
                    (CupCardCubeColour)cubeType + " " + cubeCounts[cubeType] +
                    " Expected: " + m_CubeStartingCounts[cubeType]);
                allOk = false;
            }

        return allOk;
    }

    void SetPlayerGenericButtonText(string text)
    {
        var playerStart = (int)m_CurrentPlayer;
        var playerEnd = playerStart + 1;
        if (m_CurrentPlayer == Player.Unknown)
        {
            playerStart = (int)Player.Left;
            playerEnd = PlayerCount;
        }

        for (var p = playerStart; p < playerEnd; ++p) m_GameUI.SetPlayerGenericButtonText(p, text);
    }

    void DeSelectCard(Player player, int cardIndex)
    {
        if (cardIndex < 0)
        {
            Debug.LogError("Invalid cardIndex " + cardIndex);
            return;
        }

        m_GameUI.SetPlayerCardHighlighted((int)player, cardIndex, false);
    }

    void SelectCard(Player player, int cardIndex)
    {
        if (cardIndex < 0)
        {
            Debug.LogError("Invalid cardIndex " + cardIndex);
            return;
        }

        m_GameUI.SetPlayerCardHighlighted((int)player, cardIndex, true);
    }

    void Update()
    {
        switch (m_State)
        {
            case GameState.Initialising:
                Initialise();
                m_State = GameState.NewGame;
                break;
            case GameState.NewGame:
                NewGame();
                m_State = GameState.InGame;
                break;
            case GameState.InGame:
                InGame();
                break;
            case GameState.EndGame:
                EndGame();
                break;
        }

        TakeRobotTurn();
    }

    void RobotPickCardFromHand()
    {
        // Dumb robot play the first card it can
        var playerIndex = (int)m_CurrentPlayer;
        for (var c = 0; c < HandSize; ++c)
        {
            var card = m_PlayerHands[playerIndex, c];
            foreach (var race in m_Races)
                if (race.CanPlayCard(card))
                {
                    CurrentPlayerSelectCardFromHand(c);
                    Debug.Log("Robot Chooses " + card);
                    return;
                }
        }
    }

    void RobotPlayCardOnRace()
    {
        // Dumb robot play the chosen card on the first race and side it can
        var playerIndex = (int)m_CurrentPlayer;
        var card = m_PlayerHands[playerIndex, m_ChosenHandCards[0]];
        foreach (var race in m_Races)
            if (race.CanPlayCard(card))
                for (var sideIndex = 0; sideIndex < PlayerCount; ++sideIndex)
                {
                    var side = (Player)sideIndex;
                    if (PlaySelectedCardOnARace(side, race))
                    {
                        Debug.Log("Robot Plays Card : " + card.Colour + " " + card.Value + " on Race:" + race.name + " Side:" + side);
                        return;
                    }
                }
    }

    void RobotPickCardsToDiscard()
    {
        // Dumb robot choose 4 cards randomly
        for (var c = 0; c < 4; ++c)
        {
            var cardNumber = m_Random.Next(0, 8);
            CurrentPlayerSelectCardFromHand(cardNumber);
        }
    }

    void RobotClaimCups()
    {
        // Dumb robot claim every cup it can
        for (var c = 0; c < CubeTypeCount; ++c)
        {
            var cupType = (CupCardCubeColour)c;
            string reason;
            if (CanClaimCup(cupType, out reason))
            {
                Debug.Log("Robot Claims Cup : " + cupType);
                ClaimCup(cupType);
            }
        }
    }

    void RobotPressGenericButton()
    {
        if (m_TurnState == TurnState.FinishingGame)
            return;
        if (m_GameUI.IsPlayerGenericButtonActive((int)m_CurrentPlayer))
            PlayerGenericButtonClicked();
    }

    void TakeRobotTurn()
    {
        if (!RobotActive)
            return;
        if (m_State != GameState.InGame)
            return;
        switch (m_TurnState)
        {
            case TurnState.StartingPlayerTurn:
                RobotPressGenericButton();
                break;
            case TurnState.PickCardFromHand:
                RobotPickCardFromHand();
                break;
            case TurnState.PickCardsFromHandToDiscard:
                RobotPickCardsToDiscard();
                RobotPressGenericButton();
                break;
            case TurnState.PlayCardOnRace:
                RobotPlayCardOnRace();
                break;
            case TurnState.FinishingRace:
                RobotPressGenericButton();
                break;
            case TurnState.FinishingGame:
                break;
            case TurnState.EndingPlayerTurn:
                RobotClaimCups();
                RobotPressGenericButton();
                break;
        }
    }

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
}
