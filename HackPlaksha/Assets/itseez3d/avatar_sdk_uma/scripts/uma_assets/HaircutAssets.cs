/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, December 2019
*/

#if UNITY_EDITOR
using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdk.UMA.Editor;
using ItSeez3D.AvatarSdk.UMA.Modifiers;
using System;
using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA
{
	public class HaircutAssets
	{
		public Texture2D texture = null;
		public OverlayDataAsset overlayAsset = null;
		public SlotDataAsset slotAsset = null;
		public UMAWardrobeRecipe wardrobeRecipe = null;

		private IAvatarProvider avatarProvider = null;

		public HaircutAssets(string haircutName, IAvatarProvider avatarProvider)
		{
			HaircutName = haircutName;
			ShortHaircutName = CoreTools.GetShortHaircutId(haircutName);
			this.avatarProvider = avatarProvider;
		}

		public string HaircutName { get; private set; }

		public string ShortHaircutName { get; private set; }

		public IEnumerator GenerateUmaAssets(string avatarCode, string avatarName, UMAWardrobeRecipe templateHaircutRecipe, UmaGender gender)
		{
			string haircutDirectory = AvatarSdkUmaStorage.GetUmaHaircutAssetDirectoryForAvatar(avatarName, HaircutName);
			UmaEditorUtils.CreateProjectDirectory(AvatarSdkUmaStorage.RemoveRootAssetFolderFromPath(haircutDirectory));

			var haircutRequest = avatarProvider.GetHaircutMeshAsync(avatarCode, HaircutName);
			yield return haircutRequest;
			if (haircutRequest.IsError)
				yield break;

			string avatarDirPath = AvatarSdkMgr.Storage().GetAvatarDirectory(avatarCode);
			string bonesFilePath = AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDirPath, UmaAvatarFile.BONES_DATA);
			UmaHaircutSlotModifier haircutSlotModifier = new UmaHaircutSlotModifier(HaircutName, templateHaircutRecipe, haircutRequest.Result, bonesFilePath, gender);
			try
			{
				haircutSlotModifier.Modify();

				string textureFilePath = AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.TEXTURE);
				string overlayPath = AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.OVERLAY);
				string slotPath = AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.SLOT);
				string recipePath = AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.RECIPE);
				haircutSlotModifier.SaveSlotAndOverlayAssets(slotPath, overlayPath, recipePath, textureFilePath);
				haircutSlotModifier.Revert();

				UMAAssetIndexer.Instance.ForceSave();

				texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFilePath);
				overlayAsset = AssetDatabase.LoadAssetAtPath<OverlayDataAsset>(overlayPath);
				slotAsset = AssetDatabase.LoadAssetAtPath<SlotDataAsset>(slotPath);
				wardrobeRecipe = AssetDatabase.LoadAssetAtPath<UMAWardrobeRecipe>(recipePath);
			}
			catch(Exception exc)
			{
				Debug.LogErrorFormat("Exception during generating haircut asset: {0}", exc);
				haircutSlotModifier.Revert();
			}
		}

		public void DetectExistedAssets(string avatarName)
		{
			texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.TEXTURE));
			overlayAsset = AssetDatabase.LoadAssetAtPath<OverlayDataAsset>(AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.OVERLAY));
			slotAsset = AssetDatabase.LoadAssetAtPath<SlotDataAsset>(AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.SLOT));
			wardrobeRecipe = AssetDatabase.LoadAssetAtPath<UMAWardrobeRecipe>(AvatarSdkUmaStorage.GetUmaHaircutFile(avatarName, HaircutName, UmaHaircutAssetFile.RECIPE));
		}
	}
}
#endif
