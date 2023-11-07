using Barotrauma.Items.Components;

namespace DockyardTools;

public partial class GUICloseBlocker : ItemComponent
{
    public GUICloseBlocker(Item item, ContentXElement element) : base(item, element)
    {
    }

    public override void OnItemLoaded()
    {
#if CLIENT
        Subscribe();
#endif
    }

    ~GUICloseBlocker()
    {
#if CLIENT
        Unsubscribe();
#endif
    }
}