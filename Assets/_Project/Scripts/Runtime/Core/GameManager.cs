using UnityEngine;
using UnityEngine.SceneManagement;

namespace CellEvol.Core
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Boot Flow")]
        [SerializeField] private int levleSelecScene = 1;
        [SerializeField] private bool loadGamePlayOnStart = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!loadGamePlayOnStart) return;

            if (!Application.isPlaying) return;

            var activeIndex = SceneManager.GetActiveScene().buildIndex;
            if (activeIndex == levleSelecScene) return;

            var sceneCount = SceneManager.sceneCountInBuildSettings;
            if (levleSelecScene < 0 || levleSelecScene >= sceneCount)
            {
                Debug.LogError(
                    $"[GameManager] Invalid gameplaySceneIndex = {levleSelecScene}. " +
                    $"Build Settings scene count = {sceneCount}. " +
                    $"Add scenes to Build Settings and set index correctly."
                );
                return;
            }

            SceneManager.LoadScene(levleSelecScene, LoadSceneMode.Single);
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp to a sensible range; actual validity checked at runtime because build settings count can change.
            if (levleSelecScene < 0) levleSelecScene = 0;
        }
#endif
    }

}
