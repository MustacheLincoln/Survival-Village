using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Item
{
    Player player;
    public float meleeAttackDamage;
    public float meleeAttackSpeed;
    public float meleeAttackNoise;
    public float meleeAttackRange;
    public float meleeKnockback;
    public int maxDurability;
    public int durability;
    public bool large;

    private void Start()
    {
        name = displayName;
        goid = GetInstanceID().ToString();
        player = Player.Instance;
        durability = maxDurability / 2 + Random.Range(0, maxDurability / 2 + 1);
        descriptiveText = "Damage: " + meleeAttackDamage + "\nSpeed: " + meleeAttackSpeed + "\nNoise: " + meleeAttackNoise + "\nRange: " + meleeAttackRange + "\nDurability: " + durability + "/" + maxDurability + "\nRT to swing";

        Load();
    }
    public override void Load()
    {
        durability = ES3.Load(goid + "durability", durability);
        base.Load();
    }


    public override void Save()
    {
        if (player)
        {
            ES3.Save(goid + "durability", durability);
            base.Save();
        }
    }

    public override void Equip(Player owner)
    {
        bool isReplacingEquipment = false;
        if (owner.meleeWeaponEquipped)
            if (owner.meleeWeaponEquipped != this)
            {
                int storage = 0;
                if (owner.backpackEquipped)
                    storage = owner.backpackEquipped.storage;
                if (owner.items.Count < owner.inventorySize + storage)
                {
                    owner.meleeWeaponEquipped.Unequip();
                    isReplacingEquipment = true;
                }
                else
                {
                    owner.meleeWeaponEquipped.Drop();
                }
            }
        gameObject.SetActive(true);
        GetComponent<Collider>().enabled = false;
        owner.RemoveItem(this, isReplacingEquipment);
        owner.meleeWeaponEquipped = this;
        owner.HolsterWeapon();
    }

    public void Break()
    {
        player.meleeWeaponEquipped = null;
        Destroy();
    }
}
