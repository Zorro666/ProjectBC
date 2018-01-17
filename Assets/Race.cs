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

    Image m_background;
    Image[] m_cubeImages;
    GameLogic m_gamelogic;
    State m_state;
    Card[,] m_cards;
    int[] m_cardsPlayed;
    GameObject[,] m_playedCardsGO;
    Image[,] m_playedCardsBackground;
    Text[,] m_playedCardsValue;
    int[,] m_cardsRemaining;
    BC.Player m_winner;

    public BC.Player Winner
    {
        get { return m_winner; }
    }

    void Awake()
    {
        m_cubeImages = new Image[NumberOfCubes];
        m_state = State.Finished;
        m_cards = new Card[(int)BC.Player.Count, NumberOfCubes];
        m_cardsPlayed = new int[(int)BC.Player.Count];
        m_playedCardsGO = new GameObject[(int)BC.Player.Count, NumberOfCubes];
        m_playedCardsBackground = new Image[(int)BC.Player.Count, NumberOfCubes];
        m_playedCardsValue = new Text[(int)BC.Player.Count, NumberOfCubes];
        m_cardsRemaining = new int[(int)BC.Player.Count, GameLogic.CubeTypeCount];
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

        for (int player = 0; player < (int)BC.Player.Count; ++player)
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
        bool canPlayCard = false;
        for (int playerIndex = 0; playerIndex < (int)BC.Player.Count; ++playerIndex)
        {
            canPlayCard |= (m_cardsRemaining[playerIndex, cardColour] > 0);
        }
        return canPlayCard;
    }

    public void StartRace()
    {
        for (int i = 0; i < m_cardsPlayed.Length; ++i)
            m_cardsPlayed[i] = 0;

        for (int playerIndex = 0; playerIndex < (int)BC.Player.Count; ++playerIndex)
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
            return;
        }

        foreach (Image cubeImage in m_cubeImages)
        {
            var cubeColour = m_gamelogic.NextCube();
            var colour = (int)cubeColour;
            cubeImage.color = m_gamelogic.CardCubeColour(cubeColour);
            for (int playerIndex = 0; playerIndex < (int)BC.Player.Count; ++playerIndex)
                ++m_cardsRemaining[playerIndex, colour];
        }
        if (m_state == State.Lowest)
            m_background.color = m_gamelogic.RaceLowestColour;
        else if (m_state == State.Highest)
            m_background.color = m_gamelogic.RaceHighestColour;

        m_winner = BC.Player.Unknown;
    }

    BC.Player ComputeWinner()
    {
        BC.Player maxScorePlayer = BC.Player.Unknown;
        int maxScoreValue = -1;
        BC.Player minScorePlayer = BC.Player.Unknown;
        int minScoreValue = 9999;
        bool maxTie = false;
        bool minTie = false;
        for (int playerIndex = 0; playerIndex < (int)BC.Player.Count; ++playerIndex)
        {
            int score = 0;
            BC.Player player = (BC.Player)playerIndex;
            for (int cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
                score += m_cards[playerIndex, cardIndex].Value;

            if (score == maxScoreValue)
            {
                maxTie = true;
                maxScorePlayer = m_gamelogic.CurrentPlayer;
            }

            if (score > maxScoreValue)
            {
                maxScoreValue = score;
                maxScorePlayer = player;
                maxTie = false;
            }

            if (score == minScoreValue)
            {
                minTie = true;
                minScorePlayer = m_gamelogic.CurrentPlayer;
            }

            if (score < minScoreValue)
            {
                minScoreValue = score;
                minScorePlayer = player;
                minTie = false;
            }
        }
        Debug.Log("max " + maxScorePlayer + " " + maxScoreValue + " Tie " + maxTie);
        Debug.Log("min " + minScorePlayer + " " + minScoreValue + " Tie " + minTie);
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

    public void FinishRace()
    {
        m_winner = ComputeWinner();
        Debug.Log(m_state + " Player " + m_winner + " won");
        for (int cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
        {
            int playerIndex = (int)m_winner;
            Card card = m_cards[playerIndex, cardIndex];
            m_gamelogic.AddCubeToPlayer(m_winner, card.Colour);
        }

        for (int playerIndex = 0; playerIndex < (int)BC.Player.Count; ++playerIndex)
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

    public bool PlayCard(BC.Player player, Card card)
    {
        Debug.Log(name + " PlayCard " + player + " " + card.Colour + " " + card.Value);
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
        string text = card.Value.ToString();
        ++m_cardsPlayed[playerIndex];
        m_playedCardsValue[playerIndex, cardIndex].text = text;
        var colour = m_gamelogic.CardCubeColour(card.Colour);
        m_playedCardsBackground[playerIndex, cardIndex].color = colour;
        var textColour = (card.Colour == BC.CardCubeColour.Yellow) ? Color.black : Color.white;
        m_playedCardsValue[playerIndex, cardIndex].color = textColour;
        m_playedCardsGO[playerIndex, cardIndex].SetActive(true);
        m_cards[playerIndex, cardIndex] = card;
        //Debug.Log(m_cards[playerIndex, cardIndex].Value);

        bool raceFinished = true;
        foreach (var cardsPlayed in m_cardsPlayed)
        {
            if (cardsPlayed != NumberOfCubes)
                raceFinished = false;
        }
        if (raceFinished)
            FinishRace();
        return true;
    }
}
