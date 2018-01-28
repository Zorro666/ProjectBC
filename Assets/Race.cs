using UnityEngine;
using UnityEngine.UI;

public class Race : MonoBehaviour 
{
    public enum State
    {
        Lowest,
        Highest,
        Finished
    }

    public int NumberOfCubes;

    GameLogic m_gamelogic;

    State m_state;
    Card[,] m_cards;
    int[] m_cardsPlayed;
    int[,] m_cardsRemaining;
    BC.Player m_winner;

    GameObject[,] m_playedCardsGO;
    Image[,] m_playedCardsBackground;
    Text[,] m_playedCardsValue;
    Image m_background;
    Image[] m_cubeImages;

    public BC.Player Winner
    {
        get { return m_winner; }
    }

    void Awake()
    {
        m_cubeImages = new Image[NumberOfCubes];
        m_state = State.Finished;
        m_cards = new Card[GameLogic.PlayerCount, NumberOfCubes];
        m_cardsPlayed = new int[GameLogic.PlayerCount];
        m_playedCardsGO = new GameObject[GameLogic.PlayerCount, NumberOfCubes];
        m_playedCardsBackground = new Image[GameLogic.PlayerCount, NumberOfCubes];
        m_playedCardsValue = new Text[GameLogic.PlayerCount, NumberOfCubes];
        m_cardsRemaining = new int[GameLogic.PlayerCount, GameLogic.CubeTypeCount];
    }

    void Start() 
    {
        var raceCardName = "/" + name + "/RaceCard/";
        var backgroundName = raceCardName + "Background";
        var backgroundGO = GameObject.Find(backgroundName);
        m_background = backgroundGO.GetComponent<Image>();
        if (m_background == null)
            Debug.LogError("Can't find Background " + backgroundName);
        var cubeNamePrefix = backgroundName + "/";
        for (int i = 0; i < NumberOfCubes; ++i)
        {
            var cubeIndex = i + 1;
            var cubeName = cubeNamePrefix + "Cube" + cubeIndex;
            var cubeGO = GameObject.Find(cubeName);
            m_cubeImages[i] = cubeGO.GetComponent<Image>();
            if (m_cubeImages[i] == null)
                Debug.LogError("Can't find Cube[" + cubeIndex + "] '" + cubeName + "'");
        }

        for (int player = 0; player < GameLogic.PlayerCount; ++player)
        {
            var playedCardsRootName = raceCardName + (BC.Player)player + "Card";
            for (int j = 0; j < NumberOfCubes; ++j)
            {
                var cubeIndex = j + 1;
                var playedCardsName = playedCardsRootName + cubeIndex;
                m_playedCardsGO[player, j] = GameObject.Find(playedCardsName);
                if (m_playedCardsGO[player, j] == null)
                    Debug.LogError("Can't find PlayedCardsGO " + playedCardsName);
                var playedCardsBackgroundName = playedCardsName + "/Background";
                var playedCardsBackgroundGO = GameObject.Find(playedCardsBackgroundName);
                if (playedCardsBackgroundGO == null)
                    Debug.LogError("Can't find PlayedCardsBackgroundGO " + playedCardsBackgroundName);
                m_playedCardsBackground[player, j] = playedCardsBackgroundGO.GetComponent<Image>();
                var playedCardsValueName = playedCardsBackgroundName + "/Value";
                var playedCardsValueGO = GameObject.Find(playedCardsValueName);
                if (playedCardsValueGO == null)
                    Debug.LogError("Can't find PlayedCardsValueGO " + playedCardsValueName);
                m_playedCardsValue[player, j] = playedCardsValueGO.GetComponent<Text>();
            }
        }
    }

    void Update()
    {
    }

