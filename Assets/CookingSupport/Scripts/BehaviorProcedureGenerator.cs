using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

namespace CookingSupport
{
    public interface IRecieveGenerationSwitch : IEventSystemHandler
    {
        void OnRecieveToStart(string recipeID, int step, int procedure);
        void OnRecieveToStop();
        void OnRecieveToSave(string behaviorType);
    }

    public class BehaviorProcedureGenerator : MonoBehaviour, IRecieveGenerationSwitch
    {
        public CookingSupportModerator moderator;
        public CookingSupportDatabaseManager databaseManager;
        public CookingSupportPlaybackCommon playbackCommon;

        public float ObservationInterval = 0.2f; //把持・接触カウントを行う間隔（秒）
        public int GraspingCountThres = 10; //把持回数の閾値
        public int CollidingCountThres = 10; //接触回数の閾値

        private CookingSupportPlayer player;

        private Dictionary<string, int> graspingCountMap = new Dictionary<string, int>();
        private Dictionary<string, Dictionary<string, int>> collidingCountMaps = new Dictionary<string, Dictionary<string, int>>();

        private int expertID;
        private string recipeID;
        private int step;
        private int procedure;
        private string graspingText;
        private string collidingText;
        private string behaviorType;

        private float elapsedTime;
        private bool isObserving;
        private bool isExtracting;

        private void Awake()
        {
            player = this.playbackCommon.GetComponent<CookingSupportPlayer>();

            elapsedTime = 0.0f;
            isObserving = false;
            isExtracting = false;
        }

        private void Update()
        {
            //把持・接触回数のカウント
            if (this.isObserving)
            {   
                this.elapsedTime += Time.deltaTime;
                if (this.elapsedTime >= ObservationInterval)
                {
                    CountTimesToGraspAndCollide();
                }
            }
        }

        //把持・接触の観測開始時の処理
        public void OnRecieveToStart(string recipeID, int step, int procedure)
        {
            if (!this.isObserving)
            {
                this.expertID = this.databaseManager.expertID;
                this.recipeID = recipeID;
                this.step = step;
                this.procedure = procedure;

                this.graspingCountMap.Clear();
                this.collidingCountMaps.Clear();

                this.isObserving = true;
            }
        }

        //把持・接触の観測終了時の処理
        public void OnRecieveToStop()
        {
            if (this.isObserving)
            {
                this.isObserving = false;

                this.isExtracting = true;
                ExtractObjectsToGraspAndCollide();
            }
        }

        //MySQLに行動手順を追加
        public void OnRecieveToSave(string behaviorType)
        {
            this.behaviorType = behaviorType;
            base.StartCoroutine(InsertBehaviorProcedure());
        }

        //現在の把持・接触状態をカウントマップに加算
        private void CountTimesToGraspAndCollide()
        {
            string graspedObjectsString = "";
            Dictionary<string, string> collidingObjectsMap = new Dictionary<string, string>();

            //モードで切替
            if (this.moderator.executionMode == ExecutionMode.PlayMode)
            {
                graspedObjectsString = this.player.GetGraspingObjectsString();
                collidingObjectsMap = this.player.GetCollidingObjectsMap();
            }
            else
            {
                graspedObjectsString = this.playbackCommon.GetGraspedObjectsString();
                collidingObjectsMap = this.playbackCommon.GetColldingObjectsMap();
            }

            //把持回数マップに加算
            string[] graspedObjects = graspedObjectsString.Split(',');
            foreach (string grasping in graspedObjects)
            {
                if (grasping != "")
                {
                    if (this.graspingCountMap.ContainsKey(grasping))
                    {
                        this.graspingCountMap[grasping] = this.graspingCountMap[grasping] + 1;
                    }
                    else
                    {
                        this.graspingCountMap[grasping] = 1;
                    }
                }
            }

            //接触回数マップに加算
            foreach (var collidingPair in collidingObjectsMap)
            {
                string collidingFrom = collidingPair.Key;
                string[] collidingObjects = collidingPair.Value.Split(',');

                foreach (string collidingTo in collidingObjects)
                {
                    if (collidingTo != "")
                    {
                        if (this.collidingCountMaps.ContainsKey(collidingFrom))
                        {
                            if (this.collidingCountMaps[collidingFrom].ContainsKey(collidingTo))
                            {
                                this.collidingCountMaps[collidingFrom][collidingTo] = this.collidingCountMaps[collidingFrom][collidingTo] + 1;
                            }
                            else
                            {
                                this.collidingCountMaps[collidingFrom][collidingTo] = 1;
                            }
                        }
                        else
                        {
                            this.collidingCountMaps[collidingFrom] = new Dictionary<string, int> { { collidingTo, 1 } };
                        }
                    }
                }
            }
        }

        //把持・接触物体をカウントマップをもとに抽出
        private void ExtractObjectsToGraspAndCollide()
        {
            //閾値をもとに把持物体を抽出
            List<string> graspedObjects = new List<string>();
            foreach(string obj in this.graspingCountMap.Keys)
            {
                if(this.graspingCountMap[obj] > GraspingCountThres)
                {
                    graspedObjects.Add(obj);
                }
            }

            //閾値をもとに接触物体を抽出
            List<string> collidingObjects = new List<string>();
            if (graspedObjects.Count != 0)
            {
                foreach (string grasp in graspedObjects)
                {
                    //把持された物体に限定
                    if (this.collidingCountMaps.ContainsKey(grasp))
                    {
                        foreach (string collide in this.collidingCountMaps[grasp].Keys)
                        {
                            if (this.collidingCountMaps[grasp][collide] > CollidingCountThres)
                            {
                                collidingObjects.Add(grasp);
                                collidingObjects.Add(collide);
                            }
                        }
                    }
                }
            }

            this.graspingText = string.Join(",", graspedObjects);
            this.collidingText = string.Join(",", collidingObjects.Distinct()); //重複した物体を省く

            //moderatorに結果を送信
            ExecuteEvents.Execute<IRecieveGenerationResult>(this.moderator.gameObject, null, (reciever, eventData) => reciever.OnRecieveExtractedObjects(this.graspingText, this.collidingText));

            this.isExtracting = false;
        }

        //SQLにモデルのデータを挿入
        private IEnumerator InsertBehaviorProcedure()
        {
            //抽出が終わるまで待つ
            while (this.isExtracting)
            {
                yield return new WaitForEndOfFrame();
            }

            this.databaseManager.InsertBehaviorProcedures(this.expertID, this.recipeID, this.step, this.procedure, this.graspingText, this.collidingText, this.behaviorType);
        }

        public bool GetIsExtracting()
        {
            return isExtracting;
        }
    }
}
