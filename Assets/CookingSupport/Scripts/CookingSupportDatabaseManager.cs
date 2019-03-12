using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using MySql.Data.MySqlClient;

namespace CookingSupport
{
    public class CookingSupportDatabaseManager : MonoBehaviour
    {
        [HideInInspector]
        public int expertID = 1;
        [HideInInspector]
        public int participantID = 1;
        [HideInInspector]
        public string participantName = "";

        private const string MySqlSchemaName = "cooking_support";
        //private const string MySqlSchemaNameForPractice = "cooking_task";
        private const string MySqlTableName_userData = "user_data";
        private const string MySqlTableName_expertMotions = "expert_motions";
        private const string MySqlTableName_playbackMotions = "playback_motions";
        private const string MySqlTableName_guidanceRecipes = "guidance_recipes";
        private const string MySqlTableName_behaviorProcedures = "behavior_procedures";
        private const string MySqlTableName_behaviorTypes = "behavior_types";

        //localhost
        public string mySqlIP = "localhost";
        public string mySqlPort = "3306";
        public string mySqlUsername = "root";
        public string mySqlPassword = "password";

        //NIIのデータベースへアクセスする場合，SSH接続用のport, username，passwordを入力
        private const string CookingRecipeSchema = "cookpad_data"; //tables: 'recipes', 'steps', 'ingredients'
        private const string CookingRecipeIP = "localhost";
        private const string CookingRecipePort = "3306";
        private const string CookingRecipeUsername = "root";
        private const string CookingRecipePassword = "password";

        private bool isSSHConnection = false;

        private bool isUserNameUsed = false;
        private Dictionary<string, string> usernameIdMap = new Dictionary<string, string>();
        private List<int> finishedTasks = new List<int>();

        private string recipeTitle = "";
        private List<string> recipeSteps = new List<string>();
        private List<string> recipeIngredients = new List<string>();
        private Dictionary<string, int> recipeNumberMap = new Dictionary<string, int>();

        private List<string> recipeIDs = new List<string>();
        private List<string> behaviorProcedures = new List<string>();
        private List<string> procedures = new List<string>();

        private Dictionary<string, string> recipeId2ProcessedFoodMap = new Dictionary<string, string>();

        private Dictionary<string, string> behaviorType2InstructionMap = new Dictionary<string, string>();
        private Dictionary<string, string> behaviorType2MeaningMap = new Dictionary<string, string>();
        private List<string> behaviorTypes = new List<string>();
        private List<string> processTypes = new List<string>();
        private List<string> graspTypes = new List<string>();
        private List<string> collideTypes = new List<string>();

        private List<string> handMotionData = new List<string>();
        private List<string> graspedObjectData = new List<string>();
        private List<string> collidingObjectData = new List<string>();

        //private Dictionary<string, string> handmadeExplanationMap = new Dictionary<string, string>();

        private void Awake()
        {
            //ユーザ情報を取得
            SelectUserData();
        }

        private void Start()
        {
            //DontDestroyOnLoad(this);
        }

