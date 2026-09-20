using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Pentagon : MonoBehaviour
{
    void Start()
    {
        
        // TODO 1: 정점 좌표 정의(5개 이상)
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-1f,  2f, 0f), // 0: 좌상단
            new Vector3(-2f,  0f, 0f), // 1: 좌측
            new Vector3(-1f, -2f, 0f), // 2: 좌하단
            new Vector3( 1f, -2f, 0f), // 3: 우하단
            new Vector3( 2f,  0f, 0f), // 4: 우측
            new Vector3( 1f,  2f, 0f), // 5: 우상단

        };

        // TODO 2: 삼각형 인덱스 정의
        int[] triangles = new int[]
        {
            0, 1, 2, // 첫 번째 삼각형
            0, 2, 3, // 두 번째 삼각형
            0, 3, 4, // 세 번째 삼각형
            0, 4, 5, // 네 번째 삼각형
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}