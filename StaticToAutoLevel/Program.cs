namespace StaticToAutoLevel;

#pragma warning disable CA1416
using System;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Noggog;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using System.Threading.Tasks;

// public class Settings
// {
//   [SettingName("Mod name")]
//   [Tooltip("Full name including extension")]
//   public string ModName = "SomeEsp.esp";
// 
//   [SettingName("Keyword Form Id")]
//   [Tooltip("Form Id of keyword in mod")]
//   public uint FormId = 0x800;
// }

public static class Program
{
    // static Lazy<Settings> _settings = new();
    public static async Task<int> Main(string[] args)
    {
        return await SynthesisPipeline.Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
            .SetTypicalOpen(GameRelease.SkyrimSE, new ModKey("StaticToAutoLevel.esp", ModType.Plugin))
            .Run(args);
    }

    private static void SynthesisLog(string message, bool special = false)
    {
        if (special)
        {
            Console.WriteLine();
            Console.Write(">>> ");
        }

        Console.WriteLine(message);
        if (special) Console.WriteLine();
    }

    private static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var skyrimEsm = "Skyrim.esm";
        var dragonbornEsm = "Dragonborn.esm";
        var apothecaryEsp = "Apothecary.esp";
        var reSimonrimEsp = "RESimonrim.esp";

        SynthesisLog(
            "Start patch NPCs", true);
        foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
        {
            if (npc.IsDeleted)
            {
                continue;
            }

            if (!npc.Template.IsNull && (npc.Configuration.TemplateFlags & NpcConfiguration.TemplateFlag.Stats) != 0)
            {
                continue;
            }
            
            var pclevelmult = npc.Configuration.Level as PcLevelMult;
            // if (pclevelmult != null && (pclevelmult.LevelMult < 1.5f || npc.Configuration.CalcMinLevel > 30))
            if (pclevelmult != null && (npc.Configuration.CalcMinLevel > 30 || pclevelmult.LevelMult > 1.5f))
            {
                // if ((npc.Configuration.Flags & NpcConfiguration.Flag.Summonable) != 0) { continue; }
                // if ((npc.Configuration.Flags & NpcConfiguration.Flag.IsGhost) != 0) { continue; }
                if (npc.Configuration.CalcMaxLevel < 10) { continue; }
                var modifiedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                // var levelMult = (modifiedNpc.Configuration.Flags & NpcConfiguration.Flag.Unique) != 0 ? 1.5f : 1.0f;
                // if (pclevelmult.LevelMult < levelMult)
                // {
                //     modifiedNpc.Configuration.Level = new PcLevelMult() { LevelMult = levelMult };
                // }

                if (npc.Configuration.CalcMinLevel > 10)
                {
                    modifiedNpc.Configuration.CalcMinLevel = 10;
                }

                if (pclevelmult.LevelMult > 1.5f)
                {
                    modifiedNpc.Configuration.Level = new PcLevelMult() { LevelMult = 1.5f };
                }
                continue;
            }

            var staticlevel = npc.Configuration.Level as NpcLevel;
            if (staticlevel != null)
            {
                // if ((npc.Configuration.Flags & NpcConfiguration.Flag.Summonable) != 0) { continue; }
                // if ((npc.Configuration.Flags & NpcConfiguration.Flag.IsGhost) != 0) { continue; }
                var currentLevel = staticlevel.Level;
                if (currentLevel < 10) { continue; }
                var minLevel = currentLevel > (short)(10) ? (short)(10) : currentLevel;
                var maxLevel = (short)(currentLevel * 2.5f);
                var modifiedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                var levelMult = (modifiedNpc.Configuration.Flags & NpcConfiguration.Flag.Unique) != 0 ? 1.5f : 1.0f;
                modifiedNpc.Configuration.Level = new PcLevelMult() { LevelMult = levelMult };
                modifiedNpc.Configuration.CalcMinLevel = minLevel;
                modifiedNpc.Configuration.CalcMaxLevel = maxLevel;
            }
        }

        SynthesisLog("Done patching no static level!", true);
    }
}