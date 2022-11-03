using Gallop;
using Newtonsoft.Json.Linq;
using RootMotion.Dynamics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UmaContainer : MonoBehaviour
{
    public JObject CharaData;
    public GameObject Body;
    public GameObject Tail;
    public GameObject Head;
    public GameObject PhysicsController;

    public List<Texture2D> TailTextures = new List<Texture2D>();
    public Animator UmaAnimator;
    public AnimatorOverrideController OverrideController;

    public bool isAnimatorControl;
    public Animator UmaFaceAnimator;
    public AnimatorOverrideController FaceOverrideController;

    [Header("Face")]
    public FaceDrivenKeyTarget FaceDrivenKeyTarget;
    public FaceEmotionKeyTarget FaceEmotionKeyTarget;
    public GameObject HeadBone;
    public GameObject TrackTarget;
    public float EyeHeight;

    [Header("Generic")]
    public bool IsGeneric = false;
    public string VarCostumeIdShort, VarCostumeIdLong, VarSkin, VarHeight, VarSocks, VarBust;
    public List<Texture2D> GenericBodyTextures = new List<Texture2D>();

    [Header("Mini")]
    public bool IsMini = false;
    public List<Texture2D> MiniHeadTextures = new List<Texture2D>();

    public void MergeModel()
    {
        if (!Body) return;
        EyeHeight = 0.1f;
        TrackTarget = Camera.main.gameObject;

        List<Transform> bodybones = new List<Transform>(Body.GetComponentInChildren<SkinnedMeshRenderer>().bones);
        List<Transform> emptyBones = new List<Transform>();
        emptyBones.Add(Body.GetComponentInChildren<SkinnedMeshRenderer>().rootBone.Find("Tail_Ctrl"));
        while (Body.transform.childCount > 0)
        {
            var child = Body.transform.GetChild(0);
            child.SetParent(transform);
        }
        Body.SetActive(false); //for debugging


        //MergeHead
        if (Head)
        {
            var headskins = Head.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer headskin in headskins)
            {
                emptyBones.AddRange(MergeBone(headskin, bodybones));
            }
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(transform);
            while (Head.transform.childCount > 0)
            {
                var child = Head.transform.GetChild(0);
                child.SetParent(child.name.Contains("info") ? eyes.transform : transform);
            }
            Head.SetActive(false); //for debugging
        }


        //MergeTail
        if (Tail)
        {
            var tailskin = Tail.GetComponentInChildren<SkinnedMeshRenderer>();
            emptyBones.AddRange(MergeBone(tailskin, bodybones));
            while (Tail.transform.childCount > 0)
            {
                var child = Tail.transform.GetChild(0);
                child.SetParent(transform);
            }
            Tail.SetActive(false); //for debugging
            emptyBones.ForEach(a => { if (a) Destroy(a.gameObject); });
        }

        //MergeAvatar
        UmaAnimator = gameObject.AddComponent<Animator>();
        UmaAnimator.avatar = AvatarBuilder.BuildGenericAvatar(gameObject, gameObject.name);
        OverrideController = Instantiate(UmaViewerBuilder.Instance.OverrideController);
        UmaAnimator.runtimeAnimatorController = OverrideController;

    }

    public Transform[] MergeBone(SkinnedMeshRenderer from, List<Transform> targetBones)
    {
        var rootbone = targetBones.FindLast(a => a.name.Equals(from.rootBone.name));
        if (rootbone) from.rootBone = rootbone;

        List<Transform> emptyBones = new List<Transform>();
        Transform[] tmpBone = new Transform[from.bones.Length];
        for (int i = 0; i < tmpBone.Length; i++)
        {
            var targetbone = targetBones.FindLast(a => a.name.Equals(from.bones[i].name));
            if (targetbone)
            {
                tmpBone[i] = targetbone;
                from.bones[i].position = targetbone.position;
                while (from.bones[i].transform.childCount > 0)
                {
                    from.bones[i].transform.GetChild(0).SetParent(targetbone);
                }
                emptyBones.Add(from.bones[i]);
            }
            else
            {
                tmpBone[i] = from.bones[i];
            }
        }
        from.bones = tmpBone;
        return emptyBones.ToArray();
    }

    public void LoadPhysics()
    {
        var springs = PhysicsController.GetComponentsInChildren<CySpringDataContainer>();
        foreach (CySpringDataContainer spring in springs)
        {
            spring.InitializePhysics();
        }
    }

    private void FixedUpdate()
    {
        if (TrackTarget&&!isAnimatorControl&&!IsMini)
        {
            var targetPosotion = TrackTarget.transform.position - HeadBone.transform.up*EyeHeight;
            var deltaPos = HeadBone.transform.InverseTransformPoint(targetPosotion);
            var deltaRotation = HeadBone.transform.InverseTransformDirection(deltaPos.normalized);
            var finalRotation = new Vector2(Mathf.Clamp(deltaRotation.x / 0.86f,-1,1), Mathf.Clamp(deltaRotation.y / 0.66f, -1, 1));//Limited to the angle of view 
            FaceDrivenKeyTarget.SetEyeRange(finalRotation.x, finalRotation.y, finalRotation.x, -finalRotation.y);
        }

        if (isAnimatorControl && !IsMini)
        {
            FaceDrivenKeyTarget.ProcessLocator();
        }
    }
}
