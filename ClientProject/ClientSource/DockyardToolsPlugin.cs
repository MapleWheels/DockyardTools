using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework.Input;
using ModdingToolkit.Config;
using ModdingToolkit.Networking;
using ModdingToolkit.Patches;

namespace DockyardTools
{
    public partial class DockyardToolsPlugin : IAssemblyPlugin
    {
        public static IConfigControl ControlForwardX { get; private set; }
        public static IConfigControl ControlReverseX { get; private set; }
        public static IConfigControl ControlUpY { get; private set; }
        public static IConfigControl ControlDownY { get; private set; }
        public static IConfigRangeFloat ControlSensitivity { get; private set; }
        public static IConfigControl ControlToggleDocking { get; private set; }

        private void PostLoadClient()
        {
            ControlForwardX = ConfigManager.AddConfigKeyOrMouseBind(
                "Vehicle-Forward",
                nameof(DockyardTools), Keys.Right,
                displayData: new DisplayData(
                    "Vehicle-Forward", nameof(DockyardTools), "Vehicle-Forward", nameof(DockyardTools))
            );
        
            ControlReverseX = ConfigManager.AddConfigKeyOrMouseBind(
                "Vehicle-Reverse",
                nameof(DockyardTools), Keys.Left,
                displayData: new DisplayData(
                    "Vehicle-Reverse", nameof(DockyardTools), "Vehicle-Reverse", nameof(DockyardTools))
            );
        
            ControlUpY = ConfigManager.AddConfigKeyOrMouseBind(
                "Vehicle-Up",
                nameof(DockyardTools), Keys.Up,
                displayData: new DisplayData(
                    "Vehicle-Up", nameof(DockyardTools), "Vehicle-Up", nameof(DockyardTools))
            );
        
            ControlDownY = ConfigManager.AddConfigKeyOrMouseBind(
                "Vehicle-Down",
                nameof(DockyardTools), Keys.Down,
                displayData: new DisplayData(
                    "Vehicle-Down", nameof(DockyardTools), "Vehicle-Down", nameof(DockyardTools))
            );

            ControlToggleDocking = ConfigManager.AddConfigKeyOrMouseBind(
                "Vehicle-ToggleDocking",
                nameof(DockyardTools), Keys.Z,
                displayData: new DisplayData(
                    "Vehicle-ToggleDocking", nameof(DockyardTools), "Vehicle-Down", nameof(DockyardTools))
            );
            
            ControlSensitivity = ConfigManager.AddConfigRangeFloat(
                "Vehicle-ControlSensitity",
                nameof(DockyardTools),
                20f, 4f, 100f, 19, //max value cannot be >= than OutputDeadZone
                NetworkSync.NoSync, f => f is > float.Epsilon and < float.MaxValue);
            
            
            ModdingToolkit.Patches.PatchManager.RegisterPatch(new PatchManager.PatchData(
                AccessTools.DeclaredMethod(typeof(Barotrauma.HUD), nameof(HUD.CloseHUD)), 
                null,
                new HarmonyMethod(AccessTools.DeclaredMethod(typeof(GUICloseOverride), nameof(GUICloseOverride.Post_CloseHUD)))
                ));
        }

        private void DisposeClient()
        {
            GUICloseOverride.Dipose();
        }
    }
}
