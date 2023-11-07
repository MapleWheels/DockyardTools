using Barotrauma.Items.Components;

namespace DockyardTools;

public partial class GUICloseBlocker : ItemComponent
{
    void Subscribe()
    {
        GUICloseOverride.Subscribe(this, () =>
        {
            if (Character.Controlled is null)
                return false;
            if (Character.Controlled.SelectedItem is null)
                return false;
            if (Screen.Selected is SubEditorScreen)
                return false;
            return Character.Controlled.SelectedItem == this.Item;
        });       
    }

    void Unsubscribe()
    {
        GUICloseOverride.Unsubscribe(this);
    }
}