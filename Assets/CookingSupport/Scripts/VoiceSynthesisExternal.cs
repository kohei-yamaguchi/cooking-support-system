using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SIGVerse.Common;

namespace CookingSupport
{
    public interface IReceiveTTSHandler : IEventSystemHandler
    {
        void OnReceiveTTSMessage(string msg);
    }

    public class VoiceSynthesisExternal : MonoBehaviour, IReceiveTTSHandler
    {
        private const string SpeechResultCancelled = "Cancelled";
        private const string SpeechResultStarted = "Started";
        private const string SpeechResultFinished = "Finished";

        [HeaderAttribute("SAPI")]
        public string path = "/../TTS/ConsoleSimpleTTS.exe";
        public string language = "411";
        public string gender = "Female";

        [HeaderAttribute("Guidance message param")]
        public int maxCharcters = 400;

        private List<GameObject> notificationDestinations;

        bool isSpeaking;

        private System.Diagnostics.Process speechProcess;

        // Use this for initialization
        private void Awake()
        {
            this.speechProcess = new System.Diagnostics.Process();

            this.speechProcess.StartInfo.FileName = Application.dataPath + this.path;

            this.speechProcess.StartInfo.CreateNoWindow = true;
            this.speechProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            SIGVerseLogger.Info("Text-To-Speech: " + this.speechProcess.StartInfo.FileName);

            this.isSpeaking = false;
        }

        //public void Start()
        //{
        //}

        private void Update()
        {
            if (this.isSpeaking)
            {
                if (this.speechProcess.HasExited)
                {
                    this.isSpeaking = false;
                }
            }
        }

        public bool SpeakMessage(string message)
        {
            if (this.isSpeaking)
            {
                SIGVerseLogger.Info("Text-To-Speech: isSpeaking");

                try
                {
                    if (this.speechProcess.HasExited)
                    {
                        this.speechProcess.Kill();
                    }
                }
                catch (Exception)
                {
                    SIGVerseLogger.Warn("Do nothing even if an error occurs");
                    // Do nothing even if an error occurs
                }

            }

            string truncatedMessage;
            if (message.Length > maxCharcters)
            {
                truncatedMessage = message.Substring(0, maxCharcters);
                SIGVerseLogger.Info("Length of guidance message is over " + this.maxCharcters.ToString() + " charcters.");
            }
            else
            {
                truncatedMessage = message;
            }

            // speak
            string settings = "Language=" + this.language + "; Gender=" + this.gender;
            this.speechProcess.StartInfo.Arguments = "\"" + truncatedMessage + "\" \"" + settings + "\"";

            this.speechProcess.Start();

            this.isSpeaking = true;

            return true;
        }

        public bool IsSpeaking()
        {
            return this.isSpeaking;
        }

        public void OnReceiveTTSMessage(string msg)
        {
            this.SpeakMessage(msg);
        }
    }
}