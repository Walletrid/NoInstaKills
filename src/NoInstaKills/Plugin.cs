using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace NoInstaKills;

[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(MyPluginInfo.Id, MyPluginInfo.Name, MyPluginInfo.Version)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    // Settings here are each player's own. Use ConfigSync for ones the whole lobby should share.
    internal static ConfigEntry<bool> Enabled { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        Enabled = Config.Bind("General", "Enabled", true, "Turns the mod on or off.");
        // Put the mod's hooks after this check so Enabled can turn them off.
        if (!Enabled.Value)
        {
            Log.LogInfo($"{MyPluginInfo.Name} is turned off in its config");
            return;
        }
        Log.LogInfo($"{MyPluginInfo.Name} {MyPluginInfo.Version} loaded");
    }
}
