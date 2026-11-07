using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RS_GrassField : MonoBehaviour
{
    [Header("풀밭")]
    public float radius = 5.6f;                // 풀을 심을 원의 반지름
    [Range(100, 150000)] public int bladeCount = 45000;
    public int seed = 7;                       // 같은 seed면 늘 같은 자리에 심음

    [Header("풀잎 하나 (m)")]
    public Vector2 heightRange = new Vector2(0.12f, 0.32f);
    public Vector2 widthRange = new Vector2(0.025f, 0.045f);
    public Vector2 bendRange = new Vector2(0.1f, 0.45f);   // 높이에 대한 휘는 정도
    [Range(2, 6)] public int bladeSegments = 3;            // 잎을 세로로 몇 칸으로 나눌지

    Mesh mesh;
    bool dirty;

    void OnEnable() { Build(); }
    void OnValidate() { dirty = true; }
    void Update() { if (dirty) { dirty = false; Build(); } }

    void Build()
    {
        RS_MeshBuilder b = new RS_MeshBuilder(1);
        System.Random rng = new System.Random(seed);

        for (int n = 0; n < bladeCount; n++)
        {
            // 1. 뿌리 위치: 원 안에 고르게 (반지름에 √를 씌워야 가운데에 몰리지 않음)
            float r = radius * Mathf.Sqrt(Rand(rng));
            float a = 2f * Mathf.PI * Rand(rng);
            Vector3 root = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);

            // 2. 잎마다 다른 크기, 방향, 휘는 정도
            float h = Mathf.Lerp(heightRange.x, heightRange.y, Rand(rng));
            float w = Mathf.Lerp(widthRange.x, widthRange.y, Rand(rng));
            float bend = Mathf.Lerp(bendRange.x, bendRange.y, Rand(rng)) * h;
            float yaw = 2f * Mathf.PI * Rand(rng);
            Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));     // 잎의 폭 방향
            Vector3 forward = new Vector3(-Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));  // 잎이 휘는 방향
            Color data = new Color(Rand(rng), Rand(rng), 0f, 1f);

            // 3. 정점: 높이 t = 0, 1/S, 2/S ... 마다 왼쪽, 오른쪽 한 쌍 + 맨 끝 한 개
            //    폭은 (1 − t)배로 좁아지고, 앞으로 bend × t² 만큼 휨
            int start = b.vertices.Count;
            int S = bladeSegments;
            for (int k = 0; k < S; k++)
            {
                float t = (float)k / S;
                Vector3 center = root + Vector3.up * (h * t) + forward * (bend * t * t);
                float half = w * 0.5f * (1f - t);
                b.AddVertex(center - right * half, Vector3.up, new Vector2(0f, t), data);
                b.AddVertex(center + right * half, Vector3.up, new Vector2(1f, t), data);
            }
            Vector3 tip = root + Vector3.up * h + forward * bend;
            b.AddVertex(tip, Vector3.up, new Vector2(0.5f, 1f), data);

            // 4. 삼각형: 칸마다 사각형(삼각형 2개), 맨 위는 삼각형 1개
            //    셰이더에서 Cull Off(양면)로 그리므로 순서는 신경 쓰지 않음
            for (int k = 0; k < S - 1; k++)
            {
                int l0 = start + 2 * k, r0 = l0 + 1;
                int l1 = l0 + 2, r1 = l0 + 3;
                b.AddTriangleRaw(0, l0, l1, r0);
                b.AddTriangleRaw(0, r0, l1, r1);
            }
            int lastL = start + 2 * (S - 1);
            b.AddTriangleRaw(0, lastL, start + 2 * S, lastL + 1);
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "RS_GrassField (코드로 만든 메시)" };
            mesh.hideFlags = HideFlags.DontSave;
        }
        b.FillMesh(mesh);

        // 셰이더가 바람으로 잎을 밀어내므로, 화면 밖 판정용 상자(bounds)를 조금 넉넉하게
        Bounds bounds = mesh.bounds;
        bounds.Expand(0.5f);
        mesh.bounds = bounds;

        GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // 재질이 비어 있으면 풀 셰이더(CG/RS_Grass)로 재질을 만들어 붙임
        Material current = meshRenderer.sharedMaterial;
        bool broken = current == null || current.shader == null || !current.shader.isSupported
                   || current.shader.name == "Hidden/InternalErrorShader";
        Shader grassShader = Shader.Find("CG/RS_Grass");
        if (broken && grassShader != null)
            meshRenderer.sharedMaterial = new Material(grassShader) { name = "RS_Grass (자동 생성)" };
        
    }

    static float Rand(System.Random rng) { return (float)rng.NextDouble(); }
}