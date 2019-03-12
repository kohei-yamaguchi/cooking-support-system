// Generated by gencs from turtlesim/TeleportAbsolute.srv
// DO NOT EDIT THIS FILE BY HAND!

using System;
using System.Collections;
using System.Collections.Generic;
using SIGVerse.RosBridge;
using UnityEngine;


namespace SIGVerse.RosBridge 
{
	namespace turtlesim 
	{
		[System.Serializable]
		public class TeleportAbsoluteRequest : ServiceArgs
		{
			public float x;
			public float y;
			public float theta;


			public TeleportAbsoluteRequest()
			{
				this.x = 0.0f;
				this.y = 0.0f;
				this.theta = 0.0f;
			}

			public TeleportAbsoluteRequest(float x, float y, float theta)
			{
				this.x = x;
				this.y = y;
				this.theta = theta;
			}

			new public static string GetMessageType()
			{
				return "turtlesim/TeleportAbsoluteRequest";
			}

			new public static string GetMD5Hash()
			{
				return "a130bc60ee6513855dc62ea83fcc5b20";
			}
		} // class TeleportAbsoluteRequest
	} // namespace turtlesim
} // namespace SIGVerse.ROSBridge
