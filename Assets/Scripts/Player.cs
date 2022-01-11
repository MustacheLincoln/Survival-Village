using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour, IDamageable<float>
{
    public static Player Instance { get; private set; }
    GameManager gameManager;
    NavMeshAgent navMeshAgent;
    public PlayerVitals vitals;
    public FieldOfView fov;
    Camera cam;
    Animator animator;
    public Transform backpackAttachPoint;
    public Transform rightHandHoldPoint;
    public Transform leftHandHoldPoint;
    public Transform largeMeleeWeaponAttachPoint;
    public Transform largeRangedWeaponAttachPoint;
    public Transform smallMeleeWeaponAttachPoint;
    public Transform smallRangedWeaponAttachPoint;
    public Backpack backpackEquipped;

    float pickUpTime = .25f;
    float pickUpTimeElapsed;
    public Item pickUpTarget;
    GameObject navTarget;
    public GameObject target;

    public float danger = 0;
    public float speed;
    float walkSpeed = 2;
    float runSpeed = 4;
    float crouchWalkSpeed = .75f;
    float acceleration = 50;
    float turnSpeedLow = 7;
    float turnSpeedHigh = 15;
    float grabDistance = 1.4f;
    float idleRadius = 1;
    float walkRadius = 3;
    float runRadius = 6;
    float crouchRadius = 1;
    float noiseSphereRadius = 1;
    public int inventorySize = 4;

    float rangedAttackCooldown;
    public float reloadTimeElapsed;
    public float aimTimeElapsed;
    public RangedWeapon rangedWeaponEquipped;
    bool roundChambered;
    public int pistolAmmo;
    public int rifleAmmo;

    float meleeAttackCooldown;
    public MeleeWeapon meleeWeaponEquipped;

    public List<Item> items;
    public List<String> inspected;

    public bool itemSelectionChanged;

    public Food eating;
    public Item itemSelected;
    public float eatingTimeElapsed;
    public float caloriesInInventory;
    public float millilitersInInventory;

    Vector2 input;
    Vector3 camForward;
    Vector3 camRight;
    Vector3 intent;
    Vector3 velocity;
    float turnSpeed;
    Vector3 currentPos;
    public bool isMoving = false;
    Vector3 lastPos;
    float pulseTime = .5f;

    float fovRadius = 4;
    float fovAngle = 250;

    public enum MovementState { Idle, Walking, Running, Crouching, CrouchWalking, Holding }
    public MovementState movementState;
    public enum ActionState { Idle, Reloading, Aiming, PickingUp, Inspecting }
    public ActionState actionState;
    public float searchTimeElapsed;

    private void Awake() 
    { 
        Instance = this;
        navMeshAgent = GetComponent<NavMeshAgent>();
        vitals = GetComponent<PlayerVitals>();
        fov = GetComponent<FieldOfView>();
        animator = GetComponentInChildren<Animator>();
        fov.radius = fovRadius;
        fov.angle = fovAngle;
        fov.targetMask = LayerMask.GetMask("Interactable");
        cam = Camera.main;
        navMeshAgent.speed = walkSpeed;
        navMeshAgent.acceleration = acceleration;

        navMeshAgent.Warp(ES3.Load("playerPosition", Vector3.zero));
        transform.rotation = ES3.Load("playerRotation", Quaternion.identity);

        danger = ES3.Load("playerDanger", 0f);
        rangedWeaponEquipped = ES3.Load("playerRangedWeaponEquipped", rangedWeaponEquipped);
        roundChambered = ES3.Load("playerRoundChambered", roundChambered);
        pistolAmmo = ES3.Load("playerPistolAmmo", pistolAmmo);
        rifleAmmo = ES3.Load("playerRifleAmmo", rifleAmmo);
        meleeWeaponEquipped = ES3.Load("playerMeleeWeaponEquipped", meleeWeaponEquipped);
        items = ES3.Load("playerItems", items);
        itemSelected = ES3.Load("playerItemSelected", itemSelected);
        backpackEquipped = ES3.Load("backpackEquipped", backpackEquipped);
        inspected = ES3.Load("inspected", inspected);

        currentPos = transform.position;
        lastPos = currentPos;

        movementState = MovementState.Idle;
        StartCoroutine(EmitNoisePulse());
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
    }

    private void Update()
    {
        CalculateIsMoving();
        CaptureInput();
        CalculateCamera();
        ActionStateMachine();
        MovementStateMachine();
        Animate();

        rangedAttackCooldown -= Time.deltaTime;
        meleeAttackCooldown -= Time.deltaTime;

        velocity = Vector3.Lerp(velocity, transform.forward * input.magnitude * speed, acceleration * Time.deltaTime);

        navMeshAgent.Move(velocity*Time.deltaTime);

        turnSpeed = Mathf.Lerp(turnSpeedHigh, turnSpeedLow, velocity.magnitude / 5);

        navMeshAgent.speed = speed;
    }

    private void Animate()
    {
        animator.SetBool("isWalking", (movementState == MovementState.Walking));
        animator.SetBool("isRunning", (movementState == MovementState.Running));
        animator.SetBool("isCrouching", (movementState == MovementState.Crouching));
        animator.SetBool("isCrouchWalking", (movementState == MovementState.CrouchWalking));
        animator.SetBool("isSideArmAiming", (actionState == ActionState.Aiming));
        switch (movementState)
        {
            case MovementState.Idle:
                animator.speed = 1;
                break;
            case MovementState.Crouching:
                animator.speed = 1;
                break;
            case MovementState.Walking:
                animator.speed = input.magnitude;
                break;
            case MovementState.Running:
                animator.speed = input.magnitude;
                break;
            case MovementState.CrouchWalking:
                animator.speed = input.magnitude;
                break;
        }
        if (input.magnitude == 0)
            animator.speed = 1;
        if (actionState == ActionState.Aiming)
            animator.speed = 1;
    }

    private void MovementStateMachine()
    {
        switch (movementState)
        {
            case MovementState.Idle:
                speed = walkSpeed;
                noiseSphereRadius = idleRadius;
                break;
            case MovementState.Walking:
                speed = walkSpeed;
                noiseSphereRadius = idleRadius + walkRadius * input.magnitude;
                break;
            case MovementState.Running:
                speed = runSpeed;
                noiseSphereRadius = idleRadius + runRadius * input.magnitude;
                break;
            case MovementState.Crouching:
                speed = crouchWalkSpeed;
                noiseSphereRadius = idleRadius;
                break;
            case MovementState.CrouchWalking:
                speed = crouchWalkSpeed;
                noiseSphereRadius = idleRadius + crouchRadius * input.magnitude;
                break;
            case MovementState.Holding:
                speed = 0;
                noiseSphereRadius = idleRadius;
                break;
        }
    }

    private void ActionStateMachine()
    {
        switch (actionState)
        {
            case ActionState.Idle:
                aimTimeElapsed = 0;
                reloadTimeElapsed = 0;
                pickUpTimeElapsed = 0;
                fov.radius = fovRadius;
                fov.angle = fovAngle;
                fov.targetMask = LayerMask.GetMask("Interactable");
                target = null;
                if (Input.GetMouseButton(0) || Input.GetAxis("Fire") > 0 || Input.GetKey(KeyCode.Tab))
                    MeleeAttack();
                if (Input.GetAxis("InventoryAxis") < 0)
                {
                    if (itemSelectionChanged == false)
                        ChangeItemSelectedDown();
                }
                else if (Input.GetAxis("InventoryAxis") > 0 || Input.GetKeyDown(KeyCode.BackQuote))
                {
                    if (itemSelectionChanged == false)
                        ChangeItemSelectedUp();
                }
                else
                    itemSelectionChanged = false;
                if (Input.GetButtonDown("Use"))
                    if (itemSelected)
                    {
                        if (itemSelected.GetComponent<Food>())
                            StartCoroutine(Eat(itemSelected as Food));
                        else
                        {
                            itemSelected.Equip();
                        }
                    }
                break;
            case ActionState.Reloading:
                DrawRangedWeapon();
                aimTimeElapsed = 0;
                target = null;
                reloadTimeElapsed += Time.deltaTime;
                if (reloadTimeElapsed >= rangedWeaponEquipped.reloadTime)
                {
                    if (rangedWeaponEquipped.name == "Pistol" && pistolAmmo >= rangedWeaponEquipped.magazineSize - rangedWeaponEquipped.inMagazine)
                    {
                        pistolAmmo -= rangedWeaponEquipped.magazineSize - rangedWeaponEquipped.inMagazine;
                        rangedWeaponEquipped.inMagazine = rangedWeaponEquipped.magazineSize;
                    }
                    else if (rangedWeaponEquipped.name == "Pistol" && pistolAmmo < rangedWeaponEquipped.magazineSize - rangedWeaponEquipped.inMagazine)
                    {
                        rangedWeaponEquipped.inMagazine += pistolAmmo;
                        pistolAmmo = 0;
                    }
                    if (rangedWeaponEquipped.name == "Rifle" && rifleAmmo >= rangedWeaponEquipped.magazineSize - rangedWeaponEquipped.inMagazine)
                    {
                        rifleAmmo -= rangedWeaponEquipped.magazineSize - rangedWeaponEquipped.inMagazine;
                        rangedWeaponEquipped.inMagazine = rangedWeaponEquipped.magazineSize;
                    }
                    else if (rangedWeaponEquipped.name == "Rifle" && rifleAmmo < rangedWeaponEquipped.magazineSize - rangedWeaponEquipped.inMagazine)
                    {
                        rangedWeaponEquipped.inMagazine += rifleAmmo;
                        rifleAmmo = 0;
                    }
                    reloadTimeElapsed = 0;
                    actionState = ActionState.Idle;
                }
                break;
            case ActionState.Aiming:
                DrawRangedWeapon();
                if (rangedWeaponEquipped)
                    fov.radius = rangedWeaponEquipped.rangedAttackRange;
                movementState = MovementState.Holding;
                pickUpTarget = null;
                pickUpTimeElapsed = 0;
                fov.angle = 45;
                if (Input.GetButtonDown("Reload"))
                    if (rangedWeaponEquipped.inMagazine < rangedWeaponEquipped.magazineSize)
                    {
                        if (rangedWeaponEquipped.name == "Pistol" && pistolAmmo > 0)
                            actionState = ActionState.Reloading;
                        else if (rangedWeaponEquipped.name == "Rifle" && rifleAmmo > 0)
                            actionState = ActionState.Reloading;
                    }
                if (fov.target)
                    if (fov.target.name != "Zombie")
                        fov.target = null;
                fov.targetMask = LayerMask.GetMask("Zombie");
                target = fov.target;
                if (rangedWeaponEquipped == null)
                {
                    actionState = ActionState.Idle;
                    break;
                }
                if (rangedWeaponEquipped.inMagazine > 0)
                {
                    if (target)
                        if (aimTimeElapsed < rangedWeaponEquipped.aimTime)
                            aimTimeElapsed += Time.deltaTime;
                }
                else
                {
                    aimTimeElapsed = 0;
                    if (Input.GetMouseButton(0) || Input.GetAxis("Fire") > 0 || Input.GetKey(KeyCode.Tab))
                        if (rangedWeaponEquipped.name == "Pistol" && pistolAmmo > 0)
                            actionState = ActionState.Reloading;
                        else if (rangedWeaponEquipped.name == "Rifle" && rifleAmmo > 0)
                            actionState = ActionState.Reloading;
                }
                if (Input.GetMouseButton(0) || Input.GetAxis("Fire") > 0)
                {
                    if (target)
                        if (roundChambered || rangedWeaponEquipped.fullAuto)
                            RangedAttack(target);
                }
                else
                    roundChambered = true;
                if (Input.GetMouseButton(1))
                {
                    input = Vector2.zero;
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit, int.MaxValue, 1 << LayerMask.NameToLayer("Ground")))
                        transform.LookAt(new Vector3(hit.point.x, transform.position.y, hit.point.z));
                }
                break;
            case ActionState.PickingUp:
                HolsterWeapon();
                aimTimeElapsed = 0;
                reloadTimeElapsed = 0;
                movementState = MovementState.Holding;
                pickUpTimeElapsed += Time.deltaTime;
                if (pickUpTimeElapsed >= pickUpTime)
                {
                    if (pickUpTarget)
                    {
                        navMeshAgent.ResetPath();
                        if (inspected.Contains(pickUpTarget.name))
                            PickUp();
                        else
                            Inspect(pickUpTarget);
                    }
                }
                break;
            case ActionState.Inspecting:
                HolsterWeapon();
                if (isMoving || pickUpTarget == null)
                {
                    pickUpTarget = null;
                    actionState = ActionState.Idle;
                }
                break;
        }
    }

    internal void RemoveItem(Item item, int indexModifier)
    {
        int index = items.IndexOf(item) + indexModifier;
        if (items.Contains(item))
            items.Remove(item);
        if (items.Count > 0)
        {
            if (index < items.Count - 1)
                itemSelected = items[Mathf.Max(0, index)];
            else
                itemSelected = items[items.Count - 1];
        }
        else
            itemSelected = null;

    }

    public void Inspect(Item target)
    {
        pickUpTarget = target;
        fov.target = null;
        actionState = ActionState.Inspecting;
    }

    public void HolsterWeapon()
    {
        if (rangedWeaponEquipped)
        {
            if (rangedWeaponEquipped.large)
            {
                rangedWeaponEquipped.transform.position = largeRangedWeaponAttachPoint.position;
                rangedWeaponEquipped.transform.rotation = largeRangedWeaponAttachPoint.rotation;
                rangedWeaponEquipped.transform.parent = largeRangedWeaponAttachPoint;
            }
            else
            {
                rangedWeaponEquipped.transform.position = smallRangedWeaponAttachPoint.position;
                rangedWeaponEquipped.transform.rotation = smallRangedWeaponAttachPoint.rotation;
                rangedWeaponEquipped.transform.parent = smallRangedWeaponAttachPoint;
            }
        }
        if (meleeWeaponEquipped)
        {
            if (meleeWeaponEquipped.large)
            {
                meleeWeaponEquipped.transform.position = largeMeleeWeaponAttachPoint.position;
                meleeWeaponEquipped.transform.rotation = largeMeleeWeaponAttachPoint.rotation;
                meleeWeaponEquipped.transform.parent = largeMeleeWeaponAttachPoint;
            }
            else
            {
                meleeWeaponEquipped.transform.position = smallMeleeWeaponAttachPoint.position;
                meleeWeaponEquipped.transform.rotation = smallMeleeWeaponAttachPoint.rotation;
                meleeWeaponEquipped.transform.parent = smallMeleeWeaponAttachPoint;
            }
        }
    }

    public void DrawRangedWeapon()
    {
        HolsterWeapon();
        if (rangedWeaponEquipped)
        {
            rangedWeaponEquipped.transform.position = rightHandHoldPoint.position;
            rangedWeaponEquipped.transform.rotation = rightHandHoldPoint.rotation;
            rangedWeaponEquipped.transform.parent = rightHandHoldPoint;
        }
    }

    public void DrawMeleeWeapon()
    {
        HolsterWeapon();
        if (meleeWeaponEquipped)
        {
            meleeWeaponEquipped.transform.position = rightHandHoldPoint.position;
            meleeWeaponEquipped.transform.rotation = rightHandHoldPoint.rotation;
            meleeWeaponEquipped.transform.parent = rightHandHoldPoint;
        }
    }

    private void LeaveBehind()
    {
        //Broken
        if (pickUpTarget)
            if (pickUpTarget.transform.parent)
                if (pickUpTarget.transform.parent.gameObject.GetComponent<Container>())
                    pickUpTarget.transform.parent.gameObject.GetComponent<Container>().NextItem();
        pickUpTarget = null;
        pickUpTimeElapsed = 0;
        actionState = ActionState.Idle;
        gameManager.gameState = GameManager.GameState.Playing;
    }

    private void ChangeItemSelectedUp()
    {
        if (items.Count > 0)
        {
            if (itemSelected == null)
            {
                itemSelected = items[0];
            }
            else
            {
                int i = items.IndexOf(itemSelected);
                if (i == items.Count - 1)
                    i = -1;
                itemSelected = items[i + 1];
            }
            itemSelectionChanged = true;
        }
    }

    private void ChangeItemSelectedDown()
    {
        if (items.Count > 0)
        {
            if (itemSelected == null)
            {
                itemSelected = items[items.Count - 1];
            }
            else
            {
                int i = items.IndexOf(itemSelected);
                if (i == 0)
                    i = items.Count;
                itemSelected = items[i - 1];
            }
            itemSelectionChanged = true;
        }
    }

    private void MeleeAttack()
    {
        if (meleeWeaponEquipped && meleeAttackCooldown <= 0)
        {
            DrawMeleeWeapon();
            Collider[] hitZombies = Physics.OverlapSphere(transform.position + transform.forward, meleeWeaponEquipped.meleeAttackRange, 1 << LayerMask.NameToLayer("Zombie"));
            if (hitZombies.Length > 0)
            {
                foreach (Collider zombie in hitZombies)
                {
                    zombie.gameObject.GetComponent<IDamageable<float>>().TakeDamage(meleeWeaponEquipped.meleeAttackDamage);
                    zombie.gameObject.GetComponent<NavMeshAgent>().Move((zombie.transform.position - transform.position).normalized * meleeWeaponEquipped.meleeKnockback);
                }
                if (meleeWeaponEquipped.durability > 0)
                    meleeWeaponEquipped.durability -= 1;
                else
                    meleeWeaponEquipped.Break();
            }
            EmitNoiseUnique(meleeWeaponEquipped.meleeAttackNoise);
            meleeAttackCooldown = meleeWeaponEquipped.meleeAttackSpeed;
        }
    }

    private void RangedAttack(GameObject target)
    {
        if (aimTimeElapsed >= rangedWeaponEquipped.aimTime && rangedAttackCooldown <= 0)
        {
            if (rangedWeaponEquipped.inMagazine > 0)
            {
                animator.SetTrigger("SideArmFire");
                roundChambered = false;
                target.GetComponent<IDamageable<float>>().TakeDamage(rangedWeaponEquipped.rangedAttackDamage);
                target.GetComponent<NavMeshAgent>().Move((target.transform.position - transform.position).normalized * rangedWeaponEquipped.rangedKnockback);
                EmitNoiseUnique(rangedWeaponEquipped.rangedAttackNoise);
                rangedWeaponEquipped.inMagazine -= 1;
                rangedAttackCooldown = rangedWeaponEquipped.rangedAttackSpeed;
                if (rangedWeaponEquipped.boltAction)
                    aimTimeElapsed = 0;
            }
        }
    }

    private void CalculateIsMoving()
    {
        currentPos = transform.position;
        isMoving = (currentPos != lastPos);
        lastPos = currentPos;
    }

    public IEnumerator EmitNoisePulse()
    {
        Collider[] hitZombies = Physics.OverlapSphere(transform.position, noiseSphereRadius, 1 << LayerMask.NameToLayer("Zombie"));
        if (hitZombies.Length > 0)
        {
            foreach (Collider zombie in hitZombies)
                    zombie.gameObject.GetComponent<Zombie>().StartChase(gameObject);
        }
        yield return new WaitForSeconds(pulseTime);
        StartCoroutine(EmitNoisePulse());
    }

    private void EmitNoiseUnique(float volume)
    {
        Collider[] hitZombies = Physics.OverlapSphere(transform.position, volume, 1 << LayerMask.NameToLayer("Zombie"));
        if (hitZombies.Length > 0)
        {
            foreach (Collider zombie in hitZombies)
                zombie.gameObject.GetComponent<Zombie>().StartChase(gameObject);
        }
    }

    private void PickUp()
    {
        int storage = 0;
        if (backpackEquipped)
            storage = backpackEquipped.storage;
        if (items.Count < inventorySize + storage)
        {
            if (pickUpTarget)
            {
                if (pickUpTarget.transform.parent)
                    if (pickUpTarget.transform.parent.gameObject.GetComponent<Container>())
                        pickUpTarget.transform.parent.gameObject.GetComponent<Container>().NextItem();
                pickUpTarget.AddToInventory();
                inspected.Add(pickUpTarget.name);
            }
            pickUpTarget = null;
            fov.target = null;
            pickUpTimeElapsed = 0;
            CalculateFoodInInventory();
        }
    }

    private IEnumerator Eat(Food food)
    {
        bool edible;
        if (food.calories > food.milliliters)
            edible = (vitals.calories < vitals.maxCalories - food.calories);
        else
            edible = (vitals.milliliters < vitals.maxMilliliters - food.milliliters);
        if (edible == false)
        {
            actionState = ActionState.Idle;
            yield break;
        }
        eating = food;
        HolsterWeapon();
        aimTimeElapsed = 0;
        reloadTimeElapsed = 0;
        movementState = MovementState.Holding;
        pickUpTarget = null;
        target = null;
        while (eatingTimeElapsed < food.eatingTime)
        {
            eatingTimeElapsed += Time.deltaTime;
            yield return null;
            if (isMoving)
            {
                eatingTimeElapsed = 0;
                eating = null;
                yield break;
            }
        }
        food.Eat();
        CalculateFoodInInventory();
        eatingTimeElapsed = 0;
        eating = null;
    }

    private void Use(Item item)
    {
        if (item.GetComponent<Food>())
        {
            StartCoroutine(Eat(item as Food));
            inspected.Add(item.name);
            pickUpTarget = null;
            fov.target = null;
            pickUpTimeElapsed = 0;
            return;
        }
        else if (item.GetComponent<AmmoBox>())
            item.AddToInventory();
        else
            item.Equip();
        if (item.transform.parent)
            if (item.transform.parent.gameObject.GetComponent<Container>())
                item.transform.parent.gameObject.GetComponent<Container>().NextItem();
        inspected.Add(item.name);
        pickUpTarget = null;
        fov.target = null;
        pickUpTimeElapsed = 0;
    }

    private void CaptureInput()
    {
        switch (gameManager.gameState)
        {
            case GameManager.GameState.Playing:
                if (actionState == ActionState.Inspecting)
                {
                    if (Input.GetButtonDown("Submit"))
                    {
                        PickUp();
                    }
                    else if (Input.GetButtonDown("Use"))
                    {
                        Use(pickUpTarget);
                    }
                    else if (Input.GetButtonDown("Cancel"))
                    {
                        LeaveBehind();
                    }
                    break;
                }

                input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                input = Vector2.ClampMagnitude(input, 1);

                if (Input.GetMouseButton(1) || Input.GetAxis("Aim") > 0)
                {
                    movementState = MovementState.Holding;
                    if (actionState != ActionState.Reloading)
                        actionState = ActionState.Aiming;
                }
                else
                {
                    if (actionState == ActionState.Aiming)
                        actionState = ActionState.Idle;
                    if (isMoving)
                    {
                        if (Input.GetButton("Run"))
                        {
                            if (vitals.stamina > 1)
                                movementState = MovementState.Running;
                            else
                                movementState = MovementState.Walking;
                            actionState = ActionState.Idle;
                        }
                        else if (Input.GetButton("Crouch"))
                            movementState = MovementState.CrouchWalking;
                        else
                            movementState = MovementState.Walking;
                    }
                    else if (Input.GetButton("Crouch"))
                        movementState = MovementState.Crouching;
                    else
                        movementState = MovementState.Idle;
                }

                if (input.magnitude > 0)
                {
                    navMeshAgent.ResetPath();
                    pickUpTarget = null;
                    intent = camForward * input.y + camRight * input.x;
                    if (intent != Vector3.zero)
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(intent), turnSpeed * Time.deltaTime);
                    if (actionState == ActionState.PickingUp)
                    {
                        actionState = ActionState.Idle;
                        pickUpTarget = null;
                    }
                }
                else
                {
                    if (Input.GetButton("PickUp"))
                    {
                        if (fov.target && actionState != ActionState.Aiming)
                        {
                            actionState = ActionState.Idle;
                            if (fov.target.GetComponent<Item>())
                            {
                                pickUpTarget = fov.target.GetComponent<Item>();
                                navTarget = pickUpTarget.gameObject;
                            }
                            if (fov.target.GetComponent<Container>())
                            {
                                navTarget = fov.target;
                            }
                        }
                        if (navTarget)
                            navMeshAgent.destination = navTarget.transform.position;
                    }
                }

                if (Input.GetButtonDown("PickUp"))
                    if (fov.target)
                    {
                        if (fov.target.name == "Door")
                        {
                            actionState = ActionState.Idle;
                            fov.target.GetComponent<Door>().Interact();
                        }
                    }

                if (navTarget)
                    if (Vector3.Distance(transform.position, navTarget.transform.position) < grabDistance)
                    {
                        navMeshAgent.ResetPath();
                        if (pickUpTarget)
                            if (navTarget == pickUpTarget.gameObject)
                                actionState = ActionState.PickingUp;
                        if (navTarget.GetComponent<Container>())
                            if (Input.GetButton("PickUp"))
                                if (isMoving == false)
                                    Search(navTarget.GetComponent<Container>());
                    }
                break;
            case GameManager.GameState.Paused:
                break;
        }       
    }

    private void Search(Container container)
    {
        if (container.searched == false)
        {
            searchTimeElapsed += Time.deltaTime;
            if (searchTimeElapsed >= container.searchTime)
            {
                container.Open();
                searchTimeElapsed = 0;
            }
        }
        if (container.searched == true && container.contents.Count > 0)
        {
            container.Open();
        }
    }

    private void CalculateCamera()
    {
        camForward = cam.transform.forward;
        camRight = cam.transform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward = camForward.normalized;
        camRight = camRight.normalized;
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    public void TakeDamage(float damage)
    {
        vitals.health -= damage;
        vitals.maxHealth -= damage / 10;
    }

    private void CalculateFoodInInventory()
    {
        caloriesInInventory = 0;
        millilitersInInventory = 0;
        if (items.Count > 0)
        {
            foreach (Item item in items)
            {
                if (item.GetComponent<Food>())
                {
                    Food food = item.GetComponent<Food>();
                    caloriesInInventory += food.calories;
                    millilitersInInventory += food.milliliters;
                }
            }
        }
    }

    private void OnApplicationQuit()
    {
        CheckDanger();
        Save();
    }

    private void Save()
    {
        ES3.Save("playerDanger", danger);
        ES3.Save("playerPosition", transform.position);
        ES3.Save("playerRotation", transform.rotation);
        ES3.Save("playerRangedWeaponEquipped", rangedWeaponEquipped);
        ES3.Save("playerRoundChambered", roundChambered);
        ES3.Save("playerPistolAmmo", pistolAmmo);
        ES3.Save("playerRifleAmmo", rifleAmmo);
        ES3.Save("playerMeleeWeaponEquipped", meleeWeaponEquipped);
        ES3.Save("playerItems", items);
        ES3.Save("playerItemSelected", itemSelected);
        ES3.Save("backpackEquipped", backpackEquipped);
        ES3.Save("inspected", inspected);
    }

    private void CheckDanger()
    {
        float dangerRadius = 5;
        Collider[] hitZombies = Physics.OverlapSphere(transform.position, dangerRadius, 1 << LayerMask.NameToLayer("Zombie"));
        if (hitZombies.Length > 0)
        {
            foreach (Collider zombie in hitZombies)
                danger += 10;
        }
    }
}
