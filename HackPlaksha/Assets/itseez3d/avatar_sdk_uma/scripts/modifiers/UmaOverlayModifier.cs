/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, February 2020
*/

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
	public class UmaOverlayModifier : IUmaRuntimeAssetModifier
	{
		private OverlayDataAsset overlayDataAsset = null;
		private Texture originalTexture = null;

		private string textureFile = string.Empty;

		public UmaOverlayModifier(OverlayData overlay, string textureFile)
		{
			this.textureFile = textureFile;

			if (overlay != null)
			{
				overlayDataAsset = overlay.asset;
				originalTexture = overlayDataAsset.textureList[0];
			}
			else
				Debug.LogWarning("Provided OverlayData is empty.");
		}

		public void Modify()
		{
			try
			{
				if (!string.IsNullOrEmpty(textureFile) && overlayDataAsset != null)
				{
					Texture2D texture = new Texture2D(2, 2);
					texture.LoadImage(File.ReadAllBytes(textureFile));
					overlayDataAsset.textureList[0] = texture;
				}
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Exception during modifying overlay asset: {0}", exc);
				Revert();
			}
		}

		public Texture GetModifiedTexture()
		{
			return overlayDataAsset.textureList[0];
		}

		public void Revert()
		{
			if (overlayDataAsset != null)
				overlayDataAsset.textureList[0] = originalTexture;
		}

		public OverlayDataAsset SaveModifiedOverlayAsset(string dstTextureFile, string dstOverlayAssetPath)
		{
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(textureFile) && !string.IsNullOrEmpty(dstTextureFile) && !string.IsNullOrEmpty(dstOverlayAssetPath))
			{
				if (File.Exists(dstTextureFile))
					File.Delete(dstTextureFile);
				byte[] textureBytes = File.ReadAllBytes(textureFile);
				File.WriteAllBytes(dstTextureFile, textureBytes);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(overlayDataAsset), dstOverlayAssetPath);
				OverlayDataAsset overlayAsset = AssetDatabase.LoadAssetAtPath<OverlayDataAsset>(dstOverlayAssetPath);
				overlayAsset.overlayName = Path.GetFileNameWithoutExtension(dstOverlayAssetPath);
				overlayAsset.nameHash = UMAUtils.StringToHash(overlayAsset.overlayName);
				overlayAsset.textureList[0] = AssetDatabase.LoadAssetAtPath<Texture2D>(dstTextureFile);
				UMAAssetIndexer.Instance.EvilAddAsset(typeof(OverlayDataAsset), overlayAsset);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				return overlayAsset;
			}
#else
			Debug.LogError("SaveModifiedAssets method works only in the Unity Editor.");
#endif
			return null;
		}
	}
}
