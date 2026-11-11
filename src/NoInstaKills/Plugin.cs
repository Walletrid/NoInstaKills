using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace NoInstaKills;

[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("tech.rethunk.mortar.ConfigSync")]
[BepInPlugin(MyPluginInfo.Id, MyPluginInfo.Name, MyPluginInfo.Version)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    // Settings here are each player's own. Use ConfigSync for ones the whole lobby should share.

    private void Awake()
    {
        Log = Logger;

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
