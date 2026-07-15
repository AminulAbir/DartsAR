using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DartboardLogic : MonoBehaviour
{
    [Header("UI Connection")]
    public TextMeshProUGUI scoreText;

    [Header("Logging")]
    // Drag your DataManager object here!
    public Logging.LoggerScript logger;

    [Header("Spawning Settings")]
    public GameObject dartPrefab;   // Drag your Dart Prefab here
    public Transform spawnPoint;    // Drag your "DartSpawnPoint" here
    public int maxThrows = 15;      // Set to 15
    public float dartSpacing = 0.1f; // 10cm space between darts

    [Header("Adjustments")]
    // If scores are one slice off, adjust this by +/- 18
    public float rotationAngleOffset = 0f;

    [Header("Board Dimensions (in Unity Units)")]
    // UPDATED WITH YOUR EXACT MEASUREMENTS:
    public float bullseyeRadius = 0.012f;
    public float bullseyeOuterRadius = 0.022f; 
    public float tripleRingInner = 0.11f;   // Inner Triple
    public float tripleRingOuter = 0.125f;  // Outer Triple
    public float doubleRingInner = 0.185f;  // Inner Double
    public float doubleRingOuter = 0.20f;   // Outer Double (Fixed from 0.185 to 0.2)
    
    // SCORE MAP
    private int[] scoreMap = new int[] { 11, 14, 9, 12, 5, 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8 };
    
    private int currentTotalScore = 0;
    private int throwsCount = 0;

    void Start()
    {
        SpawnDarts();

        // Auto-start logging when the game loads
        if (logger != null)
        {
            logger.StartDefaultSession();
            Debug.Log("DartboardLogic: Requested Logger Start.");
        }
    }

    void SpawnDarts()
    {
        if (dartPrefab == null || spawnPoint == null)
        {
            Debug.LogError("MISSING PREFAB OR SPAWN POINT IN DARTBOARD LOGIC!");
            return;
        }

        // Grid Logic: 5 darts per row (3 rows total for 15 darts)
        int cols = 5; 

        for (int i = 0; i < maxThrows; i++)
        {
            // Calculate grid position
            int row = i / cols;
            int col = i % cols;

            // Offset: Move them slightly apart so they don't stack
            // We use 'row * -dartSpacing' for Z to move them rows backwards
            Vector3 offset = new Vector3(col * dartSpacing, 0, row * dartSpacing);
            Vector3 spawnPos = spawnPoint.position + offset;

            // Instantiate (Create) the dart
            GameObject newDart = Instantiate(dartPrefab, spawnPos, spawnPoint.rotation);
            
            // --- SAFETY CHECK FOR LOGGING ---
            if (!newDart.CompareTag("Dart"))
            {
                Debug.LogError($"[FIX REQUIRED] The spawned dart '{newDart.name}' is Untagged! The Logger will NOT record it. Please select your Dart Prefab and set the Tag to 'Dart'.");
            }

            // Ensure Physics is Kinematic (Floating) so they don't fall instantly
            Rigidbody rb = newDart.GetComponent<Rigidbody>();
            if(rb) rb.isKinematic = true;
        }
    }

    public void ProcessHit(Vector3 worldHitPoint)
    {
        // Stop calculating if we exceeded max throws
        if (throwsCount >= maxThrows) return;

        throwsCount++;

        Vector3 localHit = transform.InverseTransformPoint(worldHitPoint);
        float distance = new Vector2(localHit.x, localHit.y).magnitude;
        
        float angle = Mathf.Atan2(localHit.y, localHit.x) * Mathf.Rad2Deg;
        angle += rotationAngleOffset;

        int hitPoints = 0;
        int multiplier = 1;
        string zone = "Miss";

        // --- SCORING LOGIC ---
        if (distance > doubleRingOuter) 
        { 
            hitPoints = 0; 
            zone = "Miss"; 
        }
        else if (distance <= bullseyeRadius) { hitPoints = 50; zone = "Bullseye"; }
        else if (distance <= bullseyeOuterRadius) { hitPoints = 25; zone = "Outer Bull"; }
        else
        {
            // Determine Ring Multiplier
            if (distance >= tripleRingInner && distance <= tripleRingOuter) 
            { 
                multiplier = 3; 
                zone = "Triple"; 
            }
            else if (distance >= doubleRingInner && distance <= doubleRingOuter) 
            { 
                multiplier = 2; 
                zone = "Double"; 
            }
            else 
            { 
                multiplier = 1; 
                zone = "Single"; 
            }

            // Determine Score Slice
            if (angle < 0) angle += 360f;
            float shiftedAngle = angle + 9f;
            
            while (shiftedAngle >= 360f) shiftedAngle -= 360f;
            while (shiftedAngle < 0f) shiftedAngle += 360f;

            int sliceIndex = Mathf.FloorToInt(shiftedAngle / 18f);
            
            if(sliceIndex < 0) sliceIndex = 0;
            if(sliceIndex >= scoreMap.Length) sliceIndex = 0;

            hitPoints = scoreMap[sliceIndex] * multiplier;
        }

        // --- UPDATE TOTAL ---
        currentTotalScore += hitPoints;
        
        // --- LOG THE EVENT ---
        if (logger != null)
        {
            // Example Output: "Hit_Triple_60" or "Hit_Miss_0"
            string eventName = $"Hit_{zone}_{hitPoints}";
            logger.AddEvent(eventName);
            Debug.Log($"[Logger] Logged Event: {eventName}");
        }

        // --- UPDATE UI ---
        if (scoreText != null)
        {
            scoreText.text = $"Total: {currentTotalScore}\nHit: {zone} {hitPoints}";
        }

        // --- GAME OVER CHECK ---
        if (throwsCount >= maxThrows)
        {
            if (scoreText != null) scoreText.text += "\n<color=yellow>FINISH!</color>";

            // Stop recording when the last dart hits the board.
            if (logger != null)
            {
                logger.StopLogging();
                Debug.Log("DartboardLogic: Max throws reached. Logger Stopped.");
            }
        }
    }
}