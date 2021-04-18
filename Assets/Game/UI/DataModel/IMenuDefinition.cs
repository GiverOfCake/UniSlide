using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The definition for a menu screen. Controls the items and actions available on this screen.
    /// </summary>
    public interface IMenuDefinition
    {

        void Init(IMenuController menuController);

        int ItemCount();

        /// <summary>
        /// Fires when the selected item is changed. Use this to know which item to process when an action button is pressed,
        /// or to play a preview clip of a song, etc.
        /// </summary>
        /// <param name="index"></param>
        void SetSelectedItem(int index);
        
        /// <summary>
        /// Return a GameObject representing the item at this index. Expected to come from a prefab and have a RectTransform.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        GameObject FetchItem(int index);

        ButtonItem[] GetButtonItems();

    }
}