    public bool CanPlayCard(Card card)
    {
        int cardColour = (int)card.Colour;
        for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            if (m_cardsRemaining[playerIndex, cardColour] > 0)
                return true;
        }
        return false;
    }

    public void ResetPlayCardButtons()
    {
        for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            int cardIndex = m_cardsPlayed[playerIndex];
            if (cardIndex < NumberOfCubes)
            {
                m_playedCardsGO[playerIndex, cardIndex].GetComponent<Button>().interactable = false;
                m_playedCardsGO[playerIndex, cardIndex].SetActive(false);
            }
        }
    }

    public void SetPlayCardButtons(Card card)
    {
        int cardColour = (int)card.Colour;
        for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            int cardIndex = m_cardsPlayed[playerIndex];
            bool setValue = false;
            bool active = false;
            if (m_cardsRemaining[playerIndex, cardColour] > 0)
            {
                active = true;
                setValue = true;
            }
            else if (cardIndex < NumberOfCubes)
            {
                active = false;
                setValue = true;
            }
            if (setValue)
            {
                SetPlayedCardToCard(playerIndex, cardIndex, card);
                m_playedCardsGO[playerIndex, cardIndex].GetComponent<Button>().interactable = active;
                m_playedCardsGO[playerIndex, cardIndex].SetActive(active);
            }
        }
    }

    public void StartRace()
    {
        for (int i = 0; i < m_cardsPlayed.Length; ++i)
            m_cardsPlayed[i] = 0;

        for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            for (int cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
            {
                m_cards[playerIndex, cardIndex] = null;
                m_playedCardsGO[playerIndex, cardIndex].SetActive(false);
            }
            for (int colour = 0; colour < GameLogic.CubeTypeCount; ++colour)
                m_cardsRemaining[playerIndex, colour] = 0;
        }

        if (m_gamelogic.CubesRemainingCount < NumberOfCubes)
            m_state = State.Finished;

        if (m_state == State.Finished)
        {
            m_background.color = m_gamelogic.RaceFinishedColour;
            foreach (Image cubeImage in m_cubeImages)
                cubeImage.color = m_gamelogic.RaceFinishedColour;
            return;
        }

        foreach (Image cubeImage in m_cubeImages)
        {
            var cubeColour = m_gamelogic.NextCube();
            var colour = (int)cubeColour;
            cubeImage.color = m_gamelogic.GetCardCubeColour(cubeColour);
            for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
                ++m_cardsRemaining[playerIndex, colour];
        }
        if (m_state == State.Lowest)
            m_background.color = m_gamelogic.RaceLowestColour;
        else if (m_state == State.Highest)
            m_background.color = m_gamelogic.RaceHighestColour;

        m_winner = BC.Player.Unknown;
    }

    BC.Player ComputeWinner(BC.Player currentPlayer)
    {
        BC.Player maxScorePlayer = BC.Player.Unknown;
        int maxScoreValue = -1;
        BC.Player minScorePlayer = BC.Player.Unknown;
        int minScoreValue = 9999;
        for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            int score = 0;
            BC.Player player = (BC.Player)playerIndex;
            for (int cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
                score += m_cards[playerIndex, cardIndex].Value;

            if (score == maxScoreValue)
            {
                maxScorePlayer = currentPlayer;
            }

            if (score > maxScoreValue)
            {
                maxScoreValue = score;
                maxScorePlayer = player;
            }

            if (score == minScoreValue)
            {
                minScorePlayer = currentPlayer;
            }

            if (score < minScoreValue)
            {
                minScoreValue = score;
                minScorePlayer = player;
            }
        }
        Debug.Log("max " + maxScorePlayer + " " + maxScoreValue + " CurrentPlayer:" + currentPlayer);
        Debug.Log("min " + minScorePlayer + " " + minScoreValue + " CurrentPlayer:" + currentPlayer);
        if (m_state == State.Lowest)
            return minScorePlayer;
        if (m_state == State.Highest)
            return maxScorePlayer;
        Debug.LogError("m_state is not Lowest or Highest");
        return BC.Player.Unknown;
    }

	public void Initialise() 
    {
        m_gamelogic = GameLogic.GetInstance();
    }

    public void NewGame() 
    {
        if ((NumberOfCubes % 2) == 1)
            m_state = State.Lowest;
        else
            m_state = State.Highest;
        StartRace();
    }

    public void FinishRace(BC.Player currentPlayer)
    {
        m_winner = ComputeWinner(currentPlayer);
        Debug.Log(m_state + " Player " + m_winner + " won");
        for (int cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
        {
            int playerIndex = (int)m_winner;
            Card card = m_cards[playerIndex, cardIndex];
            m_gamelogic.AddCubeToPlayer(m_winner, card.Colour);
        }

        for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            for (int cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
                m_gamelogic.DiscardCard(m_cards[playerIndex, cardIndex]);
        }

        if (m_state == State.Lowest)
            m_state = State.Highest;
        else if (m_state == State.Highest)
            m_state = State.Lowest;

        m_gamelogic.FinishRace(m_winner, this);
    }

    void SetPlayedCardToCard(int playerIndex, int cardIndex, Card card)
    {
        string text = card.Value.ToString();
        m_playedCardsValue[playerIndex, cardIndex].text = text;
        var colour = m_gamelogic.GetCardCubeColour(card.Colour);
        m_playedCardsBackground[playerIndex, cardIndex].color = colour;
        var textColour = (card.Colour == BC.CupCardCubeColour.Yellow) ? Color.black : Color.white;
        m_playedCardsValue[playerIndex, cardIndex].color = textColour;
    }

    public bool PlayCard(BC.Player player, Card card, BC.Player currentPlayer)
    {
        //Debug.Log(name + " PlayCard " + player + " " + card.Colour + " " + card.Value);
        if (m_state == State.Finished)
        {
            Debug.Log(name + " race is Finished");
            return false;
        }
        int playerIndex = (int)player;
        int cardIndex = m_cardsPlayed[playerIndex];
        if (cardIndex == NumberOfCubes)
        {
            Debug.Log(player + " player is full");
            return false;
        }
        int cardColour = (int)card.Colour;
        if (m_cardsRemaining[playerIndex, cardColour] == 0)
        {
            Debug.Log(player + " player " + card.Colour + " can't be played");
            return false;
        }
        --m_cardsRemaining[playerIndex, cardColour];
        ++m_cardsPlayed[playerIndex];
        SetPlayedCardToCard(playerIndex, cardIndex, card);
        m_playedCardsGO[playerIndex, cardIndex].SetActive(true);
        m_playedCardsGO[playerIndex, cardIndex].GetComponent<Button>().interactable = false;
        m_cards[playerIndex, cardIndex] = card;
        //Debug.Log(m_cards[playerIndex, cardIndex].Value);

        bool raceFinished = true;
        foreach (var cardsPlayed in m_cardsPlayed)
        {
            if (cardsPlayed != NumberOfCubes)
                raceFinished = false;
        }
        if (raceFinished)
            FinishRace(currentPlayer);
        return true;
    }
}
