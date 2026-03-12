namespace AdaptiveRoads.Patches.Segment {
    using AdaptiveRoads.Util;
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Patches;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

    [InGamePatch]
    [HarmonyPatch]
    public static class RenderSegments {
        // (RenderManager.CameraInfo cameraInfo, NetInfo info, RenderManager.Instance data, float wOffsetParent, NetManager netManager)
        public static MethodBase TargetMethod() {
            return typeof(NetSegment).GetMethod(
                "RenderSegments",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[]
                {
                    typeof(RenderManager.CameraInfo),
                    typeof(NetInfo),
                    typeof(RenderManager.Instance),
                    typeof(float),
                    typeof(NetManager)
                },
                null
                
            );
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                PatchCheckFlags(codes, original);
                Log.Info($"{ReflectionHelpers.ThisMethod} patched {original} successfully!");
                return codes;
            } catch(Exception e) {
                Log.Error(e.ToString());
                throw e;
            }
        }
        static MethodInfo mCheckFlagsExt => typeof(CheckSegmentFlagsCommons).GetMethod("CheckFlags")
            ?? throw new Exception("mCheckFlagsExt is null");
        static MethodInfo mCheckFlags => typeof(NetInfo.Segment).GetMethod("CheckFlags")
            ?? throw new Exception("mCheckFlags is null");
        private static void PatchCheckFlags(List<CodeInstruction> codes, MethodBase method, int occurance = 1) {
            var matcher = new CodeMatcher(codes);
            matcher.MatchEndForward(new CodeMatch(OpCodes.Callvirt, mCheckFlags));
            Assertion.Assert(matcher.IsValid);
            var index = matcher.Pos;
            Log.Info($"CheckFlags call found at index {index}!");

            CodeInstruction LDLoc_SegmentInfo = CheckSegmentFlagsCommons.GetPrevLdLocSegmentInfo(method, codes, index);
            CodeInstruction LDLoca_turnAround = new CodeInstruction(codes[index - 1]);
            Assertion.Assert(LDLoca_turnAround.opcode == OpCodes.Ldloca_S);
            CodeInstruction LDArg_SegmenteID = TranspilerUtils.GetLDArg(method, "segmentID");
            { // insert our checkflags after base checkflags
                var newInstructions = new[]{
                    LDLoc_SegmentInfo,
                    LDArg_SegmenteID,
                    LDLoca_turnAround,
                    new CodeInstruction(OpCodes.Call,mCheckFlagsExt),
                    new CodeInstruction(OpCodes.And),
                };
                codes.InsertInstructions(index + 1, newInstructions);
            } // end block
        }
    } // end class

    [HarmonyPatch()]
    public static class RenderInstanceOverlayPatch {
        // private void NetSegment.RenderInstance(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, ref RenderManager.Instance data)
        public static MethodBase TargetMethod() =>
            typeof(NetSegment).GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance, true); 

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                SegmentOverlay.Patch(codes, original);
                Log.Info($"{ReflectionHelpers.ThisMethod} patched {original} successfully!");
                return codes;
            } catch(Exception e) {
                Log.Error(e.ToString());
                throw e;
            }
        }
    } // end class
} // end name space
