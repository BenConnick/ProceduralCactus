using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CactusMesh))]
public class CactusRandomizer : MonoBehaviour {

    CactusMesh cactus;

	// Use this for initialization
	void OnEnable () {
        cactus = GetComponent<CactusMesh>();
        Randomize();
	}

    private void Randomize()
    {
        if (cactus == null) return;
        cactus.AutoDetectUpdates = false;
        cactus.NumBuds = Random.value > 0.9f ? 1 : Random.Range(1, 30);
        cactus.Meridians = 3 + (int)Random.Range(4f,40f / (1+Mathf.Log((float)cactus.NumBuds)));
        cactus.Parallels = 1 + (int)Random.Range(4f, 40f / (1+Mathf.Log((float)cactus.NumBuds)));
        cactus.Midsections = Random.Range(0, 10);
        const float curveConstant = 5;
        cactus.Radius = Random.Range(0.3f, 1.15f) * curveConstant / (cactus.NumBuds + curveConstant-1);
        cactus.CapsuleHeight = Random.Range(0f, cactus.Radius * 1.5f);
        cactus.Pivot = new Vector3(Random.Range(0f,cactus.Radius),cactus.CapsuleHeight/2f,0); // determined by height
        cactus.IndentPercent = cactus.NumBuds == 1 ? Random.Range(0,0.5f) : 0;
        cactus.PosOffset = new Vector3(0, 0, Random.Range(0f, cactus.Radius * 0.2f)); // determined by height
        cactus.RotOffset = new Vector3(Random.Range(0f,2f), Random.value > 0.5f ? 100f : 137.5f, Random.Range(0f, 2f));
        cactus.SclOffset = new Vector3(Random.Range(0f, 0.01f), Random.Range(0f, 0.01f), Random.Range(0f, 0.01f));
        cactus.LocalRot = new Vector3(Random.Range(0.9f, 3.1f), Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f));
        cactus.Flat = cactus.NumBuds == 1 ? 0 : (Random.Range(0.1f,0.99f));
        var taperSqrt = Random.Range(0.2f, 1.1f);
        cactus.Taper = taperSqrt * taperSqrt;
        cactus.CapsuleHeightOffset = Random.Range(0f, 0.0005f);
        cactus.TipHeightPercent = cactus.NumBuds == 1 ? 0 : Random.Range(-0.1f, 0.5f);
        cactus.BaseColor = Random.ColorHSV();
        cactus.TopColor = new Color(124f / 255f, 173f / 255f, 141 / 255f);
        float lightness = Random.Range(-0.1f, 0.1f);
        cactus.TintOffset = new Vector4(lightness, lightness, lightness, 1);
        cactus.DebugWaitDuration = 0;
        cactus.Regenerate();
    }
}
