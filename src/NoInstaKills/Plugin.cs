using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace NoInstaKills;

[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("tech.rethunk.mortar.ConfigSync")]
[BepInPlugin(MyPluginInfo.Id, MyPluginInfo.Name, MyPluginInfo.Version)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    public static AssetBundle SFXAssetBundle { get; private set; }

    public static AudioClip MaskBreakingSFX;

    // Settings here are each player's own. Use ConfigSync for ones the whole lobby should share.

    private void Awake()
    {
        Log = Logger;

        // Loading the asset bundle with the Mask Breaking SFX (Here because I can just use Info.Location)
        string pluginFolder = Path.GetDirectoryName(Info.Location);

        // Getting the bundle
        string bundlePath = Path.Combine(pluginFolder, "noinstakills-sfx");

        // Loading the bundle
        SFXAssetBundle = AssetBundle.LoadFromFile(bundlePath);

        if (SFXAssetBundle == null)
        {
            Log.LogError($"Failed to load AssetBundle from {bundlePath}!");
        }
        else
        {
            MaskBreakingSFX = SFXAssetBundle.LoadAsset<AudioClip>("MaskBreakingSFX");
        }

        // Starting up the confg
        new NoInstaKillsConfig(this, Config);

        // Put the mod's hooks after this check so Enabled can turn them off.
        if (!NoInstaKillsConfig.Enabled.Value)
        {
            Log.LogInfo($"{MyPluginInfo.Name} is turned off in its config");
            return;
        }

        var harm = new Harmony(MyPluginInfo.Id);
        harm.PatchAll();

        Log.LogInfo($"{MyPluginInfo.Name} {MyPluginInfo.Version} loaded");
    }
}
