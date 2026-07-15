using UnityEngine;

public class HandThrower : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Drag the OVRHandPrefab (Visuals) here.")]
    public OVRHand hand;            
    [Tooltip("How close (in meters) the fingers must be to the dart to grab it.")]
    public float grabRadius = 0.1f;
    [Tooltip("LayerMask must be set to 'Dart' for this to work.")]
    public LayerMask grabLayer;     
    [Tooltip("Automatically finds the Index Finger Tip. Leave empty.")]
    public Transform grabPoint;     

    [Header("Adjustments")]
    [Tooltip("Rotates the dart in your hand. Try (90, 0, 0) or (-90, 0, 0) to point it forward.")]
    public Vector3 grabRotationOffset = new Vector3(0, 0, 0);
    
    [Tooltip("Slides the dart in your hand. Adjust Z to move grip from Tip (0) to Body (0.05) or Feather (>0.1).")]
    public Vector3 grabPositionOffset = new Vector3(0, 0, 0); 

    [Header("Physics Tuning")]
    [Tooltip("Multiplies throw speed. Higher = Faster Darts.")]
    public float velocityMultiplier = 4.0f; 
    
    private DartProjectile currentDart;
    private bool isHolding = false;
    
    // Smooth velocity variables
    private Vector3[] velocityBuffer = new Vector3[5]; 
    private int bufferIndex = 0;
    private Vector3 lastPos;

    // Helper to find the fingertip
    private OVRSkeleton skeleton;
    private bool isBoneFound = false;

    void Start()
    {
        // Attempt to find skeleton if hand is assigned
        if (hand != null)
        {
            skeleton = hand.GetComponent<OVRSkeleton>();
        }
        
        // Safety: We start with grabPoint as NULL. 
        // We will NOT default to 'transform' (Wrist) anymore. 
        // This prevents the "Ring Finger/Wrist" snap issue.
        grabPoint = null; 
    }

    void Update()
    {
        // 0. AUTO-DETECT PINCH POINT
        // We run this every frame until we successfully find the Index Tip
        if (hand != null && !isBoneFound)
        {
            if (skeleton != null && skeleton.IsInitialized && skeleton.Bones.Count > 0)
            {
                foreach (var bone in skeleton.Bones)
                {
                    if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                    {
                        grabPoint = bone.Transform;
                        isBoneFound = true;
                        // Debug.Log("HandThrower: FOUND INDEX TIP!");
                        break;
                    }
                }
            }
        }

        // 1. Calculate Velocity of the Hand/Finger
        if (grabPoint != null) 
        {
            // Initialize lastPos if it's the first frame we found the bone
            if (lastPos == Vector3.zero) lastPos = grabPoint.position;

            // Handle teleport jumps
            if (Vector3.Distance(grabPoint.position, lastPos) > 0.5f) lastPos = grabPoint.position;
            
            Vector3 currentVel = (grabPoint.position - lastPos) / Time.deltaTime;
            velocityBuffer[bufferIndex] = currentVel;
            bufferIndex = (bufferIndex + 1) % velocityBuffer.Length;
            lastPos = grabPoint.position;
        }

        // 2. CHECK PINCH STATE
        // Is the user pinching with Index Finger?
        // Note: We add 'isBoneFound' check to ensure we don't grab with a broken skeleton
        if (isBoneFound && hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
             if (!isHolding) TryGrab();
        }
        else if (isHolding)
        {
             Throw();
        }
    }

    Vector3 GetSmoothedVelocity()
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 v in velocityBuffer) sum += v;
        return sum / velocityBuffer.Length;
    }

    void TryGrab()
    {
        // STRICT SAFETY CHECK:
        // If we haven't found the finger bone yet, DO NOT GRAB.
        if (grabPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(grabPoint.position, grabRadius, grabLayer);
        foreach (var hit in hits)
        {
            DartProjectile dart = hit.GetComponentInParent<DartProjectile>();
            if (dart != null)
            {
                isHolding = true;
                currentDart = dart;
                
                currentDart.GetComponent<Rigidbody>().isKinematic = true;
                
                // Snap to the pinch point
                currentDart.transform.SetParent(grabPoint);
                // NEW: Use the offset here to slide the dart position
                currentDart.transform.localPosition = grabPositionOffset;
                currentDart.transform.localRotation = Quaternion.Euler(grabRotationOffset);
                return; 
            }
        }
    }

    void Throw()
    {
        if (currentDart != null)
        {
            // Throw with boosted velocity
            Vector3 finalVel = GetSmoothedVelocity() * velocityMultiplier;
            
            // Hands don't provide easy angular velocity, so we zero it out or fake it
            currentDart.Release(finalVel, Vector3.zero);
            
            currentDart = null;
        }
        isHolding = false;
    }
}