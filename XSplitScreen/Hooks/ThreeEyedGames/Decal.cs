using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DoDad.XSplitScreen.Hooks
{
    public static class Decal
    {
        public static event Decal.hook_OnWillRenderObject OnWillRenderObject
        {
            add
            {
                HookEndpointManager.Add<Decal.hook_OnWillRenderObject>(MethodBase.GetMethodFromHandle(typeof(ThreeEyedGames.Decal).GetMethod("OnWillRenderObject").MethodHandle, typeof(ThreeEyedGames.Decal).TypeHandle), value);
            }
            remove
            {
                HookEndpointManager.Remove<Decal.hook_OnWillRenderObject>(MethodBase.GetMethodFromHandle(typeof(ThreeEyedGames.Decal).GetMethod("OnWillRenderObject").MethodHandle, typeof(ThreeEyedGames.Decal).TypeHandle), value);
            }
        }

        public delegate void orig_OnWillRenderObject(ThreeEyedGames.Decal self);

        public delegate void hook_OnWillRenderObject(Decal.orig_OnWillRenderObject orig, ThreeEyedGames.Decal self);
    }
}
