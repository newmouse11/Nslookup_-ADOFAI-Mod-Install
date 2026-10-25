using System.Collections.Generic;
using UnityEngine;

namespace AdofaiTweaks.Core { public class TweakSettings { } }

namespace AdofaiTweaks.Tweaks.KeyLimiter
{
    public class KeyLimiterSettings : global::AdofaiTweaks.Core.TweakSettings
    {
        public List<KeyCode> ActiveKeys { get; set; }
        public List<ushort> ActiveAsyncKeys { get; set; }
    }
}

namespace AdofaiTweaks { public class AdofaiTweaks { } }

