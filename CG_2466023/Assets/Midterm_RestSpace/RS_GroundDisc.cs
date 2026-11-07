using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RS_GroundDisc : MonoBehaviour
{
    public float radius = 6f;
    [Range(3, 128)] public int segments = 64;
    [Tooltip("1m에 텍스처 몇 장 (0.25 = 4m마다 한 장)")]
    public float uvScale = 0.25f;

    Mesh mesh;
    bool dirty;

    void OnEnable() { Build(); }
    void OnValidate() { dirty = true; }
    void Update() { if (dirty) { dirty = false; Build(); } }

    void Build()
    {
        RS_MeshBuilder b = new RS_MeshBuilder(1);
        b.AddDisc(0, Vector3.zero, radius, segments, uvScale);

        if (mesh == null)
        {
            mesh = new Mesh { name = "RS_GroundDisc (코드로 만든 메시)" };
            mesh.hideFlags = HideFlags.DontSave;
        }
        b.FillMesh(mesh);
        GetComponent<MeshFilter>().sharedMesh = mesh;

        
    }
}