using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements.Assets;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace SessionUrlJoin
{
    public class SessionUrlJoinMod : ResoniteMod
    {
        public override string Name => "SessionUrlJoin";
        public override string Author => "maksim789456";
        public override string Version => "2.1.1";

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
                foreach (string str in files)
                {
                    AssetClass key = AssetHelper.IdentifyClass(str);
                    if (key != AssetClass.Unknown) continue;

                    List<string> rawUrls = new List<string>();
                    // Two session urls can contains line break symbols
                    if (str.Contains('\n'))
                    {
                        string[] splitedStr = str.Replace("\r", "").Split('\n');
                        rawUrls.AddRange(splitedStr);
                    }
                    else
                    {
                        rawUrls.Add(str);
                    }

                    List<Uri> urls = new List<Uri>();
                    foreach (string rawUrl in rawUrls)
                    {
                        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri uri))
                        {
                            if (Userspace.Current.Engine.NetworkManager.IsSupportedSessionScheme(uri.Scheme) || uri.Scheme == "ressession")
                                urls.Add(uri);

                            // Parse 'Session URL'
                            if (uri.Host == "api.resonite.com" && uri.AbsolutePath.StartsWith("/open/session"))
                            {
                                var sessionId = uri.AbsolutePath.Replace("/open/session/", "");
                                var neosSessionRawUrl = "ressession:///" + sessionId;
                                if (Uri.TryCreate(neosSessionRawUrl, UriKind.Absolute, out Uri neosSessionUri))
                                    urls.Add(neosSessionUri);
                            }
                        }
                    }

                    if (urls.Count == 0) continue;
                    Debug($"Session urls: {string.Join(", ", urls)}");

                    root.World.Coroutines.StartTask(async () =>
                    {
                        await Userspace.OpenWorld(new WorldStartSettings()
                        {
                            URIs = urls,
                            GetExisting = true,
                            AutoFocus = true,
                            Relation = Userspace.WorldRelation.Independent,
                        });
                        await Task.Delay(1000);
                    });
                    return false;
                }

                Warn("No valid session urls");
                return true;
            }
        }
    }
}