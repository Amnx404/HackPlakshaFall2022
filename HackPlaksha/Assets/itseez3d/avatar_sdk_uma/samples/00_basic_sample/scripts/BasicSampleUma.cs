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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;
using ItSeez3D.AvatarSdkSamples.Core;
using UMA.Examples;
using UMA.CharacterSystem;
using System.Linq;
using ItSeez3D.AvatarSdkSamples.Core.WebCamera;
#if UNITY_EDITOR
using ItSeez3D.AvatarSdk.UMA.Editor;
#endif

namespace ItSeez3D.AvatarSdk.UMA.Samples
{
	public class BasicSampleUma : MonoBehaviour
	{
		private class DisplayedAvatarInfo
		{
			public DisplayedAvatarInfo(UmaAvatarManager umaAvatarManager)
			{
				this.umaAvatarManager = umaAvatarManager;
			}

			public UmaAvatarManager umaAvatarManager;
			public string avatarCode;
			public List<string> haircuts = new List<string>();
			public int currentHaircut;
			public Color haircutColor = Color.clear;
		}

		#region Constants
		private const string BALD_HAIRCUT_NAME = "bald";
		private const string GENERATED_HAIRCUT_NAME = "base/generated";
		#endregion

		public SdkType sdkType = SdkType.Offline;

		public UmaAvatarManager[] avatarManagers;

		public GameObject controlsHolder;
		public Button randomPhotoButton;
		public Button userPhotoButton;
		public Button cameraButton;
		public Button umaAssetsButton;
		public Text progressText;
		public Image photoPreview;
		public Toggle haircutsCompatibilityToggle;
		public Toggle eyesTextureToggle;
		public GameObject haircutsButtonsHolder;
		public Text haircutText;
		public HaircutsSelectingView haircutsSelectingView;
		public GameObject haircutRecoloringPanel;
		public WebCameraCapturer webCameraCapturer;

		public MouseOrbitImproved mouseOrbit;

		public SamplePhotoSupplier photoSupplier;

		private IUmaAvatarProvider umaAvatarProvider;

		List<DisplayedAvatarInfo> avatarsInfoList = new List<DisplayedAvatarInfo>();
		DisplayedAvatarInfo activeAvatarInfo = null;

		private PipelineType currentPipeline = PipelineType.UMA_MALE;

		protected void OnEnable()
		{
			if (!AvatarSdkMgr.IsInitialized)
			{
				AvatarSdkMgr.Init(sdkType: sdkType);
			}
		}

		private void Start()
		{
			for (int i = 0; i < avatarManagers.Length; i++)
				avatarsInfoList.Add(new DisplayedAvatarInfo(avatarManagers[i]));
			activeAvatarInfo = avatarsInfoList[0];

#if UNITY_EDITOR || UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS
			userPhotoButton.gameObject.SetActive(true);
			FileBrowser fileBrowser = userPhotoButton.GetComponentInChildren<FileBrowser>();
			if (fileBrowser != null)
				fileBrowser.fileHandler = GenerateAvatarFunc;
#endif
			webCameraCapturer.OnPhotoMade += bytes => StartCoroutine(GenerateAvatarFunc(bytes));

			HaircutRecoloring haircutRecoloring = haircutRecoloringPanel.GetComponentInChildren<HaircutRecoloring>();
			foreach(UmaAvatarManager avatarManager in avatarManagers)
			{
				avatarManager.OnAvatarModified += (c, h) => UpdateRecoloringPanelState(avatarManager);
			}

			ResetRecoloringPanelForActiveAvatar(activeAvatarInfo, null);

			StartCoroutine(Initialize());
		}

		public void OnRandomPhotoButtonClick()
		{
			UmaGender gender = UmaUtils.GetGenderForRace(activeAvatarInfo.umaAvatarManager.dynamicCharacterAvatar.activeRace.name);
			byte[] testPhoto = gender == UmaGender.Male ? photoSupplier.GetMalePhoto() : photoSupplier.GetFemalePhoto();
			StartCoroutine(GenerateAvatarFunc(testPhoto));
		}