        public void LogIn(string name)
        {
            Debug.Log("Log in");
            MySqlConnection mysqlConnection = null;

            string connString =
                "server =" + mySqlIP + ";" +
                "port =" + mySqlPort + ";" +
                "database =" + MySqlSchemaName + ";" +
                "userid =" + mySqlUsername + ";" +
                "password =" + mySqlPassword + ";";

            try
            {
                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string selectSql = "SELECT * FROM " + MySqlSchemaName + "." + MySqlTableName_userData + " WHERE user_name='" + name + "'";

                MySqlCommand mySqlCommand = new MySqlCommand(selectSql, mysqlConnection);

                IAsyncResult iAsync = mySqlCommand.BeginExecuteReader();

                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                MySqlDataReader mySqlDataReader = mySqlCommand.EndExecuteReader(iAsync);

                List<string> userInfoList = new List<string>();
                int user_id = 0;

                while (mySqlDataReader.Read())
                {
                    userInfoList.Add(mySqlDataReader.GetString("user_name"));
                    user_id = int.Parse(mySqlDataReader.GetString("user_id"));
                }

                this.participantID = user_id;
                this.participantName = name;

                mySqlDataReader.Close();
                mySqlCommand.Dispose();
                mysqlConnection.Close();

                if (userInfoList.Count == 0)
                {
                    SignUp(name);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }

        }

        public void SignUp(string name)
        {
            Debug.Log("Sign up");
            MySqlConnection mysqlConnection = null;

            string connString =
                "server =" + mySqlIP + ";" +
                "port =" + mySqlPort + ";" +
                "database =" + MySqlSchemaName + ";" +
                "userid =" + mySqlUsername + ";" +
                "password=" + mySqlPassword + ";";

            try
            {
                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                int userId = this.MakeUserId(name);
                usernameIdMap[name] = userId.ToString();

                string valueString = "(" + userId + ",'" + name + "')";
                string columnString = "(user_id, user_name)";

                string insertSql = "INSERT INTO " + MySqlSchemaName + "." + MySqlTableName_userData + " " + columnString + " VALUES " + valueString;

                MySqlCommand mysqlCommand = new MySqlCommand(insertSql, mysqlConnection);
                mysqlCommand.ExecuteNonQuery();

                Debug.Log("Inserted " + "1" + " records.");

                mysqlCommand.Dispose();
                mysqlConnection.Close();

                this.participantName = name;
                this.participantID = userId;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }

                isUserNameUsed = true;

                throw ex;
            }

        }

        private int MakeUserId(string seedString)
        {
            int seed = 0;
            char[] chars = seedString.ToCharArray();
            int len = seedString.Length - 1;
            foreach (var c in chars)
            {
                int asc = (int)c - 64;
                seed += asc * (int)Math.Pow((double)26, (double)len--);
            }
            System.Random rand = new System.Random(seed);
            int val = rand.Next();

            return val;
        }

