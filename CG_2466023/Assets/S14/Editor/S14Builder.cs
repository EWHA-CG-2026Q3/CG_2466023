// S14Builder.cs — S14 과제 장면 자동 구성 (에디터 전용)
// 메뉴: S14 > 1. Build S14 Landscape  /  S14 > 2. Save ShotCamera Screenshot
// 실습 3(화로·숯·불빛), 실습 4(Global Volume + Bloom, ShotCamera), 실습 5(연기 파티클)과
// S13 오브젝트 재질(프리미티브 3개: 텍스처 재질, 내 메시 3개: 텍스처 없는 재질 + Emission)을 구성함.
// 여러 번 실행해도 같은 결과가 되도록, 이 스크립트가 만든 오브젝트는 먼저 지우고 다시 만듦.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class S14Builder
{
    const string S14 = "Assets/S14";
    const string TEX = "Assets/ThirdParty/Textures";
    const string SRC_SCENE = "Assets/S13/S13_Landscape.unity";
    const string DST_SCENE = "Assets/S14/S14_Landscape.unity";

    // ───────────────────────────── 메뉴 1: 장면 구성 ─────────────────────────────
    [MenuItem("S14/1. Build S14 Landscape")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[S14] Play 모드를 끈 뒤 실행하세요.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets", "S14");
        EnsureFolder("Assets", "ThirdParty");
        EnsureFolder("Assets/ThirdParty", "Textures");
        ConfigureTextures();

        // S13_Landscape → S14/S14_Landscape (Save As와 같은 효과)
        if (!File.Exists(DST_SCENE))
        {
            if (!AssetDatabase.CopyAsset(SRC_SCENE, DST_SCENE))
            {
                Debug.LogError("[S14] S13_Landscape를 복사하지 못했습니다: " + SRC_SCENE);
                return;
            }
        }
        Scene scene = EditorSceneManager.OpenScene(DST_SCENE, OpenSceneMode.Single);

        // 텍스처
        Texture2D texMetal = LoadTex("S14_Metal_Corroded.png");
        Texture2D texLava = LoadTex("S14_Lava_Cracked.png");
        Texture2D texWood = LoadTex("S14_Wood_Planks.png");
        Texture2D texStone = LoadTex("S14_Stone_Cobble.png");

        // 이전에 만든 것 정리 (재실행 대비)
        foreach (string n in new[] { "FireBowl", "Coals", "FireLight", "Global Volume", "ShotCamera" })
            foreach (GameObject g in FindAll(scene, n)) UnityEngine.Object.DestroyImmediate(g);

        // ── 실습 3: 화로 ──
        Bounds table = TableBounds(scene);
        float topY = table.max.y;
        Vector3 spot = PickFreeSpotOnTable(scene, table);

        GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bowl.name = "FireBowl";
        bowl.transform.position = new Vector3(spot.x, topY + 0.05f, spot.z);
        bowl.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
        SceneManager.MoveGameObjectToScene(bowl, scene);

        Material matBowl = MakeLit("Mat_S14_FireBowl");
        matBowl.SetTexture("_BaseMap", texMetal);
        matBowl.SetColor("_BaseColor", Color.white);
        matBowl.SetFloat("_Metallic", 0.5f);
        matBowl.SetFloat("_Smoothness", 0.25f);
        Save(matBowl);
        bowl.GetComponent<MeshRenderer>().sharedMaterial = matBowl;

        Material matCoal = MakeLit("Mat_S14_Coal");
        matCoal.SetTexture("_BaseMap", null);
        matCoal.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.03f));
        matCoal.SetFloat("_Metallic", 0f);
        matCoal.SetFloat("_Smoothness", 0.1f);
        SetEmission(matCoal, new Color(191 / 255f, 117 / 255f, 3 / 255f), 5f, texLava);
        Save(matCoal);

        GameObject coals = new GameObject("Coals");
        SceneManager.MoveGameObjectToScene(coals, scene);
        coals.transform.position = bowl.transform.position + Vector3.up * 0.05f;
        Vector3[] offs =
        {
            new Vector3( 0.00f, 0.020f,  0.00f), new Vector3( 0.065f, 0.015f,  0.035f),
            new Vector3(-0.065f, 0.015f,  0.030f), new Vector3( 0.020f, 0.015f, -0.070f),
            new Vector3(-0.040f, 0.050f, -0.020f), new Vector3( 0.040f, 0.055f,  0.010f),
            new Vector3(-0.030f, 0.015f, -0.060f),
        };
        Vector3[] scales =
        {
            new Vector3(0.15f, 0.10f, 0.12f), new Vector3(0.11f, 0.08f, 0.10f),
            new Vector3(0.12f, 0.07f, 0.09f), new Vector3(0.10f, 0.08f, 0.12f),
            new Vector3(0.09f, 0.07f, 0.10f), new Vector3(0.10f, 0.06f, 0.08f),
            new Vector3(0.08f, 0.06f, 0.09f),
        };
        var rnd = new System.Random(14);
        for (int i = 0; i < offs.Length; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.name = "Coal_" + (i + 1);
            UnityEngine.Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(coals.transform, false);
            c.transform.localPosition = offs[i];
            c.transform.localScale = scales[i];
            c.transform.localRotation = Quaternion.Euler(rnd.Next(0, 360), rnd.Next(0, 360), rnd.Next(0, 360));
            c.GetComponent<MeshRenderer>().sharedMaterial = matCoal;
        }

        GameObject fireLightGO = new GameObject("FireLight");
        SceneManager.MoveGameObjectToScene(fireLightGO, scene);
        fireLightGO.transform.position = bowl.transform.position + Vector3.up * 0.3f;
        Light fl = fireLightGO.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = Color.white;
        fl.useColorTemperature = true;
        fl.colorTemperature = 1800f;
        fl.range = 2f;
        fl.intensity = 3f;
        fl.shadows = LightShadows.Soft;
        GraphicsSettings.lightsUseColorTemperature = true;

        // ── 실습 5: 연기 파티클 (FireBowl의 자식) ──
        BuildSmoke(bowl);

        // ── 실습 4: Global Volume + Bloom ──
        GameObject volGO = new GameObject("Global Volume");
        SceneManager.MoveGameObjectToScene(volGO, scene);
        Volume vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = MakeBloomProfile();

        // 카메라 Post Processing 켜기 + ShotCamera
        Camera mainCam = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(c => c.CompareTag("MainCamera") || c.name == "Main Camera");

        foreach (GameObject g in FindAll(scene, "CompositionCamera").Concat(FindAll(scene, "CompositionCamera ")))
            g.SetActive(false);   // S13 구도 카메라는 끄고 ShotCamera를 새로 씀

        GameObject shotGO = new GameObject("ShotCamera");
        SceneManager.MoveGameObjectToScene(shotGO, scene);
        Camera shot = shotGO.AddComponent<Camera>();
        if (mainCam != null)
        {
            shot.clearFlags = mainCam.clearFlags;
            shot.backgroundColor = mainCam.backgroundColor;
            shot.fieldOfView = mainCam.fieldOfView;
            shot.nearClipPlane = mainCam.nearClipPlane;
            shot.farClipPlane = mainCam.farClipPlane;
        }
        Vector3 focus = bowl.transform.position + Vector3.up * 0.25f;
        shotGO.transform.position = focus + new Vector3(0.75f, 0.65f, 1.0f);
        shotGO.transform.LookAt(focus);
        shot.depth = (mainCam != null ? mainCam.depth : 0) + 1;
        shot.enabled = false;   // 걸을 때는 끔, 스크린샷 찍을 때만 켬

        foreach (Camera cam in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);
        }

        // ── S13 오브젝트 재질 ──
        // 프리미티브: 텍스처가 있는 서로 다른 재질
        Material matWood = MakeLit("Mat_S14_Wood");
        matWood.SetTexture("_BaseMap", texWood); matWood.SetColor("_BaseColor", Color.white);
        matWood.SetFloat("_Metallic", 0f); matWood.SetFloat("_Smoothness", 0.3f); Save(matWood);

        Material matStone = MakeLit("Mat_S14_Stone");
        matStone.SetTexture("_BaseMap", texStone); matStone.SetColor("_BaseColor", Color.white);
        matStone.SetFloat("_Metallic", 0f); matStone.SetFloat("_Smoothness", 0.15f); Save(matStone);

        Material matMetal = MakeLit("Mat_S14_Metal");
        matMetal.SetTexture("_BaseMap", texMetal); matMetal.SetColor("_BaseColor", new Color(0.85f, 0.9f, 0.95f));
        matMetal.SetFloat("_Metallic", 0.85f); matMetal.SetFloat("_Smoothness", 0.6f); Save(matMetal);

        Assign(scene, "Cylinder", matWood);
        Assign(scene, "Sphere", matStone);
        Assign(scene, "Capsule", matMetal);

        // 내 메시: Base Color, Metallic, Smoothness, Emission만 (텍스처 없음)
        Material matGlow = MakeLit("Mat_S14_Diamond_Glow");
        matGlow.SetTexture("_BaseMap", null);
        matGlow.SetColor("_BaseColor", new Color(0.15f, 0.55f, 1f));
        matGlow.SetFloat("_Metallic", 0f); matGlow.SetFloat("_Smoothness", 0.7f);
        SetEmission(matGlow, new Color(0.2f, 0.65f, 1f), 2f, null); Save(matGlow);

        Material matGold = MakeLit("Mat_S14_Diamond_Gold");
        matGold.SetTexture("_BaseMap", null);
        matGold.SetColor("_BaseColor", new Color(1f, 0.77f, 0.34f));
        matGold.SetFloat("_Metallic", 1f); matGold.SetFloat("_Smoothness", 0.8f);
        SetEmission(matGold, Color.black, 0f, null); Save(matGold);

        Material matRuby = MakeLit("Mat_S14_Pyramid_Ruby");
        matRuby.SetTexture("_BaseMap", null);
        matRuby.SetColor("_BaseColor", new Color(0.65f, 0.05f, 0.12f));
        matRuby.SetFloat("_Metallic", 0.1f); matRuby.SetFloat("_Smoothness", 0.9f);
        SetEmission(matRuby, Color.black, 0f, null); Save(matRuby);

        Assign(scene, "Diamond", matGlow);
        Assign(scene, "Diamond_Sheared", matGold);
        Assign(scene, "Pyramid", matRuby);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Generate Lighting (주변광/반사 다시 계산 → S14/S14_Landscape/LightingData)
        Lightmapping.bakeCompleted -= OnBakeDone;
        Lightmapping.bakeCompleted += OnBakeDone;
        Lightmapping.BakeAsync();

        Selection.activeGameObject = bowl;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("[S14] 장면 구성 완료: " + DST_SCENE + " (Generate Lighting 진행 중)");
    }

    static void OnBakeDone()
    {
        Lightmapping.bakeCompleted -= OnBakeDone;
        Scene s = SceneManager.GetActiveScene();
        if (s.path == DST_SCENE)
        {
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
        }
        Debug.Log("[S14] Generate Lighting 완료, 장면 저장함");
    }

    // ───────────────────────────── 메뉴 2: ShotCamera 스크린샷 ─────────────────────────────
    [MenuItem("S14/2. Save ShotCamera Screenshot")]
    public static void SaveShot()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject go = FindAll(scene, "ShotCamera").FirstOrDefault();
        if (go == null) { Debug.LogError("[S14] ShotCamera가 없습니다. 먼저 1번 메뉴를 실행하세요."); return; }
        Camera cam = go.GetComponent<Camera>();

        // Play 모드: ShotCamera를 잠시 켜고 Game view를 그대로 캡처 (Bloom·연기 포함)
        if (EditorApplication.isPlaying)
        {
            string pdir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));
            Directory.CreateDirectory(pdir);
            string pfile = Path.Combine(pdir, SceneManager.GetActiveScene().name + "_ShotCamera_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            bool wasEnabled = cam.enabled;
            cam.enabled = true;
            int frames = 0;
            EditorApplication.CallbackFunction cb = null;
            cb = () =>
            {
                frames++;
                if (frames == 15) ScreenCapture.CaptureScreenshot(pfile);
                if (frames == 60)
                {
                    cam.enabled = wasEnabled;
                    EditorApplication.update -= cb;
                    Debug.Log("[S14] 스크린샷 저장: " + pfile);
                }
            };
            EditorApplication.update += cb;
            return;
        }

        const int W = 1920, H = 1080;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.antiAliasing = 4;
        RenderTexture prevTarget = cam.targetTexture;
        bool prevEnabled = cam.enabled;
        cam.targetTexture = rt;
        cam.enabled = true;
        cam.Render();
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;
        cam.enabled = prevEnabled;

        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, SceneManager.GetActiveScene().name + "_ShotCamera_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
        File.WriteAllBytes(file, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        Debug.Log("[S14] 스크린샷 저장: " + file);
    }

    // ───────────────────────────── 연기 ─────────────────────────────
    static void BuildSmoke(GameObject bowl)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(S14 + "/Mat_S14_Smoke.mat");
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, S14 + "/Mat_S14_Smoke.mat"); }
        else m.shader = sh;

        Texture2D particleTex = null;
        try { particleTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"); } catch { }
        if (particleTex == null) particleTex = AssetDatabase.LoadAssetAtPath<Texture2D>(S14 + "/S14_SmokeSoft.png");
        m.SetTexture("_BaseMap", particleTex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0f);
        m.SetFloat("_Metallic", 0f);
        // Surface Type: Transparent, Blending Mode: Alpha, Preserve Specular Lighting: 끔
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_BlendModePreserveSpecular", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.DisableKeyword("_ALPHAMODULATE_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        TryUrpValidate(m);
        Save(m);

        GameObject go = new GameObject("PS_Smoke");
        go.transform.SetParent(bowl.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        go.transform.localScale = Vector3.one;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.6f, 0.6f, 0.6f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 200;

        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 10f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;   // Emit from: Base
        shape.angle = 10f;
        shape.radius = 0.01f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve(0f);
        vel.z = new ParticleSystem.MinMaxCurve(0.3f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.75f, 0.45f, 0.25f), 0f),
                new GradientColorKey(new Color(0.60f, 0.40f, 0.28f), 0.2f),
                new GradientColorKey(new Color(0.50f, 0.50f, 0.50f), 0.6f),
                new GradientColorKey(new Color(0.40f, 0.40f, 0.40f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.55f, 0.15f),
                new GradientAlphaKey(0.35f, 0.6f),
                new GradientAlphaKey(0f, 1f),
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(1f, 1f)));

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = m;
        r.maxParticleSize = 1f;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;

        ps.Play();
    }

    // URP 에디터의 재질 검증 함수가 있으면 호출 (키워드/블렌드 상태 정리)
    static void TryUrpValidate(Material m)
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("UnityEditor.BaseShaderGUI");
                if (t == null) continue;
                var mi = t.GetMethod("SetupMaterialBlendMode",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(Material) }, null);
                mi?.Invoke(null, new object[] { m });
                return;
            }
        }
        catch (Exception e) { Debug.LogWarning("[S14] URP 재질 검증 생략: " + e.Message); }
    }

    // ───────────────────────────── Bloom 프로필 ─────────────────────────────
    static VolumeProfile MakeBloomProfile()
    {
        string path = S14 + "/S14_Landscape_VolumeProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(false);
            bloom.name = "Bloom";
            bloom.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.active = true;
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.5f);
        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // ───────────────────────────── 도우미 ─────────────────────────────
    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    static void ConfigureTextures()
    {
        foreach (string f in new[] { "S14_Metal_Corroded.png", "S14_Lava_Cracked.png", "S14_Wood_Planks.png", "S14_Stone_Cobble.png" })
        {
            var imp = AssetImporter.GetAtPath(TEX + "/" + f) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[S14] 텍스처 없음: " + TEX + "/" + f); continue; }
            imp.textureType = TextureImporterType.Default;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.maxTextureSize = 1024;
            imp.SaveAndReimport();
        }
        var soft = AssetImporter.GetAtPath(S14 + "/S14_SmokeSoft.png") as TextureImporter;
        if (soft != null) { soft.alphaIsTransparency = true; soft.wrapMode = TextureWrapMode.Clamp; soft.SaveAndReimport(); }
    }

    static Texture2D LoadTex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(TEX + "/" + file);

    static Material MakeLit(string name)
    {
        string path = S14 + "/" + name + ".mat";
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(lit) { name = name }; AssetDatabase.CreateAsset(m, path); }
        else if (m.shader != lit) m.shader = lit;
        return m;
    }

    // HDR 색 피커의 Intensity와 같음: 색 × 2^intensity
    static void SetEmission(Material m, Color c, float intensity, Texture map)
    {
        bool on = c.maxColorComponent > 0f;
        m.SetTexture("_EmissionMap", map);
        m.SetColor("_EmissionColor", on ? c * Mathf.Pow(2f, intensity) : Color.black);
        if (on)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
    }

    static void Save(UnityEngine.Object o) => EditorUtility.SetDirty(o);

    static GameObject[] FindAll(Scene scene, string name) =>
        scene.GetRootGameObjects()
             .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
             .Where(t => t.name == name)
             .Select(t => t.gameObject)
             .ToArray();

    static void Assign(Scene scene, string name, Material m)
    {
        var gos = FindAll(scene, name).Where(g => g.GetComponent<MeshRenderer>() != null).ToArray();
        if (gos.Length == 0) { Debug.LogWarning("[S14] 오브젝트 없음: " + name); return; }
        foreach (var g in gos)
        {
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            EditorUtility.SetDirty(mr);
        }
    }

    static Bounds TableBounds(Scene scene)
    {
        GameObject t = FindAll(scene, "Table").FirstOrDefault()
                    ?? scene.GetRootGameObjects().FirstOrDefault(g => g.name.Contains("Table"));
        if (t != null)
        {
            var rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                return b;
            }
        }
        Debug.LogWarning("[S14] Table을 찾지 못해 기본 위치를 씀");
        return new Bounds(new Vector3(0f, 1.2f, 2.1f), new Vector3(2f, 0.78f, 1f));
    }

    // 테이블 위에서 S13 오브젝트들과 가장 멀리 떨어진 자리 고르기
    static Vector3 PickFreeSpotOnTable(Scene scene, Bounds table)
    {
        string[] names = { "Cylinder", "Sphere", "Capsule", "Diamond", "Diamond_Sheared", "Pyramid" };
        var obstacles = names.SelectMany(n => FindAll(scene, n))
                             .Select(g => g.GetComponent<Renderer>())
                             .Where(r => r != null)
                             .Select(r => r.bounds)
                             .ToList();
        Debug.Log("[S14] Table bounds: " + table);
        float mx = Mathf.Max(0.05f, table.extents.x - 0.17f);
        float mz = Mathf.Max(0.05f, table.extents.z - 0.17f);
        Vector3 best = table.center;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i <= 12; i++)
            for (int j = 0; j <= 12; j++)
            {
                var p = new Vector3(table.center.x - mx + 2 * mx * i / 12f, table.max.y, table.center.z - mz + 2 * mz * j / 12f);
                float d = float.PositiveInfinity;
                foreach (var b in obstacles)
                {
                    Vector3 q = b.ClosestPoint(new Vector3(p.x, b.center.y, p.z));
                    d = Mathf.Min(d, new Vector2(p.x - q.x, p.z - q.z).magnitude);
                }
                // 너무 가장자리로 밀리지 않도록 중앙 가중치
                float score = Mathf.Min(d, 0.35f) - 0.02f * new Vector2(p.x - table.center.x, p.z - table.center.z).magnitude;
                if (score > bestScore) { bestScore = score; best = p; }
            }
        return best;
    }
}
