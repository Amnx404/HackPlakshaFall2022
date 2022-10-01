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
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA
{
	public enum UmaAssetFile
	{
		RACE,
		T_POSE,
		RECIPE,
		HEAD_SLOT,
		EYES_SLOT,
		MOUTH_SLOT,
		HEAD_OVERLAY,
		EYES_OVERLAY,
		BODY_OVERLAY,
		HEAD_TEXTURE,
		EYES_TEXTURE,
		BODY_TEXTURE
	}

	public enum UmaHaircutAssetFile
	{
		TEXTURE,
		OVERLAY,
		SLOT,
		RECIPE
	}

	public enum UmaAvatarFile
	{
		PHOTO,
		FULLBODY_MESH,
		HEAD_TEXTURE,
		EYES_TEXTURE,
		BODY_TEXTURE,
		BONES_DATA
	}

	public enum UmaDirectory
	{
		TEMPLATES,
		GENERATED_UMA_ASSETS,
	}

	public class TemplateRaceData
	{
		public string name;
		public UmaGender gender;
		public string raceAssetName;
		public string recipeAssetName;
		public string raceAssetFile;
		public string recipeAssetFile;
	}

	public static class AvatarSdkUmaStorage
	{
		private static Dictionary<UmaDirectory, string> directoriesList = new Dictionary<UmaDirectory, string>()
		{
			{ UmaDirectory.TEMPLATES, "Assets/itseez3d/avatar_sdk_uma/templates" },
			{ UmaDirectory.GENERATED_UMA_ASSETS, "Assets/itseez3d_uma_generated" },
		};

		private static Dictionary<UmaAssetFile, string> assetsFilesList = new Dictionary<UmaAssetFile, string>()
		{
			{ UmaAssetFile.RACE, "race.asset"},
			{ UmaAssetFile.T_POSE, "t_pose.asset" },
			{ UmaAssetFile.RECIPE, "recipe.asset"},
			{ UmaAssetFile.HEAD_SLOT, "head_slot.asset"},
			{ UmaAssetFile.EYES_SLOT, "eyes_slot.asset"},
			{ UmaAssetFile.MOUTH_SLOT, "mouth_slot.asset"},
			{ UmaAssetFile.HEAD_OVERLAY, "head_overlay.asset"},
			{ UmaAssetFile.EYES_OVERLAY, "eyes_overlay.asset"},
			{ UmaAssetFile.BODY_OVERLAY, "body_overlay.asset"},
			{ UmaAssetFile.HEAD_TEXTURE, "head.jpg"},
			{ UmaAssetFile.EYES_TEXTURE, "eyes.jpg"},
			{ UmaAssetFile.BODY_TEXTURE, "body.jpg"}
		};

		private static Dictionary<UmaHaircutAssetFile, string> haircutAssetsFilesList = new Dictionary<UmaHaircutAssetFile, string>()
		{
			{ UmaHaircutAssetFile.TEXTURE, "texture.png" },
			{ UmaHaircutAssetFile.OVERLAY, "overlay.asset" },
			{ UmaHaircutAssetFile.SLOT, "slot.asset" },
			{ UmaHaircutAssetFile.RECIPE, "recipe.asset" }
		};

		private static Dictionary<UmaAvatarFile, string> umaAvatarFilesList = new Dictionary<UmaAvatarFile, string>()
		{
			{ UmaAvatarFile.PHOTO, "photo.jpg" },
			{ UmaAvatarFile.FULLBODY_MESH, "model.ply" },
			{ UmaAvatarFile.HEAD_TEXTURE, "model.jpg" },
			{ UmaAvatarFile.EYES_TEXTURE, "eyes_texture.jpg" },
			{ UmaAvatarFile.BODY_TEXTURE, "body_texture.jpg" },
			{ UmaAvatarFile.BONES_DATA, "bones.bin" }
		};

		private static List<TemplateRaceData> templateRaces = new List<TemplateRaceData>()
		{
			new TemplateRaceData()
			{
				name = "avatar_sdk_male_highpoly",
				gender = UmaGender.Male,
				raceAssetName = "avatar_sdk_male_highpoly_race",
				recipeAssetName = "avatar_sdk_male_highpoly_recipe",
				raceAssetFile = "Assets/itseez3d/avatar_sdk_uma/templates/Races/avatar_sdk_male_highpoly_race.asset",
				recipeAssetFile = "Assets/itseez3d/avatar_sdk_uma/templates/Recipes/avatar_sdk_male_highpoly_recipe.asset"
			},
			new TemplateRaceData()
			{
				name = "avatar_sdk_female_highpoly",
				gender = UmaGender.Female,
				raceAssetName = "avatar_sdk_female_highpoly_race",
				recipeAssetName = "avatar_sdk_female_highpoly_recipe",
				raceAssetFile = "Assets/itseez3d/avatar_sdk_uma/templates/Races/avatar_sdk_female_highpoly_race.asset",
				recipeAssetFile = "Assets/itseez3d/avatar_sdk_uma/templates/Recipes/avatar_sdk_female_highpoly_recipe.asset"
			},
			new TemplateRaceData()
			{
				name = "HumanMaleHighPoly",
				gender = UmaGender.Male,
				raceAssetName = "Human Male HighPoly",
				recipeAssetName = "HumanMaleHighPoly",
				raceAssetFile = "Assets/UMA/Content/Core/HumanMale/RaceData/Human Male HighPoly.asset",
				recipeAssetFile = "Assets/UMA/Content/Core/HumanMale/Recipes/BaseRecipes/HumanMaleHighPoly.asset"
			},
			new TemplateRaceData()
			{
				name = "HumanFemaleHighPoly",
				gender = UmaGender.Female,
				raceAssetName = "Human Female HighPoly",
				recipeAssetName = "HumanFemaleHighPoly",
				raceAssetFile = "Assets/UMA/Content/Core/HumanFemale/RaceData/Human Female HighPoly.asset",
				recipeAssetFile = "Assets/UMA/Content/Core/HumanFemale/Recipes/Base Recipes/HumanFemaleHighPoly.asset"
			}
		};

		private static Dictionary<UmaGender, string> haircutsRecipesNames = new Dictionary<UmaGender, string>()
		{
			{ UmaGender.Male, "avatar_sdk_male_hair_recipe" },
			{ UmaGender.Female, "avatar_sdk_female_hair_recipe"}
		};

		private static Dictionary<UmaGender, string> umaTPosesTemplatesPaths = new Dictionary<UmaGender, string>()
		{
			{ UmaGender.Male, "Male_TPose" },
			{ UmaGender.Female, "Female_TPose" }
		};

		public static string GetDirectoryPath(UmaDirectory dir)
		{
			return directoriesList[dir];
		}

		public static TemplateRaceData GetTemplateRace(string raceName)
		{
			return templateRaces.FirstOrDefault(r => r.name == raceName);
		}

		public static List<string> GetTemplateRacesNames()
		{
			return templateRaces.Select(r => r.name).ToList();
		}

		public static List<TemplateRaceData> GetTemplateRaces()
		{
			return templateRaces;
		}

		public static string GetRaceTemplateFile(string raceName)
		{
			TemplateRaceData raceData = GetTemplateRace(raceName);
			if (raceData != null)
				return raceData.raceAssetFile;
			else
			{
				Debug.LogErrorFormat("There is no template for race: {0}", raceName);
				return string.Empty;
			}
		}

		public static string GetRecipeTemplateFile(string raceName)
		{
			TemplateRaceData raceData = GetTemplateRace(raceName);
			if (raceData != null)
				return raceData.recipeAssetFile;
			else
			{
				Debug.LogErrorFormat("There is no template for race: {0}", raceName);
				return string.Empty;
			}
		}

		public static string GetHaircutRecipeTemplateFile(string raceName)
		{
			UmaGender gender = UmaUtils.GetGenderForRace(raceName);
			return Path.Combine(directoriesList[UmaDirectory.TEMPLATES], string.Format("Haircuts/{0}.asset", haircutsRecipesNames[gender]));
		}

		public static UMAWardrobeRecipe GetHaircutRecipeTemplate(string raceName)
		{
			UmaGender gender = UmaUtils.GetGenderForRace(raceName);
			return UMAAssetIndexer.Instance.GetAsset<UMAWardrobeRecipe>(haircutsRecipesNames[gender]);
		}

		public static string GetUmaAssetsDirectoryForAvatar(string avatarName)
		{
			return Path.Combine(directoriesList[UmaDirectory.GENERATED_UMA_ASSETS], avatarName);
		}

		public static string GetUmaAssetFile(string avatarName, UmaAssetFile file)
		{
			return Path.Combine(GetUmaAssetsDirectoryForAvatar(avatarName), avatarName + "_" + assetsFilesList[file]);
		}

		public static string GetUmaHaircutsAssetDirectoryForAvatar(string avatarName)
		{
			return Path.Combine(directoriesList[UmaDirectory.GENERATED_UMA_ASSETS], Path.Combine(avatarName, "haircuts"));
		}

		public static string GetUmaHaircutAssetDirectoryForAvatar(string avatarName, string haircutName)
		{
			return Path.Combine(GetUmaHaircutsAssetDirectoryForAvatar(avatarName), CoreTools.GetShortHaircutId(haircutName));
		}

		public static string GetUmaHaircutFile(string avatarName, string haircutName, UmaHaircutAssetFile haircutFile)
		{
			string shortHaircutName = CoreTools.GetShortHaircutId(haircutName);
			return Path.Combine(GetUmaHaircutAssetDirectoryForAvatar(avatarName, haircutName), string.Format("{0}_{1}_{2}", avatarName, shortHaircutName, haircutAssetsFilesList[haircutFile]));
		}

		public static string GetUmaAvatarFile(string avatarDirectory, UmaAvatarFile file)
		{
			return Path.Combine(avatarDirectory, umaAvatarFilesList[file]);
		}

		public static string RemoveRootAssetFolderFromPath(string dir)
		{
			string assetsDir = "Assets";
			if (dir.StartsWith(assetsDir))
				dir = dir.Substring(dir.IndexOf(assetsDir) + assetsDir.Length + 1);
			return dir;
		}

		public static string GetUmaTPosePath(UmaGender gender)
		{
			return umaTPosesTemplatesPaths[gender];
		}
	}
}
