using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework.Input;
using ModdingToolkit.Config;
using ModdingToolkit.Networking;

namespace DockyardTools
{
    public partial class DockyardToolsPlugin : IAssemblyPlugin
    {
        public static readonly IConfigControl ControlForwardX = ConfigManager.AddConfigKeyOrMouseBind(
            "Vehicle-Forward",
            nameof(DockyardTools), Keys.D,
            displayData: new DisplayData(
                "Vehicle-Forward", nameof(DockyardTools), "Vehicle-Forward", nameof(DockyardTools))
        );
    
        public static readonly IConfigControl ControlReverseX = ConfigManager.AddConfigKeyOrMouseBind(
            "Vehicle-Reverse",
            nameof(DockyardTools), Keys.A,
            displayData: new DisplayData(
                "Vehicle-Reverse", nameof(DockyardTools), "Vehicle-Reverse", nameof(DockyardTools))
        );
    
        public static readonly IConfigControl ControlUpY = ConfigManager.AddConfigKeyOrMouseBind(
            "Vehicle-Up",
            nameof(DockyardTools), Keys.W,
            displayData: new DisplayData(
                "Vehicle-Up", nameof(DockyardTools), "Vehicle-Up", nameof(DockyardTools))
        );
    
        public static readonly IConfigControl ControlDownY = ConfigManager.AddConfigKeyOrMouseBind(
            "Vehicle-Down",
            nameof(DockyardTools), Keys.S,
            displayData: new DisplayData(
                "Vehicle-Down", nameof(DockyardTools), "Vehicle-Down", nameof(DockyardTools))
        );

        public static readonly IConfigRangeFloat ControlSensitivity = ConfigManager.AddConfigRangeFloat(
            "Vehicle-ControlSensitity",
            nameof(DockyardTools),
            0.6f, 0.1f, 1.9f, 19, //max value cannot be >= than OutputDeadZone
            NetworkSync.NoSync, f => f is > float.Epsilon and < float.MaxValue);
    }
}
