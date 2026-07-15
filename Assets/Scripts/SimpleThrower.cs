using UnityEngine;

public class SimpleThrower : MonoBehaviour
{
    [Header("Configuration")]
    public OVRInput.Controller controller; // Choose LTouch or RTouch
    public float grabRadius = 0.15f;       // How close hand must be to grab
    public LayerMask grabLayer;            // Set to "Dart" layer
    public Transform handAnchor;           // Where the dart snaps to

    [Header("Adjustments")]
    // NEW: Change this if your dart points down/up when grabbed. 
    // Try setting X to 90 or -90 in the Inspector.
    public Vector3 grabRotationOffset = new Vector3(0, 0, 0); 
    
    [Header("Physics Tuning")]
    public float velocityMultiplier = 1.3f; // 1.3x force feels more realistic
    
    private DartProjectile currentDart;
    private Vector3[] velocityBuffer = new Vector3[5]; // Stores last 5 frames of movement
    private int bufferIndex = 0;
    private Vector3 lastPos;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        // 1. Calculate Velocity Manually (Smoother than OVR built-in velocity)
        Vector3 currentVel = (transform.position - lastPos) / Time.deltaTime;
        velocityBuffer[bufferIndex] = currentVel;
        bufferIndex = (bufferIndex + 1) % velocityBuffer.Length;
        lastPos = transform.position;

        // 2. CHECK GRAB (Grip Button Down)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller))
        {
            TryGrab();
        }

        // 3. CHECK THROW (Grip Button Released)
        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller))
        {
            if (currentDart != null)
            {
                Throw();
            }
        }
    }

    // Average the last 5 frames to avoid "jittery" throws
    Vector3 GetSmoothedVelocity()
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 v in velocityBuffer) sum += v;
        return sum / velocityBuffer.Length;
    }

    void TryGrab()
    {
        // Create an invisible sphere around hand to find darts
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRadius, grabLayer);
        foreach (var hit in hits)
        {
            // Look for the script on the parent object
            DartProjectile dart = hit.GetComponentInParent<DartProjectile>();
            if (dart != null)
            {
                currentDart = dart;
                
                // Disable physics so it doesn't fall out of hand
                currentDart.GetComponent<Rigidbody>().isKinematic = true;
                
                // Snap to hand
                currentDart.transform.SetParent(handAnchor);
                currentDart.transform.localPosition = Vector3.zero;

                // NEW CODE IS HERE: Apply the manual rotation fix
                currentDart.transform.localRotation = Quaternion.Euler(grabRotationOffset);

                return; // Grab the first one we find
            }
        }
    }

    void Throw()
    {
        // Apply the smoothed velocity + boost
        Vector3 finalVel = GetSmoothedVelocity() * velocityMultiplier;
        Vector3 angVel = OVRInput.GetLocalControllerAngularVelocity(controller);

        currentDart.Release(finalVel, angVel);
        currentDart = null; // Hand is empty
    }
}