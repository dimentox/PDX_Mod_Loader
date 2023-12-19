using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Colossal.Serialization.Entities;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using HarmonyLib;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using static Game.Modding.ToolchainDeployment;


namespace PDX_Mod_Loader
{

    [HarmonyPatch(typeof(GameManager), "CreateWorld")]
    public class GameManager_CreateSystems_Patch
    {
        public static void Postfix(GameManager __instance)
        {
            Debug.LogWarning("---> Loading wrapper");

            Debug.LogWarning("---> Loading wrapper init");
            if (Plugin.Wrapper != null)
            {
                Debug.LogWarning("---> Loading wrapper testtoolkit");
                if (Plugin.Wrapper.TestToolKit())
                {
                    Debug.LogWarning("---> calling wrapper init");
                    Plugin.Wrapper.Init();
                }
            }
            else
            {
                throw new Exception("NULL WRAPPER");
            }
        }
    }


    public class Wrapper
    {
        public static PluginManager managerTest;
        public bool loaded;

        public bool TestToolKit()
        {
            if (currentState == DeploymentState.NotInstalled || currentState == DeploymentState.Outdated)
            {
                RunWithUI();
                GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(
                    new ConfirmationDialog("Common.DIALOG_TITLE[Warning]",
                        "This will Take some time and unity will open and when its done it will close! Do not exit the application. Just wait!",
                        "Common.DIALOG_ACTION[Yes]", "I like turtles!", Array.Empty<LocalizedString>()), delegate { });
                return false;
            }

            return true;
        }


        public void Init()
        {
            GameManager.instance.onGameLoadingComplete += OnGameLoadingComplete;
            Debug.LogWarning("*****PRE LOADED MANAGER *****");
            var path = Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Mods")).FullName;
            Debug.LogWarning($"*****LOADED MANAGER *****{path}");

            managerTest =
                new PluginManager(path);
        }

        private void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            Debug.LogWarningFormat("===>Instance_onGameLoad<=== **{0}** **{1}**", purpose, mode);
            if (purpose == Purpose.Cleanup && mode == GameMode.MainMenu && loaded == false)
            {
                loaded = true;
                managerTest.LoadPlugins();
                managerTest.WorldCreated();
            }
        }
    }


    public class PluginManager : GameSystemBase
    {
        public bool modsEnabled = true;
        private List<IMod> plugins;

        public PluginManager(string path)
        {
            this.path = path;
            Debug.LogWarning($"*****CTOR {path} ****");
        }

        public string path { get; set; }

        public void LoadPlugins()
        {
            plugins = new List<IMod>();
            Debug.LogWarning("*****Loading plugis ****");
            // If mods are disabled, early out - this allows us to disable mods, enter Play Mode, exit Play Mode
            //and be sure that the managed assemblies have been unloaded (assuming DomainReload occurs)
            if (!modsEnabled)
                return;

            var folder = path; //Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Mods"));
            if (Directory.Exists(folder))
            {
                Debug.LogWarning($"*****Loading {folder} ****");
                var modfolders = Directory.GetDirectories(folder);
                Debug.LogWarning($"*****MODS {modfolders} ****");
                foreach (var modfolder in modfolders)
                {

                    Debug.LogWarning($"*****Loading {modfolder} ****");
                    var mods = Directory.GetDirectories(modfolder);
                    Debug.LogWarning($"*****MODS {mods} ****");
                    foreach (var mod in mods)
                    {
                        Debug.LogWarning($"*****Loading {mod} ****");
                        var modName = Path.GetFileName(mod);
                        var monoAssembly = Path.Combine(mod, $"{modName}.dll");
                        Debug.LogWarning($"*****Loading {modName} {monoAssembly} ****");
                        if (File.Exists(monoAssembly))
                        {
                            Debug.LogWarning($"*****Loading plugin *****{modName}");
                            var managedPlugin = Assembly.LoadFile(monoAssembly);
                            Debug.LogWarning($"*****Loading plugin *****{monoAssembly}");
                            // var pluginModule = managedPlugin.GetType("IMod");
                            //Debug.LogWarning($"*****Loading plugin *****{pluginModule}");
                            //var plugin = (IMod)Activator.CreateInstance(pluginModule);
                            bool modFound = false;
                            managedPlugin.GetTypes()
                                .Where(t => t != typeof(IMod) && typeof(IMod).IsAssignableFrom(t))
                                .ToList()
                                .ForEach(x =>
                                {
                                    if (!modFound)
                                    {
                                        var plugin = (IMod)Activator.CreateInstance(x);
                                        plugins.Add(plugin);
                                        var burstedAssembly =
                                            Path.Combine(mod,
                                                $"{modName}_win_x86_64.dll"); // Burst dll (assuming windows 64bit)
                                        if (File.Exists(burstedAssembly))
                                        {
                                            Debug.LogWarning($"*****Loading plugin Burst *****{burstedAssembly}");
                                            BurstRuntime.LoadAdditionalLibrary(burstedAssembly);
                                        }
                                    }

                                });
                        }
                    }
                }
            }
            foreach (var plugin in plugins) plugin.OnLoad();

        }

        // Update is called once per frame
        public void WorldCreated()
        {
            foreach (var plugin in plugins)
            {
                Debug.LogWarning($"Calling OnWorldCreated {plugin}");
                plugin.OnCreateWorld(World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UpdateSystem>());
            }
        }

        public override void OnUpdate()
        {
           
        }
    }


}