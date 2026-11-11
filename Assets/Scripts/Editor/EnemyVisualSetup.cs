using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 3DG > Setup Enemy Visuals: puts the RPG Monster models on the enemy prefabs. Safe to run again.
/// For each enemy it:
///  1. Switches the monster's Built-in "Standard" materials to URP Lit (else they render pink).
///  2. Builds Assets/Animations/Enemies/{Enemy}.controller: one state per clip, no transitions
///     (EnemyVisual picks the state from code; the packs' demo controllers just cycle every clip).
///  3. In the enemy prefab: hides the placeholder "Model", adds the monster as "Visual" (scaled to
///     the given height, feet at the pivot), assigns the controller, and fills in EnemyVisual.
///  4. ShieldEnemy only: puts the hexagon shield model (Assets/Meshs/HexShield.obj) on the "Shield" child,
///     with a transparent purple material (created once at ShieldMaterialPath, tweak it freely after)
///     and the BoxCollider sized to the model.
/// Edit the table below to change a monster, its size, or which clip plays for what.
/// </summary>
public static class EnemyVisualSetup
{
    private const string ControllerFolder = "Assets/Animations/Enemies";

    // Hexagon shield of the ShieldEnemy. The mesh is 1 unit tall, front = +Z.
    private const string ShieldMeshPath = "Assets/Meshs/HexShield.obj";
    private const string ShieldMaterialPath = "Assets/Materials/Enemies/HexShield.mat";
    private const float ShieldSize = 1.4f; // meters tall
    private static readonly Vector3 ShieldPosition = new(0f, 1.0f, 0.9f); // local to the enemy root
    private static readonly Color ShieldColor = new(0.6f, 0.25f, 1f, 0.45f); // alpha = see-through

    private class Entry
    {
        public string enemy; // enemy prefab
        public string monster; // monster prefab (Polyart version, closest to the game's low-poly look)
        public string animFolder; // folder of the monster's animation FBX files
        public float height; // model height in meters (enemy collider is 1.5m tall, contact sphere 0.6m)
        public float moveAnimSpeed; // speed the move clip looks right at, 0 = don't scale playback
        public float runAnimSpeed; // same for the run clip

        // Clip FBX file names in animFolder. Empty = not used.
        public string idle,
            move,
            attack,
            hit,
            windup,
            action,
            special,
            stun,
            run;
    }

    private const string Duo = "Assets/RPG Monster DUO PBR Polyart/";
    private const string Buddies = "Assets/RPGMonsterBuddiesPBRPA/";
    private const string Partners = "Assets/RPGMonsterPartnersPBRPolyart/";

    private static readonly Entry[] Entries =
    {
        new()
        {
            enemy = "Assets/Prefabs/Enemies/LightEnemy.prefab",
            monster = Duo + "Prefabs/PolyartDefault/SlimePolyart.prefab",
            animFolder = Duo + "Animations/Slime/",
            height = 1.3f,
            moveAnimSpeed = 3.5f,
            idle = "IdleBattle_Slime_Anim.fbx",
            move = "Run_Slime_Anim.fbx",
            attack = "Attack01_Slime_Anim.fbx",
            hit = "GetHit_Slime_Anim.fbx",
        },
        new()
        {
            enemy = "Assets/Prefabs/Enemies/HeavyEnemy.prefab",
            monster = Partners + "Prefabs/Character/ChestMonsterPolyartDefault.prefab",
            animFolder = Partners + "Animations/Chest Monster/",
            height = 2.0f,
            moveAnimSpeed = 1.5f, // speeds up with the enrage
            idle = "IdleBattle.fbx",
            move = "WalkFWD.fbx",
            attack = "Attack01.fbx", // contact hits
            hit = "GetHit.fbx",
            windup = "Taunting.fbx", // slam windup, stretched to slamWindupTime
            special = "Attack02.fbx", // the slam, plays once
        },
        new()
        {
            enemy = "Assets/Prefabs/Enemies/ChargeEnemy.prefab",
            monster = Duo + "Prefabs/PolyartDefault/TurtleShellPolyart.prefab",
            animFolder = Duo + "Animations/TurtleShell/",
            height = 1.6f,
            idle = "IdleBattle_TurtleShell_Anim.fbx",
            move = "Walk_TurtleShell_Anim.fbx",
            attack = "Attack01_TurtleShell_Anim.fbx",
            hit = "GetHit_TurtleShell_Anim.fbx",
            windup = "Taunt_TurtleShell_Anim.fbx",
            action = "Defend_TurtleShell_Anim.fbx", // charging: hides in its shell (plays once, holds the pose)
        },
        new()
        {
            enemy = "Assets/Prefabs/Enemies/JumpEnemy.prefab",
            monster = Buddies + "Prefab/MushroomAngryPA.prefab",
            animFolder = Buddies + "Animation/Mushroom/",
            height = 1.7f,
            idle = "Mushroom_IdleBattleAngry.fbx",
            move = "Mushroom_runFWDAngry.fbx",
            attack = "Mushroom_Attack01Angry.fbx",
            hit = "Mushroom_GetHitAngry.fbx",
            action = "Mushroom_runFWDAngry.fbx", // in the air, stretched to the jump time
        },
        new()
        {
            enemy = "Assets/Prefabs/Enemies/ShieldEnemy.prefab",
            monster = Partners + "Prefabs/Character/BeholderPolyartDefault.prefab",
            animFolder = Partners + "Animations/Beholder/",
            height = 2.0f,
            moveAnimSpeed = 2f,
            idle = "IdleBattle.fbx",
            move = "WalkFWD.fbx",
            attack = "Attack01.fbx",
            hit = "GetHit.fbx",
            stun = "Taunting.fbx", // after the shield breaks, stretched to the stun time
            run = "Run.fbx", // after the shield breaks
            runAnimSpeed = 4f,
        },
    };

