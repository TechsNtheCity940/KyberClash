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
            // Restart with R key
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameManager.Instance?.RestartMatch();
            }

            // Quit with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
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