using System.Collections.Generic;
using UnityEngine;

namespace KyberKlash.VFX
{
    /// <summary>
    /// Simple object pool for VFX
    /// </summary>
    public class VFXPool : MonoBehaviour
    {
        [System.Serializable]
        public class PoolEntry
        {
            public string key;
            public GameObject prefab;
            public int initialSize = 10;
        }

        [SerializeField] private PoolEntry[] pools;
        private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();

        public static VFXPool Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Object.Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var entry in pools)
            {
                if (entry.prefab == null) continue;

                var queue = new Queue<GameObject>();
                prefabDict[entry.key] = entry.prefab;

                for (int i = 0; i < entry.initialSize; i++)
                {
                    var obj = Object.Instantiate(entry.prefab, transform);
                    obj.SetActive(false);
                    queue.Enqueue(obj);
                }

                poolDict[entry.key] = queue;
            }
        }

        /// <summary>
        /// Get object from pool
        /// </summary>
        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            if (!poolDict.TryGetValue(key, out var queue))
            {
                // Create new pool entry on demand
                if (prefabDict.TryGetValue(key, out var prefab))
                {
                    var spawnedObject = Object.Instantiate(prefab, position, rotation);
                    return spawnedObject;
                }
                return null;
            }

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else
            {
                obj = Object.Instantiate(prefabDict[key], transform);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Return object to pool
        /// </summary>
        public void Return(string key, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (poolDict.TryGetValue(key, out var queue))
            {
                queue.Enqueue(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
        }

        /// <summary>
        /// Spawn and auto-return after lifetime
        /// </summary>
        public GameObject Spawn(string key, Vector3 position, Quaternion rotation, float lifetime)
        {
            var obj = Get(key, position, rotation);
            if (obj != null && lifetime > 0f)
            {
                StartCoroutine(AutoReturn(key, obj, lifetime));
            }
            return obj;
        }

        private System.Collections.IEnumerator AutoReturn(string key, GameObject obj, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            Return(key, obj);
        }
    }
}