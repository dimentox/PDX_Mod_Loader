using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.Internal;
using Colossal.Json;
using Colossal.Plugins;
using Colossal.Serialization.Entities;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.UI;
using Game.UI.Localization;
using Game.UI.Menu;
using HarmonyLib;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static Game.Modding.ToolchainDeployment;
using PluginInfo = Colossal.Plugins.PluginInfo;






namespace PDX_Mod_Loader
{

    [HarmonyPatch(typeof(GameManager), "CreateWorld")]
    public class GameManager_CreateSystems_Patch
    {
        public static Wrapper wrapper;

        public static void Postfix(GameManager __instance)
        {
            wrapper = new Wrapper();
            if (wrapper != null)
            {
                if (wrapper.TestToolKit())
                {
                    wrapper.Init();
                }
            }
          
        }
    }

    public class SettingsManager : ModSetting
    {
        public override void SetDefaults()
        {
          
        }
        public override AutomaticSettings.SettingPageData GetPageData(string id, bool addPrefix)
        {
            return AutomaticSettings.FillSettingsPage(this, id, addPrefix);
        }
        //private static bool RegisterInOptionsUI(Setting instance, string name, bool addPrefix)
        //{
        //    World defaultGameObjectInjectionWorld = World.DefaultGameObjectInjectionWorld;
        //    OptionsUISystem optionsUISystem = ((defaultGameObjectInjectionWorld != null) ? defaultGameObjectInjectionWorld.GetOrCreateSystemManaged<OptionsUISystem>() : null);
        //    if (optionsUISystem != null)
        //    {
        //        optionsUISystem.RegisterSetting(instance, name, addPrefix);
        //        return true;
        //    }
        //    return false;
        //}

        public SettingsManager(IMod mod) : base(mod)
        {
          
           Type type = mod.GetType();
           Debug.LogWarningFormat("*****LOADED SETTINGS ***** {0}", mod);
          // AssetDatabase.global.LoadSettings("Editor Settings", this.editor, new EditorSettings());
            //SettingsManager.RegisterInOptionsUI(this, nameof(mod), false);


            //this.id = string.Concat(new string[]
            //{
            //    type.Assembly.GetName().Name,
            //    ".",
            //    type.Namespace,
            //    ".",
            //    type.Name
            //});
        }

        
    }
    public class Wrapper
    {
        public MyPluginManager managerTest;

        public bool TestToolKit()
        {
            if (Game.Modding.ToolchainDeployment.currentState == DeploymentState.NotInstalled || Game.Modding.ToolchainDeployment.currentState == DeploymentState.Outdated)
            {
                Game.Modding.ToolchainDeployment.RunWithUI();
                GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(new ConfirmationDialog("Common.DIALOG_TITLE[Warning]", "This will Take some time and unity will open and when its done it will close! Do not exit the application. Just wait!", "Common.DIALOG_ACTION[Yes]", "I like turtles!", Array.Empty<LocalizedString>()), delegate (int msg)
                {
                   
                });
                return false;
            }

            return true;
        }

        public void Start()
        {
           
        }

        public void Init()
        {
            Debug.LogWarning("*****LOADED MANAGER *****");
            managerTest =
                new MyPluginManager(
                    Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Mods")).FullName,
                    typeof(IMod));
        }
    }

    public class PDXMod
    {
        public string ID { get; set; }
        public IMod Mod { get; set; }
        public SettingsManager SettingsManager { get; set; }

        public PDXMod()
        {
            
        }

        public PDXMod(IMod mod, SettingsManager manager)
        {
            this.Mod = mod;
            this.SettingsManager = manager;
            this.ID = nameof(mod);
            AssetDatabase.global.LoadSettings("ModSettings", this.SettingsManager, new SettingsManager(mod));
            this.SettingsManager.RegisterInOptionsUI();
        }
        private readonly List<Setting> m_Settings = new List<Setting>();
    }
    public class MyPluginManager : PluginManager
    {
        public Dictionary<string,PDXMod> Mods = new();
        public List<PluginInfo> Plugins = new();
        public bool ready { get; set; }
        public MyPluginManager(string rootPath, params Type[] types) : base(rootPath, types)
        {
          
            GameManager.instance.onGameLoadingComplete += OnGameLoadingComplete;
            GameManager.instance.onGamePreload += Instance_onGamePreload;
            ready = false;

        }

