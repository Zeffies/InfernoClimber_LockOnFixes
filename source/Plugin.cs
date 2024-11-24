using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace FixLockOnWeirdness;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    // variables to keep track of how long you've been locked on
    public static float initialTime = -1f;
    public static float currentTime = -1f;
    // variable to help remove the "Quick Camera Reset" function if you're coming out of a lock-on
    public static bool stillHoldingR3 = false;
    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony("com.Zeffies.FixLockOnWeirdness");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(MCACameraModeControl), "Update")]
public class MCACameraModeControl_Update
{
    // prefix so i can replace the previous function before it runs the code
    [HarmonyPrefix]
    // with a harmony bool, you can do return false to skip the original method from being called. that way i can completely change what happens
    public static bool FixLockOnWeirdnessHopefully(MCACameraModeControl __instance)
    {
        // the below line is the same as what MCACamerModeControl.Update has, but the names of things are slightly different because you can't use "this"
        // with harmony...as far as i know, lol
        if (AnSingleton<AnGameMgr>.Instance.GetPlayerGos() != null && AnSingleton<AnGameMgr>.Instance.GetPlayerGos().IsTargetMoveMode())
        {
            // if you press QuickCameraReset (same button as lock-on) while locked on, return to normal camera
            if(AnSingleton<AnGameMgr>.Instance.GetPlayerGos().GetInputPerifelal().GetKeyDown(InputCode.QuickCameraReset))
            {
                __instance.m_masterCamera.SetRotationMode(CameraRotation.None);
                __instance.SetCameraControlMode(MCACameraModeControl.CameraControlMode.Normal);
                Plugin.stillHoldingR3 = true;
                return false;
            }
            // the way i fix the lock-on whipping around is a simple "check if it's been 1/4th of a second before changing camera mode"
            // i use the playStageTime because the game always has a stage timer any time you control your character and it was an easy
            // variable to hook into
            if (Plugin.initialTime == -1f)
            {
                // get the time you pressed lock on
                Plugin.initialTime = AnTimer.playStageTime;
            }
            // constantly gets the current time
            Plugin.currentTime = AnTimer.playStageTime;
            // then checks if 1/4th of a second has passed since you started locking on. this seems to be more than enough time
            // for your character to rotate properly before making the camera follow behind him
            if(Plugin.currentTime - Plugin.initialTime < .25f)
            {
                return false;
            }
            else
            {
                __instance.SetCameraControlMode(MCACameraModeControl.CameraControlMode.Target);
                Plugin.initialTime = -1f;
                return false;
            }
        }
        else
        {
            if(Plugin.stillHoldingR3 && !AnSingleton<AnGameMgr>.Instance.GetPlayerGos().GetInputPerifelal().GetKeyDown(InputCode.QuickCameraReset))
            {
                Plugin.stillHoldingR3 = false;
            }
            Plugin.initialTime = -1;

            // i'm gonna be honest, this shit doesn't look right to me but it works extremely well through lots of testing so i don't wanna
            // fix something that isn't broken
            if(Plugin.stillHoldingR3 == true)
            {
                __instance.m_masterCamera.SetRotationMode(CameraRotation.None);
                __instance.SetCameraControlMode(MCACameraModeControl.CameraControlMode.Normal);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
