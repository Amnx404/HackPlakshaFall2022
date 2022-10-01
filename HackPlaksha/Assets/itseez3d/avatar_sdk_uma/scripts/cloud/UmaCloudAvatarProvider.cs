/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, December 2019
*/

using ItSeez3D.AvatarSdk.Cloud;
using ItSeez3D.AvatarSdk.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA.Cloud
{
	public class UmaCloudAvatarProvider : CloudAvatarProvider, IUmaAvatarProvider
	{
		private readonly string umaBodyTextureName = "body_texture";
		private readonly string umaEyesTextureName = "eyes_texture";

		UmaAvatarNameStorage nameStorage = new UmaAvatarNameStorage();

		#region IUmaAvatarProvider
		public AsyncRequest MoveUmaAvatarDataToLocalStorage(string avatarCode)
		{
			return DownloadAndSaveUmaAvatarDataAsync(avatarCode);
		}

		public AsyncRequest MoveHaircutDataToLocalStorage(string avatarCode, string haircutId)
		{
			return DownloadAndSaveHaircutDataAsync(avatarCode, haircutId);
		}

		public string LoadUmaAvatarName(string avatarCode)
		{
			return nameStorage.LoadUmaAvatarName(avatarCode);
		}

		public void SaveUmaAvatarName(string avatarCode, string avatarName)
		{
			nameStorage.SaveUmaAvatarName(avatarCode, avatarName);
		}

		#endregion

		public AsyncRequest DownloadAndSaveUmaAvatarDataAsync(string avatarCode)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str(Strings.DownloadingAvatar));
			AvatarSdkMgr.SpawnCoroutine(DownloadAndSaveUmaAvatarDataFunc(avatarCode, request));
			return request;
		}

		public AsyncRequest DownloadAndSaveUmaHeadTextureAsync(AvatarData avatarData)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str(Strings.DownloadingHeadTexture));
			AvatarSdkMgr.SpawnCoroutine(DownloadAndSaveTextureFunc(avatarData, string.Empty, null, request));
			return request;
		}

		public AsyncRequest DownloadAndSaveUmaBodyTextureAsync(AvatarData avatarData)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str("Downloading body texture"));
			AvatarSdkMgr.SpawnCoroutine(DownloadAndSaveTextureFunc(avatarData, umaBodyTextureName, null, request));
			return request;
		}

		public AsyncRequest DownloadAndSaveUmaEyesTextureAsync(AvatarData avatarData)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str("Downloading eyes texture"));
			AvatarSdkMgr.SpawnCoroutine(DownloadAndSaveTextureFunc(avatarData, umaEyesTextureName, null, request));
			return request;
		}

		public AsyncRequest DownloadAndSaveUmaBonesAsync(AvatarData avatarData)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str("Downloading uma bones"));
			AvatarSdkMgr.SpawnCoroutine(DownloadAndSaveUmaBonesFunc(avatarData, request));
			return request;
		}

		private IEnumerator DownloadAndSaveUmaAvatarDataFunc(string avatarCode, AsyncRequest request)
		{
			var avatarDataRequest = GetAvatarAsync(avatarCode);
			yield return request.AwaitSubrequest(avatarDataRequest, 0.05f);
			if (request.IsError)
				yield break;

			AvatarData avatarData = avatarDataRequest.Result;
			var headTextureRequest = DownloadAndSaveUmaHeadTextureAsync(avatarData);
			var bodyTextureRequest = DownloadAndSaveUmaBodyTextureAsync(avatarData);
			var eyesTextureRequest = DownloadAndSaveUmaEyesTextureAsync(avatarData);
			var bonesRequest = DownloadAndSaveUmaBonesAsync(avatarData);

			yield return request.AwaitSubrequests(1.0f, headTextureRequest, bodyTextureRequest, eyesTextureRequest, bonesRequest);
			if (request.IsError)
				yield break;

			request.IsDone = true;
		}

		private IEnumerator DownloadAndSaveUmaBonesFunc(AvatarData avatarData, AsyncRequest request)
		{
			var bonesRequest = connection.DownloadUmaBonesDataAsync(avatarData);
			yield return request.AwaitSubrequest(bonesRequest, 0.95f);
			if (request.IsError)
				yield break;

			string bonesFilename = AvatarSdkMgr.Storage().GetAvatarDirectory(avatarData.code);
			ZipUtils.Unzip(bonesRequest.Result, bonesFilename);

			request.IsDone = true;
		}
	}
}
