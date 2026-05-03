using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework.Input;

namespace DockyardTools
{
    public partial class DockyardToolsPlugin : IAssemblyPlugin
    {
        private Harmony _harmony;
        
        public static ISettingControl ControlForwardX { get; private set; }
        public static ISettingControl ControlReverseX { get; private set; }
        public static ISettingControl ControlUpY { get; private set; }
        public static ISettingControl ControlDownY { get; private set; }
        public static ISettingRangeBase<float> InputSensitivity { get; private set; }
        public static ISettingControl ControlToggleDocking { get; private set; }
        public static ISettingControl ControlExtraAttack { get; private set; }
        public static ISettingControl ControlUtility { get; private set; }

        //Injected
        public IConfigService ConfigService { get; set; }
        public IPluginManagementService PluginService { get; set; }
        private ContentPackage _ownerPackage;
        public ContentPackage OwnerPackage => _ownerPackage;
        
        private void PostLoadClient()
        {
            PluginService.TryGetPackageForPlugin<DockyardToolsPlugin>(out _ownerPackage);
            
            ControlForwardX = ConfigService.TryGetConfig<ISettingControl>(OwnerPackage, nameof(ControlForwardX), out var t1) ? t1 : null;
            ControlReverseX = ConfigService.TryGetConfig<ISettingControl>(OwnerPackage, nameof(ControlReverseX), out var t2) ? t2 : null;
            ControlUpY = ConfigService.TryGetConfig<ISettingControl>(OwnerPackage, nameof(ControlUpY), out var t3) ? t3: null;
            ControlDownY = ConfigService.TryGetConfig<ISettingControl>(OwnerPackage, nameof(ControlDownY), out var t4) ? t4 : null;
            ControlToggleDocking = ConfigService.TryGetConfig<ISettingControl>(OwnerPackage, nameof(ControlToggleDocking), out var t5) ? t5 : null;
            ControlExtraAttack = ConfigService.TryGetConfig<ISettingControl>(OwnerPackage, nameof(ControlExtraAttack), out var t6) ? t6 : null;
            ControlUtility = ConfigService.TryGetConfig<ISettingControl>(OwnerPackage, nameof(ControlUtility), out var t7) ? t7 : null;
            InputSensitivity = ConfigService.TryGetConfig<ISettingRangeBase<float>>(OwnerPackage, nameof(InputSensitivity), out var t8) ? t8 : null;
            
            Patch();
        }

        private void DisposeClient()
        {
            GUICloseOverride.Dipose();
            UnpatchAll();
        }

        private void Patch()
        {
            _harmony ??= new Harmony(nameof(DockyardTools));
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(Barotrauma.HUD), nameof(HUD.CloseHUD)),
                null,
                new HarmonyMethod(AccessTools.DeclaredMethod(
                    typeof(GUICloseOverride),
                    nameof(GUICloseOverride.Post_CloseHUD))
                ));
        }

        private void UnpatchAll()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
