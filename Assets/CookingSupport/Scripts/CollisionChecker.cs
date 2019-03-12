using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CookingSupport
{
    //This script needs to be attached to target objects.
    public class CollisionChecker : MonoBehaviour
    {
        private List<string> collidingObjects = new List<string>();

        private List<string> exitedObjects = new List<string>();

        private const string TagNameOfGraspables = "Graspables";
        private const string IngredientHeader = "ingredient";
        private const string ProcessedFooedHeader = "processed_food";
        private const string LeftHandColliderName = "EthanLeftHand";
        private const string RightHandColliderName = "EthanRightHand";

        private CookingSupportPlaybackCommon common;

        void Start()
        {
            common = GameObject.FindObjectOfType<CookingSupportPlaybackCommon>();
        }

        void Update()
        {
            //把持している物体と接触していた場合，接触物体から削除
            if (collidingObjects.Contains(LeftHandColliderName) && this.common.objectInLeftHand != "")
            {
                collidingObjects.Remove(LeftHandColliderName);
            }
            if (collidingObjects.Contains(RightHandColliderName) && this.common.objectInRightHand != "")
            {
                collidingObjects.Remove(RightHandColliderName);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            //材料どうしの接触を無視する場合
            bool isBothIngredient = false;
            //if (this.gameObject.name.Contains(IngredientHeader) || this.gameObject.name.Contains(ProcessedFooedHeader))
            //{
            //    if (collision.gameObject.name.Contains(IngredientHeader) || collision.gameObject.name.Contains(ProcessedFooedHeader))
            //    {
            //        isBothIngredient = true;
            //    }
            //}

            //把持物体の場合，接触物体に追加
            if (collision.gameObject.tag == TagNameOfGraspables && !collidingObjects.Contains(collision.gameObject.name))
            {
                if (!isBothIngredient)
                {
                    collidingObjects.Add(collision.gameObject.name);
                }
            }

            //分離物体から削除
            if (exitedObjects.Contains(collision.gameObject.name))
            {
                exitedObjects.Remove(collision.gameObject.name);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            bool isCollided = false;

            if (!collidingObjects.Contains(other.gameObject.name))
            {
                //左手
                if (other.gameObject.name == LeftHandColliderName && this.common.objectInLeftHand == "")
                {
                    isCollided = true;
                }
                //右手
                if (other.gameObject.name == RightHandColliderName && this.common.objectInRightHand == "")
                {
                    isCollided = true;
                }
                //物体
                if (other.gameObject.tag == TagNameOfGraspables)
                {
                    isCollided = true;
                }
            }

            //いずれかに該当していたら，接触物体に追加
            if (isCollided)
            {
                collidingObjects.Add(other.gameObject.name);
            }

            if (exitedObjects.Contains(other.gameObject.name))
            {
                exitedObjects.Remove(other.gameObject.name);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            //接触物体から削除（分離物体に追加）
            if (collidingObjects.Contains(collision.gameObject.name))
            {
                collidingObjects.Remove(collision.gameObject.name);
                exitedObjects.Add(collision.gameObject.name);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            //接触物体から削除
            if (collidingObjects.Contains(other.gameObject.name))
            {
                collidingObjects.Remove(other.gameObject.name);
                exitedObjects.Add(other.gameObject.name);
            }
        }

        public List<string> GetCollidingObjects()
        {
            return collidingObjects;
        }

        public List<string> GetExitedObjects()
        {
            return exitedObjects;
        }

        public void ClearObjects()
        {
            this.collidingObjects.Clear();
            this.exitedObjects.Clear();
        }
    }
}
