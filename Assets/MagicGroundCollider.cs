using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicGroundCollider : MonoBehaviour {

    public Transform target;
    public TerrainMesh terrain;

    const float colliderHalfHeight = 0.5f;

    private void FixedUpdate()
    {
        float terrainTransformScale = terrain.transform.localScale.x;
        var p = target.position;
        var perlin = p / terrainTransformScale - terrain.transform.position;

        float normalizedX = perlin.x;
        float normalizedZ = perlin.z;

        float height = TerrainMesh.CalculateCombinedPerlin(normalizedX, normalizedZ, terrain.OuterScale, terrain.OuterHeight, terrain.InnerScale, terrain.InnerHeight);
        //float height = Mathf.PerlinNoise(normalizedX * terrain.InnerScale, normalizedZ * terrain.InnerScale) * terrain.InnerHeight + Mathf.PerlinNoise(normalizedX * terrain.OuterScale, normalizedZ * terrain.OuterScale) * terrain.OuterHeight;

        height *= terrainTransformScale;

        if (p.y < height) target.transform.position = new Vector3(p.x,height + colliderHalfHeight,p.z);

        transform.position = new Vector3(p.x, height - colliderHalfHeight, p.z);
    }
}
