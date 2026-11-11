using HarmonyLib;

namespace NoInstaKills;

public class KillPlayerToDamagePlayer
{
    public static string? DamageCause;

    //public static AudioSource? MaskBreakSFX;

    //AudioClip defaultClip = (Unity) WalletMods.Load<AudioClip>("beep");

    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "KillPlayer", new[] { typeof(UnityEngine.Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(UnityEngine.Vector3), typeof(bool) })]
    internal static class PlayerControllerBKillPlayerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(GameNetcodeStuff.PlayerControllerB __instance, UnityEngine.Vector3 bodyVelocity, bool spawnBody, CauseOfDeath causeOfDeath, int deathAnimation, UnityEngine.Vector3 positionOffset, bool setOverrideDropItems)
        {
            Plugin.Log.LogDebug($"Checking if the player survives 100 damage...");
            if (DamageCause == "KillPlane")
            {
                return true;
            }

            if (__instance.health - 100  > 0)
            {
                __instance.DamagePlayer(100, true, true, causeOfDeath, deathAnimation, false, bodyVelocity);
                Plugin.Log.LogDebug($"Player had {__instance.health + 100} health (More than 100), so they will survive with {__instance.health}");
                return false;
            }

            Plugin.Log.LogDebug($"Player has less than 100 health, so they will not survive the attack. Resuming KillPlayer() function...");
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
        [HarmonyPostfix]
        private static void Postfix(HauntedMaskItem __instance)
        {
            DamageCause = "MaskItem";
        }
    }

    [HarmonyPatch(typeof(HauntedMaskItem), "CreateMimicServerRpc", new[] { typeof(bool), typeof(UnityEngine.Vector3) })]
    internal static class HauntedMaskItemCreateMimicServerRpcPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(HauntedMaskItem __instance, bool inFactory, UnityEngine.Vector3 playerPositionAtDeath)
        {
            if (DamageCause == "MaskItem" && !__instance.previousPlayerHeldBy.isPlayerDead)
            {
               Plugin.Log.LogDebug("Stopping spawning of a Masked from using a Mask Item and surviving");
               __instance.CancelAttachToPlayerOnLocalClient();
               //HUDManager.Instance.UIAudio.PlayOneShot(MaskBreakSFX, 1f);
               return false;
            }

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

        //[HarmonyTranspiler]
        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{

        //}
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
}
