using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerControl : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform cameraTransform;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalClamp = 80f;

    [Header("Bug Spray")]
    [SerializeField] private float sprayRange = 2f;
    [SerializeField] private float sprayRadius = 0.5f;
    [SerializeField] private float sprayCooldown = 0.3f;
    [SerializeField] private LayerMask antMask;
    [SerializeField] private ParticleSystem sprayVFX;

    [Header("Gel Trap")]
    [SerializeField] private GameObject gelTrapPrefab;
    [SerializeField] private int maxGelTraps = 3;
    [SerializeField] private float gelFreezeDuration = 10f;

    [Header("Borax")]
    [SerializeField] private GameObject boraxPrefab;
    [SerializeField] private int maxBoraxUses = 2;
    [SerializeField] private float boraxRadius = 3f;
    [SerializeField] private float boraxKillChance = 0.1f;
    [SerializeField] private float boraxDelay = 5f;

    [Header("Tracker")]
    [SerializeField] private LineRenderer trackerLinePrefab;
    [SerializeField] private float trackerDuration = 8f;
    [SerializeField] private float trackerCooldown = 30f;

    private CharacterController cc;
    private float verticalLookAngle;
    private Vector3 velocity;

    private float sprayTimer;
    private int gelTrapsLeft;
    private int boraxUsesLeft;
    private float trackerCooldownTimer;

    private List<LineRenderer> activeTrackerLines = new List<LineRenderer>();

    private void Awake()
    {
        //TESTING PURPOSES:
        trackerLinePrefab = new LineRenderer();
        cc = GetComponent<CharacterController>();
        gelTrapsLeft = maxGelTraps;
        boraxUsesLeft = maxBoraxUses;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        //if (!GameWorld.Instance.MatchActive) return;

        HandleLook();
        HandleMovement();
        HandleWeapons();
        HandleAbilities();
        UpdateTimers();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalLookAngle -= mouseY;
        verticalLookAngle = Mathf.Clamp(verticalLookAngle, -verticalClamp, verticalClamp);
        cameraTransform.localEulerAngles = new Vector3(verticalLookAngle, 0f, 0f);
    }

    private void HandleMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        if (move.magnitude > 1f) move.Normalize();

        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        cc.Move((move * speed + velocity) * Time.deltaTime);
    }

    private void HandleWeapons()
    {
        if (Input.GetMouseButton(0) && sprayTimer <= 0f)
        {
            FireSpray();
            sprayTimer = sprayCooldown;
        }

        if (Input.GetKeyDown(KeyCode.Q) && gelTrapsLeft > 0)
            PlaceGelTrap();

        if (Input.GetKeyDown(KeyCode.E) && boraxUsesLeft > 0)
            PlaceBorax();
    }

    private void FireSpray()
    {
        if (sprayVFX != null) sprayVFX.Play();

        Vector3 origin = cameraTransform.position;
        Vector3 dir = cameraTransform.forward;
        Vector3 endpoint = Physics.Raycast(origin, dir, out RaycastHit wallHit, sprayRange)
                           ? wallHit.point
                           : origin + dir * sprayRange;

        Collider[] hits = Physics.OverlapSphere(endpoint, sprayRadius, antMask);
        foreach (var c in hits)
            c.GetComponent<AntAI>()?.GetCaught();
    }

    private void PlaceGelTrap()
    {
        if (!Physics.Raycast(cameraTransform.position, Vector3.down, out RaycastHit hit, 3f)) return;

        GameObject trap = Instantiate(gelTrapPrefab, hit.point, Quaternion.identity);
        trap.GetComponent<GelTrap>()?.Init(gelFreezeDuration);
        gelTrapsLeft--;
    }

    private void PlaceBorax()
    {
        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, 4f)) return;

        GameObject bx = Instantiate(boraxPrefab, hit.point, Quaternion.identity);
        bx.GetComponent<BoraxTrap>()?.Init(boraxRadius, boraxKillChance, boraxDelay, antMask);
        boraxUsesLeft--;
    }

    private void HandleAbilities()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && trackerCooldownTimer <= 0f)
            ActivateTracker();
    }

    public void ActivateTracker()
    {
        StartCoroutine(TrackerRoutine());
        trackerCooldownTimer = trackerCooldown;
    }

    private IEnumerator TrackerRoutine()
    {
        foreach (var lr in activeTrackerLines)
            if (lr != null) Destroy(lr.gameObject);
        activeTrackerLines.Clear();

        AntAI[] ants = FindObjectsOfType<AntAI>();
        foreach (var ant in ants)
        {
            if (!ant.gameObject.activeInHierarchy) continue;
            LineRenderer lr = Instantiate(trackerLinePrefab);
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position + Vector3.up);
            lr.SetPosition(1, ant.transform.position);
            activeTrackerLines.Add(lr);
        }

        yield return new WaitForSeconds(trackerDuration);

        foreach (var lr in activeTrackerLines)
            if (lr != null) Destroy(lr.gameObject);
        activeTrackerLines.Clear();
    }

    private void UpdateTimers()
    {
        if (sprayTimer > 0f) sprayTimer -= Time.deltaTime;
        if (trackerCooldownTimer > 0f) trackerCooldownTimer -= Time.deltaTime;
    }

    public int GelTrapsLeft => gelTrapsLeft;
    public int BoraxUsesLeft => boraxUsesLeft;
    public float TrackerCD => Mathf.Max(0f, trackerCooldownTimer);
}