		public void OnCameraPhotoButtonClick()
		{
			StartCoroutine(GenerateAvatarFromCameraPhotoAsync());
		}

		public void OnAvatarUmaAssetsButtonClick()
		{
#if UNITY_EDITOR
			var window = AvatarSdkUmaWindow.ShowWindow();
			window.SelectAvatar(activeAvatarInfo.avatarCode);
#endif
		}

		public void OnRacesDropdownChanged(int idx)
		{
			DisplayedAvatarInfo prevAvatarInfo = activeAvatarInfo;
			activeAvatarInfo = avatarsInfoList[idx];
			UmaAvatarManager activeAvatarManager = avatarManagers[idx];
			currentPipeline = UmaUtils.GetPipelineTypeForRace(activeAvatarManager.dynamicCharacterAvatar.activeRace.name);
			mouseOrbit.target = activeAvatarManager.dynamicCharacterAvatar.transform;
			haircutsButtonsHolder.SetActive(!string.IsNullOrEmpty(activeAvatarInfo.avatarCode));
			if (activeAvatarInfo.haircuts.Count > 0)
			{
				haircutText.text = activeAvatarInfo.haircuts[activeAvatarInfo.currentHaircut];
				haircutsSelectingView.InitItems(activeAvatarInfo.avatarCode, activeAvatarInfo.haircuts, umaAvatarProvider);
			}
			if (!string.IsNullOrEmpty(activeAvatarInfo.avatarCode))
				StartCoroutine(SampleUtils.DisplayPhotoPreview(activeAvatarInfo.avatarCode, photoPreview));
			ResetRecoloringPanelForActiveAvatar(activeAvatarInfo, prevAvatarInfo);
		}

		public void OnPrevHaircutButtonClick()
		{
			activeAvatarInfo.currentHaircut--;
			if (activeAvatarInfo.currentHaircut < 0)
				activeAvatarInfo.currentHaircut = activeAvatarInfo.haircuts.Count - 1;
			StartCoroutine(ShowActiveAvatarAsync());
		}

		public void OnNextHaircutButtonClick()
		{
			activeAvatarInfo.currentHaircut++;
			if (activeAvatarInfo.currentHaircut >= activeAvatarInfo.haircuts.Count)
				activeAvatarInfo.currentHaircut = 0;
			StartCoroutine(ShowActiveAvatarAsync());
		}

		public void OnHaircutsListButtonClick()
		{
			controlsHolder.SetActive(false);
			haircutsSelectingView.Show(new List<string>() { activeAvatarInfo.haircuts[activeAvatarInfo.currentHaircut] }, list =>
			{
				controlsHolder.SetActive(true);
				string selectedHaircutName = list[0];
				int haircutIdx = activeAvatarInfo.haircuts.IndexOf(selectedHaircutName);
				if (haircutIdx >= 0)
				{
					activeAvatarInfo.currentHaircut = haircutIdx;
					StartCoroutine(ShowActiveAvatarAsync());
				}
			});
		}

		public void DocumentationButtonClick()
		{
			DocumentationHelper.OpenDocumentationInBrowser("integration_with_uma.html", Flavour.CLOUD);
		}

		private IEnumerator Initialize()
		{
			SetControlsInteractable(false);

			//Create and intialize avatar provider
			umaAvatarProvider = UmaAvatarProviderFactory.CreateUmaAvatarProvider(sdkType);
			yield return Await(umaAvatarProvider.InitializeAsync());

			var isUmaPipelineAvailableRequest = umaAvatarProvider.IsPipelineSupportedAsync(currentPipeline);
			yield return Await(isUmaPipelineAvailableRequest);
			if (isUmaPipelineAvailableRequest.IsError)
				yield break;

			progressText.text = string.Empty;
			if (isUmaPipelineAvailableRequest.Result)
				SetControlsInteractable(true);
			else
				progressText.text = "You can't generate uma avatars.\nThis option is available on the PRO subscription plan.";
		}

