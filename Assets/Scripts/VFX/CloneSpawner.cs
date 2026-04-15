using UnityEngine;

public class CloneSpawner : MonoBehaviour {

    [Header("Clone Spawn")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool useSpawnerRotation = true;
    [SerializeField] private bool destroySpawnerAfterSpawn = true;

    public void SpawnCloneFromAnimationEvent() {
        SpawnClone();
    }

    public GameObject SpawnClone() {
        if(clonePrefab == null) {
            Debug.LogWarning("CloneSpawner: clonePrefab is not assigned.", this);
            return null;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        Quaternion rotation = useSpawnerRotation ? transform.rotation : origin.rotation;

        GameObject clone = Instantiate(clonePrefab, origin.position, rotation);
        if(destroySpawnerAfterSpawn) {
            Destroy(this);
        }

        return clone;
    }
}
