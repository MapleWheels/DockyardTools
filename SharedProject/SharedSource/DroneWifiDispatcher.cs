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
        }
        else
        {
            LinkedDockingPort = dockPortItem.GetComponent<DockingPort>();
            var droneIdString = dockPortItem.GetTags()
                .FirstOrDefault(tag => !tag.IsEmpty && REGEX_DOCKTAGID.IsMatch(tag.Value), Identifier.Empty);
            if (droneIdString.Equals(Identifier.Empty))
            {
                ModUtils.Logging.PrintError($"DroneWifiDispatcher: Unable to bind to docking port, no drone wifi channel id found!");
                IsActive = false;
            }
            else
            {
                // parse tags
            }
        }

    }
}