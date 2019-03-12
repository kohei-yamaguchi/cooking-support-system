using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;

namespace CookingSupport
{
    public interface IRecieveRecipeControl : IEventSystemHandler
    {
        void OnRecieveToStopChoosing();
        void OnRecieveToStartChoosing();
        void OnRecieveToChangeStepColor(int step);
    }

    public class CookingSupportRecipeManager : MonoBehaviour, IRecieveRecipeControl
    {
        public CookingSupportDatabaseManager databaseManager;

        public UnityEngine.UI.Text titleText;
        public ScrollRect stepsScrollPanel;
        public UnityEngine.UI.Text stepsText;
        public UnityEngine.UI.Text ingredientsText;
        public GameObject descriptionPanel;

        public int beginningTaskNumber = 0;
        public bool isRecipeChangedAfterFinish = false;

        private int currentTaskNumber;

        private List<string> recipeIDs = new List<string>();
        private string title = "";
        private List<string> steps = new List<string>();
        private List<string> ingredients = new List<string>();

        private bool canChooseRecipes;
        private bool isRecipeInfoLoaded;
        private bool isRecipeInfoReady;

        private void Awake()
        {
            titleText.text = "";
            stepsText.text = "";
            ingredientsText.text = "";

            currentTaskNumber = beginningTaskNumber;
            canChooseRecipes = false;
            isRecipeInfoReady = false;

            stepsScrollPanel.verticalNormalizedPosition = 1.0f;
        }

        private void Start()
        {
            Task.Run(async () =>
            {
                await LoadGuidanceRecipeList();
                await LoadRecipeInfo();
            });
        }

        private void Update()
        {
            try
            {
                //右スティックの上下で，ステップ文のスクロール
                ScrollMessage(OVRInput.Get(OVRInput.RawAxis2D.RThumbstick).y);

                //ガイダンスが開始されるまで，右スティック左右でレシピの変更
                if (this.canChooseRecipes)
                {
                    if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight) || Input.GetKeyDown(KeyCode.RightArrow)) { NextScenceID(); }
                    if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickLeft) || Input.GetKeyDown(KeyCode.LeftArrow)) { BackScenceID(); }
                }

                //レシピ情報が読み込まれたら，パネルに表示
                if (this.isRecipeInfoLoaded)
                {
                    SetRecipeInfo();
                    this.isRecipeInfoLoaded = false;
                    this.isRecipeInfoReady = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                throw ex;
            }
        }

        //ガイダンス対象のレシピIDを全て読み込む
        private async Task LoadGuidanceRecipeList()
        {
            this.recipeIDs.Clear();

            await Task.Run(() => this.databaseManager.SelectGuidanceRecipes());

            this.recipeIDs = new List<string>(databaseManager.GetRecipeIDs());

            this.canChooseRecipes = true;
        }

        //現在のレシピIDの情報（題名，ステップ，材料）を読み込む
        private async Task LoadRecipeInfo()
        {
            Debug.Log("Load task no." + this.currentTaskNumber);

            string recipeID = this.recipeIDs[currentTaskNumber];

            await Task.Run(() => this.databaseManager.SelectRecipeData(recipeID));

            this.steps.Clear();
            this.ingredients.Clear();

            this.title = this.databaseManager.GetRecipeTitle();
            this.steps = new List<string>(this.databaseManager.GetRecipeSteps());
            this.ingredients = this.databaseManager.GetRecipeIngredients();

            this.isRecipeInfoLoaded = true;
        }

        //読み込んだレシピ情報をパネルに表示
        private void SetRecipeInfo()
        {
            this.titleText.text = this.title;

            string stepsStr = "";
            for (int i = 0; i < this.steps.Count; i++)
            {
                stepsStr += "<color=#00FF00>Step " + (i + 1).ToString("D2") + ":\n</color><color=#696969>" + steps[i] + "</color>\n";
            }
            this.stepsText.text = stepsStr;

            string ingredientsStr = "";
            foreach (var ingredient in this.ingredients)
            {
                ingredientsStr += ingredient + "\n";
            }
            this.ingredientsText.text = ingredientsStr;

            Debug.Log("Set a recipe.");
        }

        //次のレシピIDのレシピ情報を読み込む
        public void NextScenceID()
        {
            Debug.Log("Next --> ");
            this.currentTaskNumber++;
            if (this.currentTaskNumber >= this.recipeIDs.Count)
            {
                this.currentTaskNumber = 0;
            }

            Task.Run(() => LoadRecipeInfo());
        }

        //前のレシピIDのレシピ情報を読み込む
        public void BackScenceID()
        {
            Debug.Log("<-- Back ");
            this.currentTaskNumber--;
            if (this.currentTaskNumber < 0)
            {
                this.currentTaskNumber = this.recipeIDs.Count - 1;
            }

            Task.Run(() => LoadRecipeInfo());
        }

        //パネルのステップ文をスクロール
        public void ScrollMessage(float num)
        {
            num = num / 300;
            this.stepsScrollPanel.verticalNormalizedPosition += num;
            if (stepsScrollPanel.verticalNormalizedPosition < 0.0f) stepsScrollPanel.verticalNormalizedPosition = 0.0f;
        }

        //現在のステップ文を赤にする
        public void OnRecieveToChangeStepColor(int step)
        {
            string settingSteps = "";
            for (int i = 0; i < this.steps.Count; i++)
            {
                string colorName = "#696969";
                if (i == step - 1)
                {
                    colorName = "#FF0000";
                }
                steps[i] = steps[i].Trim();
                settingSteps += "<color=#00FF00>Step " + (i + 1).ToString("D2") + ":\n</color><color=" + colorName + ">" + steps[i] + "</color>\n";
            }
            this.stepsText.text = settingSteps;
        }

        //ガイダンス開始後，レシピ変更を不可能にする
        public void OnRecieveToStopChoosing()
        {
            this.canChooseRecipes = false;
            this.isRecipeInfoReady = false;
            descriptionPanel.SetActive(false);
        }

        //ガイダンス終了後，レシピ変更を可能にする
        public void OnRecieveToStartChoosing()
        {
            this.canChooseRecipes = true;
            if (this.isRecipeChangedAfterFinish)
            {
                this.currentTaskNumber++;
            }

            Task.Run(async () => await LoadRecipeInfo());
            //操作の説明
            descriptionPanel.SetActive(true);
        }

        public int GetCurrentScenarioID()
        {
            return this.currentTaskNumber;
        }

        public string GetCurrentRecipeID()
        {
            return this.recipeIDs[this.currentTaskNumber];
        }

        public string GetCurrentStepText(int step)
        {
            return this.steps[step-1];
        }

        public string GetTitle()
        {
            return this.title;
        }

        public int GetStepCount()
        {
            return this.steps.Count;
        }

        public bool GetIsRecipeInfoReady()
        {
            return this.isRecipeInfoReady;
        }
    }
}