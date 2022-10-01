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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UMA;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ItSeez3D.AvatarSdk.UMA.Modifiers
{
	public enum SlotType
	{
		Head,
		Eyes,
		Mouth
	}

	public class UmaSlotModifier : IUmaRuntimeAssetModifier
	{
		SlotDataAsset slotDataAsset = null;
		UMAMeshData originalMeshData = null;

		UmaGender gender;
		SlotType slotType;

		Vector3[] fullbodyModelVertices;

		UmaOverlayModifier overlayModifier = null;

		public UmaSlotModifier(UmaGender gender, SlotType slotType, SlotData slot, string textureFile, Vector3[] fullbodyModelVertices)
		{
			this.gender = gender;
			this.slotType = slotType;
			this.fullbodyModelVertices = fullbodyModelVertices;

			if (slot != null)
			{
				slotDataAsset = slot.asset;
				originalMeshData = slotDataAsset.meshData.DeepCopy();

				if (!string.IsNullOrEmpty(textureFile))
				{
					OverlayData overlay = slot.GetOverlay(0);
					overlayModifier = new UmaOverlayModifier(overlay, textureFile);
				}
			}
			else
				Debug.LogWarningFormat("Provided SlotData is empty for {0} BodySlotType", slotType);
		}

		public void Modify()
		{
			try
			{
				if (slotDataAsset != null)
				{
					ModifySlotVertices(fullbodyModelVertices, slotDataAsset.meshData.vertices);
					if (overlayModifier != null)
					{
						overlayModifier.Modify();

						if (slotType == SlotType.Eyes)
							UpdateEyesUVmapping();
					}
				}
			}
			catch(Exception exc)
			{
				Debug.LogErrorFormat("Exception during modifying body slot: {0}", exc);
				Revert();
			}
		}

		public void Revert()
		{
			if (originalMeshData != null && slotDataAsset != null)
				slotDataAsset.meshData = originalMeshData;

			if (overlayModifier != null)
				overlayModifier.Revert();
		}

		public OverlayDataAsset SaveModifiedOverlayAsset(string dstTextureFile, string dstOverlayAssetPath)
		{
			if (overlayModifier != null)
				return overlayModifier.SaveModifiedOverlayAsset(dstTextureFile, dstOverlayAssetPath);
			else
			{
				Debug.LogWarningFormat("Unable to save overlay asset: {0}. Modifier is null.", dstOverlayAssetPath);
				return null;
			}
		}

		public SlotDataAsset SaveModifiedSlotAsset(string dstSlotAssetPath)
		{
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(dstSlotAssetPath))
			{
				AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(slotDataAsset), dstSlotAssetPath);
				SlotDataAsset dstSlotDataAsset = AssetDatabase.LoadAssetAtPath<SlotDataAsset>(dstSlotAssetPath);
				dstSlotDataAsset.slotName = Path.GetFileNameWithoutExtension(dstSlotAssetPath);
				dstSlotDataAsset.nameHash = UMAUtils.StringToHash(dstSlotDataAsset.slotName);
				dstSlotDataAsset.meshData = slotDataAsset.meshData;
				UMAAssetIndexer.Instance.EvilAddAsset(typeof(SlotDataAsset), dstSlotDataAsset);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				return dstSlotDataAsset;
			}
#else
			Debug.LogError("SaveModifiedAssets method works only in the Unity Editor.");
#endif
			return null;
		}

		private void ModifySlotVertices(Vector3[] fullbodyVertices, Vector3[] slotVertices)
		{
			TextAsset asset = Resources.Load(GetIndicesAssetName()) as TextAsset;
			using (BinaryReader reader = new BinaryReader(new MemoryStream(asset.bytes)))
			{
				int countVertices = reader.ReadInt32();
				if (slotVertices.Length != countVertices)
				{
					Debug.LogErrorFormat("Invalid slot vertices count: {0} vs {1}", countVertices, slotVertices.Length);
					return;
				}

				int countTemplateVertices = reader.ReadInt32();
				if (fullbodyVertices.Length < countTemplateVertices)
				{
					Debug.LogErrorFormat("Invalid template vertices count: {0} vs {1}", countTemplateVertices, fullbodyVertices.Length);
					return;
				}

				for (int i = 0; i < countVertices; i++)
				{
					int vertexIdx = reader.ReadInt32();
					Vector3 v = fullbodyVertices[vertexIdx];
					slotVertices[i] = new Vector3(-v.x, v.z, v.y);
				}
			}
		}

		private void UpdateEyesUVmapping()
		{
			// UV mapping should be changed in case non parametric texture is used (eyes from the photo).
			// It can be detected by the texture size (1024x512 vs 512x512)
			Texture eyesTexture = overlayModifier.GetModifiedTexture();
			if (eyesTexture.width == 1024)
			{
				Vector2[] uv = slotDataAsset.meshData.uv;
				for (int i = 0; i < slotDataAsset.meshData.vertices.Length; i++)
				{
					if (slotDataAsset.meshData.vertices[i].x > 0)
						uv[i].x = uv[i].x / 2.0f;
					else
						uv[i].x = 0.5f + uv[i].x / 2.0f;
				}
				slotDataAsset.meshData.uv = uv;
			}
		}

		private string GetIndicesAssetName()
		{
			switch (slotType)
			{
				case SlotType.Head: return gender == UmaGender.Male ? "uma_male_head_indices" : "uma_female_head_indices";
				case SlotType.Eyes: return gender == UmaGender.Male ? "uma_male_eyes_indices" : "uma_female_eyes_indices";
				case SlotType.Mouth: return gender == UmaGender.Male ? "uma_male_mouth_indices" : "uma_female_mouth_indices";
				default: return string.Empty;
			}

		}
	}
}
