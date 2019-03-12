using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NewtonVR;
using System.Linq;
using UnityEngine.EventSystems;
using System.IO;
using SIGVerse.Common;
using System.Threading.Tasks;

namespace CookingSupport
{
    //行動手順モデルの要素
    public struct BehaviorProcedure
    {
        public List<string> graspedObjects;
        public List<string> collidingObjects;
        public string behaviorType;
    }

    //実行モードの種類
    public enum ExecutionMode
    {
        AllGuidanceMode,
        NeededGuidanceMode,
        NoGuidanceMode,
        RecordMode,
        PlayMode,
        ExtraMode
    }

    //再生データのテーブルの種類
    public enum PlaybackTableName
    {
        expert,
        user
    }

    public interface IRecieveRecognitionResult : IEventSystemHandler
    {
        void OnRecieveMotionError(float sum);
    }

    public interface IRecieveGenerationResult : IEventSystemHandler
    {
        void OnRecieveExtractedObjects(string graspedObjects, string collidingObjects);
    }

    public class CookingSupportModerator : MonoBehaviour, IRecieveRecognitionResult, IRecieveGenerationResult
    {
        [Header("script")]
        public CookingSupportRecipeManager recipeManager;
        public CookingSupportDatabaseManager databaseManager;
        public CookingSupportPlaybackCommon playbackCommon;
        public BehaviorProcedureGenerator generator;
        public BehaviorRecognizer recognizer;

        private CookingSupportRecorder recorder;
        private CookingSupportPlayer player;
        private VoiceSynthesisExternal sapi;

        [Header("panel")]
        public GameObject instructionPanel;
        public UnityEngine.UI.Text instructionText;
        public UnityEngine.UI.Text headerText;
        public GameObject howToPanel;
        public UnityEngine.UI.Text howToText;
        public GameObject avatarNoticePanel;
        public UnityEngine.UI.Text avatarNoticeText;
        public GameObject taskListPanel;
        public UnityEngine.UI.Text taskListText;
        public UnityEngine.UI.Text stateText;

        [Header("ingredient")]
        public Transform ingredientParent;
        public GameObject ingredientPrefab;
        public Material transparentMaterial;
        public Material processedMaterial;

        [Header("mode")]
        public ExecutionMode executionMode = ExecutionMode.AllGuidanceMode;
        public bool isMotionRecognized = false;
        public bool isModelGenerated = false;
        public bool existsProcessedFood = false;

        [Header("option")]
        public float DecisionIntervalTime = 0.2f; //把持・接触判定を行う間隔（秒）
        public int CorrectDecisionCount = 10; //正しい把持・接触になった回数の閾値
        public float IncorrectDecisionTime = 30.0f; //間違っていると判定される秒数
        public bool isMotionPlayedFirstly = false;
        public bool isIncorrectOnWrongGrasp = false;

        [Header("play")]
        public PlaybackTableName motionTableNameForPlay = PlaybackTableName.expert;
        public int beginningStepForPlay = 1;

        [Header("id")]
        public int expertID = 1;
        public int userID = 1;
        public string userName = "yamaguchi";
        public bool isEndedTaskChecked = false;

        [Header("experiment")]
        public bool isInOperationPractice = false;
        public bool isUnderWOZDecision = false;
        public UnityEngine.UI.Text remainingText;
        public bool isRecordingUserMotion = false;
        public bool isRecordingElapsedTime = false;
        public bool isRecordingMotionError = false;
        //public int experimentType = 0; //1 or 2 or 3

        private const string TagNameOfGraspables = "Graspables";
        private const string IngredientHeader = "ingredient";
        private const string ProcessedFoodHeader = "processed_food";
        private const string RecordingFolder = "RecordData";

        private const float MovingDistanceThres = 0.2f; //動いている判定の閾値
        private const float MotionSimilarityThres = 0.6f; //正しい動作の類似度の閾値

        private string recipeID;
        private int currentStep;
        private int currentProcedure;

        private string objectIdInLeftHand;
        private string objectIdInRightHand;

        private List<GameObject> targetObjects = new List<GameObject>();
        private List<GameObject> ingredients = new List<GameObject>();
        private Dictionary<string, string> ingredientOnBaseMap = new Dictionary<string, string>();
        private List<String> processedFoodNames = new List<String>();
        private Transform processedFoodParent;

        private SortedDictionary<string, BehaviorProcedure> behaviorProceduresMap = new SortedDictionary<string, BehaviorProcedure>();

        private Dictionary<string, string> behaviorTypeInstructionMap = new Dictionary<string, string>();
        private Dictionary<string, string> behaviorTypeMeaningMap = new Dictionary<string, string>();
        private List<string> behaviorTypes = new List<string>();
        private List<string> processingBehaviorTypes = new List<string>();
        private List<string> graspingBehaviorTypes = new List<string>();
        private List<string> collidingBehaviorTypes = new List<string>();

        private string extractedGraspedText = "";
        private string extractedCollidingText = "";
        private int selectedTypeIndex = 0;

        private bool isGuidanceStarted;
        private bool isDuringGuidance;
        private bool isBehaviorProceduresSet;

        private bool isChoosingBehaviorType;
        private bool onRecording;
        private bool onPlaying;

        private bool isCorrectMotion;
        private bool isIncorrectMotion;

        private float incorrectElapsedTime;
        private float elapsedTimeForDecision;

        private int totalCorrectTimes;
        private float maxSimilarity;
        private float currentError;

        private float wholeElapsedTime;
        private float procedureElapsedTime;

        private string completedTimeStr = "";
        private List<string> completedTimeStrings = new List<string>();

        private Vector3 initHeadPos;
        private Vector3 initLeftHandPos;
        private Vector3 initRightHandPos;

        //private List<int> allIdsForPlay = new List<int> { 1, 2, 3 }; //for extra mode

        private void Awake()
        {
            recorder = this.playbackCommon.gameObject.GetComponent<CookingSupportRecorder>();
            player = this.playbackCommon.gameObject.GetComponent<CookingSupportPlayer>();
            sapi = this.gameObject.GetComponent<VoiceSynthesisExternal>();

            instructionText.text = "右のパネルからレシピを選択してください．";
            headerText.text = "";
            howToPanel.SetActive(false);
            avatarNoticePanel.SetActive(false);
            taskListPanel.SetActive(false);
            stateText.text = "";

            this.databaseManager.expertID = this.expertID;
            this.databaseManager.participantID = this.userID;
            this.databaseManager.participantName = this.userName;

            //ユーザの完了済みタスクを取得して除外
            if (isEndedTaskChecked)
            {
                Task.Run(async () => await CheckFinishedTasks());
            }

            //変数を初期化
            InitializeFields();

            //加工後の材料を置くトレーを取得
            this.processedFoodParent = this.targetObjects.Find(x => x.name == "tray").transform.parent;
        }

        private void Start()
        {
            this.databaseManager.SelectBehaviorTypes();
            behaviorTypeInstructionMap = this.databaseManager.GetBehaviorInstructionMap();
            behaviorTypeMeaningMap = this.databaseManager.GetBehaviorMeaningMap();
            behaviorTypes = this.databaseManager.GetBehaviorTypes();
            processingBehaviorTypes = this.databaseManager.GetProcessTypes();
            graspingBehaviorTypes = this.databaseManager.GetGraspTypes();
            collidingBehaviorTypes = this.databaseManager.GetCollideTypes();
        }

        //使用する変数を初期化
        private void InitializeFields()
        {
            currentStep = 1;
            currentProcedure = 1;

            isGuidanceStarted = false;
            isDuringGuidance = false;
            isBehaviorProceduresSet = false;

            isChoosingBehaviorType = false;
            onRecording = false;
            onPlaying = false;

            isCorrectMotion = false;
            isIncorrectMotion = false;

            incorrectElapsedTime = 0.0f;
            elapsedTimeForDecision = 0.0f;

            totalCorrectTimes = 0;
            maxSimilarity = 0.0f;
            currentError = 1.0f;

            wholeElapsedTime = 0.0f;
            procedureElapsedTime = 0.0f;

            this.ingredientOnBaseMap.Clear();
            this.processedFoodNames.Clear();

            initHeadPos = new Vector3();
            initLeftHandPos = new Vector3();
            initRightHandPos = new Vector3();

            this.playbackCommon.TriggerObjectNames.Clear();

            //対象物体の位置をリセット
            this.playbackCommon.ResetTargetObjectTransforms();

            //材料を消す
            foreach (GameObject obj in this.ingredients)
            {
                obj.SetActive(false);
                //Destroy(obj);
            }
            this.ingredients.Clear();

            //対象物体を初期化
            this.playbackCommon.InitializeTargetObjects();

            //対象物体をセット
            this.targetObjects = this.playbackCommon.GetTargetObjects();
        }

        //private void SetExperimentFields() //for experiment
        //{
        //    if (this.experimentType == 1 || this.experimentType == 2 || this.experimentType == 3)
        //    {
        //        this.existsProcessedFood = true;
        //        this.isUnderWOZDecision = true;

        //        this.isRecordingUserMotion = true;
        //        this.isRecordingElapsedTime = true;
        //        this.isRecordingMotionError = true;

        //        this.databaseManager.participantID = this.experimentType * 100 + this.participantID;
        //    }

