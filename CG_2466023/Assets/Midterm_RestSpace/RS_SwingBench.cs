using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RS_SwingBench : MonoBehaviour
{
    [Header("벤치 크기 (m)")]
    public float seatWidth = 1.5f;
    public float seatDepth = 0.55f;
    public float hangLength = 1.6f;     // 회전축에서 좌판까지 거리 (밧줄 길이)
    public float backHeight = 0.55f;    // 등받이 높이
    [Range(0f, 30f)] public float backTilt = 12f;   // 등받이가 뒤로 기운 각도
    public float armHeight = 0.25f;     // 팔걸이 높이 (좌판 위로)
    [Range(2, 12)] public int seatSlatCount = 6;    // 좌판 널빤지 개수
    [Range(2, 10)] public int backSlatCount = 5;    // 등받이 널빤지 개수
    public bool addCushion = true;
    public float ropeRadius = 0.012f;
    public float uvScale = 1f;

    [Header("흔들기 (Play 중)")]
    [Range(0f, 30f)] public float amplitude = 10f;  // 최대 각도(도)
    public float period = 3f;                       // 한 번 왕복하는 시간(초)
    public Vector3 pivot = Vector3.zero;            // 회전축이 지나는 점 (이 오브젝트 좌표)

    [Header("Play 전 미리 보기: 이 각도로 기울여 둠")]
    [Range(-30f, 30f)] public float previewAngle = 0f;

    public float CurrentAngle { get; private set; }

    Mesh mesh;
    Vector3[] baseVertices;   // 흔들기 전 원래 정점
    Vector3[] baseNormals;
    Vector3[] verts;          // 매 프레임 계산한 정점 (배열을 다시 쓰려고 보관)
    Vector3[] norms;
    bool dirty;

    void OnEnable() { Build(); }
    void OnValidate() { dirty = true; }

    void Update()
    {
        if (dirty) { dirty = false; Build(); }
        if (baseVertices == null) return;

        // 1. 이번 프레임의 각도 θ = amplitude × sin(2π t / period)
        float angle;
        if (Application.isPlaying)
            angle = amplitude * Mathf.Sin(2f * Mathf.PI * Time.time / Mathf.Max(0.1f, period));
        else
            angle = previewAngle;
        CurrentAngle = angle;

        // 2. 행렬 합성: 회전축으로 옮기고(T(−pivot)) → x축으로 돌리고(Rx) → 제자리로(T(pivot))
        //    오른쪽 행렬부터 정점에 적용됨
        Matrix4x4 M = TranslationMatrix(pivot) * RotationXMatrix(angle) * TranslationMatrix(-pivot);

        // 3. 정점마다 한 번 곱함 (S08, S10과 같은 방식)
        for (int i = 0; i < baseVertices.Length; i++)
        {
            verts[i] = M.MultiplyPoint3x4(baseVertices[i]);   // 위치: (x, y, z, 1) — 이동까지 적용
            norms[i] = M.MultiplyVector(baseNormals[i]);      // normal: (x, y, z, 0) — 회전만 적용
        }
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.RecalculateBounds();
    }

    // x축 회전 행렬 (S09 RotationXMatrixRaw와 같은 값)
    // 1열: e₁ → (1, 0, 0) / 2열: e₂ → (0, c, s) / 3열: e₃ → (0, −s, c) / 4열: 원점 그대로
    Matrix4x4 RotationXMatrix(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        Matrix4x4 R = Matrix4x4.identity;
        R.m11 = c; R.m12 = -s;
        R.m21 = s; R.m22 = c;
        return R;
    }

    // 이동 행렬: 4열에 이동량
    Matrix4x4 TranslationMatrix(Vector3 t)
    {
        Matrix4x4 T = Matrix4x4.identity;
        T.m03 = t.x;
        T.m13 = t.y;
        T.m23 = t.z;
        return T;
    }

    // 벤치 메시 만들기 ============================================================================

    void Build()
    {
        const int WOOD = 0, ROPE = 1, CUSHION = 2;
        RS_MeshBuilder b = new RS_MeshBuilder(3);

        float W = seatWidth, D = seatDepth;
        float seatY = pivot.y - hangLength;      // 좌판 높이 (회전축 아래)
        float slatT = 0.035f;                    // 널빤지 두께

        // 1. 좌판 널빤지: 앞(−z)에서 뒤(+z)로 seatSlatCount개를 고르게
        float slatD = D / seatSlatCount * 0.8f;  // 널빤지 하나의 폭 (사이에 틈 20%)
        for (int i = 0; i < seatSlatCount; i++)
        {
            float t = seatSlatCount == 1 ? 0.5f : (float)i / (seatSlatCount - 1);
            float z = Mathf.Lerp(-D * 0.5f + slatD * 0.5f, D * 0.5f - slatD * 0.5f, t);
            b.AddBox(WOOD, new Vector3(0f, seatY, z), new Vector3(W, slatT, slatD), uvScale);
        }

        // 2. 좌판 아래 틀: 양옆 + 앞뒤
        float railY = seatY - 0.05f;
        for (int side = -1; side <= 1; side += 2)
            b.AddBox(WOOD, new Vector3(side * (W * 0.5f - 0.04f), railY, 0f), new Vector3(0.06f, 0.07f, D), uvScale);
        b.AddBox(WOOD, new Vector3(0f, railY, -D * 0.5f + 0.03f), new Vector3(W, 0.07f, 0.06f), uvScale);
        b.AddBox(WOOD, new Vector3(0f, railY, D * 0.5f - 0.03f), new Vector3(W, 0.07f, 0.06f), uvScale);

        // 3. 등받이: 뒤로 backTilt만큼 기운 판. Euler(+θ, 0, 0)이면 위쪽(y)이 뒤쪽(+z)으로 기욺
        Quaternion backRot = Quaternion.Euler(backTilt, 0f, 0f);
        Vector3 backUp = backRot * Vector3.up;              // 등받이를 따라 올라가는 방향
        Vector3 backFront = backRot * Vector3.back;         // 등받이 앞면이 보는 방향
        Vector3 backBase = new Vector3(0f, seatY + slatT * 0.5f, D * 0.5f - 0.03f);

        float backSlatH = backHeight / backSlatCount * 0.7f;
        for (int j = 0; j < backSlatCount; j++)
        {
            float along = 0.06f + backHeight * (j + 0.5f) / backSlatCount;
            b.AddBox(WOOD, backBase + backUp * along, new Vector3(W - 0.08f, backSlatH, 0.03f), backRot, uvScale);
        }
        // 등받이 기둥 두 개
        Vector3[] backPostTop = new Vector3[2];
        for (int k = 0; k < 2; k++)
        {
            int side = k == 0 ? -1 : 1;
            Vector3 sideOffset = Vector3.right * side * (W * 0.5f - 0.03f);
            float postLen = backHeight + 0.12f;
            b.AddBox(WOOD, backBase + sideOffset + backUp * (postLen * 0.5f) + backFront * -0.02f,
                     new Vector3(0.06f, postLen, 0.05f), backRot, uvScale);
            backPostTop[k] = backBase + sideOffset + backUp * postLen + backFront * -0.02f;
        }

        // 4. 팔걸이 + 앞 기둥
        float armY = seatY + armHeight;
        float armX = W * 0.5f + 0.02f;
        for (int side = -1; side <= 1; side += 2)
        {
            b.AddBox(WOOD, new Vector3(side * armX, armY, 0f), new Vector3(0.07f, 0.04f, D + 0.06f), uvScale);
            b.AddBox(WOOD, new Vector3(side * armX, seatY + armHeight * 0.5f, -D * 0.5f + 0.03f),
                     new Vector3(0.05f, armHeight, 0.05f), uvScale);
        }

        // 5. 밧줄: 회전축 위의 점에서 팔걸이 앞끝, 등받이 기둥 꼭대기로 (옆에서 보면 V자)
        for (int k = 0; k < 2; k++)
        {
            int side = k == 0 ? -1 : 1;
            Vector3 frontEnd = new Vector3(side * armX, armY + 0.02f, -D * 0.5f + 0.02f);
            Vector3 frontTop = new Vector3(side * armX, pivot.y, pivot.z);
            Vector3 backTop = new Vector3(backPostTop[k].x, pivot.y, pivot.z);
            b.AddCylinder(ROPE, frontTop, frontEnd, ropeRadius, 8, true, 4f);
            b.AddCylinder(ROPE, backTop, backPostTop[k], ropeRadius, 8, true, 4f);
            // 들보에 감은 고리
            float x0 = Mathf.Min(frontTop.x, backTop.x) - 0.02f;
            float x1 = Mathf.Max(frontTop.x, backTop.x) + 0.02f;
            b.AddCylinder(ROPE, new Vector3(x0, pivot.y, pivot.z), new Vector3(x1, pivot.y, pivot.z), 0.085f, 12, true, 4f);
        }

        // 6. 쿠션 (좌판 위 + 등받이 앞)
        if (addCushion)
        {
            b.AddBox(CUSHION, new Vector3(0f, seatY + slatT * 0.5f + 0.045f, -0.01f),
                     new Vector3(W - 0.14f, 0.09f, D - 0.08f), uvScale);
            b.AddBox(CUSHION, backBase + backUp * (0.06f + backHeight * 0.5f) + backFront * 0.05f,
                     new Vector3(W - 0.2f, backHeight * 0.8f, 0.07f), backRot, uvScale);
        }

        // 메시에 넣고, 원래 정점과 normal을 보관 (흔들 때 여기에 행렬을 곱함)
        if (mesh == null)
        {
            mesh = new Mesh { name = "RS_SwingBench (코드로 만든 메시)" };
            mesh.hideFlags = HideFlags.DontSave;
            mesh.MarkDynamic();   // 매 프레임 정점을 바꾼다고 GPU에 알려 줌
        }
        b.FillMesh(mesh);
        GetComponent<MeshFilter>().sharedMesh = mesh;

        baseVertices = b.vertices.ToArray();
        baseNormals = b.normals.ToArray();
        verts = new Vector3[baseVertices.Length];
        norms = new Vector3[baseNormals.Length];

        
    }

    // Scene 뷰: 선택하면 회전축(빨강)과 지금 각도의 좌판 위치(노랑)를 그림
    void OnDrawGizmosSelected()
    {
        Vector3 a = transform.TransformPoint(pivot + Vector3.left * (seatWidth * 0.5f + 0.3f));
        Vector3 c = transform.TransformPoint(pivot + Vector3.right * (seatWidth * 0.5f + 0.3f));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(a, c);

        float rad = CurrentAngle * Mathf.Deg2Rad;
        Vector3 seatLocal = pivot + new Vector3(0f, -Mathf.Cos(rad), -Mathf.Sin(rad)) * hangLength;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.TransformPoint(pivot), transform.TransformPoint(seatLocal));
        Gizmos.DrawWireSphere(transform.TransformPoint(seatLocal), 0.05f);
    }
}