        private void Instance_onGamePreload(Purpose purpose, GameMode mode)
        {
            Debug.LogWarningFormat("===>Instance_onGamePreload<=== **{0}** **{1}**", purpose, mode);
            if (!ready && purpose == Purpose.NewGame)
            {
              
                ready = true;

            }
            else
            {
               
            }

         
        }

        public event Action<PluginInfo> PluginLoaded;
        public event Action<PluginInfo> PluginUnloaded;
        public event Action OnDispose;

        public void Start()
        {
           
        }

        private void loadmods()
        {
            foreach (var pluginInfo in Plugins)
            {
                Debug.LogWarning("*****Loading a mod*****");
                var mod = loaMod(pluginInfo);
              
                Debug.LogWarning("*****Loaded a mod*****");
                if (mod != null && mod.ID != "")
                {

                    
                    Mods.Add(pluginInfo.assembly.FullName, mod);
                }
            }

            var assets = AssetDatabase.user.GetAssets<Colossal.IO.AssetDatabase.SettingAsset>();
            assets.ForEach(x =>
            {
               
            });
            

        }
        private void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            Debug.LogWarningFormat("===>OnGameLoadingComplete<=== **{0}** **{1}**", purpose, mode);
            Debug.LogWarning("****GAMELOADING COMPLETE*****");
            if (purpose == Purpose.Cleanup && mode == GameMode.MainMenu)
            {
     
                loadmods();
            }
            if (mode == GameMode.MainMenu)
            {
                Debug.LogWarning("*****INMENU*****");
               
            }
            else if (mode == GameMode.Game)
            {
               
                Debug.LogWarning("*****INGAME*****");
               
            }
        }

        private PDXMod loaMod(PluginInfo info)
        {
            var tmods = new List<IMod>();
            SettingsManager manager;
            var pdxmod = new PDXMod();
            bool modFound = false;
            try
            {
                var assem = info.assembly;

                info.assembly.GetTypes()
                    .Where(t => t != typeof(IMod) && typeof(IMod).IsAssignableFrom(t))
                    .ToList()
                    .ForEach(x =>
                        {
                            if (!modFound)
                            {
                                var mod = (IMod)Activator.CreateInstance(x);
                               

                               var burstedAssembly = Path.Combine(info.rootPath, info.assembly.GetName().Name, $"{info.assembly.GetName().Name}_win_x86_64.dll");      // Burst dll (assuming windows 64bit)
                                Debug.LogWarningFormat("****Looking for {0}", burstedAssembly);
                                if (File.Exists(burstedAssembly))
                                {
                                    Debug.LogWarningFormat("****Loading BUrst for {0}", burstedAssembly);
                                    LoadAdditionalLibrary(burstedAssembly);
                                }
                               
                                mod.OnLoad();
                                mod.OnCreateWorld(World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UpdateSystem>());
                                manager = new SettingsManager(mod);
                                pdxmod.SettingsManager = manager;
                                pdxmod.Mod = mod;
                                

                            
                                modFound = true;
                            }
                        }
                    );
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

          
            return pdxmod;
        }

        public override void OnPluginLoaded(PluginInfo info)
        {
            base.OnPluginLoaded(info);
            Debug.LogWarning(("*****LOADED PLUGIN {0}*****", info.assemblyPath));
            PluginLoaded?.Invoke(info);
            Plugins.Add(info);
           

        }

        private void Dispose()
        {
            var OnOnDispose = OnDispose;
            if (OnOnDispose != null) OnOnDispose();

            base.Dispose();
            PluginLoaded = null;
            PluginUnloaded = null;
        }

        public override void OnPluginUnloaded(PluginInfo info)
        {
            base.OnPluginUnloaded(info);
            PluginUnloaded?.Invoke(info);
        }
    }
}