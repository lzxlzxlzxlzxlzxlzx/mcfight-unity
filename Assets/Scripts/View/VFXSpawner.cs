using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary>
    /// VFXSpawner: loads prefabs from Resources/VFX/ and spawns particle effects at world positions.
    /// Handles 2D coordinate adaptation (z=0), scaling, and auto-destroy.
    /// </summary>
    public static class VFXSpawner
    {
        // Global scale factor: Cartoon FX prefabs are designed for 3D, our game is 2D with small world units
        private const float GLOBAL_SCALE = 1.0f;

        private static readonly Dictionary<string, GameObject> _cache = new();
        private static readonly List<GameObject> _activeEffects = new();
        private static Transform _vfxRoot;

        static Transform GetRoot()
        {
            if (_vfxRoot == null)
            {
                var go = new GameObject("VFX_Root");
                Object.DontDestroyOnLoad(go);
                _vfxRoot = go.transform;
            }
            return _vfxRoot;
        }

        static GameObject LoadPrefab(string path)
        {
            if (!_cache.TryGetValue(path, out var prefab))
            {
                prefab = Resources.Load<GameObject>(path);
                _cache[path] = prefab;
            }
            return prefab;
        }

        /// <summary>
        /// Spawn a VFX prefab at a world position. Auto-destroys after lifetime.
        /// </summary>
        /// <param name="vfxPath">Path under Resources/VFX/ (without extension)</param>
        /// <param name="position">World position (x, y, 0)</param>
        /// <param name="scaleMul">Additional scale multiplier on top of GLOBAL_SCALE</param>
        /// <param name="lifetime">Auto-destroy after this many seconds (0 = use prefab default)</param>
        /// <returns>The spawned GameObject, or null if prefab not found</returns>
        public static GameObject Spawn(string vfxPath, Vector3 position, float scaleMul = 1f, float lifetime = 0f)
        {
            var prefab = LoadPrefab("VFX/" + vfxPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[VFXSpawner] Prefab not found: VFX/{vfxPath}");
                return null;
            }

            var go = Object.Instantiate(prefab, GetRoot());
            go.transform.position = new Vector3(position.x, position.y, 0);

            // Cartoon FX particles have tiny startSizes designed for 3D cameras.
            // Our 2D orthographic camera shows 720 world units tall.
            // Normalize all particle sizes to a fixed range so they're visible.
            float targetSize = 15f * scaleMul;

            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                // Normalize startSize to target value, preserving min/max ratio for TwoConstants mode
                var startSize = main.startSize;
                if (startSize.mode == ParticleSystemCurveMode.Constant)
                {
                    startSize.constant = targetSize;
                    main.startSize = startSize;
                }
                else if (startSize.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    startSize.constantMin = targetSize * 0.7f;
                    startSize.constantMax = targetSize * 1.3f;
                    main.startSize = startSize;
                }

                // Scale up gravity for 2D world
                var gravity = main.gravityModifier;
                if (gravity.constant != 0f)
                {
                    gravity.constant = 0.5f;
                    main.gravityModifier = gravity;
                }

                // Scale up startSpeed for 2D world
                var startSpeed = main.startSpeed;
                if (startSpeed.mode == ParticleSystemCurveMode.Constant && startSpeed.constant > 0f)
                {
                    startSpeed.constant = Mathf.Min(startSpeed.constant * 50f, 300f);
                    main.startSpeed = startSpeed;
                }

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 150;
                }

                // Restart to apply new sizes
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }

            // Disable any light components (2D game doesn't use dynamic lighting)
            foreach (var light in go.GetComponentsInChildren<Light>())
                light.enabled = false;

            if (lifetime > 0f)
                Object.Destroy(go, lifetime);
            else
                Object.Destroy(go, 3f);

            _activeEffects.Add(go);
            return go;
        }

        /// <summary>
        /// Spawn with team color tint applied to ParticleSystems.
        /// </summary>
        public static GameObject Spawn(string vfxPath, Vector3 position, int team, float scaleMul = 1f, float lifetime = 0f)
        {
            var go = Spawn(vfxPath, position, scaleMul, lifetime);
            if (go != null)
            {
                var color = team == 0 ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.4f, 0.3f);
                TintParticleSystem(go, color);
            }
            return go;
        }

        /// <summary>
        /// Spawn with rotation (for directional effects like cones).
        /// </summary>
        public static GameObject Spawn(string vfxPath, Vector3 position, Quaternion rotation, float scaleMul = 1f, float lifetime = 0f)
        {
            var go = Spawn(vfxPath, position, scaleMul, lifetime);
            if (go != null)
                go.transform.rotation = rotation;
            return go;
        }

        static void TintParticleSystem(GameObject go, Color color)
        {
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                var col = main.startColor;
                col.color = color;
                main.startColor = col;
            }
        }

        /// <summary>
        /// Clear all active VFX (called on battle end).
        /// </summary>
        public static void ClearAll()
        {
            foreach (var go in _activeEffects)
            {
                if (go != null) Object.Destroy(go);
            }
            _activeEffects.Clear();
        }

        /// <summary>
        /// Clean up destroyed effects from the active list.
        /// </summary>
        public static void Cleanup()
        {
            _activeEffects.RemoveAll(go => go == null);
        }

        // ===== P0 VFX convenience methods =====

        /// <summary> Melee hit: light impact for normal attacks </summary>
        public static void SpawnMeleeHit(Vector3 pos, float damage)
        {
            if (damage >= 30f)
                Spawn("Hit/CFXR3 Hit Light C (Air)", pos, 1.5f, 1.5f);
            else if (damage >= 15f)
                Spawn("Hit/CFXR3 Hit Misc C", pos, 1.2f, 1.2f);
            else
                Spawn("Hit/CFXR3 Hit Misc A", pos, 1.0f, 1f);
        }

        /// <summary> Explosion: for AOE / stomps / finishers </summary>
        public static void SpawnExplosion(Vector3 pos, float scale = 1.5f)
        {
            Spawn("Explosion/CFXR3 Fire Explosion A", pos, scale, 2.5f);
        }

        /// <summary> Shockwave ring: for area effects </summary>
        public static void SpawnShockwave(Vector3 pos, float scale = 1.2f)
        {
            Spawn("Area/CFXR ScreenDistortion Ring", pos, scale, 2f);
        }

        /// <summary> Magic aura: for lock-on markers </summary>
        public static void SpawnMagicAura(Vector3 pos, float scale = 1.0f, float lifetime = 2f)
        {
            Spawn("Area/CFXR3 Magic Aura A (Runic)", pos, scale, lifetime);
        }

        /// <summary> Light hit: for beam/laser impacts </summary>
        public static void SpawnLightHit(Vector3 pos, float scale = 1.0f)
        {
            Spawn("Hit/CFXR3 Hit Light A (Air)", pos, scale, 1.2f);
        }

        /// <summary> Ice hit: for frost effects </summary>
        public static void SpawnIceHit(Vector3 pos, float scale = 1.0f)
        {
            Spawn("Hit/CFXR3 Hit Ice B (Air)", pos, scale, 1.2f);
        }

        /// <summary> Fire hit: for burn effects </summary>
        public static void SpawnFireHit(Vector3 pos, float scale = 1.0f)
        {
            Spawn("Hit/CFXR3 Hit Fire B (Air)", pos, scale, 1.2f);
        }

        /// <summary> Smoke puff: for death effects </summary>
        public static void SpawnSmokePuff(Vector3 pos, float scale = 1.0f)
        {
            Spawn("Death/CFXR3 Hit Misc F Smoke Only", pos, scale, 1.5f);
        }

        /// <summary> Sky rays: for obelisk/beam-rain </summary>
        public static void SpawnSkyRays(Vector3 pos, float scale = 0.8f, float lifetime = 1f)
        {
            Spawn("Area/CFXR3 Sky Rays (Loop)", pos, scale, lifetime);
        }

        /// <summary> Vortex tornado: for sandstorm/tornado effects </summary>
        public static void SpawnVortex(Vector3 pos, float scale = 0.3f, float lifetime = 5f)
        {
            Spawn("Area/CFX3_VortexTornado", pos, scale, lifetime);
        }
    }
}
