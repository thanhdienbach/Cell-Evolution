using UnityEngine;
using UnityEngine.SceneManagement;

namespace CellEvol.Core
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Boot Flow")]
        [SerializeField] private int gameplaySceneIndex = 1;
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
            if (activeIndex == gameplaySceneIndex) return;

            var sceneCount = SceneManager.sceneCountInBuildSettings;
            if (gameplaySceneIndex < 0 || gameplaySceneIndex >= sceneCount)
            {
                Debug.LogError(
                    $"[GameManager] Invalid gameplaySceneIndex = {gameplaySceneIndex}. " +
                    $"Build Settings scene count = {sceneCount}. " +
                    $"Add scenes to Build Settings and set index correctly."
                );
                return;
            }

            SceneManager.LoadScene(gameplaySceneIndex, LoadSceneMode.Single);
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp to a sensible range; actual validity checked at runtime because build settings count can change.
            if (gameplaySceneIndex < 0) gameplaySceneIndex = 0;
        }
#endif
    }

}
