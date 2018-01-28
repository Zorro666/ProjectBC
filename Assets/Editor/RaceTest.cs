using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class RaceTest {

	[Test]
	public void RaceTestSimplePasses() 
    {
		// Use the Assert class to test conditions.
	}
    // Test
    //BC.Player Race.ComputeWinner()
    //Need to setup m_cards
    //public bool PlayCard(BC.Player player, Card card, BC.Player currentPlayer)
    // Need to setup m_cardsPlayeda & m_cardsRemaining : to test return false cases


	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator RaceTestWithEnumeratorPasses() 
    {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}
}
