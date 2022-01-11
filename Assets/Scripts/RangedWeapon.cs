using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Item
{
    Player player;
    public float rangedAttackDamage;
    public float rangedAttackSpeed;
    public float rangedAttackNoise;
    public float rangedAttackRange;
    public float rangedKnockback;
    public bool large;
    public int magazineSize;
    public int inMagazine;
    public float reloadTime;
    public float aimTime;
    public Ammo.AmmoType ammoType;

    public enum GunType { FullAuto, SemiAuto, BoltAction }
    public GunType gunType;

    private void Start()
    {
        name = displayName;
        goid = GetInstanceID().ToString();
        player = Player.Instance;
        descriptiveText = "Semi-automatic\nDamage: " + rangedAttackDamage + "\nTime to aim: " + aimTime + "\nNoise: " + rangedAttackNoise + "\nRange: " + rangedAttackRange + "\nLT to aim, RT to fire";
        Load();
    }

    public override void Load()
    {
        inMagazine = ES3.Load(goid + "inMagazine", inMagazine);
        base.Load();
    }

    public override void Save()
    {
        if (player)
        {
            ES3.Save(goid + "inMagazine", inMagazine);
            base.Save();
        }
    }

    public override void Equip(Player owner)
    {
        bool isReplacingEquipment = false;
        if (owner.rangedWeaponEquipped)
            if (owner.rangedWeaponEquipped != this)
            {
                int storage = 0;
                if (owner.backpackEquipped)
                    storage = owner.backpackEquipped.storage;
                if (owner.items.Count < owner.inventorySize + storage)
                {
                    owner.rangedWeaponEquipped.Unequip();
                    isReplacingEquipment = true;
                }
                else
                {
                    owner.Drop(owner.rangedWeaponEquipped);
                }
            }
        gameObject.SetActive(true);
        GetComponent<Collider>().enabled = false;
        owner.RemoveItem(this, isReplacingEquipment);
        owner.rangedWeaponEquipped = this;
        owner.HolsterWeapon();
    }
}
