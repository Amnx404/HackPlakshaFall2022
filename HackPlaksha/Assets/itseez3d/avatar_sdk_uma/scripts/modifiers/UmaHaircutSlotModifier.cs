/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, January 2020
*/

using ItSeez3D.AvatarSdk.Core;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UMA;
using UnityEngine;
using UMA.CharacterSystem;
using System;

#if UNITY_EDITOR
using UnityEditor;
using ItSeez3D.AvatarSdk.UMA.Editor;
#endif

namespace ItSeez3D.AvatarSdk.UMA.Modifiers
{
	public class UmaHaircutSlotModifier : IUmaRuntimeAssetModifier
	{
		private readonly string headAdjustBoneName = "HeadAdjust";
		private readonly string neckAdjustBoneName = "NeckAdjust";
		private readonly string neckBoneName = "Neck";

		TexturedMesh haircutMesh;
		string haircutName;

		UMAWardrobeRecipe haircutWardrobeRecipe = null;
		UMAData.UMARecipe haircutRecipe = new UMAData.UMARecipe();
		SlotData haircutSlot = null;
		OverlayData haircutOverlay = null;

		SlotDataAsset haircutSlotAsset;
		UMAMeshData originalMeshData = null;

		OverlayDataAsset haircutOverlayAsset;
		Texture originalTexture = null;
		Material originalMaterial = null;

		UmaTPose tPose = null;

		UmaTPoseModifier tPoseModifier = null;

		public UmaHaircutSlotModifier(string haircutName, UMAWardrobeRecipe haircutWardrobeRecipe, TexturedMesh haircutMesh, string bonesFile, UmaGender gender)
		{
			//Haircut mesh is generated for the character in default UMA T-Pose.
			//So we should use default T-Pose for binding yet in case the custom TPose is used
			string tPoseAssetPath = AvatarSdkUmaStorage.GetUmaTPosePath(gender);
			tPose = Resources.Load<UmaTPose>(tPoseAssetPath);
			tPose.DeSerialize();

			tPoseModifier = new UmaTPoseModifier(tPose, bonesFile);
			tPoseModifier.Modify();

			this.haircutMesh = haircutMesh;
			this.haircutName = haircutName;
			this.haircutWardrobeRecipe = haircutWardrobeRecipe;

			UMAContextBase umaContext = UMAContext.FindInstance();
			this.haircutWardrobeRecipe.Load(haircutRecipe, umaContext);
			haircutSlot = haircutRecipe.slotDataList.FirstOrDefault(s => s != null);
			haircutSlotAsset = haircutSlot.asset;
			originalMeshData = haircutSlotAsset.meshData.DeepCopy();

			haircutOverlay = haircutSlot.GetOverlay(0);
			haircutOverlayAsset = haircutOverlay.asset;
			originalTexture = haircutOverlayAsset.textureList[0];
			originalMaterial = haircutOverlayAsset.material.material;

			UseLitShader = true;
		}

		public void Modify()
		{
			try
			{
				UMAMeshData meshData = haircutSlotAsset.meshData;
				meshData.vertexCount = haircutMesh.mesh.vertexCount;
				meshData.vertices = new Vector3[meshData.vertexCount];
				for (int j = 0; j < haircutMesh.mesh.vertices.Length; j++)
				{
					Vector3 v = haircutMesh.mesh.vertices[j];
					meshData.vertices[j] = new Vector3(-v.x, v.z, v.y);
				}
				meshData.uv = haircutMesh.mesh.uv;
				meshData.submeshes[0].triangles = haircutMesh.mesh.triangles;

				SetSkinning(meshData);

				meshData.normals = haircutMesh.mesh.normals;
				meshData.tangents = new Vector4[0];

				haircutOverlayAsset.textureList[0] = haircutMesh.texture;
				haircutOverlayAsset.material.material = new Material(haircutOverlayAsset.material.material);
				haircutOverlayAsset.material.material = ShadersUtils.ConfigureHaircutMaterial(haircutOverlayAsset.material.material, haircutName, UseLitShader);
			}
			catch(Exception exc)
			{
				Debug.LogErrorFormat("Exception during modifying haircut slot: {0}", exc);
				Revert();
			}
		}

		public void Revert()
		{
			haircutSlotAsset.meshData = originalMeshData;
			haircutOverlayAsset.textureList[0] = originalTexture;
			haircutOverlayAsset.material.material = originalMaterial;

			if (tPoseModifier != null)
				tPoseModifier.Revert();
		}

		public void SaveSlotAndOverlayAssets(string dstSlotAssetPath, string dstOverlayAssetPath, string dstRecipeAssetPath, string dstTextureFile)
		{
#if UNITY_EDITOR
			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(haircutSlotAsset), dstSlotAssetPath);
			SlotDataAsset dstSlotDataAsset = AssetDatabase.LoadAssetAtPath<SlotDataAsset>(dstSlotAssetPath);
			dstSlotDataAsset.slotName = Path.GetFileNameWithoutExtension(dstSlotAssetPath);
			dstSlotDataAsset.nameHash = UMAUtils.StringToHash(dstSlotDataAsset.slotName);
			dstSlotDataAsset.meshData = haircutSlotAsset.meshData;
			UMAAssetIndexer.Instance.EvilAddAsset(typeof(SlotDataAsset), dstSlotDataAsset);
			AssetDatabase.SaveAssets();

