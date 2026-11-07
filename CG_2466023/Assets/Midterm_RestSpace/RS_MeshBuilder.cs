using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RS_MeshBuilder
{
    public readonly List<Vector3> vertices = new List<Vector3>();
    public readonly List<Vector3> normals = new List<Vector3>();
    public readonly List<Vector2> uvs = new List<Vector2>();
    public readonly List<Color> colors = new List<Color>();
    readonly List<List<int>> submeshes = new List<List<int>>();   // 재질마다 삼각형 목록 하나

    public RS_MeshBuilder(int submeshCount = 1)
    {
        for (int i = 0; i < submeshCount; i++) submeshes.Add(new List<int>());
    }

    // 기본 단위: 정점, 삼각형 =======================================================

    public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        return AddVertex(position, normal, uv, Color.white);
    }

    public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv, Color color)
    {
        vertices.Add(position);
        normals.Add(normal);
        uvs.Add(uv);
        colors.Add(color);
        return vertices.Count - 1;   // 방금 넣은 정점의 번호
    }

    // 바깥 방향(outward)에서 봤을 때 시계 방향이 되도록 순서를 맞춰 넣음
    public void AddTriangle(int sub, int a, int b, int c, Vector3 outward)
    {
        Vector3 faceNormal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        if (Vector3.Dot(faceNormal, outward) < 0f)
        {
            int temp = b; b = c; c = temp;   // 반대로 적혀 있으면 두 정점을 바꿈
        }
        submeshes[sub].Add(a);
        submeshes[sub].Add(b);
        submeshes[sub].Add(c);
    }

    // 순서를 그대로 넣음 (양면으로 그리는 풀잎처럼 앞뒤가 상관없을 때)
    public void AddTriangleRaw(int sub, int a, int b, int c)
    {
        submeshes[sub].Add(a);
        submeshes[sub].Add(b);
        submeshes[sub].Add(c);
    }

    // 사각형 = 삼각형 2개. p0 → p1 → p2 → p3 은 둘레를 따라 차례로
    public void AddQuad(int sub, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal,
                        Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        int i0 = AddVertex(p0, normal, uv0);
        int i1 = AddVertex(p1, normal, uv1);
        int i2 = AddVertex(p2, normal, uv2);
        int i3 = AddVertex(p3, normal, uv3);
        AddTriangle(sub, i0, i1, i2, normal);
        AddTriangle(sub, i0, i2, i3, normal);
    }

    //  육면체 =======================================================================

    // center: 중심, size: 가로·세로·깊이, rotation: 회전, uvScale: 1m에 텍스처 몇 장
    // 면마다 정점을 따로 둠 (6면 × 4 = 24개) → 면마다 normal이 따로 (S19의 Faceted와 같은 이유)
    public void AddBox(int sub, Vector3 center, Vector3 size, Quaternion rotation, float uvScale = 1f)
    {
        Vector3 h = size * 0.5f;
        Vector3 ax = rotation * Vector3.right;     // 상자의 x축
        Vector3 ay = rotation * Vector3.up;        // 상자의 y축
        Vector3 az = rotation * Vector3.forward;   // 상자의 z축

        AddBoxFace(sub, center, ax, ay, az, h.x, h.y, h.z, uvScale);   // +x 면
        AddBoxFace(sub, center, -ax, ay, az, h.x, h.y, h.z, uvScale);   // −x 면
        AddBoxFace(sub, center, ay, ax, az, h.y, h.x, h.z, uvScale);   // +y 면
        AddBoxFace(sub, center, -ay, ax, az, h.y, h.x, h.z, uvScale);   // −y 면
        AddBoxFace(sub, center, az, ax, ay, h.z, h.x, h.y, uvScale);   // +z 면
        AddBoxFace(sub, center, -az, ax, ay, h.z, h.x, h.y, uvScale);   // −z 면
    }

    public void AddBox(int sub, Vector3 center, Vector3 size, float uvScale = 1f)
    {
        AddBox(sub, center, size, Quaternion.identity, uvScale);
    }

    // n: 면의 바깥 방향, d: 중심에서 면까지 거리, t1/t2: 면 위의 두 방향, e1/e2: 그 방향의 반 길이
    void AddBoxFace(int sub, Vector3 center, Vector3 n, Vector3 t1, Vector3 t2,
                    float d, float e1, float e2, float uvScale)
    {
        Vector3 c = center + n * d;
        Vector3 p0 = c - t1 * e1 - t2 * e2;
        Vector3 p1 = c + t1 * e1 - t2 * e2;
        Vector3 p2 = c + t1 * e1 + t2 * e2;
        Vector3 p3 = c - t1 * e1 + t2 * e2;

        // UV: 실제 길이(m) × uvScale. 나뭇결(텍스처의 가로)이 긴 쪽을 따라가도록
        float L1 = 2f * e1 * uvScale, L2 = 2f * e2 * uvScale;
        if (e1 >= e2)
            AddQuad(sub, p0, p1, p2, p3, n, new Vector2(0, 0), new Vector2(L1, 0), new Vector2(L1, L2), new Vector2(0, L2));
        else
            AddQuad(sub, p0, p1, p2, p3, n, new Vector2(0, 0), new Vector2(0, L1), new Vector2(L2, L1), new Vector2(L2, 0));
    }

    // 원기둥 (반복문) =====================================================

    // from에서 to까지 이어지는 원기둥
    //   점 i의 각도 = 360° / n × i
    //   점 i의 위치 = 중심 + (u·cos(각도) + v·sin(각도)) × 반지름   (u, v: 축에 수직인 두 방향)
    public void AddCylinder(int sub, Vector3 from, Vector3 to, float radius, int segments,
                            bool caps = true, float uvScale = 1f)
    {
        Vector3 axis = to - from;
        float length = axis.magnitude;
        if (length < 1e-5f || segments < 3) return;
        Vector3 dir = axis / length;

        // 축에 수직인 두 방향 u, v 구하기 (외적 두 번)
        Vector3 helper = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
        Vector3 u = Vector3.Cross(dir, helper).normalized;
        Vector3 v = Vector3.Cross(dir, u);

        float circumference = 2f * Mathf.PI * radius;

        // 1. 옆면: 아래 원, 위 원의 점을 한 쌍씩 (UV 이음매 때문에 n + 1쌍)
        int sideStart = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            Vector3 radial = u * Mathf.Cos(angle) + v * Mathf.Sin(angle);   // 옆면의 normal = 바깥 방향
            float around = circumference * i / segments * uvScale;
            AddVertex(from + radial * radius, radial, new Vector2(0f, around));
            AddVertex(to + radial * radius, radial, new Vector2(length * uvScale, around));
        }
        for (int i = 0; i < segments; i++)
        {
            int b0 = sideStart + 2 * i;   // i번째 아래
            int t0 = b0 + 1;              // i번째 위
            int b1 = b0 + 2;              // i+1번째 아래
            int t1 = b0 + 3;              // i+1번째 위
            Vector3 outward = normals[b0] + normals[b1];
            AddTriangle(sub, b0, b1, t1, outward);
            AddTriangle(sub, b0, t1, t0, outward);
        }

        if (!caps) return;

        // 2. 뚜껑 두 개: 가운데 점 + 원 위의 이웃한 두 점
        AddCap(sub, from, -dir, u, v, radius, segments, uvScale);
        AddCap(sub, to, dir, u, v, radius, segments, uvScale);
    }

    void AddCap(int sub, Vector3 center, Vector3 normal, Vector3 u, Vector3 v,
                float radius, int segments, float uvScale)
    {
        int c = AddVertex(center, normal, Vector2.one * 0.5f);
        int ringStart = vertices.Count;
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            float cs = Mathf.Cos(angle), sn = Mathf.Sin(angle);
            Vector3 p = center + (u * cs + v * sn) * radius;
            AddVertex(p, normal, new Vector2(0.5f + cs * radius * uvScale, 0.5f + sn * radius * uvScale));
        }
        for (int i = 0; i < segments; i++)
        {
            int a = ringStart + i;
            int b = ringStart + (i + 1) % segments;   // 마지막 점 다음은 다시 0번
            AddTriangle(sub, c, a, b, normal);
        }
    }

    //  n각형 판 (반복문) ===================================================================

    // 위(+y)를 바라보는 원판. uvScale: 1m에 텍스처 몇 장
    public void AddDisc(int sub, Vector3 center, float radius, int segments, float uvScale = 1f)
    {
        Vector3 up = Vector3.up;
        int c = AddVertex(center, up, new Vector2(center.x, center.z) * uvScale);
        int ringStart = vertices.Count;
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            Vector3 p = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            AddVertex(p, up, new Vector2(p.x, p.z) * uvScale);
        }
        for (int i = 0; i < segments; i++)
            AddTriangle(sub, c, ringStart + i, ringStart + (i + 1) % segments, up);
    }

    //  Mesh로 옮기기 =============================================================================

    public void FillMesh(Mesh mesh)
    {
        mesh.Clear();
        // 정점이 65535개를 넘으면 32비트 번호가 필요함 (풀밭)
        mesh.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.subMeshCount = submeshes.Count;
        for (int i = 0; i < submeshes.Count; i++)
            mesh.SetTriangles(submeshes[i], i);
        mesh.RecalculateBounds();
    }

    
    
}