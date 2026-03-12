﻿using GameBananaAPI;
using GameBananaAPI.Data;
using GorillaMelonManager.Models.Mods;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GorillaMelonManager.Models.Persistence;

namespace GorillaMelonManager.Services
{
    public class BrowserService
    {
        public async Task<IEnumerable<BrowserMod>> GetMods(int page)
        {
            if (API.gameId == -1)
                API.SetCurrentGame(9496);

            SubfeedData data = await API.GetSubfeedData(page, string.Empty, "Mod", string.Empty);
            string desc;
            List<RecordData> subData = data._aRecords;
            List<BrowserMod> modsToReturn = [];

            foreach (RecordData item in subData)
            {
                ProfilePageData profile = await API.GetModProfilePage(item._idRow);

                if (profile._sDescription == null || profile._sDescription.Length == 0)
                {
                    desc = "No description provided.";
                }
                else
                {
                    desc = profile._sDescription;
                }
                
                if (!item._aTags.Contains("ModLoader: MelonLoader") && !File.Exists(Path.Combine(ManagerSettings.Default.GamePath, "Plugins", "MelInEx.dll")))
                    continue;
                
                if (item._aRootCategory.ToString() == "Software")
                    continue;

                if (profile._bIsWithheld || profile._bIsTrashed || profile._bIsPrivate)
                    continue;

                modsToReturn.Add(
                    new BrowserMod(
                        item._sName,
                        desc,
                        item._aSubmitter._sName,
                        API.GetDownloadURL(profile._aFiles[0]._idRow),
                        API.GetCompleteImageURL(profile._aPreviewMedia._aImages[0]._sFile),
                        profile._nDownloadCount,
                        profile._nLikeCount,
                        profile._aRequirements,
                        profile._aFiles[0]._sMd5Checksum
                    )
                );
            }

            return modsToReturn;
        }
    }
}
