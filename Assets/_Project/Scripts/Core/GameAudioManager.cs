using UnityEngine;

namespace KyberKlash.Core
{
    [DisallowMultipleComponent]
    public class GameAudioManager : MonoBehaviour
    {
        private const string AudioResourceRoot = "KyberKlash/Audio/";

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private string currentMusicKey;

        public static GameAudioManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            GameObject audioObject = new GameObject("GameAudioManager");
            DontDestroyOnLoad(audioObject);
            audioObject.AddComponent<GameAudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.55f;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.85f;
        }

        public void PlayMusic(string key, float volume = 0.55f)
        {
            if (string.IsNullOrEmpty(key) || currentMusicKey == key) return;

            AudioClip clip = Resources.Load<AudioClip>(AudioResourceRoot + key);
            if (clip == null)
            {
                currentMusicKey = string.Empty;
                return;
            }

            currentMusicKey = key;
            musicSource.clip = clip;
            musicSource.volume = volume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            currentMusicKey = string.Empty;
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PlaySfx(string key, float volume = 1f)
        {
            if (string.IsNullOrEmpty(key) || sfxSource == null) return;

            AudioClip clip = Resources.Load<AudioClip>(AudioResourceRoot + key);
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
