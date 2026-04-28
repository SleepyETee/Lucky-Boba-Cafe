// ============================================================
// FILE: AnimationSetupTool.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Editor utility that auto-generates Animation
//              Controllers and Clips for Player and Customer.
//              Run via menu: Tools > Lucky Boba > Generate Animations
// ============================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AnimationSetupTool
{
    private static readonly string AnimFolder = "Assets/Animations";

    [MenuItem("Tools/Lucky Boba/1. Generate Animations", false, 11)]
    public static void GenerateAllAnimations()
    {
        EnsureFolder(AnimFolder);
        CreatePlayerAnimator();
        CreateCustomerAnimator();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnimSetup] All animation assets generated in Assets/Animations/");
    }

    // ==================== PLAYER ====================

    static void CreatePlayerAnimator()
    {
        string controllerPath = $"{AnimFolder}/PlayerAnimator.controller";

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters (match PlayerController.cs hashes)
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);

        var rootLayer = controller.layers[0];
        var stateMachine = rootLayer.stateMachine;

        // Clips
        AnimationClip idleClip = CreateBobClip("PlayerIdle", 0.02f, 1.0f);
        AnimationClip walkClip = CreateBobClip("PlayerWalk", 0.04f, 2.0f);
        AnimationClip interactClip = CreateSquashClip("PlayerInteract", 0.15f);

        // States
        var idleState = stateMachine.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idleClip;

        var walkState = stateMachine.AddState("Walk", new Vector3(300, 80, 0));
        walkState.motion = walkClip;

        var interactState = stateMachine.AddState("Interact", new Vector3(550, 0, 0));
        interactState.motion = interactClip;

        stateMachine.defaultState = idleState;

        // Transitions: Idle <-> Walk
        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        toWalk.duration = 0.1f;
        toWalk.hasExitTime = false;

        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        toIdle.duration = 0.1f;
        toIdle.hasExitTime = false;

        // Transition: Any -> Interact -> Idle
        var toInteract = stateMachine.AddAnyStateTransition(interactState);
        toInteract.AddCondition(AnimatorConditionMode.If, 0, "Interact");
        toInteract.duration = 0.05f;
        toInteract.hasExitTime = false;

        var interactToIdle = interactState.AddTransition(idleState);
        interactToIdle.hasExitTime = true;
        interactToIdle.exitTime = 1f;
        interactToIdle.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        Debug.Log("[AnimSetup] Created PlayerAnimator controller");
    }

    // ==================== CUSTOMER ====================

    static void CreateCustomerAnimator()
    {
        string controllerPath = $"{AnimFolder}/CustomerAnimator.controller";

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters (match Customer.cs hashes)
        controller.AddParameter("Walk", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Idle", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Happy", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Angry", AnimatorControllerParameterType.Trigger);

        var rootLayer = controller.layers[0];
        var stateMachine = rootLayer.stateMachine;

        // Clips
        AnimationClip idleClip = CreateBobClip("CustomerIdle", 0.015f, 0.8f);
        AnimationClip walkClip = CreateBobClip("CustomerWalk", 0.035f, 2.5f);
        AnimationClip happyClip = CreateBounceClip("CustomerHappy");
        AnimationClip angryClip = CreateShakeClip("CustomerAngry");

        // States
        var idleState = stateMachine.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idleClip;

        var walkState = stateMachine.AddState("Walk", new Vector3(300, 80, 0));
        walkState.motion = walkClip;

        var happyState = stateMachine.AddState("Happy", new Vector3(550, 0, 0));
        happyState.motion = happyClip;

        var angryState = stateMachine.AddState("Angry", new Vector3(550, 80, 0));
        angryState.motion = angryClip;

        stateMachine.defaultState = idleState;

        // Transitions
        var anyToWalk = stateMachine.AddAnyStateTransition(walkState);
        anyToWalk.AddCondition(AnimatorConditionMode.If, 0, "Walk");
        anyToWalk.duration = 0.1f;
        anyToWalk.hasExitTime = false;

        var anyToIdle = stateMachine.AddAnyStateTransition(idleState);
        anyToIdle.AddCondition(AnimatorConditionMode.If, 0, "Idle");
        anyToIdle.duration = 0.1f;
        anyToIdle.hasExitTime = false;

        var anyToHappy = stateMachine.AddAnyStateTransition(happyState);
        anyToHappy.AddCondition(AnimatorConditionMode.If, 0, "Happy");
        anyToHappy.duration = 0.05f;
        anyToHappy.hasExitTime = false;

        var anyToAngry = stateMachine.AddAnyStateTransition(angryState);
        anyToAngry.AddCondition(AnimatorConditionMode.If, 0, "Angry");
        anyToAngry.duration = 0.05f;
        anyToAngry.hasExitTime = false;

        // Happy/Angry return to Idle after playing
        var happyToIdle = happyState.AddTransition(idleState);
        happyToIdle.hasExitTime = true;
        happyToIdle.exitTime = 1f;
        happyToIdle.duration = 0.15f;

        var angryToIdle = angryState.AddTransition(idleState);
        angryToIdle.hasExitTime = true;
        angryToIdle.exitTime = 1f;
        angryToIdle.duration = 0.15f;

        EditorUtility.SetDirty(controller);
        Debug.Log("[AnimSetup] Created CustomerAnimator controller");
    }

    // ==================== CLIP FACTORIES ====================

    /// <summary>Gentle Y-scale bobbing (idle/walk).</summary>
    static AnimationClip CreateBobClip(string name, float amount, float speed)
    {
        var clip = new AnimationClip();
        clip.name = name;

        float duration = 1f / Mathf.Max(0.1f, speed);
        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(duration * 0.5f, 1f + amount);
        curve.AddKey(duration, 1f);

        clip.SetCurve("", typeof(Transform), "localScale.y", curve);

        // Slight X scale for walk
        if (amount > 0.03f)
        {
            var xCurve = new AnimationCurve();
            xCurve.AddKey(0f, 1f);
            xCurve.AddKey(duration * 0.25f, 1f + amount * 0.3f);
            xCurve.AddKey(duration * 0.5f, 1f);
            xCurve.AddKey(duration * 0.75f, 1f + amount * 0.3f);
            xCurve.AddKey(duration, 1f);
            clip.SetCurve("", typeof(Transform), "localScale.x", xCurve);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, $"{AnimFolder}/{name}.anim");
        return clip;
    }

    /// <summary>Quick squash-stretch for interact.</summary>
    static AnimationClip CreateSquashClip(string name, float amount)
    {
        var clip = new AnimationClip();
        clip.name = name;

        float d = 0.3f;
        var yc = new AnimationCurve();
        yc.AddKey(0f, 1f);
        yc.AddKey(d * 0.3f, 1f - amount);
        yc.AddKey(d * 0.6f, 1f + amount * 0.5f);
        yc.AddKey(d, 1f);
        clip.SetCurve("", typeof(Transform), "localScale.y", yc);

        var xc = new AnimationCurve();
        xc.AddKey(0f, 1f);
        xc.AddKey(d * 0.3f, 1f + amount * 0.5f);
        xc.AddKey(d * 0.6f, 1f - amount * 0.3f);
        xc.AddKey(d, 1f);
        clip.SetCurve("", typeof(Transform), "localScale.x", xc);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, $"{AnimFolder}/{name}.anim");
        return clip;
    }

    /// <summary>Happy bounce (scale up and down).</summary>
    static AnimationClip CreateBounceClip(string name)
    {
        var clip = new AnimationClip();
        clip.name = name;

        float d = 0.5f;
        var yc = new AnimationCurve();
        yc.AddKey(0f, 1f);
        yc.AddKey(d * 0.2f, 1.15f);
        yc.AddKey(d * 0.4f, 0.92f);
        yc.AddKey(d * 0.6f, 1.1f);
        yc.AddKey(d * 0.8f, 0.95f);
        yc.AddKey(d, 1f);
        clip.SetCurve("", typeof(Transform), "localScale.y", yc);

        var xc = new AnimationCurve();
        xc.AddKey(0f, 1f);
        xc.AddKey(d * 0.2f, 0.9f);
        xc.AddKey(d * 0.4f, 1.06f);
        xc.AddKey(d * 0.6f, 0.93f);
        xc.AddKey(d * 0.8f, 1.03f);
        xc.AddKey(d, 1f);
        clip.SetCurve("", typeof(Transform), "localScale.x", xc);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, $"{AnimFolder}/{name}.anim");
        return clip;
    }

    /// <summary>Angry shake (X position oscillation).</summary>
    static AnimationClip CreateShakeClip(string name)
    {
        var clip = new AnimationClip();
        clip.name = name;

        float d = 0.4f;
        var xc = new AnimationCurve();
        int shakes = 6;
        for (int i = 0; i <= shakes; i++)
        {
            float t = (float)i / shakes * d;
            float val = (i % 2 == 0 ? 0f : (i % 4 == 1 ? 0.08f : -0.08f));
            // Dampen over time
            val *= 1f - (float)i / shakes;
            xc.AddKey(t, val);
        }
        clip.SetCurve("", typeof(Transform), "localPosition.x", xc);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, $"{AnimFolder}/{name}.anim");
        return clip;
    }

    // ==================== HELPERS ====================

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
