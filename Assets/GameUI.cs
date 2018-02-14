using System;
using BC;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    Image[,] m_PlayerCardBackgrounds;
    Image[,] m_PlayerCardOutlines;
    Text[,] m_PlayerCardValues;

    //TODO: make a Player class and store this data per Player
    Text[,] m_PlayerCubeCountsTexts;
    GameObject[,] m_PlayerCupGOs;
    Image[,] m_PlayerCupImages;
    Text[,] m_PlayerCupValues;
    GameObject[] m_PlayerGenericButtons;
    GameObject[] m_PlayerHandGOs;
    Text[] m_PlayerWildcardCubeCountTexts;
    Button m_RobotButton;
    Text m_RobotButtonText;
    Button[] m_UnclaimedCupButtons;

    GameObject[] m_UnclaimedCupGOs;
    public Text StatusText;

    public void SetRobotButtonText(string text)
    {
        m_RobotButtonText.text = text;
    }

    public void SetStatusText(string text)
    {
        StatusText.text = text;
    }

    public void SetCupStatus(int cupIndex, bool available)
    {
        m_UnclaimedCupGOs[cupIndex].SetActive(available);
    }

    public void SetCupInteractible(int cupIndex, bool interactible)
    {
        m_UnclaimedCupButtons[cupIndex].interactable = interactible;
    }

    public void SetPlayerCubeCountColour(int playerIndex, int cubeIndex, Color colour)
    {
        m_PlayerCubeCountsTexts[playerIndex, cubeIndex].color = colour;
    }

    public void SetPlayerCubeCountValue(int playerIndex, int cubeIndex, int cubeValue)
    {
        m_PlayerCubeCountsTexts[playerIndex, cubeIndex].text = cubeValue.ToString();
    }

    public void SetPlayerWildcardCubeCountValue(int playerIndex, int cubeValue)
    {
        m_PlayerWildcardCubeCountTexts[playerIndex].text = cubeValue.ToString();
    }

    public void SetPlayerHandActive(int playerIndex, bool active)
    {
        m_PlayerHandGOs[playerIndex].SetActive(active);
    }

    public void SetPlayerCardHighlighted(int playerIndex, int cardIndex, bool highlighted)
    {
        m_PlayerCardOutlines[playerIndex, cardIndex].color = highlighted ? Color.white : Color.black;
    }

    public void SetPlayerCard(int playerIndex, int cardIndex, Card card, Color colour)
    {
        var text = card.Value.ToString();
        m_PlayerCardValues[playerIndex, cardIndex].text = text;
        var textColour = card.Colour == CupCardCubeColour.Yellow ? Color.black : Color.white;
        m_PlayerCardValues[playerIndex, cardIndex].color = textColour;
        m_PlayerCardBackgrounds[playerIndex, cardIndex].color = colour;
    }

    public void SetPlayerCup(int playerIndex, int cupDisplayIndex, CupCardCubeColour cupType, int value, Color colour)
    {
        m_PlayerCupImages[playerIndex, cupDisplayIndex].color = colour;
        m_PlayerCupValues[playerIndex, cupDisplayIndex].text = value.ToString();
        var textColour = cupType == CupCardCubeColour.Yellow ? Color.black : Color.white;
        m_PlayerCupValues[playerIndex, cupDisplayIndex].color = textColour;
    }

    public void SetPlayerCupActive(int playerIndex, int cupIndex, bool active)
    {
        m_PlayerCupGOs[playerIndex, cupIndex].SetActive(active);
    }

    public void ShowPlayerGenericButton(int playerIndex)
    {
        m_PlayerGenericButtons[playerIndex].SetActive(true);
    }

    public bool IsPlayerGenericButtonActive(int playerIndex)
    {
        return m_PlayerGenericButtons[playerIndex].activeSelf;
    }

    public void HidePlayerGenericButtons()
    {
        for (var p = 0; p < GameLogic.PlayerCount; ++p)
            m_PlayerGenericButtons[p].SetActive(false);
    }

    public void SetPlayerGenericButtonText(int playerIndex, string text)
    {
        m_PlayerGenericButtons[playerIndex].GetComponentInChildren<Text>().text = text;
    }

    void Awake()
    {
        m_UnclaimedCupGOs = new GameObject [GameLogic.CubeTypeCount];
        m_UnclaimedCupButtons = new Button [GameLogic.CubeTypeCount];
        m_PlayerCubeCountsTexts = new Text [GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_PlayerWildcardCubeCountTexts = new Text [GameLogic.PlayerCount];
        m_PlayerHandGOs = new GameObject [GameLogic.PlayerCount];
        m_PlayerCardOutlines = new Image [GameLogic.PlayerCount, GameLogic.HandSize];
        m_PlayerCardBackgrounds = new Image [GameLogic.PlayerCount, GameLogic.HandSize];
        m_PlayerCardValues = new Text [GameLogic.PlayerCount, GameLogic.HandSize];
        m_PlayerCupGOs = new GameObject [GameLogic.PlayerCount, GameLogic.MaxCupsPerPlayer];
        m_PlayerCupImages = new Image [GameLogic.PlayerCount, GameLogic.MaxCupsPerPlayer];
        m_PlayerCupValues = new Text [GameLogic.PlayerCount, GameLogic.MaxCupsPerPlayer];
        m_PlayerGenericButtons = new GameObject [GameLogic.PlayerCount];
    }

    public void Initialise()
    {
        var gameBoardUIRootName = "/GameBoard/UI/";
        var unclaimedCupRootName = gameBoardUIRootName + "CupsBackground/";
        for (var cupIndex = 0; cupIndex < GameLogic.CubeTypeCount; ++cupIndex)
        {
            var unclaimedCupName = unclaimedCupRootName + (CupCardCubeColour)cupIndex;
            m_UnclaimedCupGOs[cupIndex] = GameObject.Find(unclaimedCupName);
            if (m_UnclaimedCupGOs[cupIndex] == null)
                Debug.LogError("Can't find cup " + (CupCardCubeColour)cupIndex + " " + unclaimedCupName);
            else
                m_UnclaimedCupButtons[cupIndex] = m_UnclaimedCupGOs[cupIndex].GetComponent<Button>();
            if (m_UnclaimedCupButtons[cupIndex] == null)
                Debug.LogError("Can't find cup " + (CupCardCubeColour)cupIndex + " Button Component");
        }

        for (var player = 0; player < GameLogic.PlayerCount; ++player)
        {
            var playerUIRootName = gameBoardUIRootName + (Player)player + "Player/";
            var playerHandRootName = playerUIRootName + "Hand/";
            m_PlayerHandGOs[player] = GameObject.Find(playerHandRootName);
            if (m_PlayerHandGOs[player] == null)
                Debug.LogError("Can't find Player Hand UI GameObject " + (Player)player + " " + playerHandRootName);

            var playerGenericButtonName = playerUIRootName + "GenericButton";
            m_PlayerGenericButtons[player] = GameObject.Find(playerGenericButtonName);
            if (m_PlayerGenericButtons[player] == null)
                Debug.LogError("Can't find GenericButton for Player " + (Player)player + " " + playerGenericButtonName);

            var playerCupsRootName = playerUIRootName + "CupsBackground/";
            for (var cupIndex = 0; cupIndex < GameLogic.MaxCupsPerPlayer; ++cupIndex)
            {
                var playerCupIndex = cupIndex + 1;
                var cupImageName = playerCupsRootName + "Cup" + playerCupIndex;
                m_PlayerCupGOs[player, cupIndex] = GameObject.Find(cupImageName);
                if (m_PlayerCupGOs[player, cupIndex] == null)
                    Debug.LogError("Can't find Cup " + playerCupIndex + " Image GameObject " + cupImageName);
                else
                    m_PlayerCupImages[player, cupIndex] = m_PlayerCupGOs[player, cupIndex].GetComponent<Image>();
                if (m_PlayerCupImages[player, cupIndex] == null)
                    Debug.LogError("Can't find Cup " + playerCupIndex + " Image Component");

                var cupValueName = cupImageName + "/Value";
                var cupValueGameObject = GameObject.Find(cupValueName);
                if (cupValueGameObject == null)
                    Debug.LogError("Can't find Cup " + playerCupIndex + " Value GameObject " + cupValueName);
                else
                    m_PlayerCupValues[player, cupIndex] = cupValueGameObject.GetComponent<Text>();
                if (m_PlayerCupValues[player, cupIndex] == null)
                    Debug.LogError("Can't find Cup " + playerCupIndex + " Value Component");
            }

            for (var card = 0; card < GameLogic.HandSize; ++card)
            {
                var cardIndex = card + 1;
                var playerCardRootName = playerHandRootName + "Card" + cardIndex + "/";

                var playerCardOutlineGameObject = GameObject.Find(playerCardRootName);
                if (playerCardOutlineGameObject == null)
                    Debug.LogError("Can't find PlayerCardOutlineGameObject " + playerCardRootName);
                else
                    m_PlayerCardOutlines[player, card] = playerCardOutlineGameObject.GetComponent<Image>();
                if (m_PlayerCardOutlines[player, card] == null)
                    Debug.LogError("Can't find Player " + (Player)player + " Card[" + cardIndex + " Outline Image " + playerCardRootName);

                var playerCardBackgroundName = playerCardRootName + "Background";
                var playerCardBackgroundGameObject = GameObject.Find(playerCardBackgroundName);
                if (playerCardBackgroundGameObject == null)
                    Debug.LogError("Can't find PlayerCardBackgroundGameObject " + playerCardBackgroundName);
                else
                    m_PlayerCardBackgrounds[player, card] = playerCardBackgroundGameObject.GetComponent<Image>();

                var playerCardValueName = playerCardBackgroundName + "/Value";
                var playerCardValueGameObject = GameObject.Find(playerCardValueName);
                if (playerCardValueGameObject == null)
                    Debug.LogError("Can't find PlayerCardValueGameObject " + playerCardValueName);
                else
                    m_PlayerCardValues[player, card] = playerCardValueGameObject.GetComponent<Text>();
                if (m_PlayerCardValues[player, card] == null)
                    Debug.LogError("Can't find Player " + (Player)player + " Card[" + cardIndex + " Value Text " + playerCardValueName);
            }

            var playerCubesBackgroundRootName = playerUIRootName + "CubesBackground/";
            for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType)
            {
                var cubeCountText = playerCubesBackgroundRootName + (CupCardCubeColour)cubeType;
                var cubeCountGameObject = GameObject.Find(cubeCountText);
                if (cubeCountGameObject == null)
                    Debug.LogError("Can't find " + (CupCardCubeColour)cubeType + " cube count GameObject " + cubeCountText);
                m_PlayerCubeCountsTexts[player, cubeType] = cubeCountGameObject.GetComponent<Text>();
                if (m_PlayerCubeCountsTexts[player, cubeType] == null)
                    Debug.LogError("Can't find " + (CupCardCubeColour)cubeType + " cube count UI Text Component");
            }

            var wildcardCubeCountText = playerCubesBackgroundRootName + "White";
            var wildcardCubeCountGameObject = GameObject.Find(wildcardCubeCountText);
            if (wildcardCubeCountGameObject == null)
                Debug.LogError("Can't find wildcard cube count GameObject " + wildcardCubeCountText);

            m_PlayerWildcardCubeCountTexts[player] = wildcardCubeCountGameObject.GetComponent<Text>();
            if (m_PlayerWildcardCubeCountTexts[player] == null)
                Debug.LogError("Can't find wildcard cube count UI Text Component");
        }

        var robotButtonName = gameBoardUIRootName + "LeftPlayer/Robot";
        var robotButtonGameObject = GameObject.Find(robotButtonName);
        if (robotButtonGameObject == null)
            Debug.LogError("Can't find Robot Button GameObject " + robotButtonName);
        m_RobotButton = robotButtonGameObject.GetComponent<Button>();
        if (m_RobotButton == null)
            Debug.LogError("Can't find Robot Button Component");
        var robotButtonTextName = gameBoardUIRootName + "LeftPlayer/Robot/Text";
        var robotButtonTextGameObject = GameObject.Find(robotButtonTextName);
        if (robotButtonTextGameObject == null)
            Debug.LogError("Can't find Robot button Text GameObject " + robotButtonTextName);
        m_RobotButtonText = robotButtonTextGameObject.GetComponent<Text>();
        if (m_RobotButtonText == null)
            Debug.LogError("Can't find Robot button Text Component");

        if (!Validate())
            Debug.LogError("Validation failed!");
    }

    bool Validate()
    {
        if (StatusText == null)
        {
            Debug.LogError("StatusText is null");
            return false;
        }

        for (var cupIndex = 0; cupIndex < GameLogic.CubeTypeCount; cupIndex++)
        {
            if (m_UnclaimedCupGOs[cupIndex] == null)
            {
                Debug.LogError("m_unclaimedCupGOs[" + cupIndex + "] is null");
                return false;
            }

            if (m_UnclaimedCupButtons == null)
            {
                Debug.LogError("m_unclaimedCupButtons[" + cupIndex + "] is null");
                return false;
            }
        }

        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; playerIndex++)
        {
            if (m_PlayerWildcardCubeCountTexts[playerIndex] == null)
            {
                Debug.LogError("m_playerWildcardCubeCountTexts[" + playerIndex + "] is null");
                return false;
            }

            if (m_PlayerHandGOs[playerIndex] == null)
            {
                Debug.LogError("m_playerHandGOs[" + playerIndex + "] is null");
                return false;
            }

            if (m_PlayerGenericButtons[playerIndex] == null)
            {
                Debug.LogError("m_playerGenericButtons[" + playerIndex + "] is null");
                return false;
            }

            for (var cubeIndex = 0; cubeIndex < GameLogic.CubeTypeCount; cubeIndex++)
                if (m_PlayerCubeCountsTexts[playerIndex, cubeIndex] == null)
                {
                    Debug.LogError("m_playerCubeCountsTexts[" + playerIndex + "," + cubeIndex + "] is null");
                    return false;
                }

            for (var card = 0; card < GameLogic.HandSize; ++card)
            {
                if (m_PlayerCardOutlines[playerIndex, card] == null)
                {
                    Debug.LogError("m_playerCardOutlines[" + playerIndex + "," + card + "] is null");
                    return false;
                }

                if (m_PlayerCardBackgrounds[playerIndex, card] == null)
                {
                    Debug.LogError("m_playerCardBackgrounds[" + playerIndex + "," + card + "] is null");
                    return false;
                }

                if (m_PlayerCardValues[playerIndex, card] == null)
                {
                    Debug.LogError("m_playerCardValues[" + playerIndex + "," + card + "] is null");
                    return false;
                }
            }

            for (var cupIndex = 0; cupIndex < GameLogic.MaxCupsPerPlayer; ++cupIndex)
            {
                if (m_PlayerCupGOs[playerIndex, cupIndex] == null)
                {
                    Debug.LogError("m_playerCupGOs[" + playerIndex + "," + cupIndex + "] is null");
                    return false;
                }

                if (m_PlayerCupImages[playerIndex, cupIndex] == null)
                {
                    Debug.LogError("m_playerCupImages[" + playerIndex + "," + cupIndex + "] is null");
                    return false;
                }

                if (m_PlayerCupValues[playerIndex, cupIndex] == null)
                {
                    Debug.LogError("m_playerCupValues[" + playerIndex + "," + cupIndex + "] is null");
                    return false;
                }
            }
        }

        if (m_RobotButton == null)
            Debug.LogError("m_robotButton is null");
        if (m_RobotButtonText == null)
            Debug.LogError("m_robotButtonText is null");

        return true;
    }
}
