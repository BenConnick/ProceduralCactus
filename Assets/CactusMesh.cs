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
    [Range(0, 0.99f)]
    public float Flat;
    [Range(0, 1)]
    public float Taper;
    [Range(0,1)]
    public float TipHeightPercent;
    public Color BaseColor;
    public Color TopColor;

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
    public Vector4 TintOffset;

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
    private float prevTipHeightPercent;
    private Color prevBaseColor;
    private Color prevTopColor;
    private Vector4 prevTintOffset;

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
            prevCapsuleHeightOffset != CapsuleHeightOffset ||
            prevTipHeightPercent != TipHeightPercent ||
            prevBaseColor != BaseColor ||
            prevTopColor != TopColor ||
            prevTintOffset != TintOffset)
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
            prevTipHeightPercent = TipHeightPercent;
            prevBaseColor = BaseColor;
            prevTopColor = TopColor;
            prevTintOffset = TintOffset;
            Regenerate();
        }

    }

    public void Regenerate()
    {
        StopAllCoroutines();
        StartCoroutine(CreateMesh());
    }

    private Color GetTintedColor(int bud, int parallel) {
        Vector4 top = TopColor;
        Vector4 bot = BaseColor;
        Vector4 budResult = Vector4.Lerp(top, bot, parallel / (float)(Parallels + Midsections + 1));
        Vector4 tintResult = (bud / (float)NumBuds) * TintOffset;
        return (Color)(budResult + tintResult);
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

        int numPoints = numVerts * numBuds;

        float PreservePercent = 1 - IndentPercent;

        // VERTICIES
        List<Vector3> startingPoints = new List<Vector3>(numPoints);
        Color[] colors = new Color[numVerts * numBuds];
        for (int bud = 0; bud < numBuds; bud++)
        {
            BudTransform tf = new BudTransform();
            PrepTransform(ref tf, bud, SclOffset, RotOffset, PosOffset, Pivot, Flat, LocalRot);
            float midsectionHeight = capsuleHeight + bud * CapsuleHeightOffset;
            // create starting vert
            {
                Vector3 point = Vector3.up * radius;
                point += new Vector3(0, midsectionHeight / 2f + radius * TipHeightPercent * 0.5f, 0);
                point = ApplyTransform(point, ref tf);
                startingPoints.Add(point);
                colors[bud * numVerts] = GetTintedColor(bud,0);
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

                int inCactus = (parallel) / (parallels + midsections);
                int perCactus = parallel % (parallels + midsections);

                float xAngle = parallelPercent * 180;
                float xAngleRad = xAngle * Mathf.Deg2Rad;
                float sinX = Mathf.Sin(xAngleRad);
                float cosX = Mathf.Cos(xAngleRad);
                float taperAmount = 1 - (Taper * inCactus); // taper for a sharp point
                float y = radius * cosX;
                float z = -radius * sinX * taperAmount;


                for (int meridian = 0; meridian < meridians; meridian++)
                {
                    float meridianPercent = meridian / (float)meridians;

                    float yAngleRad = meridianPercent * (360f * Mathf.Deg2Rad);
                    float sinY = Mathf.Sin(yAngleRad);
                    float cosY = Mathf.Cos(yAngleRad);

                    float preserve = meridian % 2 == 0 ? PreservePercent : 0f;
                    float indentZ = preserve * z;


                    Vector3 point = new Vector3
                    {
                        x = indentZ * cosY,
                        y = y,
                        z = -indentZ * sinY 
                    };

                    if (mid) // midsection
                    {
                        int m = perCactus - (parallels >> 1);
                        point += new Vector3(0, midsectionHeight * 0.5f - (m / (float)midsections) * midsectionHeight, 0);
                    }
                    else
                    {
                        // midsection offset
                        if (top) // top
                        {
                            point += new Vector3(0, midsectionHeight / 2f, 0);
                            float temp = parallel + 1;
                            temp = 1 / (temp * temp);
                            point.y += radius * TipHeightPercent * temp;
                        }
                        else // bottom
                        {
                            point += new Vector3(0, -midsectionHeight / 2f, 0);
                        }
                    }

                    point = ApplyTransform(point, ref tf);
                    startingPoints.Add(point);
                    colors[bud * numVerts + (parallel-1) * meridians + meridian] = GetTintedColor(bud, parallel);
                    if (debug) Debug.DrawLine(point, point + (point) * 0.05f, Color.red, 10000, depthTest);
                }
            }
            // create ending vert
            {
                Vector3 point = Vector3.down * radius;
                point += new Vector3(0, -midsectionHeight / 2f, 0);
                point = ApplyTransform(point, ref tf);
                startingPoints.Add(point);
                colors[(bud+1) * numVerts -1] = GetTintedColor(bud, parallels + meridians + 1);
                if (debug) Debug.DrawLine(point, point + (point) * 0.05f, Color.red, 10000, depthTest);
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("set verts");
        newMesh.SetVertices(startingPoints);
        Profiler.EndSample();

        Profiler.BeginSample("set colors");
        newMesh.colors = colors;
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

    private struct BudTransform
    {
        public Vector3 pivot;
        public Quaternion pivotQuat;
        public Vector3 scale;
        public Vector3 totalOffset;
        public Quaternion leafQuat;
        public float flat;
    }

    private static void PrepTransform(ref BudTransform tf, int iteration, Vector3 scaleOffset, Vector3 rotationOffset, Vector3 positionOffset, Vector3 pivot, float flat, Vector3 localRot)
    {
        tf.pivot = pivot;
        tf.pivotQuat = Quaternion.Euler(localRot * iteration);
        tf.scale = Vector3.one + scaleOffset * iteration;
        tf.totalOffset = positionOffset * iteration;
        tf.leafQuat = Quaternion.Euler(rotationOffset * iteration);
        tf.flat = flat;
    }

    // application order is scale, rotate, move
    private static Vector3 ApplyTransform(Vector3 point, ref BudTransform tf)
    {

        //Profiler.BeginSample("Do Transform");

        // flat first
        float flattening = point.z < 0 ? 1.5f : 0.5f;
        point.z *= (1 - tf.flat * flattening);

        // then "pivot" then local rot
        Vector3 p1 = tf.pivotQuat * (point + tf.pivot);
        // first scale then offset
        Vector3 p2 = mul(p1, tf.scale) + tf.totalOffset;
        // finally rotation
        Vector3 p3 = (tf.leafQuat * p2);
        
        //Profiler.EndSample();
        
        return p3;
    }

    private static Vector3 mul(Vector3 a, Vector3 b)
    {
        return new Vector3
        {
            x = a.x * b.x,
            y = a.y * b.y,
            z = a.z * b.z
        };
    }
}

