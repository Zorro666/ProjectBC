using System;
using BC;
using UnityEngine;

public class Race : MonoBehaviour
{
    GameLogic m_GameLogic;
    RaceLogic m_RaceLogic;
    RaceUI m_RaceUI;

    public int NumberOfCubes => m_RaceLogic.NumberOfCubes;

    public RaceState State => m_RaceLogic.State;

    void Awake()
    {
        m_RaceLogic = new RaceLogic();
    }

    public void Initialise(GameLogic gameLogic)
    {
        m_GameLogic = gameLogic;
        m_RaceUI = GetComponent<RaceUI>();
        if (m_RaceUI == null)
        {
            Debug.LogError("m_raceUI is NULL");
            return;
        }
        m_RaceUI.Initialise(gameLogic);
        m_RaceLogic.Initialise(m_RaceUI.NumberOfCubes, m_RaceUI,
            m_GameLogic.GetCubesRemainingCount,
            m_GameLogic.NextCube,
            m_GameLogic.AddCubeToPlayer,
            m_GameLogic.DiscardCard,
            m_GameLogic.FinishRace);
    }

    public void ResetPlayCardButtons()
    {
        m_RaceLogic.ResetPlayCardButtons();
    }

    public void SetPlayCardButtons(Card card)
    {
        m_RaceLogic.SetPlayCardButtons(card);
    }

    public void StartRace()
    {
        m_RaceLogic.StartRace();
    }

    public bool CanPlayCard(Card card)
    {
        return m_RaceLogic.CanPlayCard(card);
    }

    public void NewGame()
    {
        m_RaceLogic.NewGame();
    }

    public bool PlayCard(Player player, Card card, Player currentPlayer)
    {
        return m_RaceLogic.PlayCard(player, card, currentPlayer);
    }

    public CupCardCubeColour GetCube(int i)
    {
        return m_RaceLogic.GetCube(i);
    }

    public Card GetPlayedCard(Player side, int i)
    {
        return m_RaceLogic.GetPlayedCard(side, i);
    }
}
