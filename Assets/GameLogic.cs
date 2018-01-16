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
        InGame
    }

    enum TurnState
    {
        StartingPlayerTurn,
        PickCardFromHand,
        PlayCardOnRace,
        FinishingRace
    }

    System.Random m_random;
    BC.CardCubeColour[] m_cubes;
    Color[] m_cardCubeColours;
    int[] m_cubeCounts;
    GameState m_state;
    TurnState m_turnState;
    Race[] m_races;
    int m_frame;
    int m_cubesRemainingCount;
    int m_cubesTotalCount;
    Card[] m_fullDeck;
    Queue<Card> m_drawDeck;
    Queue<Card> m_discardDeck;
    BC.Player m_currentPlayer;
    int m_chosenHandCardIndex;
    Race m_finishedRace;

    //TODO: make a Player class and store this data per Player
    Text[,] m_playerCubeCountsTexts;
    int[,] m_playerCubeCounts;
    GameObject[] m_playerHandGO;
    Image[,] m_playerCardBackground;
    Text[,] m_playerCardValue;
    Card[,] m_playerHands;

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
            {
                ExitStartingPlayerTurn();
                break;
            }
            case TurnState.FinishingRace:
            {
                ExitFinishingRace();
                break;
            }
            default:
            {
                Debug.Log("Unknown m_turnState: " + m_turnState);
                break;
            }
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
        HideHands();
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
        m_chosenHandCardIndex = -1;
        if (!validCard)
            DiscardCard(card);
        if (m_drawDeck.Count == 0)
            StartDrawDeckFromDiscardDeck();
        ReplacePlayerCardInHand((BC.Player)playerIndex, cardIndex);
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

    void ExitFinishingRace()
    {
        SetActiveGenericBottomButton(false);
        m_frame = 0;
        m_state = GameState.InGame;
        m_currentPlayer = m_finishedRace.Winner;
        m_finishedRace.StartRace();
        m_finishedRace = null;
        EndPlayerTurn();
        StartPlayerTurn();
    }

    void ExitStartingPlayerTurn()
    {
        SetActiveGenericBottomButton(false);
        m_turnState = TurnState.PickCardFromHand;
        ShowHand(m_currentPlayer);
        UpdateStatus();
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
            for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            {
                var cubeCountText = playerCubesBackgroundRootName + (BC.CardCubeColour)cubeType;
                var cubeCountGO = GameObject.Find(cubeCountText);
                if (cubeCountGO == null)
                    Debug.LogError("Can't find " + (BC.CardCubeColour)cubeType + " cube counts GameObject " + cubeCountText);
                m_playerCubeCountsTexts[player, cubeType] = cubeCountGO.GetComponent<Text>();
                if (m_playerCubeCountsTexts[player, cubeType] == null)
                    Debug.LogError("Can't find " + (BC.CardCubeColour)cubeType + " cube counts UI Text Component");
            }
            var playerHandRootName = playerUIRootName + "Hand/";
            m_playerHandGO[player] = GameObject.Find(playerHandRootName);
            if (m_playerHandGO[player] == null)
                Debug.LogError("Can't find Player Hand UI GameObject " + (BC.Player)player + " " + playerHandRootName);
            for (int card = 0; card < HandSize; ++card)
            {
                int cardIndex = card + 1;
                var playerCardRootName = playerHandRootName + "Card" + cardIndex.ToString() + "/";
                var playerCardBackgroundName = playerCardRootName + "Background";
                var playerCardBackgroundGO = GameObject.Find(playerCardBackgroundName);
                if (playerCardBackgroundGO == null)
                    Debug.LogError("Can't find PlayerCardBackgroundGO " + playerCardBackgroundName);
                m_playerCardBackground[player, card] = playerCardBackgroundGO.GetComponent<Image>();
                var playerCardValueName = playerCardBackgroundName + "/Value";
                var playerCardValueGO = GameObject.Find(playerCardValueName);
                if (playerCardValueGO == null)
                    Debug.LogError("Can't find PlayerCardValueGO " + playerCardValueName);
                m_playerCardValue[player, card] = playerCardValueGO.GetComponent<Text>();
                if (m_playerCardValue[player, card] == null)
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

        HideHands();
        for (int player = 0; player < GameLogic.PlayerCount; ++player)
        {
            for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            {
                m_playerCubeCountsTexts[player, cubeType].color = CardCubeColour((BC.CardCubeColour)cubeType);
                m_playerCubeCounts[player, cubeType] = 0;
            }
            UpdateCubeCounts((BC.Player)player);
        }
        StartPlayerTurn();
    }

    void UpdateCubeCounts(BC.Player player)
    {
        int playerIndex = (int)player;
        for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
        {
            int cubeValue = m_playerCubeCounts[playerIndex, cubeType];
            m_playerCubeCountsTexts[playerIndex, cubeType].text = cubeValue.ToString();
        }
    }

    void InGame()
    {
        switch (m_turnState)
        {
            case TurnState.FinishingRace:
            {
                FinishingRace();
                break;
            }
        }
        ++m_frame;
        if (m_frame == 30)
        {
            m_frame = 0;
        }
        ValidateCubes();
    }

    void FinishingRace()
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
                StatusText.text = m_currentPlayer + " Player: Choose a Card to Play";
                break;
            case TurnState.PlayCardOnRace:
                StatusText.text = m_currentPlayer + " Player: Choose the Race to Play On";
                break;
            case TurnState.FinishingRace:
                StatusText.text = m_finishedRace.Winner + " Player Won the Race";
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
        m_drawDeck = m_discardDeck;
        ShuffleDrawDeck();
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
        m_playerHandGO[(int)player].SetActive(true);
    }

    void HideHands()
    {
        for (int player = 0; player < GameLogic.PlayerCount; ++player)
            HideHand((BC.Player)player);
    }

    void HideHand(BC.Player player)
    {
        m_playerHandGO[(int)player].SetActive(false);
    }

    void UpdatePlayerCardUI(BC.Player player, int cardIndex)
    {
        int playerIndex = (int)player;
        Card card = m_playerHands[playerIndex, cardIndex];
        string text = card.Value.ToString();
        int cardColour = (int)card.Colour;
        var colour = CardCubeColour(card.Colour);

        m_playerCardValue[playerIndex, cardIndex].text = text;
        m_playerCardBackground[playerIndex, cardIndex].color = colour;
        var textColour = (card.Colour == BC.CardCubeColour.Yellow) ? Color.black : Color.white;
        m_playerCardValue[playerIndex, cardIndex].color = textColour;
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
        m_playerHandGO = new GameObject[GameLogic.PlayerCount];
        m_playerCardBackground = new Image[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCardValue = new Text[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerHands = new Card[GameLogic.PlayerCount, GameLogic.HandSize];

        SetActiveGenericBottomButton(false);
    }

    void Start()
    {
    }

    public bool ValidateCubes()
    {
        int[] cubeCounts = new int[(int)BC.CardCubeColour.Count];
        bool error = false;
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
                    error = true;
                    Debug.LogError("Unknown cube value " + cube);
                    break;
            }
        }
        for (int cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
        {
            if (m_cubeCounts[cubeType] != cubeCounts[cubeType])
            {
                error = true;
                Debug.LogError("Cube count is incorrect " + (BC.CardCubeColour)cubeType + " " + cubeCounts[cubeType]);
            }
        }
        return error;
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
        }
    }
}
