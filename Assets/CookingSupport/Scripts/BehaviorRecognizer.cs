using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using System.Threading;

namespace CookingSupport
{
    public interface IRecieveRecognitionSwitch : IEventSystemHandler
    {
        void OnRecieveToLoad(string recipeID, int step, int procedure);
        void OnRecieveToStart(bool isBoth);
        void OnRecieveToStop();
    }

    public class BehaviorRecognizer : MonoBehaviour, IRecieveRecognitionSwitch
    {
        public const int PatternLength = 100; //マッチングに用いる利用者の動作データの長さ
        public CookingSupportModerator moderator;
        public CookingSupportPlaybackCommon playbackCommon;
        public CookingSupportDatabaseManager databaseManager;

        private CookingSupportRecorder recorder;
        private CookingSupportPlayer player;

        private string recipeID;
        private int step;
        private int procedure;
        private List<string> expertHandStrings = new List<string>();

        private List<List<double>> userMotions = new List<List<double>>();
        private List<List<double>> expertMotions = new List<List<double>>();
        private double[,] distance;
        private double[,] f;

        private double sum;

        private double elapsedTime;
        private double previousRecordedTime;

        private bool isStoring;
        private bool isBoth;
        private bool isExpertMotionLoaded;

        private List<Transform> targetTransforms = new List<Transform>();
        private int targetDims;

        private int inclinationLimit = 0; //0:assymetry, 1:symmetry

        private void Awake()
        {
            recorder = this.playbackCommon.gameObject.GetComponent<CookingSupportRecorder>();
            player = this.playbackCommon.gameObject.GetComponent<CookingSupportPlayer>();

            isStoring = false;
            isBoth = false;
            isExpertMotionLoaded = false;
            elapsedTime = 0.0f;

            targetTransforms.Add(this.playbackCommon.GetLeftHand());
            targetTransforms.Add(this.playbackCommon.GetRightHand());
            targetDims = targetTransforms.Count * 3;
        }

        private void Update()
        {
            if (this.isStoring && !this.player.GetIsPlaying())
            {
                this.elapsedTime += Time.deltaTime;

                //動作データを蓄積
                SaveMotion();
                //動作データが一定フレーム数を超えたら，マッチングを行う
                if (this.userMotions.Count >= PatternLength)
                {
                    Debug.Log("Start computing.");
                    SynchronizationContext context = SynchronizationContext.Current;
                    Task.Run(async () => await ComputeDTW(context));
                    this.isStoring = false;
                }
            }
        }

        //経験者の手の動作データを取得
        public void OnRecieveToLoad(string recipeID, int step, int procedure)
        {
            this.recipeID = recipeID;
            this.step = step;
            this.procedure = procedure;

            this.expertHandStrings = this.databaseManager.SelectHandMotions(this.recipeID, this.step, this.procedure, "expert");

            this.isExpertMotionLoaded = true;
        }

        //認識開始
        public void OnRecieveToStart(bool isBoth)
        {
            this.isBoth = isBoth;

            this.isStoring = false;
            this.elapsedTime = 0.0f;
            this.previousRecordedTime = 0.0f;
            this.userMotions.Clear();

            base.StartCoroutine(SetExpertMotions());
        }

        //認識停止
        public void OnRecieveToStop()
        {
            this.isStoring = false;
        }

        //経験者の手の動作データを配列にセット
        private IEnumerator SetExpertMotions()
        {
            this.expertMotions.Clear();
            while (!this.isExpertMotionLoaded)
            {
                yield return new WaitForSeconds(0.1f);
            }

            foreach (string handString in this.expertHandStrings)
            {
                string[] stringArray = handString.Split(',');
                double[] doubleArray = stringArray.Select(x => double.Parse(x)).ToArray();
                this.expertMotions.Add(new List<double>(doubleArray));
            }

            this.isStoring = true;
            this.isExpertMotionLoaded = false;
        }

        //利用者の手の動作データを蓄積
        private void SaveMotion()
        {
            if (1000.0 * (this.elapsedTime - this.previousRecordedTime) < this.recorder.RecordInterval) { return; }

            List<double> transformValues = new List<double>();

            foreach (Transform transform in this.targetTransforms)
            {
                transformValues.Add(transform.position.x);
                transformValues.Add(transform.position.y);
                transformValues.Add(transform.position.z);
            }

            this.userMotions.Add(transformValues);

            this.previousRecordedTime = this.elapsedTime;
        }

        //DPマッチング開始
        private void StartComputing()
        {
            SynchronizationContext context = SynchronizationContext.Current;
            Task.Run(async() => await ComputeDTW(context));
        }

