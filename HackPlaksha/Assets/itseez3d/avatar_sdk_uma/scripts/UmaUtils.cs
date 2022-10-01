/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, January 2019
*/

using ItSeez3D.AvatarSdk.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA
{
	public class UmaUtils
	{
		public static UmaGender GetGenderForRace(string raceName)
		{
			return raceName.ToLower().Contains("female") ? UmaGender.Female : UmaGender.Male;
		}

		public static PipelineType GetPipelineTypeForRace(string raceName)
		{
			UmaGender gender = GetGenderForRace(raceName);
			return gender == UmaGender.Male ? PipelineType.UMA_MALE : PipelineType.UMA_FEMALE;
		}

		public static SlotData FindSlotByName(UMAData.UMARecipe recipe, string slotName)
		{
			SlotData slotData = recipe.slotDataList.FirstOrDefault(s => s != null && s.slotName == slotName);
			if (slotData == null)
			{
				Debug.LogFormat("Slot {0} not found.", slotName);
				return null;
			}
			return slotData;
		}

		public static SlotData FindSlotByName(DynamicCharacterAvatar avatar, string slotName)
		{
			for (int i = 0; i < avatar.umaData.GetSlotArraySize(); i++)
			{
				SlotData slot = avatar.umaData.GetSlot(i);
				if (slot.slotName == slotName)
					return slot;
			}
			return null;
		}

		public static OverlayData FindOverlayByName(UMAData.UMARecipe recipe, string overlayName)
		{
			for (int i = 0; i < recipe.slotDataList.Length; i++)
			{
				SlotData slot = recipe.slotDataList[i];
				if (slot != null)
				{
					foreach (OverlayData overlayData in slot.GetOverlayList())
					{
						if (overlayData.overlayName == overlayName)
							return overlayData;
					}
				}
			}
			return null;
		}

		public static OverlayData FindOverlayByName(DynamicCharacterAvatar avatar, string overlayName)
		{
			for (int i = 0; i < avatar.umaData.GetSlotArraySize(); i++)
			{
				SlotData slot = avatar.umaData.GetSlot(i);
				foreach(OverlayData overlayData in slot.GetOverlayList())
				{
					if (overlayData.overlayName == overlayName)
						return overlayData;
				}
			}
			return null;
		}

		public static List<SkeletonBone> ReadBones(string poseFile)
		{
			List<SkeletonBone> bones = new List<SkeletonBone>();

			using (FileStream stream = new FileStream(poseFile, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int nodesCount = reader.ReadInt32();
					for (int i = 0; i < nodesCount; i++)
					{
						SkeletonBone bone;
						int nameLength = reader.ReadInt32();
						byte[] nameBytes = reader.ReadBytes(nameLength);
						bone.name = Encoding.UTF8.GetString(nameBytes, 0, nameBytes.Length);

						bone.rotation.x = reader.ReadSingle();
						bone.rotation.y = reader.ReadSingle() * -1;
						bone.rotation.z = reader.ReadSingle() * -1;
						bone.rotation.w = reader.ReadSingle();

						bone.position.x = reader.ReadSingle() * -1;
						bone.position.y = reader.ReadSingle();
						bone.position.z = reader.ReadSingle();

						bone.scale.x = reader.ReadSingle();
						bone.scale.y = reader.ReadSingle();
						bone.scale.z = reader.ReadSingle();

						bones.Add(bone);
					}
				}
			}

			return bones;
		}

		public static Vector3[] ReadVerticesFromBonesFile(string bonesFile)
		{
			Vector3[] vertices = null;
			using (FileStream stream = new FileStream(bonesFile, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int nodesCount = reader.ReadInt32();
					for (int i = 0; i < nodesCount; i++)
					{
						int nameLength = reader.ReadInt32();
						reader.ReadBytes(nameLength);
						reader.ReadBytes(40);
					}

					int verticesCount = reader.ReadInt32();
					vertices = new Vector3[verticesCount];
					for (int i=0; i<verticesCount; i++)
					{
						vertices[i].x = reader.ReadSingle() * -1;
						vertices[i].y = reader.ReadSingle();
						vertices[i].z = reader.ReadSingle();
					}
				}
			}
			return vertices;
		}

		public static Matrix4x4 GetGlobalMatrix(int boneHash, UMATransform[] bones, UmaTPose tPose)
		{
			Matrix4x4 mat = Matrix4x4.identity;
			while (true)
			{
				bool isFound = false;
				for (int i = 0; i < bones.Length; i++)
				{
					if (bones[i].hash == boneHash)
					{
						isFound = true;
						boneHash = bones[i].parent;
						SkeletonBone bone = tPose.boneInfo.FirstOrDefault(b => b.name == bones[i].name);
						mat = Matrix4x4.TRS(bone.position, bone.rotation, bone.scale) * mat;
						break;
					}
				}
				if (!isFound)
					break;
			}

			SkeletonBone globalBone = tPose.boneInfo.FirstOrDefault(b => b.name == "Global");
			mat = Matrix4x4.TRS(globalBone.position, globalBone.rotation, globalBone.scale) * mat;
			mat = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 180.0f), Vector3.one) * mat;
			return mat;
		}

		public static Matrix4x4 GetBindPose(int boneHash, UMATransform[] bones, UmaTPose tPose)
		{
			Matrix4x4 mat = GetGlobalMatrix(boneHash, bones, tPose);
			return mat.inverse;
		}

		public static void PrintMatrix(Matrix4x4 mat)
		{
			Debug.LogWarningFormat("{0}, {1}, {2}, {3}, {4}, {6}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}",
					mat.m00, mat.m01, mat.m02, mat.m03, mat.m10, mat.m11, mat.m12, mat.m13, mat.m20, mat.m21, mat.m22, mat.m23, mat.m30, mat.m31, mat.m32, mat.m33);
		}
	}
}
