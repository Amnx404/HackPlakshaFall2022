/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, January 2019
*/

#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UMA;
using UnityEditor;
using UnityEngine;
using System.IO;
using UMA.CharacterSystem;

namespace ItSeez3D.AvatarSdk.UMA.Editor
{
	public class UmaEditorUtils
	{
		public static void SerializeModificationsInObject(UnityEngine.Object obj, string propertyName, Action doModifications)
		{
			SerializedObject serializedObject = new SerializedObject(obj);
			doModifications();
			object propertyValue = obj.GetType().GetField(propertyName).GetValue(obj);

			if (propertyValue is UnityEngine.Object)
			{
				serializedObject.FindProperty(propertyName).objectReferenceValue = (UnityEngine.Object)propertyValue;
			}
			else if (propertyValue is string)
			{
				serializedObject.FindProperty(propertyName).stringValue = (string)propertyValue;
			}
			else
			{
				Debug.LogErrorFormat("Unsupport type to save modifications: {0}", propertyValue.GetType());
			}
			serializedObject.ApplyModifiedProperties();
		}

		public static void ChangeOverlayInSlot(SlotData slotData, OverlayDataAsset overlayDataAsset, OverlayColorData sharedColor = null, bool removeAdditionalOverlays = false)
		{
			OverlayData overlayData = new OverlayData(overlayDataAsset);
			if (sharedColor != null)
				overlayData.colorData = sharedColor;
			if (removeAdditionalOverlays)
				slotData.SetOverlayList(new List<OverlayData> { overlayData });
			else
				slotData.SetOverlay(0, overlayData);
		}

		public static UMATextRecipe CopyRecipe(UMATextRecipe srcRecipe, string dstRecipePath)
		{
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(srcRecipe), dstRecipePath);
			UMATextRecipe dstRecipe = AssetDatabase.LoadAssetAtPath<UMATextRecipe>(dstRecipePath);
			UMAAssetIndexer.Instance.EvilAddAsset(typeof(UMATextRecipe), dstRecipe);
			return dstRecipe;
		}

		public static RaceData CopyRace(RaceData srcRace, string dstRacePath, UMATextRecipe baseRaceRecipe)
		{
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(srcRace), dstRacePath);
			RaceData dstRace = AssetDatabase.LoadAssetAtPath<RaceData>(dstRacePath);
			if (baseRaceRecipe != null)
				dstRace.baseRaceRecipe = baseRaceRecipe;
			dstRace.raceName = Path.GetFileNameWithoutExtension(dstRacePath);

			List<string> crosscompatibleRaces = srcRace.GetCrossCompatibleRaces();
			crosscompatibleRaces.Add(srcRace.name);
			dstRace.SetCrossCompatibleRaces(crosscompatibleRaces);

			UMAAssetIndexer.Instance.EvilAddAsset(typeof(RaceData), dstRace);
			return dstRace;
		}

		public static void AddAvatarSdkTemplatesToGlobalLibrary()
		{
			List<TemplateRaceData> templateRaces = AvatarSdkUmaStorage.GetTemplateRaces();
			foreach(TemplateRaceData raceData in templateRaces)
			{
				var recipe = UMAAssetIndexer.Instance.GetAssetItem<UMATextRecipe>(raceData.recipeAssetName);
				if (recipe == null)
					AddAssetToGlobalLibrary(raceData.recipeAssetFile);

				var race = UMAAssetIndexer.Instance.GetAssetItem<RaceData>(raceData.raceAssetName);
				if (race == null)
					AddAssetToGlobalLibrary(raceData.raceAssetFile);
			}
			UMAAssetIndexer.Instance.ForceSave();
		}

		public static void AddAvatarSdkContentToGlobalLibrary()
		{
			string templatesDir = AvatarSdkUmaStorage.GetDirectoryPath(UmaDirectory.TEMPLATES);
			string generatedDataDir = AvatarSdkUmaStorage.GetDirectoryPath(UmaDirectory.GENERATED_UMA_ASSETS);

			List<string> existedDirectories = new List<string>() { templatesDir };
			if (AssetDatabase.AssetPathToGUID(generatedDataDir) != string.Empty)
				existedDirectories.Add(generatedDataDir);

			string[] guids = AssetDatabase.FindAssets(string.Empty, existedDirectories.ToArray());

			foreach(string assetGuid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
				AddAssetToGlobalLibrary(assetPath);
			}
			UMAAssetIndexer.Instance.ForceSave();
			UMAAssetIndexer.Instance.RebuildIndex();
			Debug.Log("Avatar SDK assets were added to the UMA Global Library!");
		}

		public static void CreateProjectDirectory(string dir)
		{
			string[] folders = dir.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			List<string> existingPath = new List<string> { "Assets" };
			for (int i = 0; i < folders.Length; ++i)
			{
				var prevPathStr = string.Join("/", existingPath.ToArray());
				existingPath.Add(folders[i]);
				var existingPathStr = string.Join("/", existingPath.ToArray());
				if (!AssetDatabase.IsValidFolder(existingPathStr))
					AssetDatabase.CreateFolder(prevPathStr, folders[i]);
				AssetDatabase.SaveAssets();
			}
			AssetDatabase.Refresh();
		}

		private static void AddAssetToGlobalLibrary(string assetPath)
		{
			var assetInstance = AssetDatabase.LoadAssetAtPath(assetPath, typeof(object));
			if (assetInstance != null)
			{
				if (UMAAssetIndexer.Instance.GetTypes().Contains(assetInstance.GetType()))
					UMAAssetIndexer.Instance.AddAsset(assetInstance.GetType(), Path.GetFileNameWithoutExtension(assetPath), assetPath, assetInstance);
			}
		}
	}
}
#endif