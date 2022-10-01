/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, January 2020
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UMA;
using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ItSeez3D.AvatarSdk.UMA.Modifiers
{
	public class UmaTPoseModifier : IUmaRuntimeAssetModifier
	{
		UmaTPose tPose = null;
		List<SkeletonBone> originalBones = null;
		string bonesFile = string.Empty;

		public UmaTPoseModifier(UmaTPose tPose, string bonesFile)
		{
			this.bonesFile = bonesFile;
			this.tPose = tPose;
			tPose.DeSerialize();
			originalBones = tPose.boneInfo.ToList();
		}

		public void Modify()
		{
			try
			{
				List<SkeletonBone> bones = UmaUtils.ReadBones(bonesFile);
				ModifyTPose(tPose, bones);
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Exception during modifying TPose asset: {0}", exc);
				Revert();
			}
		}

		public void Revert()
		{
			tPose.boneInfo = originalBones.ToArray();
		}

		public UmaTPose GetModifiedTPose()
		{
			return tPose;
		}

		public UmaTPose SaveModifiedTPose(string dstTPoseAssetPath)
		{
#if UNITY_EDITOR
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(tPose), dstTPoseAssetPath);
			UmaTPose avatarTPose = AssetDatabase.LoadAssetAtPath<UmaTPose>(dstTPoseAssetPath);
			avatarTPose.DeSerialize();
			avatarTPose.boneInfo = tPose.boneInfo;
			avatarTPose.name = Path.GetFileNameWithoutExtension(dstTPoseAssetPath);
			avatarTPose.Serialize();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return avatarTPose;
#else
			Debug.LogError("SaveModifiedTPose works only in the Unity Editor.");
			return null;
#endif
		}

		private void ModifyTPose(UmaTPose tPose, List<SkeletonBone> bones)
		{
			for (int i = 0; i < tPose.boneInfo.Length; i++)
			{
				SkeletonBone bone = bones.Find(b => b.name == tPose.boneInfo[i].name);
				//Don't apply SpineAdjust bone because it modifies the body as well
				if (!string.IsNullOrEmpty(bone.name) && bone.name != "SpineAdjust")
					tPose.boneInfo[i] = bone;
			}
		}
	}
}
