// Generated by gencs from diagnostic_msgs/SelfTest.srv
// DO NOT EDIT THIS FILE BY HAND!

using System;
using System.Collections;
using System.Collections.Generic;
using SIGVerse.RosBridge;
using UnityEngine;

using SIGVerse.RosBridge.diagnostic_msgs;

namespace SIGVerse.RosBridge 
{
	namespace diagnostic_msgs 
	{
		[System.Serializable]
		public class SelfTestResponse : ServiceResponse
		{
			public string id;
			public sbyte passed;
			public System.Collections.Generic.List<diagnostic_msgs.DiagnosticStatus>  status;


			public SelfTestResponse()
			{
				this.id = "";
				this.passed = 0;
				this.status = new System.Collections.Generic.List<diagnostic_msgs.DiagnosticStatus>();
			}

			public SelfTestResponse(string id, sbyte passed, System.Collections.Generic.List<diagnostic_msgs.DiagnosticStatus>  status)
			{
				this.id = id;
				this.passed = passed;
				this.status = status;
			}

			new public static string GetMessageType()
			{
				return "diagnostic_msgs/SelfTestResponse";
			}

			new public static string GetMD5Hash()
			{
				return "ac21b1bab7ab17546986536c22eb34e9";
			}
		} // class SelfTestResponse
	} // namespace diagnostic_msgs
} // namespace SIGVerse.ROSBridge

