using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class TerrainManager : MonoBehaviour {

    public TerrainMesh terrainPrefab;
    public CactusMesh cactusPrefab;
    public Transform DebugCenterMarker;

    const float gridSize = 200;
    const float gridOffset = gridSize * 1.5f;
    const float halfWidth = gridSize * 0.5f;
    const int cactusDist = 15;
    private FirstPersonController player;
    Vector3 currentCenter = new Vector3(gridOffset, 0, gridOffset);
    TerrainMesh[] land = new TerrainMesh[9];
    float cooldownEnd;
    

    private void Start()
    {
        player = FindObjectOfType<FirstPersonController>();
        MakeDesert();
        StartCoroutine(MakeCactusesAsyncSpiral(currentCenter.x,currentCenter.z));
        DebugCenterMarker.transform.position = currentCenter;
    }

    private float GetHeight(float x, float z)
    {
        return TerrainMesh.CalculateCombinedPerlin(x,z,terrainPrefab.OuterScale, terrainPrefab.OuterHeight, terrainPrefab.InnerScale, terrainPrefab.InnerHeight);
    }

    private void MakeDesert()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                var landSquare = GameObject.Instantiate<TerrainMesh>(terrainPrefab);
                landSquare.gameObject.name = "Terrain (" + x + "," + z + ")";
                land[x * 3 + z] = landSquare;
            }
        }
        MoveDesert(0, 0);
    }

    private void MoveDesert(int xMove, int zMove)
    {
        bool forceUpdateAll = xMove == 0 && zMove == 0;
        currentCenter += new Vector3(gridSize * xMove, 0, gridSize * zMove);
        DebugCenterMarker.transform.position = currentCenter;
        TerrainMesh[] temp = new TerrainMesh[9];
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++) {
                int newX = (x - xMove) % 3;
                if (newX < 0) newX += 3;
                int newZ = (z - zMove) % 3;
                if (newZ < 0) newZ += 3;
                var landSquare = land[(x * 3) + z];
                temp[newX * 3 + newZ] = landSquare;

                if (forceUpdateAll || (xMove > 0 && x < 1 || xMove < 0 && x > 1 || zMove > 0 && z < 1 || zMove < 0 && z > 1))
                {
                    var xOrigin = currentCenter.x - halfWidth;
                    var zOrigin = currentCenter.z - halfWidth;
                    var xPos = (newX - 1) * gridSize + xOrigin;
                    var zPos = (newZ - 1) * gridSize + zOrigin;
                    landSquare.transform.position = new Vector3(xPos, 0, zPos);
                    landSquare.UVOffset = new Vector2(xPos / gridSize, zPos / gridSize);
                    landSquare.Regenerate();
                }
                
            }
        }
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                land[x * 3 + z] = temp[x * 3 + z];
            }
        }
        cooldownEnd = Time.time + 2;
    }

    public void Update()
    {
        if (cooldownEnd < Time.time)
        {
            // move right
            if (player.transform.position.x > currentCenter.x + halfWidth)
            {
                MoveDesert(1, 0);
            }
            // move left
            else if (player.transform.position.x < currentCenter.x - halfWidth)
            {
                MoveDesert(-1, 0);
            }
            // move fwd
            else if (player.transform.position.z > currentCenter.z + halfWidth)
            {
                MoveDesert(0, 1);
            }
            // move back
            else if (player.transform.position.z < currentCenter.z - halfWidth)
            {
                MoveDesert(0, -1);
            }
        }
    }

    private IEnumerator MakeCactusesAsync(int xStart, int zStart)
    {
        for (int x = 0; x < (int)gridSize; x += cactusDist)
        {
            for (int z = 0; z < (int)gridSize; z += cactusDist)
            {
                yield return new WaitForSeconds(0.02f);
                var cactus = GameObject.Instantiate<CactusMesh>(cactusPrefab);
                cactus.transform.position = new Vector3(x + xStart, GetHeight(x + xStart, z + zStart), z + zStart);
            }
        }
        yield return null;
    }

    private IEnumerator MakeCactusesAsyncSpiral(float xStart, float zStart)
    {
        float cactusDistSq = cactusDist * cactusDist;
        Vector3 start = new Vector3(xStart, 0, zStart);

        for (int x = 1; x < (int)gridSize; x += cactusDist)
        {
            for (int z = 0; z < 360; z += 360 / x)
            {
                yield return new WaitForSeconds(0.02f);
                var cactus = GameObject.Instantiate<CactusMesh>(cactusPrefab);
                Vector3 pos = start + Quaternion.Euler(0, z, 0) * Vector3.forward * (x/3);
                cactus.transform.position = pos + Vector3.up * GetHeight(pos.x, pos.z);
            }
        }
        yield return null;
    }
}