			if (File.Exists(dstTextureFile))
				File.Delete(dstTextureFile);
			File.WriteAllBytes(dstTextureFile, haircutMesh.texture.EncodeToPNG());
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(haircutOverlayAsset), dstOverlayAssetPath);
			OverlayDataAsset dstOverlayAsset = AssetDatabase.LoadAssetAtPath<OverlayDataAsset>(dstOverlayAssetPath);
			dstOverlayAsset.overlayName = Path.GetFileNameWithoutExtension(dstOverlayAssetPath);
			dstOverlayAsset.nameHash = UMAUtils.StringToHash(dstOverlayAsset.overlayName);
			dstOverlayAsset.textureList[0] = AssetDatabase.LoadAssetAtPath<Texture2D>(dstTextureFile);
			UMAAssetIndexer.Instance.EvilAddAsset(typeof(OverlayDataAsset), dstOverlayAsset);

			AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(haircutWardrobeRecipe), dstRecipeAssetPath);
			UMAWardrobeRecipe dstRecipeAsset = AssetDatabase.LoadAssetAtPath<UMAWardrobeRecipe>(dstRecipeAssetPath);
			haircutSlot.asset = dstSlotDataAsset;
			haircutOverlay.asset = dstOverlayAsset;
			UmaEditorUtils.SerializeModificationsInObject(dstRecipeAsset, "recipeString", () =>
			{
				dstRecipeAsset.Save(haircutRecipe, UMAContext.FindInstance());
			});
			UMAAssetIndexer.Instance.EvilAddAsset(typeof(UMAWardrobeRecipe), dstRecipeAsset);

			//Add the recipe to the DynamicCharacterSystem to be able use it right now if the play mode is active
			if (Application.isPlaying)
			{
				UMAContext umaContext = UMAContext.FindInstance() as UMAContext;
				umaContext.dynamicCharacterSystem.AddRecipe(dstRecipeAsset);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
#else
			Debug.LogError("SaveSlotAndOverlayAssets method works only in the Unity Editor.");
#endif
		}

		public bool UseLitShader { get; set; }

		private void SetSkinning(UMAMeshData meshData)
		{
			meshData.boneNameHashes = new int[] { UMAUtils.StringToHash(headAdjustBoneName), UMAUtils.StringToHash(neckAdjustBoneName) };
			AddNeckAdjustBone(meshData);

			Matrix4x4 headAdjustMat = UmaUtils.GetGlobalMatrix(meshData.boneNameHashes[0], meshData.umaBones, tPose);
			Matrix4x4 neckAdjustMat = UmaUtils.GetGlobalMatrix(meshData.boneNameHashes[1], meshData.umaBones, tPose);
			Matrix4x4 headMat = UmaUtils.GetGlobalMatrix(UMAUtils.StringToHash("Head"), meshData.umaBones, tPose);

			//Model is rotate on 90 degrees around the X, so we should compare Z coordinate, not Y
			float headPosZ = headMat.GetColumn(3).z;
			float neckAdjustPosZ = neckAdjustMat.GetColumn(3).z;

			meshData.boneWeights = new UMABoneWeight[meshData.vertexCount];
			for (int i = 0; i < meshData.vertexCount; i++)
			{
				meshData.boneWeights[i].boneIndex0 = 0;
				meshData.boneWeights[i].boneIndex1 = 1;
				meshData.boneWeights[i].boneIndex2 = 0;
				meshData.boneWeights[i].boneIndex3 = 0;

				Vector3 v = meshData.vertices[i];
				float headAdjBoneWeight = 0;
				float neckAdjBoneWeight = 0;
				float heightFraction = headPosZ - neckAdjustPosZ;
				if (v.z > headPosZ)
				{
					headAdjBoneWeight = 1;
				}
				else if (v.z < neckAdjustPosZ)
				{
					neckAdjBoneWeight = 1;
				}
				else
				{
					headAdjBoneWeight = (v.z - neckAdjustPosZ) / heightFraction;
					neckAdjBoneWeight = (headPosZ - v.z) / heightFraction;
				}

				meshData.boneWeights[i].weight0 = headAdjBoneWeight;
				meshData.boneWeights[i].weight1 = neckAdjBoneWeight;
				meshData.boneWeights[i].weight2 = 0;
				meshData.boneWeights[i].weight3 = 0;
			}

			meshData.bindPoses = new Matrix4x4[] { headAdjustMat.inverse, neckAdjustMat.inverse };
		}

		private void AddNeckAdjustBone(UMAMeshData meshData)
		{
			if (meshData.umaBones.FirstOrDefault(b => b.name == neckAdjustBoneName) == null)
			{
				UMATransform neckAdjustTransform = new UMATransform()
				{
					name = neckAdjustBoneName,
					hash = UMAUtils.StringToHash(neckAdjustBoneName),
					parent = UMAUtils.StringToHash(neckBoneName),
					position = Vector3.zero,
					rotation = Quaternion.identity,
					scale = Vector3.one
				};
				List<UMATransform> bones = meshData.umaBones.ToList();
				bones.Add(neckAdjustTransform);
				meshData.umaBones = bones.ToArray();
			}
		}
	}
}
