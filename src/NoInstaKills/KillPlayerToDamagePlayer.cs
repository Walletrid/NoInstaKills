using System.Numerics;
using Dissonance;
using HarmonyLib;
using UnityEngine;

namespace NoInstaKills;

public class KillPlayerToDamagePlayer
{
    public static string? DamageCause;

    //public static AudioSource? MaskBreakSFX;

    //AudioClip defaultClip = (Unity) WalletMods.Load<AudioClip>("beep");

    private static void ResetDamageCause()
    {
        DamageCause = null;
    }

    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "KillPlayer", new[] { typeof(UnityEngine.Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(UnityEngine.Vector3), typeof(bool) })]
    internal static class PlayerControllerBKillPlayerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameNetcodeStuff.PlayerControllerB __instance, UnityEngine.Vector3 bodyVelocity, bool spawnBody, CauseOfDeath causeOfDeath, int deathAnimation, UnityEngine.Vector3 positionOffset, bool setOverrideDropItems)
        {
            if (DamageCause == "KillPlane")
            {
                // Forces Kill planes to kill the player
                ResetDamageCause();
                return true;
            }

            ResetDamageCause();

            Plugin.Log.LogDebug($"Checking if the player survives 100 damage...");
            if (__instance.health - NoInstaKillsConfig.InstaKillDamage.Value  > 0)
            {
                __instance.DamagePlayer(NoInstaKillsConfig.InstaKillDamage.Value, true, true, causeOfDeath, deathAnimation, false, bodyVelocity);
                Plugin.Log.LogDebug($"Player had {__instance.health + NoInstaKillsConfig.InstaKillDamage.Value} health (More than the Insta-Kill damage value set in the config [{NoInstaKillsConfig.InstaKillDamage.Value}]), so they will survive with {__instance.health}");
                return false;
            }

            Plugin.Log.LogDebug($"Player has less than {NoInstaKillsConfig.InstaKillDamage.Value} health, so they will not survive the attack. Resuming KillPlayer() function...");
            return true;
        }
    }

    //Bonus patches to fix bugs ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    //KillPlanes to always insta-kill
    [HarmonyPatch(typeof(KillLocalPlayer), "KillPlayer", new[] { typeof(GameNetcodeStuff.PlayerControllerB) })]
    internal static class KillLocalPlayerKillPlayerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(KillLocalPlayer __instance, GameNetcodeStuff.PlayerControllerB playerWhoTriggered)
        {
            DamageCause = "KillPlane";
            Plugin.Log.LogDebug($"Damage by kill plane, forcing patch to not activate.");
        }
    }

    //Maneaters to be able to kill you on second attack
    [HarmonyPatch(typeof(CaveDwellerAI), "FinishKillAnimation")]
    internal static class CaveDwellerAIFinishKillAnimationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CaveDwellerAI __instance)
        {
            __instance.startingKillAnimationLocalClient = false; // This variable stops the Maneater from killing you
            Plugin.Log.LogDebug("Fixed Maneater from not killing the player");
        }
    }

    //HauntedMaskItems to not spawn new maskeds
    [HarmonyPatch(typeof(HauntedMaskItem), "FinishAttaching")]
    internal static class HauntedMaskItemFinishAttachingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(HauntedMaskItem __instance)
        {
            if (__instance.IsOwner && !__instance.finishedAttaching)
            {
                //Runs the Damage check in here because I'm too stupid to think of a better way to check if the player survives and if I did not do this. I'd literally have to rewrite Vanilla code (I'm kind of already doing it in this code), which would be useless. And like, if I check it right now. If it doesn't suceed, it ain't running again up there :>
                if (__instance.previousPlayerHeldBy.health - NoInstaKillsConfig.InstaKillDamage.Value  > 0)
                {
                    Plugin.Log.LogDebug($"Player had {__instance.previousPlayerHeldBy.health} health (More than the Insta-Kill damage value set in the config [{NoInstaKillsConfig.InstaKillDamage.Value}]), so they will survive with {__instance.previousPlayerHeldBy.health - NoInstaKillsConfig.InstaKillDamage.Value}");
                    __instance.previousPlayerHeldBy.DamagePlayer(NoInstaKillsConfig.InstaKillDamage.Value, true, true, CauseOfDeath.Suffocation, deathAnimation: __instance.maskTypeId, false, UnityEngine.Vector3.zero);
                    HUDManager.Instance.UIAudio.PlayOneShot(Plugin.MaskBreakingSFX, 1f);
                    __instance.CancelAttachToPlayerOnLocalClient();
                    return false;
                }
            }

            Plugin.Log.LogDebug("Resuming the FinishAttaching() function since either the player wont survive or this isn't the player using the mask,...");
            return true;
            }
    }

    //Maskeds infecting you to not spawn a new masked
    [HarmonyPatch(typeof(MaskedPlayerEnemy), "killAnimation")]
    internal static class MaskedPlayerEnemykillAnimationPatch
    {
        [HarmonyPrefix]
        private static void Prefix(MaskedPlayerEnemy __instance)
        {
            if (__instance.inSpecialAnimationWithPlayer != null)
            {
                DamageCause = "MaskEnemy";
            }
        }
    }

    [HarmonyPatch(typeof(MaskedPlayerEnemy), "CreateMimicClientRpc", new[] { typeof(Unity.Netcode.NetworkObjectReference), typeof(bool), typeof(int) })]
    internal static class MaskedPlayerEnemyCreateMimicClientRpcPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MaskedPlayerEnemy __instance, Unity.Netcode.NetworkObjectReference netObjectRef, bool inFactory, int playerKilled)
        {
            if (DamageCause == "MaskEnemy")
            {
                Plugin.Log.LogDebug("Stopping spawning of a Masked from being infected by another masked and surviving");
                return false;
            }

            return true;
        }
    }

    // Stops bodies from spawning if the player is alive
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "SpawnDeadBody", new[] { typeof(int), typeof(UnityEngine.Vector3), typeof(int), typeof(GameNetcodeStuff.PlayerControllerB), typeof(int), typeof(UnityEngine.Transform), typeof(UnityEngine.Transform), typeof(UnityEngine.Vector3) })]
    internal static class PlayerControllerBSpawnDeadBodyPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameNetcodeStuff.PlayerControllerB __instance, int playerId, UnityEngine.Vector3 bodyVelocity, int causeOfDeath, GameNetcodeStuff.PlayerControllerB deadPlayerController, int deathAnimation, UnityEngine.Transform setPhysicsParent, UnityEngine.Transform overridePosition, UnityEngine.Vector3 positionOffset)
        {
            if (!deadPlayerController.isPlayerDead)
            {
                Plugin.Log.LogDebug($"Prevented spawning of a dead body of player with Id {playerId}, since they are alive and theoretically shouldn't get a body spawned.");
                return false;
            }

            return true;
        }
    }


    // Stops the Bracken from spawning a random body when it damages you
    [HarmonyPatch(typeof(FlowermanAI), "FinishKillAnimation", new[] { typeof(bool) })]
    internal static class FlowermanAIFinishKillAnimationPatch
    {
        [HarmonyPrefix]
        private static void Prefix(FlowermanAI __instance, ref bool carryingBody)
        {
            if (!__instance.inSpecialAnimationWithPlayer.isPlayerDead)
            {
                Plugin.Log.LogDebug("Stopping a bracken from spawning a body");
                __instance.inSpecialAnimationWithPlayer.deadBody = null;
                carryingBody = false;
                __instance.SetEnemyStunned(true, 1f);
            }
        }
    }
}
