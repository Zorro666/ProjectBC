using UnityEngine;
using UnityEngine.UI;
using BC;

public class RaceUI : MonoBehaviour 
{
    public int NumberOfCubes;

    GameObject[,] m_playedCardsGO;
    Image[,] m_playedCardsBackground;
    Text[,] m_playedCardsValue;
    Image m_background;
    Image[] m_cubeImages;
    GameLogic m_gameLogic;

    void Awake()
    {
        m_cubeImages = new Image[NumberOfCubes];
        m_playedCardsGO = new GameObject[GameLogic.PlayerCount, NumberOfCubes];
        m_playedCardsBackground = new Image[GameLogic.PlayerCount, NumberOfCubes];
        m_playedCardsValue = new Text[GameLogic.PlayerCount, NumberOfCubes];
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
            var playedCardsRootName = raceCardName + (Player)player + "Card";
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

    public void SetPlayCardButtons(int playerIndex, int cardIndex, bool active)
    {
        SetPlayCardButtonInteractable(playerIndex, cardIndex, active);
        m_playedCardsGO[playerIndex, cardIndex].SetActive(active);
    }

    public void SetPlayCardButtonInteractable(int playerIndex, int cardIndex, bool interactable)
    {
        m_playedCardsGO[playerIndex, cardIndex].GetComponent<Button>().interactable = interactable;
    }

    public void SetFinished()
    {
        m_background.color = m_gameLogic.RaceFinishedColour;
        foreach (Image cubeImage in m_cubeImages)
            cubeImage.color = m_gameLogic.RaceFinishedColour;
    }

    public void StartRace(RaceState raceState)
    {
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
            {
                m_playedCardsGO[playerIndex, cardIndex].SetActive(false);
            }
        }

        if (raceState == RaceState.Lowest)
            m_background.color = m_gameLogic.RaceLowestColour;
        else if (raceState == RaceState.Highest)
            m_background.color = m_gameLogic.RaceHighestColour;
    }

    public void SetCube(int index, CupCardCubeColour colour)
    {
        m_cubeImages[index].color = m_gameLogic.GetCardCubeColour(colour);
    }

	public void Initialise(GameLogic gamelogic) 
    {
        m_gameLogic = gamelogic;
    }

    public void SetPlayedCardToCard(int playerIndex, int cardIndex, Card card)
    {
        string text = card.Value.ToString();
        m_playedCardsValue[playerIndex, cardIndex].text = text;
        var colour = m_gameLogic.GetCardCubeColour(card.Colour);
        m_playedCardsBackground[playerIndex, cardIndex].color = colour;
        var textColour = (card.Colour == CupCardCubeColour.Yellow) ? Color.black : Color.white;
        m_playedCardsValue[playerIndex, cardIndex].color = textColour;
    }
}
