
#define ENABLE_PROFILE

using UnityEngine;
using UnityEngine.Profiling;


public sealed class ProfileManager
{
    public bool EnableProfile;

    public static void BeginSample(string name)
    {
#if ENABLE_PROFILE        
        Profiler.BeginSample(name);
#endif
    }

    public static void EndSample()
    {
#if ENABLE_PROFILE        
        Profiler.EndSample();
#endif        
    }
}