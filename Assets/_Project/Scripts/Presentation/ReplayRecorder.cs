using Fotbool.Core;
using UnityEngine;

namespace Fotbool.Presentation
{
    /// <summary>Fixed-size transform ring buffer. Playback renders collider-free proxy meshes, never rewinds live rigidbodies.</summary>
    public sealed class ReplayRecorder : MonoBehaviour
    {
        private const int Capacity = 300; // ten seconds at 30Hz, approximately 200KB for 23 actors
        private const float Interval = 1f / 30f;
        public MatchManager Match;
        public bool IsPlaying { get; private set; }
        public float PlaybackTime { get; private set; }
        private Transform[] sources;
        private Transform[] ghosts;
        private Renderer[] liveRenderers;
        private bool[] visibility;
        private Vector3[,] positions;
        private Quaternion[,] rotations;
        private float[] timestamps;
        private int head, count, first, cursor;
        private float nextSample;
        private GameObject ghostRoot;

        public void Initialize(Transform[] actors)
        {
            sources = actors;
            positions = new Vector3[Capacity, actors.Length];
            rotations = new Quaternion[Capacity, actors.Length];
            timestamps = new float[Capacity];
            ghosts = new Transform[actors.Length];
            var renderers = new System.Collections.Generic.List<Renderer>();
            ghostRoot = new GameObject("Replay Visuals");
            for (int i = 0; i < actors.Length; i++)
            {
                var root = new GameObject("Replay Actor " + i);
                root.transform.SetParent(ghostRoot.transform, false);
                ghosts[i] = root.transform;
                foreach (var mesh in actors[i].GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = mesh.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;
                    var copy = new GameObject("Visual");
                    copy.transform.SetParent(root.transform, false);
                    copy.transform.localPosition = actors[i].InverseTransformPoint(mesh.transform.position);
                    copy.transform.localRotation = Quaternion.Inverse(actors[i].rotation) * mesh.transform.rotation;
                    copy.transform.localScale = mesh.transform.lossyScale;
                    copy.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                    copy.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                    renderers.Add(renderer);
                }
            }
            liveRenderers = renderers.ToArray();
            visibility = new bool[liveRenderers.Length];
            ghostRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            if (sources == null || IsPlaying || Match == null || Match.Phase != MatchPhase.Playing || Time.time < nextSample) return;
            nextSample = Time.time + Interval;
            timestamps[head] = Time.time;
            for (int i = 0; i < sources.Length; i++)
            { positions[head, i] = sources[i].position; rotations[head, i] = sources[i].rotation; }
            head = (head + 1) % Capacity;
            count = Mathf.Min(count + 1, Capacity);
        }

        public bool Begin()
        {
            if (IsPlaying || count < 2) return false;
            first = (head - count + Capacity) % Capacity;
            cursor = 0; PlaybackTime = 0; IsPlaying = true;
            for (int i = 0; i < liveRenderers.Length; i++)
            { visibility[i] = liveRenderers[i].enabled; liveRenderers[i].enabled = false; }
            ghostRoot.SetActive(true);
            Step(0, out _);
            return true;
        }

        public bool Step(float delta, out Vector3 ballPosition)
        {
            ballPosition = Vector3.zero;
            if (!IsPlaying) return false;
            PlaybackTime += Mathf.Max(0, delta);
            float time = timestamps[first] + PlaybackTime;
            while (cursor < count - 2 && timestamps[(first + cursor + 1) % Capacity] < time) cursor++;
            int a = (first + cursor) % Capacity, b = (a + 1) % Capacity;
            float t = Mathf.InverseLerp(timestamps[a], timestamps[b], time);
            for (int i = 0; i < ghosts.Length; i++)
            {
                ghosts[i].SetPositionAndRotation(Vector3.Lerp(positions[a, i], positions[b, i], t),
                    Quaternion.Slerp(rotations[a, i], rotations[b, i], t));
            }
            ballPosition = ghosts[ghosts.Length - 1].position; // ball must be last actor
            return time < timestamps[(head - 1 + Capacity) % Capacity];
        }

        public void End()
        {
            if (!IsPlaying) return;
            for (int i = 0; i < liveRenderers.Length; i++)
                if (liveRenderers[i] != null) liveRenderers[i].enabled = visibility[i];
            ghostRoot.SetActive(false); IsPlaying = false;
        }

        private void OnDestroy() { End(); if (ghostRoot != null) Destroy(ghostRoot); }
    }
}
