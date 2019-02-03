using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CactusMesh))]
public class CactusRandomizer : MonoBehaviour {

    CactusMesh cactus;

	// Use this for initialization
	void Start () {
        cactus = GetComponent<CactusMesh>();
        Randomize();
	}

    private void Randomize()
    {
        if (cactus == null) return;
        cactus.AutoDetectUpdates = false;
        cactus.NumBuds = Random.value > 0.9f ? 1 : Random.Range(1, 30);
        cactus.Meridians = Random.Range(4,40);
        cactus.Parallels = Random.Range(4,40);
        cactus.Midsections = Random.Range(0, 5);
        cactus.Radius = Random.Range(0.8f, 1.15f) / Mathf.Sqrt(cactus.NumBuds);
        cactus.CapsuleHeight = Random.Range(0f, 0.5f);
        cactus.Pivot = new Vector3(Random.Range(0f,cactus.Radius),cactus.CapsuleHeight/2f,0); // determined by height
        cactus.IndentPercent = cactus.NumBuds == 1 ? Random.Range(0,0.5f) : 0;
        cactus.PosOffset = new Vector3(0, 0, Random.Range(0f, cactus.Radius * 0.2f)); // determined by height
        cactus.RotOffset = new Vector3(Random.Range(0f,2f), Random.value > 0.5f ? 100f : 137.5f, Random.Range(0f, 2f));
        cactus.SclOffset = new Vector3(Random.Range(0f, 0.01f), Random.Range(0f, 0.01f), Random.Range(0f, 0.01f));
        cactus.LocalRot = new Vector3(Random.Range(0.9f, 3.1f), Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f));
        cactus.Flat = cactus.NumBuds == 1 ? 1 : (Random.value > 0.5f ? 0.4f : 1f);
        //cactus.Taper = cactus.Taper; busted
        cactus.CapsuleHeightOffset = Random.Range(0f, 0.0001f);
        cactus.DebugWaitDuration = 0;
        cactus.Regenerate();
    }
}
