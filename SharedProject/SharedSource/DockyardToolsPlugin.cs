using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Barotrauma;
using Barotrauma.LuaCs;

namespace DockyardTools
{
    public partial class DockyardToolsPlugin : IAssemblyPlugin
    {
        public IStorageService StorageService { get; set; }
        public ILuaScriptManagementService LuaScriptService { get; set; }
        public static DockyardToolsPlugin Instance { get; private set; }

        public DockyardToolsPlugin()
        {
            Instance = this;
            RegisterUserData();
        }

        private void RegisterUserData()
        {
            MoonSharp.Interpreter.UserData.RegisterType<DockyardToolsPlugin>();
            MoonSharp.Interpreter.UserData.RegisterType<DoorController>();
            MoonSharp.Interpreter.UserData.RegisterType<DroneWifiDispatcher>();
            MoonSharp.Interpreter.UserData.RegisterType<DroneWifiDispatcher>();
            MoonSharp.Interpreter.UserData.RegisterType<ESCU>();
            MoonSharp.Interpreter.UserData.RegisterType<ETCU>();
            MoonSharp.Interpreter.UserData.RegisterType<FighterHUD>();
            MoonSharp.Interpreter.UserData.RegisterType<LinkedStructureRepair>();
            MoonSharp.Interpreter.UserData.RegisterType<LuaComponent>();
            MoonSharp.Interpreter.UserData.RegisterType<MachineStateReader>();
            MoonSharp.Interpreter.UserData.RegisterType<PlayerInputCapture>();
            MoonSharp.Interpreter.UserData.RegisterType<SignalCounter>();
            MoonSharp.Interpreter.UserData.RegisterType<VerticalEngine>();
        }

        public void Initialize()
        {
            // When your plugin is loading, use this instead of the constructor
            // Put any code here that does not rely on other plugins.
        }

        public void OnLoadCompleted()
        {
#if CLIENT
            PostLoadClient();
#endif
        }

        public void PreInitPatching()
        {
            // Not yet supported: Called during the Barotrauma startup phase before vanilla content is loaded.
        }

        public void Dispose()
        {
#if CLIENT
            DisposeClient();
#endif
        }
    }
}