    [MenuItem("3DG/Setup Enemy Visuals")]
    private static void Run()
    {
        EnsureFolder(ControllerFolder);
        int done = 0;
        foreach (Entry e in Entries)
        {
            try
            {
                if (Setup(e))
                    done++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Enemy visual setup failed for {e.enemy}: {ex}");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Enemy visual setup: {done}/{Entries.Length} enemies done.");
    }

    private static bool Setup(Entry e)
    {
        GameObject monster = AssetDatabase.LoadAssetAtPath<GameObject>(e.monster);
        if (monster == null)
        {
            Debug.LogError($"Monster prefab not found: {e.monster}");
            return false;
        }

        UpgradeMaterials(monster);
        AnimatorController controller = BuildController(e, out Dictionary<string, string> states);

        GameObject root = PrefabUtility.LoadPrefabContents(e.enemy);
        try
        {
            Transform placeholder = root.transform.Find("Model");
            if (placeholder != null)
                placeholder.gameObject.SetActive(false);

            Transform old = root.transform.Find("Visual");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(monster, root.transform);
            visual.name = "Visual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;
            FitToHeight(root.transform, visual.transform, e.height);
            SetLayer(visual, root.layer);

            Animator animator = visual.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (!root.TryGetComponent(out EnemyVisual enemyVisual))
                enemyVisual = root.AddComponent<EnemyVisual>();

            var so = new SerializedObject(enemyVisual);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("idleState").stringValue = states.GetValueOrDefault("idle", "");
            so.FindProperty("moveState").stringValue = states.GetValueOrDefault("move", "");
            so.FindProperty("attackState").stringValue = states.GetValueOrDefault("attack", "");
            so.FindProperty("hitState").stringValue = states.GetValueOrDefault("hit", "");
            so.FindProperty("windupState").stringValue = states.GetValueOrDefault("windup", "");
            so.FindProperty("actionState").stringValue = states.GetValueOrDefault("action", "");
            so.FindProperty("specialState").stringValue = states.GetValueOrDefault("special", "");
            so.FindProperty("stunState").stringValue = states.GetValueOrDefault("stun", "");
            so.FindProperty("runState").stringValue = states.GetValueOrDefault("run", "");
            so.FindProperty("runAnimSpeed").floatValue = e.runAnimSpeed;
            so.FindProperty("moveAnimSpeed").floatValue = e.moveAnimSpeed;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (root.GetComponent<ShieldEnemy>() != null)
                SetupShield(root.transform.Find("Shield"));

            PrefabUtility.SaveAsPrefabAsset(root, e.enemy);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------------- Shield ----------------

    private static void SetupShield(Transform shield)
    {
        if (shield == null)
        {
            Debug.LogWarning("ShieldEnemy has no \"Shield\" child; shield model skipped.");
            return;
        }

        Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(ShieldMeshPath).OfType<Mesh>().FirstOrDefault();
        if (mesh == null)
        {
            Debug.LogWarning($"Shield mesh not found: {ShieldMeshPath}");
            return;
        }

        shield.SetLocalPositionAndRotation(ShieldPosition, Quaternion.identity);
        shield.localScale = Vector3.one * ShieldSize;

        if (shield.TryGetComponent(out MeshFilter filter))
            filter.sharedMesh = mesh;

        if (shield.TryGetComponent(out MeshRenderer renderer))
        {
            renderer.sharedMaterial = GetOrCreateShieldMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        if (shield.TryGetComponent(out BoxCollider box))
        {
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;
        }
    }

    // Transparent purple URP Lit. Only created when missing, so Inspector tweaks are kept.
    private static Material GetOrCreateShieldMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ShieldMaterialPath);
        if (mat != null)
            return mat;

        EnsureFolder("Assets/Materials/Enemies");
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "HexShield" };

        // Surface Type = Transparent, Blending Mode = Alpha (what the URP Lit Inspector sets)
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;

        mat.SetColor("_BaseColor", ShieldColor);
        mat.SetFloat("_Smoothness", 0.85f); // glassy highlights on the facets
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b) * 0.35f); // soft glow

