namespace Clock
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Clock;
    // An enum defines a variable type with a few prenamed values // a
    public enum eCardState
    {
        drawpile,
        tableau,
        target,
        discard
    }
    public class CardProspector : Card
    {

        [Header("Set Dynamically: CardProspector")]
        public eCardState state = eCardState.drawpile;

        public List<CardProspector> hiddenBy = new List<CardProspector>();

        public int layoutID;


        public SlotDef slotDef;

        override public void OnMouseUpAsButton()
        {
            // Call the CardClicked method on the Prospector singleton
            Prospector.S.CardClicked(this);
            // Also call the base class (Card.cs) version of this method
            base.OnMouseUpAsButton(); // a
        }
    }
}