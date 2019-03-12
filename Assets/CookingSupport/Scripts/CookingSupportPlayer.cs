using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using SIGVerse.Common;

namespace CookingSupport
{
    public interface IRecievePlaySwitch : IEventSystemHandler
    {
        void OnRecieveToLoad(string recipeID, int step, int procedure, string table);
        void OnRecieveToStart();
        void OnRecieveToStop();
    }

    //再生リストの要素
    public class UpdatingTransformTimeSeries
    {
        public float elapsedTime { get; set; }
        private List<UpdatingTransformData> updatingTransformList = new List<UpdatingTransformData>();
        private string graspedObjectsString;
        private Dictionary<string, string> collidingObjectsMap = new Dictionary<string, string>();

        public UpdatingTransformTimeSeries()
        {
            this.updatingTransformList = new List<UpdatingTransformData>();
        }

        public void AddUpdatingTransformData(UpdatingTransformData updatingTransformData)
        {
            this.updatingTransformList.Add(updatingTransformData);
        }

        public void AssignGraspedObjectsString(string graspedObjectsString)
        {
            this.graspedObjectsString = graspedObjectsString;
        }

        public void AddCollidingObjectsMap(string key, string value)
        {
            this.collidingObjectsMap[key] = value;
        }

        public List<UpdatingTransformData> GetUpdatingTransformList()
        {
            return this.updatingTransformList;
        }

        public string GetGraspedObjectsString()
        {
            return this.graspedObjectsString;
        }

        public Dictionary<string, string> GetCollidingObjectsMap()
        {
            return this.collidingObjectsMap;
        }
    }

    [RequireComponent(typeof(CookingSupportPlaybackCommon))]
    public class CookingSupportPlayer : MonoBehaviour, IRecievePlaySwitch
    {
        public GameObject instructor;

        private float elapsedTime;

        private Dictionary<string, Transform> targetPathTransformMap = new Dictionary<string, Transform>();
        private List<UpdatingTransformTimeSeries> playingTransformList = new List<UpdatingTransformTimeSeries>();
        private int playingTransformIndex;

        private string graspedObjectsString;
        private Dictionary<string, string> collidingObjectsMap = new Dictionary<string, string>();

        private List<string> motionData = new List<string>();
        private List<string> handData = new List<string>();
        private List<string> graspingData = new List<string>();
        private List<string> collidingData = new List<string>();

        private bool isPlaying;
        private bool isNoMotion;
        private bool isPreparing;
        private bool isMotionLoaded;

        private CookingSupportPlaybackCommon playbackCommon;
        private CookingSupportDatabaseManager databaseManager;

        private string recipeID;
        private int step;
        private int procedure;
        private string tableName;

        private void Awake()
        {
            playbackCommon = this.gameObject.GetComponent<CookingSupportPlaybackCommon>();
            databaseManager = this.playbackCommon.databaseManager;
            instructor.SetActive(false);

            isPlaying = false;
            isPreparing = false;
            isNoMotion = false;
            elapsedTime = 0.0f;
        }

        private void Update()
        {
            this.elapsedTime += Time.deltaTime;

            //動作データを適用
            if (this.isPlaying)
            {
                PlayMotion();
            }

            //動作データが読み込まれたら，再生するためのパスマップと再生リストを作成
            if (this.isMotionLoaded)
            {
                SetTargetObjectPathTranformMap();
                CreatePlayingTransformList();

                this.isMotionLoaded = false;
                this.isPreparing = false;
            }
        }

        //動作データの準備
        public void OnRecieveToLoad(string recipeID, int step, int procedure, string table)
        {
            try
            {
                Debug.Log("Start loading motions.");

                this.recipeID = recipeID;
                this.step = step;
                this.procedure = procedure;
                this.tableName = table;

                this.isPreparing = true;
                this.isNoMotion = false;
                this.isMotionLoaded = false;

                Task.Run(async () => await LoadMotions());
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                throw ex;
            }
        }

