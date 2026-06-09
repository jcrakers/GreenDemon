using UnityEngine;
using System.IO;
using System.Collections;

namespace GreenDemon;

public class GreenDemonData : MonoBehaviour
{
    private static GameObject greenDemon;
    private static GameObject player;
    private static AudioSource audioSource;

    private static PlayerMovement playerMovement;
    private static Stats stats;

    private float chaseSpeed = 2f;

    private bool isChasing = false;

    public static void CreateGreenDemon(float x = 0, float y = 0, float z = 0)
    {
        greenDemon = Billboard.CreateBillboard(Path.Combine(GreenDemonPlugin.imagesPath, "GreenDemon.png"), 60, 59);
        greenDemon.transform.position = new Vector3(x, y, z);

        greenDemon.name = "GreenDemon";

        SphereCollider sphereCollider = greenDemon.AddComponent<SphereCollider>();
        sphereCollider.radius = 0.1f;
        sphereCollider.isTrigger = true;
        sphereCollider.enabled = false;
            
        Billboard billboard = greenDemon.GetComponent<Billboard>();
        billboard.mirrorCamera = true;
        billboard.levelY = false;
        billboard.GetComponent<MeshCollider>().enabled = false;

        player = GameObject.Find("Player");
        playerMovement = player.GetComponent<PlayerMovement>();
        stats = player.GetComponent<Stats>();

        audioSource = greenDemon.AddComponent<AudioSource>();
        greenDemon.AddComponent<GreenDemonData>();
    }

    void Start()
    {
        StartCoroutine(LoadAndPlayExternalAudio("GreenDemonSpawn.wav"));
        StartCoroutine(StartSpawnAnimation());
    }

    IEnumerator LoadAndPlayExternalAudio(string audioFileName)
    {
        // Build the absolute path to the file
        string fullPath = Path.Combine(GreenDemonPlugin.audioPath, audioFileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError("[GreenDemon] Audio file missing at: " + fullPath);
        yield break;
        }

        audioSource.minDistance = 15f;
        audioSource.maxDistance = 150f;

        audioSource.ignoreListenerVolume = true;

        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // For local files, the URI scheme must be formatted as "file://"
        string url = "file://" + fullPath;

        // Use Unity 4.6's WWW class to stream the audio file into memory
        using (WWW www = new WWW(url))
        {
            // Wait until the file is completely finished reading from disk
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError("[GreenDemon] Failed to stream audio: " + www.error);
            }
            else
            {
                // Assign the downloaded external clip to your source and play it!
                audioSource.clip = www.GetAudioClip(true, false); // arguments: (3D, stream)
                audioSource.Play();
            }
        }
    }    

    IEnumerator StartSpawnAnimation()
    {
        float duration = 0.7f;
        float timer = 0f;
        float flySpeed = 7f;

        while (timer < duration)
        {
            // Move the object upward along the Y axis
            greenDemon.transform.position += Vector3.up * flySpeed * Time.deltaTime;
            
            // Advance the timer by the time passed since the last frame
            timer += Time.deltaTime;
            
            // Wait until the next frame before continuing the loop
            yield return null;
        }

        StartCoroutine(SpinAnimation());
    }

    IEnumerator SpinAnimation()
    {
        float spinAmount = 3.4f;
        
        float rotationSpeed = 650f;
        float degreesToSpin = 360f * spinAmount;

        float totalDegrees = 0f;

        Vector3 pivotPoint = greenDemon.transform.position + Vector3.up * 1.4f;

        Vector3 verticalOrbitAxis = Camera.main.transform.position - greenDemon.transform.position;
        verticalOrbitAxis.y = 0f;
        verticalOrbitAxis.Normalize();
        
        while (totalDegrees < degreesToSpin)
        {
            float rotationAmount = rotationSpeed * Time.deltaTime;

            if (totalDegrees + rotationAmount > degreesToSpin)
            {
                rotationAmount = degreesToSpin - totalDegrees;
            }

            greenDemon.transform.RotateAround(pivotPoint, verticalOrbitAxis, rotationAmount);

            totalDegrees += rotationAmount;
            yield return null;
        }

        isChasing = true;
        greenDemon.GetComponent<SphereCollider>().enabled = true;
    }

    void OnTriggerEnter(Collider collider)
    {
        GreenDemonPlugin.Logger.LogInfo($"$GreenDemon OnTriggerEnter - instant death: {GreenDemonPlugin.InstantDeath}");
        // 1. Verify if the object entering the bubble is our cached player instance
        if (player != null && collider.gameObject == player)
        {
            GreenDemonPlugin.Logger.LogInfo($"$GreenDemon OnTriggerEnter - player entered bubble");
            if (GreenDemonPlugin.InstantDeath) 
            {
                stats.health = 0;
            }
            else
            {
                stats.StartCoroutine_Auto(stats.HurtPlayer(GreenDemonPlugin.DamageAmount));
            }
            
            Billboard billboard = GetComponent<Billboard>();
            if (billboard != null)
            {
                billboard.enabled = false;
                // If the billboard creates a separate child mesh object, hide its renderer:
                Renderer rend = GetComponentInChildren<Renderer>();
                if (rend != null) rend.enabled = false;
            }

            StartCoroutine(PlayDeathSoundAndDestroy());
        }
    }

    IEnumerator PlayDeathSoundAndDestroy()
    {
        // 1. Kick off your existing dynamic audio loading stream and wait for it
        // We modify LoadAndPlayExternalAudio to return an IEnumerator so we can yield on it
        yield return StartCoroutine(LoadAndPlayExternalAudio("Death.wav"));

        // 2. Wait for the duration of the clip that was just assigned and played
        if (audioSource != null && audioSource.clip != null)
        {
            yield return new WaitForSeconds(audioSource.clip.length);
        }
        else
        {
            // Fallback safety wait if the file failed to load
            yield return new WaitForSeconds(1.0f);
        }

        // 3. Now that the sound is done, safely clean up the GameObject
        Destroy(gameObject);
    }

    void Update()
    {
        // 1. Maintain your existing safety exit checks
        if (!isChasing || greenDemon == null || player == null || playerMovement.cutscene || !Screen.lockCursor)
        {
            return;
        }
    
        // 2. Calculate the current distance between the demon and the player
        float distance = Vector3.Distance(greenDemon.transform.position, player.transform.position);

        // 3. Define your rubber band boundaries
        float minSpeed = 3f;       // Speed when right next to the player
        float maxSpeed = 100f;        // Catch-up speed when the player is far away
        float closeDistance = 1.5f;    // Distance where minimum speed applies
        float farDistance = 100f;     // Distance where maximum speed applies

        // 4. Calculate a percentage (0.0 to 1.0) of how far the player is within our thresholds
        // InverseLerp outputs 0 if distance <= closeDistance, and 1 if distance >= farDistance
        float distanceFactor = Mathf.InverseLerp(closeDistance, farDistance, distance);

        // 5. Blend smoothly between minSpeed and maxSpeed based on that factor
        float dynamicSpeed = Mathf.Lerp(minSpeed, maxSpeed, distanceFactor);

        // 6. Look towards the player (optional, but keeps movement aligned with intent)
        // Since your billboard handles rotation, we just need to move the position directly.
        Vector3 direction = (player.transform.position - greenDemon.transform.position).normalized;

        // 7. Translate the transform position toward the player frame-by-frame
        greenDemon.transform.position += direction * dynamicSpeed * Time.deltaTime;
    }
}