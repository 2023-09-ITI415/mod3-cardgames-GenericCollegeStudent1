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
    public float reloadDelay = 2f;
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
        SetUpUITexts();
    }

    void SetUpUITexts()
    {
        // ... (Existing code remains unchanged)
    }

    void Start()
    {
        Scoreboard.S.score = ScoreManager.SCORE;

        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);

        layout = GetComponent<Layout>();
        layout.ReadLayout(layoutXML.text);
        drawPile = ConvertListCardsToListCardClocks(deck.cards);
        LayoutGame();
    }

    List<CardClock> ConvertListCardsToListCardClocks(List<Card> lCD)
    {
        List<CardClock> lCC = new List<CardClock>();
        CardClock tCC;
        foreach (Card tCD in lCD)
        {
            tCC = tCD as CardClock;
            lCC.Add(tCC);
        }
        return (lCC);
    }

    CardClock Draw()
    {
        CardClock cd = drawPile[0];
        drawPile.RemoveAt(0);
        return (cd);
    }

    void LayoutGame()
    {
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        CardClock cc;
        foreach (SlotDef tSD in layout.slotDefs)
        {
            cc = Draw();
            cc.faceUp = tSD.faceUp;
            cc.transform.parent = layoutAnchor;
            cc.transform.localPosition = new Vector3(
            layout.multiplier.x * tSD.x,
            layout.multiplier.y * tSD.y,
            -tSD.layerID);
            cc.layoutID = tSD.id;
            cc.slotDef = tSD;
            cc.state = CardState.tableau;
            cc.SetSortingLayerName(tSD.layerName);
            tableau.Add(cc);
        }

        foreach (CardClock tCC in tableau)
        {
            foreach (int hid in tCC.slotDef.hiddenBy)
            {
                cc = FindCardByLayoutID(hid);
                tCC.hiddenBy.Add(cc);
            }
        }

        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    CardClock FindCardByLayoutID(int layoutID)
    {
        foreach (CardClock tCC in tableau)
        {
            if (tCC.layoutID == layoutID)
            {
                return (tCC);
            }
        }
        return (null);
    }

    void SetTableauFaces()
    {
        foreach (CardClock cd in tableau)
        {
            bool faceUp = true;
            foreach (CardClock cover in cd.hiddenBy)
            {
                if (cover.state == CardState.tableau)
                {
                    faceUp = false;
                }
            }
            cd.faceUp = faceUp;
        }
    }

    void MoveToDiscard(CardClock cc)
    {
        cc.state = CardState.discard;
        discardPile.Add(cc);
        cc.transform.parent = layoutAnchor;
        cc.transform.localPosition = new Vector3(
        layout.multiplier.x * layout.discardPile.x,
        layout.multiplier.y * layout.discardPile.y,
        -layout.discardPile.layerID + 0.5f);
        cc.faceUp = true;
        cc.SetSortingLayerName(layout.discardPile.layerName);
        cc.SetSortOrder(-100 + discardPile.Count);
    }

    void MoveToTarget(CardClock cc)
    {
        if (target != null) MoveToDiscard(target);
        target = cc;
        cc.state = CardState.target;
        cc.transform.parent = layoutAnchor;
        cc.transform.localPosition = new Vector3(
        layout.multiplier.x * layout.discardPile.x,
        layout.multiplier.y * layout.discardPile.y,
        -layout.discardPile.layerID);
        cc.faceUp = true;
        cc.SetSortingLayerName(layout.discardPile.layerName);
        cc.SetSortOrder(0);
    }

    void UpdateDrawPile()
    {
        CardClock cc;
        for (int i = 0; i < drawPile.Count; i++)
        {
            cc = drawPile[i];
            cc.transform.parent = layoutAnchor;
            Vector2 dpStagger = layout.drawPile.stagger;
            cc.transform.localPosition = new Vector3(
            layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
            layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
            -layout.drawPile.layerID + 0.1f * i);
            cc.faceUp = false;
            cc.state = CardState.drawpile;
            cc.SetSortingLayerName(layout.drawPile.layerName);
            cc.SetSortOrder(-10 * i);
        }
    }

    public void CardClicked(CardClock cc)
    {
        switch (cc.state)
        {
            case CardState.target:
                break;
            case CardState.drawpile:
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                ScoreManager.EVENT(eScoreEvent.draw);
                FloatingScoreHandler(eScoreEvent.draw);
                break;
            case CardState.tableau:
                bool validMatch = true;
                if (!cc.faceUp)
                {
                    validMatch = false;
                }
                if (!AdjacentRank(cc, target))
                {
                    validMatch = false;
                }
                if (!validMatch) return;
                tableau.Remove(cc);
                MoveToTarget(cc);
                SetTableauFaces();
                ScoreManager.EVENT(eScoreEvent.mine);
                FloatingScoreHandler(eScoreEvent.mine);
                break;
        }
        CheckForGameOver();
    }

    void CheckForGameOver()
    {
        if (tableau.Count == 0)
        {
            GameOver(true);
            return;
        }
        if (drawPile.Count > 0)
        {
            return;
        }
        foreach (CardClock cc in tableau)
        {
            if (AdjacentRank(cc, target))
            {
                return;
            }
        }
        GameOver(false);
    }

    void ShowResultsUI(bool show)
    {
        gameOverText.gameObject.SetActive(show);
        roundResultText.gameObject.SetActive(show);
    }

    void GameOver(bool won)
    {
        int score = ScoreManager.SCORE;
        if (fsRun != null) score += fsRun.score;
        if (won)
        {
            gameOverText.text = "Round Over";
            roundResultText.text = "You won this round!\nRound Score: " + score;
            ShowResultsUI(true);
            ScoreManager.EVENT(eScoreEvent.gameWin);
            FloatingScoreHandler(eScoreEvent.gameWin);
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
            ShowResultsUI(true);
            ScoreManager.EVENT(eScoreEvent.gameLoss);
            FloatingScoreHandler(eScoreEvent.gameLoss);
        }
        Invoke("ReloadLevel", reloadDelay);
    }

    void ReloadLevel()
    {
        SceneManager.LoadScene("__Clock");
    }

    public bool AdjacentRank(CardClock c0, CardClock c1)
    {
        if (!c0.faceUp || !c1.faceUp) return (false);
        if (Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }
        if (c0.rank == 1 && c1.rank == 13) return (true);
        if (c0.rank == 13 && c1.rank == 1) return (true);
        return (false);
    }

    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;
        switch (evt)
        {
            case eScoreEvent.draw:
            case eScoreEvent.gameWin:
            case eScoreEvent.gameLoss:
                if (fsRun != null)
                {
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null;
                }
                break;
            case eScoreEvent.mine:
                FloatingScore fs;
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if (fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }
    }
}
