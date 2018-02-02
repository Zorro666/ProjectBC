using UnityEngine;
using UnityEngine.UI;
using BC;

public class GameUI : MonoBehaviour
{
    public Text StatusText;

    GameObject [] m_unclaimedCupGOs;
    Button [] m_unclaimedCupButtons;
    Button m_robotButton;
    Text m_robotButtonText;

    //TODO: make a Player class and store this data per Player
    Text [,] m_playerCubeCountsTexts;
    Text [] m_playerWildcardCubeCountTexts;
    GameObject [] m_playerHandGOs;
    Image [,] m_playerCardOutlines;
    Image [,] m_playerCardBackgrounds;
    Text [,] m_playerCardValues;
    GameObject [,] m_playerCupGOs;
    Image [,] m_playerCupImages;
    Text [,] m_playerCupValues;
    GameObject [] m_playerGenericButtons;

    public void SetRobotButtonText (string text)
    {
        m_robotButtonText.text = text;
    }

    public void SetStatusText (string text)
    {
        StatusText.text = text;
    }

    public void SetCupStatus (int cupIndex, bool available)
    {
        m_unclaimedCupGOs [cupIndex].SetActive (available);
    }

    public void SetCupInteractible (int cupIndex, bool interactible)
    {
        m_unclaimedCupButtons [cupIndex].interactable = interactible;
    }

    //TODO: this should be in logic not UI
    public bool IsCupInteractible (int cupIndex)
    {
        return m_unclaimedCupButtons [cupIndex].interactable;
    }

    public void SetPlayerCubeCountColour (int playerIndex, int cubeIndex, Color colour)
    {
        m_playerCubeCountsTexts [playerIndex, cubeIndex].color = colour;
    }

    public void SetPlayerCubeCountValue (int playerIndex, int cubeIndex, int cubeValue)
    {
        m_playerCubeCountsTexts [playerIndex, cubeIndex].text = cubeValue.ToString ();
    }

    public void SetPlayerWildcardCubeCountValue (int playerIndex, int cubeValue)
    {
        m_playerWildcardCubeCountTexts [playerIndex].text = cubeValue.ToString ();
    }

    public void SetPlayerHandActive (int playerIndex, bool active)
    {
        m_playerHandGOs [playerIndex].SetActive (active);
    }

    public void SetPlayerCardHighlighted (int playerIndex, int cardIndex, bool highlighted)
    {
        m_playerCardOutlines [playerIndex, cardIndex].color = highlighted ? Color.white : Color.black;
    }

    public void SetPlayerCard (int playerIndex, int cardIndex, Card card, Color colour)
    {
        string text = card.Value.ToString ();
        m_playerCardValues [playerIndex, cardIndex].text = text;
        var textColour = (card.Colour == CupCardCubeColour.Yellow) ? Color.black : Color.white;
        m_playerCardValues [playerIndex, cardIndex].color = textColour;
        m_playerCardBackgrounds [playerIndex, cardIndex].color = colour;
    }

    public void SetPlayerCup (int playerIndex, int cupDisplayIndex, CupCardCubeColour cupType, int value, Color colour)
    {
        m_playerCupImages [playerIndex, cupDisplayIndex].color = colour;
        m_playerCupValues [playerIndex, cupDisplayIndex].text = value.ToString ();
        var textColour = (cupType == CupCardCubeColour.Yellow) ? Color.black : Color.white;
        m_playerCupValues [playerIndex, cupDisplayIndex].color = textColour;
    }

    public void SetPlayerCupActive (int playerIndex, int cupIndex, bool active)
    {
        m_playerCupGOs [playerIndex, cupIndex].SetActive (active);
    }

    public void ShowPlayerGenericButton (int playerIndex)
    {
        m_playerGenericButtons [playerIndex].SetActive (true);
    }

    public void HidePlayerGenericButtons ()
    {
        for (var p = 0; p < GameLogic.PlayerCount; ++p)
            m_playerGenericButtons [p].SetActive (false);
    }

