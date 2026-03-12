namespace AdaptiveRoads.Patches.Segment {
    using AdaptiveRoads.Manager;
    using AdaptiveRoads.Util;
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Patches;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
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
        static NetworkExtensionManager man_ => NetworkExtensionManager.Instance;
        public static bool CheckFlags(NetInfo.Segment segment, int id, ref bool turnAround) {
            CheckSegmentFlagsCommons.CheckFlags(segment, (ushort)id, ref turnAround);
            var segmentExt = segment?.GetMetaData();
            if (segmentExt == null) return true; // bypass
            ushort uid = (ushort)id;
            ref NetSegmentExt netSegmentExt = ref man_.SegmentBuffer[id];
            ref NetSegment netSegment = ref uid.ToSegment();
            ref NetNode netNodeStart = ref netSegment.m_startNode.ToNode();
            ref NetNode netNodeEnd = ref netSegment.m_endNode.ToNode();
            ref NetNodeExt netNodeExtStart = ref man_.NodeBuffer[netSegment.m_startNode];
            ref NetNodeExt netNodeExtEnd = ref man_.NodeBuffer[netSegment.m_endNode];

            var segmentTailFlags = netSegmentExt.Start.m_flags;
            var segmentHeadFlags = netSegmentExt.End.m_flags;
            var nodeTailFlags = netNodeStart.flags;
            var nodeHeadFlags = netNodeEnd.flags;
            var nodeExtTailFlags = netNodeExtStart.m_flags;
            var nodeExtHeadFlags = netNodeExtEnd.m_flags;

            bool reverse = /*netSegment.IsInvert() ^*/ NetUtil.LHT;
            if (reverse) {
                Helpers.Swap(ref segmentTailFlags, ref segmentHeadFlags);
                Helpers.Swap(ref nodeTailFlags, ref nodeHeadFlags);
                //Log.DebugWait($"CheckSegmentFlagsCommons: segment:{segmentID} is reverse");
            }

            {
                turnAround = false;
                bool ret = segment.CheckFlags(netSegment.m_flags, turnAround);
                ret = ret && segmentExt.CheckFlags(
                    netSegmentExt.m_flags,
                    tailFlags: segmentTailFlags,
                    headFlags: segmentHeadFlags,
                    tailNodeFlags: nodeTailFlags,
                    headNodeFlags: nodeHeadFlags,
                    tailNodeExtFlags: nodeExtTailFlags,
                    headNodeExtFlags: nodeExtHeadFlags,
                    userData: netSegmentExt.UserData,
                    turnAround);
                if (ret) return true;
            }
            {
                turnAround = true;
                bool ret = segment.CheckFlags(netSegment.m_flags, turnAround);
                ret = ret && segmentExt.CheckFlags(
                    netSegmentExt.m_flags,
                    tailFlags: segmentTailFlags,
                    headFlags: segmentHeadFlags,
                    tailNodeFlags: nodeTailFlags,
                    headNodeFlags: nodeHeadFlags,
                    tailNodeExtFlags: nodeExtTailFlags,
                    headNodeExtFlags: nodeExtHeadFlags,
                    userData: netSegmentExt.UserData,
                    turnAround);
                if (ret) return true;
            }

            //fail
            turnAround = false;
            return false;
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

            CodeInstruction LDLoca_turnAround = new CodeInstruction(codes[index - 1]);
            Assertion.Assert(LDLoca_turnAround.opcode == OpCodes.Ldloca_S);
            CodeInstruction LDLoc_Segment = new CodeInstruction(OpCodes.Ldloc_1);
            CodeInstruction LDLoc_SegmentID = new CodeInstruction(OpCodes.Ldloc_0);
            Log.Info($"SegmentID index: {matcher.Pos + 1}");
            matcher.Advance(1).Insert(
                LDLoc_Segment,
                LDLoc_SegmentID,
                LDLoca_turnAround,
                new CodeInstruction(OpCodes.Call, mCheckFlagsExt),
                new CodeInstruction(OpCodes.And)
            );
        }
    } 

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
