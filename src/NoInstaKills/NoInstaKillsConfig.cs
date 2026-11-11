using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Configuration;
using ConfigSync;

namespace NoInstaKills;

class NoInstaKillsConfig
{
    internal static ConfigEntry<bool> Enabled { get; private set; } = null!;
    internal static SyncedEntry<int> InstaKillDamage { get; private set; } = null!;

    public NoInstaKillsConfig(Plugin plugin,ConfigFile cfg)
    {
        //Temporarily disables auto-saving of the config since it's inefficient
        cfg.SaveOnConfigSet = false;

        //Registering configs
        SyncedConfig shared = SyncedConfig.For(plugin);
        Enabled = cfg.Bind("General", "Enabled", true, "Turns the mod on or off");
        InstaKillDamage = shared.Share(cfg.Bind("General", "Insta-Kill damage", 100, "Defines how much damage a Insta-Kill attack should do (RECOMMENDED TO KEEP AT 100, MAKING IT FAIR AND BALANCED)"));

        //Removes old Configs
        ClearOrphanedEntries(cfg);

        //Manually saves
        cfg.Save();

        //Re-enables so changes are auto-applied
        cfg.SaveOnConfigSet = true;

    static void ClearOrphanedEntries(ConfigFile cfg)
    {
        // Find the private property `OrphanedEntries` from the type `ConfigFile` //
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        // And get the value of that property from our ConfigFile instance //
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        // And finally, clear the `OrphanedEntries` dictionary //
        orphanedEntries.Clear();
    }
    }
}
