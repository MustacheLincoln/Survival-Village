using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
    Player player;
    public float maxMaxHealth = 100;
    public float maxHealth;
    public float health;
    public float maxMaxStamina = 100;
    public float maxStamina;
    public float stamina;
    public float maxCalories = 2000;
    public float calories;
    public float maxMilliliters = 2000;
    public float milliliters;
    private float timeUntilStarving;
    private float timeUntilDehydrated;
    public float exertion;
    public float baseExertion = 1;
    public float healthExertion = 1;
    public float staminaExertion = 1;
    public float recuperation = 1;

    public bool starving;
    public bool dehydrated;

    private void Start()
    {
        player = Player.Instance;
        maxHealth = maxMaxHealth;
        health = maxHealth;
        maxStamina = maxMaxStamina;
        stamina = maxStamina;
        calories = maxCalories / 2;
        milliliters = maxMilliliters / 2;
    }

    private void CalculateTimeLeft()
    {
        timeUntilStarving = (calories + player.caloriesInInventory) / .000385f / 60 / 60 / 60;
        timeUntilDehydrated = (milliliters + player.millilitersInInventory) / .000385f / 60 / 60 / 60; //Add items in inventory
        print(timeUntilDehydrated);
    }

    private void Update()
    {
        switch (player.movementState)
        {
            case Player.MovementState.Idle:
                recuperation = 3;
                break;
            case Player.MovementState.Walking:
                recuperation = 1;
                break;
            case Player.MovementState.Running:
                recuperation = 0;
                stamina -= 10 * Time.deltaTime;
                maxStamina -= .1f * Time.deltaTime;
                break;
            case Player.MovementState.Crouching:
                recuperation = 2;
                break;
        }

        if (health < maxHealth)
        {
            health += ((calories + milliliters) / (maxCalories + maxMilliliters)) * recuperation * Time.deltaTime;
            healthExertion = 1 * recuperation;
            health = Mathf.Clamp(health, 0, maxHealth);
        }
        else
            healthExertion = 0;

        if (stamina < maxStamina)
        {
            if (player.movementState != Player.MovementState.Running)
            {
                stamina += ((calories+milliliters) / (maxCalories+maxMilliliters)) * recuperation * Time.deltaTime;
                staminaExertion = 1 * recuperation;
            }
            stamina = Mathf.Clamp(stamina, 0, maxStamina);

        }
        else
            staminaExertion = 0;

        if (calories > 0)
        {
            exertion = ((baseExertion + healthExertion + staminaExertion) * .023f * Time.deltaTime);
            calories -= exertion;
            timeUntilStarving = milliliters / exertion / 60 / 60 / 60;
            calories = Mathf.Clamp(calories, 0, maxCalories);
        }
        starving = (calories <= 0);
        if (starving)
            Starving();

        if (milliliters > 0)
        {
            exertion = ((baseExertion + healthExertion + staminaExertion) * .023f * Time.deltaTime);
            milliliters -= exertion;
            timeUntilDehydrated = milliliters / exertion / 60 / 60 / 60;
            milliliters = Mathf.Clamp(milliliters, 0, maxMilliliters);
        }
        dehydrated = (milliliters <= 0);
        if (dehydrated)
            Dehydrated();

        if (health <= 0)
        {
            player.Die();
        }

        CalculateTimeLeft();
    }
    void Starving()
    {
        health -= Time.deltaTime;
    }
    void Dehydrated()
    {
        health -= Time.deltaTime;
    }
}