    public void SetPlayerGenericButtonText (int playerIndex, string text)
    {
        m_playerGenericButtons [playerIndex].GetComponentInChildren<Text> ().text = text;
    }

    void Awake ()
    {
        m_unclaimedCupGOs = new GameObject [GameLogic.CubeTypeCount];
        m_unclaimedCupButtons = new Button [GameLogic.CubeTypeCount];
        m_playerCubeCountsTexts = new Text [GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_playerWildcardCubeCountTexts = new Text [GameLogic.PlayerCount];
        m_playerHandGOs = new GameObject [GameLogic.PlayerCount];
        m_playerCardOutlines = new Image [GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCardBackgrounds = new Image [GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCardValues = new Text [GameLogic.PlayerCount, GameLogic.HandSize];
        m_playerCupGOs = new GameObject [GameLogic.PlayerCount, GameLogic.MaxCupsPerPlayer];
        m_playerCupImages = new Image [GameLogic.PlayerCount, GameLogic.MaxCupsPerPlayer];
        m_playerCupValues = new Text [GameLogic.PlayerCount, GameLogic.MaxCupsPerPlayer];
        m_playerGenericButtons = new GameObject [GameLogic.PlayerCount];
    }

    public void Initialise ()
    {
        var gameBoardUIRootName = "/GameBoard/UI/";
        var unclaimedCupRootName = gameBoardUIRootName + "CupsBackground/";
        for (var cupIndex = 0; cupIndex < GameLogic.CubeTypeCount; ++cupIndex) {
            var unclaimedCupName = unclaimedCupRootName + (CupCardCubeColour)cupIndex;
            m_unclaimedCupGOs [cupIndex] = GameObject.Find (unclaimedCupName);
            if (m_unclaimedCupGOs [cupIndex] == null)
                Debug.LogError ("Can't find cup " + (CupCardCubeColour)cupIndex + " " + unclaimedCupName);
            m_unclaimedCupButtons [cupIndex] = m_unclaimedCupGOs [cupIndex].GetComponent<Button> ();
            if (m_unclaimedCupButtons [cupIndex] == null)
                Debug.LogError ("Can't find cup " + (CupCardCubeColour)cupIndex + " Button Component");
        }

        for (var player = 0; player < GameLogic.PlayerCount; ++player) {
            var playerUIRootName = gameBoardUIRootName + (Player)player + "Player/";
            var playerHandRootName = playerUIRootName + "Hand/";
            m_playerHandGOs [player] = GameObject.Find (playerHandRootName);
            if (m_playerHandGOs [player] == null)
                Debug.LogError ("Can't find Player Hand UI GameObject " + (Player)player + " " + playerHandRootName);

            var playerGenericButtonName = playerUIRootName + "GenericButton";
            m_playerGenericButtons [player] = GameObject.Find (playerGenericButtonName);
            if (m_playerGenericButtons [player] == null)
                Debug.LogError ("Can't find GenericButton for Player " + (Player)player + " " + playerGenericButtonName);

            var playerCupsRootName = playerUIRootName + "CupsBackground/";
            for (var cupIndex = 0; cupIndex < GameLogic.MaxCupsPerPlayer; ++cupIndex) {
                var playerCupIndex = (cupIndex + 1);
                var cupImageName = playerCupsRootName + "Cup" + playerCupIndex;
                m_playerCupGOs [player, cupIndex] = GameObject.Find (cupImageName);
                if (m_playerCupGOs [player, cupIndex] == null)
                    Debug.LogError ("Can't find Cup " + playerCupIndex + " Image GameObject " + cupImageName);

                m_playerCupImages [player, cupIndex] = m_playerCupGOs [player, cupIndex].GetComponent<Image> ();
                if (m_playerCupImages [player, cupIndex] == null)
                    Debug.LogError ("Can't find Cup " + playerCupIndex + " Image Component");

                var cupValueName = cupImageName + "/Value";
                var cupValueGO = GameObject.Find (cupValueName);
                if (cupValueGO == null)
                    Debug.LogError ("Can't find Cup " + playerCupIndex + " Value GameObject " + cupValueName);
                m_playerCupValues [player, cupIndex] = cupValueGO.GetComponent<Text> ();
                if (m_playerCupValues [player, cupIndex] == null)
                    Debug.LogError ("Can't find Cup " + playerCupIndex + " Value Component");
            }

            for (var card = 0; card < GameLogic.HandSize; ++card) {
                var cardIndex = card + 1;
                var playerCardRootName = playerHandRootName + "Card" + cardIndex.ToString () + "/";

                var playerCardOutlineGO = GameObject.Find (playerCardRootName);
                if (playerCardOutlineGO == null)
                    Debug.LogError ("Can't find PlayerCardOutlineGO " + playerCardRootName);
                m_playerCardOutlines [player, card] = playerCardOutlineGO.GetComponent<Image> ();
                if (m_playerCardOutlines [player, card] == null)
                    Debug.LogError ("Can't find Player " + (Player)player + " Card[" + cardIndex + " Outline Image " + playerCardRootName);

                var playerCardBackgroundName = playerCardRootName + "Background";
                var playerCardBackgroundGO = GameObject.Find (playerCardBackgroundName);
                if (playerCardBackgroundGO == null)
                    Debug.LogError ("Can't find PlayerCardBackgroundGO " + playerCardBackgroundName);
                m_playerCardBackgrounds [player, card] = playerCardBackgroundGO.GetComponent<Image> ();

                var playerCardValueName = playerCardBackgroundName + "/Value";
                var playerCardValueGO = GameObject.Find (playerCardValueName);
                if (playerCardValueGO == null)
                    Debug.LogError ("Can't find PlayerCardValueGO " + playerCardValueName);
                m_playerCardValues [player, card] = playerCardValueGO.GetComponent<Text> ();
                if (m_playerCardValues [player, card] == null)
                    Debug.LogError ("Can't find Player " + (Player)player + " Card[" + cardIndex + " Value Text " + playerCardValueName);
            }

            var playerCubesBackgroundRootName = playerUIRootName + "CubesBackground/";
            for (var cubeType = 0; cubeType < GameLogic.CubeTypeCount; ++cubeType) {
                var cubeCountText = playerCubesBackgroundRootName + (CupCardCubeColour)cubeType;
                var cubeCountGO = GameObject.Find (cubeCountText);
                if (cubeCountGO == null)
                    Debug.LogError ("Can't find " + (CupCardCubeColour)cubeType + " cube count GameObject " + cubeCountText);
                m_playerCubeCountsTexts [player, cubeType] = cubeCountGO.GetComponent<Text> ();
                if (m_playerCubeCountsTexts [player, cubeType] == null)
                    Debug.LogError ("Can't find " + (CupCardCubeColour)cubeType + " cube count UI Text Component");
            }
            var wildcardCubeCountText = playerCubesBackgroundRootName + "White";
            var wildcardCubeCountGO = GameObject.Find (wildcardCubeCountText);
            if (wildcardCubeCountGO == null)
                Debug.LogError ("Can't find wildcard cube count GameObject " + wildcardCubeCountText);

            m_playerWildcardCubeCountTexts [player] = wildcardCubeCountGO.GetComponent<Text> ();
            if (m_playerWildcardCubeCountTexts [player] == null)
                Debug.LogError ("Can't find wildcard cube count UI Text Component");
        }
        var robotButtonName = gameBoardUIRootName + "LeftPlayer/Robot";
        var robotButtonGO = GameObject.Find (robotButtonName);
        if (robotButtonGO == null)
            Debug.LogError ("Can't find Robot Button GO " + robotButtonName);
        m_robotButton = robotButtonGO.GetComponent<Button> ();
        if (m_robotButton == null)
            Debug.LogError ("Can't find Robot Button Component");
        var robotButtonTextName = gameBoardUIRootName + "LeftPlayer/Robot/Text";
        var robotButtonTextGO = GameObject.Find (robotButtonTextName);
        if (robotButtonTextGO == null)
            Debug.LogError ("Can't find Robot button Text GO " + robotButtonTextName);
        m_robotButtonText = robotButtonTextGO.GetComponent<Text> ();
        if (m_robotButtonText == null)
            Debug.LogError ("Can't find Robot button Text Component");

        if (!Validate ())
            Debug.LogError ("Validation failed!");
    }

    bool Validate ()
    {
        if (StatusText == null) {
            Debug.LogError ("StatusText is null");
            return false;
        }

        for (int cupIndex = 0; cupIndex < GameLogic.CubeTypeCount; cupIndex++) {
            if (m_unclaimedCupGOs [cupIndex] == null) {
                Debug.LogError ("m_unclaimedCupGOs[" + cupIndex + "] is null");
                return false;
            }
            if (m_unclaimedCupButtons == null) {
                Debug.LogError ("m_unclaimedCupButtons[" + cupIndex + "] is null");
                return false;
            }
        }
        for (int playerIndex = 0; playerIndex < GameLogic.PlayerCount; playerIndex++) {
            if (m_playerWildcardCubeCountTexts [playerIndex] == null) {
                Debug.LogError ("m_playerWildcardCubeCountTexts[" + playerIndex + "] is null");
                return false;
            }
            if (m_playerHandGOs [playerIndex] == null) {
                Debug.LogError ("m_playerHandGOs[" + playerIndex + "] is null");
                return false;
            }
            if (m_playerGenericButtons [playerIndex] == null) {
                Debug.LogError ("m_playerGenericButtons[" + playerIndex + "] is null");
                return false;
            }

            for (int cubeIndex = 0; cubeIndex < GameLogic.CubeTypeCount; cubeIndex++) {
                if (m_playerCubeCountsTexts [playerIndex, cubeIndex] == null) {
                    Debug.LogError ("m_playerCubeCountsTexts[" + playerIndex + "," + cubeIndex + "] is null");
                    return false;
                }
            }
            for (var card = 0; card < GameLogic.HandSize; ++card) {
                if (m_playerCardOutlines [playerIndex, card] == null) {
                    Debug.LogError ("m_playerCardOutlines[" + playerIndex + "," + card + "] is null");
                    return false;
                }
                if (m_playerCardBackgrounds [playerIndex, card] == null) {
                    Debug.LogError ("m_playerCardBackgrounds[" + playerIndex + "," + card + "] is null");
                    return false;
                }
                if (m_playerCardValues [playerIndex, card] == null) {
                    Debug.LogError ("m_playerCardValues[" + playerIndex + "," + card + "] is null");
                    return false;
                }
            }
            for (var cupIndex = 0; cupIndex < GameLogic.MaxCupsPerPlayer; ++cupIndex) {
                if (m_playerCupGOs [playerIndex, cupIndex] == null) {
                    Debug.LogError ("m_playerCupGOs[" + playerIndex + "," + cupIndex + "] is null");
                    return false;
                }
                if (m_playerCupImages [playerIndex, cupIndex] == null) {
                    Debug.LogError ("m_playerCupImages[" + playerIndex + "," + cupIndex + "] is null");
                    return false;
                }
                if (m_playerCupValues [playerIndex, cupIndex] == null) {
                    Debug.LogError ("m_playerCupValues[" + playerIndex + "," + cupIndex + "] is null");
                    return false;
                }
            }
        }
        if (m_robotButton == null)
            Debug.LogError ("m_robotButton is null");
        if (m_robotButtonText == null)
            Debug.LogError ("m_robotButtonText is null");

        return true;
    }
}
