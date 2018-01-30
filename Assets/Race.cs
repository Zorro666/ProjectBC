using UnityEngine;
using BC;

public class Race : MonoBehaviour 
{
    RaceLogic m_raceLogic;
    RaceUI m_raceUI;
    GameLogic m_gameLogic;

    public Player Winner
    {
        get { return m_raceLogic.Winner; }
    }

    void Awake()
    {
        m_raceLogic = new RaceLogic();
    }

    void Start() 
    {
    }

    void Update()
    {
    }

    public bool CanPlayCard(Card card)
    {
        return m_raceLogic.CanPlayCard(card);
    }

    public void ResetPlayCardButtons()
    {
        m_raceLogic.ResetPlayCardButtons();
    }

    public void SetPlayCardButtons(Card card)
    {
        m_raceLogic.SetPlayCardButtons(card);
    }

    public void StartRace()
    {
        m_raceLogic.StartRace();
    }

	public void Initialise(GameLogic gameLogic) 
    {
        m_gameLogic = gameLogic;
        m_raceUI = GetComponent<RaceUI>();
        if (m_raceUI == null)
            Debug.LogError("m_raceUI is NULL");
        m_raceUI.Initialise(gameLogic);
        m_raceLogic.Initialise(m_raceUI.NumberOfCubes, m_raceUI,
                               m_gameLogic.GetCubesRemainingCount,
                               m_gameLogic.NextCube,
                               m_gameLogic.AddCubeToPlayer,
                               m_gameLogic.DiscardCard,
                               m_gameLogic.FinishRace);
    }

    public void NewGame() 
    {
        m_raceLogic.NewGame();
    }

    public void FinishRace(Player currentPlayer)
    {
        m_raceLogic.FinishRace(currentPlayer);
    }

    public bool PlayCard(Player player, Card card, Player currentPlayer)
    {
        return m_raceLogic.PlayCard(player, card, currentPlayer);
    }
}