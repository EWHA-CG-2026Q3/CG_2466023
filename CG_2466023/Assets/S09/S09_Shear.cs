using UnityEngine;


[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public class S09_Shear : MonoBehaviour
{
    [SerializeField] float k = -0.8f;   // 학번 2466023

    DiamondMesh diamondMesh;

    void OnEnable()
    {
        diamondMesh = GetComponent<DiamondMesh>();
    }

    void Update()
    {
        if (diamondMesh == null || diamondMesh.BaseVertices == null) return;

        Vector3[] baseVertices = diamondMesh.BaseVertices;
        float[,] Sh = ShearMatrixRaw(k);
        Vector3[] verts = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
            verts[i] = FromHomogeneous(MultiplyMatrixVectorRaw(Sh, ToHomogeneous(baseVertices[i])));
        diamondMesh.SetVertices(verts);
    }

    
    void OnValidate()
    {
        float[,] Sh = ShearMatrixRaw(k);
        Vector4 h = ToHomogeneous(new Vector3(0.5f, 1f, 0.5f)); // 꼭대기 
        Debug.Log($"v = {h}");
        h = MultiplyMatrixVectorRaw(Sh, h);
        Debug.Log($"Shear(k={k}) 적용 → {FromHomogeneous(h)}");
    }

    // 1열: e₁ 그대로 / 2열: e₂ → (k, 1, 0) / 3열: e₃ 그대로 / 4열: 원점 그대로
    float[,] ShearMatrixRaw(float k)
    {
        return new float[,] {
            { 1f, k,  0f, 0f },
            { 0f, 1f, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    Vector4 ToHomogeneous(Vector3 v) => new Vector4(v.x, v.y, v.z, 1f);
    Vector3 FromHomogeneous(Vector4 h) => new Vector3(h.x, h.y, h.z);

    Vector4 MultiplyMatrixVectorRaw(float[,] M, Vector4 v)
    {
        float[] input = { v.x, v.y, v.z, v.w };
        float[] result = new float[4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                result[row] += M[row, col] * input[col];
        return new Vector4(result[0], result[1], result[2], result[3]);
    }
}