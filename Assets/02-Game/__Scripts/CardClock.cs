using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardState
{
    drawpile,
    tableau,
    target,
    discard
}

public class CardClock : Card
{
    public CardState state = CardState.drawpile;
    public List<CardClock> hiddenBy = new List<CardClock>();
    public int layoutID;
    public SlotDef slotDef;

    override public void OnMouseUpAsButton()
    {
        Clock.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }
}
