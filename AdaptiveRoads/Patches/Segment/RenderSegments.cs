namespace AdaptiveRoads.Patches.Segment {
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using KianCommons;
using KianCommons.Patches;
using System.Reflection;

    [InGamePatch]
    [HarmonyPatch]
    public static class RenderSegments {
        // Target: private void NetSegment.RenderSegments(RenderManager.CameraInfo cameraInfo, NetInfo info, ref RenderManager.Instance data, float vScale, NetManager instance)
        public static MethodBase TargetMethod() {
            return typeof(NetSegment).GetMethod(
                "RenderSegments", 
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] {
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
                CheckSegmentFlagsCommons.PatchCheckFlags(codes, original);
                Log.Info($"{ReflectionHelpers.ThisMethod} patched {original} successfully!");
                return codes;
            } catch(Exception e) {
                Log.Error(e.ToString());
                throw;
            }
        }
    }
}
