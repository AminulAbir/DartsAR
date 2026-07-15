using UnityEngine;

public class DartProjectile : MonoBehaviour
{
    [Header("Settings")]
    public Transform tipPosition; // We will drag the Tip object here
    public LayerMask boardLayer;  // We will set this to "Dartboard"

    private Rigidbody rb;
    private bool isStuck = false;
    private bool isThrown = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Move Center of Mass forward so it doesn't flip in air
        rb.centerOfMass = new Vector3(0, 0, 0.05f); 
    }

    void FixedUpdate()
    {
        // Aerodynamics: Rotate to face forward if moving fast
        //if (isThrown && !isStuck && rb.velocity.sqrMagnitude > 0.5f)
        //{
        //    Quaternion targetRotation = Quaternion.LookRotation(rb.velocity);
        //    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        //}
    }

    void OnCollisionEnter(Collision collision)
    {
        // DEBUG LINE: Print what we hit!
        Debug.Log("I hit: " + collision.gameObject.name);
        if (isStuck || !isThrown) return;

        // Check if we hit the dartboard
        if (collision.gameObject.CompareTag("Dartboard"))
        {
            StickToBoard(collision);
        }
        else
        {
            isThrown = false; // Hit the floor/wall, stop rotating
        }
    }

    void StickToBoard(Collision collision)
    {
        isStuck = true;
        isThrown = false;

        // Stop Physics
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Parent to board
        transform.SetParent(collision.transform);

        // Send Score
        // Note: We look for the script on the parent or the object itself
        DartboardLogic board = collision.gameObject.GetComponentInParent<DartboardLogic>();
        if (board != null)
        {
            board.ProcessHit(tipPosition.position);
        }
    }

    public void Release(Vector3 velocity, Vector3 angularVelocity)
    {
        isStuck = false;
        isThrown = true;
        rb.isKinematic = false;
        transform.SetParent(null); 
        
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;
    }
}