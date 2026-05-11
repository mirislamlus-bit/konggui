using UnityEngine;

public sealed class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId;

    public string SpawnPointId => string.IsNullOrEmpty(spawnPointId) ? gameObject.name : spawnPointId;

    public void Configure(string id)
    {
        spawnPointId = id;
        gameObject.name = id;
    }
}
