using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Seed")]
public class SeedProperty : SpawnerProperty
{
    [Header("Seed Property")]
    [SerializeField]
    private PlantProperty plantProperty;
    [SerializeField]
    private LayerMask farmPlotLayer;
    [SerializeField]
    private LayerMask plantLayer;

    [Header("Seed Sound Settings")]
    [SerializeField]
    private AudioClip plantSuccessSound;
    [SerializeField]
    private AudioClip plantFailSound;
    [SerializeField]
    private AudioClip plantMatureSound;
    [SerializeField]
    private AudioClip plantHarvestSound;
    [SerializeField]
    private AudioClip plantDestroySound;
    [SerializeField]
    private AudioClip plantGrowSound;

    public PlantProperty PlantProperty => plantProperty;
    public LayerMask FarmPlotLayer => farmPlotLayer;
    public LayerMask PlantLayer => plantLayer;

    public AudioClip PlantSuccessSound => plantSuccessSound;
    public AudioClip PlantFailSound => plantFailSound;
    public AudioClip PlantMatureSound => plantMatureSound;
    public AudioClip PlantHarvestSound => plantHarvestSound;
    public AudioClip PlantDestroySound => plantDestroySound;
    public AudioClip PlantGrowSound => plantGrowSound;

    public override ScriptableObject InitScript => plantProperty;
}
