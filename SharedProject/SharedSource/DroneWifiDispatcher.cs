using System.Text.RegularExpressions;
using Barotrauma.Items.Components;

namespace DockyardTools;

public partial class DroneWifiDispatcher : ItemComponent
{
    #region SYMBOLDEF

    private const string S_WIFICHANNEL_VELX_IN = "out_setch0_velx_in";
    private const string S_WIFICHANNEL_VELY_IN = "out_setch1_vely_in";
    private const string S_WIFICHANNEL_TURRETANG_IN = "out_setch2_turretang_in";
    private const string S_WIFICHANNEL_TURRETTRIG_IN = "out_setch3_turrettrig_in";
    private const string S_WIFICHANNEL_DOCKINGCMD_IN = "out_setch4_dockingcmd_in";
    private const string S_WIFICHANNEL_VELX_OUT = "out_setch5_velx_out";
    private const string S_WIFICHANNEL_VELY_OUT = "out_setch6_vely_out";
    private const string S_WIFICHANNEL_DEPTH_OUT = "out_setch7_depth_out";
    private const string S_WIFICHANNEL_BATTCHG_OUT = "out_setch8_battchg_out";
    private const string S_TAG_LINKED_PORT = "smartdockingport";
    
    #endregion
    
    private static readonly Regex REGEX_DOCKTAGID = new Regex("^dronewifich=[0-9]+$");

    public byte DroneId { get; private set; }
    public DockingPort? LinkedDockingPort { get; private set; } = null;
    
    public DroneWifiDispatcher(Item item, ContentXElement element) : base(item, element)
    {
        isActive = true; //resolve in OnMapLoaded
    }

    public override void OnMapLoaded()
    {
        if (item.linkedTo
                .FirstOrDefault(me => me is Item it && it.GetComponent<DockingPort>() is not null, null) is not Item dockPortItem
            || !dockPortItem.HasTag(S_TAG_LINKED_PORT))
        {
            IsActive = false;
            return;
        }
        
        LinkedDockingPort = dockPortItem.GetComponent<DockingPort>();
        var droneIdString = dockPortItem.GetTags()
            .FirstOrDefault(tag => !tag.IsEmpty && REGEX_DOCKTAGID.IsMatch(tag.Value), Identifier.Empty);
        if (droneIdString.Equals(Identifier.Empty))
        {
            ModUtils.Logging.PrintError($"{nameof(DroneWifiDispatcher)}: Unable to bind to docking port, no drone wifi channel id found!");
            IsActive = false;
            return;
        }
        
        // parse tags
        var splitstring = droneIdString.ToString().Split("=");
        if (splitstring.Length != 2)
        {
            ModUtils.Logging.PrintError($"{nameof(DroneWifiDispatcher)}: Incorrect amount of arguments in tag.");
            IsActive = false;
            return;
        }

        if (!int.TryParse(splitstring[1], out var droneId))
        {
            ModUtils.Logging.PrintError($"{nameof(DroneWifiDispatcher)}: Cannot parse wifi channel/drone id.");
            IsActive = false;
            return;
        }

        if (droneId is < 0 or > byte.MaxValue)
        {
            ModUtils.Logging.PrintError($"{nameof(DroneWifiDispatcher)}: The Wifi Channel if {droneId} is out of range.");
            IsActive = false;
            return;
        }

        DroneId = (byte)droneId;
        
        // send values
#if SERVER
        item.CreateServerEvent(this);
        SendIdChannels();
#elif CLIENT
        if (GameMain.IsSingleplayer)
        {
            SendIdChannels();
        }
#endif
    }

    private void SendIdChannels()
    {
        int ch = DroneId;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_VELX_IN);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_VELY_IN);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_TURRETANG_IN);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_TURRETTRIG_IN);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_DOCKINGCMD_IN);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_VELX_OUT);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_VELY_OUT);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_DEPTH_OUT);
        ch++;
        item.SendSignal(ch.ToString(), S_WIFICHANNEL_BATTCHG_OUT);
    }
}