using System.Collections.Generic;
using UnityEngine;
using NewtonVR;
using System.Linq;

namespace CookingSupport
{
    public class UpdatingTransformData
    {
        public Transform UpdatingTransform { get; set; }

        public Vector3 LocalPosition { get; set; }
        public Vector3 LocalRotation { get; set; }
        public Vector3? LocalScale { get; set; }

        public void UpdateTransform()
        {
            this.UpdatingTransform.localPosition = this.LocalPosition;
            this.UpdatingTransform.localEulerAngles = this.LocalRotation;

            if (this.LocalScale != null)
            {
                this.UpdatingTransform.localScale = this.LocalScale.Value;
            }
        }
    }

    public class CookingSupportPlaybackCommon : MonoBehaviour
    {
        public CookingSupportDatabaseManager databaseManager;

        public GameObject avatar;

        public NVRHead head;
        public NVRHand leftHand;
        public NVRHand rightHand;

        public Transform leftHandTransform;
        public Transform rightHandTransform;

        public GameObject graspingLeftHandPrefab;
        public GameObject graspingRightHandPrefab;

        [HideInInspector]
        public string objectInLeftHand = "";
        [HideInInspector]
        public string objectInRightHand = "";

        private List<UpdatingTransformData> initialLeftFingers = new List<UpdatingTransformData>();
        private List<UpdatingTransformData> initialRightFingers = new List<UpdatingTransformData>();

        public const int TypeDefMotion = 10;
        public const int TypeValMotion = 11;
        public const string TagNameOfGraspables = "Graspables";

        private List<GameObject> targetObjects = new List<GameObject>();

        private Dictionary<string, UpdatingTransformData> initialTransformMap = new Dictionary<string, UpdatingTransformData>();
        private List<Rigidbody> targetRigidbodies = new List<Rigidbody>();

        private List<GameObject> targetTools = new List<GameObject>();
        private List<GameObject> targetBases = new List<GameObject>();

        [HideInInspector]
        public List<string> TriggerObjectNames { get; set; } = new List<string>();

        private void Awake()
        {
            //把持可能な物体を対象物体とする
            targetObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("Graspables"));

            foreach (GameObject targetObj in targetObjects)
            {
                //初期トランスフォームを設定
                this.SetInitialTransform(targetObj.transform);

                //道具
                if(targetObj.transform.parent.name == "CookingTools")
                {
                    this.targetTools.Add(targetObj);
                }
                //台
                if(targetObj.transform.parent.name == "CookingBases")
                {
                    this.targetBases.Add(targetObj);
                }
            }

            //左手の初期値
            foreach (Transform finger in this.leftHandTransform)
            {
                UpdatingTransformData initialTransform = new UpdatingTransformData();
                initialTransform.UpdatingTransform = transform;

                initialTransform.LocalPosition = transform.localPosition;
                initialTransform.LocalRotation = transform.localEulerAngles;
                initialTransform.LocalScale = transform.localScale;

                this.initialLeftFingers.Add(initialTransform);
            }
            //右手の初期値
            foreach (Transform finger in this.rightHandTransform)
            {
                UpdatingTransformData initialTransform = new UpdatingTransformData();
                initialTransform.UpdatingTransform = transform;

                initialTransform.LocalPosition = transform.localPosition;
                initialTransform.LocalRotation = transform.localEulerAngles;
                initialTransform.LocalScale = transform.localScale;

                this.initialRightFingers.Add(initialTransform);
            }
        }

        private void Update()
        {
            //左
            //トリガーを押した場合，指を曲げた手に変更
            if (this.leftHand.HoldButtonPressed)
            {
                foreach (Transform finger in this.graspingLeftHandPrefab.transform)
                {
                    Transform avatarFinger = this.leftHandTransform.Find(finger.name);
                    avatarFinger.localRotation = finger.localRotation;
                }
            }
            //トリガーを離した場合，初期の手にリセット
            if (this.leftHand.HoldButtonUp)
            {
                foreach (UpdatingTransformData finger in this.initialLeftFingers)
                {
                    finger.UpdateTransform();
                }
            }

            //右
            if (this.rightHand.HoldButtonPressed)
            {
                foreach (Transform finger in this.graspingRightHandPrefab.transform)
                {
                    Transform avatarFinger = this.rightHandTransform.Find(finger.name);
                    avatarFinger.localRotation = finger.localRotation;
                }
            }
            if (this.rightHand.HoldButtonUp)
            {
                foreach (UpdatingTransformData finger in this.initialRightFingers)
                {
                    finger.UpdateTransform();
                }
            }

            //把持された物体を格納
            this.objectInLeftHand = GetObjectIdInLeftHand();
            this.objectInRightHand = GetObjectIdInRightHand();
        }

