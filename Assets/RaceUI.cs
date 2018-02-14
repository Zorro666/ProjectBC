using System;
using BC;
using UnityEngine;
using UnityEngine.UI;

public class RaceUI : MonoBehaviour
{
    Image m_Background;
    Image[] m_CubeImages;
    GameLogic m_GameLogic;
    Image[,] m_PlayedCardsBackground;

    GameObject[,] m_PlayerCardsGameObject;
    Text[,] m_PlayedCardsValue;
    public int NumberOfCubes;

    void Awake()
    {
        m_CubeImages = new Image [NumberOfCubes];
        m_PlayerCardsGameObject = new GameObject [GameLogic.PlayerCount, NumberOfCubes];
        m_PlayedCardsBackground = new Image [GameLogic.PlayerCount, NumberOfCubes];
        m_PlayedCardsValue = new Text [GameLogic.PlayerCount, NumberOfCubes];
    }

    public void SetPlayCardButtons(int playerIndex, int cardIndex, bool active)
    {
        SetPlayCardButtonInteractable(playerIndex, cardIndex, active);
        m_PlayerCardsGameObject[playerIndex, cardIndex].SetActive(active);
    }

    public void SetPlayCardButtonInteractable(int playerIndex, int cardIndex, bool interactable)
    {
        m_PlayerCardsGameObject[playerIndex, cardIndex].GetComponent<Button>().interactable = interactable;
    }

    public void SetFinished()
    {
        m_Background.color = m_GameLogic.RaceFinishedColour;
        foreach (var cubeImage in m_CubeImages)
            cubeImage.color = m_GameLogic.RaceFinishedColour;
    }

    public void StartRace(RaceState raceState)
    {
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
            m_PlayerCardsGameObject[playerIndex, cardIndex].SetActive(false);

        if (raceState == RaceState.Lowest)
            m_Background.color = m_GameLogic.RaceLowestColour;
        else if (raceState == RaceState.Highest)
            m_Background.color = m_GameLogic.RaceHighestColour;
    }

    public void SetCube(int index, CupCardCubeColour colour)
    {
        m_CubeImages[index].color = m_GameLogic.GetCardCubeColour(colour);
    }

    public void Initialise(GameLogic gamelogic)
    {
        m_GameLogic = gamelogic;

        var raceCardName = "/" + name + "/RaceCard/";
        var backgroundName = raceCardName + "Background";
        var backgroundGameObject = GameObject.Find(backgroundName);
        m_Background = backgroundGameObject.GetComponent<Image>();
        if (m_Background == null)
            Debug.LogError("Can't find Background " + backgroundName);
        var cubeNamePrefix = backgroundName + "/";
        for (var i = 0; i < NumberOfCubes; ++i)
        {
            var cubeIndex = i + 1;
            var cubeName = cubeNamePrefix + "Cube" + cubeIndex;
            var cubeGameObject = GameObject.Find(cubeName);
            m_CubeImages[i] = cubeGameObject.GetComponent<Image>();
            if (m_CubeImages[i] == null)
                Debug.LogError("Can't find Cube[" + cubeIndex + "] '" + cubeName + "'");
        }

        for (var player = 0; player < GameLogic.PlayerCount; ++player)
        {
            var playedCardsRootName = raceCardName + (Player)player + "Card";
            for (var j = 0; j < NumberOfCubes; ++j)
            {
                var cubeIndex = j + 1;
                var playedCardsName = playedCardsRootName + cubeIndex;
                m_PlayerCardsGameObject[player, j] = GameObject.Find(playedCardsName);
                if (m_PlayerCardsGameObject[player, j] == null)
                    Debug.LogError("Can't find PlayedCardsGO " + playedCardsName);
                var playedCardsBackgroundName = playedCardsName + "/Background";
                var playedCardsBackgroundGameObject = GameObject.Find(playedCardsBackgroundName);
                if (playedCardsBackgroundGameObject == null)
                    Debug.LogError("Can't find PlayedCardsBackgroundGameObject " + playedCardsBackgroundName);
                else
                    m_PlayedCardsBackground[player, j] = playedCardsBackgroundGameObject.GetComponent<Image>();
                var playedCardsValueName = playedCardsBackgroundName + "/Value";
                var playedCardsValueGameObject = GameObject.Find(playedCardsValueName);
                if (playedCardsValueGameObject == null)
                    Debug.LogError("Can't find PlayedCardsValueGameObject " + playedCardsValueName);
                else
                    m_PlayedCardsValue[player, j] = playedCardsValueGameObject.GetComponent<Text>();
            }
        }
    }

    public void SetPlayedCardToCard(int playerIndex, int cardIndex, Card card)
    {
        var text = card.Value.ToString();
        m_PlayedCardsValue[playerIndex, cardIndex].text = text;
        var colour = m_GameLogic.GetCardCubeColour(card.Colour);
        m_PlayedCardsBackground[playerIndex, cardIndex].color = colour;
        var textColour = card.Colour == CupCardCubeColour.Yellow ? Color.black : Color.white;
        m_PlayedCardsValue[playerIndex, cardIndex].color = textColour;
    }
}
