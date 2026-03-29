using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; 
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class CockpitControlHandAttachment : MonoBehaviour
{
    [Header("Control Attach Poses")]
    public XRGrabInteractable grabInteractable;
    public Transform defaultAttach;
    public Transform leftAttach;
    public Transform rightAttach;

    [Header("Avatar Hand Targets")]
    public bool snapHumanoidHands = true;
    public VRHumanoidBinder humanoidBinder;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Optional Controller Visual Targets")]
    public bool snapControllerVisuals;
    public Transform leftControllerVisual;
    public Transform rightControllerVisual;

    struct TargetRestoreState
    {
        public Transform target;
        public Transform originalParent;
        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;
        public Vector3 originalLocalScale;
        public bool attached;
    }

    TargetRestoreState _leftHandState;
    TargetRestoreState _rightHandState;
    TargetRestoreState _leftControllerState;
    TargetRestoreState _rightControllerState;

    void Awake()
    {
        ResolveReferences();
        CacheRestoreState(ref _leftHandState, leftHandTarget);
        CacheRestoreState(ref _rightHandState, rightHandTarget);
        CacheRestoreState(ref _leftControllerState, leftControllerVisual);
        CacheRestoreState(ref _rightControllerState, rightControllerVisual);
    }

    void OnEnable()
    {
        ResolveReferences();

        if (!grabInteractable)
            return;

        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        if (grabInteractable)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        RestoreAllTargets();
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        ResolveReferences();

        var handedness = ResolveHandedness(args.interactorObject);
        var attach = GetAttachForHand(handedness);
        if (!attach)
            return;

        switch (handedness)
        {
            case InteractorHandedness.Left:
                if (snapHumanoidHands)
                    AttachTarget(ref _leftHandState, leftHandTarget, attach);

                if (snapControllerVisuals)
                    AttachTarget(ref _leftControllerState, leftControllerVisual, attach);
                break;

            case InteractorHandedness.Right:
                if (snapHumanoidHands)
                    AttachTarget(ref _rightHandState, rightHandTarget, attach);

                if (snapControllerVisuals)
                    AttachTarget(ref _rightControllerState, rightControllerVisual, attach);
                break;
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        switch (ResolveHandedness(args.interactorObject))
        {
            case InteractorHandedness.Left:
                RestoreTarget(ref _leftHandState);
                RestoreTarget(ref _leftControllerState);
                break;

            case InteractorHandedness.Right:
                RestoreTarget(ref _rightHandState);
                RestoreTarget(ref _rightControllerState);
                break;
        }
    }

    public Transform GetAttachForHand(InteractorHandedness handedness)
    {
        switch (handedness)
        {
            case InteractorHandedness.Left:
                if (leftAttach)
                    return leftAttach;
                break;

            case InteractorHandedness.Right:
                if (rightAttach)
                    return rightAttach;
                break;
        }

        if (defaultAttach)
            return defaultAttach;

        if (grabInteractable && grabInteractable.attachTransform)
            return grabInteractable.attachTransform;

        return transform;
    }

    void ResolveReferences()
    {
        if (!grabInteractable)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (!defaultAttach && grabInteractable)
            defaultAttach = grabInteractable.attachTransform;

        if (!humanoidBinder)
        {
            var plane = GetComponentInParent<PlaneController>(true);
            var searchRoot = plane ? plane.transform : transform.root;
            humanoidBinder = searchRoot.GetComponentInChildren<VRHumanoidBinder>(true);
        }

        if (!leftHandTarget && humanoidBinder)
            leftHandTarget = humanoidBinder.leftHandTarget;

        if (!rightHandTarget && humanoidBinder)
            rightHandTarget = humanoidBinder.rightHandTarget;
    }

    static InteractorHandedness ResolveHandedness(IXRInteractor interactor)
    {
        if (interactor == null)
            return InteractorHandedness.None;

        if (interactor.handedness != InteractorHandedness.None)
            return interactor.handedness;

        var interactorName = interactor.transform ? interactor.transform.name : string.Empty;
        if (interactorName.IndexOf("left", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return InteractorHandedness.Left;
        if (interactorName.IndexOf("right", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return InteractorHandedness.Right;

        return InteractorHandedness.None;
    }

    static void CacheRestoreState(ref TargetRestoreState state, Transform target)
    {
        if (!target)
            return;

        state.target = target;
        state.originalParent = target.parent;
        state.originalLocalPosition = target.localPosition;
        state.originalLocalRotation = target.localRotation;
        state.originalLocalScale = target.localScale;
        state.attached = false;
    }

    static void AttachTarget(ref TargetRestoreState state, Transform target, Transform attach)
    {
        if (!target || !attach)
            return;

        if (state.target != target)
            CacheRestoreState(ref state, target);

        target.SetParent(attach, false);
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = state.originalLocalScale;
        state.attached = true;
    }

    static void RestoreTarget(ref TargetRestoreState state)
    {
        if (!state.attached || !state.target)
            return;

        state.target.SetParent(state.originalParent, false);
        state.target.localPosition = state.originalLocalPosition;
        state.target.localRotation = state.originalLocalRotation;
        state.target.localScale = state.originalLocalScale;
        state.attached = false;
    }

    void RestoreAllTargets()
    {
        RestoreTarget(ref _leftHandState);
        RestoreTarget(ref _rightHandState);
        RestoreTarget(ref _leftControllerState);
        RestoreTarget(ref _rightControllerState);
    }
}
