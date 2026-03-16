namespace AdaptiveRoads.Patches.Lane {
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using KianCommons.Patches;
    using KianCommons;
    using ColossalFramework.Math;
    using System;
    using AdaptiveRoads.Manager;

    internal static class SeedIndexCommons {
        public static void Patch(List<CodeInstruction> codes, MethodBase method) {
            MethodBase constructor = typeof(Randomizer).GetConstructor(new[] { typeof(int) })
                ?? throw new Exception("Randomizer constructor not found");
            MethodInfo mGetSeed = typeof(SeedIndexCommons).GetMethod(nameof(GetSeed), throwOnError: true);
            
            /*int iLdProp = codes.Search(_c => _c.IsLdLoc(typeof(NetLaneProps.Prop), method));
            for (int occurance = 1; occurance<=2; occurance++) {
                int iNewRandomizer = codes.Search(_c => _c.Calls(NewRandomizer), count: occurance);
                codes.InsertInstructions(iNewRandomizer,
                    new[] {
                    // seed0 already on the stack
                    TranspilerUtils.GetLDArg(method, "laneID"),
                    codes[iLdProp].Clone(),
                    new CodeInstruction(OpCodes.Call, mGetSpeed),
                    });*/

            // Find the local variable load for Prop
            int iLdProp = codes.Search(_c => _c.IsLdLoc(typeof(NetLaneProps.Prop), method));
            if (iLdProp == -1) throw new Exception("Could not find NetLaneProps.Prop local load");

            // Loop backwards (2 then 1) so indices don't shift
            for (int occurance = 2; occurance >= 1; occurance--) {
                int iNewRandomizer = codes.Search(_c => 
                    _c.opcode == OpCodes.Call && _c.operand as ConstructorInfo == constructor, 
                    count: occurance);

                if (iNewRandomizer != -1) {
                    codes.InsertInstructions(iNewRandomizer, new[] {
                    TranspilerUtils.GetLDArg(method, "laneID"),
                    codes[iLdProp].Clone(),
                    new CodeInstruction(OpCodes.Call, mGetSeed),
                    });
                }
            }
        }

        public static int GetSeed(int seed0, uint laneId, NetLaneProps.Prop prop) {
            try {
                if (prop.GetMetaData() is NetInfoExtionsion.LaneProp metadata && metadata.SeedIndex != 0) {
                    return unchecked((int)laneId + (metadata.SeedIndex - 1));
                }
            } catch(Exception ex) { ex.Log(); }
            return seed0;

        }
    }
}
