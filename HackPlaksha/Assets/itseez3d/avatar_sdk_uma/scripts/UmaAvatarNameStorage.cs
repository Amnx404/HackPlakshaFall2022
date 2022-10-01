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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA
{
	public class UmaAvatarNameStorage 
	{
		private readonly string avatarNameFile = "uma_avatar_name.txt";

		public string LoadUmaAvatarName(string avatarCode)
		{
			string avatarNameFilePath = Path.Combine(AvatarSdkMgr.Storage().GetAvatarDirectory(avatarCode), avatarNameFile);
			if (!File.Exists(avatarNameFilePath))
				return avatarCode;

			string avatarName = File.ReadAllText(avatarNameFilePath);
			if (string.IsNullOrEmpty(avatarName))
				return avatarCode;

			return avatarName;
		}

		public void SaveUmaAvatarName(string avatarCode, string avatarName)
		{
			string avatarNameFilePath = Path.Combine(AvatarSdkMgr.Storage().GetAvatarDirectory(avatarCode), avatarNameFile);
			File.WriteAllText(avatarNameFilePath, avatarName);
		}
	}
}