        //再生開始時の処理
        public void OnRecieveToStart()
        {
            this.isNoMotion = false;
            try
            {
                //準備が終わったら
                if (!this.isPlaying && !this.isPreparing)
                {
                    Debug.Log("Player: Initialise");

                    //指導者アバタを出現させる
                    this.instructor.SetActive(true);

                    //対象物体の重力と衝突判定をなくす
                    foreach (GameObject obj in this.playbackCommon.GetTargetObjects())
                    {
                        obj.GetComponent<Rigidbody>().useGravity = false;
                        obj.GetComponent<Collider>().isTrigger = true;
                    }

                    this.playingTransformIndex = 0;
                    this.elapsedTime = 0.0f;

                    this.isPlaying = true;
                    this.isPreparing = false;
                }
                else
                {
                    Debug.Log("already start playing.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                throw ex;
            }
        }

        //再生終了時の処理
        public void OnRecieveToStop()
        {
            try
            {
                if (this.isPlaying)
                {
                    this.isPlaying = false;

                    //重力と衝突判定を元に戻す
                    foreach (GameObject obj in this.playbackCommon.GetTargetObjects())
                    {
                        //元々トリガー状態の物体以外
                        if (!this.playbackCommon.TriggerObjectNames.Contains(obj.name))
                        {
                            obj.GetComponent<Rigidbody>().useGravity = true;
                            obj.GetComponent<Collider>().isTrigger = false;
                        }
                    }

                    //位置・姿勢をリセットする
                    this.playbackCommon.ResetTargetObjectTransforms();

                    //アバタを消す
                    this.instructor.SetActive(false);

                    Debug.Log("Player : playing finished.");
                }
                else
                {
                    Debug.Log("already finish playing.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                throw ex;
            }
        }

        //録画データを再生
        private void PlayMotion()
        {
            //現在のインデックスが録画データ長を超えたら終了
            if (this.playingTransformIndex >= this.playingTransformList.Count)
            {
                OnRecieveToStop();
                return;
            }

            UpdatingTransformTimeSeries updatingTransformList = null;

            //経過時間を超える時刻のデータになるまで待つ
            while (this.elapsedTime >= this.playingTransformList[this.playingTransformIndex].elapsedTime)
            {
                updatingTransformList = this.playingTransformList[this.playingTransformIndex];
                this.playingTransformIndex++;

                if (this.playingTransformIndex >= this.playingTransformList.Count)
                {
                    break;
                }
            }

            if (updatingTransformList == null) { return; }

            //対象物体の位置・姿勢を現在のデータで更新
            foreach (UpdatingTransformData updatingTransformData in updatingTransformList.GetUpdatingTransformList())
            {
                updatingTransformData.UpdateTransform();
            }

            //現在の把持物体を格納
            this.graspedObjectsString = updatingTransformList.GetGraspedObjectsString();
            //現在の接触物体を格納
            this.collidingObjectsMap = new Dictionary<string, string>(updatingTransformList.GetCollidingObjectsMap());
        }

        //録画データを読み込み
        private async Task LoadMotions()
        {
            this.motionData.Clear();

            await Task.Run(() =>
            {
                this.motionData = this.databaseManager.SelectPlaybackMotions(this.recipeID, this.step, this.procedure, this.tableName);
            });

            this.handData = this.databaseManager.GetHandData();
            this.graspingData = this.databaseManager.GetGraspedObjects();
            this.collidingData = this.databaseManager.GetCollidingObjects();

            //動作データがないか判定
            if (this.motionData.Count == 0)
            {
                this.isNoMotion = true;
            }

            if (isNoMotion)
            {
                Debug.Log("No motions: " + this.step + "-" + this.procedure);
            }
            else
            {
                Debug.Log("Finish loading motions.");
            }

            this.isMotionLoaded = true;
        }

        //対象物体のパスとトランスフォームのマップを作成
        private void SetTargetObjectPathTranformMap()
        {
            this.targetPathTransformMap.Clear();

            //アバタの各部位
            foreach (Transform transform in this.instructor.GetComponentsInChildren<Transform>())
            {
                this.targetPathTransformMap.Add(SIGVerseUtils.GetHierarchyPath(transform), transform);
            }

            //対象物体
            foreach (GameObject targetObj in this.playbackCommon.GetTargetObjects())
            {
                this.targetPathTransformMap.Add(SIGVerseUtils.GetHierarchyPath(targetObj.transform), targetObj.transform);
            }
        }

        //録画データの再生リストを作成
        private void CreatePlayingTransformList()
        {
            this.playingTransformList.Clear();
            List<Transform> transformOrder = new List<Transform>();
            List<int> ignoredPathIndexes = new List<int>();
            List<string> targetNames = new List<string>();

            for (int index = 0; index < this.motionData.Count; index++)
            {
                string[] columnArray = this.motionData[index].Split(new char[] { '\t' }, 2);
                if (columnArray.Length < 2) { continue; }

                string headerStr = columnArray[0];
                string dataStr = columnArray[1];

                string[] headerArray = headerStr.Split(',');
                List<string> dataArray = new List<string>(dataStr.Split('\t'));

                List<string> collidingArray = new List<string>(this.collidingData[index].Split('\t'));

                //最初の定義の行の場合
                if (int.Parse(headerArray[1]) == CookingSupportPlaybackCommon.TypeDefMotion)
                {
                    transformOrder.Clear();
                    ignoredPathIndexes.Clear();
                    targetNames.Clear();

                    for (int i = 0; i < dataArray.Count; i++)
                    {
                        string transformPath = dataArray[i];
                        if (transformPath.Contains("Avatar"))
                        {
                            //再生対象のアバタにパスを変更
                            string replacedPath = transformPath.Replace("Avatar", this.instructor.name);
                            transformOrder.Add(this.targetPathTransformMap[replacedPath]);
                        }
                        else if (this.targetPathTransformMap.ContainsKey(transformPath))
                        {
                            transformOrder.Add(this.targetPathTransformMap[transformPath]);
                        }
                        else
                        {
                            //存在しないパスのインデックスを保存
                            ignoredPathIndexes.Add(i);
                            Debug.Log("Couldn't find the object that path is " + transformPath);
                        }
                    }

                    targetNames = collidingArray;
                }
                //それ以外のデータの行の場合
                else if (int.Parse(headerArray[1]) == CookingSupportPlaybackCommon.TypeValMotion)
                {
                    if (transformOrder.Count == 0) { continue; }

                    //経過時間を格納
                    UpdatingTransformTimeSeries timeSeriesMotionsData = new UpdatingTransformTimeSeries
                    {
                        elapsedTime = float.Parse(headerArray[0])
                    };

                    for (int i = dataArray.Count - 1; i >= 0; i--)
                    {
                        //存在しないパスのインデックスのデータを削除
                        if (ignoredPathIndexes.Contains(i))
                        {
                            dataArray.RemoveAt(i);
                        }
                    }

                    //動作データを格納
                    for (int i = 0; i < dataArray.Count; i++)
                    {
                        string[] transformValues = dataArray[i].Split(',');

                        UpdatingTransformData transformPlayer = new UpdatingTransformData();
                        transformPlayer.UpdatingTransform = transformOrder[i];

                        transformPlayer.LocalPosition = new Vector3(float.Parse(transformValues[0]), float.Parse(transformValues[1]), float.Parse(transformValues[2]));
                        transformPlayer.LocalRotation = new Vector3(float.Parse(transformValues[3]), float.Parse(transformValues[4]), float.Parse(transformValues[5]));

                        //スケールがある場合
                        if (transformValues.Length == 9)
                        {
                            transformPlayer.LocalScale = new Vector3(float.Parse(transformValues[6]), float.Parse(transformValues[7]), float.Parse(transformValues[8]));
                        }

                        //トランスフォームを追加
                        timeSeriesMotionsData.AddUpdatingTransformData(transformPlayer);
                    }

                    //把持物体の文字列を格納
                    timeSeriesMotionsData.AssignGraspedObjectsString(graspingData[index]);

                    //接触物体のマップを格納
                    for (int i = 0; i < collidingArray.Count; i++)
                    {
                        timeSeriesMotionsData.AddCollidingObjectsMap(targetNames[i], collidingArray[i]);
                    }

                    //再生リストに追加
                    this.playingTransformList.Add(timeSeriesMotionsData);
                }
            }
        }

        public bool GetIsPlaying()
        {
            return this.isPlaying;
        }

        public bool GetIsNoMotions()
        {
            return this.isNoMotion;
        }

        public bool GetIsPreparing()
        {
            return this.isPreparing;
        }

        public string GetGraspingObjectsString()
        {
            return this.graspedObjectsString;
        }

        public Dictionary<string, string> GetCollidingObjectsMap()
        {
            return this.collidingObjectsMap;
        }

        public List<string> GetHandData()
        {
            return this.handData;
        }
    }
}