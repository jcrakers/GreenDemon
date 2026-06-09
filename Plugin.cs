using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using System.IO;

namespace GreenDemon;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class GreenDemonPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static string resourcesPath;
    public static string imagesPath;
    public static string audioPath;

    private float spawnTimer;
    private float spawnDelayValue;
    private bool shouldSpawn = false;

    public static bool InstantDeath { get; private set; }
    public static int DamageAmount { get; private set; }

    private ConfigEntry<float> spawnDelay;
    private ConfigEntry<bool> instantDeath;
    private ConfigEntry<int> damageAmount;

        
    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        spawnDelay = Config.Bind("General", "Spawn Delay", 2f, 
            new ConfigDescription("Delay before spawning (default: 2 seconds)", new AcceptableValueRange<float>(0f, 60f)));

        instantDeath = Config.Bind("General", "Instant Death", true, 
            new ConfigDescription("Enable instant death (default: true)", new AcceptableValueRange<bool>(false, true)));

        damageAmount = Config.Bind("General", "Damage Amount", 2, 
            new ConfigDescription("Amount of damage to deal if not instant death (default: 2)", new AcceptableValueRange<int>(0, 10)));

        // Create paths
        var greenDemonPath = Path.Combine(Paths.PluginPath, "GreenDemon");
        resourcesPath = Path.Combine(greenDemonPath, "Resources");
        imagesPath = Path.Combine(resourcesPath, "Images");
        audioPath = Path.Combine(resourcesPath, "Audio");
        
        spawnDelayValue = spawnDelay.Value;
        InstantDeath = instantDeath.Value;
        DamageAmount = damageAmount.Value;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // 1. Create a ray from the center of the camera viewport
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            // 2. Cast the ray to find a physical surface
            // You can add a maximum distance if you don't want it spawning miles away
            if (Physics.Raycast(ray, out hit))
            {
                // 3. Extract the x, y, z coordinates from the hit point
                float x = hit.point.x;
                float y = hit.point.y;
                float z = hit.point.z;

                // 4. Call your custom creation method
                GreenDemonData.CreateGreenDemon(x, y, z);
            }
        }
    }

    private void OnLevelWasLoaded(int level)
    {
        if (level != 0) {
            shouldSpawn = true;
            spawnTimer = 0f;
        }
    }
    
    void FixedUpdate()
    {
        if (shouldSpawn && spawnDelayValue != 0)
        {
            spawnTimer += Time.fixedDeltaTime;
            if (spawnTimer >= spawnDelayValue)
            {
                shouldSpawn = false;
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    GreenDemonData.CreateGreenDemon(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                }
            }
        }
    }
}
