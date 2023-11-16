using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// An enum defines a variable type with a few prenamed values // a
public enum CardState
{
    drawpile,
    tableau,
    target,
    discard,
    clock,
    center

}
public class CardClock : Card
{

    [Header("Set Dynamically: CardClock")]
    public CardState state = CardState.drawpile;

    public List<CardClock> hiddenBy = new List<CardClock>();

    public int layoutID;


    public SlotDef slotDef;

    override public void OnMouseUpAsButton()
    {
        // Call the CardClicked method on the Prospector singleton
        Clock.S.CardClicked(this);

        // Also call the base class (Card.cs) version of this method
        base.OnMouseUpAsButton(); // a
    }
}