		private IEnumerator GenerateAvatarFromCameraPhotoAsync()
		{
			string photoPath = string.Empty;
#if UNITY_ANDROID
			AndroidImageSupplier imageSupplier = new AndroidImageSupplier();
			yield return imageSupplier.CaptureImageFromCameraAsync();
			photoPath = imageSupplier.FilePath;
#elif UNITY_IOS
			IOSImageSupplier imageSupplier = IOSImageSupplier.Create();
			yield return imageSupplier.CaptureImageFromCameraAsync();
			photoPath = imageSupplier.FilePath;
#else
			webCameraCapturer.gameObject.SetActive(true);
#endif
			if (string.IsNullOrEmpty(photoPath))
				yield break;
			byte[] bytes = File.ReadAllBytes(photoPath);
			yield return GenerateAvatarFunc(bytes);
		}

		private IEnumerator GenerateAvatarFunc(byte[] photoBytes)
		{
			SetControlsInteractable(false);
			photoPreview.gameObject.SetActive(false);

			var parametersRequest = umaAvatarProvider.GetParametersAsync(ComputationParametersSubset.ALL, currentPipeline);
			yield return Await(parametersRequest);
			ComputationParameters parameters = ComputationParameters.Empty;
			parameters.haircuts = parametersRequest.Result.haircuts;
			parameters.avatarModifications = parametersRequest.Result.avatarModifications;
			parameters.avatarModifications.allowModifyVertices.Value = haircutsCompatibilityToggle.isOn;
			parameters.avatarModifications.parametricEyesTexture.Value = eyesTextureToggle.isOn;
			parameters.avatarModifications.removeSmile.Value = true;
			parameters.avatarModifications.removeGlasses.Value = true;
			parameters.avatarModifications.enhanceLighting.Value = true;

			var initializeRequest = umaAvatarProvider.InitializeAvatarAsync(photoBytes, "name", "description", currentPipeline, parameters);
			yield return Await(initializeRequest);
			activeAvatarInfo.avatarCode = initializeRequest.Result;

			StartCoroutine(SampleUtils.DisplayPhotoPreview(activeAvatarInfo.avatarCode, photoPreview));

			var calculateRequest = umaAvatarProvider.StartAndAwaitAvatarCalculationAsync(activeAvatarInfo.avatarCode);
			yield return Await(calculateRequest);

			var downloadingRequest = umaAvatarProvider.MoveUmaAvatarDataToLocalStorage(activeAvatarInfo.avatarCode);
			yield return Await(downloadingRequest);

			var haircutsListRequest = umaAvatarProvider.GetHaircutsIdAsync(activeAvatarInfo.avatarCode);
			yield return Await(haircutsListRequest);
			UpdateAvailableHaircutsList(activeAvatarInfo, haircutsListRequest.Result);
			haircutsSelectingView.InitItems(activeAvatarInfo.avatarCode, activeAvatarInfo.haircuts, umaAvatarProvider);

			yield return ShowActiveAvatarAsync();

			UpdateAssetsButtonState();
			haircutsButtonsHolder.SetActive(true);
			SetControlsInteractable(true);
		}

		private IEnumerator ShowActiveAvatarAsync()
		{
			SetControlsInteractable(false);
			string haircutName = activeAvatarInfo.currentHaircut == 0 ? string.Empty : activeAvatarInfo.haircuts[activeAvatarInfo.currentHaircut];
			if (!string.IsNullOrEmpty(haircutName))
			{
				var haircutDownloadRequest = umaAvatarProvider.MoveHaircutDataToLocalStorage(activeAvatarInfo.avatarCode, haircutName);
				yield return Await(haircutDownloadRequest);
			}

			yield return activeAvatarInfo.umaAvatarManager.ModifyAvatarAsync(activeAvatarInfo.avatarCode, haircutName);

			haircutText.text = activeAvatarInfo.haircuts[activeAvatarInfo.currentHaircut];

			SetControlsInteractable(true);
		}

