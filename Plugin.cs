using System.Linq;
using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace PDX_Mod_Loader;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Wrapper Wrapper { get; set; }
    private void Awake()
    {
        Wrapper = new Wrapper();
        // Plugin startup logic
        var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony");

        var patchedMethods = harmony.GetPatchedMethods().ToArray();
        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    
    }
}
