using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainMesh : MonoBehaviour
{
    public bool AutoDetectUpdates = true;

    [Header("Land Settings")]
    [Range(2, 210)]
    public int Meridians; // minimum 2
    [Range(2, 210)]
    public int Parallels; // minimum 2
    [Range(0.01f, 0.1f)]
    public float OuterScale;
    [Range(0.1f, 0.99f)]
    public float InnerScale;
    [Range(1f, 100f)]
    public float OuterHeight;
    [Range(1f, 100f)]
    public float InnerHeight;
    public Vector2 UVOffset;

    [Header("Debug Settings")]
    [Range(0, 1)]
    public float DebugWaitDuration;

    private bool debug = false;

    private const bool depthTest = true;

    private int prevMeridians;
    private int prevParallels;
    private float prevOuterScale; // the scale of the perlin noise coordinates compared to the mesh coordinates
    private float prevInnerScale; // the scale of the perlin noise coordinates compared to the mesh coordinates
    private float prevOuterHeight;
    private float prevInnerHeight;

    MeshFilter mf;
    List<Vector2> uvs;
    List<int> indicies;

    private void Update()
    {
        if (!AutoDetectUpdates) return;
        if (prevMeridians != Meridians ||
            prevParallels != Parallels ||
            prevOuterScale != OuterScale ||
            prevInnerScale != InnerScale ||
            prevOuterHeight != OuterHeight ||
            prevInnerHeight != InnerHeight)
        {
            prevMeridians = Meridians;
            prevParallels = Parallels;
            prevOuterScale = OuterScale;
            prevInnerScale = InnerScale;
            prevOuterHeight = OuterHeight;
            prevInnerHeight = InnerHeight;
            Regenerate();
        }

    }

    public void Regenerate()
    {
        StopAllCoroutines();
        StartCoroutine(CreateMesh());
    }

    private IEnumerator CreateMesh()
    {
        // transition code, useless except to avoid rewriting
        int meridians = Meridians;
        int parallels = Parallels;

        debug = DebugWaitDuration > 0;
        if (meridians < 3 || parallels < 1) throw new System.Exception("Assert: Meridians must be >= 3, Parallels must be >= 1");
        int numVerts = meridians * parallels; // avoid recomputation, cache value of num verts
        if (numVerts >= 65000) throw new System.Exception("Assert: Num Verts must be < 65000");
        var waitInstruction = new WaitForSeconds(DebugWaitDuration);
        mf = GetComponent<MeshFilter>();
        Mesh newMesh = new Mesh();

        // VERTICIES
        List<Vector3> startingPoints = new List<Vector3>();
        
        for (int parallel = 0; parallel < parallels; parallel++)
        {
            for (int meridian = 0; meridian < meridians; meridian++)
            {
                float height = CalculateCombinedPerlin(meridian + transform.position.x, parallel + transform.position.z, OuterScale, OuterHeight, InnerScale, InnerHeight);
                Vector3 point = new Vector3(meridian, height, parallel);

                startingPoints.Add(point);
                if (debug) Debug.DrawLine(point, point + (point) * 0.05f, Color.red, 10000, depthTest);
            }
        }

        newMesh.SetVertices(startingPoints);

        // UVs
        uvs = new List<Vector2>();
        for (float y = 0; y < parallels; y++)
        {
            for (float x = 0; x < meridians; x++)
            {
                uvs.Add(new Vector2(x / (float)(meridians-1) + UVOffset.x, y / (float)(parallels-1) + UVOffset.y));
            }
        }
        newMesh.SetUVs(0, uvs);

        // INDICIES (TRIS)
        indicies = new List<int>();
        int o = 0; // offset

        // (quads)
        for (int p = 0; p < parallels - 1; p++)
        {
            for (int m = 0; m < meridians - 1; m++)
            {
                int i = p * meridians + m;

                // faces are quads (two tris)
                indicies.Add(o + i + meridians + 1);
                indicies.Add(o + i + 1);
                indicies.Add(o + i);

                indicies.Add(o + i + meridians);
                indicies.Add(o + i + meridians + 1);
                indicies.Add(o + i);

                // DEBUG 
                if (debug)
                {
                    var color = new Color(i / (float)numVerts, 0, i / (float)numVerts);
                    // draw triangles
                    int length = indicies.Count;
                    yield return waitInstruction;
                    Debug.DrawLine(startingPoints[indicies[length - 6]], startingPoints[indicies[length - 5]], color, 10000, depthTest);
                    yield return waitInstruction;
                    Debug.DrawLine(startingPoints[indicies[length - 5]], startingPoints[indicies[length - 4]], color, 10000, depthTest);
                    yield return waitInstruction;
                    Debug.DrawLine(startingPoints[indicies[length - 4]], startingPoints[indicies[length - 6]], color, 10000, depthTest);
                    yield return waitInstruction;

                    Debug.DrawLine(startingPoints[indicies[length - 3]], startingPoints[indicies[length - 2]], color, 10000, depthTest);
                    yield return waitInstruction;
                    Debug.DrawLine(startingPoints[indicies[length - 2]], startingPoints[indicies[length - 1]], color, 10000, depthTest);
                    yield return waitInstruction;
                    Debug.DrawLine(startingPoints[indicies[length - 1]], startingPoints[indicies[length - 3]], color, 10000, depthTest);
                }
                DrawDebugPoint(startingPoints, i);
            }
        }

        newMesh.SetTriangles(indicies, 0);
        newMesh.name = "ProceduralMesh";
        newMesh.RecalculateNormals();
        mf.mesh = newMesh;
    }

    private void DrawDebugPoint(List<Vector3> points, int i)
    {
        // draw line ver each vert
        if (debug) Debug.DrawLine(points[i], points[i] + points[i].normalized * 0.05f, Color.yellow, 10000, depthTest);
    }

    public static float CalculateCombinedPerlin(float x, float y, float OuterScale, float OuterHeight, float InnerScale, float InnerHeight)
    {
        return Mathf.PerlinNoise(x * InnerScale, y * InnerScale) * InnerHeight +
                    Mathf.PerlinNoise(x * OuterScale, y * OuterScale) * OuterHeight;
    }
}
