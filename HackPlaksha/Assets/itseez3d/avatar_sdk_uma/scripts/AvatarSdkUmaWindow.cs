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
using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdk.UMA.Modifiers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA.Editor
{
	public class AvatarSdkUmaWindow : EditorWindow
	{
		enum WindowState
		{
			NotInitialized,
			Initializing,
			Initialized
		}

		WindowState windowState = WindowState.NotInitialized;

		UMAContextBase umaContext = null;

		IUmaAvatarProvider umaAvatarProvider = null;

		string[] templateRaces = AvatarSdkUmaStorage.GetTemplateRacesNames().ToArray();
		int currentRace = 0;

		TemplateAssets templateAssets;
		AvatarAssets avatarAssets;

		GUIStyle foldoutStyle, greenBoldButtonStyle;
		bool showTemplatesFoldout = false;
		bool showAvatarFoldout = true;
		bool showHaircutsFoldout = false;
		bool showAvatarsListFoldout = false;
		bool showGeneratedAssetsFoldout = false;
		bool showGeneratedHaircutsAssetsFoldout = false;
		List<bool> generatedHaircutsAssetsFoldoutsList = new List<bool>();

		string[] createdUmaAvatars = new string[0];
		int currentAvatarIdx = -1;
		string currentAvatarName = string.Empty;
		Dictionary<string, bool> selectedHaircuts = new Dictionary<string, bool>();

		string requestedAvatarToSelect = string.Empty; 

		Texture2D avatarPhotoPreview = null;

		Vector2 windowScrollPosition = Vector2.zero;
		Vector2 haircutsScrollPosition = Vector2.zero;
		Vector2 avatarsScrollPosition = Vector2.zero;

		private SdkType sdkType = SdkType.Cloud;

		[MenuItem("Window/itSeez3D Avatar SDK/UMA/UMA Avatars")]
		public static AvatarSdkUmaWindow ShowWindow()
		{
			var window = (AvatarSdkUmaWindow)EditorWindow.GetWindow(typeof(AvatarSdkUmaWindow));
			window.titleContent.text = "Avatar SDK UMA";
			window.minSize = new Vector2(480, 250);
			window.Show();
			return window;
		}

		[MenuItem("Window/itSeez3D Avatar SDK/UMA/Force to Update Global Library")]
		public static void ForceToUpdateUmaGlobalLibrary()
		{
			UmaEditorUtils.AddAvatarSdkContentToGlobalLibrary();
		}

		public void SelectAvatar(string avatarCode)
		{
			if (windowState == WindowState.Initialized)
			{
				UpdateCreatedUmaAvatars();
				ChangeSelectedAvatar(avatarCode);
			}
			else
				requestedAvatarToSelect = avatarCode;
		}

		private void InitializeAsync()
		{
			windowState = WindowState.Initializing;
			Coroutines.EditorRunner.instance.Run(Initialize());
		}

		private IEnumerator Initialize()
		{
			if (umaAvatarProvider == null)
			{
				umaAvatarProvider = UmaAvatarProviderFactory.CreateUmaAvatarProvider(sdkType);
				yield return umaAvatarProvider.InitializeAsync();
			}

			UpdateCreatedUmaAvatars();
			if (createdUmaAvatars.Length > 0)
				ChangeSelectedAvatar(string.IsNullOrEmpty(requestedAvatarToSelect) ? createdUmaAvatars[0] : requestedAvatarToSelect);

			umaContext = UMAContext.FindInstance();
			if (umaContext == null)
			{
				UMAContext.CreateEditorContext();
				umaContext = UMAContext.FindInstance();
			}

			UmaEditorUtils.AddAvatarSdkTemplatesToGlobalLibrary();

			avatarAssets = new AvatarAssets(umaContext, umaAvatarProvider, currentAvatarName);
			templateAssets = new TemplateAssets(templateRaces[currentRace]);

			windowState = WindowState.Initialized;
		}

		protected void OnEnable()
		{
			windowState = WindowState.NotInitialized;
			CheckAvatarSdkMgrStatus();
		}

		void OnGUI()
		{
			if (windowState == WindowState.NotInitialized)
			{
				CheckAvatarSdkMgrStatus();
				if (windowState == WindowState.NotInitialized)
				{
					EditorGUILayout.HelpBox("To generate UMA assets in Play mode you should initialize  AvatarSdkMgr (Cloud SDK) on the scene.", MessageType.Error);
					return;
				}
			}

			if (windowState == WindowState.Initializing)
			{
				EditorGUILayout.HelpBox("Initializing Avatar SDK...", MessageType.Info);
				return;
			}

			InitUI();

			windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);
			{
				DisplayTemplatesFoldout();
				GUILayout.Space(10);
				DisplayAvatarFoldout();

				if (createdUmaAvatars.Length > 0)
				{
					GUILayout.Space(10);
					DisplayAvatarsListFoldout();
				}
			}
			EditorGUILayout.EndScrollView();
		}

		protected void InitUI()
		{
			if (foldoutStyle == null)
			{
				foldoutStyle = EditorStyles.foldout;
				foldoutStyle.fontStyle = FontStyle.Bold;
				foldoutStyle.onActive.textColor = foldoutStyle.onNormal.textColor;
				foldoutStyle.onFocused.textColor = foldoutStyle.onNormal.textColor;
				foldoutStyle.active.textColor = foldoutStyle.onNormal.textColor;
				foldoutStyle.focused.textColor = foldoutStyle.onNormal.textColor;
			}
			if (greenBoldButtonStyle == null)
			{
				greenBoldButtonStyle = new GUIStyle("Button");
				greenBoldButtonStyle.normal.textColor = new Color(107f / 255f, 142f / 255f, 35f / 255f);
				greenBoldButtonStyle.fontStyle = FontStyle.Bold;
			}
		}

		private void DisplayPhotoPreview(Texture texture)
		{
			if (texture != null)
			{
				float previewYPosition = 75.0f;
				if (showTemplatesFoldout)
					previewYPosition += 108.0f;

				float previewAspect = (float)texture.height / (float)texture.width;
				int newWidth = (int)Mathf.Min(position.width, texture.width, 150) - 8;

				Vector2 previewSize = new Vector2(-newWidth, (previewAspect * newWidth));
				GUI.DrawTexture(new Rect(new Vector2((position.width - previewSize.x) * 0.5f, previewYPosition), previewSize), texture);
				GUILayout.Space(previewSize.y + 10);
			}
		}

		private void DisplayTemplatesFoldout()
		{
			showTemplatesFoldout = EditorGUILayout.Foldout(showTemplatesFoldout, "Template Assets", foldoutStyle);
			if (showTemplatesFoldout)
			{
				EditorGUILayout.BeginVertical("Box");
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Race name:", GUILayout.Width(145f));
						int selectedTemplateRace = EditorGUILayout.Popup(currentRace, templateRaces);
						if (selectedTemplateRace != currentRace)
							ChangeCurrentTemplateRace(selectedTemplateRace);
					}
					GUILayout.EndHorizontal();

					templateAssets.race = (RaceData)EditorGUILayout.ObjectField("Race:", templateAssets.race, typeof(RaceData), false);
					templateAssets.recipe = (UMATextRecipe)EditorGUILayout.ObjectField("Recipe:", templateAssets.recipe, typeof(UMATextRecipe), false);
					templateAssets.haircutRecipe = (UMAWardrobeRecipe)EditorGUILayout.ObjectField("Haircut Recipe:", templateAssets.haircutRecipe, typeof(UMAWardrobeRecipe), false);

					GUILayout.Space(5);
					if (GUILayout.Button("Force to Update Global Library"))
						UmaEditorUtils.AddAvatarSdkContentToGlobalLibrary();
				}
				EditorGUILayout.EndVertical();
			}
		}

		private void DisplayAvatarFoldout()
		{
			showAvatarFoldout = EditorGUILayout.Foldout(showAvatarFoldout, "Avatar", foldoutStyle);
			if (showAvatarFoldout)
			{
				EditorGUILayout.BeginVertical("Box");
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label("Code:", GUILayout.Width(50f));
						int selectedIdx = EditorGUILayout.Popup(currentAvatarIdx, createdUmaAvatars);
						if (selectedIdx != currentAvatarIdx)
						{
							currentAvatarIdx = selectedIdx;
							ChangeSelectedAvatar(GetCurrentAvatarCode());
						}
						if (GUILayout.Button("Update avatars", GUILayout.Width(100f)))
							UpdateCreatedUmaAvatars();
					}
					EditorGUILayout.EndHorizontal();

					if (currentAvatarIdx >= 0)
					{
						if (avatarPhotoPreview == null)
							UpdateAvatarPhotoPreview(createdUmaAvatars[currentAvatarIdx]);
						DisplayPhotoPreview(avatarPhotoPreview);

						EditorGUILayout.BeginHorizontal();
						{
							GUILayout.Label("Name:", GUILayout.Width(100f));
							string avatarNameModified = EditorGUILayout.TextField(currentAvatarName);
							if (!string.IsNullOrEmpty(avatarNameModified) && avatarNameModified != currentAvatarName)
								currentAvatarName = avatarNameModified;
						}
						EditorGUILayout.EndHorizontal();

						if (selectedHaircuts.Count > 0 || isLoadingHaircuts)
						{
							showHaircutsFoldout = EditorGUILayout.Foldout(showHaircutsFoldout, "Haircuts", foldoutStyle);
							if (showHaircutsFoldout)
							{
								EditorGUILayout.BeginVertical("Box");
								{
									EditorGUILayout.BeginHorizontal();
									{
										if (GUILayout.Button("Select All"))
											ChangeAllHaircutsSelection(true);
										if (GUILayout.Button("Deselect All"))
											ChangeAllHaircutsSelection(false);
									}
									EditorGUILayout.EndHorizontal();

									haircutsScrollPosition = GUILayout.BeginScrollView(haircutsScrollPosition, GUILayout.Height(200));
									{
									
										if (isLoadingHaircuts)
											GUILayout.Label("Loading haircuts...");

										List<string> updatedHaircuts = new List<string>();
										foreach (var haircut in selectedHaircuts)
										{
											bool isSelected = haircut.Value;
											bool newState = GUILayout.Toggle(isSelected, haircut.Key);
											if (newState != isSelected)
												updatedHaircuts.Add(haircut.Key);
										}
										foreach (string haircutName in updatedHaircuts)
											selectedHaircuts[haircutName] = !selectedHaircuts[haircutName];
									
									}
									GUILayout.EndScrollView();
								}
								EditorGUILayout.EndVertical();
							}
						}

						if (GUILayout.Button("Generate UMA Assets"))
							Coroutines.EditorRunner.instance.Run(GenerateUmaAssets());

						if (!avatarAssets.IsEmpty)
						{
							GUILayout.Space(10);
							DisplayGeneratedAssets();
						}
						
					}
					else
					{
						if (createdUmaAvatars == null || createdUmaAvatars.Length == 0)
							EditorGUILayout.HelpBox("There are no UMA avatars yet. \nUse the 00_basic_sample_uma to generate them.", MessageType.Info);
					}
				}
				EditorGUILayout.EndVertical();
				
			}
		}

		private void DisplayGeneratedAssets()
		{
			showGeneratedAssetsFoldout = EditorGUILayout.Foldout(showGeneratedAssetsFoldout, "UMA Assets", foldoutStyle);
			if (showGeneratedAssetsFoldout)
			{
				EditorGUILayout.BeginVertical("Box");
				{
					if (isGeneratingAssets)
					{
						GUILayout.Label("Generating assets...");
					}
					else
					{
						EditorGUILayout.ObjectField("Avatar Race:", avatarAssets.avatarRace, typeof(RaceData), false);
						EditorGUILayout.ObjectField("T-Pose:", avatarAssets.avatarTPose, typeof(UmaTPose), false);
						EditorGUILayout.ObjectField("Avatar Recipe:", avatarAssets.avatarRecipe, typeof(UMATextRecipe), false);
						GUILayout.Space(10);

						EditorGUILayout.ObjectField("Head Overlay:", avatarAssets.avatarHeadOverlayAsset, typeof(OverlayDataAsset), false);
						EditorGUILayout.ObjectField("Eyes Overlay:", avatarAssets.avatarEyesOverlayAsset, typeof(OverlayDataAsset), false);
						EditorGUILayout.ObjectField("Body Overlay:", avatarAssets.avatarBodyOverlayAsset, typeof(OverlayDataAsset), false);
						GUILayout.Space(10);

						EditorGUILayout.ObjectField("Head Slot:", avatarAssets.avatarHeadSlotAsset, typeof(SlotDataAsset), false);
						EditorGUILayout.ObjectField("Eyes Slot:", avatarAssets.avatarEyesSlotAsset, typeof(SlotDataAsset), false);
						EditorGUILayout.ObjectField("Mouth Slot:", avatarAssets.avatarMouthSlotAsset, typeof(SlotDataAsset), false);

						if (avatarAssets.haircutsAssets.Count > 0)
						{
							GUILayout.Space(10);
							showGeneratedHaircutsAssetsFoldout = EditorGUILayout.Foldout(showGeneratedHaircutsAssetsFoldout, "Haircuts", foldoutStyle);
							if (showGeneratedHaircutsAssetsFoldout)
							{
								EditorGUILayout.BeginVertical("Box");
								{
									for (int i = 0; i < avatarAssets.haircutsAssets.Count; i++)
									{
										generatedHaircutsAssetsFoldoutsList[i] = EditorGUILayout.Foldout(generatedHaircutsAssetsFoldoutsList[i],
											avatarAssets.haircutsAssets[i].ShortHaircutName, foldoutStyle);
										if (generatedHaircutsAssetsFoldoutsList[i])
										{
											EditorGUILayout.BeginVertical("Box");
											{
												EditorGUILayout.ObjectField("Texture:", avatarAssets.haircutsAssets[i].texture, typeof(Texture2D), false);
												EditorGUILayout.ObjectField("Overlay:", avatarAssets.haircutsAssets[i].overlayAsset, typeof(OverlayDataAsset), false);
												EditorGUILayout.ObjectField("Slot:", avatarAssets.haircutsAssets[i].slotAsset, typeof(SlotDataAsset), false);
												EditorGUILayout.ObjectField("Wardrobe Recipe:", avatarAssets.haircutsAssets[i].wardrobeRecipe, typeof(UMAWardrobeRecipe), false);
											}
											EditorGUILayout.EndVertical();
										}
									}
								}
								EditorGUILayout.EndVertical();
							}
						}
					}
				}
				EditorGUILayout.EndVertical();
			}
		}

		private void DisplayAvatarsListFoldout()
		{
			showAvatarsListFoldout = EditorGUILayout.Foldout(showAvatarsListFoldout, "Avatars List", foldoutStyle);
			if (showAvatarsListFoldout)
			{
				EditorGUILayout.BeginVertical("Box");
				{
					bool showScroll = createdUmaAvatars.Length > 18;
					if (showScroll)
						avatarsScrollPosition = GUILayout.BeginScrollView(avatarsScrollPosition, GUILayout.Height(400));
					{
						string currentAvatarCode = GetCurrentAvatarCode();
						foreach (string avatarCode in createdUmaAvatars)
						{
							GUILayout.BeginHorizontal();
							{
								GUIStyle buttonStyle = avatarCode == currentAvatarCode ? greenBoldButtonStyle : GUI.skin.button;
								if (GUILayout.Button(avatarCode, buttonStyle, GUILayout.Width(position.width - 150)))
									ChangeSelectedAvatar(avatarCode);
								if (GUILayout.Button("Delete"))
								{
									if (EditorUtility.DisplayDialog("Delete avatar?", "Are you sure you want to delete " + avatarCode + "?", "Yes", "No"))
										Coroutines.EditorRunner.instance.Run(DeleteAvatar(avatarCode));
								}
							}
							GUILayout.EndHorizontal();
						}

						if (createdUmaAvatars.Length > 1 && GUILayout.Button("Delete All"))
						{
							if (EditorUtility.DisplayDialog("Delete all saved avatars?", "Are you sure you want to delete all avatars? ", "Yes", "No"))
								Coroutines.EditorRunner.instance.Run(DeleteAllAvatars());
						}
					}
					if (showScroll)
						GUILayout.EndScrollView();
				}
				EditorGUILayout.EndVertical();
			}
		}

		private void CheckAvatarSdkMgrStatus()
		{
			bool isMgrAvailable = false;
			if (AvatarSdkMgr.IsInitialized && AvatarSdkMgr.GetSdkType() == sdkType)
				isMgrAvailable = true;
			else
			{
				if (!EditorApplication.isPlayingOrWillChangePlaymode)
				{
					if (AvatarSdkMgr.IsInitialized)
						AvatarSdkMgr.ChangeSdkType(sdkType);
					else
						AvatarSdkMgr.Init(sdkType: sdkType);
					isMgrAvailable = true;
				}
			}

			if (isMgrAvailable && windowState == WindowState.NotInitialized)
				InitializeAsync();
		}

		private bool isGeneratingAssets = false;
		private IEnumerator GenerateUmaAssets()
		{
			isGeneratingAssets = true;
			string avatarCode = GetCurrentAvatarCode();
			umaAvatarProvider.SaveUmaAvatarName(avatarCode, currentAvatarName);
			List<string> haircutsToGenerate = new List<string>();
			foreach (var h in selectedHaircuts)
			{
				if (h.Value)
					haircutsToGenerate.Add(h.Key);
			}
			yield return avatarAssets.GenerateUmaAssets(GetCurrentAvatarCode(), currentAvatarName, haircutsToGenerate, templateAssets);
			UpdateGeneratedHaircutsFoldouts();
			isGeneratingAssets = false;
		}

		private void UpdateAvatarPhotoPreview(string avatarCode)
		{
			if (avatarPhotoPreview != null)
			{
				DestroyImmediate(avatarPhotoPreview);
				avatarPhotoPreview = null;
			}

			if (!string.IsNullOrEmpty(avatarCode))
			{
				string photoPath = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.PHOTO);
				if (File.Exists(photoPath))
				{
					byte[] photoBytes = File.ReadAllBytes(photoPath);
					avatarPhotoPreview = new Texture2D(2, 2);
					avatarPhotoPreview.LoadImage(photoBytes);
					avatarPhotoPreview.Apply();
				}
			}
		}

		private int GetSuitableTemplateRaceIdx(UmaGender gender)
		{
			string raceName = AvatarSdkUmaStorage.GetTemplateRaces().FirstOrDefault(r => r.gender == gender).name;

			for (int i=0; i<templateRaces.Length; i++)
			{
				if (templateRaces[i] == raceName)
					return i;
			}

			return 0;
		}

		private void UpdateCreatedUmaAvatars()
		{
			List<string> umaAvatars = new List<string>();
			string avatarsDir = AvatarSdkMgr.Storage().GetAvatarsDirectory();
			string[] avatarDirectories = Directory.GetDirectories(avatarsDir, "*", SearchOption.TopDirectoryOnly);
			foreach (string dir in avatarDirectories)
			{
				string avatarCode = Path.GetFileName(dir);
				if (sdkType == SdkType.Cloud)
				{
					Guid guid = Guid.Empty;
					if (!Utils.TryParseGuid(avatarCode, out guid))
						continue;
				}

				PipelineType pipelineType = CoreTools.LoadPipelineType(avatarCode);
				if (pipelineType == PipelineType.UMA_MALE || pipelineType == PipelineType.UMA_FEMALE)
					umaAvatars.Add(avatarCode);
			}

			string currentAvatarCode = GetCurrentAvatarCode();
			createdUmaAvatars = umaAvatars.ToArray();

			currentAvatarIdx = GetAvatarIdx(currentAvatarCode);
			if (currentAvatarIdx == -1)
				ChangeSelectedAvatar(string.Empty);
		}

		private void ChangeSelectedAvatar(string avatarCode)
		{
			currentAvatarName = umaAvatarProvider.LoadUmaAvatarName(avatarCode);
			currentAvatarIdx = GetAvatarIdx(avatarCode);

			PipelineType pipelineType = CoreTools.LoadPipelineType(avatarCode);
			UmaGender gender = pipelineType == PipelineType.UMA_MALE ? UmaGender.Male : UmaGender.Female;
			ChangeCurrentTemplateRace(GetSuitableTemplateRaceIdx(gender));
			UpdateAvatarPhotoPreview(avatarCode);

			avatarAssets = new AvatarAssets(umaContext, umaAvatarProvider, currentAvatarName);
			UpdateGeneratedHaircutsFoldouts();

			if (!string.IsNullOrEmpty(avatarCode))
				LoadHaircutsAsync(avatarCode);
		}

		private string GetCurrentAvatarCode()
		{
			if (createdUmaAvatars != null && createdUmaAvatars.Length > 0 && currentAvatarIdx >= 0)
				return createdUmaAvatars[currentAvatarIdx];
			else
				return string.Empty;
		}

		private int GetAvatarIdx(string avatarCode)
		{
			for (int i = 0; i < createdUmaAvatars.Length; i++)
			{
				if (createdUmaAvatars[i] == avatarCode)
				{
					return i;
				}
			}
			return -1;
		}

		private void ChangeCurrentTemplateRace(int idx)
		{
			currentRace = idx;
			templateAssets = new TemplateAssets(templateRaces[currentRace]);
		}

		private bool isLoadingHaircuts = false;
		private IEnumerator haircutsLoadingCoroutine = null;
		private void LoadHaircutsAsync(string avatarCode)
		{
			if (isLoadingHaircuts)
				Coroutines.EditorRunner.instance.Stop(haircutsLoadingCoroutine);
			haircutsLoadingCoroutine = LoadHaircuts(avatarCode);
			Coroutines.EditorRunner.instance.Run(haircutsLoadingCoroutine);
		}

		private IEnumerator LoadHaircuts(string avatarCode)
		{
			isLoadingHaircuts = true;
			selectedHaircuts.Clear();
			var haircutsRequest = umaAvatarProvider.GetHaircutsIdAsync(avatarCode);
			yield return haircutsRequest;
			if (!haircutsRequest.IsError)
			{
				List<string> haircuts = haircutsRequest.Result.ToList();
				string generatedHaircutName = "base/generated";
				if (haircuts.Contains(generatedHaircutName))
				{
					haircuts.Remove(generatedHaircutName);
					haircuts.Insert(0, generatedHaircutName);
				}
				foreach (string haircutName in haircuts)
					selectedHaircuts.Add(haircutName, haircutName.Contains("generated"));
			}
			isLoadingHaircuts = false;
		}

		private IEnumerator DeleteAvatar(string avatarCode, bool updateAvatarList = true)
		{
			var deleteRequest = umaAvatarProvider.DeleteAvatarAsync(avatarCode);
			yield return deleteRequest;
			if (updateAvatarList)
				UpdateCreatedUmaAvatars();
		}

		private IEnumerator DeleteAllAvatars()
		{
			for (int i=0; i<createdUmaAvatars.Length; i++)
			{
				EditorUtility.DisplayProgressBar("Deleting ...", createdUmaAvatars[i], ((float)i) / createdUmaAvatars.Length);
				yield return DeleteAvatar(createdUmaAvatars[i], false);
			}
			EditorUtility.ClearProgressBar();
			UpdateCreatedUmaAvatars();
		}

		private void ChangeAllHaircutsSelection(bool isSelected)
		{
			List<string> haircuts = selectedHaircuts.Keys.ToList();
			haircuts.ForEach(h => selectedHaircuts[h] = isSelected);
		}

		private void UpdateGeneratedHaircutsFoldouts()
		{
			generatedHaircutsAssetsFoldoutsList.Clear();
			for (int i = 0; i < avatarAssets.haircutsAssets.Count; i++)
				generatedHaircutsAssetsFoldoutsList.Add(false);
		}
	}
}
#endif