using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI Instance { get; private set; }
    Player player;
    GameManager gameManager;
    CameraController camController;

    public Item inspectTarget;

    public TMP_Text timeSurvivedLabel;
    public TMP_Text realTimeLabel;
    public TMP_Text timeLeftLabel;
    public TMP_Text rangedWeaponLabel;
    public TMP_Text meleeWeaponLabel;
    public TMP_Text itemLabel;
    public TMP_Text inspectLabel;
    public TMP_Text inspectText;
    public Image healthRadial;
    public Image staminaRadial;
    public Image hungerRadial;
    public Image thirstRadial;
    public Image burnedHealthRadial;
    public Image burnedStaminaRadial;
    public GameObject inventoryBar;

    public Image eatingProgressRadial;
    public Image reloadProgressRadial;
    public Image aimProgressRadial;
    public Image searchProgressRadial;
    public TMP_Text targetLabel;

    public List<InventorySlot> inventorySlots;

    private void Awake() { Instance = this; }

    private void Start()
    {
        player = Player.Instance;
        gameManager = GameManager.Instance;
        camController = CameraController.Instance;
    }

    private void Update()
    {
        ScreenSpaceUI();
        WorldSpaceUI();
    }

    private void ScreenSpaceUI()
    {
        DateTime time = DateTime.Now;
        realTimeLabel.text = time.Hour.ToString().PadLeft(2, '0') + ":" + time.Minute.ToString().PadLeft(2, '0');
        timeSurvivedLabel.text = gameManager.timeSurvived.Hours.ToString().PadLeft(2, '0') + ":" + gameManager.timeSurvived.Minutes.ToString().PadLeft(2, '0') + ":" + gameManager.timeSurvived.Seconds.ToString().PadLeft(2, '0');

        if (player)
        {
            float timeLeft = (Mathf.Min(player.vitals.timeUntilStarving, player.vitals.timeUntilDehydrated));
            if (timeLeft > 1)
                timeLeftLabel.text = "You can surivive " + (int)timeLeft + " hours on what's in your pack";
            else
                timeLeftLabel.text = "You can surivive less than an hour on what's in your pack";


            healthRadial.fillAmount = player.vitals.health / player.vitals.maxMaxHealth;
            staminaRadial.fillAmount = player.vitals.stamina / player.vitals.maxMaxStamina;
            hungerRadial.fillAmount = player.vitals.calories / player.vitals.maxCalories;
            thirstRadial.fillAmount = player.vitals.milliliters / player.vitals.maxMilliliters;

            burnedHealthRadial.fillAmount = (player.vitals.maxMaxHealth - player.vitals.maxHealth) / player.vitals.maxMaxHealth;
            burnedStaminaRadial.fillAmount = (player.vitals.maxMaxStamina - player.vitals.maxStamina) / player.vitals.maxMaxStamina;

            if (player.rangedWeaponEquipped)
            {
                if (player.rangedWeaponEquipped.name == "Pistol")
                    rangedWeaponLabel.text = "Pistol " + player.rangedWeaponEquipped.inMagazine + "/" + player.pistolAmmo;
                if (player.rangedWeaponEquipped.name == "Rifle")
                    rangedWeaponLabel.text = "Rifle " + player.rangedWeaponEquipped.inMagazine + "/" + player.rifleAmmo;
            }
            else
                rangedWeaponLabel.text = "-----";
            if (player.meleeWeaponEquipped)
                meleeWeaponLabel.text = player.meleeWeaponEquipped.name + " " + player.meleeWeaponEquipped.durability + "/" + player.meleeWeaponEquipped.maxDurability;
            else
                meleeWeaponLabel.text = "-----";
            if (player.itemSelected)
                itemLabel.text = player.itemSelected.name;
            else
                itemLabel.text = "-----";

            if (gameManager.gameState == GameManager.GameState.Inspecting)
            {
                if (player.pickUpTarget)
                {
                    inspectTarget = player.pickUpTarget;
                    inspectLabel.text = inspectTarget.name;
                    inspectText.text = inspectTarget.descriptiveText;
                    camController.depthOfField.gaussianStart.value = 15;
                    camController.depthOfField.gaussianEnd.value = 15;
                    camController.black.color = new Color(0, 0, 0, .33f);
                    inspectTarget.transform.position = camController.inspectPoint.transform.position;
                    inspectTarget.transform.rotation = camController.inspectPoint.transform.rotation;
                }
            }
            else
            {
                if (inspectTarget)
                {
                    inspectTarget.transform.position = player.pickUpPosition;
                    inspectTarget.transform.rotation = player.pickUpRotation;
                }
                inspectTarget = null;
                inspectLabel.text = null;
                inspectText.text = null;
                camController.depthOfField.gaussianStart.value = 150;
                camController.depthOfField.gaussianEnd.value = 225;
                camController.black.color = new Color(0, 0, 0, 0);
            }

            if (player.items.Count > 0)
            {
                for (int i = 0; i < player.items.Count; i++)
                {
                    inventorySlots[i].image.sprite = player.items[i].icon;
                    if (player.items[i] == player.itemSelected)
                        inventorySlots[i].selected.gameObject.SetActive(true);
                    else
                        inventorySlots[i].selected.gameObject.SetActive(false);
                }
            }
        }

    }

    private void WorldSpaceUI()
    {
        if (player)
        {
            reloadProgressRadial.transform.position = player.transform.position;
            if (player.rangedWeaponEquipped)
                reloadProgressRadial.fillAmount = player.reloadTimeElapsed / player.rangedWeaponEquipped.reloadTime;
            else
                reloadProgressRadial.fillAmount = 0;

            eatingProgressRadial.transform.position = player.transform.position;
            if (player.itemSelected)
            {
                if (player.itemSelected is Food)
                {
                    var food = player.itemSelected as Food;
                    eatingProgressRadial.fillAmount = player.eatingTimeElapsed / food.eatingTime;
                }
            }     
            else
                eatingProgressRadial.fillAmount = 0;

            if (player.fov.target)
            {
                targetLabel.text = player.fov.target.name;
                targetLabel.transform.position = player.fov.target.transform.position;
                if (player.fov.target.name == "Zombie")
                    targetLabel.text = null;
                if (player.fov.target.gameObject.GetComponent<Container>())
                {
                    var container = player.fov.target.gameObject.GetComponent<Container>();
                    searchProgressRadial.transform.position = player.fov.target.transform.position;
                    searchProgressRadial.fillAmount = player.searchTimeElapsed / container.searchTime;
                }
                else
                    searchProgressRadial.fillAmount = 0;
            }
            else
                targetLabel.text = null;

            if (gameManager.gameState == GameManager.GameState.Inspecting)
                targetLabel.text = null;

            if (player.actionState == Player.ActionState.Aiming)
            {
                if (player.target)
                {
                    aimProgressRadial.transform.position = player.target.transform.position;
                    if (player.rangedWeaponEquipped)
                        aimProgressRadial.fillAmount = player.aimTimeElapsed / player.rangedWeaponEquipped.aimTime;
                    else
                        aimProgressRadial.fillAmount = 0;
                }
                else
                    aimProgressRadial.fillAmount = 0;
            }
            else
                aimProgressRadial.fillAmount = 0;
        }
        else
        {
            reloadProgressRadial.enabled = false;
            eatingProgressRadial.enabled = false;
            targetLabel.enabled = false;
            aimProgressRadial.enabled = false;
        }
    }
}
