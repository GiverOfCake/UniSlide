using System;
using UnityEngine;

namespace Game.UI
{
    
    /// <summary>
    /// A virtual button mapping for use in menus.
    /// The order these are given decides where on the controller these are physically mapped to.
    /// </summary>
    public class ButtonItem
    {
        /// <summary>
        /// The localisation key for the text on this item.
        /// </summary>
        public string TextKey;

        /// <summary>
        /// The width of this item, in arbitrary relative units.
        /// </summary>
        public int Width;

        /// <summary>
        /// The color of this item.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Alternative keys that perform the equivalent action, e.g. enter or the arrow keys.
        /// </summary>
        public KeyCode[] AlternateKeys;

        /// <summary>
        /// The action to perform when this button is pressed.
        /// </summary>
        public Action OnPress;
    }
}