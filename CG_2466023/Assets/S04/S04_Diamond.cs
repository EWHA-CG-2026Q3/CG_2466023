using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_Diamond : MonoBehaviour
{
    void Start()
    {
        // TODO 1: 정점 6개 정의 (위/아래 꼭짓점 + 가운데 고리 4개)
        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 0f,  1f,  0f), // 0
            new Vector3( 0f, -1f,  0f), // 1
            new Vector3( 0f,  0f,  1f), // 2
            new Vector3( 1f,  0f,  0f), // 3
            new Vector3( 0f,  0f, -1f), // 4
            new Vector3(-1f,  0f,  0f), // 5
        };

        // TODO 2: 삼각형 8개(위 4개 + 아래 4개) winding order 정의
        int[] triangles = new int[]
        {
            // 위쪽 피라미드 (꼭짓점 0 기준)
            0, 2, 3, // 앞-오른쪽 면
            0, 3, 4, // 오른쪽-뒤 면
            0, 4, 5, // 뒤-왼쪽 면
            0, 5, 2, // 왼쪽-앞 면

            // 아래쪽 피라미드 (꼭짓점 1 기준)
            1, 3, 2, // 앞-오른쪽 면
            1, 4, 3, // 오른쪽-뒤 면
            1, 5, 4, // 뒤-왼쪽 면
            1, 2, 5, // 왼쪽-앞 면
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}