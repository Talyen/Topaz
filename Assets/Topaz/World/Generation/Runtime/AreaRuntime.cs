using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Topaz.Gameplay;

namespace Topaz.Generation
{
    /// <summary>The active area's scene is a lifetime boundary, never a state authority.</summary>
    public sealed class AreaRuntime : MonoBehaviour
    {
        public static Scene CreateScene(string areaId) => SceneManager.CreateScene("Topaz "+areaId);
        public static IEnumerator Release(Scene scene)
        {if(scene.IsValid() && scene.isLoaded){var operation=SceneManager.UnloadSceneAsync(scene);if(operation!=null)while(!operation.isDone)yield return null;}}
    }
    public sealed class AreaExit : MonoBehaviour
    {
        public AreaConnection Connection { get; private set; }
        public string DestinationLabel { get; private set; }
        public void Initialize(AreaConnection connection,string label){Connection=connection;DestinationLabel=label;}
    }
}
