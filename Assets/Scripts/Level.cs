using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public enum GameModes
{
    Checkpoints = 1,
    CoopFive = 2
}
public class Level : MonoBehaviour
{
    public List<GravityField> Fields;
    public GravityField InitField;
    private List<GravityField> Fields_CheckpointSpawnable, Fields_PlayerSpawnable;
    public GameObject RotationalSystem;
    public float RotationalSpeed;

    public float MatchTime;

    void Awake()
    {
        Fields = transform.GetComponentsInChildren<GravityField>().ToList();
        Fields_CheckpointSpawnable = Fields.FindAll(f => f.FieldFlags.HasFlag(GravityField.Flags.CheckpointSpawnable));
        Fields_PlayerSpawnable = Fields.FindAll(f => f.FieldFlags.HasFlag(GravityField.Flags.PlayerSpawnable));
    }

    public GravityField RandomCheckpointField()
    {
        return Fields_CheckpointSpawnable[Random.Range(0, Fields_CheckpointSpawnable.Count)];
    }

    public GravityField RandomPlayerSpawnField()
    {
        return Fields_PlayerSpawnable[Random.Range(0, Fields_PlayerSpawnable.Count)];
    }

    void Update()
    {
        if (RotationalSystem)
            RotationalSystem.transform.Rotate(0, RotationalSpeed * Time.deltaTime, 0, Space.Self);
    }

}