        //DPマッチングの計算
        public async Task ComputeDTW(SynchronizationContext context)
        {
            this.distance = new double[this.userMotions.Count, this.expertMotions.Count];
            this.f = new double[this.userMotions.Count, this.expertMotions.Count];
            this.sum = double.PositiveInfinity;

            //経験者と利用者でフレーム同士の誤差を算出
            await Task.Run(() => ComputeDistance());

            //時系列の終点をずらしながら最小の誤差を算出
            for (int y = 0; y < PatternLength / 2; y++)
            {
                await Task.Run(() =>
                {
                    //初期化
                    InitializeF();

                    //フレーム同士の誤差を累積して最小となるルートを算出し，累積誤差を取り出す
                    double oneSum = ComputeFBackward(this.f.GetLength(0) - 1, this.f.GetLength(1) - 1 - y);
                    if (oneSum < this.sum)
                    {
                        this.sum = oneSum;
                    }
                });
            }

            //Debug.Log("dp sum: " + this.sum.ToString());
            Debug.Log("similarity: " + this.GetSimilarity());

            await Task.Run(() => context.Post((state) => ExecuteEvents.Execute<IRecieveRecognitionResult>(this.moderator.gameObject, null, (reciever, eventData) => reciever.OnRecieveMotionError(this.GetSum())), null));

            //計算を終えたら，蓄積開始
            this.userMotions.Clear();
            this.isStoring = true;
        }

        //２つの系列におけるフレーム同士の誤差距離を計算
        private void ComputeDistance()
        {
            int startIndex = 0;
            if (!this.isBoth)
            {
                startIndex = 3;
            }

            for (int i = 0; i < this.userMotions.Count; ++i)
            {
                for (int j = 0; j < this.expertMotions.Count; ++j)
                {
                    double dist = 0;
                    for (int num = startIndex; num < this.targetDims; ++num)
                    {
                        //xyzそれぞれの距離を計算
                        dist += Math.Pow(userMotions[i][num] - expertMotions[j][num], 2);
                        //移動差を出す場合
                        /*double userDiff, expertDiff;
                        if (i == 0)
                        {
                            userDiff = 0;
                        }
                        else
                        {
                            userDiff = this.userMotions[i][num] - this.userMotions[i - 1][num];
                        }
                        if (j == 0)
                        {
                            expertDiff = 0;
                        }
                        else
                        {
                            expertDiff = this.expertMotions[j][num] - this.expertMotions[j - 1][num];
                        }
                        dist = Math.Pow(userDiff - expertDiff, 2);*/
                    }

                    this.distance[i, j] = Math.Sqrt(dist);
                }
            }
        }

        //F行列を初期化
        private void InitializeF()
        {
            for (int i = 0; i < this.f.GetLength(0); ++i)
            {
                for (int j = 0; j < this.f.GetLength(1); ++j)
                {
                    this.f[i, j] = -1.0f;
                }
            }
        }

        //フレーム同士の誤差距離をもとに最小の累積誤差距離を再帰的に算出
        private double ComputeFBackward(int i, int j)
        {
            if (i < 0)
            {
                return 0.0f;
            }
            else if (j < 0)
            {
                return double.PositiveInfinity;
            }
            else if (this.f[i, j] >= 0.0f)
            {
                return this.f[i, j];
            }
            else  // -1 is nothing
            {
                double minCost = double.PositiveInfinity;
                switch (inclinationLimit)
                {
                    case 0:
                        minCost = Math.Min(ComputeFBackward(i, j - 1), minCost);
                        minCost = Math.Min(ComputeFBackward(i - 1, j - 1), minCost);
                        minCost = Math.Min(ComputeFBackward(i - 1, j), minCost);
                        break;
                    case 1:
                        minCost = Math.Min(ComputeFBackward(i - 1, j - 2), minCost);
                        minCost = Math.Min(ComputeFBackward(i - 1, j - 1), minCost);
                        minCost = Math.Min(ComputeFBackward(i - 1, j), minCost);
                        break;
                    case 2:
                        minCost = Math.Min(ComputeFBackward(i, j - 1), minCost);
                        minCost = Math.Min(ComputeFBackward(i - 1, j - 1), minCost);
                        minCost = Math.Min(ComputeFBackward(i - 2, j - 1), minCost);
                        break;
                    default:
                        break;
                }

                this.f[i, j] = this.distance[i, j] + minCost;
            }
            return this.f[i, j];
        }

        //private List<int[]> SearchRoute()
        //{
        //    int i = 0;
        //    int j = 0;
        //    List<int[]> route = new List<int[]>();
        //    while (i < this.f.GetLength(0) || j < this.f.GetLength(1))
        //    {
        //        if (this.f[i + 1, j + 1] <= this.f[i + 1, j] && this.f[i + 1, j + 1] <= this.f[i, j + 1])
        //        {
        //            i++;
        //            j++;
        //        }
        //        else if (this.f[i, i + 1] <= this.f[i + 1, j])
        //        {
        //            j++;
        //        }
        //        else
        //        {
        //            i++;
        //        }
        //        route.Add(new int[] { i, j });
        //    }
        
        //    return route;
        //}

        public float GetSum()
        {
            return (float)sum;
        }

        public float GetSimilarity()
        {
            return (float)(1 - sum / PatternLength);
        }
    }
}
