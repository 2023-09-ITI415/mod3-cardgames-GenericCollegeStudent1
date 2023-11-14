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
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

    void Awake()
    {
        S = this;
        SetUpUITexts();
    }

    void SetUpUITexts()
    {
        // Set up the HighScore UI Text
        GameObject go = GameObject.Find("HighScore");
        if (go != null)
        {
            highScoreText = go.GetComponent<Text>();
        }
        int highScore = ScoreManager.HIGH_SCORE;
        string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
        go.GetComponent<Text>().text = hScore;
        // Set up the UI Texts that show at the end of the round
        go = GameObject.Find("GameOver");
        if (go != null)
        {
            gameOverText = go.GetComponent<Text>();
        }
        go = GameObject.Find("RoundResult");
        if (go != null)
        {
            roundResultText = go.GetComponent<Text>();
        }
        // Make the end of round texts invisible
        ShowResultsUI(false);
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

    List<CardProspector> ConvertListCardsToListCardClocks(List<Card> lCD)
    {
        List<CardProspector> lCC = new List<CardProspector>();
        CardProspector tCC;
        foreach (Card tCD in lCD)
        {
            tCC = tCD as CardProspector;
            lCC.Add(tCC);
        }
        return (lCC);
    }

    CardProspector Draw()
    {
        CardProspector cd = drawPile[0];
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

        CardProspector cc;
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
            cc.state = eCardState.tableau;
            cc.SetSortingLayerName(tSD.layerName);
            tableau.Add(cc);
        }

        foreach (CardProspector tCC in tableau)
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

    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach (CardProspector tCC in tableau)
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
        foreach (CardProspector cd in tableau)
        {
            bool faceUp = true;
            foreach (CardProspector cover in cd.hiddenBy)
            {
                if (cover.state == eCardState.tableau)
                {
                    faceUp = false;
                }
            }
            cd.faceUp = faceUp;
        }
    }

    void MoveToDiscard(CardProspector cc)
    {
        cc.state = eCardState.discard;
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

    void MoveToTarget(CardProspector cc)
    {
        if (target != null) MoveToDiscard(target);
        target = cc;
        cc.state = eCardState.target;
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
        CardProspector cc;
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
            cc.state = eCardState.drawpile;
            cc.SetSortingLayerName(layout.drawPile.layerName);
            cc.SetSortOrder(-10 * i);
        }
    }

    public void CardClicked(CardProspector cc)
    {
        switch (cc.state)
        {
            case eCardState.target:
                break;
            case eCardState.drawpile:
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                ScoreManager.EVENT(eScoreEvent.draw);
                FloatingScoreHandler(eScoreEvent.draw);
                break;
            case eCardState.tableau:
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
        foreach (CardProspector cc in tableau)
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

    public bool AdjacentRank(CardProspector c0, CardProspector c1)
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
