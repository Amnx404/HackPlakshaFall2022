/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, December 2019
*/

using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdk.UMA.Modifiers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ItSeez3D.AvatarSdk.UMA
{
	public class UmaAvatarManager : MonoBehaviour
	{
		public string avatarCode = "";

		public string haircutName = "";

		public Shader bodyShader;

		public bool useLitShaderForHaircut = true;

		public DynamicCharacterAvatar dynamicCharacterAvatar;

		private HaircutRecoloring haircutRecoloring = null;

		private Texture2D haircutTexture = null;

		/// <summary>
		/// Only a single avatar can be modified at a time. This mutex allows to synchronize modification order for multiple avatars.
		/// </summary>
		private static AutoResetEvent modificationMutex = new AutoResetEvent(true);

		private void Start()
		{
			if (!string.IsNullOrEmpty(avatarCode))
			{
				dynamicCharacterAvatar.CharacterCreated.AddAction(data => StartCoroutine(ModifyAvatarOnStartAsync(data)));
			}

			dynamicCharacterAvatar.CharacterUpdated.AddAction(data =>
			{
				if (bodyShader != null)
				{
					SkinnedMeshRenderer meshRenderer = dynamicCharacterAvatar.umaData.GetRenderer(0);
					Material[] materials = meshRenderer.materials;
					for (int i = 0; i < materials.Length; i++)
					{
						if (materials[i].name.Contains("UMA_Mat_Diffuse_Normal_Metallic"))
							materials[i].shader = bodyShader;
					}
					meshRenderer.materials = materials;
				}
			});
		}

		public event Action<string, string> OnAvatarModified;

		/// <summary>
		/// Modifies DynamicCharacterAvatar with the current parameters
		/// </summary>
		public void ModifyAvatar()
		{
			if (!string.IsNullOrEmpty(avatarCode))
				StartCoroutine(ModifyAvatarAsync(avatarCode, haircutName, !string.IsNullOrEmpty(haircutName)));
		}

		/// <summary>
		/// Modifies DynamicCharacterAvatar with the provided parameters
		/// </summary>
		/// <param name="avatarCode">avatar code</param>
		/// <param name="haircutName">haircut name</param>
		/// <param name="removeCurrentHaircut">If True, the haircut wardrobe recipe will be removed</param>
		public IEnumerator ModifyAvatarAsync(string avatarCode, string haircutName, bool removeCurrentHaircut = true)
		{
			CoroutineExecutionResult modificationResult = new CoroutineExecutionResult();
			yield return CoroutineUtils.AwaitCoroutine(ModifyAvatarRoutine(avatarCode, haircutName, removeCurrentHaircut), modificationResult);
			if (!modificationResult.success)
			{
				Debug.LogErrorFormat("Unable to modify UMA character: {0}", modificationResult.exception);
				modificationMutex.Set();
			}
		}

		public void SetHaircutRecoloring(HaircutRecoloring haircutRecoloring)
		{
			this.haircutRecoloring = haircutRecoloring;
			ConfigureHaircutRecoloring(Color.clear);
		}

		public void SetHaircutRecoloring(HaircutRecoloring haircutRecoloring, Color currentColor)
		{
			this.haircutRecoloring = haircutRecoloring;
			ConfigureHaircutRecoloring(currentColor);
		}

		private IEnumerator ModifyAvatarRoutine(string avatarCode, string haircutName, bool removeCurrentHaircut = true)
		{
			while (!modificationMutex.WaitOne(0))
			{
				Debug.Log("Waiting other avatar modification finished");
				yield return null;
			}

			this.avatarCode = avatarCode;
			this.haircutName = haircutName;

			UmaSlotNames slotNames = UmaSlotNames.GetSlotNamesForRace(dynamicCharacterAvatar.activeRace.data);
			if (slotNames == null)
			{
				Debug.LogErrorFormat("Race {0} isn't supported by Avatar SDK", dynamicCharacterAvatar.activeRace.name);
				yield break;
			}
			
			UmaGender gender = UmaUtils.GetGenderForRace(dynamicCharacterAvatar.activeRace.name);

			SlotData headSlot = UmaUtils.FindSlotByName(dynamicCharacterAvatar, slotNames.HeadSlotName);
			SlotData eyesSlot = UmaUtils.FindSlotByName(dynamicCharacterAvatar, slotNames.EyesSlotName);
			SlotData mouthSlot = UmaUtils.FindSlotByName(dynamicCharacterAvatar, slotNames.MouthSlotName);
			OverlayData bodyOverlay = UmaUtils.FindOverlayByName(dynamicCharacterAvatar, slotNames.BodyOverlayName);

			List<IUmaRuntimeAssetModifier> slotModifiers = new List<IUmaRuntimeAssetModifier>();
			string avatarDirPath = AvatarSdkMgr.Storage().GetAvatarDirectory(avatarCode);
			Vector3[] vertices = UmaUtils.ReadVerticesFromBonesFile(AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDirPath, UmaAvatarFile.BONES_DATA));

			string bonesFilePath = AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDirPath, UmaAvatarFile.BONES_DATA);
			var tPoseModifier = new UmaTPoseModifier(dynamicCharacterAvatar.activeRace.racedata.TPose, bonesFilePath);
			tPoseModifier.Modify();

			slotModifiers.Add(new UmaSlotModifier(gender, SlotType.Head, headSlot, AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDirPath, UmaAvatarFile.HEAD_TEXTURE), vertices));
			slotModifiers.Add(new UmaSlotModifier(gender, SlotType.Eyes, eyesSlot, AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDirPath, UmaAvatarFile.EYES_TEXTURE), vertices));
			slotModifiers.Add(new UmaSlotModifier(gender, SlotType.Mouth, mouthSlot, string.Empty, vertices));
			slotModifiers.Add(new UmaOverlayModifier(bodyOverlay, AvatarSdkUmaStorage.GetUmaAvatarFile(avatarDirPath, UmaAvatarFile.BODY_TEXTURE)));

			if (removeCurrentHaircut)
				dynamicCharacterAvatar.ClearSlot("Hair");

			if (!string.IsNullOrEmpty(haircutName))
			{
				var haircutMeshRequest = CoreTools.LoadHaircutFromDiskAsync(avatarCode, haircutName);
				yield return haircutMeshRequest;
				if (!haircutMeshRequest.IsError)
				{
					haircutTexture = haircutMeshRequest.Result.texture;

					UMAWardrobeRecipe haircutTextRecipe = AvatarSdkUmaStorage.GetHaircutRecipeTemplate(dynamicCharacterAvatar.activeRace.name);
					if (haircutTextRecipe != null)
					{
						var haircutModifier = new UmaHaircutSlotModifier(haircutName, haircutTextRecipe, haircutMeshRequest.Result, bonesFilePath, gender);
						haircutModifier.UseLitShader = useLitShaderForHaircut;
						slotModifiers.Add(haircutModifier);
						dynamicCharacterAvatar.SetSlot(haircutTextRecipe);
					}
					else
					{
						Debug.LogErrorFormat("Haircut template recipe was not found! Please add {0} files to the UMA Global Library.", AvatarSdkUmaStorage.GetDirectoryPath(UmaDirectory.TEMPLATES));
					}
				}
				else
				{
					Debug.LogErrorFormat("Unable to load haircut: {0}", haircutMeshRequest.ErrorMessage);
				}
			}

			slotModifiers.ForEach(m => m.Modify());

			dynamicCharacterAvatar.CharacterUpdated.AddAction(d =>
			{
				slotModifiers.ForEach(m => m.Revert());
				tPoseModifier.Revert();
				ConfigureHaircutRecoloring(Color.clear);
				if (OnAvatarModified != null)
					OnAvatarModified(avatarCode, haircutName);
				modificationMutex.Set();
			});

			dynamicCharacterAvatar.BuildCharacter();
			dynamicCharacterAvatar.ForceUpdate(true, true, true);
		}

		private IEnumerator ModifyAvatarOnStartAsync(UMAData data)
		{
			dynamicCharacterAvatar.gameObject.SetActive(false);
			yield return new WaitForEndOfFrame();
			yield return ModifyAvatarAsync(avatarCode, haircutName, !string.IsNullOrEmpty(haircutName));
			dynamicCharacterAvatar.gameObject.SetActive(true);
		}

		private void ConfigureHaircutRecoloring(Color currentColor)
		{
			if (haircutRecoloring != null && dynamicCharacterAvatar.umaData != null)
			{
				if (dynamicCharacterAvatar.umaData.rendererCount > 0)
				{
					SkinnedMeshRenderer avatarMeshRenderer = dynamicCharacterAvatar.umaData.GetRenderer(0);
					foreach (Material material in avatarMeshRenderer.materials)
					{
						if (material.shader.name == ShadersUtils.haircutSolidLitShaderName ||
							material.shader.name == ShadersUtils.haircutStrandLitShaderName ||
							material.shader.name == ShadersUtils.haircutSolidUnlitShaderName ||
							material.shader.name == ShadersUtils.haircutStrandUnlitShaderName)
						{
							if (currentColor == Color.clear)
								haircutRecoloring.ResetHaircutMaterial(material, haircutTexture, Color.clear);
							else
								haircutRecoloring.ResetHaircutMaterial(material, haircutTexture, Color.clear, currentColor);
							break;
						}
					}
				}
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UmaAvatarManager))]
	public class UmaAvatarManagerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			if (Application.isPlaying)
			{
				var umaAvatarManager = (UmaAvatarManager)target;
				if (!string.IsNullOrEmpty(umaAvatarManager.avatarCode))
				{
					if (GUILayout.Button("Update Character"))
						umaAvatarManager.ModifyAvatar();
				}
			}
		}
	}
#endif
}
