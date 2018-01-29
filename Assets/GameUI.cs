using UnityEngine;
using UnityEngine.UI;
using BC;

public class GameUI : MonoBehaviour
{
    public Text StatusText;

    GameObject[] m_unclaimedCupGOs;
    Button[] m_unclaimedCupButtons;

    Text[,] m_playerCubeCountsTexts;
    Text[] m_playerWildcardCubeCountTexts;
    GameObject[] m_playerHandGOs;
    Image[,] m_playerCardOutlines;
    Image[,] m_playerCardBackgrounds;
    Text[,] m_playerCardValues;

#if JAKE_ZERO
    //TODO: make a Player class and store this data per Player
    Card[,] m_playerHands;
    GameObject[,] m_playerCupGOs;
    Image[,] m_playerCupImages;
    Text[,] m_playerCupValues;
    GameObject[] m_playerGenericButtons;
#endif

    public void SetStatusText(string text)
    {
        StatusText.text = text;
    }

    public void SetCupStatus(int cupIndex, bool available)
    {
        m_unclaimedCupGOs[cupIndex].SetActive(available);
    }

    public void SetCupInteractible(int cupIndex, bool interactible)
    {
        m_unclaimedCupButtons[cupIndex].interactable = interactible;
    }

    //TODO: this should be in logic not UI
    public bool IsCupInteractible(int cupIndex)
    {
        return m_unclaimedCupButtons[cupIndex].interactable;
    }

    public void SetPlayerCubeCountColour(int playerIndex, int cubeIndex, Color colour)
    {
        m_playerCubeCountsTexts[playerIndex, cubeIndex].color = colour;
    }

    public void SetPlayerCubeCountValue(int playerIndex, int cubeIndex, int cubeValue)
    {
        m_playerCubeCountsTexts[playerIndex, cubeIndex].text = cubeValue.ToString();
    }

    public void SetPlayerWildcardCubeCountValue(int playerIndex, int cubeValue)
    {
        m_playerWildcardCubeCountTexts[playerIndex].text = cubeValue.ToString();
    }

    public void SetPlayerHandActive(int playerIndex, bool active)
    {
        m_playerHandGOs[playerIndex].SetActive(active);
    }

    public void SetPlayerCardHighlighted(int playerIndex, int cardIndex, bool highlighted)
    {
        m_playerCardOutlines[playerIndex, cardIndex].color = highlighted ? Color.white : Color.black;
    }

    public void SetPlayerCard(int playerIndex, int cardIndex, Card card, Color colour)
    {
        string text = card.Value.ToString();
        m_playerCardValues[playerIndex, cardIndex].text = text;
        var textColour = (card.Colour == CupCardCubeColour.Yellow) ? Color.black : Color.white;
        m_playerCardValues[playerIndex, cardIndex].color = textColour;
        m_playerCardBackgrounds[playerIndex, cardIndex].color = colour;
    }

    void Awake()
    {
        m_unclaimedCupGOs = new GameObject[GameLogic.CubeTypeCount];
        m_unclaimedCupButtons = new Button[GameLogic.CubeTypeCount];
        m_playerCubeCountsTexts = new Text[GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_playerWildcardCubeCountTexts = new Text[GameLogic.PlayerCount];
        m_playerHandGOs = new GameObject[GameLogic.PlayerCount];
        m_playerCardOutlines = new Image[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCardBackgrounds = new Image[GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCardValues = new Text[GameLogic.PlayerCount, GameLogic.HandSize];
    }

    public void Initialise()
    {
        var gameBoardUIRootName = "/GameBoard/UI/";
        var unclaimedCupRootName = gameBoardUIRootName + "CupsBackground/";
        for (var cupIndex = 0; cupIndex < GameLogic.CubeTypeCount; ++cupIndex)
        {
            var unclaimedCupName = unclaimedCupRootName + (CupCardCubeColour)cupIndex;
            m_unclaimedCupGOs[cupIndex] = GameObject.Find(unclaimedCupName);
            if (m_unclaimedCupGOs[cupIndex] == null)
                Debug.LogError("Can't find cup " + (CupCardCubeColour)cupIndex + " " + unclaimedCupName);
            m_unclaimedCupButtons[cupIndex] = m_unclaimedCupGOs[cupIndex].GetComponent<Button>();
            if (m_unclaimedCupButtons[cupIndex] == null)
                Debug.LogError("Can't find cup " + (CupCardCubeColour)cupIndex + " Button Component");
        }

        for (var player = 0; player < GameLogic.PlayerCount; ++player)
        {
            var playerUIRootName = gameBoardUIRootName + (Player)player + "Player/";
            var playerHandRootName = playerUIRootName + "Hand/";
            m_playerHandGOs[player] = GameObject.Find(playerHandRootName);
            if (m_playerHandGOs[player] == null)
                Debug.LogError("Can't find Player Hand UI GameObject " + (Player)player + " " + playerHandRootName);

            for (var card = 0; card < GameLogic.HandSize; ++card)
            {
                var cardIndex = card + 1;
                var playerCardRootName = playerHandRootName + "Card" + cardIndex.ToString() + "/";

                var playerCardOutlineGO = GameObject.Find(playerCardRootName);
                if (playerCardOutlineGO == null)
                    Debug.LogError("Can't find PlayerCardOutlineGO " + playerCardRootName);
                m_playerCardOutlines[player, card] = playerCardOutlineGO.GetComponent<Image>();
                if (m_playerCardOutlines[player, card] == null)
                    Debug.LogError("Can't find Player " + (Player)player + " Card[" + cardIndex + " Outline Image " + playerCardRootName);

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
                    Debug.LogError("Can't find Player " + (Player)player + " Card[" + cardIndex + " Value Text " + playerCardValueName);
            }

            var playerCubesBackgroundRootName = playerUIRootName + "CubesBackground/";
            for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            {
                var cubeCountText = playerCubesBackgroundRootName + (CupCardCubeColour)cubeType;
                var cubeCountGO = GameObject.Find(cubeCountText);
                if (cubeCountGO == null)
                    Debug.LogError("Can't find " + (CupCardCubeColour)cubeType + " cube count GameObject " + cubeCountText);
                m_playerCubeCountsTexts[player, cubeType] = cubeCountGO.GetComponent<Text>();
                if (m_playerCubeCountsTexts[player, cubeType] == null)
                    Debug.LogError("Can't find " + (CupCardCubeColour)cubeType + " cube count UI Text Component");
            }
            var wildcardCubeCountText = playerCubesBackgroundRootName + "White";
            var wildcardCubeCountGO = GameObject.Find(wildcardCubeCountText);
            if (wildcardCubeCountGO == null)
                Debug.LogError("Can't find wildcard cube count GameObject " + wildcardCubeCountText);

            m_playerWildcardCubeCountTexts[player] = wildcardCubeCountGO.GetComponent<Text>();
            if (m_playerWildcardCubeCountTexts[player] == null)
                Debug.LogError("Can't find wildcard cube count UI Text Component");
        }
    }
}