        //対象物体を初期化
        public void InitializeTargetObjects()
        {
            this.targetObjects.Clear();
            this.initialTransformMap.Clear();
            this.targetRigidbodies.Clear();

            this.targetObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("Graspables"));

            foreach (GameObject targetObj in this.targetObjects)
            {
                SetInitialTransform(targetObj.transform);

                Rigidbody[] rigidbodies = targetObj.transform.GetComponentsInChildren<Rigidbody>();

                foreach (Rigidbody rigidbody in rigidbodies)
                {
                    this.targetRigidbodies.Add(rigidbody);
                }
            }
        }

        //現在のトランスフォームを初期値に設定
        public void SetInitialTransform(Transform targetTransform)
        {
            Transform[] transforms = targetTransform.GetComponentsInChildren<Transform>();

            foreach (Transform transform in transforms)
            {
                UpdatingTransformData initialTransform = new UpdatingTransformData
                {
                    UpdatingTransform = transform,
                    LocalPosition = transform.localPosition,
                    LocalRotation = transform.localEulerAngles,
                    LocalScale = transform.localScale
                };

                this.initialTransformMap[transform.name] = initialTransform;
            }
        }

        //対象物体の位置・姿勢を初期値に戻す
        public void ResetTargetObjectTransforms()
        {
            foreach (UpdatingTransformData initialTransform in this.initialTransformMap.Values)
            {
                initialTransform.UpdateTransform();
            }

            foreach (Rigidbody rigidbody in this.targetRigidbodies)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        //把持されている物体の名前を取得
        public string GetGraspedObjectId(NVRHand hand)
        {
            string graspedObject = "";
            if (hand.HoldButtonPressed)
            {
                if (hand.IsInteracting)
                {
                    if (hand.CurrentlyInteracting.tag == TagNameOfGraspables)
                    {
                        graspedObject = hand.CurrentlyInteracting.name;
                    }
                }
            }
            return graspedObject;
        }

        //左手
        public string GetObjectIdInLeftHand()
        {
            return GetGraspedObjectId(leftHand);
        }
        //右手
        public string GetObjectIdInRightHand()
        {
            return GetGraspedObjectId(rightHand);
        }

        //対象物体で把持されている物体を文字列で取得
        public string GetGraspedObjectsString()
        {
            string graspedObjectsString = objectInLeftHand + ","  + objectInRightHand;

            return graspedObjectsString;
        }

        //対象物体が接触している物体をマップで取得
        public Dictionary<string, string> GetColldingObjectsMap()
        {
            Dictionary<string, string> collidingMap = new Dictionary<string, string>();

            foreach (GameObject obj in targetObjects)
            {
                List<string> collidingIds = obj.GetComponent<CollisionChecker>().GetCollidingObjects();
                string collidingString = string.Join(",", collidingIds);
                if(collidingString == "")
                {
                    collidingString = ",";
                }
                collidingMap[obj.name] = collidingString;
            }

            return collidingMap;
        }

        public GameObject GetAvatar()
        {
            return avatar;
        }

        public Transform GetHead()
        {
            return head.gameObject.transform;
        }

        public Transform GetLeftHand()
        {
            return leftHand.gameObject.transform;
        }

        public Transform GetRightHand()
        {
            return rightHand.gameObject.transform;
        }

        public List<GameObject> GetTargetObjects()
        {
            return targetObjects;
        }

        public List<GameObject> GetTargetTools()
        {
            return targetTools;
        }

        public List<GameObject> GetTargetBases()
        {
            return targetBases;
        }
    }
}