using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RS_SwingFrame : MonoBehaviour
{
    [Header("크기 (m)")]
    public float beamHeight = 2.2f;       // 들보 높이 (벤치가 매달리는 축)
    public float beamLength = 2.6f;       // 들보 길이
    public float beamRadius = 0.07f;
    public float legSideOffset = 1.15f;   // A자 다리의 x 위치 (가운데에서 양옆으로)
    public float legSpread = 0.95f;       // 바닥에서 다리가 앞뒤로 벌어진 거리
    public float legRadius = 0.055f;
    [Range(0.1f, 0.8f)] public float braceHeightRatio = 0.4f;   // 가로 버팀대 높이 (들보 높이에 대한 비율)

    [Header("원기둥을 몇 각형으로 (클수록 둥글게)")]
    [Range(3, 48)] public int segments = 16;

    [Header("텍스처: 1m에 몇 장")]
    public float uvScale = 1f;

    Mesh mesh;
    bool dirty;

    void OnEnable() { Build(); }
    void OnValidate() { dirty = true; }   // Inspector 값을 바꾸면 다음 Update에서 다시 만듦
    void Update() { if (dirty) { dirty = false; Build(); } }

    void Build()
    {
        RS_MeshBuilder b = new RS_MeshBuilder(1);

        // 1. 꼭대기 들보: x축 방향 원기둥
        b.AddCylinder(0,
            new Vector3(-beamLength * 0.5f, beamHeight, 0f),
            new Vector3(beamLength * 0.5f, beamHeight, 0f),
            beamRadius, segments, true, uvScale);

        // 2. 양쪽 A자 다리: side = −1(왼쪽), +1(오른쪽)
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * legSideOffset;
            Vector3 top = new Vector3(x, beamHeight, 0f);
            Vector3 front = new Vector3(x, 0f, -legSpread);
            Vector3 back = new Vector3(x, 0f, legSpread);

            // A자의 두 획
            b.AddCylinder(0, front, top, legRadius, segments, true, uvScale);
            b.AddCylinder(0, back, top, legRadius, segments, true, uvScale);

            // A자의 가로획: 높이 y에서 다리의 z 위치 = legSpread × (1 − y / beamHeight) (닮은 삼각형)
            float braceY = beamHeight * braceHeightRatio;
            float braceZ = legSpread * (1f - braceHeightRatio);
            b.AddCylinder(0,
                new Vector3(x, braceY, -braceZ),
                new Vector3(x, braceY, braceZ),
                legRadius * 0.8f, segments, true, uvScale);

            // 바닥 받침: 두 다리를 잇는 각목 (육면체)
            b.AddBox(0, new Vector3(x, 0.04f, 0f), new Vector3(0.12f, 0.08f, legSpread * 2f + 0.3f), uvScale);

            // 들보 끝을 감싸는 작은 고정 블록
            b.AddBox(0, new Vector3(x, beamHeight, 0f),
                     new Vector3(0.14f, beamRadius * 2.6f, beamRadius * 2.6f), uvScale);
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "RS_SwingFrame (코드로 만든 메시)" };
            mesh.hideFlags = HideFlags.DontSave;   // 씬에 저장하지 않고, 열 때마다 코드로 다시 만듦
        }
        b.FillMesh(mesh);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}