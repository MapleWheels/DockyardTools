using System.Globalization;
using System.Reflection.PortableExecutable;
using Barotrauma.Items.Components;

namespace DockyardTools;

public sealed class MachineStateReader : Powered
{
    private ImmutableList<ItemContainer> _turretLoaderContainers = ImmutableList<ItemContainer>.Empty;
    private ImmutableList<Fabricator> _fabricators = ImmutableList<Fabricator>.Empty;
    private ImmutableList<Deconstructor> _deconstructors = ImmutableList<Deconstructor>.Empty;

    private const int MaxDevices = 6;
    // ReSharper disable once InconsistentNaming
    private const int DECIMALPLACES = 1;

    public enum DeviceType
    {
        Fabricator, Deconstructor, LoaderBase
    }
    
    [InGameEditable, Serialize(DeviceType.LoaderBase, IsPropertySaveable.Yes, description: "What type of machine?")]
    public DeviceType MachineType { get; set; }

    [Editable(0, 10), Serialize(3, IsPropertySaveable.Yes, description: "Ticks between updating. 0 to disable.")]
    public int TicksBetweenUpdates { get; set; }
    private int _ticksUntilUpdate { get; set; }
    
    public MachineStateReader(Item item, ContentXElement element) : base(item, element)
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        IsActive = true;
    }

    public override void OnMapLoaded()
    {
        base.OnMapLoaded();
        if (MachineType is DeviceType.LoaderBase)
        {
            _turretLoaderContainers = this.item.linkedTo
                .Where(me => me is Item it 
                             && it.Submarine.Equals(this.item.Submarine)
                             && it.GetComponent<ItemContainer>() is { })
                .Select(me => ((Item)me).GetComponent<ItemContainer>())
                .ToImmutableList();
        }
        else if (MachineType is DeviceType.Fabricator)
        {
            _fabricators = this.item.linkedTo
                .Where(me => me is Item it 
                             && it.Submarine.Equals(this.item.Submarine)
                             && it.GetComponent<Fabricator>() is { })
                .Select(me => ((Item)me).GetComponent<Fabricator>())
                .ToImmutableList();
        }
        else if (MachineType is DeviceType.Deconstructor)
        {
            _deconstructors = this.item.linkedTo
                .Where(me => me is Item it 
                             && it.Submarine.Equals(this.item.Submarine)
                             && it.GetComponent<Deconstructor>() is { })
                .Select(me => ((Item)me).GetComponent<Deconstructor>())
                .ToImmutableList();
        }
    }

    public override void Update(float deltaTime, Camera cam)
    {
        if (TicksBetweenUpdates > 0)
        {
            _ticksUntilUpdate--;
            if (_ticksUntilUpdate < 1)
            {
                _ticksUntilUpdate = TicksBetweenUpdates;
                switch (MachineType)    
                {
                    case DeviceType.LoaderBase:
                        UpdateAmmoBoxData();
                        break;
                    case DeviceType.Fabricator:
                        UpdateFabricatorData();
                        break;
                    case DeviceType.Deconstructor:
                        UpdateDeconstructorData();
                        break;
                }
            }
        }
    }

    private void UpdateFabricatorData()
    {
        for (int i = 0; i < _fabricators.Count && i < MaxDevices; i++)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_fabricators[i] is null)
                continue;
            var fab = _fabricators[i];
            if (fab.State is not Fabricator.FabricatorState.Active)
            {
                item.SendSignal("0", GetConnectionNameForSlot(i));
                continue;
            }
            item.SendSignal((fab.progressState*100f).FormatToDecimalPlace(0), GetConnectionNameForSlot(i));
        }
    }

    private void UpdateDeconstructorData()
    {
        for (int i = 0; i < _deconstructors.Count && i < MaxDevices; i++)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_deconstructors[i] is null)
                continue;
            var decon = _deconstructors[i];
            item.SendSignal((decon.progressState*100f).FormatToDecimalPlace(0), GetConnectionNameForSlot(i));
        }
    }
    
    private void UpdateAmmoBoxData()
    {
        for (int i = 0; i < _turretLoaderContainers.Count && i < MaxDevices; i++)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_turretLoaderContainers[i] is null || _turretLoaderContainers[i].Inventory is null)
                continue;

            Item? ammobox = null;
            foreach (Inventory.ItemSlot slot in _turretLoaderContainers[i].Inventory.slots)
            {
                foreach (Item slotItem in slot.Items)
                {
                    if (slotItem.HasTag("ammobox"))
                    {
                        ammobox = slotItem;
                        break;
                    }
                }

                if (ammobox is not null)
                    break;
            }

            if (ammobox is not null)
            {
                item.SendSignal(new Signal(ammobox.Condition.FormatToDecimalPlace(DECIMALPLACES)), GetConnectionNameForSlot(i));
            }
        }
    }
    
    private string GetConnectionNameForSlot(int i) => $"item{i}_statuscondition";

}