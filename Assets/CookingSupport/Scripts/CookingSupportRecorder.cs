#pragma warning disable 0414
using UnityEngine;
using System;
using System.Collections.Generic;
using SIGVerse.Common;
using UnityEngine.EventSystems;
using System.Threading.Tasks;

namespace CookingSupport
{
    public interface IRecieveRecordSwitch : IEventSystemHandler
    {
        void OnRecieveToStart(string recipeID, int step, int procedure, string table);
        void OnRecieveToStop();
    }

    [RequireComponent(typeof(CookingSupportPlaybackCommon))]
    public class CookingSupportRecorder : MonoBehaviour, IRecieveRecordSwitch
    {
        public int RecordInterval = 20; //録画を行う間隔（ミリ秒）

        public List<string> ignoringKeywords = new List<string> { "OVRCameraRig", "Avatar/LeftEyeAnchor", "Avatar/RightEyeAnchor", "Avatar/TrackerAnchor" };

        private List<Transform> targetTransformInstances　= new List<Transform>();
        private List<string> targetNames = new List<string>();

        private float elapsedTime;
        private float previousRecordedTime;

        private List<string> savedMotionStrings = new List<string>();
        private string savedMotionDefinitionString;
        private List<string> savedHandStrings = new List<string>();
        private List<string> savedGraspingStrings = new List<string>();
        private List<string> savedCollidingStrings = new List<string>();
        private string savedCollidingDefinitionString;

        private bool isRecording;
        private bool isWriting;

        private string recipeID;
        private int step;
        private int procedure;
        private string table;

        private CookingSupportPlaybackCommon playbackCommon;
        private CookingSupportDatabaseManager databaseManager;

        private void Awake()
        {
            playbackCommon = this.gameObject.GetComponent<CookingSupportPlaybackCommon>();
            databaseManager = this.playbackCommon.databaseManager;

            isRecording = false;
            isWriting = false;
            elapsedTime = 0.0f;
            previousRecordedTime = 0.0f;
        }

        private void Update()
        {
            this.elapsedTime += Time.deltaTime;

            //動作データを蓄積
            if (this.isRecording)
            {
                this.SaveMotion();
            }
        }

