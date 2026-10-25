using System.Collections.Generic;
using UnityEngine;

namespace KeyboardChatterBlocker
{
    public class KeyLimiterProfile
    {
        public string name;
        public bool isSelected;
        public List<KeyCode> allowedKeys;
        public List<ushort> allowedAsyncKeys;
    }

    public class Setting { public List<KeyLimiterProfile> keyLimiterProfiles; }

    public class Main
    {
        public static Setting setting;
        public static KeyLimiterProfile selectedKeyLimiterProfile;
    }
}
