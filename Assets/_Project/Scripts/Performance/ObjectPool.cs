using System.Collections.Generic;
using UnityEngine;

namespace Fotbool.Performance
{
    /// <summary>Bounded reusable GameObject pool for particles, crowd props, and UI objects.</summary>
    public sealed class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int capacity = 32;
        private readonly Queue<GameObject> available = new Queue<GameObject>();
        private readonly HashSet<GameObject> leased = new HashSet<GameObject>();
        private void Awake()
        {
            if (prefab == null) { enabled = false; return; }
            for (int i = 0; i < Mathf.Max(1, capacity); i++)
            { var obj = Instantiate(prefab, transform); obj.SetActive(false); available.Enqueue(obj); }
        }
        public GameObject Rent(Vector3 position, Quaternion rotation)
        {
            if (available.Count == 0) return null;
            var obj = available.Dequeue(); leased.Add(obj);
            obj.transform.SetPositionAndRotation(position, rotation); obj.SetActive(true);
            return obj;
        }
        public bool Return(GameObject obj)
        {
            if (obj == null || !leased.Remove(obj)) return false;
            obj.SetActive(false); obj.transform.SetParent(transform, false); available.Enqueue(obj); return true;
        }
    }
}
