using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class TerrainManager : MonoBehaviour {

    public TerrainMesh terrainPrefab;
    public CactusMesh cactusPrefab;
    public Transform DebugCenterMarker;

    const float gridSize = 200;
    const float gridOffset = gridSize * 1.5f;
    const float halfWidth = gridSize * 0.5f;
    private FirstPersonController player;
    Vector3 currentCenter = new Vector3(gridOffset, 0, gridOffset);
    TerrainMesh[] land = new TerrainMesh[9];
    float cooldownEnd;
    

    private void Start()
    {
        player = FindObjectOfType<FirstPersonController>();
        MakeDesert();
        StartCoroutine(MakeCactusesAsync());
        DebugCenterMarker.transform.position = currentCenter;
    }

    private float GetHeight(float x, float z)
    {
        return TerrainMesh.CalculateCombinedPerlin(x,z,terrainPrefab.OuterScale, terrainPrefab.OuterHeight, terrainPrefab.InnerScale, terrainPrefab.InnerHeight);
    }

    private void MakeDesert()
    {
        Debug.Log(string.Format("Start"));
        for (int x = 0; x < 3; x++)
        {
            string s = "[";
            for (int z = 0; z < 3; z++)
            {
                var xPos = (x-1) * gridSize + (currentCenter.x - halfWidth);
                var zPos = (z-1) * gridSize + (currentCenter.z - halfWidth);
                var landSquare = GameObject.Instantiate<TerrainMesh>(terrainPrefab);
                landSquare.gameObject.name = "Terrain (" + x + "," + z + ")";
                land[x * 3 + z] = landSquare;
                s += "" + land[x * 3 + z].name + ",";
                landSquare.transform.position = new Vector3(xPos, 0, zPos);
            }
            s += "]";
            Debug.Log(s);
        }
    }

    private void MoveDesert(int xMove, int zMove)
    {
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

                if (xMove > 0 && x < 1 || xMove < 0 && x > 1 || zMove > 0 && z < 1 || zMove < 0 && z > 1)
                {
                    var xPos = (newX - 1) * gridSize + (currentCenter.x - halfWidth);
                    var zPos = (newZ - 1) * gridSize + (currentCenter.z - halfWidth);
                    landSquare.transform.position = new Vector3(xPos, 0, zPos);
                    landSquare.Regenerate();
                }
                
            }
        }
        Debug.Log(string.Format("Move {0},{1}", xMove, zMove));
        for (int x = 0; x < 3; x++)
        {
            string s = "[";
            for (int z = 0; z < 3; z++)
            {
                land[x * 3 + z] = temp[x * 3 + z];
                s += "" + land[x * 3 + z].name + ",";
            }
            s += "]";
            Debug.Log(s);
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

    private IEnumerator MakeCactusesAsync()
    {
        int xStart = 200;
        int zStart = 200;
        for (int x = 0; x < 200; x += 25)
        {
            for (int z = 0; z < 200; z += 25)
            {
                //yield return new WaitForSeconds(0.02f);
                var cactus = GameObject.Instantiate<CactusMesh>(cactusPrefab);
                cactus.transform.position = new Vector3(x + xStart, GetHeight(x + xStart, z + zStart), z + zStart);
            }
        }
        yield return null;
    }
}