        //録画開始時の処理
        public void OnRecieveToStart(string recipeID, int step, int procedure, string table)
        {
            try
            {
                if (!this.isRecording)
                {
                    this.recipeID = recipeID;
                    this.step = step;
                    this.procedure = procedure;
                    this.table = table;

                    this.savedMotionStrings = new List<string>();
                    this.savedMotionDefinitionString = string.Empty;

                    this.savedHandStrings.Clear();
                    this.savedGraspingStrings.Clear();
                    this.savedCollidingStrings.Clear();
                    this.savedCollidingDefinitionString = string.Empty;

                    this.elapsedTime = 0.0f;
                    this.previousRecordedTime = 0.0f;

                    Debug.Log("Start Recording");

                    SetTargetTransforms();
                    CreatePlaybackMotionDefinition();

                    this.isRecording = true;
                }
                else
                {
                    Debug.Log("already start recording.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                throw ex;
            }
        }

        //録画終了時の処理
        public void OnRecieveToStop()
        {
            try
            {
                if (this.isRecording)
                {
                    Debug.Log("Stop Recording");

                    this.isRecording = false;
                    this.isWriting = true;

                    Task.Run(async () => await InsertMotions());
                }
                else
                {
                    Debug.Log("cannot finish recording.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                throw ex;
            }
        }

        //録画を行う対象物体を格納
        private void SetTargetTransforms()
        {
            this.targetTransformInstances.Clear();

            //アバタの各部位
            foreach (Transform avatarTransform in this.playbackCommon.GetAvatar().GetComponentsInChildren<Transform>())
            {
                if (!this.ignoringKeywords.Exists(x => SIGVerseUtils.GetHierarchyPath(avatarTransform).Contains(x)))
                {
                    this.targetTransformInstances.Add(avatarTransform);
                }
            }

            //対象物体
            foreach (GameObject graspingCandidate in this.playbackCommon.GetTargetObjects())
            {
                this.targetTransformInstances.Add(graspingCandidate.transform);
            }
        }

        //録画データのヘッダーを作成
        private void CreatePlaybackMotionDefinition()
        {
            //動作データのヘッダー
            this.savedMotionDefinitionString += "0.0," + CookingSupportPlaybackCommon.TypeDefMotion;

            List<string> linkPathMotionList = new List<string>();
            foreach (Transform transform in this.targetTransformInstances)
            {
                string linkPath = SIGVerseUtils.GetHierarchyPath(transform);

                this.savedMotionDefinitionString += "\t" + linkPath;

                if (linkPathMotionList.Contains(linkPath))
                {
                    Debug.LogError("Objects in the same path exist. path = " + linkPath);
                    throw new Exception("Objects in the same path exist.");
                }

                linkPathMotionList.Add(linkPath);
            }

            //接触している物体のヘッダー
            this.targetNames.Clear();
            foreach (GameObject obj in this.playbackCommon.GetTargetObjects())
            {
                this.targetNames.Add(obj.name);
            }

            this.savedCollidingDefinitionString = string.Join("\t", this.targetNames);
        }

        //行動データを蓄積
        private void SaveMotion()
        {
            //前に録画した時間から録画間隔の時間を過ぎていない場合，リターン
            if (1000.0 * (this.elapsedTime - this.previousRecordedTime) < RecordInterval) { return; }

            //動作データ
            string motionLineStr = string.Empty;

            motionLineStr += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero) + "," + CookingSupportPlaybackCommon.TypeValMotion;

            foreach (Transform transform in this.targetTransformInstances)
            {
                motionLineStr += "\t" +
                    Math.Round(transform.localPosition.x, 4, MidpointRounding.AwayFromZero) + "," +
                    Math.Round(transform.localPosition.y, 4, MidpointRounding.AwayFromZero) + "," +
                    Math.Round(transform.localPosition.z, 4, MidpointRounding.AwayFromZero) + "," +
                    Math.Round(transform.localEulerAngles.x, 4, MidpointRounding.AwayFromZero) + "," +
                    Math.Round(transform.localEulerAngles.y, 4, MidpointRounding.AwayFromZero) + "," +
                    Math.Round(transform.localEulerAngles.z, 4, MidpointRounding.AwayFromZero);
                //Math.Round(transform.localScale.x,       4, MidpointRounding.AwayFromZero) + "," +
                //Math.Round(transform.localScale.y,       4, MidpointRounding.AwayFromZero) + "," +
                //Math.Round(transform.localScale.z,       4, MidpointRounding.AwayFromZero);
            }

            this.savedMotionStrings.Add(motionLineStr);

            //手の動作データ
            string handLineStr = 
                Math.Round(this.playbackCommon.GetLeftHand().position.x, 4, MidpointRounding.AwayFromZero) + "," +
                Math.Round(this.playbackCommon.GetLeftHand().position.y, 4, MidpointRounding.AwayFromZero) + "," +
                Math.Round(this.playbackCommon.GetLeftHand().position.z, 4, MidpointRounding.AwayFromZero) + "," +
                Math.Round(this.playbackCommon.GetRightHand().position.x, 4, MidpointRounding.AwayFromZero) + "," +
                Math.Round(this.playbackCommon.GetRightHand().position.y, 4, MidpointRounding.AwayFromZero) + "," +
                Math.Round(this.playbackCommon.GetRightHand().position.z, 4, MidpointRounding.AwayFromZero);

            this.savedHandStrings.Add(handLineStr);

            //把持されている物体のデータ
            this.savedGraspingStrings.Add(this.playbackCommon.GetGraspedObjectsString());

            //接触している物体のデータ
            List<string> collidingObjs = new List<string>();
            Dictionary<string, string> collidingMap = this.playbackCommon.GetColldingObjectsMap();
            foreach (string name in this.targetNames)
            {
                collidingObjs.Add(collidingMap[name]);
            }
            this.savedCollidingStrings.Add(string.Join("\t", collidingObjs));

            this.previousRecordedTime = this.elapsedTime;
        }

        //録画データをSQLに挿入
        private async Task InsertMotions()
        {
            await Task.Run(() => this.databaseManager.InsertPlaybackMotions(this.savedMotionDefinitionString, this.savedMotionStrings, this.savedHandStrings, this.savedGraspingStrings,
                this.savedCollidingDefinitionString, this.savedCollidingStrings, this.recipeID, this.step, this.procedure, this.table));

            this.isWriting = false;
        }

        public bool GetIsRecording()
        {
            return this.isRecording;
        }

        public bool GetIsWriting()
        {
            return this.isWriting;
        }

        public int GetSavedCount()
        {
            return this.savedMotionStrings.Count;
        }
    }
}