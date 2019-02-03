using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(MeshFilter))]
public class CactusMesh : MonoBehaviour
{
    public bool AutoDetectUpdates = true;

    [Header("Bud Settings")]
    [Range(3, 50)]
    public int Meridians; // minimum 3
    [Range(1, 50)]
    public int Parallels; // minimum 1
    [Range(0, 100)]
    public int Midsections;
    [Range(0.01f, 10f)]
    public float Radius;
    [Range(0f, 10f)]
    public float CapsuleHeight;
    [Range(0f, 0.99f)]
    public float IndentPercent;
    public Vector3 Pivot;
    [Range(0.01f, 1)]
    public float Flat;
    [Range(0, 1)]
    public float Taper;

    [Header("Reproduction Settings")]
    [Range(1, 50)]
    public int NumBuds; // minimum 1
    public Vector3 PosOffset;
    public Vector3 RotOffset;
    [Range(-1, 1)]
    public float ScaleUniform;
    public Vector3 SclOffset;
    public Vector3 LocalRot;
    [Range(0f, 10f)]
    public float CapsuleHeightOffset;

    [Header("Debug Settings")]
    [Range(0, 1)]
    public float DebugWaitDuration;

    private bool debug = false;

    private const bool depthTest = true;

    private int prevMeridians;
    private int prevParallels;
    private int prevMidsection;
    private float prevRadius;
    private float prevIndentPercent;
    private float prevCapsuleHeight;
    private int prevNumBuds;
    private Vector3 prevPosOff;
    private Vector3 prevRotOff;
    private Vector3 prevSclOff;
    private Vector3 prevPivot;
    private float prevFlat;
    private float prevTaper;
    private Vector3 prevLocalRot;
    private float prevCapsuleHeightOffset;

    MeshFilter mf;
    List<Vector2> uvs;
    List<int> indicies;

