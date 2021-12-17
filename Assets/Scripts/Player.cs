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
    NavMeshAgent navMeshAgent;
    public PlayerVitals vitals;
    public FieldOfView fov;
    Camera cam;

    float pickUpTime = .25f;
    float pickUpTimeElapsed;
    public GameObject pickUpTarget;
    public GameObject target;

    public float speed;
    float walkSpeed = 3;
    float runSpeed = 6;
    float crouchSpeed = 1.5f;
    float acceleration = 50;
    float turnSpeedLow = 7;
    float turnSpeedHigh = 15;
    float grabDistance = 1.4f;
    float idleRadius = 1;
    float walkRadius = 5;
    float runRadius = 10;
    float crouchRadius = 2;
    float noiseSphereRadius;

    public bool hasRangedWeapon;
    public float rangedAttackDamage;
    public float rangedAttackSpeed;
    public float rangedAttackNoise;
    public float rangedAttackRange;
    public float rangedKnockback;
    public bool fullAuto;
    public bool semiAuto;
    public bool boltAction;
    public int magazineSize;
    public float reloadTime = 1;
    public float aimTime;
    float rangedAttackCooldown;
    public float reloadTimeElapsed;
    public float aimTimeElapsed;
    public GameObject rangedWeaponEquipped;
    bool roundChambered;
    public bool weaponChanged = false;
    public int pistolAmmo;
    public int rifleAmmo;

    public bool hasMeleeWeapon;
    public float meleeAttackDamage;
    public float meleeAttackSpeed;
    public float meleeAttackNoise;
    public float meleeAttackRange;
    public float meleeKnockback;
    float meleeAttackCooldown;
    public GameObject meleeWeaponEquipped;

    public List<GameObject> rangedWeapons;
    public List<GameObject> meleeWeapons;
    public List<GameObject> items;

    public bool itemSelectionChanged;

    public GameObject itemSelected;
    public float eatingTime = 1;
    public float eatingTimeElapsed;

    Vector2 input;
    Vector3 camForward;
    Vector3 camRight;
    Vector3 intent;
    Vector3 velocity;
    float turnSpeed;
    Vector3 currentPos;
    Vector3 lastPos;
    float pulseTime = .5f;

    float fovRadius = 4;
    float fovAngle = 250;

    public enum MovementState { Idle, Walking, Running, Crouching, Holding }
    public MovementState movementState;
    public enum ActionState { Idle, Reloading, Aiming, PickingUp, Eating }
    public ActionState actionState;

    private void Awake() { Instance = this; }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        vitals = GetComponent<PlayerVitals>();
        fov = GetComponent<FieldOfView>();
        fov.radius = fovRadius;
        fov.angle = fovAngle;
        fov.targetMask = LayerMask.GetMask("Interactable");
        cam = Camera.main;
        navMeshAgent.speed = walkSpeed;
        navMeshAgent.acceleration = acceleration;
        hasMeleeWeapon = false;
        hasRangedWeapon = false;

        movementState = MovementState.Idle;
        StartCoroutine(EmitNoisePulse());
    }

    private void Update()
    {
        CaptureInput();
        CalculateCamera();
        MovementStateMachine();
        ActionStateMachine();

        rangedAttackCooldown -= Time.deltaTime;
        meleeAttackCooldown -= Time.deltaTime;

        velocity = Vector3.Lerp(velocity, transform.forward * input.magnitude * speed, acceleration * Time.deltaTime);

        navMeshAgent.Move(velocity*Time.deltaTime);

        turnSpeed = Mathf.Lerp(turnSpeedHigh, turnSpeedLow, velocity.magnitude / 5);

        navMeshAgent.speed = speed;

        if (input.magnitude > 0)
        {
            navMeshAgent.ResetPath();
            pickUpTarget = null;
            intent = camForward * input.y + camRight * input.x;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(intent), turnSpeed * Time.deltaTime);
        }
        else
        {
            if (Input.GetButton("PickUp"))
            {
                if (fov.target && actionState != ActionState.Aiming)
                {
                    actionState = ActionState.Idle;
                    if (fov.target.name != "Door")
                        pickUpTarget = fov.target;
                }
                if (pickUpTarget)
                    navMeshAgent.destination = pickUpTarget.transform.position;
            }
        }

        if (Input.GetButtonDown("PickUp"))
            if (fov.target)
                if (fov.target.name == "Door")
                {
                    actionState = ActionState.Idle;
                    fov.target.GetComponent<Door>().Interact();
                }

        if (pickUpTarget)
            if (Vector3.Distance(transform.position, pickUpTarget.transform.position) < grabDistance)
                actionState = ActionState.PickingUp;
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
                speed = crouchSpeed;
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
                if (Input.GetMouseButton(0) || Input.GetAxis("Fire") > 0)
                    MeleeAttack();
                if (Input.GetAxis("ChangeWeapon") < 0)
                {
                    if (weaponChanged == false)
                        ChangeMeleeWeapon();
                }
                else if (Input.GetAxis("ChangeWeapon") > 0)
                {
                    if (weaponChanged == false)
                    ChangeRangedWeapon();
                }
                else
                    weaponChanged = false;
                if (Input.GetAxis("Inventory") < 0)
                {
                    if (itemSelectionChanged == false)
                        ChangeItemSelectedDown();
                }
                else if (Input.GetAxis("Inventory") > 0)
                {
                    if (itemSelectionChanged == false)
                        ChangeItemSelectedUp();
                }
                else
                    itemSelectionChanged = false;
                if (Input.GetButtonDown("Eat"))
                    if (itemSelected)
                        if (itemSelected.GetComponent<Food>() != null)
                            actionState = ActionState.Eating;
                break;
            case ActionState.Reloading:
                aimTimeElapsed = 0;
                target = null;
                reloadTimeElapsed += Time.deltaTime;
                if (reloadTimeElapsed >= reloadTime)
                {
                    if (rangedWeaponEquipped.name == "Pistol" && pistolAmmo >= magazineSize - rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine)
                    {
                        pistolAmmo -= magazineSize - rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine;
                        rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine = magazineSize;
                    }
                    else if (rangedWeaponEquipped.name == "Pistol" && pistolAmmo < magazineSize - rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine)
                    {
                        rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine += pistolAmmo;
                        pistolAmmo = 0;
                    }
                    if (rangedWeaponEquipped.name == "Rifle" && rifleAmmo >= magazineSize - rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine)
                    {
                        rifleAmmo -= magazineSize - rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine;
                        rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine = magazineSize;
                    }
                    else if (rangedWeaponEquipped.name == "Rifle" && rifleAmmo < magazineSize - rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine)
                    {
                        rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine += rifleAmmo;
                        rifleAmmo = 0;
                    }
                    reloadTimeElapsed = 0;
                    actionState = ActionState.Idle;
                }
                break;
            case ActionState.Aiming:
                movementState = MovementState.Holding;
                pickUpTarget = null;
                pickUpTimeElapsed = 0;
                fov.radius = rangedAttackRange;
                fov.angle = 45;
                if (Input.GetButtonDown("Reload"))
                    if (rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine < magazineSize)
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
                if (rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine > 0)
                {
                    if (target)
                        if (aimTimeElapsed < aimTime)
                            aimTimeElapsed += Time.deltaTime;
                }
                else
                {
                    aimTimeElapsed = 0;
                    if (Input.GetMouseButton(0) || Input.GetAxis("Fire") > 0)
                        if (rangedWeaponEquipped.name == "Pistol" && pistolAmmo > 0)
                            actionState = ActionState.Reloading;
                        else if (rangedWeaponEquipped.name == "Rifle" && rifleAmmo > 0)
                            actionState = ActionState.Reloading;
                }
                if (Input.GetMouseButton(0) || Input.GetAxis("Fire") > 0)
                {
                    if (target)
                        if (roundChambered || fullAuto)
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
                aimTimeElapsed = 0;
                reloadTimeElapsed = 0;
                movementState = MovementState.Holding;
                pickUpTimeElapsed += Time.deltaTime;
                if (pickUpTimeElapsed >= pickUpTime)
                {
                    if (pickUpTarget)
                        pickUpTarget.GetComponent<IPickUpable>().PickUp();
                    pickUpTarget = null;
                    pickUpTimeElapsed = 0;
                    actionState = ActionState.Idle;
                }
                break;
            case ActionState.Eating:
                aimTimeElapsed = 0;
                reloadTimeElapsed = 0;
                movementState = MovementState.Holding;
                pickUpTarget = null;
                target = null;
                eatingTimeElapsed += Time.deltaTime;
                if (eatingTimeElapsed >= eatingTime)
                {
                    if (itemSelected)
                    {
                        itemSelected.GetComponent<Food>().Eat();
                        int index = items.IndexOf(itemSelected);
                        items.Remove(itemSelected);
                        if (items.Count > 0)
                            itemSelected = items[0];
                        if (items.Count <= 0)
                            itemSelected = null;
                    }
                    eatingTimeElapsed = 0;
                    actionState = ActionState.Idle;
                }
                break;
        }
    }

    private void ChangeMeleeWeapon()
    {
        if (hasMeleeWeapon)
        {
            int i = meleeWeapons.IndexOf(meleeWeaponEquipped);
            if (i == meleeWeapons.Count - 1)
                i = -1;
            meleeWeapons[i + 1].GetComponent<MeleeWeapon>().Equip();
            weaponChanged = true;
        }
    }

    private void ChangeRangedWeapon()
    {
        if (hasRangedWeapon)
        {
            int i = rangedWeapons.IndexOf(rangedWeaponEquipped);
            if (i == rangedWeapons.Count - 1)
                i = -1;
            rangedWeapons[i + 1].GetComponent<RangedWeapon>().Equip();
            weaponChanged = true;
        }
    }

    private void ChangeItemSelectedUp()
    {
        if (items.Count > 0)
        {
            int i = items.IndexOf(itemSelected);
            if (i == items.Count - 1)
                i = -1;
            itemSelected = items[i + 1];
            itemSelectionChanged = true;
        }
    }

    private void ChangeItemSelectedDown()
    {
        if (items.Count > 0)
        {
            int i = items.IndexOf(itemSelected);
            if (i == 0)
                i = items.Count;
            itemSelected = items[i - 1];
            itemSelectionChanged = true;
        }
    }

    private void MeleeAttack()
    {
        if (hasMeleeWeapon && meleeAttackCooldown <= 0)
        {
            Collider[] hitZombies = Physics.OverlapSphere(transform.position + transform.forward, meleeAttackRange, 1 << LayerMask.NameToLayer("Zombie"));
            if (hitZombies.Length > 0)
            {
                foreach (Collider zombie in hitZombies)
                {
                    zombie.gameObject.GetComponent<IDamageable<float>>().TakeDamage(meleeAttackDamage);
                    zombie.gameObject.GetComponent<NavMeshAgent>().Move((zombie.transform.position - transform.position).normalized * meleeKnockback);
                }
            }
            EmitNoiseUnique(meleeAttackNoise);
            meleeAttackCooldown = meleeAttackSpeed;
        }
    }

    private void RangedAttack(GameObject target)
    {
        if (aimTimeElapsed >= aimTime && rangedAttackCooldown <= 0)
        {
            if (rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine > 0)
            {
                roundChambered = false;
                target.GetComponent<IDamageable<float>>().TakeDamage(rangedAttackDamage);
                target.GetComponent<NavMeshAgent>().Move((target.transform.position - transform.position).normalized * rangedKnockback);
                EmitNoiseUnique(rangedAttackNoise);
                rangedWeaponEquipped.GetComponent<RangedWeapon>().inMagazine -= 1;
                rangedAttackCooldown = rangedAttackSpeed;
                if (boltAction)
                    aimTimeElapsed = 0;
            }
        }
    }

    private bool IsMoving()
    {
        currentPos = transform.position;
        bool isMoving = (currentPos != lastPos);
        lastPos = currentPos;
        return isMoving;
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

    private void CaptureInput()
    {
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
            if (IsMoving())
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
                    movementState = MovementState.Crouching;
                else
                    movementState = MovementState.Walking;
            }
            else
                movementState = MovementState.Idle;
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

    public void Eat(float cals)
    {
        vitals.calories += cals;
    }

    public void TakeDamage(float damage)
    {
        vitals.health -= damage;
        vitals.maxHealth -= damage / 10;
    }
}
