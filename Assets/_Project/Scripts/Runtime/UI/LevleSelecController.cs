using System;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace CellEvol.UI
{
    public sealed class LevleSelecController : MonoBehaviour
    {
        [Header("Scene name rule")]
        [Tooltip("Number of digits used for the scene name. Example: 4 => 0010")]
        [SerializeField] private int digits = 4;

        [Tooltip("Optional: add an offset if your level numbering differs from scene numbering.")]
        [SerializeField] private int levleToSceneOffset = 0;


        public void LoadLevle(int levleNumber)
        {
            if (digits <= 0 || digits > 8)
            {
                Debug.LogError($"Invalid digits={digits}. Expected 1..8.");
                return;
            }

            int sceneNumber = levleNumber + levleToSceneOffset;
            if (sceneNumber < 0)
            {
                Debug.LogError($"Computed sceneNumber={sceneNumber} is invalid.");
                return;
            }

            string sceneName = sceneNumber.ToString(new string('0', digits));

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not in Build Settings (or name mismatch).");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        public void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("sceneName is null/empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not in Build Settings (or name mismatch).");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
