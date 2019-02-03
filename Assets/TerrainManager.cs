using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour {

    public TerrainMesh terrainPrefab;
    public CactusMesh cactusPrefab;

    private void Start()
    {
        StartCoroutine(MakeCactusesAsync());
    }

    private float GetHeight(float x, float z)
    {
        return TerrainMesh.CalculateCombinedPerlin(x,z,terrainPrefab.OuterScale, terrainPrefab.OuterHeight, terrainPrefab.InnerScale, terrainPrefab.InnerHeight);
    }

    private IEnumerator MakeCactusesAsync()
    {
        int xStart = 200;
        int zStart = 200;
        for (int x = 0; x < 200; x += 25)
        {
            for (int z = 0; z < 200; z += 25)
            {
                yield return new WaitForSeconds(0.1f);
                var cactus = GameObject.Instantiate<CactusMesh>(cactusPrefab);
                cactus.transform.position = new Vector3(x + xStart, GetHeight(x + xStart, z + zStart), z + zStart);
            }
        }
    }
}