        //    if (this.executionMode != ExecutionMode.PlayMode)
        //    {
        //        switch (this.experimentType)
        //        {
        //            case 1:
        //                this.executionMode = ExecutionMode.NoGuidanceMode;
        //                break;
        //            case 2:
        //                this.executionMode = ExecutionMode.AllGuidanceMode;
        //                break;
        //            case 3:
        //                this.executionMode = ExecutionMode.NoGuidanceMode;
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}

        private void Update()
        {
            try
            {
                //把持されている物体を取得
                this.objectIdInLeftHand = this.playbackCommon.GetObjectIdInLeftHand();
                this.objectIdInRightHand = this.playbackCommon.GetObjectIdInRightHand();

                //ガイダンス開始後
                if (this.isGuidanceStarted)
                {
                    //操作練習のときはガイダンス停止
                    if (!this.isInOperationPractice)
                    {
                        switch (this.executionMode)
                        {
                            case ExecutionMode.AllGuidanceMode:
                                OnAllGuidanceMode();
                                break;
                            case ExecutionMode.NeededGuidanceMode:
                                OnNeededGuidanceMode();
                                break;
                            case ExecutionMode.NoGuidanceMode:
                                OnNoGuidanceMode();
                                break;
                            case ExecutionMode.RecordMode:
                                OnRecordMode();
                                break;
                            case ExecutionMode.PlayMode:
                                OnPlayMode();
                                break;
                            case ExecutionMode.ExtraMode:
                                break;
                        }
                    }

                    //ボタンで，行動実演を開始・停止
                    if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.B))
                    {
                        if (!this.player.GetIsPreparing() && !this.player.GetIsNoMotions())
                        {
                            if (!this.player.GetIsPlaying())
                            {
                                Debug.Log("Start playback.");
                                this.incorrectElapsedTime = 0.0f;
                                ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart());
                            }
                            else
                            {
                                Debug.Log("Stop playback.");
                                ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                            }
                        }
                        else
                        {
                            Debug.Log("Cannot play motions.");
                        }
                    }
                }
                //レシピ情報が読み込まれたら，ガイダンス開始を待つ
                else if (this.recipeManager.GetIsRecipeInfoReady())
                {
                    //手の軌道の距離を算出するモード
                    //if (this.executionMode == ExecutionMode.ExtraMode)
                    //{
                    //    base.StartCoroutine(ComputeAllHandDistances());
                    //    return;
                    //}

                    //ボタンで，ガイダンス開始
                    if (OVRInput.GetDown(OVRInput.RawButton.X) || Input.GetKeyDown(KeyCode.X))
                    {
                        //SetExperimentFields(); //for experiment
                        //変数を初期化
                        InitializeFields();

                        StartGuidance();

                        this.isGuidanceStarted = true;
                    }

                    //終了
                    if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.B))
                    {
                        Debug.Log("Application quit.");
                        Application.Quit();
                    }
                }

                //ボタンで，対象物体の位置をリセット
                if (OVRInput.GetDown(OVRInput.RawButton.Y))
                {
                    this.playbackCommon.ResetTargetObjectTransforms();
                }

            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                SIGVerseLogger.Error(exception.Message);
                throw exception;
                //Application.Quit();
            }
        }

        //録画モード
        private void OnRecordMode()
        {
            if (this.recorder.GetIsRecording())
            {
                //経過時間の記録
                this.procedureElapsedTime += Time.deltaTime;
            }

            //行動の種類の選択時，スティックで変更
            if (this.isChoosingBehaviorType)
            {
                //右
                if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    this.selectedTypeIndex++;
                    if (this.selectedTypeIndex >= this.behaviorTypes.Count)
                    {
                        this.selectedTypeIndex = 0;
                    }
                    this.instructionText.text = this.behaviorTypeMeaningMap[this.behaviorTypes[this.selectedTypeIndex]];
                }

                //左
                if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickLeft) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    this.selectedTypeIndex--;
                    if (this.selectedTypeIndex < 0)
                    {
                        this.selectedTypeIndex = this.behaviorTypes.Count - 1;
                    }
                    this.instructionText.text = this.behaviorTypeMeaningMap[this.behaviorTypes[this.selectedTypeIndex]];
                }
            }

            //録画をしていないとき，Xボタン以外の操作
            if (!this.onRecording && !this.isChoosingBehaviorType)
            {
                //ボタンで，ステップ変更
                if (OVRInput.GetDown(OVRInput.RawButton.A) || Input.GetKeyDown(KeyCode.A))
                {
                    //経過時間を蓄積
                    this.completedTimeStrings.Add(this.completedTimeStr);
                    this.completedTimeStr = "";

                    //次のステップがない場合，終了
                    if (this.currentStep >= this.recipeManager.GetStepCount())
                    {
                        //時間を記録
                        if (this.isRecordingElapsedTime)
                        {
                            string filename = RecordingFolder + "/" + this.userID + "_" + this.recipeID + "_time.csv";
                            using (var sw = new StreamWriter(filename, true))
                            {
                                sw.WriteLine(this.recipeID + "," + this.userID + "," + this.userName);
                                for (int i = 0; i < this.completedTimeStrings.Count; i++)
                                {
                                    string time = this.completedTimeStrings[i];
                                    sw.Write(time + ",,");
                                }
                                sw.WriteLine(this.wholeElapsedTime.ToString());
                            }
                        }
                        this.completedTimeStrings.Clear();

                        base.StartCoroutine(ShowNoticeMessagePanelForAvatar("全てのステップが終了しました．", 3.0f));
                        ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStartChoosing());
                        this.instructionText.text = "次のレシピを選択してください．";
                        this.headerText.text = "";
                        this.howToPanel.SetActive(false);

                        //加工後の材料名をレシピ情報に追加
                        if (!this.existsProcessedFood && this.isModelGenerated)
                        {
                            List<string> processedJapaneseName = this.processedFoodNames.Select(x => FindTargetGameObject(x).GetComponentInChildren<TextMesh>().text).ToList();
                            this.databaseManager.InsertProcessedFoods(processedJapaneseName, this.recipeID);
                        }

                        //初期化
                        InitializeFields();

                        this.stateText.text = "";
                    }
                    //次のステップがある場合
                    else
                    {
                        base.StartCoroutine(ShowNoticeMessagePanelForAvatar("次のステップに進んでください．", 2.0f));
                        this.currentStep++;
                        this.currentProcedure = 1;

                        ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToChangeStepColor(this.currentStep));
                        this.headerText.text = "<color=#FF0000>ステップ" + this.currentStep.ToString() + ":</color>";
                        this.instructionText.text = recipeManager.GetCurrentStepText(this.currentStep);
                    }
                }

                //ボタンで，行動をせずに次の手順に移る
                if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.B))
                {
                    this.completedTimeStr += this.procedureElapsedTime.ToString() + ",";
                    this.procedureElapsedTime = 0.0f;

                    BehaviorProcedure oneProcedure = new BehaviorProcedure
                    {
                        graspedObjects = new List<string>(),
                        collidingObjects = new List<string>(),
                        behaviorType = "put"
                    };
                    this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)] = oneProcedure;

                    this.currentProcedure++;

                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar("Next: " + MakeProcedureText(this.currentStep, this.currentProcedure), 1.0f));
                }
            }

            //ボタンで状態変更
            if (OVRInput.GetDown(OVRInput.RawButton.X) || Input.GetKeyDown(KeyCode.X))
            {
                //録画状態時に押した場合，行動選択状態に移る
                if (this.onRecording)
                {
                    if (this.recorder.GetSavedCount() > 100) //行動データが短すぎないように
                    {
                        //経過時間を蓄積
                        this.completedTimeStr += this.procedureElapsedTime.ToString() + ",";
                        this.procedureElapsedTime = 0.0f;

                        //録画を停止
                        ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                        //観測を停止し，把持・接触が行われた物体を抽出
                        ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());

                        base.StartCoroutine(ShowNoticeMessagePanelForAvatar("行った行動の種類を選択してください．", 2.0f));
                        this.howToPanel.SetActive(false);
                        this.headerText.text = "右スティックの左右：変更　　Xボタン：決定";
                        this.instructionText.text = this.behaviorTypeMeaningMap[this.behaviorTypes[this.selectedTypeIndex]];

                        this.isChoosingBehaviorType = true;
                        this.onRecording = false;

                        this.stateText.text = "行動選択中";
                    }
                }
                //行動選択状態時に押した場合，待機状態に移る
                else if (this.isChoosingBehaviorType)
                {
                    //モデル生成を行う
                    if (this.isModelGenerated)
                    {
                        //現在表示されている行動の種類を伝達し，書き込む
                        ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToSave(this.behaviorTypes[this.selectedTypeIndex]));

                        //generatorから受け取った把持・接触物体と選択された行動の種類をモデルに蓄積
                        BehaviorProcedure oneProcedure = new BehaviorProcedure
                        {
                            graspedObjects = new List<string>(this.extractedGraspedText.Split(',')),
                            collidingObjects = new List<string>(this.extractedCollidingText.Split(',')),
                            behaviorType = this.behaviorTypes[this.selectedTypeIndex]
                        };
                        this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)] = oneProcedure;
                    }

                    //把持された物体の位置を変更
                    ChangeGraspedObjectTransform();
                    //材料の見た目を変化
                    ChangeIngredientAppearance();

                    this.howToPanel.SetActive(true);
                    this.headerText.text = "<color=#FF0000>ステップ" + this.currentStep.ToString() + ":</color>";
                    this.instructionText.text = recipeManager.GetCurrentStepText(this.currentStep);

                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(MakeProcedureText(this.currentStep, this.currentProcedure) + "終了．\nXボタンで次の行動を開始します．", 2.0f));

                    this.currentProcedure++;

                    this.isChoosingBehaviorType = false;

                    this.stateText.text = "待機中";
                }
                //待機状態時に押した場合，録画状態に移る
                else
                {
                    //録画を開始
                    ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure, "expert"));
                    //把持・接触の観測を開始
                    ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure));

                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(MakeProcedureText(this.currentStep, this.currentProcedure) + "開始．\nXボタンで現在の行動を終了します．", 2.0f));

                    this.extractedGraspedText = "";
                    this.extractedCollidingText = "";

                    this.onRecording = true;

                    this.stateText.text = "録画中";
                }
            }
        }

        //再生モード
        private void OnPlayMode()
        {
            //再生中は待機
            if (this.player.GetIsPlaying())
            {
                //経過時間の記録
                this.procedureElapsedTime += Time.deltaTime;

                //手順を飛ばす
                if (Input.GetKeyDown(KeyCode.X))
                {
                    this.procedureElapsedTime = 0.0f;
                    this.completedTimeStr += this.procedureElapsedTime.ToString() + ",";

                    ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());

                    ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());

                    if (this.isMotionRecognized)
                    {
                        ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                    }

                    this.currentProcedure++;
                }
            }
            //再生が終了したとき，行動データの準備
            else if (this.onPlaying)
            {
                //経過時間を蓄積
                this.completedTimeStr += this.procedureElapsedTime.ToString() + ",";
                this.procedureElapsedTime = 0.0f;

                //把持・接触の観測を停止し，物体を抽出
                ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());

                //モデル生成を行う場合，SQLに書き込む
                if (this.isModelGenerated)
                {
                    ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToSave("put"));
                }

                //認識停止
                if (this.isMotionRecognized)
                {
                    ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                    ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure));
                }

                //経験者の行動手順モデルに基づいて材料を変化
                if (this.motionTableNameForPlay == PlaybackTableName.expert && !this.isModelGenerated)
                {
                    ChangeIngredientAppearance();
                }

                this.currentProcedure++;

                //行動データを読み込む
                ExecuteEvents.Execute<IRecievePlaySwitch>(
                    this.player.gameObject,
                    null,
                    (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure, Enum.GetName(typeof(PlaybackTableName), this.motionTableNameForPlay))
                );

                this.onPlaying = false;
            }
            //行動データの準備が完了したとき，開始
            else if (!this.player.GetIsPreparing())
            {
                //現在の手順の行動データがない場合
                if (this.player.GetIsNoMotions())
                {
                    //次のステップがない場合
                    if (this.currentStep >= this.recipeManager.GetStepCount())
                    {
                        //経過時間を蓄積
                        this.completedTimeStrings.Add(this.completedTimeStr);
                        this.completedTimeStr = "";

                        //時間を記録
                        if (this.isRecordingElapsedTime)
                        {
                            string filename = RecordingFolder + "/" + this.userID + "_" + this.recipeID + "_time.csv";
                            using (var sw = new StreamWriter(filename, true))
                            {
                                sw.WriteLine(this.recipeID + "," + this.userID + "," + this.userName);
                                for (int i = 0; i < this.completedTimeStrings.Count; i++)
                                {
                                    string time = this.completedTimeStrings[i];
                                    sw.Write(time + ",,");
                                }
                                sw.WriteLine(this.wholeElapsedTime.ToString());
                            }
                        }
                        this.completedTimeStrings.Clear();

                        base.StartCoroutine(ShowNoticeMessagePanelForAvatar("Finish!", 3.0f));
                        ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStartChoosing());
                        this.instructionText.text = "";
                        this.headerText.text = "";
                        this.howToPanel.SetActive(false);

                        //初期化
                        InitializeFields();
                    }
                    //次のステップがある場合
                    else
                    {
                        //経過時間を蓄積
                        this.completedTimeStrings.Add(this.completedTimeStr);
                        this.completedTimeStr = "";

                        this.currentStep++;
                        this.currentProcedure = 1;

                        ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToChangeStepColor(this.currentStep));
                        this.headerText.text = "<color=#FF0000>ステップ" + this.currentStep.ToString() + ":</color>";
                        this.instructionText.text = recipeManager.GetCurrentStepText(this.currentStep);

                        //行動データを読み込む
                        ExecuteEvents.Execute<IRecievePlaySwitch>(
                            this.player.gameObject,
                            null,
                            (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure, Enum.GetName(typeof(PlaybackTableName), this.motionTableNameForPlay))
                        );
                    }
                }
                //現在の手順の行動データがある場合
                else
                {
                    //再生開始
                    ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart());

                    //把持・接触の観測を開始
                    ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure));

                    //認識開始
                    if (this.isMotionRecognized)
                    {
                        ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(IsRecognitionTargetsBothHands()));
                    }

                    this.onPlaying = true;
                }
            }
        }

        //実験用モード（レシピのみ）
        private void OnNoGuidanceMode()
        {
            this.wholeElapsedTime += Time.deltaTime;
            this.procedureElapsedTime += Time.deltaTime;

            //ボタンで，次のステップに進む
            if (OVRInput.GetDown(OVRInput.RawButton.X) || Input.GetKeyDown(KeyCode.X))
            {
                //録画停止
                if (this.isRecordingUserMotion)
                {
                    ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                }

                //観測停止
                ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());

                //時間記録
                this.completedTimeStrings.Add(this.completedTimeStr);
                this.completedTimeStr = "";

                //次のステップがない場合
                if (this.currentStep >= this.recipeManager.GetStepCount())
                {
                    //経過時間保存
                    string filename = RecordingFolder + "/elapsed_time.csv";
                    using (var sw = new StreamWriter(filename, true))
                    {
                        sw.WriteLine(this.recipeID + "," + this.userID + "," + this.userName);
                        for (int i = 0; i < this.completedTimeStrings.Count; i++)
                        {
                            string time = this.completedTimeStrings[i];
                            sw.Write(time + ",,");
                        }
                        sw.WriteLine(this.wholeElapsedTime.ToString());
                    }
                    this.completedTimeStrings.Clear();

                    string message = "全てのステップが終了しました．";
                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                    ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));

                    ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStartChoosing());
                    this.instructionText.text = "";
                    this.headerText.text = "";
                    this.howToPanel.SetActive(false);

                    //初期化
                    InitializeFields();
                }
                //次のステップがある場合
                else
                {
                    this.currentStep++;
                    this.currentProcedure = 1;

                    string message = "続いて，ステップ" + this.currentStep.ToString() + "の作業をしてください．";
                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                    ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));

                    ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToChangeStepColor(this.currentStep));
                    this.headerText.text = "<color=#FF0000>ステップ" + this.currentStep.ToString() + ":</color>";
                    this.instructionText.text = recipeManager.GetCurrentStepText(this.currentStep);
                }
            }

            //OKボタンを押したら
            if (this.isCorrectMotion || OVRInput.GetDown(OVRInput.RawButton.A))
            {
                this.isDuringGuidance = false;
                //録画停止
                if (this.isRecordingUserMotion)
                {
                    ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                }
                //観測停止
                ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());

                //時間記録
                this.completedTimeStr += this.procedureElapsedTime.ToString() + ",";
                this.procedureElapsedTime = 0.0f;

                string message = "次の作業をしてください．";
                base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 1.0f));

                BehaviorProcedure oneProcedure = new BehaviorProcedure
                {
                    graspedObjects = new List<string>(this.extractedGraspedText.Split(',')),
                    collidingObjects = new List<string>(this.extractedCollidingText.Split(',')),
                    behaviorType = "put"
                };
                this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)] = oneProcedure;

                //対象物体の位置と見た目を変更
                ChangeGraspedObjectTransform();
                ChangeIngredientAppearance();

                this.currentProcedure++;

                this.isCorrectMotion = false;
            }

            //停止後，行動データの保存とモデルの抽出が完了してから，開始
            if (!this.isDuringGuidance)
            {
                if (!this.recorder.GetIsWriting() && !this.generator.GetIsExtracting())
                {
                    //録画開始
                    if (this.isRecordingUserMotion)
                    {
                        ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure, "user"));
                    }
                    //観測開始
                    ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure));

                    this.isDuringGuidance = true;

                    Debug.Log("start");
                }
            }
        }

        //必要時ガイダンスモード
        private void OnNeededGuidanceMode()
        {
            //再生が実行されていないとき
            if (!this.player.GetIsPlaying())
            {
                this.wholeElapsedTime += Time.deltaTime;
                this.procedureElapsedTime += Time.deltaTime;

                this.incorrectElapsedTime += Time.deltaTime;

                //正しい把持・接触状態の場合，１加算
                if (IsGraspingCollidingStateCorrect() && !this.isUnderWOZDecision)
                {
                    this.totalCorrectTimes++;
                    this.incorrectElapsedTime = 0.0f;

                    //正しい状態になったときの初期位置
                    if(this.totalCorrectTimes == 1)
                    {
                        this.initLeftHandPos = this.playbackCommon.GetLeftHand().position;
                        this.initRightHandPos = this.playbackCommon.GetRightHand().position;
                    }
                }

                //一定時間正しい行動にならない場合，行動実演
                if (this.incorrectElapsedTime >= IncorrectDecisionTime || this.isIncorrectMotion)
                {
                    AdviseCorrectBehavior();
                    this.incorrectElapsedTime = 0.0f;
                    this.isIncorrectMotion = false;
                }

                //正しい行動の閾値を超えた場合，次の手順へ進む
                if (this.totalCorrectTimes >= CorrectDecisionCount || this.isCorrectMotion)
                {
                    if (!this.isMotionRecognized || this.maxSimilarity >= MotionSimilarityThres)
                    {
                        this.isCorrectMotion = false;

                        //録画停止
                        if (this.isRecordingUserMotion)
                        {
                            ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                        }
                        //観測停止
                        //ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnStopObserving());
                        //認識停止
                        if (this.isMotionRecognized)
                        {
                            ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                        }

                        this.isDuringGuidance = false;

                        //次に進む
                        base.StartCoroutine(ShowNoticeMessagePanelForAvatar("OK!", 1.0f));
                        base.StartCoroutine(DelayMethod(1.0f, () => GoToNextProcedure()));

                        this.incorrectElapsedTime = 0.0f;
                        this.totalCorrectTimes = 0;
                        this.maxSimilarity = 0.0f;
                        this.isIncorrectMotion = false;
                    }
                }
            }

            //行動データの保存が完了してから，ガイダンス開始
            if (!this.isDuringGuidance)
            {
                if (!this.recorder.GetIsWriting() && !this.generator.GetIsExtracting())
                {
                    //録画開始
                    if (this.isRecordingUserMotion)
                    {
                        ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure, "user"));
                    }
                    //観測開始
                    //ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnStartObserving(this.recipeID, this.currentStep, this.currentProcedure));
                    //認識対象が片手か両手かを伝達し，認識開始
                    if (this.isMotionRecognized)
                    {
                        ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(IsRecognitionTargetsBothHands()));
                    }

                    this.isDuringGuidance = true;
                }
            }

            //for experiment
            this.remainingText.text = ((int)IncorrectDecisionTime - (int)incorrectElapsedTime).ToString() + "[s]";
        }

        //常時ガイダンスモード
        private void OnAllGuidanceMode()
        {
            //ガイダンス中
            if (this.isDuringGuidance)
            {
                //再生が実行されていないとき
                if (!this.player.GetIsPlaying())
                {
                    this.wholeElapsedTime += Time.deltaTime;
                    this.procedureElapsedTime += Time.deltaTime;

                    this.incorrectElapsedTime += Time.deltaTime;

                    //正しい把持・接触状態の場合，１加算
                    if (IsGraspingCollidingStateCorrect() && !this.isUnderWOZDecision)
                    {
                        this.totalCorrectTimes++;
                        this.incorrectElapsedTime = 0.0f;

                        //正しい状態になったときの初期位置
                        if (this.totalCorrectTimes == 1)
                        {
                            this.initLeftHandPos = this.playbackCommon.GetLeftHand().position;
                            this.initRightHandPos = this.playbackCommon.GetRightHand().position;
                        }
                    }

                    //一定時間正しい行動にならない場合，行動実演
                    if (this.incorrectElapsedTime >= IncorrectDecisionTime || this.isIncorrectMotion)
                    {
                        AdviseCorrectBehavior();
                        this.incorrectElapsedTime = 0.0f;
                        this.isIncorrectMotion = false;
                    }

                    //正しい行動の閾値を超えた場合，次の手順へ進む
                    if (this.totalCorrectTimes >= CorrectDecisionCount || this.isCorrectMotion)
                    {
                        if (!this.isMotionRecognized || this.maxSimilarity >= MotionSimilarityThres)
                        {
                            this.isCorrectMotion = false;

                            //録画停止
                            if (this.isRecordingUserMotion)
                            {
                                ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                            }
                            //観測停止
                            //ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnStopObserving());
                            //認識停止
                            if (this.isMotionRecognized)
                            {
                                ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStop());
                            }

                            //次に進む
                            base.StartCoroutine(ShowNoticeMessagePanelForAvatar("OK!", 1.0f));
                            base.StartCoroutine(DelayMethod(1.0f, () => GoToNextProcedure()));

                            this.incorrectElapsedTime = 0.0f;
                            this.totalCorrectTimes = 0;
                            this.maxSimilarity = 0.0f;
                            this.isIncorrectMotion = false;

                            this.isDuringGuidance = false;
                        }
                    }
                }

                //for experiment
                this.remainingText.text = ((int)IncorrectDecisionTime - (int)incorrectElapsedTime).ToString() + "[s]";
            }
            //ガイダンス待機中
            else
            {
                //ステップの最初は，ステップ文を見せる
                if (this.currentProcedure == 1)
                {
                    //行動手順モデルの読み込みが終わった場合
                    if (this.isBehaviorProceduresSet)
                    {
                        //ボタンを押すか，頭を一定距離動かしたとき
                        if (OVRInput.GetDown(OVRInput.RawButton.X) || Input.GetKeyDown(KeyCode.X) || Vector3.Distance(this.playbackCommon.GetHead().position, this.initHeadPos) >= 0.5)
                        {
                            this.isDuringGuidance = true;

                            //教示文章を伝達
                            ShowCurrentProcedureInstruction();

                            this.howToPanel.SetActive(false);

                            this.totalCorrectTimes = 0;
                            this.maxSimilarity = 0.0f;
                            this.isCorrectMotion = false;
                            this.isIncorrectMotion = false;

                            //録画開始
                            if (this.isRecordingUserMotion)
                            {
                                ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure, "user"));
                            }
                            //観測開始
                            //ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnStartObserving(this.recipeID, this.currentStep, this.currentProcedure));
                            //認識対象が片手か両手かを伝達し，認識開始
                            if (this.isMotionRecognized)
                            {
                                ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(IsRecognitionTargetsBothHands()));
                            }
                        }
                    }
                }
                //ステップの最初以外は，行動データの保存が完了してから，ガイダンス開始
                else
                {
                    if (!this.recorder.GetIsWriting() && !this.generator.GetIsExtracting())
                    {
                        //録画開始
                        if (this.isRecordingUserMotion)
                        {
                            ExecuteEvents.Execute<IRecieveRecordSwitch>(this.recorder.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(this.recipeID, this.currentStep, this.currentProcedure, "user"));
                        }
                        //観測開始
                        //ExecuteEvents.Execute<IRecieveGenerationSwitch>(this.generator.gameObject, null, (reciever, eventData) => reciever.OnStartObserving(this.recipeID, this.currentStep, this.currentProcedure));
                        //認識対象が片手か両手かを伝達し，認識開始
                        if (this.isMotionRecognized)
                        {
                            ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart(IsRecognitionTargetsBothHands()));
                        }

                        this.isDuringGuidance = true;
                    }
                }
            }
        }

        //利用者の把持・接触行動が正しいどうか判定
        private bool IsGraspingCollidingStateCorrect()
        {
            this.elapsedTimeForDecision += Time.deltaTime;

            //一定周期で判定を行う
            if (this.elapsedTimeForDecision <= DecisionIntervalTime)
            {
                return false;
            }

            string correctBehaviorType = this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].behaviorType;
            List<string> correctGraspedObjects = new List<string>(this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].graspedObjects);
            List<string> correctCollidingObjects = new List<string>(this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].collidingObjects);

            //ない行動の種類の場合，リターン
            if (!this.behaviorTypes.Contains(correctBehaviorType))
            {
                Debug.LogError("this behavior type does not exist: " + correctBehaviorType);
                return false;
            }

            //間違った物体を持っていた場合，すぐに間違い判定（行動実演）
            if (this.isIncorrectOnWrongGrasp)
            {
                if (this.objectIdInLeftHand != "" && !correctGraspedObjects.Contains(this.objectIdInLeftHand))
                {
                    this.isIncorrectMotion = true;
                }
                if (this.objectIdInRightHand != "" && !correctGraspedObjects.Contains(this.objectIdInRightHand))
                {
                    this.isIncorrectMotion = true;
                }
            }

            bool isAllOk = false;
            bool isGraspOk = false;
            bool isCollideOk = false;

            //把持されている物体を判定
            List<string> nowGraspingObjects = new List<string>() { this.objectIdInLeftHand, this.objectIdInRightHand };
            if (correctGraspedObjects.Count != 0)
            {
                string correctOne = FindTargetGameObject(correctGraspedObjects[0]).name;
                //片手
                if (correctGraspedObjects.Count == 1 && nowGraspingObjects.Contains(correctOne))
                {
                    isGraspOk = true;
                }
                //両手
                else if (correctGraspedObjects.Count == 2)
                {
                    string correctTheOther = FindTargetGameObject(correctGraspedObjects[1]).name;
                    if (nowGraspingObjects.Contains(correctOne) && nowGraspingObjects.Contains(correctTheOther))
                    {
                        isGraspOk = true;
                    }
                }
            }

            //接触している物体を判定
            if (correctCollidingObjects.Count >= 2)
            {
                //手は物体でないので入れ替え
                if (correctCollidingObjects[0] == "EthanLeftHand" || correctCollidingObjects[0] == "EthanRightHand")
                {
                    string hand = correctCollidingObjects[0];
                    correctCollidingObjects.Remove(hand);
                    correctCollidingObjects.Add(hand);
                }

                string targetString = correctCollidingObjects[0];
                //接触元の対象物体を探索
                GameObject targetObj = FindTargetGameObject(targetString);
                //対象物体が接触している物体を取り出す
                List<string> nowCollidingObjects = new List<string>(targetObj.GetComponent<CollisionChecker>().GetCollidingObjects());

                //接触先の対象物体を判定
                for (int index = 1; index < correctCollidingObjects.Count; index++)
                {
                    string opponentString;
                    if (correctCollidingObjects[index] == "EthanLeftHand" || correctCollidingObjects[index] == "EthanRightHand")
                    {
                        opponentString = correctCollidingObjects[index];
                    }
                    else
                    {
                        opponentString = FindTargetGameObject(correctCollidingObjects[index]).name;
                    }

                    if (nowCollidingObjects.Contains(opponentString))
                    {
                        isCollideOk = true;
                    }
                }
            }

            //初期位置から一定の距離を動いたか判定
            bool isHandMoving = false;
            if (this.totalCorrectTimes >= 1)
            {
                if (Vector3.Distance(this.playbackCommon.GetLeftHand().position, this.initLeftHandPos) >= MovingDistanceThres)
                {
                    isHandMoving = true;
                }
                if (Vector3.Distance(this.playbackCommon.GetRightHand().position, this.initRightHandPos) >= MovingDistanceThres)
                {
                    isHandMoving = true;
                }
            }

            //把持が対象の行動か，接触が対象の行動かによって正しい行動を判定
            if (this.graspingBehaviorTypes.Contains(correctBehaviorType) && this.collidingBehaviorTypes.Contains(correctBehaviorType))
            {
                if (isGraspOk && isCollideOk)
                {
                    isAllOk = true;
                }
            }
            else if (this.graspingBehaviorTypes.Contains(correctBehaviorType))
            {
                if (isGraspOk)
                {
                    isAllOk = true;
                    //把持するだけの場合，動いた後に正しいと判定
                    if (isHandMoving)
                    {
                        this.isCorrectMotion = true;
                    }
                }
            }
            else if (this.collidingBehaviorTypes.Contains(correctBehaviorType))
            {
                if (isCollideOk)
                {
                    isAllOk = true;
                }
            }
            else
            {
                isAllOk = true;
            }

            this.elapsedTimeForDecision = 0.0f;
            return isAllOk;
        }

        //間違った行動をした場合に行動の実演と教示文章の伝達を行う
        private void AdviseCorrectBehavior()
        {
            if (this.executionMode == ExecutionMode.NeededGuidanceMode)
            {
                string message = "正しい作業を教えます！";
                base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 2.0f));
                base.StartCoroutine(DelayMethod(3.0f, () => ShowCurrentProcedureInstruction()));
                base.StartCoroutine(DelayMethod(4.0f, () => ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart())));
            }

            if(this.executionMode == ExecutionMode.AllGuidanceMode)
            {
                string message = "模範の動作を実演します！\n少し後ろに下がって見てください．";
                base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));
                base.StartCoroutine(DelayMethod(3.0f, () => ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStart())));
            }
        }

        //教示パネルに現在の手順の教示文章を表示
        private void ShowCurrentProcedureInstruction()
        {
            string correctBehaviorType = this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].behaviorType;
            List<string> correctGraspedObjects = new List<string>(this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].graspedObjects);
            List<string> correctCollidingObjects = new List<string>(this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].collidingObjects);

            Debug.Log("correct objects to grasp: " + string.Join(",", correctGraspedObjects));
            Debug.Log("correct objects to collide: " + string.Join(",", correctCollidingObjects));

            //ヘッダー文章を変更
            if (this.executionMode == ExecutionMode.NeededGuidanceMode)
            {
                this.headerText.text = "現在行う作業";
            }
            if (this.executionMode == ExecutionMode.AllGuidanceMode)
            {
                this.headerText.text = "手順" + MakeProcedureText(this.currentStep, this.currentProcedure);
            }

            string explanationStr = "";
            try
            {
                //把持が対象の行動の場合，把持物体についての説明文を作成
                if (this.graspingBehaviorTypes.Contains(correctBehaviorType))
                {
                    if (correctGraspedObjects.Count != 0)
                    {
                        //片手のみの場合
                        if (correctGraspedObjects.Count == 1)
                        {
                            explanationStr += "'" + FindTargetGameObject(correctGraspedObjects[0]).GetComponentInChildren<TextMesh>(true).text + "'を";
                        }
                        else
                        {
                            //両手に同じ物体が把持されている場合
                            if (correctGraspedObjects[0] == correctGraspedObjects[1])
                            {
                                explanationStr += "'" + FindTargetGameObject(correctGraspedObjects[0]).GetComponentInChildren<TextMesh>(true).text + "'を交互に";
                            }
                            //異なる物体の場合
                            else
                            {
                                explanationStr += "'" + FindTargetGameObject(correctGraspedObjects[0]).GetComponentInChildren<TextMesh>(true).text + "'と'" + FindTargetGameObject(correctGraspedObjects[1]).GetComponentInChildren<TextMesh>(true).text + "'を";
                            }
                        }

                        explanationStr += "持って，\n";
                    }
                }

                //接触が対象の行動の場合，接触物体についての説明文を作成
                if (this.collidingBehaviorTypes.Contains(correctBehaviorType))
                {
                    if (correctCollidingObjects.Count >= 2)
                    {
                        //両手の場合，片手は無視
                        if(correctCollidingObjects.Contains("EthanLeftHand") && correctCollidingObjects.Contains("EthanRightHand"))
                        {
                            correctCollidingObjects.Remove("EthanLeftHand");
                        }

                        for (int index = 0; index < correctCollidingObjects.Count; index++)
                        {
                            string objectWord = "";
                            //手の場合
                            if (correctCollidingObjects[index] == "EthanLeftHand" || correctCollidingObjects[index] == "EthanRightHand")
                            {
                                if (correctGraspedObjects.Count != 0)
                                {
                                    objectWord = "'もう片方の手'";
                                }
                                else
                                {
                                    objectWord = "'手'";
                                }
                            }
                            //物体の場合
                            else
                            {
                                objectWord = "'" + FindTargetGameObject(correctCollidingObjects[index]).GetComponentInChildren<TextMesh>(true).text + "'";
                            }

                            if (index == 0)
                            {
                                explanationStr += objectWord + "を";
                            }
                            else if (index == correctCollidingObjects.Count - 1)
                            {
                                explanationStr += objectWord + "に";
                            }
                            //置くの場合，”～と”は省く
                            else if (!correctBehaviorType.Contains("put"))
                            {
                                explanationStr += objectWord + "と";
                            }
                        }

                        //置くの場合，”当てて”は省く
                        if (!correctBehaviorType.Contains("put"))
                        {
                            explanationStr += "当てて，\n";
                        }
                    }
                }

                //行動の種類についての説明文を作成
                explanationStr += this.behaviorTypeInstructionMap[correctBehaviorType] + "ください";

                //教示パネルに表示
                this.instructionText.text = explanationStr;
                //音声合成用に変換
                string speech = explanationStr.Replace("-", "の").Replace("'", "");
                ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(speech));
            }
            catch(Exception exception)
            {
                this.instructionText.text = string.Join(",", correctCollidingObjects);
                Debug.LogError(exception.Message);
            }
        }

        //ガイダンス開始時に行う処理
        private void StartGuidance()
        {
            Debug.Log("Start guidance.");
            //ガイダンスを行うレシピIDを取得
            this.recipeID = recipeManager.GetCurrentRecipeID();

            //モデル生成を行うとき以外，行動手順モデルを読み込む
            if (((this.executionMode == ExecutionMode.RecordMode || this.executionMode == ExecutionMode.PlayMode) && this.isModelGenerated)
                || this.executionMode == ExecutionMode.NoGuidanceMode)
            {
                this.behaviorProceduresMap.Clear();
                //Debug.Log("No model now.");
            }
            else
            {
                this.isBehaviorProceduresSet = false;
                Task.Run(async () => await LoadBehaviorProcedures());
            }
            
            //経験者の行動データを読み込む
            ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure, "expert"));
            //経験者の手の動作データの読み込み
            if (this.isMotionRecognized)
            {
                ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure));
            }

            //対象物体の位置をリセット
            this.playbackCommon.ResetTargetObjectTransforms();
            //材料モデルを生成
            GenerateIngredients();

            string message = "";
            switch (this.executionMode)
            {
                case ExecutionMode.RecordMode:
                    message = "行動の一覧から1つの行動を選んで，\nXボタンで行動を開始してください．";
                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                    this.howToText.text = "Xボタン：行動を開始／終了\nAボタン：次のステップへ進む";
                    this.taskListText.text = string.Join(", ", this.behaviorTypeMeaningMap.Values);
                    this.taskListPanel.SetActive(true);
                    break;

                case ExecutionMode.PlayMode:
                    this.currentStep = this.beginningStepForPlay;
                    message = "開始します．";
                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 2.0f));
                    this.howToPanel.SetActive(false);
                    //設定したテーブルの行動データを準備
                    ExecuteEvents.Execute<IRecievePlaySwitch>(
                        this.player.gameObject,
                        null,
                        (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure, Enum.GetName(typeof(PlaybackTableName), this.motionTableNameForPlay))
                    );
                    break;

                case ExecutionMode.NoGuidanceMode:
                    //操作の説明
                    this.howToText.text = "Xボタン：現在のステップにおける全ての作業が終了";
                    message = "開始します．\n始めに，ステップ1の作業をしてください．";
                    //アバタの前に表示
                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                    //音声合成
                    ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));
                    break;

                case ExecutionMode.NeededGuidanceMode:
                    this.howToText.text = "作業を間違えたとき，正しい作業を教えます．";
                    message = "開始します！\n始めに，ステップ1の作業をしましょう．";
                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                    ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));
                    break;

                case ExecutionMode.AllGuidanceMode:
                    this.howToText.text = "Xボタンを押すか，左スティックを動かすと\nこのステップのガイダンスが開始されます．";
                    message = "開始します！\n\n最初のステップを読んだ後，Xボタンを押してください．";
                    base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                    ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));
                    //開始時に教示文章とともに行動実演
                    if (this.isMotionPlayedFirstly)
                    {
                        this.incorrectElapsedTime = IncorrectDecisionTime - 5.0f;
                    }
                    break;
            }

            //recipeManagerでレシピの変更をできなくする
            ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStopChoosing());
            //recipeManagerで現在のステップ文の色を赤くする
            ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToChangeStepColor(this.currentStep));

            //教示パネルにステップ文を表示
            this.headerText.text = "<color=#FF0000>ステップ" + this.currentStep.ToString() + ":</color>";
            this.instructionText.text = this.recipeManager.GetCurrentStepText(this.currentStep);
            this.howToPanel.SetActive(true); //操作方法パネル

            this.incorrectElapsedTime = 0.0f;
            this.initHeadPos = this.playbackCommon.GetHead().position;

            this.isGuidanceStarted = true;
        }

        //次の手順に進む際に行う処理
        private void GoToNextProcedure()
        {
            //経過時間の蓄積
            this.completedTimeStr += this.procedureElapsedTime.ToString() + ",";
            this.procedureElapsedTime = 0.0f;

            //把持された物体の初期位置を変更
            ChangeGraspedObjectTransform();
            //材料の見た目と衝突判定を変化
            ChangeIngredientAppearance();

            this.incorrectElapsedTime = 0.0f;

            //行動手順モデルの次の手順がある場合
            if (this.behaviorProceduresMap.ContainsKey(MakeProcedureText(this.currentStep, this.currentProcedure + 1)))
            {
                this.currentProcedure++;

                //経験者の行動データを読み込む
                ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure, "expert"));

                //認識用動作データの読み込み
                if (this.isMotionRecognized)
                {
                    ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure));
                }

                //必要時ガイダンスモードはレシピのステップ文を見せる
                if (this.executionMode == ExecutionMode.NeededGuidanceMode)
                {
                    string message = "次の作業をしましょう．";
                    ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));

                    this.headerText.text = "<color=#FF0000>ステップ" + this.currentStep.ToString() + ":</color>";
                    this.instructionText.text = this.recipeManager.GetCurrentStepText(this.currentStep);
                }
                //常時ガイダンスモードは行動手順の教示文章を見せる
                if (this.executionMode == ExecutionMode.AllGuidanceMode)
                {
                    //教示文章を見せる
                    ShowCurrentProcedureInstruction();

                    //始めに行動実演をするとき
                    if (this.isMotionPlayedFirstly)
                    {
                        this.incorrectElapsedTime = IncorrectDecisionTime - 5.0f;
                    }
                }

                Debug.Log("current procedure: " + MakeProcedureText(this.currentStep, this.currentProcedure));
            }
            //次の手順がなく，次のステップがある場合
            else if (this.behaviorProceduresMap.ContainsKey(MakeProcedureText(this.currentStep + 1, 1)))
            {
                //ステップの経過時間を格納
                this.completedTimeStrings.Add(this.completedTimeStr);
                this.completedTimeStr = "";

                this.currentStep++;
                this.currentProcedure = 1;

                string message = "次のステップに進みましょう！";
                base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 3.0f));
                ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));

                //レシピパネルのステップ文の色を切替
                ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToChangeStepColor(this.currentStep));

                this.headerText.text = "<color=#FF0000>ステップ" + this.currentStep.ToString() + ":</color>";
                this.instructionText.text = this.recipeManager.GetCurrentStepText(this.currentStep);
                this.howToPanel.SetActive(true);

                this.isDuringGuidance = false; //for all guidance mode

                //経験者の行動データを読み込む
                ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure, "expert"));

                //認識用動作データの読み込み
                if (this.isMotionRecognized)
                {
                    ExecuteEvents.Execute<IRecieveRecognitionSwitch>(this.recognizer.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, this.currentStep, this.currentProcedure));
                }

                Debug.Log("current procedure: " + MakeProcedureText(this.currentStep, this.currentProcedure));
            }
            //全てのステップを終えた場合
            else
            {
                this.isGuidanceStarted = false;

                this.completedTimeStrings.Add(this.completedTimeStr);
                this.completedTimeStr = "";
                //全体の経過時間を保存
                if (isRecordingElapsedTime)
                {
                    //string currentTimeStr = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string filename = RecordingFolder + "/elapsed_time.csv";
                    using (var sw = new StreamWriter(filename, true))
                    {
                        sw.WriteLine(this.recipeID + "," + this.userID + "," + this.userName);
                        for (int i = 0; i < this.completedTimeStrings.Count; i++)
                        {
                            string time = this.completedTimeStrings[i];
                            sw.Write(time + ",");
                        }
                        sw.WriteLine(this.wholeElapsedTime.ToString());
                    }
                }
                this.completedTimeStrings.Clear();

                string message = "出来上がり！\n全てのステップの作業が終了しました．";
                base.StartCoroutine(ShowNoticeMessagePanelForAvatar(message, 2.0f));
                ExecuteEvents.Execute<IReceiveTTSHandler>(this.sapi.gameObject, null, (reciever, eventData) => reciever.OnReceiveTTSMessage(message));

                this.instructionText.text = "別のレシピを体験する場合は，\n右のパネルから選択してください．\n\n終了する場合は，Bボタンを押してください．";
                this.headerText.text = "";
                this.howToPanel.SetActive(false);

                //終えたレシピをユーザ情報に記録
                if (this.isEndedTaskChecked)
                {
                    this.databaseManager.InsertFinishedTasks(this.userName, this.recipeID);
                }

                //recipeManagerでレシピの選択ができるようにする
                ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStartChoosing());

                //変数を初期化
                InitializeFields();

                Debug.Log("Finished all procedures.");
            }

            this.initHeadPos = this.playbackCommon.GetHead().position;
        }

        //行動手順モデルをMySQLから読み込む
        private async Task LoadBehaviorProcedures()
        {
            this.behaviorProceduresMap.Clear();

            await Task.Run(() => this.databaseManager.SelectBehaviorProcedures(this.recipeID, this.expertID));

            List<string> behaviorProcedures = this.databaseManager.GetBehaviorProcedures();
            foreach(string procedureStr in behaviorProcedures)
            {
                string[] data = procedureStr.Split('\t');
                BehaviorProcedure procedure = new BehaviorProcedure
                {
                    graspedObjects = new List<string>(data[2].Split(',')),
                    collidingObjects = new List<string>(data[3].Split(',')),
                    behaviorType = data[4]
                };
                this.behaviorProceduresMap[MakeProcedureText(int.Parse(data[0]), int.Parse(data[1]))] = procedure;
            }

            if(this.behaviorProceduresMap.Count == 0)
            {
                Debug.LogError("No model");
            }

            this.isBehaviorProceduresSet = true;
        }

        //レシピに書かれた材料のモデルを生成
        private void GenerateIngredients()
        {
            this.ingredients.Clear();

            List<string> ingredientStrings = new List<string>(this.databaseManager.GetRecipeIngredients());

            //原材料をテーブル上に生成
            float intervalPos = 0.20f;
            float sizeX = 1.0f;
            float sizeZ = 0.6f;
            float xInterval = 5;
            for (int i = 0; i < ingredientStrings.Count; i++)
            {
                //等間隔で配置
                float objX = -sizeX / 2 + intervalPos * (int)(i % xInterval);
                float objZ = -sizeZ / 2 + intervalPos * (int)(i / xInterval);
                Vector3 initPos = new Vector3(objX, 0.8f, objZ);

                GameObject ingredient = Instantiate(this.ingredientPrefab, this.ingredientParent) as GameObject; //オブジェクト生成
                ingredient.transform.localPosition = initPos; //位置
                ingredient.transform.localEulerAngles = new Vector3(0f, -30f, 0f); //姿勢
                ingredient.name = IngredientHeader + (i + 1).ToString(); //名称
                ingredient.GetComponentInChildren<TextMesh>(true).text = ingredientStrings[i]; //表示名

                this.ingredients.Add(ingredient);
            }

            //加工後の材料をトレイ上に生成
            if (this.existsProcessedFood)
            {
                List<string> processedFoodStrings = new List<string>(this.databaseManager.GetProcessedFoodString(this.recipeID).Split(','));

                sizeX = 0.6f;
                sizeZ = 0.2f;
                xInterval = 4;
                for (int i = 0; i < processedFoodStrings.Count; i++)
                {
                    float objX = -sizeX / 2 + intervalPos * (int)(i % xInterval);
                    float objZ = -sizeZ / 2 + intervalPos * (int)(i / xInterval);
                    Vector3 initPos = new Vector3(objX, 0.04f, objZ);

                    GameObject processedFood = Instantiate(this.ingredientPrefab, this.processedFoodParent) as GameObject;
                    processedFood.transform.localPosition = initPos;
                    processedFood.name = ProcessedFoodHeader + (i + 1).ToString();
                    processedFood.GetComponentInChildren<TextMesh>(true).text = processedFoodStrings[i];
                    processedFood.GetComponentInChildren<MeshRenderer>().material = this.processedMaterial;

                    this.ingredients.Add(processedFood);
                    this.processedFoodNames.Add(processedFood.name);
                }
            }

            //材料が追加された対象物体を更新
            this.playbackCommon.InitializeTargetObjects();
            this.targetObjects = this.playbackCommon.GetTargetObjects();
        }

        //材料モデルを実行された行動によって変化させる
        private void ChangeIngredientAppearance()
        {
            List<GameObject> targetIngredients = new List<GameObject>();
            string targetBase = "";

            List<string> targetObjectNames = this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].graspedObjects
                .Concat(this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].collidingObjects).Distinct().ToList();
            string currentMotionType = this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].behaviorType;

            //行動手順から材料名を取り出す
            foreach (string targetStr in targetObjectNames)
            {
                //材料を探索
                if (targetStr.Contains(IngredientHeader) || targetStr.Contains(ProcessedFoodHeader))
                {
                    targetIngredients.Add(FindTargetGameObject(targetStr));
                }

                //材料が置かれている台を探索
                if (this.playbackCommon.GetTargetBases().Exists(x => x.name == targetStr))
                {
                    targetBase = targetStr;
                }
            }

            //対象の台に置かれている材料を追加
            foreach (string ingredientStr in this.ingredientOnBaseMap.Keys)
            {
                if (this.ingredientOnBaseMap[ingredientStr] == targetBase && !targetIngredients.Exists(x => x.name == ingredientStr))
                {
                    targetIngredients.Add(FindTargetGameObject(ingredientStr));
                }
            }

            //置くか加工する行動の場合
            if (currentMotionType.Contains("put") || this.processingBehaviorTypes.Contains(currentMotionType))
            {
                //置かれた材料を変化させる
                for (int index = 0; index < targetIngredients.Count; index++)
                {
                    //１つの材料の衝突判定を変更
                    if (index == 0)
                    {
                        //置く行動をした場合
                        if (currentMotionType.Contains("put"))
                        {
                            targetIngredients[index].GetComponent<NVRInteractableItem>().enabled = false; //把持できなくする
                            targetIngredients[index].GetComponent<Rigidbody>().useGravity = false; // 重力をなくす
                            targetIngredients[index].GetComponent<MeshCollider>().isTrigger = true; // 衝突判定をなくす（トリガー状態）
                            targetIngredients[index].GetComponent<Rigidbody>().velocity = Vector3.zero; //慣性をなくす
                            targetIngredients[index].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

                            //対象の台に材料が複数ある場合，名前を変更
                            if (targetIngredients.Count >= 2)
                            {
                                targetIngredients[index].GetComponentInChildren<TextMesh>().text = "材料";
                            }

                            ////表示名を消す
                            //targetIngredients[index].transform.Find("ExplanationText").gameObject.SetActive(false);

                            //トリガー状態の物体名に追加
                            this.playbackCommon.TriggerObjectNames.Add(targetIngredients[index].name);

                            //初期トランスフォームを変更
                            this.playbackCommon.SetInitialTransform(targetIngredients[index].transform);
                            //材料：乗っている台のマップに追加
                            this.ingredientOnBaseMap[targetIngredients[index].name] = targetBase;
                        }

                        //加工する行動をした場合
                        else if (this.processingBehaviorTypes.Contains(currentMotionType))
                        {
                            //加工後の材料がない場合
                            if (!this.existsProcessedFood)
                            {
                                //材料を加工後のものへ変更
                                targetIngredients[index].GetComponent<NVRInteractableItem>().enabled = true; // 把持可能にする
                                targetIngredients[index].GetComponent<Rigidbody>().useGravity = true; //重力を戻す
                                targetIngredients[index].GetComponent<MeshCollider>().isTrigger = false; // 衝突判定を戻す

                                targetIngredients[index].GetComponent<MeshRenderer>().material = this.processedMaterial; //加工後の色にする
                                targetIngredients[index].transform.Find("ExplanationText").gameObject.SetActive(true); //表示名を表示

                                //新しい表示名を作成
                                string motionWord = behaviorTypeInstructionMap[this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].behaviorType];
                                string ingredientWord = targetIngredients[index].GetComponentInChildren<TextMesh>().text;
                                if (ingredientWord.Contains("材料"))
                                {
                                    ingredientWord = "材料";
                                }
                                targetIngredients[index].GetComponentInChildren<TextMesh>().text = motionWord.Substring(0, motionWord.Length - 1) + "た後の" + ingredientWord;

                                //初期トランスフォームを変更
                                this.playbackCommon.SetInitialTransform(targetIngredients[index].transform);

                                //トリガー状態の物体名から除去
                                this.playbackCommon.TriggerObjectNames.Remove(targetIngredients[index].name);

                                //材料：乗っている台のマップから削除
                                this.ingredientOnBaseMap.Remove(targetIngredients[index].name);

                                //加工後の材料に追加
                                this.processedFoodNames.Add(targetIngredients[index].name);
                            }
                            //加工後の材料がある場合
                            else
                            {
                                targetIngredients[index].GetComponent<NVRInteractableItem>().enabled = false; //把持できなくする
                                targetIngredients[index].GetComponent<Rigidbody>().useGravity = false; // 重力をなくす
                                targetIngredients[index].GetComponent<MeshCollider>().isTrigger = true; // 衝突判定をなくす（トリガー状態）
                                targetIngredients[index].GetComponent<Rigidbody>().velocity = Vector3.zero; //慣性をなくす
                                targetIngredients[index].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                                targetIngredients[index].GetComponent<MeshRenderer>().material = this.transparentMaterial; //透明色にする
                                targetIngredients[index].transform.Translate(new Vector3(0f, 2f, 0f)); // 空中に移動させる

                                //初期トランスフォームを変更
                                this.playbackCommon.SetInitialTransform(targetIngredients[index].transform);

                                //材料：乗っている台のマップから削除
                                if (this.ingredientOnBaseMap.ContainsKey(targetIngredients[index].name))
                                {
                                    this.ingredientOnBaseMap.Remove(targetIngredients[index].name);
                                }
                            }

                        }

                    }
                    //１つ以外の対象の台に置かれている材料は見えなくする
                    else
                    {
                        targetIngredients[index].GetComponent<NVRInteractableItem>().enabled = false; //把持できなくする
                        targetIngredients[index].GetComponent<Rigidbody>().useGravity = false; // 重力をなくす
                        targetIngredients[index].GetComponent<MeshCollider>().isTrigger = true; // 衝突判定をなくす（トリガー状態）
                        targetIngredients[index].GetComponent<Rigidbody>().velocity = Vector3.zero; //慣性をなくす
                        targetIngredients[index].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                        targetIngredients[index].GetComponent<MeshRenderer>().material = this.transparentMaterial; //透明色にする
                        targetIngredients[index].transform.Translate(new Vector3(0f, 2f, 0f)); // 空中に移動させる

                        //初期トランスフォームを変更
                        this.playbackCommon.SetInitialTransform(targetIngredients[index].transform);

                        //材料：乗っている台のマップから削除
                        if (this.ingredientOnBaseMap.ContainsKey(targetIngredients[index].name))
                        {
                            this.ingredientOnBaseMap.Remove(targetIngredients[index].name);
                        }
                    }
                }
            }
        }

        //物体名からゲームオブジェクトを検索
        private GameObject FindTargetGameObject(string objectName)
        {
            GameObject target = null;
            if (this.targetObjects.Exists(x => x.name == objectName))
            {
                target = this.targetObjects.Find(x => x.name == objectName);
            }
            else if (objectName.Contains(ProcessedFoodHeader))
            {
                int processedNumber = Int32.Parse(objectName.Replace(ProcessedFoodHeader, ""));
                if (processedNumber <= this.processedFoodNames.Count)
                {
                    string replacedObjectName = this.processedFoodNames[processedNumber - 1];
                    target = this.targetObjects.Find(x => x.name == replacedObjectName);
                }
                else
                {
                    Debug.LogError("this object does not exist: " + objectName);
                }
            }
            else
            {
                Debug.LogError("this object does not exist: " + objectName);
            }

            return target;
        }

        //把持された物体の初期トランスフォームを変更
        private void ChangeGraspedObjectTransform()
        {
            List<string> graspedObjects = this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].graspedObjects;
            foreach (string targetStr in graspedObjects)
            {
                if (targetStr != "")
                {
                    GameObject target = FindTargetGameObject(targetStr);
                    this.playbackCommon.SetInitialTransform(target.transform);
                }
            }
        }

        //行動手順モデルのキーを生成
        private string MakeProcedureText(int step, int procedure)
        {
            string procedureStr = step.ToString() + "-" + procedure.ToString();
            return procedureStr;
        }

        //動作認識の対象が両手かどうか判定
        private bool IsRecognitionTargetsBothHands()
        {
            List<string> hands = new List<string> { "EthanLeftHand", "EthanRightHand" };
            if (this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].graspedObjects.Count == 2)
            {
                return true;
            }
            if (this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].graspedObjects.Count == 1
                && (hands.Exists(x => this.behaviorProceduresMap[MakeProcedureText(this.currentStep, this.currentProcedure)].collidingObjects.Contains(x))))
            {
                return true;
            }

            return false;
        }

        //アバタの前にパネルを表示
        private IEnumerator ShowNoticeMessagePanelForAvatar(string text, float waitTime = 1.0f)
        {
            this.avatarNoticeText.text = text;
            this.avatarNoticePanel.SetActive(true);

            yield return new WaitForSeconds(waitTime);

            this.avatarNoticePanel.SetActive(false);
        }

        //ユーザが完了しているレシピをチェック
        private async Task CheckFinishedTasks()
        {
            //databaseManagerが終えたタスクを読み込まなくなる
            await Task.Run(() => this.databaseManager.SelectFinishedTasks());

            if (databaseManager.GetIsUserNameUsed())
            {
                this.instructionText.text = "SQLの認証に失敗しました\n再起動してください";
                Debug.LogError("connect error");
                Application.Quit();
            }
        }

        //関数の実行を遅らせる
        private IEnumerator DelayMethod(float waitTime, Action action)
        {
            yield return new WaitForSeconds(waitTime);
            action();
        }

        //recognizerから現在の動作の誤差を受け取った際の処理
        public void OnRecieveMotionError(float sum)
        {
            this.currentError = sum;

            if (this.recognizer.GetSimilarity() > this.maxSimilarity)
            {
                this.maxSimilarity = this.recognizer.GetSimilarity();
                this.incorrectElapsedTime = 0.0f;
            }

            if (this.isRecordingMotionError)
            {
                string filename = RecordingFolder + "/" + this.userID + "_" + this.recipeID + "_error.csv";
                using (StreamWriter sw = new StreamWriter(filename, true))
                {
                    sw.WriteLine(this.currentStep.ToString() + "," + this.currentProcedure.ToString() + "," + this.currentError);
                }
            }
        }

        //generatorから観測停止後に抽出した把持・接触物体を受け取った際の処理
        public void OnRecieveExtractedObjects(string graspedObjects, string collidingObjects)
        {
            this.extractedGraspedText = graspedObjects;
            this.extractedCollidingText = collidingObjects;
            Debug.Log("extracted grasped objects: " + this.extractedGraspedText);
            Debug.Log("extracted colliding objects: " + this.extractedCollidingText);
        }

        //private IEnumerator ComputeAllHandDistances() //for extra mode
        //{
        //    ExecuteEvents.Execute<IRecieveRecipeControl>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnRecieveToStopChoosing());
        //    this.recipeID = recipeManager.GetCurrentRecipeID();

        //    foreach (int id in this.allIdsForPlay)
        //    {
        //        float handMovementDistance = 0.0f;

        //        this.databaseManager.participantID = this.experimentType * 100 + id;

        //        for (int step = 1; step <= this.recipeManager.GetStepCount(); step++)
        //        {
        //            for (int procedure = 1; procedure < 20; procedure++)
        //            {
        //                List<Vector3> leftMotions = new List<Vector3>();
        //                List<Vector3> rightMotions = new List<Vector3>();

        //                ExecuteEvents.Execute<IRecievePlaySwitch>(this.player.gameObject, null, (reciever, eventData) => reciever.OnRecieveToLoad(this.recipeID, step, procedure, "user"));

        //                while (!this.player.GetIsPreparing())
        //                {
        //                    yield return new WaitForSeconds(0.1f);
        //                }

        //                if (!this.player.GetIsNoMotions())
        //                {
        //                    List<string> handData = this.player.GetHandData();

        //                    foreach (string handString in handData)
        //                    {
        //                        string[] stringArray = handString.Split(',');
        //                        float[] doubleArray = stringArray.Select(x => float.Parse(x)).ToArray();
        //                        Vector3 leftMotion = new Vector3(doubleArray[0], doubleArray[1], doubleArray[2]);
        //                        Vector3 rightMotion = new Vector3(doubleArray[3], doubleArray[4], doubleArray[5]);
        //                        leftMotions.Add(leftMotion);
        //                        rightMotions.Add(rightMotion);
        //                    }

        //                    float distance = 0.0f;
        //                    for (int i = 1; i < leftMotions.Count; i++)
        //                    {
        //                        distance += Vector3.Distance(leftMotions[i - 1], leftMotions[i]) + Vector3.Distance(rightMotions[i - 1], rightMotions[i]);
        //                    }

        //                    handMovementDistance += distance;
        //                    this.completedTimeStr += distance.ToString() + ",";

        //                    yield return new WaitForSeconds(0.1f);
        //                }
        //            }

        //            this.completedTimeStrings.Add(this.completedTimeStr);
        //            this.completedTimeStr = "";
        //        }


        //        using (StreamWriter sw = new StreamWriter(RecordingFolder + "/distance.csv", true))
        //        {
        //            sw.Write(id.ToString() + ",,");
        //            foreach (string str in this.completedTimeStrings)
        //            {
        //                sw.Write(str + ",");
        //            }
        //            sw.WriteLine(handMovementDistance.ToString());
        //        }

        //        this.completedTimeStrings.Clear();

        //        yield return new WaitForSeconds(0.1f);
        //    }

        //    //ExecuteEvents.Execute<ISwitchGuidance>(this.recipeManager.gameObject, null, (reciever, eventData) => reciever.OnFinishGuidance());
        //}

        public string GetRecipeID()
        {
            return this.recipeID;
        }
        
        public int GetCurrentStep()
        {
            return this.currentStep;
        }

        public int GetCurrentProcedure()
        {
            return this.currentProcedure;
        }

        //for experiment
        public void OnClickInCorrectButton()
        {
            this.isIncorrectMotion = true;
            Debug.Log("Pressed 'No'");
        }

        public void OnClickCorrectButton()
        {
            this.isCorrectMotion = true;
            Debug.Log("Pressed 'OK'");
        }

        private void OnApplicationQuit()
        {
        }

        //private void CheckGraspingStatus(NVRHand hand)
        //{
        //    if (hand.HoldButtonDown)
        //    {
        //        if (hand.IsInteracting)
        //        {
        //            Debug.Log("Interaction: " + hand.name + ".GraspObject");
        //        }
        //        else
        //        {
        //            Debug.Log("Interaction: " + hand.name + ".HoldButtonDown");
        //        }
        //    }

        //    if (hand.HoldButtonUp)
        //    {
        //        Debug.Log("Interaction: " + hand.name + ".HoldButtonUp");
        //    }
        //}

        //private void ShowAllProcedures()
        //{
        //    int step = 1;
        //    int procedure = 1;
        //    bool isLeft = true;
        //    string explanationStr = "<b>【調理手順】</b>\n\n";
        //    while (isLeft)
        //    {
        //        string[] graspedObjects = this.motionProceduresMap[MakeProcedureStr(step, procedure)][0].Split(',');
        //        string[] collidedObjects = this.motionProceduresMap[MakeProcedureStr(step, procedure)][1].Split(',');

        //        explanationStr += "手順" + MakeProcedureStr(step, procedure) + ":\n";
        //        if (graspedObjects[0] != "nothing")
        //        {
        //            foreach (var grasp in graspedObjects.Select((value, index) => new { value, index }))
        //            {
        //                if (grasp.value == "hand") { explanationStr += "手"; }
        //                else
        //                {
        //                    explanationStr += "'" + objectNameMap[grasp.value] + "'";
        //                }

        //                if (grasp.index == graspedObjects.Length - 1) { explanationStr += "を"; }
        //                else { explanationStr += "と"; }
        //            }
        //            explanationStr += "取ってください\n";
        //        }
        //        if (collidedObjects[0] != "nothing")
        //        {
        //            if (graspedObjects[0] != "nothing") { explanationStr += "そして、"; }
        //            foreach (var collide in collidedObjects.Select((value, index) => new { value, index }))
        //            {
        //                if (collide.value == "hand") { explanationStr += "手"; }
        //                else
        //                {
        //                    explanationStr += "'" + objectNameMap[collide.value] + "'";
        //                }

        //                if (collide.index == collidedObjects.Length - 1) { explanationStr += "を"; }
        //                else { explanationStr += "と"; }
        //            }
        //            explanationStr += "接触させてください\n";
        //        }
        //        explanationStr += "\n";

        //        if (this.motionProceduresMap.ContainsKey(MakeProcedureStr(step, procedure + 1)))
        //        {
        //            procedure++;
        //        }
        //        else if (this.motionProceduresMap.ContainsKey(MakeProcedureStr(step + 1, 1)))
        //        {
        //            step++;
        //            procedure = 1;
        //        }
        //        else
        //        {
        //            isLeft = false;
        //        }
        //    }

        //    explanationStr += "以上です！";
        //    procedureText.text = explanationStr;

        //    this.elapsedCorrectTime = 0.0f;
        //    this.elapsedIncorrectTime = 0.0f;
        //}

        //public void ScrollMessage(float num)
        //{
        //    num = num / 100;
        //    this.scrollRect.verticalNormalizedPosition += num;
        //    if (scrollRect.verticalNormalizedPosition < 0.0f) { scrollRect.verticalNormalizedPosition = 0.0f; }
        //}
    }
}
