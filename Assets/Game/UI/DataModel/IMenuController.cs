using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Interface for use by <see cref="IMenuDefinition"/>s in order to read/write to game state or navigate to other scenes/menus.
    /// </summary>
    public interface IMenuController
    {

        GameStateHolder GameState(); 
        
        void GoBack();
        
        void AdvanceTo(IMenuDefinition newMenu);

        void AdvanceTo(Scene scene);

    }
}