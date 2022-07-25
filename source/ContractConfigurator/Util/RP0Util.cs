using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator
{
    public static class RP0Util
    {
        public static bool RP0Detected => _RP0DetectTried ? _RP0Detected : FindRP0();

        private static bool _RP0Detected = false;
        private static bool _RP0DetectTried = false;
        private static bool FindRP0()
        {
            _RP0DetectTried = true;
            _RP0Detected = AssemblyLoader.loadedAssemblies.Any(a => a.name.Equals("RP-0", StringComparison.OrdinalIgnoreCase));
            return _RP0Detected;
        }
    }
}
