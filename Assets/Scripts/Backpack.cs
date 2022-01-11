using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Backpack : Item
{
    public int storage;

    private void Start()
    {
        name = displayName;
        goid = GetInstanceID().ToString();
        descriptiveText = "Adds " + storage + " Inventory Slots";
        Load();
    }

    public override void Equip(Player owner)
    {
        int indexModifier = 0;
        if (owner.backpackEquipped)
            if (owner.backpackEquipped != this)
            {
                owner.backpackEquipped.Unequip();
                indexModifier = -1;
            }
        gameObject.SetActive(true);
        GetComponent<Collider>().enabled = false;
        owner.RemoveItem(this, indexModifier);
        owner.backpackEquipped = this;
        transform.position = owner.backpackAttachPoint.position;
        transform.rotation = owner.backpackAttachPoint.rotation;
        transform.parent = owner.backpackAttachPoint;
    }
}