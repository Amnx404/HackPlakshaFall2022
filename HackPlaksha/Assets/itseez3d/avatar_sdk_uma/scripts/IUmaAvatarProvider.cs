/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, February 2020
*/

using ItSeez3D.AvatarSdk.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA
{
	public interface IUmaAvatarProvider : IAvatarProvider
	{
		AsyncRequest MoveUmaAvatarDataToLocalStorage(string avatarCode);

		AsyncRequest MoveHaircutDataToLocalStorage(string avatarCode, string haircutId);

		void SaveUmaAvatarName(string avatarCode, string currentAvatarName);

		string LoadUmaAvatarName(string avatarCode);
	}

	public static class UmaAvatarProviderFactory
	{
		private static Dictionary<SdkType, string> instanceNames = new Dictionary<SdkType, string>()
		{
			{ SdkType.Cloud, "ItSeez3D.AvatarSdk.UMA.Cloud.UmaCloudAvatarProvider" },
			{ SdkType.Offline, "ItSeez3D.AvatarSdk.UMA.Offline.UmaOfflineAvatarProvider" }
		};


		public static IUmaAvatarProvider CreateUmaAvatarProvider(SdkType sdkType)
		{
			string className = instanceNames[sdkType];
			Assembly assembly = Assembly.GetExecutingAssembly();
			Type implType = assembly.GetType(className);
			if (implType == null)
			{
				Debug.LogErrorFormat("Unable to create instance of: {0}", implType);
				return null;
			}
			return (IUmaAvatarProvider)Activator.CreateInstance(implType);
		}
	}
}
