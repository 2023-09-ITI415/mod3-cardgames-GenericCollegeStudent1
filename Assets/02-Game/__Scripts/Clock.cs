using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Clock : MonoBehaviour
{

    static public Clock S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
    public float reloadDelay = 2f;// 2 sec delay between rounds
    public Text gameOverText, roundResultText, highScoreText;




    [Header("Set Dynamically")]
    public Deck deck;
    public Layout layout;
    public List<CardClock> drawPile;
    public Transform layoutAnchor;
    public CardClock target;
    public List<CardClock> tableau;
    public List<CardClock> discardPile;
    public FloatingScore fsRun;


    void Awake()
    {
        S = this;

    }


    void Start()
    {

        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards); //this suffles the deck by reference//a
                                      //Card c;
                                      //for(int cNum = 0; cNum < deck.cards.Count; cNum++)
                                      // {
                                      //c = deck.cards[cNum];
                                      //c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
                                      // }
        layout = GetComponent<Layout>(); //get the layout component
        layout.ReadLayout(layoutXML.text); //pass LaoutXML to it

        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();
    }

    List<CardClock> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardClock> lCP = new List<CardClock>();
        CardClock tCP;
        foreach (Card tCD in lCD)
        {
            tCP = tCD as CardClock; // a
            lCP.Add(tCP);
        }
        return (lCP);
    }

    // The Draw function will pull a single card from the drawPile and return it
    CardClock Draw()
    {
        CardClock cd = drawPile[0]; // Pull the 0th CardClock
        drawPile.RemoveAt(0); // Then remove it from List<> drawPile
        return (cd); // And return it
    }
    // LayoutGame() positions the initial tableau of cards, a.k.a. the "mine"
    void LayoutGame()
    {
        // Create an empty GameObject to serve as an anchor for the tableau // a
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            // ^ Create an empty GameObject named _LayoutAnchor in the Hierarchy
            layoutAnchor = tGO.transform; // Grab its Transform
            layoutAnchor.transform.position = layoutCenter; // Position it
        }
        CardClock cp;
        // Follow the layout
        foreach (SlotDef tSD in layout.slotDefs)
        {
            // ^ Iterate through all the SlotDefs in the layout.slotDefs as tSD
            cp = Draw(); // Pull a card from the top (beginning) of the draw Pile
            cp.faceUp = tSD.faceUp; // Set its faceUp to the value in SlotDef
            cp.transform.parent = layoutAnchor; // Make its parent layoutAnchor
                                                // This replaces the previous parent: deck.deckAnchor, which
                                                // appears as _Deck in the Hierarchy when the scene is playing.

            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);
            // ^ Set the localPosition of the card based on slotDef
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            // CardProspectors in the tableau have the state CardState.tableau
            cp.state = CardState.tableau;
            //CardProspectors in the tableau have the state cardstate.tableau
            cp.SetSortingLayerName(tSD.layerName); //set the sorting layers
            tableau.Add(cp); // Add this CardClock to the List<> tableau

            // Set which cards are hiding others
            foreach (CardClock tCP in tableau)
            {
                foreach (int hid in tCP.slotDef.hiddenBy)
                {
                    cp = FindCardByLayoutID(hid);
                    tCP.hiddenBy.Add(cp);
                }
            }
        }

        // Set up the initial target card
        MoveToTarget(Draw());
        // Set up the Draw pile
        UpdateDrawPile();
    }

    // Convert from the layoutID int to the CardClock with that ID
    CardClock FindCardByLayoutID(int layoutID)
    {
        foreach (CardClock tCP in tableau)
        {
            // Search through all cards in the tableau List<>
            if (tCP.layoutID == layoutID)
            {
                // If the card has the same ID, return it
                return (tCP);
            }
        }
        // If it's not found, return null
        return (null);
    }
    // This turns cards in the Mine face-up or face-down
    void SetTableauFaces()
    {
        foreach (CardClock cd in tableau)
        {
            bool faceUp = true; // Assume the card will be face-up
            foreach (CardClock cover in cd.hiddenBy)
            {
                if (cover != null) // Check if cover is not null
                {
                    // If either of the covering cards are in the tableau
                    if (cover.state == CardState.tableau)
                    {
                        faceUp = false; // then this card is face-down
                    }
                }
            }
            cd.faceUp = faceUp; // Set the value on the card
        }
    }

    // Moves the current target to the discardPile
    void MoveToDiscard(CardClock cd)
    {
        // Set the state of the card to discard
        cd.state = CardState.discard;
        discardPile.Add(cd); // Add it to the discardPile List<>
        cd.transform.parent = layoutAnchor; // Update its transform parent
                                            // Position this card on the discardPile
        cd.transform.localPosition = new Vector3(
        layout.multiplier.x * layout.discardPile.x,
        layout.multiplier.y * layout.discardPile.y,
        -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        // Place it on top of the pile for depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }
    // Make cd the new target card
    void MoveToTarget(CardClock cd)
    {
        // If there is currently a target card, move it to discardPile
        if (target != null) MoveToDiscard(target);
        target = cd; // cd is the new target
        cd.state = CardState.target;
        cd.transform.parent = layoutAnchor;
        // Move to the target position
        cd.transform.localPosition = new Vector3(
        layout.multiplier.x * layout.discardPile.x,
        layout.multiplier.y * layout.discardPile.y,
        -layout.discardPile.layerID);
        cd.faceUp = true; // Make it face-up
                          // Set the depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }
    // Arranges all the cards of the drawPile to show how many are left
    void UpdateDrawPile()
    {
        CardClock cd;
        // Go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;
            // Position it correctly with the layout.drawPile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(
            layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
            layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
            -layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false; // Make them all face-down
            cd.state = CardState.drawpile;
            // Set depth sorting
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    // CardClicked is called any time a card in the game is clicked
    public void CardClicked(CardClock cd)
    {
        // The reaction is determined by the state of the clicked card
        switch (cd.state)
        {
            case CardState.target:
                // Clicking the target card does nothing
                break;
            case CardState.drawpile:
                // Clicking any card in the drawPile will draw the next card
                MoveToDiscard(target); // Moves the target to the discardPile
                MoveToTarget(Draw()); // Moves the next drawn card to the target
                UpdateDrawPile(); // Restacks the drawPile
                ScoreManager.EVENT(eScoreEvent.draw);



                break;
            case CardState.tableau:
                // Clicking a card in the tableau will check if it's a valid play

                bool validMatch = true;
                if (!cd.faceUp)
                {
                    // If the card is face-down, it's not valid
                    validMatch = false;
                }
                if (!AdjacentRank(cd, target))
                {
                    // If it's not an adjacent rank, it's not valid
                    validMatch = false;
                }
                if (!validMatch) return; // return if not valid
                                         // If we got here, then: Yay! It's a valid card.
                tableau.Remove(cd); // Remove it from the tableau List
                MoveToTarget(cd); // Make it the target card

                SetTableauFaces(); //update tableau card face-ups
                ScoreManager.EVENT(eScoreEvent.mine);


                break;
        }

        // After the card is clicked, check if the game is over
        CheckForGameOver();
    }

    // Check to see whether the game is over or not
    void CheckForGameOver()
    {
        // If the tableau is empty, the game is over
        if (tableau.Count == 0)
        {
            // Call GameOver() with a win
            GameOver(true);
        }
        else if (drawPile.Count == 0)
        {
            // Check for remaining valid plays
            foreach (CardClock cd in tableau)
            {
                if (AdjacentRank(cd, target))
                {
                    // If there is a valid play, the game's not over
                    return;
                }
            }
            // Since there are no valid plays, the game is over
            // Call GameOver with a loss
            GameOver(false);
        }
    }

    // Called when the game is over. Simple for now, but expandable
    void GameOver(bool won)
    {
        int score = ScoreManager.SCORE;
        if (fsRun != null) score += fsRun.score;

        if (won)
        {
            gameOverText.text = "Round Over";
            roundResultText.text = "You won this round!\nRound Score: " + score;
            //print("Game Over. You won! :)");
            ScoreManager.EVENT(eScoreEvent.gameWin);


        }
        else
        {
            gameOverText.text = "Game Over";
            if (ScoreManager.HIGH_SCORE <= score)
            {
                string str = "You got the high score!\nHigh score: " + score;
                roundResultText.text = str;
            }
            else
            {
                roundResultText.text = "Your final score was: " + score;
            }

            //print("Game Over. You Lost. :(");


        }

        Invoke("ReloadLevel", reloadDelay); // a
    }
    void ReloadLevel()
    {
        // Reload the scene, resetting the game
        SceneManager.LoadScene("__Clock");
    }



    // Return true if the two cards are adjacent in rank (A & K wrap around)
    public bool AdjacentRank(CardClock c0, CardClock c1)
    {
        // If either card is face-down, it's not adjacent.
        if (!c0.faceUp || !c1.faceUp) return (false);
        // If they are 1 apart, they are adjacent
        if (Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }
        // If one is Ace and the other King, they are adjacent
        if (c0.rank == 1 && c1.rank == 13) return (true);
        if (c0.rank == 13 && c1.rank == 1) return (true);
        // Otherwise, return false
        return (false);
    }

}


