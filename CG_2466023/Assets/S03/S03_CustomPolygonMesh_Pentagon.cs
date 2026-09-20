  using UnityEngine;

  [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
  public class S03_CustomPolygonMesh_Pentagon : MonoBehaviour
  {
      void Start()
      {
          // TODO 1: 정점 좌표 정의(5개 이상)
          Vector3[] vertices = new Vector3[]
          {
              // 예: new Vector3(0f, 1f, 0f),
              
          };

          // TODO 2: 삼각형 인덱스 정의
          int[] triangles = new int[]
          {
              
          };

          Mesh mesh = new Mesh();
          mesh.vertices = vertices;
          mesh.triangles = triangles;
          mesh.RecalculateNormals();

          GetComponent<MeshFilter>().mesh = mesh;
          GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
      }
  }