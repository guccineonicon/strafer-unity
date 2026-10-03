using System.Collections;
using System.Collections.Generic;
using Strafer.Characters;
using UnityEngine;

namespace Strafer.Core
{
    /// <summary>
    /// Free-for-all match rules.
    ///
    /// At startup it builds a practice arena if the scene has no ground, spawns the
    /// player and a row of practice dummies, and hides any scene camera so the
    /// player's camera takes over. During the match it awards one kill to the
    /// killer of each character and respawns the dead after a short delay: the
    /// player at a spawn point, dummies where they started.
    ///
    /// Added automatically by <see cref="StraferAutoStart"/>, or place one in a
    /// scene yourself to customize it.
    /// </summary>
    public class StraferGameMode : MonoBehaviour
    {
        [Header("Respawn")]
        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float respawnDelay = 2f;

        [Header("Characters")]
        [SerializeField, Tooltip("Optional prefab with a StraferCharacter, used for the player. Customize tuning or the gun model here. Left empty, a default character is built in code.")]
        private StraferCharacter playerPrefab;

        [SerializeField, Tooltip("Optional prefab for practice dummies. Falls back to the player prefab, then to a default character.")]
        private StraferCharacter dummyPrefab;

        [Header("Practice")]
        [SerializeField, Range(0, 16), Tooltip("Dummies spawned in front of the first spawn point. Skipped if the scene already contains non-player characters.")]
        private int practiceDummyCount = 3;

        [SerializeField, Min(0f), Tooltip("Distance in meters from the spawn point to the row of dummies.")]
        private float practiceDummyDistance = 8f;

        [SerializeField, Min(0f), Tooltip("Side-to-side gap in meters between dummies.")]
        private float practiceDummySpacing = 3f;

        [SerializeField, Tooltip("Build a simple practice arena when there is no ground under the first spawn point.")]
        private bool buildArenaWhenNoGround = true;

        private readonly List<StraferSpawnPoint> spawnPoints = new List<StraferSpawnPoint>();

        private void OnEnable()
        {
            StraferCharacter.AnyCharacterDied += HandleCharacterDeath;
        }

        private void OnDisable()
        {
            StraferCharacter.AnyCharacterDied -= HandleCharacterDeath;
        }

        private void Start()
        {
            spawnPoints.AddRange(FindObjectsByType<StraferSpawnPoint>(FindObjectsSortMode.InstanceID));

            Vector3 origin;
            Quaternion facing;
            GetSpawnTransform(0, out origin, out facing);

            if (buildArenaWhenNoGround && !Physics.Raycast(origin + Vector3.up, Vector3.down, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                PracticeArena.Build(origin);
            }

            StraferCharacter[] existing = FindObjectsByType<StraferCharacter>(FindObjectsSortMode.None);
            bool hasPlayer = false;
            bool hasHandPlacedDummies = false;
            foreach (StraferCharacter character in existing)
            {
                if (character.PlayerControlled)
                {
                    hasPlayer = true;
                }
                else
                {
                    hasHandPlacedDummies = true;
                }
            }

            if (!hasPlayer)
            {
                SpawnCharacter(playerPrefab, origin, facing, true, "Player");
            }

            // Respect hand-placed dummies.
            if (!hasHandPlacedDummies)
            {
                SpawnPracticeDummies(origin, facing);
            }

            DisableSceneCameras();
        }

        private void SpawnPracticeDummies(Vector3 origin, Quaternion facing)
        {
            StraferCharacter prefab = dummyPrefab != null ? dummyPrefab : playerPrefab;
            Vector3 forward = facing * Vector3.forward;
            Vector3 right = facing * Vector3.right;

            // Dummies face back toward the spawn point.
            Quaternion dummyFacing = Quaternion.LookRotation(-forward, Vector3.up);
            float rowHalfWidth = (practiceDummyCount - 1) * practiceDummySpacing * 0.5f;

            for (int i = 0; i < practiceDummyCount; i++)
            {
                Vector3 position = origin + forward * practiceDummyDistance + right * (i * practiceDummySpacing - rowHalfWidth);
                SpawnCharacter(prefab, position, dummyFacing, false, "PracticeDummy" + i);
            }
        }

        /// <summary>
        /// Creates a character. It is built under an inactive holder so
        /// <see cref="StraferCharacter.PlayerControlled"/> is set before the
        /// character's startup code runs.
        /// </summary>
        private static StraferCharacter SpawnCharacter(StraferCharacter prefab, Vector3 position, Quaternion rotation, bool playerControlled, string objectName)
        {
            GameObject holder = new GameObject("SpawnHolder");
            holder.SetActive(false);

            StraferCharacter character;
            if (prefab != null)
            {
                character = Instantiate(prefab, position, rotation, holder.transform);
            }
            else
            {
                GameObject characterObject = new GameObject();
                characterObject.transform.SetParent(holder.transform, false);
                characterObject.transform.SetPositionAndRotation(position, rotation);
                character = characterObject.AddComponent<StraferCharacter>();
            }

            character.name = objectName;
            character.PlayerControlled = playerControlled;
            character.transform.SetParent(null, true);
            Destroy(holder);
            return character;
        }

        private void HandleCharacterDeath(StraferCharacter victim, StraferCharacter killer)
        {
            // Self-kills (for example a reflected shot) do not award points.
            if (killer != null && killer != victim)
            {
                killer.Score++;
            }

            StartCoroutine(RespawnAfterDelay(victim));
        }

        private IEnumerator RespawnAfterDelay(StraferCharacter victim)
        {
            yield return new WaitForSeconds(respawnDelay);

            if (victim == null)
            {
                yield break;
            }

            if (!victim.PlayerControlled)
            {
                victim.Revive();
                yield break;
            }

            Vector3 position;
            Quaternion rotation;
            GetSpawnTransform(Random.Range(0, Mathf.Max(1, spawnPoints.Count)), out position, out rotation);
            victim.Revive(position, rotation);
        }

        private void GetSpawnTransform(int index, out Vector3 position, out Quaternion rotation)
        {
            if (index >= 0 && index < spawnPoints.Count && spawnPoints[index] != null)
            {
                Transform spawn = spawnPoints[index].transform;
                position = spawn.position;
                rotation = Quaternion.Euler(0f, spawn.eulerAngles.y, 0f);
                return;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        /// <summary>Turns off cameras and audio listeners that are not the player's, so the player's view takes over.</summary>
        private static void DisableSceneCameras()
        {
            StraferCharacter[] characters = FindObjectsByType<StraferCharacter>(FindObjectsSortMode.None);
            Camera playerCamera = null;
            foreach (StraferCharacter character in characters)
            {
                if (character.PlayerControlled && character.ViewCamera != null)
                {
                    playerCamera = character.ViewCamera;
                    break;
                }
            }

            if (playerCamera == null)
            {
                return;
            }

            foreach (Camera sceneCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (sceneCamera != playerCamera)
                {
                    sceneCamera.gameObject.SetActive(false);
                }
            }
        }
    }
}
