// Generated by gencs from std_msgs/UInt16.msg
// DO NOT EDIT THIS FILE BY HAND!

using System;
using System.Collections;
using System.Collections.Generic;
using SIGVerse.RosBridge;
using UnityEngine;


namespace SIGVerse.RosBridge 
{
	namespace std_msgs 
	{
		[System.Serializable]
		public class UInt16 : RosMessage
		{
			public System.UInt16 data;


			public UInt16()
			{
				this.data = 0;
			}

			public UInt16(System.UInt16 data)
			{
				this.data = data;
			}

			new public static string GetMessageType()
			{
				return "std_msgs/UInt16";
			}

			new public static string GetMD5Hash()
			{
				return "1df79edf208b629fe6b81923a544552d";
			}
		} // class UInt16
	} // namespace std_msgs
} // namespace SIGVerse.ROSBridge