        private void SelectUserData()
        {
            MySqlConnection mysqlConnection = null;

            string connString =
                "server =" + mySqlIP + ";" +
                "port =" + mySqlPort + ";" +
                "database =" + MySqlSchemaName + ";" +
                "userid =" + mySqlUsername + ";" +
                "password=" + mySqlPassword + ";";

            try
            {
                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string selectSql = "SELECT * FROM " + MySqlSchemaName + "." + MySqlTableName_userData;

                MySqlCommand mySqlCommand = new MySqlCommand(selectSql, mysqlConnection);
                IAsyncResult iAsync = mySqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                MySqlDataReader mySqlDataReader = mySqlCommand.EndExecuteReader(iAsync);

                while (mySqlDataReader.Read())
                {
                    usernameIdMap.Add(mySqlDataReader.GetString("user_name"), mySqlDataReader.GetString("user_id"));
                }
                mySqlDataReader.Close();
                mySqlCommand.Dispose();
                mysqlConnection.Close();

            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        public void SelectFinishedTasks()
        {
            MySqlConnection mysqlConnection = null;

            string connString =
                "server =" + mySqlIP + ";" +
                "port =" + mySqlPort + ";" +
                "database =" + MySqlSchemaName + ";" +
                "userid =" + mySqlUsername + ";" +
                "password=" + mySqlPassword + ";";

            try
            {
                this.finishedTasks.Clear();

                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string selectSql = String.Format("SELECT user_id,finished_tasks FROM {0}.{1} WHERE user_name='{2}'", MySqlSchemaName, MySqlTableName_userData, this.participantName);

                MySqlCommand mySqlCommand = new MySqlCommand(selectSql, mysqlConnection);
                IAsyncResult iAsync = mySqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                MySqlDataReader mySqlDataReader = mySqlCommand.EndExecuteReader(iAsync);

                string userID = "";
                string taskString = "";
                while (mySqlDataReader.Read())
                {
                    if (!mySqlDataReader.IsDBNull(0))
                    {
                        userID = mySqlDataReader.GetString(0);
                    }
                    if (!mySqlDataReader.IsDBNull(1))
                    {
                        taskString = mySqlDataReader.GetString(1);
                    }
                }

                if (userID == "")
                {
                    SignUp(this.participantName);
                }
                else if(taskString != "")
                {
                    Debug.Log("finished tasks: " + taskString);

                    List<string> finishedStrings = new List<string>(taskString.Split(','));
                    this.finishedTasks = finishedStrings.ConvertAll(x => Int32.Parse(x));
                }

                mySqlDataReader.Close();
                mySqlCommand.Dispose();
                mysqlConnection.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        public void InsertFinishedTasks(string userName, string recipeID)
        {
            MySqlConnection mysqlConnection = null;

            string connString =
                "server =" + mySqlIP + ";" +
                "port =" + mySqlPort + ";" +
                "database =" + MySqlSchemaName + ";" +
                "userid =" + mySqlUsername + ";" +
                "password=" + mySqlPassword + ";";

            try
            {
                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                this.finishedTasks.Add(this.recipeNumberMap[recipeID]);
                this.finishedTasks.Sort();

                string taskString = string.Empty;
                foreach (int num in this.finishedTasks)
                {
                    taskString += num + ",";
                }
                taskString = taskString.TrimEnd(',');

                string insertSql = String.Format("UPDATE {0}.{1} SET finished_tasks='{2}' WHERE user_name='{3}'",
                    MySqlSchemaName, MySqlTableName_userData, taskString, userName);

                MySqlCommand mysqlCommand = new MySqlCommand(insertSql, mysqlConnection);
                mysqlCommand.ExecuteNonQuery();

                Debug.Log("Inserted 1 finished task.");

                mysqlCommand.Dispose();
                mysqlConnection.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        public void SelectGuidanceRecipes()
        {
            MySqlConnection mysqlConnection = null;

            try
            {
                this.recipeIDs.Clear();
                this.recipeNumberMap.Clear();
                this.recipeId2ProcessedFoodMap.Clear();

                string connectString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password =" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connectString);
                mysqlConnection.Open();

                string selectString = "SELECT * FROM " + MySqlSchemaName + "." + MySqlTableName_guidanceRecipes;
                MySqlCommand mysqlCommand = new MySqlCommand(selectString, mysqlConnection);

                IAsyncResult iAsync = mysqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                this.recipeIDs.Clear();
                MySqlDataReader mysqlDataReader = mysqlCommand.EndExecuteReader(iAsync);
                while (mysqlDataReader.Read())
                {
                    this.recipeNumberMap[mysqlDataReader.GetString("recipe_id")] = int.Parse(mysqlDataReader.GetString("task_id"));
                    if (!this.finishedTasks.Contains(int.Parse(mysqlDataReader.GetString("task_id"))))
                    {
                        this.recipeIDs.Add(mysqlDataReader.GetString("recipe_id"));
                    }

                    if (!mysqlDataReader.IsDBNull(2))
                    {
                        this.recipeId2ProcessedFoodMap.Add(mysqlDataReader.GetString("recipe_id"), mysqlDataReader.GetString("processed_foods"));
                    }
                }

                Debug.Log("the number of recipes: " + this.recipeIDs.Count);

                mysqlDataReader.Close();
                mysqlCommand.Dispose();
                mysqlConnection.Clone();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        public void InsertProcessedFoods(List<string> processedFoods, string recipeID)
        {
            MySqlConnection mysqlConnection = null;

            try
            {
                string connString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password=" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string processedString = string.Empty;
                foreach (string food in processedFoods)
                {
                    processedString += food + ",";
                }
                processedString = processedString.TrimEnd(',');

                string insertSql = String.Format("UPDATE {0}.{1} SET processed_foods='{2}' WHERE recipe_id='{3}'",
                    MySqlSchemaName, MySqlTableName_guidanceRecipes, processedString, recipeID);

                MySqlCommand mysqlCommand = new MySqlCommand(insertSql, mysqlConnection);
                mysqlCommand.ExecuteNonQuery();

                Debug.Log("Inserted processed foods.");

                mysqlCommand.Dispose();
                mysqlConnection.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        private List<string> SelectRecipeInfoFromTable(string tableName, string recipeId)
        {
            MySqlConnection mysqlConnection = null;

            List<string> taskInfoList = new List<string>();

            string connString =
                "server =" + mySqlIP + ";" +
                "port =" + mySqlPort + ";" +
                "database =" + CookingRecipeSchema + ";" +
                "userid =" + mySqlUsername + ";" +
                "password=" + mySqlPassword + ";";

            if (this.isSSHConnection)
            {
                connString =
                    "server =" + CookingRecipeIP + ";" +
                    "port =" + CookingRecipePort + ";" +
                    "database =" + CookingRecipeSchema + ";" +
                    "userid =" + CookingRecipeUsername + ";" +
                    "password=" + CookingRecipePassword + ";";
            }

            string idColumn = "";
            string selectColumn = "";
            string option = "";
            if (tableName == "recipes")
            {
                idColumn = "id";
                selectColumn = "title";
            }
            else if (tableName == "steps")
            {
                idColumn = "recipe_id";
                selectColumn = "memo";
                option = "ORDER BY position";
            }
            else if (tableName == "ingredients")
            {
                idColumn = "recipe_id";
                selectColumn = "name";
            }

            try
            {
                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string selectSql = String.Format("SELECT * FROM {0}.{1} WHERE {2}='{3}' {4}", CookingRecipeSchema, tableName, idColumn, recipeId, option);

                MySqlCommand mySqlCommand = new MySqlCommand(selectSql, mysqlConnection);
                IAsyncResult iAsync = mySqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                MySqlDataReader mySqlDataReader = mySqlCommand.EndExecuteReader(iAsync);

                while (mySqlDataReader.Read())
                {
                    taskInfoList.Add(mySqlDataReader.GetString(selectColumn));
                }

                mySqlDataReader.Close();
                mySqlCommand.Dispose();
                mysqlConnection.Close();

                return taskInfoList;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                return taskInfoList;
            }
        }

        public void SelectRecipeData(string recipeID)
        {
            try
            {
                this.recipeTitle = SelectRecipeInfoFromTable("recipes", recipeID)[0];
                this.recipeSteps.Clear();
                this.recipeIngredients.Clear();
                this.recipeSteps = new List<string>(SelectRecipeInfoFromTable("steps", recipeID));
                this.recipeIngredients = new List<string>(SelectRecipeInfoFromTable("ingredients", recipeID));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                throw ex;
            }
        }

        public List<string> SelectPlaybackMotions(string recipeID, int step, int procedure, string table)
        {
            MySqlConnection mysqlConnection = null;

            List<string> motionDataList = new List<string>();
            try
            {
                this.handMotionData.Clear();
                this.graspedObjectData.Clear();
                this.collidingObjectData.Clear();

                string connString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password =" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string tableName = MySqlTableName_playbackMotions; //if (table == "participant")
                int recordingID = this.participantID;
                if (table == "expert")
                {
                    tableName = MySqlTableName_expertMotions;
                    recordingID = this.expertID;
                }

                string columnString = "motion_data, hand_data, grasped_objects, colliding_objects";

                string selectSql = String.Format("SELECT {0} FROM {1}.{2} WHERE recording_id={3} AND recipe_id='{4}' AND step={5} AND procedure_number={6}",
                    columnString, MySqlSchemaName, tableName, recordingID.ToString(), recipeID, step.ToString(), procedure.ToString());

                MySqlCommand mysqlCommand = new MySqlCommand(selectSql, mysqlConnection);
                IAsyncResult iAsync = mysqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                MySqlDataReader mysqlDataReader = mysqlCommand.EndExecuteReader(iAsync);

                while (mysqlDataReader.Read())
                {
                    if (!mysqlDataReader.IsDBNull(0))
                    {
                        motionDataList.Add(mysqlDataReader.GetString(0));

                        if (!mysqlDataReader.IsDBNull(1))
                        {
                            this.handMotionData.Add(mysqlDataReader.GetString(1));
                        }
                        else
                        {
                            this.handMotionData.Add("");
                        }

                        if (!mysqlDataReader.IsDBNull(2))
                        {
                            this.graspedObjectData.Add(mysqlDataReader.GetString(2));
                        }
                        else
                        {
                            this.graspedObjectData.Add("");
                        }

                        if (!mysqlDataReader.IsDBNull(3))
                        {
                            this.collidingObjectData.Add(mysqlDataReader.GetString(3));
                        }
                        else
                        {
                            this.collidingObjectData.Add("");
                        }
                    }
                }

                if (this.handMotionData.Count != 0)
                {
                    this.handMotionData.RemoveAt(0);
                }
                Debug.Log("Got playback motions: " + recordingID);

                mysqlDataReader.Close();
                mysqlCommand.Dispose();
                mysqlConnection.Close();

                return motionDataList;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                return motionDataList;
            }
        }

        public List<string> SelectHandMotions(string recipeID, int step, int procedure, string table)
        {
            MySqlConnection mysqlConnection = null;

            List<string> motionDataList = new List<string>();
            try
            {
                this.handMotionData.Clear();
                this.graspedObjectData.Clear();
                this.collidingObjectData.Clear();

                string connString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password =" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string tableName = MySqlTableName_playbackMotions; //if (table == "participant")
                int recordingID = this.participantID;
                if (table == "expert")
                {
                    tableName = MySqlTableName_expertMotions;
                    recordingID = this.expertID;
                }

                string columnString = "hand_data";

                string selectSql = String.Format("SELECT {0} FROM {1}.{2} WHERE recording_id={3} AND recipe_id='{4}' AND step={5} AND procedure_number={6}",
                    columnString, MySqlSchemaName, tableName, recordingID.ToString(), recipeID, step.ToString(), procedure.ToString());

                MySqlCommand mysqlCommand = new MySqlCommand(selectSql, mysqlConnection);
                IAsyncResult iAsync = mysqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                MySqlDataReader mysqlDataReader = mysqlCommand.EndExecuteReader(iAsync);

                while (mysqlDataReader.Read())
                {
                    if (!mysqlDataReader.IsDBNull(0))
                    {
                        motionDataList.Add(mysqlDataReader.GetString(0));
                    }
                }

                motionDataList.RemoveAt(0);

                Debug.Log("Got hand motion length: " + motionDataList.Count);

                mysqlDataReader.Close();
                mysqlCommand.Dispose();
                mysqlConnection.Close();

                return motionDataList;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                return motionDataList;
            }
        }

        public void InsertPlaybackMotions(string motionHeader, List<string> motionData, List<string> handData, List<string> graspedObjects, string collidingHeader, List<string> collidingObjects, string recipeID, int step, int procedure, string table)
        {
            MySqlConnection mysqlConnection = null;

            try
            {
                string connString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password =" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                // Write user data
                string insertSql = string.Empty;
                Debug.Log(motionHeader);
                CreateInsertQuery_motion(ref insertSql, motionHeader, "LeftHandAnchor\tRightHandAnchor", "InLeftHand,InRightHand", collidingHeader, recipeID, step, procedure, table);

                MySqlCommand mysqlCommand = new MySqlCommand(insertSql, mysqlConnection);
                mysqlCommand.ExecuteNonQuery();

                Debug.Log("Inserted header");

                int cnt = 0;
                insertSql = string.Empty;

                for (int i = 0; i < motionData.Count; i++)
                {
                    CreateInsertQuery_motion(ref insertSql, motionData[i], handData[i], graspedObjects[i], collidingObjects[i], recipeID, step, procedure, table);
                    cnt++;

                    if (cnt == 100)
                    {
                        mysqlCommand = new MySqlCommand(insertSql, mysqlConnection);
                        mysqlCommand.ExecuteNonQuery();

                        cnt = 0;
                        insertSql = string.Empty;
                    }
                }

                if (cnt != 0)
                {
                    mysqlCommand = new MySqlCommand(insertSql, mysqlConnection);
                    mysqlCommand.ExecuteNonQuery();
                }

                Debug.Log("Inserted " + step.ToString() + "-" + procedure.ToString() + " " + motionData.Count.ToString() + " records.");

                mysqlCommand.Dispose();
                mysqlConnection.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        private void CreateInsertQuery_motion(ref string insertSql, string dataString, string handString, string graspingString, string collidingString, string recipeID, int step, int procedure, string table)
        {
            string[] dataStringsArray = dataString.Split("\t".ToCharArray(), 2);
            string[] headerArray = dataStringsArray[0].Split(",".ToCharArray());

            int elapsedTime = (int)(float.Parse(headerArray[0]) * 1000);
            int dataType = (int)(int.Parse(headerArray[1]));

            string tableName = MySqlTableName_playbackMotions; //if (table == "participant")
            int recordingID = this.participantID;
            if (table == "expert")
            {
                tableName = MySqlTableName_expertMotions;
                recordingID = this.expertID;
            }

            string valueString =
                "(" +
                    recordingID + "," +
                    "'" + recipeID + "'," +
                    step.ToString() + "," +
                    procedure.ToString() + "," +
                    elapsedTime.ToString() + "," +
                    dataType.ToString() + "," +
                    "'" + dataString + "'," +
                    "'" + handString + "'," +
                    "'" + graspingString + "'," +
                    "'" + collidingString + "'" +
                ")";

            string columnString = "(recording_id, recipe_id, step, procedure_number, elapsed_time, data_type, motion_data, hand_data, grasped_objects, colliding_objects)";

            if (insertSql == string.Empty)
            {
                insertSql =
                    "INSERT INTO " + MySqlSchemaName + "." + tableName + " " + columnString + " VALUES " + valueString;
            }
            else
            {
                insertSql += "," + valueString;
            }
        }

        public void SelectBehaviorProcedures(string recipeID, int expertID)
        {
            MySqlConnection mysqlConnection = null;

            try
            {
                this.behaviorProcedures.Clear();

                string connectString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password =" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connectString);
                mysqlConnection.Open();

                string columnString = "step, procedure_number, grasped_objects, colliding_objects, behavior_type";

                string selectSql = String.Format("SELECT {0} FROM {1}.{2} WHERE expert_id={3} AND recipe_id='{4}' ORDER BY step, procedure_number", 
                    columnString, MySqlSchemaName, MySqlTableName_behaviorProcedures, expertID.ToString(), recipeID);

                MySqlCommand mysqlCommand = new MySqlCommand(selectSql, mysqlConnection);

                IAsyncResult iAsync = mysqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                
                MySqlDataReader mysqlDataReader = mysqlCommand.EndExecuteReader(iAsync);
                while (mysqlDataReader.Read())
                {
                    string procedureStr = mysqlDataReader.GetString(0);
                    for(int i = 1; i < 5; i++)
                    {
                        procedureStr += "\t" + mysqlDataReader.GetString(i);
                    }

                    this.behaviorProcedures.Add(procedureStr);
                }

                Debug.Log("Got behavior procedure");

                mysqlDataReader.Close();
                mysqlCommand.Dispose();
                mysqlConnection.Close();
            }
            catch (Exception ex)
            {
                Debug.Log(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        public void InsertBehaviorProcedures(int expertID, string recipeID, int step, int procedure, string graspingString, string collidingString, string behaviorType ="")
        {
            MySqlConnection mysqlConnection = null;

            try
            {
                string connString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password =" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connString);
                mysqlConnection.Open();

                string columnString = "(expert_id, recipe_id, step, procedure_number, grasped_objects, colliding_objects, behavior_type)";

                if (graspingString == "")
                {
                    graspingString = null;
                }
                if (collidingString == "")
                {
                    collidingString = null;
                }

                string valueString = 
                "(" +
                    expertID.ToString() + "," +
                    "'" + recipeID + "'," +
                    step.ToString() + "," +
                    procedure.ToString() + "," +
                    "'" + graspingString + "'," +
                    "'" + collidingString + "'," +
                    "'" + behaviorType + "')";

                string insertSql = String.Format("INSERT INTO {0}.{1} {2} VALUES {3}", MySqlSchemaName, MySqlTableName_behaviorProcedures, columnString, valueString);

                MySqlCommand mysqlCommand = new MySqlCommand(insertSql, mysqlConnection);
                mysqlCommand.ExecuteNonQuery();

                Debug.Log("Inserted " + step.ToString() + "-" + procedure.ToString());

                mysqlCommand.Dispose();
                mysqlConnection.Close();
            }
            catch (Exception ex)
            {
                Debug.Log(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        public void SelectBehaviorTypes()
        {
            MySqlConnection mysqlConnection = null;

            try
            {
                this.behaviorType2InstructionMap.Clear();
                this.behaviorTypes.Clear();
                this.processTypes.Clear();
                this.graspTypes.Clear();
                this.collideTypes.Clear();

                string connectString =
                    "server =" + mySqlIP + ";" +
                    "port =" + mySqlPort + ";" +
                    "database =" + MySqlSchemaName + ";" +
                    "userid =" + mySqlUsername + ";" +
                    "password =" + mySqlPassword + ";";

                mysqlConnection = new MySqlConnection(connectString);
                mysqlConnection.Open();

                string selectString = "SELECT * FROM " + MySqlSchemaName + "." + MySqlTableName_behaviorTypes;

                MySqlCommand mysqlCommand = new MySqlCommand(selectString, mysqlConnection);

                IAsyncResult iAsync = mysqlCommand.BeginExecuteReader();
                while (!iAsync.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                MySqlDataReader mysqlDataReader = mysqlCommand.EndExecuteReader(iAsync);
                while (mysqlDataReader.Read())
                {
                    string behaviorType = mysqlDataReader.GetString("behavior_type");

                    this.behaviorType2InstructionMap.Add(behaviorType, mysqlDataReader.GetString("instruction_form"));
                    this.behaviorType2MeaningMap.Add(behaviorType, mysqlDataReader.GetString("meaning"));
                    this.behaviorTypes.Add(behaviorType);

                    if (Boolean.Parse(mysqlDataReader.GetString("is_processed")))
                    {
                        this.processTypes.Add(behaviorType);
                    }
                    if (Boolean.Parse(mysqlDataReader.GetString("is_grasped")))
                    {
                        this.graspTypes.Add(behaviorType);
                    }
                    if (Boolean.Parse(mysqlDataReader.GetString("is_collided")))
                    {
                        this.collideTypes.Add(behaviorType);
                    }
                }

                Debug.Log("Got motion types");

                mysqlDataReader.Close();
                mysqlCommand.Dispose();
                mysqlConnection.Close();
            }
            catch (Exception ex)
            {
                Debug.Log(ex);

                if (mysqlConnection != null)
                {
                    mysqlConnection.Close();
                }
                throw ex;
            }
        }

        public bool GetIsUserNameUsed()
        {
            return this.isUserNameUsed;
        }

        public List<int> GetFinishedTasks()
        {
            return this.finishedTasks;
        }

        public string GetRecipeTitle()
        {
            return this.recipeTitle;
        }

        public List<string> GetRecipeSteps()
        {
            return this.recipeSteps;
        }

        public List<string> GetRecipeIngredients()
        {
            return this.recipeIngredients;
        }

        public List<string> GetRecipeIDs()
        {
            return this.recipeIDs;
        }

        public List<string> GetBehaviorProcedures()
        {
            return this.behaviorProcedures;
        }

        public string GetProcessedFoodString(string recipeID)
        {
            return this.recipeId2ProcessedFoodMap[recipeID];
        }

        public Dictionary<string, string> GetBehaviorInstructionMap()
        {
            return this.behaviorType2InstructionMap;
        }

        public Dictionary<string, string> GetBehaviorMeaningMap()
        {
            return this.behaviorType2MeaningMap;
        }

        public List<string> GetBehaviorTypes()
        {
            return this.behaviorTypes;
        }

        public List<string> GetProcessTypes()
        {
            return this.processTypes;
        }

        public List<string> GetGraspTypes()
        {
            return this.graspTypes;
        }

        public List<string> GetCollideTypes()
        {
            return this.collideTypes;
        }

        public List<string> GetHandData()
        {
            return this.handMotionData;
        }

        public List<string> GetGraspedObjects()
        {
            return this.graspedObjectData;
        }

        public List<string> GetCollidingObjects()
        {
            return this.collidingObjectData;
        }

        public List<string> GetProcedures()
        {
            return this.procedures;
        }

        //public void SelectHandmadeProcedures(string recipeID)
        //{
        //    MySqlConnection mysqlConnection = null;
        //    try
        //    {
        //        this.handmadeExplanationMap.Clear();
        //        this.procedures.Clear();

        //        string connectString =
        //            "server =" + mysqlIpInputField + ";" +
        //            "port =" + mysqlPortInputField + ";" +
        //            "database =" + MySqlSchemaName + ";" +
        //            "userid =" + mysqlUserInputField + ";" +
        //            "password =" + mysqlPassInputField + ";";

        //        mysqlConnection = new MySqlConnection(connectString);
        //        mysqlConnection.Open();

        //        string selectString = "SELECT step, procedure_number, explanation FROM " + MySqlSchemaName + "." + MySqlTableName_handmadeProcedures + " WHERE recipe_id='" + recipeID + "'";

        //        MySqlCommand mysqlCommand = new MySqlCommand(selectString, mysqlConnection);

        //        IAsyncResult iAsync = mysqlCommand.BeginExecuteReader();
        //        while (!iAsync.IsCompleted)
        //        {
        //            Thread.Sleep(100);
        //        }

        //        MySqlDataReader mysqlDataReader = mysqlCommand.EndExecuteReader(iAsync);
        //        while (mysqlDataReader.Read())
        //        {
        //            string procedureStr = mysqlDataReader.GetString(0) + "-" + mysqlDataReader.GetString(1);

        //            string explanation = "";
        //            if (!mysqlDataReader.IsDBNull(2))
        //            {
        //                explanation = mysqlDataReader.GetString(2);
        //            }
        //            this.handmadeExplanationMap.Add(procedureStr, explanation);

        //            this.procedures.Add(procedureStr);
        //        }

        //        Debug.Log("Got a explanation info");

        //        mysqlDataReader.Close();
        //        mysqlCommand.Dispose();
        //        mysqlConnection.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Log(ex);

        //        if (mysqlConnection != null)
        //        {
        //            mysqlConnection.Close();
        //        }
        //        throw ex;
        //    }
        //}

        //public Dictionary<string, string> GetHandmadeExplanationMap()
        //{
        //    return this.handmadeExplanationMap;
        //}
    }
}