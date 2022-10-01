/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, December 2019
*/

using System.Collections;
using System.Collections.Generic;
using UMA;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.UMA
{
	public class UmaSlotNames 
	{
		private static Dictionary<string, UmaSlotNames> supportedRacesWithSlots = new Dictionary<string, UmaSlotNames>()
		{
			{ "avatar_sdk_male_highpoly", new UmaSlotNames("FR_High_MaleFace", "FR_High_MaleEyes", "FR_High_MaleTorso", "FR_High_MaleInnerMouth", "M_Bod smooth") },
			{ "avatar_sdk_female_highpoly", new UmaSlotNames("FR_High_FemaleFace", "FR_High_FemaleEyes", "FR_High_FemaleTorso", "FR_High_FemaleInnerMouth", "F_H_Bod") },
			//{ "avatar_sdk_male", new UmaSlotNames("M_High poly Head", "MaleEyes", "MaleTorso", "MaleInnerMouth") },
			//{ "avatar_sdk_female", new UmaSlotNames("F_High poly head", "FemaleEyes", "FemaleTorso", "FemaleInnerMouth") },
			{ "HumanMaleHighPoly", new UmaSlotNames("FR_High_MaleFace", "FR_High_MaleEyes", "FR_High_MaleTorso", "FR_High_MaleInnerMouth", "M_Bod Overlay 1") },
			{ "HumanFemaleHighPoly", new UmaSlotNames("FR_High_FemaleFace", "FR_High_FemaleEyes", "FR_High_FemaleTorso", "FR_High_FemaleInnerMouth", "F_H_Bod") },
			//{ "HumanMale", new UmaSlotNames("M_High poly Head", "MaleEyes", "MaleTorso", "MaleInnerMouth", "M_Bod Overlay 1") },
			//{ "HumanFemale", new UmaSlotNames("F_High poly head", "FemaleEyes", "FemaleTorso", "FemaleInnerMouth", "F_H_Bod") }
		};

		public static UmaSlotNames GetSlotNamesForRace(RaceData raceData)
		{
			if (supportedRacesWithSlots.ContainsKey(raceData.raceName))
				return supportedRacesWithSlots[raceData.raceName];

			/*foreach(var raceWithSlots in supportedRacesWithSlots)
			{
				if (raceData.GetCrossCompatibleRaces().Contains(raceWithSlots.Key))
					return raceWithSlots.Value;
			}*/

			return null;
		}

		private UmaSlotNames(string headSlotName, string eyesSlotName, string bodySlotName, string mouthSlotName, string bodyOverlayName)
		{
			HeadSlotName = headSlotName;
			EyesSlotName = eyesSlotName;
			BodySlotName = bodySlotName;
			MouthSlotName = mouthSlotName;
			BodyOverlayName = bodyOverlayName;
		}

		public string HeadSlotName { get; private set; }
		public string EyesSlotName { get; private set; }
		public string BodySlotName { get; private set; }
		public string MouthSlotName { get; private set; }
		public string BodyOverlayName { get; private set; }
	}
}
