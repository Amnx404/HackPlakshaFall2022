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
using System.IO;
using UMA;
using UMA.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA
{
	public class AvatarAssets
	{
		public RaceData avatarRace = null;
		public UmaTPose avatarTPose = null;
		public UMATextRecipe avatarRecipe = null;
		public OverlayDataAsset avatarHeadOverlayAsset = null;
		public OverlayDataAsset avatarEyesOverlayAsset = null;
		public OverlayDataAsset avatarBodyOverlayAsset = null;
		public SlotDataAsset avatarHeadSlotAsset = null;
		public SlotDataAsset avatarEyesSlotAsset = null;
		public SlotDataAsset avatarMouthSlotAsset = null;
		public List<HaircutAssets> haircutsAssets = new List<HaircutAssets>();

		private UMAContextBase umaContext = null;

		private IAvatarProvider avatarProvider = null;

		public AvatarAssets(UMAContextBase umaContext, IAvatarProvider avatarProvider, string avatarName)
		{
			this.umaContext = umaContext;
			this.avatarProvider = avatarProvider;
			IsEmpty = true;
			DetectExistedAssets(avatarName);
		}

		public bool IsEmpty { get; private set; }

		public IEnumerator GenerateUmaAssets(string avatarCode, string avatarName, List<string> haircutsNames, TemplateAssets templateAssets)
		{
			EditorUtility.DisplayProgressBar("Generating UMA assets...", "", 0);
			yield return CoroutineUtils.AwaitCoroutine(GenerateUmaAssetsRoutine(avatarCode, avatarName, haircutsNames, templateAssets));
			EditorUtility.ClearProgressBar();
		}

		public IEnumerator GenerateUmaAssetsRoutine(string avatarCode, string avatarName, List<string> haircutsNames, TemplateAssets templateAssets)
		{
			string avatarDataDirectory = AvatarSdkMgr.Storage().GetAvatarDirectory(avatarCode);

			string generatedUmaDataPath = AvatarSdkUmaStorage.GetUmaAssetsDirectoryForAvatar(avatarName);
			UmaEditorUtils.CreateProjectDirectory(AvatarSdkUmaStorage.RemoveRootAssetFolderFromPath(generatedUmaDataPath));

			// Create a copy of the template recipe
			avatarRecipe = UmaEditorUtils.CopyRecipe(templateAssets.recipe, AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.RECIPE));

			//Create a copy of the template race and replace the base recipe to the created one
			avatarRace = UmaEditorUtils.CopyRace(templateAssets.race, AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.RACE), avatarRecipe);

			//Call this method to immediately update races in the Race Library
			UMAContext.Instance.AddRace(avatarRace);

			{//Create a T-Pose
				UmaTPoseModifier tPoseModifier = new UmaTPoseModifier(avatarRace.TPose, AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDataDirectory, UmaAvatarFile.BONES_DATA));
				try
				{
					tPoseModifier.Modify();
					avatarTPose = tPoseModifier.SaveModifiedTPose(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.T_POSE));
				}
				finally
				{
					tPoseModifier.Revert();
				}

				UmaEditorUtils.SerializeModificationsInObject(avatarRace, "TPose", () =>
				{
					avatarRace.TPose = avatarTPose;
				});
			}

			UMAData.UMARecipe recipe = new UMAData.UMARecipe();
			avatarRecipe.Load(recipe, umaContext);
			recipe.raceData = avatarRace;

			//Replace slots and overlays in recipe
			UmaSlotNames slotNames = UmaSlotNames.GetSlotNamesForRace(templateAssets.race);
			UmaGender gender = UmaUtils.GetGenderForRace(templateAssets.race.name);
			Vector3[] vertices = UmaUtils.ReadVerticesFromBonesFile(AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDataDirectory, UmaAvatarFile.BONES_DATA));

			{ // head
				SlotData headSlot = UmaUtils.FindSlotByName(recipe, slotNames.HeadSlotName);
				UmaSlotModifier headSlotModifier = new UmaSlotModifier(gender, SlotType.Head, headSlot,
					AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDataDirectory, UmaAvatarFile.HEAD_TEXTURE), vertices);
				try
				{
					headSlotModifier.Modify();
					avatarHeadOverlayAsset = headSlotModifier.SaveModifiedOverlayAsset(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.HEAD_TEXTURE),
						AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.HEAD_OVERLAY));
					avatarHeadSlotAsset = headSlotModifier.SaveModifiedSlotAsset(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.HEAD_SLOT));
					UmaEditorUtils.ChangeOverlayInSlot(headSlot, avatarHeadOverlayAsset, recipe.sharedColors[0]);
					headSlot.asset = avatarHeadSlotAsset;
				}
				finally
				{
					headSlotModifier.Revert();
				}
			}

			{ // eyes
				SlotData eyesSlot = UmaUtils.FindSlotByName(recipe, slotNames.EyesSlotName);
				UmaSlotModifier eyesSlotModifier = new UmaSlotModifier(gender, SlotType.Eyes, eyesSlot,
					AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDataDirectory, UmaAvatarFile.EYES_TEXTURE), vertices);
				try
				{
					eyesSlotModifier.Modify();
					avatarEyesOverlayAsset = eyesSlotModifier.SaveModifiedOverlayAsset(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.EYES_TEXTURE),
						AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.EYES_OVERLAY));
					avatarEyesSlotAsset = eyesSlotModifier.SaveModifiedSlotAsset(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.EYES_SLOT));
					UmaEditorUtils.ChangeOverlayInSlot(eyesSlot, avatarEyesOverlayAsset, null, true);
					eyesSlot.asset = avatarEyesSlotAsset;
				}
				finally
				{
					eyesSlotModifier.Revert();
				}
			}

			{ // mouth
				SlotData mouthSlot = UmaUtils.FindSlotByName(recipe, slotNames.MouthSlotName);
				UmaSlotModifier mouthSlotModifier = new UmaSlotModifier(gender, SlotType.Mouth, mouthSlot, string.Empty, vertices);
				try
				{
					mouthSlotModifier.Modify();
					avatarMouthSlotAsset = mouthSlotModifier.SaveModifiedSlotAsset(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.MOUTH_SLOT));
					mouthSlot.asset = avatarMouthSlotAsset;
				}
				finally
				{
					mouthSlotModifier.Revert();
				}
			}

			{ // body
				SlotData bodySlot = UmaUtils.FindSlotByName(recipe, slotNames.BodySlotName);
				OverlayData bodyOverlay = UmaUtils.FindOverlayByName(recipe, slotNames.BodyOverlayName);
				UmaOverlayModifier bodyOverlayModifier = new UmaOverlayModifier(bodyOverlay, AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDataDirectory, UmaAvatarFile.BODY_TEXTURE));
				try
				{
					bodyOverlayModifier.Modify();
					avatarBodyOverlayAsset = bodyOverlayModifier.SaveModifiedOverlayAsset(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.BODY_TEXTURE),
						AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, UmaAssetFile.BODY_OVERLAY));
					UmaEditorUtils.ChangeOverlayInSlot(bodySlot, avatarBodyOverlayAsset, recipe.sharedColors[0]);
				}
				finally
				{
					bodyOverlayModifier.Revert();
				}
			}

			UmaEditorUtils.SerializeModificationsInObject(avatarRecipe, "recipeString", () =>
			{
				avatarRecipe.Save(recipe, umaContext);
			});
			UMAAssetIndexer.Instance.ForceSave();

			IsEmpty = false;

			if (haircutsNames != null && haircutsNames.Count > 0)
				yield return GenerateHaircuts(avatarCode, avatarName, haircutsNames, templateAssets.haircutRecipe, gender);
		}

		private IEnumerator GenerateHaircuts(string avatarCode, string avatarName, List<string> haircutsNames, UMAWardrobeRecipe templateHaircutRecipe, UmaGender gender)
		{
			string avatarHaircutsDirectory = AvatarSdkUmaStorage.GetUmaHaircutsAssetDirectoryForAvatar(avatarName);
			UmaEditorUtils.CreateProjectDirectory(AvatarSdkUmaStorage.RemoveRootAssetFolderFromPath(avatarHaircutsDirectory));

			for (int i = 0; i < haircutsNames.Count; i++)
			{
				EditorUtility.DisplayProgressBar("Generating haircuts assets...", haircutsNames[i], ((float)i) / haircutsNames.Count);
				HaircutAssets haircutAsset = haircutsAssets.Find(a => a.ShortHaircutName == CoreTools.GetShortHaircutId(haircutsNames[i]));
				if (haircutAsset == null)
				{
					haircutAsset = new HaircutAssets(haircutsNames[i], avatarProvider);
					haircutsAssets.Add(haircutAsset);
				}
				// We would like to safely end execution in case of exception
				yield return CoroutineUtils.AwaitCoroutine(haircutAsset.GenerateUmaAssets(avatarCode, avatarName, templateHaircutRecipe, gender));
				
			}
		}

		private T LoadAsset<T>(string avatarName, UmaAssetFile umaAssetFile) where T : UnityEngine.Object
		{
			T asset = AssetDatabase.LoadAssetAtPath<T>(AvatarSdkUmaStorage.GetUmaAssetFile(avatarName, umaAssetFile));
			if (asset != null)
				IsEmpty = false;
			return asset;
		}

		private void DetectExistedAssets(string avatarName)
		{
			avatarRace = LoadAsset<RaceData>(avatarName, UmaAssetFile.RACE);
			avatarTPose = LoadAsset<UmaTPose>(avatarName, UmaAssetFile.T_POSE);
			avatarRecipe = LoadAsset<UMATextRecipe>(avatarName, UmaAssetFile.RECIPE);
			avatarHeadOverlayAsset = LoadAsset<OverlayDataAsset>(avatarName, UmaAssetFile.HEAD_OVERLAY);
			avatarEyesOverlayAsset = LoadAsset<OverlayDataAsset>(avatarName, UmaAssetFile.EYES_OVERLAY);
			avatarBodyOverlayAsset = LoadAsset<OverlayDataAsset>(avatarName, UmaAssetFile.BODY_OVERLAY);
			avatarHeadSlotAsset = LoadAsset<SlotDataAsset>(avatarName, UmaAssetFile.HEAD_SLOT);
			avatarEyesSlotAsset = LoadAsset<SlotDataAsset>(avatarName, UmaAssetFile.EYES_SLOT);
			avatarMouthSlotAsset = LoadAsset<SlotDataAsset>(avatarName, UmaAssetFile.MOUTH_SLOT);

			haircutsAssets.Clear();
			string haircutsDirectory = AvatarSdkUmaStorage.GetUmaHaircutsAssetDirectoryForAvatar(avatarName);
			if (AssetDatabase.IsValidFolder(haircutsDirectory))
			{
				foreach (string haircutDir in AssetDatabase.GetSubFolders(haircutsDirectory))
				{
					string haircutName = Path.GetFileName(haircutDir);
					HaircutAssets hairAssets = new HaircutAssets(haircutName, avatarProvider);
					hairAssets.DetectExistedAssets(avatarName);
					haircutsAssets.Add(hairAssets);
				}
			}
		}
	}
}
#endif