		private void UpdateAvailableHaircutsList(DisplayedAvatarInfo avatarInfo, string[] availableHaircuts)
		{
			avatarInfo.haircuts.Clear();
			avatarInfo.haircuts.Add(BALD_HAIRCUT_NAME);
			avatarInfo.currentHaircut = 0;
			if (availableHaircuts != null && availableHaircuts.Length > 0)
			{
				avatarInfo.haircuts.AddRange(availableHaircuts);
				if (avatarInfo.haircuts.Contains(GENERATED_HAIRCUT_NAME))
				{
					avatarInfo.haircuts.Remove(GENERATED_HAIRCUT_NAME);
					avatarInfo.haircuts.Insert(1, GENERATED_HAIRCUT_NAME);
					avatarInfo.currentHaircut = 1;
				}
			}
		}

		private IEnumerator Await(params AsyncRequest[] requests)
		{
			foreach (var r in requests)
				while (!r.IsDone)
				{
					// yield null to wait until next frame (to avoid blocking the main thread)
					yield return null;

					// This function will throw on any error. Such primitive error handling only provided as
					// an example, the production app probably should be more clever about it.
					if (r.IsError)
					{
						Debug.LogError(r.ErrorMessage);
						progressText.text = r.ErrorMessage;
						SetControlsInteractable(true);
						throw new Exception(r.ErrorMessage);
					}

					// Each requests may or may not contain "subrequests" - the asynchronous subtasks needed to
					// complete the request. The progress for the requests can be tracked overall, as well as for
					// every subtask. The code below shows how to recursively iterate over current subtasks
					// to display progress for them.
					var progress = new List<string>();
					progress.Add(string.Format("{0}: {1}%", r.State, r.ProgressPercent.ToString("0.0")));
					if (r.CurrentSubrequest != null)
					{
						progress.Add(string.Format("{0}: {1}%", r.CurrentSubrequest.State, r.CurrentSubrequest.ProgressPercent.ToString("0.0")));
					}
					progressText.text = string.Join("\n", progress.ToArray());
				}

			progressText.text = string.Empty;
		}

		private void SetControlsInteractable(bool interactable)
		{
			var controls = controlsHolder.GetComponentsInChildren<Selectable>();

			foreach (var c in controls)
				c.interactable = interactable;
		}

		private void UpdateAssetsButtonState()
		{
#if UNITY_EDITOR
			umaAssetsButton.gameObject.SetActive(!string.IsNullOrEmpty(activeAvatarInfo.avatarCode));
#endif
		}

		private void UpdateRecoloringPanelState(UmaAvatarManager umaAvatarManager)
		{
			if (activeAvatarInfo.umaAvatarManager == umaAvatarManager)
				haircutRecoloringPanel.SetActive(!string.IsNullOrEmpty(umaAvatarManager.haircutName));
		}

		private void ResetRecoloringPanelForActiveAvatar(DisplayedAvatarInfo currentAvatarInfo, DisplayedAvatarInfo previousAvatarInfo)
		{
			HaircutRecoloring haircutRecoloring = haircutRecoloringPanel.GetComponentInChildren<HaircutRecoloring>();
			if (previousAvatarInfo != null)
				previousAvatarInfo.haircutColor = haircutRecoloring.CurrentColor;

			for (int i = 0; i < avatarManagers.Length; i++)
				avatarManagers[i].SetHaircutRecoloring(null);
			currentAvatarInfo.umaAvatarManager.SetHaircutRecoloring(haircutRecoloring, currentAvatarInfo.haircutColor);

			UpdateRecoloringPanelState(currentAvatarInfo.umaAvatarManager);
		}
	}
}
