// S15Builder.cs — S15 과제 장면 자동 구성 (에디터 전용)
// 메뉴: S15 > 1. Build S15 Landscape
// S14_Landscape를 S15/S15_Landscape로 복사하고(실습 5 연기는 S14에서 완성),
// 실습 6: S15_SG_Flow(Shader Graph)로 만든 Mat_S15_SG_Flow를 테이블 상판(Cube_UpperPlate)에 입힘.
// 과제 3: 바닥 물웅덩이 Quad(WaterPuddle)에 Mat_S15_SG_Flow_Puddle을 입힘.
// 스크린샷은 S14 > 2. Save ShotCamera Screenshot (Play 중) 를 그대로 사용.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S15Builder
{
    const string S15 = "Assets/S15";
    const string TEX = "Assets/ThirdParty/Textures";
    const string SRC_SCENE = "Assets/S14/S14_Landscape.unity";
    const string DST_SCENE = "Assets/S15/S15_Landscape.unity";
    const string GRAPH = "Assets/S15/S15_SG_Flow.shadergraph";

    [MenuItem("S15/1. Build S15 Landscape")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("[S15] Play 모드를 끈 뒤 실행하세요."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.IsValidFolder(S15)) AssetDatabase.CreateFolder("Assets", "S15");
        string[] texFiles = { "S15_Water1_Caustic_Seamless.png", "S15_Water2_Ripple_Seamless.png", "S15_Puddle_Rain_Seamless.png" };
        foreach (string f in texFiles)
        {
            var imp = AssetImporter.GetAtPath(TEX + "/" + f) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[S15] 텍스처 없음: " + f); continue; }
            imp.wrapMode = TextureWrapMode.Repeat;   // 이어지는 텍스처: Repeat
            imp.maxTextureSize = 1024;               // Max Size 1024
            imp.SaveAndReimport();
        }
        Texture2D water1 = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX + "/" + texFiles[0]);
        Texture2D water2 = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX + "/" + texFiles[1]);
        Texture2D puddle = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX + "/" + texFiles[2]);

        // S14_Landscape → S15/S15_Landscape (Save As)
        if (!File.Exists(DST_SCENE) && !AssetDatabase.CopyAsset(SRC_SCENE, DST_SCENE))
        {
            Debug.LogError("[S15] S14_Landscape를 복사하지 못했습니다.");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);

        Shader flow = AssetDatabase.LoadAssetAtPath<Shader>(GRAPH);
        if (flow == null) { Debug.LogError("[S15] S15_SG_Flow 셰이더 그래프를 불러오지 못했습니다: " + GRAPH); return; }

        // ── 실습 6: 테이블 상판에 흐르는 물 (두 층) ──
        Material top = MakeFlowMat("Mat_S15_SG_Flow", flow, water1, water2,
            new Vector2(0f, 0.05f), new Vector2(0.02f, 0.1f), new Vector2(2f, 1f), 0.5f);   // 상판 Scale (1.8, 0.03, 0.9) → Tiling (2, 1)

        GameObject plate = FindAll(scene, "Cube_UpperPlate").FirstOrDefault();
        if (plate != null)
        {
            var mr = plate.GetComponent<MeshRenderer>();
            mr.sharedMaterial = top;
            EditorUtility.SetDirty(mr);
            PrefabUtility.RecordPrefabInstancePropertyModifications(mr);
        }
        else Debug.LogWarning("[S15] Cube_UpperPlate(테이블 상판)를 찾지 못함");

        // ── 과제 3: 바닥 물웅덩이 ──
        Material pud = MakeFlowMat("Mat_S15_SG_Flow_Puddle", flow, puddle, water2,
            new Vector2(0.03f, -0.02f), new Vector2(-0.04f, 0.03f), new Vector2(1.5f, 1f), 0.35f);

        foreach (GameObject g in FindAll(scene, "WaterPuddle")) Object.DestroyImmediate(g);
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "WaterPuddle";
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        SceneManager.MoveGameObjectToScene(quad, scene);
        Vector3 spot = new Vector3(-0.55f, 0f, 1.05f);   // 테이블 앞쪽, 걸어 들어오는 길 옆
        spot.y = FloorY(spot) + 0.004f;
        quad.transform.position = spot;
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // 위를 향하게 눕힘
        quad.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
        quad.GetComponent<MeshRenderer>().sharedMaterial = pud;
        quad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // ShotCamera: 앞쪽 왼편에서 물웅덩이(앞)·흐르는 상판·연기(뒤)가 함께 보이는 구도
        GameObject shot = FindAll(scene, "ShotCamera").FirstOrDefault();
        if (shot != null)
        {
            shot.transform.position = new Vector3(-1.25f, 2.05f, -0.35f);
            shot.transform.LookAt(new Vector3(0.0f, 1.3f, 1.8f));
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Lightmapping.bakeCompleted -= OnBakeDone;
        Lightmapping.bakeCompleted += OnBakeDone;
        Lightmapping.BakeAsync();   // Generate Lighting

        Selection.activeGameObject = quad;
        Debug.Log("[S15] 장면 구성 완료: " + DST_SCENE + " (puddle y=" + spot.y.ToString("F3") + ")");
    }

    static void OnBakeDone()
    {
        Lightmapping.bakeCompleted -= OnBakeDone;
        Scene s = SceneManager.GetActiveScene();
        if (s.path == DST_SCENE) { EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s); }
        Debug.Log("[S15] Generate Lighting 완료, 장면 저장함");
    }

    static Material MakeFlowMat(string name, Shader sh, Texture t1, Texture t2, Vector2 f1, Vector2 f2, Vector2 tiling, float blend)
    {
        string path = S15 + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
        else m.shader = sh;
        m.SetTexture("_MainTex1", t1);
        m.SetTexture("_MainTex2", t2);
        m.SetVector("_Flow1", f1);
        m.SetVector("_Flow2", f2);
        m.SetVector("_FlowTiling", tiling);
        m.SetFloat("_FlowBlend", blend);
        EditorUtility.SetDirty(m);
        return m;
    }

    // 위에서 아래로 광선을 쏴 정자 바닥 높이 찾기
    static float FloorY(Vector3 p)
    {
        Physics.SyncTransforms();
        var hits = Physics.RaycastAll(new Vector3(p.x, 2.5f, p.z), Vector3.down, 5f)
                          .Where(h => h.point.y < 1.5f)
                          .OrderByDescending(h => h.point.y)
                          .ToArray();
        if (hits.Length > 0) return hits[0].point.y;
        Debug.LogWarning("[S15] 바닥을 찾지 못해 기본 높이(0.86)를 씀");
        return 0.86f;
    }

    static GameObject[] FindAll(Scene scene, string name) =>
        scene.GetRootGameObjects()
             .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
             .Where(t => t.name == name)
             .Select(t => t.gameObject)
             .ToArray();
}
