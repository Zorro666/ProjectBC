using UnityEngine;
using BC;

public class RaceLogic
{
    public delegate int CubesRemainingInBagDelegate();
    public delegate CupCardCubeColour NextCubeDelegate();
    public delegate void AddCubeToPlayerDelegate(Player winner, CupCardCubeColour cube);
    public delegate void DiscardCardDelegate(Card card);
    public delegate void FinishRaceDelegate(Player winner, RaceLogic race);

    NextCubeDelegate m_NextCube;
    CubesRemainingInBagDelegate m_CubesRemainingInBag;
    AddCubeToPlayerDelegate m_AddCubeToPlayer;
    DiscardCardDelegate m_DiscardCard;
    FinishRaceDelegate m_FinishRace;

    public RaceState State { get; private set; }
    Card[,] m_cards;
    int[] m_cardsPlayed;
    int[,] m_cardsRemaining;
    Player m_winner;
    RaceUI m_raceUI;

    public int NumberOfCubes { get; private set; }
    public Player Winner
    {
        get { return m_winner; }
    }

    public bool CanPlayCard(Card card)
    {
        var cardColour = (int)card.Colour;
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            if (m_cardsRemaining[playerIndex, cardColour] > 0)
                return true;
        }
        return false;
    }

    public void ResetPlayCardButtons()
    {
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            var cardIndex = m_cardsPlayed[playerIndex];
            if (cardIndex < NumberOfCubes)
            {
                if (m_raceUI)
                {
                    m_raceUI.SetPlayCardButtons(playerIndex, cardIndex, false);
                }
            }
        }
    }

    public void SetPlayCardButtons(Card card)
    {
        var cardColour = (int)card.Colour;
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            var cardIndex = m_cardsPlayed[playerIndex];
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
                if (m_raceUI)
                {
                    m_raceUI.SetPlayedCardToCard(playerIndex, cardIndex, card);
                    m_raceUI.SetPlayCardButtons(playerIndex, cardIndex, active);
                }
            }
        }
    }

    public void StartRace()
    {
        for (var i = 0; i < m_cardsPlayed.Length; ++i)
            m_cardsPlayed[i] = 0;

        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
            {
                m_cards[playerIndex, cardIndex] = null;
                if (m_raceUI)
                {
                    m_raceUI.SetPlayCardButtons(playerIndex, cardIndex, false);
                }
            }
            for (var colour = 0; colour < GameLogic.CubeTypeCount; ++colour)
                m_cardsRemaining[playerIndex, colour] = 0;
        }

        if (m_CubesRemainingInBag() < NumberOfCubes)
            State = RaceState.Finished;

        if (State == RaceState.Finished)
        {
            if (m_raceUI)
            {
                m_raceUI.SetFinished();
            }
            return;
        }

        for (var i = 0; i < NumberOfCubes; i++)
        {
            var cubeColour = m_NextCube();
            var colour = (int)cubeColour;
            if (m_raceUI)
            {
                m_raceUI.SetCube(i, cubeColour);
            }
            for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
                ++m_cardsRemaining[playerIndex, colour];
        }
        if (m_raceUI)
        {
            m_raceUI.StartRace(State);
        }
        m_winner = Player.Unknown;
    }

    string Name { get { return "Race" + NumberOfCubes.ToString(); } }

    Player ComputeWinner(Player currentPlayer)
    {
        Player maxScorePlayer = Player.Unknown;
        var maxScoreValue = -1;
        Player minScorePlayer = Player.Unknown;
        var minScoreValue = 9999;
        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            var score = 0;
            Player player = (Player)playerIndex;
            for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
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
        if (State == RaceState.Lowest)
            return minScorePlayer;
        if (State == RaceState.Highest)
            return maxScorePlayer;
        Debug.LogError("m_state is not Lowest or Highest");
        return Player.Unknown;
    }

    public void Initialise(int numberOfCubes, RaceUI raceUI, 
                           CubesRemainingInBagDelegate CubesRemainingInBag,
                           NextCubeDelegate NextCube,
                           AddCubeToPlayerDelegate AddCubeToPlayer,
                           DiscardCardDelegate DiscardCard,
                           FinishRaceDelegate FinishRace)
    {
        m_CubesRemainingInBag = CubesRemainingInBag;
        m_NextCube = NextCube;
        m_AddCubeToPlayer = AddCubeToPlayer;
        m_DiscardCard = DiscardCard;
        m_FinishRace = FinishRace;

        m_raceUI = raceUI;
        NumberOfCubes = numberOfCubes;
        State = RaceState.Finished;
        m_cards = new Card[GameLogic.PlayerCount, NumberOfCubes];
        m_cardsPlayed = new int[GameLogic.PlayerCount];
        m_cardsRemaining = new int[GameLogic.PlayerCount, GameLogic.CubeTypeCount];
        m_winner = Player.Unknown;
    }

    public void NewGame()
    {
        if ((NumberOfCubes % 2) == 1)
            State = RaceState.Lowest;
        else
            State = RaceState.Highest;
        StartRace();
    }

    public void FinishRace(Player currentPlayer)
    {
        m_winner = ComputeWinner(currentPlayer);
        Debug.Log(State + " Player " + m_winner + " won");
        for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
        {
            var playerIndex = (int)m_winner;
            Card card = m_cards[playerIndex, cardIndex];
            m_AddCubeToPlayer(m_winner, card.Colour);
        }

        for (var playerIndex = 0; playerIndex < GameLogic.PlayerCount; ++playerIndex)
        {
            for (var cardIndex = 0; cardIndex < NumberOfCubes; ++cardIndex)
                m_DiscardCard(m_cards[playerIndex, cardIndex]);
        }

        if (State == RaceState.Lowest)
            State = RaceState.Highest;
        else if (State == RaceState.Highest)
            State = RaceState.Lowest;

        m_FinishRace(m_winner, this);
    }

    public bool PlayCard(Player player, Card card, Player currentPlayer)
    {
        //Debug.Log(Name + " PlayCard " + player + " " + card.Colour + " " + card.Value);
        if (State == RaceState.Finished)
        {
            Debug.Log(Name + " race is Finished");
            return false;
        }
        var playerIndex = (int)player;
        var cardIndex = m_cardsPlayed[playerIndex];
        if (cardIndex == NumberOfCubes)
        {
            Debug.Log(player + " player is full");
            return false;
        }
        var cardColour = (int)card.Colour;
        if (m_cardsRemaining[playerIndex, cardColour] == 0)
        {
            Debug.Log(player + " player " + card.Colour + " can't be played");
            return false;
        }
        --m_cardsRemaining[playerIndex, cardColour];
        ++m_cardsPlayed[playerIndex];
        if (m_raceUI)
        {
            m_raceUI.SetPlayedCardToCard(playerIndex, cardIndex, card);
            m_raceUI.SetPlayCardButtons(playerIndex, cardIndex, true);
            m_raceUI.SetPlayCardButtonInteractable(playerIndex, cardIndex, false);
        }
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
