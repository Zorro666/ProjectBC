using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLogic : MonoBehaviour
{
    public Color RaceLowestColour;
    public Color RaceHighestColour;
    public Color RaceFinishedColour;
    public Text StatusText;
    public GameObject GenericBottomButton;

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
        DiscardCardsFromHand,
        PlayCardOnRace,
        FinishingRace,
        FinishingGame
    }

    System.Random m_random;
    BC.CardCubeColour[] m_cubes;
    Color[] m_cardCubeColours;
    int[] m_cubeCounts;
    int[] m_cubeWinningCounts;
    GameState m_state;
    TurnState m_turnState;
    Race[] m_races;
    int m_frame;
    int m_cubesRemainingCount;
    int m_cubesTotalCount;
    Card[] m_fullDeck;
    Queue<Card> m_drawDeck;
    Queue<Card> m_discardDeck;
    BC.Player m_roundWinner;
    BC.Player m_currentPlayer;
    int m_chosenHandCardIndex;
    Race m_finishedRace;

    //TODO: make a Player class and store this data per Player
    Text[,] m_playerCubeCountsTexts;
    int[,] m_playerCubeCounts;
    GameObject[] m_playerHandGOs;
    Image[,] m_playerCardBackgrounds;
    Text[,] m_playerCardValues;
    Card[,] m_playerHands;
    Image[,] m_playerCupImages;
    bool[,] m_playerCups;

    static public int CubeTypeCount
    {
        get { return (int)BC.CardCubeColour.Count; }
    }

    static public int PlayerCount
    {
        get { return (int)BC.Player.Count; }
    }

    static public int RacesCount
    {
        get { return 4; }
    }

    static public int HandSize
    {
        get { return 8; }
    }

    static public GameLogic GetInstance()
    {
        var logic = GameObject.Find("/Logic");
        if (logic == null)
        {
            Debug.LogError("Can't find '/Logic' GameObject");
            return null;
        }
        var gamelogic = logic.GetComponent<GameLogic>();
        if (gamelogic == null)
            Debug.LogError("Can't find 'GameLogic' Component");
        return gamelogic;
    }

    public int CubesRemainingCount
    {
        get { return m_cubesRemainingCount; }
    }

    public void DiscardCard(Card card)
    {
        m_discardDeck.Enqueue(card);
    }

    public BC.Player CurrentPlayer
    {
        get { return m_currentPlayer; }
    }

    public Color CardCubeColour(BC.CardCubeColour cardCubeType)
    {
        return m_cardCubeColours[(int)cardCubeType];
    }

    public void AddCubeToPlayer(BC.Player player, BC.CardCubeColour cubeType)
    {
        ++m_playerCubeCounts[(int)player, (int)cubeType];
    }

    public void FinishRace(BC.Player winner, Race race)
    {
        UpdateCubeCounts((BC.Player)winner);
        m_turnState = TurnState.FinishingRace;
        m_finishedRace = race;
        m_frame = 0;
        UpdateStatus();
    }

    public void GenericBottomButtonClicked()
    {
        switch (m_turnState)
        {
            case TurnState.StartingPlayerTurn:
                ExitStartingPlayerTurn();
                break;
            case TurnState.DiscardCardsFromHand:
                ExitDiscardCardsFromHand();
                break;
            case TurnState.FinishingRace:
                ExitFinishingRace();
                break;
            case TurnState.FinishingGame:
                ExitFinishingGame();
                break;
            default:
                Debug.Log("Unknown m_turnState: " + m_turnState);
                break;
        }
    }

    public void PlayerHandCardClicked(GameObject source)
    {
        if (m_state != GameState.InGame)
            return;
        if ((m_turnState != TurnState.PickCardFromHand) && (m_turnState != TurnState.PlayCardOnRace))
            return;
        var playerHandSource = source.transform.parent.parent.name;
        var currentPlayerHand = m_currentPlayer + "Player";
        if (playerHandSource != currentPlayerHand)
            return;
        Debug.Log("PlayerHandCardClicked source:" + source.name + " greatgrandparent:" + source.transform.parent.parent.name);
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
        int playerIndex = (int)m_currentPlayer;
        m_chosenHandCardIndex = cardNumber - 1;
        var card = m_playerHands[playerIndex, m_chosenHandCardIndex];
        Debug.Log("Selected card Colour: " + card.Colour + " Value: " + card.Value);
        m_turnState = TurnState.PlayCardOnRace;
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
        int raceNumber = int.Parse(raceName.Substring(4));
        //Debug.Log("PlayCard " + source.name + " " + raceName + " " + raceNumber);
        if ((raceNumber < 1) || (raceNumber > 4))
        {
            Debug.LogError("PlayCard invalid raceNumber " + raceNumber);
            return;
        }
        var sideString = source.name.Substring(8);
        BC.Player side = BC.Player.Unknown;
        if (sideString == "Left")
            side = BC.Player.Left;
        else if (sideString == "Right")
            side = BC.Player.Right;

        //int cardIndex = m_random.Next(GameLogic.HandSize);
        int playerIndex = (int)m_currentPlayer;
        var cardIndex = m_chosenHandCardIndex;
        if ((cardIndex < 0) || (cardIndex >= HandSize))
        {
            Debug.LogError("PlayCard invalid cardIndex " + cardIndex);
            return;
        }
        var card = m_playerHands[playerIndex, cardIndex];
        bool validCard = m_races[raceNumber - 1].PlayCard(side, card);
        if (!validCard)
        {
            StatusText.text = "Wrong Race. Please choose a different Race";
            return;
        }

        HideHands();
        m_chosenHandCardIndex = -1;
        if (!validCard)
            DiscardCard(card);
        PlayerDrawNewCard(m_currentPlayer, cardIndex);
        if (m_finishedRace == null)
        {
            EndPlayerTurn();
            StartPlayerTurn();
        }
    }

    int CubesTotalCount
    {
        get { return m_cubesTotalCount; }
        set { m_cubesTotalCount = value; }
    }

    void PlayerDrawNewCard(BC.Player player, int cardIndex)
    {
        if (m_drawDeck.Count == 0)
            StartDrawDeckFromDiscardDeck();
        ReplacePlayerCardInHand(player, cardIndex);
    }

    void ExitFinishingRace()
    {
        SetActiveGenericBottomButton(false);
        for (BC.Player player = BC.Player.First; player < BC.Player.Count; ++player)
        {
            if (HasPlayerWon(player))
            {
                m_roundWinner = player;
                m_turnState = TurnState.FinishingGame;
                UpdateStatus();
                return;
            }
        }

        m_frame = 0;
        m_state = GameState.InGame;
        m_currentPlayer = m_finishedRace.Winner;
        m_finishedRace.StartRace();
        m_finishedRace = null;
        EndPlayerTurn();
        StartPlayerTurn();
    }

    void ExitFinishingGame()
    {
        m_state = GameState.Initialising;
    }

    void ExitStartingPlayerTurn()
    {
        SetActiveGenericBottomButton(false);
        ShowHand(m_currentPlayer);
        if (PlayerCanPlayCardOnARace())
        {
            m_turnState = TurnState.PickCardFromHand;
        }
        else
        {
            m_turnState = TurnState.DiscardCardsFromHand;
            SetGenericBottomButtonText("Discard 4 Cards");
            SetActiveGenericBottomButton(true);
        }
        UpdateStatus();
    }

    void ExitDiscardCardsFromHand()
    {
        // Randomly choose 4 different cards to discard
        const int numCardsToDiscard = 4;
        int[] cardsToDiscard = new int[numCardsToDiscard];
        int[] cardIndexes = new int[HandSize];

        for (int c = 0; c < HandSize; ++c)
            cardIndexes[c] = c;

        int numCardsToChooseFrom = HandSize;
        for (int i = 0; i < numCardsToDiscard; ++i)
        {
            var discardIndex = m_random.Next(numCardsToChooseFrom);
            var cardIndex = 0;
            var cardToDiscardIndex = -1;
            for (int c = 0; c < HandSize; ++c)
            {
                if (cardIndexes[c] >= 0)
                {
                    if (cardIndex == discardIndex)
                    {
                        cardToDiscardIndex = c;
                        cardIndexes[c] = -1;
                        break;
                    }
                    ++cardIndex;
                }
            }
            if (cardToDiscardIndex >= 0)
            {
                cardsToDiscard[i] = cardToDiscardIndex;
            }
            else
            {
                cardsToDiscard[i] = 0;
                Debug.LogError("Invalid cardToDiscardIndex");
            }
            --numCardsToChooseFrom;
        }

        //TODO: move this logic into the state machine to support UI replacing the random discard card selection
        int playerIndex = (int)m_currentPlayer;
        for (int i = 0; i < numCardsToDiscard; ++i)
        {
            var cardIndex = cardsToDiscard[i];
            Debug.Log("Discard Card[" + i + "] " + cardIndex);
            var card = m_playerHands[playerIndex, cardIndex];
            DiscardCard(card);
            PlayerDrawNewCard(m_currentPlayer, cardIndex);
        }
        //TODO: check if player can still play a card and let them play it
        EndPlayerTurn();
        StartPlayerTurn();
    }

    bool PlayerCanPlayCardOnARace()
    {
        int playerIndex = (int)m_currentPlayer;
        bool canPlayCard = false;
        for (int cardIndex = 0; cardIndex < HandSize; ++cardIndex)
        {
            var card = m_playerHands[playerIndex, cardIndex];
            foreach (var race in m_races)
                canPlayCard |= race.CanPlayCard(card);
        }
        return canPlayCard;
    }

    public BC.CardCubeColour NextCube()
    {
        if (m_cubesRemainingCount > 0)
            --m_cubesRemainingCount;
        var cubeType = m_cubes[m_cubesRemainingCount];
        int cubeTypeIndex = (int)cubeType;
        --m_cubeCounts[cubeTypeIndex];
        if (m_cubeCounts[cubeTypeIndex] < 0)
            Debug.LogError("Negative cubeCounts " + cubeType);
        return m_cubes[m_cubesRemainingCount];
    }

    void Initialise()
    {
        CreateFullDeck();
        var cubeIndex = 0;
        for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
        {
            var count = m_cubeCounts[cubeType];
            for (int i = 0; i < count; ++i)
                m_cubes[cubeIndex++] = (BC.CardCubeColour)cubeType;
        }
        if (CubesTotalCount != cubeIndex)
            Debug.LogError("cubes count not matching " + CubesTotalCount + " != " + cubeIndex);

        for (int i = 0; i < RacesCount; ++i)
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
            race.Initialise();

        var gameBoardUIRootName = "/GameBoard/UI/";
        for (int player = 0; player < GameLogic.PlayerCount; ++player)
        {
            var playerUIRootName = gameBoardUIRootName + (BC.Player)player + "Player/";
            var playerCubesBackgroundRootName = playerUIRootName + "CubesBackground/";
            var playerCupsRootName = playerUIRootName + "CupsBackground/";
            for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            {
                var cubeCountText = playerCubesBackgroundRootName + (BC.CardCubeColour)cubeType;
                var cubeCountGO = GameObject.Find(cubeCountText);
                if (cubeCountGO == null)
                    Debug.LogError("Can't find " + (BC.CardCubeColour)cubeType + " cube counts GameObject " + cubeCountText);
                m_playerCubeCountsTexts[player, cubeType] = cubeCountGO.GetComponent<Text>();
                if (m_playerCubeCountsTexts[player, cubeType] == null)
                    Debug.LogError("Can't find " + (BC.CardCubeColour)cubeType + " cube counts UI Text Component");

                var cupImageName = playerCupsRootName + (BC.CardCubeColour)cubeType;
                var cupImageGO = GameObject.Find(cupImageName);
                if (cupImageGO == null)
                    Debug.LogError("Can't find " + (BC.CardCubeColour)cubeType + " cup Image GameObject " + cupImageName);
                m_playerCupImages[player, cubeType] = cupImageGO.GetComponent<Image>();
                if (m_playerCupImages[player, cubeType] == null)
                    Debug.LogError("Can't find " + (BC.CardCubeColour)cubeType + " cup UI Image Component");

                m_playerCups[player, cubeType] = false;
            }
            var playerHandRootName = playerUIRootName + "Hand/";
            m_playerHandGOs[player] = GameObject.Find(playerHandRootName);
            if (m_playerHandGOs[player] == null)
                Debug.LogError("Can't find Player Hand UI GameObject " + (BC.Player)player + " " + playerHandRootName);
            for (int card = 0; card < HandSize; ++card)
            {
                int cardIndex = card + 1;
                var playerCardRootName = playerHandRootName + "Card" + cardIndex.ToString() + "/";
                var playerCardBackgroundName = playerCardRootName + "Background";
                var playerCardBackgroundGO = GameObject.Find(playerCardBackgroundName);
                if (playerCardBackgroundGO == null)
                    Debug.LogError("Can't find PlayerCardBackgroundGO " + playerCardBackgroundName);
                m_playerCardBackgrounds[player, card] = playerCardBackgroundGO.GetComponent<Image>();
                var playerCardValueName = playerCardBackgroundName + "/Value";
                var playerCardValueGO = GameObject.Find(playerCardValueName);
                if (playerCardValueGO == null)
                    Debug.LogError("Can't find PlayerCardValueGO " + playerCardValueName);
                m_playerCardValues[player, card] = playerCardValueGO.GetComponent<Text>();
                if (m_playerCardValues[player, card] == null)
                    Debug.LogError("Can't find Player " + (BC.Player)player + " Card[" + cardIndex + " Value Text " + playerCardValueName);
            }
        }
    }

    void NewGame()
    {
        SetActiveGenericBottomButton(false);
        CreateDrawDeck();
        ShuffleDrawDeck();
        DealHands();
        m_cubesRemainingCount = CubesTotalCount;
        ShuffleCubes();
        foreach (var race in m_races)
            race.NewGame();
        m_frame = 0;
        m_finishedRace = null;
        m_chosenHandCardIndex = -1;
        m_currentPlayer = (BC.Player)m_random.Next(GameLogic.PlayerCount);
        m_roundWinner = BC.Player.Unknown;

        HideHands();
        for (int player = 0; player < GameLogic.PlayerCount; ++player)
        {
            for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            {
                m_playerCubeCountsTexts[player, cubeType].color = CardCubeColour((BC.CardCubeColour)cubeType);
                m_playerCubeCounts[player, cubeType] = 0;
                m_playerCupImages[player, cubeType].enabled = false;
            }
            UpdateCubeCounts((BC.Player)player);
        }
        StartPlayerTurn();
    }

    void AwardCupToPlayer(BC.Player player, BC.CardCubeColour cupColour)
    {
        HideCup(cupColour);
        ShowCup(player, cupColour);
        m_playerCups[(int)player, (int)cupColour] = true;
    }

    void HideCup(BC.CardCubeColour cupColour)
    {
        for (int player = 0; player < GameLogic.PlayerCount; ++player)
            m_playerCupImages[player, (int)cupColour].enabled = false;
    }

    void ShowCup(BC.Player player, BC.CardCubeColour cupColour)
    {
        m_playerCupImages[(int)player, (int)cupColour].enabled = true;
    }

    bool HasPlayerWon(BC.Player player)
    {
        int cupsWonCount = 3;
        for (int cupType = 0; cupType < GameLogic.CubeTypeCount; ++cupType)
            cupsWonCount += (m_playerCups[(int)player, (int)cupType] == true) ? 1 : 0;

        return (cupsWonCount >= 3);
    }

    void UpdateCubeCounts(BC.Player player)
    {
        int playerIndex = (int)player;
        for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
        {
            int cubeValue = m_playerCubeCounts[playerIndex, cubeType];
            m_playerCubeCountsTexts[playerIndex, cubeType].text = cubeValue.ToString();
            var cubeCountToWin = m_cubeWinningCounts[cubeType];
            if (cubeValue >= cubeCountToWin)
                AwardCupToPlayer(player, (BC.CardCubeColour)cubeType);
        }
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
        SetGenericBottomButtonText("Continue");
        SetActiveGenericBottomButton(true);
    }

    void FinishingRace()
    {
        ++m_frame;
        SetGenericBottomButtonText("Continue");
        SetActiveGenericBottomButton(true);
    }

    void FinishingGame()
    {
        ++m_frame;
        SetGenericBottomButtonText("Continue");
        SetActiveGenericBottomButton(true);
    }

    void StartPlayerTurn()
    {
        m_chosenHandCardIndex = -1;
        m_turnState = TurnState.StartingPlayerTurn;
        SetGenericBottomButtonText("Continue");
        SetActiveGenericBottomButton(true);
        UpdateStatus();
    }

    void EndPlayerTurn()
    {
        HideHands();
        m_currentPlayer++;
        if (m_currentPlayer == BC.Player.Count)
            m_currentPlayer = BC.Player.First;
    }

    void UpdateStatus()
    {
        switch (m_turnState)
        {
            case TurnState.StartingPlayerTurn:
                StatusText.text = m_currentPlayer + " Player: Press Continue to Start Turn";
                break;
            case TurnState.PickCardFromHand:
                StatusText.text = "Choose a Card to Play";
                break;
            case TurnState.DiscardCardsFromHand:
                StatusText.text = "No card can be Played. Press Discard to Contiue";
                break;
            case TurnState.PlayCardOnRace:
                StatusText.text = "Choose a Race to Play on";
                break;
            case TurnState.FinishingRace:
                StatusText.text = m_finishedRace.Winner + " Player Won the Race";
                break;
            case TurnState.FinishingGame:
                StatusText.text = m_roundWinner + " Player Won the Game";
                break;
        }
    }

    void CreateFullDeck()
    {
        int[][] allCardValues = new int[(int)BC.CardCubeColour.Count][];
        allCardValues[(int)BC.CardCubeColour.Grey] = new int[] { 1, 4, 7, 10, 13 };
        allCardValues[(int)BC.CardCubeColour.Blue] = new int[] { 1, 3, 5, 7, 9, 11, 13 };
        allCardValues[(int)BC.CardCubeColour.Green] = new int[] { 1, 2, 4, 6, 7, 8, 10, 12, 13 };
        allCardValues[(int)BC.CardCubeColour.Yellow] = new int[] { 1, 2, 3, 5, 6, 7, 8, 9, 11, 12, 13 };
        allCardValues[(int)BC.CardCubeColour.Red] = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        int cardIndex = 0;
        for (int colour = 0; colour < (int)BC.CardCubeColour.Count; ++colour)
        {
            int[] cardValues = allCardValues[colour];
            if (m_cubeCounts[colour] != cardValues.Length)
                Debug.LogError((BC.CardCubeColour)colour + " CardValues length does not match cubeCounts");
            foreach (int v in cardValues)
            {
                m_fullDeck[cardIndex] = new Card((BC.CardCubeColour)colour, v);
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
        for (int i = 0; i < drawDeck.Length; ++i)
        {
            int newIndex = m_random.Next(0, drawDeck.Length);
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

    void ReplacePlayerCardInHand(BC.Player player, int cardIndex)
    {
        Card card = DrawCard();
        if (card == null)
            Debug.LogError("null Card from Draw Deck");
        m_playerHands[(int)player, cardIndex] = card;
        UpdatePlayerCardUI(player, cardIndex);
    }

    void DealHands()
    {
        for (int player = 0; player < GameLogic.PlayerCount; ++player)
        {
            for (int i = 0; i < HandSize; ++i)
            {
                ReplacePlayerCardInHand((BC.Player)player, i);
            }
        }
    }

    void ShowHand(BC.Player player)
    {
        m_playerHandGOs[(int)player].SetActive(true);
    }

    void HideHands()
    {
        for (int player = 0; player < GameLogic.PlayerCount; ++player)
            HideHand((BC.Player)player);
    }

    void HideHand(BC.Player player)
    {
        m_playerHandGOs[(int)player].SetActive(false);
    }

    void UpdatePlayerCardUI(BC.Player player, int cardIndex)
    {
        int playerIndex = (int)player;
        Card card = m_playerHands[playerIndex, cardIndex];
        string text = card.Value.ToString();
        int cardColour = (int)card.Colour;
        var colour = CardCubeColour(card.Colour);

        m_playerCardValues[playerIndex, cardIndex].text = text;
        m_playerCardBackgrounds[playerIndex, cardIndex].color = colour;
        var textColour = (card.Colour == BC.CardCubeColour.Yellow) ? Color.black : Color.white;
        m_playerCardValues[playerIndex, cardIndex].color = textColour;
    }

    void ShuffleCubes()
    {
        for (int i = 0; i < m_cubes.Length; ++i)
        {
            int newIndex = m_random.Next(0, m_cubes.Length);
            var temp = m_cubes[newIndex];
            m_cubes[newIndex] = m_cubes[i];
            m_cubes[i] = temp;
        }
    }

    void Awake()
    {
        m_random = new System.Random();
        m_cubeCounts = new int[GameLogic.CubeTypeCount];
        m_cubeCounts[(int)BC.CardCubeColour.Grey] = 5;
        m_cubeCounts[(int)BC.CardCubeColour.Blue] = 7;
        m_cubeCounts[(int)BC.CardCubeColour.Green] = 9;
        m_cubeCounts[(int)BC.CardCubeColour.Yellow] = 11;
        m_cubeCounts[(int)BC.CardCubeColour.Red] = 13;
        m_cubeWinningCounts = new int[GameLogic.CubeTypeCount];
        m_cubeWinningCounts[(int)BC.CardCubeColour.Grey] = 3;
        m_cubeWinningCounts[(int)BC.CardCubeColour.Blue] = 4;
        m_cubeWinningCounts[(int)BC.CardCubeColour.Green] = 5;
        m_cubeWinningCounts[(int)BC.CardCubeColour.Yellow] = 6;
        m_cubeWinningCounts[(int)BC.CardCubeColour.Red] = 7;
        var numCubesTotal = 0;
        foreach (int count in m_cubeCounts)
            numCubesTotal += count;
        CubesTotalCount = numCubesTotal;

        m_cardCubeColours = new Color[GameLogic.CubeTypeCount];
        m_cardCubeColours[0] = Color.grey;
        m_cardCubeColours[1] = Color.blue;
        m_cardCubeColours[2] = Color.green;
        m_cardCubeColours[3] = Color.yellow;
        m_cardCubeColours[4] = Color.red;

        m_cubes = new BC.CardCubeColour[CubesTotalCount];
        m_races = new Race[GameLogic.RacesCount];
        m_state = GameState.Initialising;

        m_fullDeck = new Card[CubesTotalCount];
        m_drawDeck = new Queue<Card>();
        m_discardDeck = new Queue<Card>();
        m_currentPlayer = BC.Player.Unknown;

        m_playerCubeCountsTexts = new Text[GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_playerCubeCounts = new int[GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_playerHandGOs = new GameObject[GameLogic.PlayerCount];
        m_playerCardBackgrounds = new Image[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCardValues = new Text[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerHands = new Card[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCupImages = new Image[GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_playerCups = new bool[GameLogic.PlayerCount, GameLogic.CubeTypeCount];

        SetActiveGenericBottomButton(false);
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
        for (int cupType = 0; cupType < GameLogic.CubeTypeCount; ++cupType)
        {
            int cupWinCount = 0;
            for (int player = 0; player < GameLogic.PlayerCount; ++player)
                cupWinCount += (m_playerCups[player, cupType] == true) ? 1 : 0;
            if (cupWinCount > 1)
            {
                Debug.LogError("Cup " + (BC.CardCubeColour)cupType + " Invalid cupWinCount " + cupWinCount);
                allOk = false;
            }
        }
        return allOk;
    }

    bool ValidateCubes()
    {
        // Verify count of cubes left to play is correct
        int[] cubeCounts = new int[(int)BC.CardCubeColour.Count];
        bool allOk = true;
        for (int i = 0; i < m_cubesRemainingCount; ++i)
        {
            var cube = m_cubes[i];
            switch (cube)
            {
                case BC.CardCubeColour.Grey:
                    cubeCounts[(int)cube]++;
                    break;
                case BC.CardCubeColour.Blue:
                    cubeCounts[(int)cube]++;
                    break;
                case BC.CardCubeColour.Green:
                    cubeCounts[(int)cube]++;
                    break;
                case BC.CardCubeColour.Yellow:
                    cubeCounts[(int)cube]++;
                    break;
                case BC.CardCubeColour.Red:
                    cubeCounts[(int)cube]++;
                    break;
                default:
                    allOk = false;
                    Debug.LogError("Unknown cube value " + cube);
                    break;
            }
        }
        for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
        {
            if (m_cubeCounts[cubeType] != cubeCounts[cubeType])
            {
                allOk = false;
                Debug.LogError("Cube count is incorrect " + (BC.CardCubeColour)cubeType + " " + cubeCounts[cubeType]);
            }
        }
        //TODO: count cubes in bag + races + players must match the totals
        return allOk;
    }

    void SetGenericBottomButtonText(string text)
    {
        GenericBottomButton.GetComponentInChildren<Text>().text = text;
    }

    void SetActiveGenericBottomButton(bool enable)
    {
        GenericBottomButton.SetActive(enable);
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
