using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;

namespace SessionUrlJoin
{
    public class SessionUrlJoinMod : NeosMod
    {
        public override string Name => "SessionUrlJoin";
        public override string Author => "maksim789456";
        public override string Version => "1.0.0";

        public override void OnEngineInit()
        {
            var harmony = new Harmony("me.maksim789456.SessionUrlJoin");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(BatchFolderImporter), "BatchImport", typeof(Slot), typeof(IEnumerable<string>), typeof(bool))]
        class BatchFolderImporter_BatchImport_Patch
        {
            static bool Prefix(Slot root, IEnumerable<string> files)
            {
                IEnumerable<string> rawUrls = files.Where(x =>
                    x.StartsWith("lnl-nat://") || x.StartsWith("neos-steam://") || x.StartsWith("lnl://"));

                List<Uri> urls = new List<Uri>();
                foreach (var rawUrl in rawUrls)
                {
                    try
                    {
                        var url = new Uri(rawUrl);
                        urls.Add(url);
                    }
                    catch (Exception e)
                    {
                        Warn($"Url {rawUrl} cannot parse. Error message: {e.Message}");
                    }
                }

                if (urls.Count == 0)
                {
                    Warn("Don't valid urls");
                    return true;
                }

                root.World.Coroutines.StartTask(async () =>
                {
                    LoadingIndicator loadingIndicator = await LoadingIndicator.CreateIndicator();

                    await Userspace.OpenWorld(new WorldStartSettings()
                    {
                        URIs = urls,
                        GetExisting = true,
                        Relation = Userspace.WorldRelation.Nest,
                        LoadingIndicator = loadingIndicator
                    });
                    await Task.Delay(1000);
                });
                return false;
            }
        }
    }
}