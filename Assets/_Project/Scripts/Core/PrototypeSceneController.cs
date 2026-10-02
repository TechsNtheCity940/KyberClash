using UnityEngine;
using UnityEngine.SceneManagement;
using KyberKlash.Core;
using KyberKlash.Player;

namespace KyberKlash.Core
{
    /// <summary>
    /// Scene controller for the prototype arena scene
    /// </summary>
    public class PrototypeSceneController : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CameraManager cameraManager;

        protected virtual void Awake()
        {
            // Ensure singletons exist
            if (GameManager.Instance == null && gameManager != null)
            {
                Instantiate(gameManager.gameObject);
            }
            if (CameraManager.Instance == null && cameraManager != null)
            {
                Instantiate(cameraManager.gameObject);
            }
        }

        protected virtual void Start()
        {
            // Setup complete - GameManager will spawn players
        }

        protected virtual void Update()
        {
            if (GameManager.Instance == null) return;

            // Quick restart with R (rematch / restart current match).
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameManager.Instance.RestartMatch();
            }

            // Escape is now owned by GameFlowManager (pause overlay). If no match is
            // active, fall back to quitting so the editor/standalone session still exits.
            if (Input.GetKeyDown(KeyCode.Escape) && !GameManager.Instance.IsMatchActive)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}