        AssetDatabase.CreateAsset(mat, ShieldMaterialPath);
        return mat;
    }

    // ---------------- Materials ----------------

    // Built-in "Standard" renders pink in URP. Switch to URP Lit and carry over texture/color/smoothness.
    private static void UpgradeMaterials(GameObject prefab)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogWarning("URP Lit shader not found; monster materials left as they are.");
            return;
        }

        foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || m.shader == null || m.shader.name != "Standard")
                    continue;

                Texture tex = m.GetTexture("_MainTex");
                Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                float smooth = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.2f;
                float metal = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;

                m.shader = lit;
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", smooth);
                m.SetFloat("_Metallic", metal);
                EditorUtility.SetDirty(m);
                Debug.Log($"Upgraded material to URP Lit: {AssetDatabase.GetAssetPath(m)}");
            }
        }
    }

    // ---------------- Animator ----------------

    private static AnimatorController BuildController(Entry e, out Dictionary<string, string> states)
    {
        string path = $"{ControllerFolder}/{Path.GetFileNameWithoutExtension(e.enemy)}.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path); // rebuilt from scratch every run

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        states = new Dictionary<string, string>();

        // idle first: it becomes the default state
        var slots = new (string slot, string file)[]
        {
            ("idle", e.idle),
            ("move", e.move),
            ("attack", e.attack),
            ("hit", e.hit),
            ("windup", e.windup),
            ("action", e.action),
            ("special", e.special),
            ("stun", e.stun),
            ("run", e.run),
        };

        foreach ((string slot, string file) in slots)
        {
            if (string.IsNullOrEmpty(file))
                continue;

            AnimationClip clip = LoadClip(e.animFolder + file);
            if (clip == null)
            {
                Debug.LogWarning($"Clip not found: {e.animFolder + file}");
                continue;
            }

            AnimatorState state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name);
            if (state == null)
            {
                state = sm.AddState(clip.name);
                state.motion = clip;
            }
            if (slot == "idle")
                sm.defaultState = state;
            states[slot] = clip.name;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip LoadClip(string fbxPath) =>
        AssetDatabase
            .LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

    // ---------------- Helpers ----------------

    // Scales the model so its renderers are `height` tall, then puts its lowest point at the root's pivot.
    private static void FitToHeight(Transform root, Transform visual, float height)
    {
        if (!TryGetBounds(visual, out Bounds b) || b.size.y < 0.0001f)
            return;

        visual.localScale = Vector3.one * (height / b.size.y);

        if (TryGetBounds(visual, out b))
        {
            float bottom = root.InverseTransformPoint(b.min).y;
            visual.localPosition += Vector3.up * -bottom;
        }
    }

    // World bounds of the actual vertices. Renderer.bounds is not used: a skinned mesh's bounds is a
    // loose box (covers all poses), which put some models above or below the ground.
    private static bool TryGetBounds(Transform t, out Bounds bounds)
    {
        Bounds result = default; // local copy: a local function can't write an out parameter
        bool any = false;

        void Add(Vector3 p)
        {
            if (!any)
                result = new Bounds(p, Vector3.zero);
            else
                result.Encapsulate(p);
            any = true;
        }

        foreach (SkinnedMeshRenderer smr in t.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var baked = new Mesh();
            smr.BakeMesh(baked, true); // current pose, with the renderer's scale
            Matrix4x4 m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            foreach (Vector3 v in baked.vertices)
                Add(m.MultiplyPoint3x4(v));
            Object.DestroyImmediate(baked);
        }

        foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null)
                continue;
            Matrix4x4 m = mf.transform.localToWorldMatrix;
            foreach (Vector3 v in mf.sharedMesh.vertices)
                Add(m.MultiplyPoint3x4(v));
        }

        bounds = result;
        return any;
    }

    private static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayer(child.gameObject, layer);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
