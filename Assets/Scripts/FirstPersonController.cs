using System;
using System.Collections;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;
    private bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool ShouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchingAnimation && characterController.isGrounded;

    [Header("Funtional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canHeadbob = true;
    [SerializeField] private bool willSlideOnSlope = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField]
    private bool hasFootsteps = true;


    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode zoomKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode interactKey = KeyCode.Mouse0;

    [Header("Health parameters")]
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float timeBeforeHealthRegen = 3;
  // [SerializeField] private float healthValueIncrement = 1;
    [SerializeField] private float healthTimeIncrement = 0.1f;
    private float currentHealth;
    private Coroutine regeneratingHealth;
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;



    [Header("Movement Controller")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 10.0f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float slopeSpeed = 8.0f;



    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 15.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouching Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0f, 0);
    private bool isCrouching;
    private bool duringCrouchingAnimation;


    [Header("Headbob parameters")]
    [SerializeField] private float walkBobSpeed = 14.0f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18.0f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 8.0f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;


    [Header("Zoom parameters")]
    [SerializeField] private float timeToZoom = 0.3f;
    [SerializeField] private float zoomFOV = 30.0f;
    private float defaultFOV;
    private Coroutine zoomRoutine;

    [Header("Foot step parameter")]
    [SerializeField] private float baseStepSpeed = 0.3f;
    [SerializeField] private float crouchStepmultiplier = 0.05f;
    [SerializeField] private float sprintStepmultiplier = 0.2f;
    [SerializeField] private AudioSource footStepAudio = default;//to avoid error in the begining when debugging
    [SerializeField] private AudioClip[] woodClips = default;
    [SerializeField] private AudioClip[] metalClips = default;
    [SerializeField] private AudioClip[] grassClips = default;
    //[SerializeField] private AudioClip[] concreteClips = default;

    private float footstepTimer = 0;
    private float GetCurrentOffset => isCrouching ? baseStepSpeed * crouchStepmultiplier : IsSprinting ? baseStepSpeed * sprintStepmultiplier : baseStepSpeed;

    //Sliding Parameter

    private Vector3 hitPointNormal;
    private bool IsSliding
    {
        get
        {
            if (Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit)

            {

                canJump = false;

            }

            else { canJump = true; }

            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 3f))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;

            }
            else
            {
                return false;
            }
        }
    }

    //Sliding Parameter
    [Header("Interaction")]
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    [SerializeField] private LayerMask interactionLayer = default;
    private Interactables currentInteractables;


    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;

    public static FirstPersonController instance;

    private void OnEnable()
    {
        OnTakeDamage += ApplyDamage;
    }

    private void OnDisable()
    {
        OnTakeDamage -= ApplyDamage;
    }

    void Awake()
    {
        currentHealth = maxHealth;
        instance = this;
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYPos = playerCamera.transform.localPosition.y;
        defaultFOV = playerCamera.fieldOfView;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (CanMove)
        {
            HandleKeyboardInput();
            HandleMouseInput();
            if (canJump)
                HandleJump();
            if (canCrouch)
                HandleCrouch();
            if (canHeadbob)
                HandleHeadBob();
            if (canZoom)
                HandleZoom();

            if (hasFootsteps)
                HandFootsteps();

            if (canInteract)
            {
                HandleInteractionCheck();
                HandleInteractionInput();
            }
            ApplyFinalMovement();
        }
    }

    private void HandFootsteps()
    {
        if (!characterController.isGrounded) return;
        if (currentInput == Vector2.zero) return;
        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0)
        {
            if (Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 3))
            {
                switch (hit.collider.tag)
                {
                    case "footstep/grass":
                        footStepAudio.PlayOneShot(grassClips[UnityEngine.Random.Range(0, grassClips.Length-1)]);
                        break;

                    case "footstep/metal":
                        footStepAudio.PlayOneShot(metalClips[UnityEngine.Random.Range(0, metalClips.Length - 1)]);
                        break;

                    case "footstep/wood":
                        footStepAudio.PlayOneShot(woodClips[UnityEngine.Random.Range(0, woodClips.Length - 1)]);
                        break;
                    default:
                        footStepAudio.PlayOneShot(woodClips[UnityEngine.Random.Range(0, woodClips.Length - 1)]);
                        break;


                }
            }
            footstepTimer = GetCurrentOffset;
        }
    }
    private void ApplyDamage(float damage) 
    {
        currentHealth -= damage;

        OnDamage?.Invoke(currentHealth);

        if (currentHealth <= 0)
            KillPlayer();
        else if (regeneratingHealth != null)
            StopCoroutine(regeneratingHealth);

        regeneratingHealth= StartCoroutine(RegenerateHealth());
    }

    private void KillPlayer()
    {
        currentHealth = 0;
        if (regeneratingHealth != null)
            StopCoroutine(RegenerateHealth());

        print("Dead");
    }
    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;
        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
                playerCamera.transform.localPosition.z);
        }
    }

    private void HandleKeyboardInput()
    {
        currentInput = new Vector2((isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));
        float moveDirectionY = moveDirection.y;

        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }
    private void HandleMouseInput()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void HandleZoom()
    {
        if (Input.GetKeyDown(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(true));
        }
        if (Input.GetKeyUp(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(false));
        }
    }

    private void HandleInteractionCheck()
    {
        if (Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.gameObject.layer == 9 && (currentInteractables == null || hit.collider.gameObject.GetInstanceID() != currentInteractables.GetInstanceID()))
            {
                hit.collider.TryGetComponent<Interactables>(out currentInteractables);
                if (currentInteractables)
                    currentInteractables.OnFocus();
            }
        }
        else if (currentInteractables)
        {
            currentInteractables.OnLoseFocus();
            currentInteractables = null;
        }
    }
    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractables != null && Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayer))
        {
            currentInteractables.OnInteract();
        }
    }
    private void ApplyFinalMovement()
    {
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        if (willSlideOnSlope && IsSliding)
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (ShouldJump)
            moveDirection.y = jumpForce;
    }
    private void HandleCrouch()
    {
        if (ShouldCrouch)
            StartCoroutine(CrouchStand());
    }
    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 20f))
            yield break;
        duringCrouchingAnimation = true;

        float timeElapsed = 0;
        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchingAnimation = false;
    }

    private IEnumerator ToggleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float startingFOV = playerCamera.fieldOfView;
        float timeElapsed = 0;

        while (timeElapsed < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, timeElapsed / timeToZoom);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }
    private IEnumerator RegenerateHealth()
    {
        yield return new WaitForSeconds(timeBeforeHealthRegen);
        WaitForSeconds timeToWait = new WaitForSeconds(healthTimeIncrement);

        while (currentHealth < maxHealth)
        {
            currentHealth += healthTimeIncrement;

            if(currentHealth > maxHealth)
                currentHealth = maxHealth;

            OnHeal?.Invoke(currentHealth);
            yield return timeToWait;
        }
        regeneratingHealth = null;
    }


}