    private void Update()
    {
        if (!AutoDetectUpdates) return;
        if (ScaleUniform != 0 && ScaleUniform != SclOffset.x)
        {
            SclOffset = new Vector3(ScaleUniform, ScaleUniform, ScaleUniform);
        }
        if (prevMeridians != Meridians ||
            prevParallels != Parallels ||
            prevRadius != Radius ||
            prevIndentPercent != IndentPercent ||
            prevMidsection != Midsections ||
            prevCapsuleHeight != CapsuleHeight ||
            prevNumBuds != NumBuds ||
            prevPosOff != PosOffset ||
            prevRotOff != RotOffset ||
            prevSclOff != SclOffset ||
            prevPivot != Pivot ||
            prevFlat != Flat ||
            prevTaper != Taper ||
            prevLocalRot != LocalRot ||
            prevCapsuleHeightOffset != CapsuleHeightOffset)
        {
            prevMeridians = Meridians;
            prevParallels = Parallels;
            prevRadius = Radius;
            prevIndentPercent = IndentPercent;
            prevMidsection = Midsections;
            prevCapsuleHeight = CapsuleHeight;
            prevNumBuds = NumBuds;
            prevPosOff = PosOffset;
            prevRotOff = RotOffset;
            prevSclOff = SclOffset;
            prevPivot = Pivot;
            prevFlat = Flat;
            prevTaper = Taper;
            prevLocalRot = LocalRot;
            prevCapsuleHeightOffset = CapsuleHeightOffset;
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
        int midsections = Midsections;
        int numBuds = NumBuds;
        float radius = Radius;
        float capsuleHeight = CapsuleHeight;

        debug = DebugWaitDuration > 0;
        if (meridians < 3 || parallels < 1) throw new System.Exception("Assert: Meridians must be >= 3, Parallels must be >= 1");
        int numVerts = meridians * (parallels + midsections) + 2; // avoid recomputation, cache value of num verts
        if (numVerts * numBuds >= 65000) throw new System.Exception("Assert: Num Verts must be < 65000");
        var waitInstruction = new WaitForSeconds(DebugWaitDuration);
        mf = GetComponent<MeshFilter>();
        Mesh newMesh = new Mesh();

        Profiler.BeginSample("make verts");
        // VERTICIES
        List<Vector3> startingPoints = new List<Vector3>();
        for (int bud = 0; bud < numBuds; bud++)
        {
            float midsectionHeight = capsuleHeight + bud * CapsuleHeightOffset;
            // create starting vert
            {
                Vector3 point = Vector3.up * radius;
                point += new Vector3(0, midsectionHeight / 2f, 0);
                point = DoTransform(point, bud, SclOffset, RotOffset, PosOffset, Pivot, Flat, LocalRot);
                startingPoints.Add(point);
                if (debug) Debug.DrawLine(point, point + (point) * 0.05f, Color.red, 10000, depthTest);
            }

            // parrallel = 1 instead of zero because we skipped the first one 
            for (int parallel = 1; parallel < parallels + 1 + midsections; parallel++)
            {
                const int numPoles = 2;
                if (parallel == parallels + numPoles + midsections - 1) continue;
                bool mid = true;
                bool top = true;
                float parallelPercent = 0.5f; // midsection
                if (parallel <= (parallels + numPoles) / 2) // top
                {
                    mid = false;
                    top = true;
                    parallelPercent = parallel / (float)(parallels + numPoles - 1);
                }
                else if (parallel >= (parallels + numPoles) / 2 + midsections)// bottom
                {
                    mid = false;
                    top = false;
                    parallelPercent = (parallel - midsections) / (float)(parallels + numPoles - 1);
                }
                float xAngle = parallelPercent * 180;
                for (int meridian = 0; meridian < meridians; meridian++)
                {
                    float meridianPercent = meridian / (float)meridians;
                    float yAngle = meridianPercent * 360;
                    //startingPoints[x * (gridWidth) + y] = new Vector3(x * cellWidth, y * cellWidth, 0
                    float rad = radius - radius * (Taper * (parallels - parallel) / parallels); // taper for a sharp point
                    Vector3 point = Quaternion.Euler(xAngle, yAngle, 0) * Vector3.up * rad;

                    // indents
                    float indentPercent = meridian % 2 == 0 ? IndentPercent : 0f;
                    var indent = Vector3.ProjectOnPlane(point, Vector3.up) * indentPercent;
                    point -= indent;

                    int perCactus = parallel % (parallels + midsections);

                    if (mid) // midsection
                    {
                        int m = perCactus - parallels / 2;
                        point += new Vector3(0, midsectionHeight / 2 - (m / (float)midsections) * midsectionHeight, 0);
                    }
                    else 
                    {
                        // midsection offset
                        if (top) // top
                        {
                            point += new Vector3(0, midsectionHeight / 2f, 0);
                        }
                        else // bottom
                        {
                            point += new Vector3(0, -midsectionHeight / 2f, 0);
                        }
                    }

                    point = DoTransform(point, bud, SclOffset, RotOffset, PosOffset, Pivot, Flat, LocalRot);
                    startingPoints.Add(point);
                    if (debug) Debug.DrawLine(point, point + (point) * 0.05f, Color.red, 10000, depthTest);
                }
            }
            // create ending vert
            {
                Vector3 point = Vector3.down * radius;
                point += new Vector3(0, -midsectionHeight / 2f, 0);
                point = DoTransform(point, bud, SclOffset, RotOffset, PosOffset, Pivot, Flat, LocalRot);
                startingPoints.Add(point);
                if (debug) Debug.DrawLine(point, point + (point) * 0.05f, Color.red, 10000, depthTest);
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("set verts");
        newMesh.SetVertices(startingPoints);
        Profiler.EndSample();

        // UVs
        Profiler.BeginSample("make uvs");
        uvs = new List<Vector2>();
        for (int bud = 0; bud < numBuds; bud++)
        {
            uvs.Add(new Vector2(1, 0));

            for (float y = 0; y < parallels + midsections; y++)
            {
                for (float x = 0; x < meridians; x++)
                {
                    uvs.Add(new Vector2(x / (float)(meridians), 2 * y / (float)(parallels + midsections)));
                }
            }
            uvs.Add(new Vector2(1, 1));
            
        }
        Profiler.EndSample();
        Profiler.BeginSample("Set UV");
        newMesh.SetUVs(0, uvs);
        Profiler.EndSample();

        // INDICIES (TRIS)
        Profiler.BeginSample("Add Indicies");
        indicies = new List<int>();
        for (int bud = 0; bud < numBuds; bud++)
        {
            int o = bud * numVerts; // offset

            // top (tris)
            for (int i = 1; i < meridians; i++)
            {
                indicies.Add(o);
                indicies.Add(o + i);
                indicies.Add(o + i + 1);
            }
            indicies.Add(o);
            indicies.Add(o + meridians);
            indicies.Add(o + 1);
            DrawDebugPoint(startingPoints, o);

            o += 1; // offset by "north pole" to make math easier for tube

            // tube (quads)
            for (int p = 0; p < parallels + midsections - 1; p++)
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
                // wrap around (tube)
                int i2 = p * meridians + meridians - 1;
                indicies.Add(o + i2 + 1);
                indicies.Add(o + i2 + 1 - meridians);
                indicies.Add(o + i2);

                indicies.Add(o + i2 + meridians);
                indicies.Add(o + i2 + 1);
                indicies.Add(o + i2);
                // DEBUG 
                if (debug)
                {
                    var color = new Color(i2 / (float)numVerts, 0, i2 / (float)numVerts);
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
                DrawDebugPoint(startingPoints, i2);
            }

            // change offset to make math easier
            o += numVerts-meridians-2;

            // bottom (tris)
            for (int i = 0; i < meridians-1; i++)
            {
                indicies.Add(o + i);
                indicies.Add(o + meridians);
                indicies.Add(o + i + 1);
                DrawDebugPoint(startingPoints, i);
            }
            // wrap
            indicies.Add(o + meridians - 1);
            indicies.Add(o + meridians);
            indicies.Add(o);
            
        }
        Profiler.EndSample();

        Profiler.BeginSample("Set indicies");
        newMesh.SetTriangles(indicies, 0);
        Profiler.EndSample();
        newMesh.name = "ProceduralMesh";
        Profiler.BeginSample("recalc normals");
        newMesh.RecalculateNormals();
        Profiler.EndSample();
        Profiler.BeginSample("set mesh");
        mf.mesh = newMesh;
        Profiler.EndSample();
    }

    private void DrawDebugPoint(List<Vector3> points, int i)
    {
        // draw line ver each vert
        if (debug) Debug.DrawLine(points[i], points[i] + points[i].normalized * 0.05f, Color.yellow, 10000, depthTest);
    }

    private void SaveFile()
    {
        SOSerializer.SerializeObject<CactusMesh>(this, System.DateTime.Now.ToString());
    }

    private void LoadFile(string fileName)
    {
        var cactus = SOSerializer.DeSerializeObject<CactusMesh>(fileName);
        this.Meridians = cactus.Meridians;
        this.Parallels = cactus.Parallels; 
        this.Midsections = cactus.Midsections;
        this.Radius = cactus.Radius;
        this.CapsuleHeight = cactus.CapsuleHeight;
        this.IndentPercent = cactus.IndentPercent;
        this.Pivot = cactus.Pivot;
        this.NumBuds = cactus.NumBuds;
        this.PosOffset = cactus.PosOffset;
        this.RotOffset = cactus.RotOffset;
        this.ScaleUniform = cactus.ScaleUniform;
        this.SclOffset = cactus.SclOffset;
        this.LocalRot = cactus.LocalRot;
        this.Flat = cactus.Flat;
        this.Taper = cactus.Taper;
        this.DebugWaitDuration = cactus.DebugWaitDuration;
}

    // application order is scale, rotate, move
    private static Vector3 DoTransform(Vector3 point, int iteration, Vector3 scaleOffset, Vector3 rotationOffset, Vector3 positionOffset, Vector3 pivot, float flat, Vector3 localRot)
    {
        Profiler.BeginSample("Do Transform");
        //Matrix4x4 rot = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(rotationOffset * iteration), Vector3.one);
        //Matrix4x4 scale = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one + scaleOffset * iteration);
        //Matrix4x4 trans = Matrix4x4.TRS(positionOffset * iteration, Quaternion.identity, Vector3.one);
       // Matrix4x4 trs = Matrix4x4.TRS(positionOffset * iteration, Quaternion.Euler(rotationOffset * iteration), Vector3.one + scaleOffset * iteration);
        
        //Matrix4x4 pivotMat = Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one);
        //Matrix4x4 localRotMat = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(localRot * iteration), Vector3.one);
        
        // flat first then "pivot" then local rot then other
        Vector3 p1 = Quaternion.Euler(localRot * iteration) * new Vector3(point.x + pivot.x, point.y + pivot.y, point.z * flat + pivot.z);
        Vector3 p2 = (Quaternion.Euler(rotationOffset * iteration) * p1);
        Vector3 scl = (Vector3.one + scaleOffset * iteration);
        Vector3 totalOffset = positionOffset * iteration;
        var p3 = new Vector3(p2.x * scl.x + totalOffset.x, p2.y * scl.y + totalOffset.y, p2.z * scl.z + totalOffset.z);
        Profiler.EndSample();
        
        return p3;
    }
}
