using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator
{
    public static class PrincipiaUtil
    {
        public static bool PrincipiaDetected => _PrincipiaDetectTried ? _PrincipiaDetected : FindPrincipia();

        private static bool _PrincipiaDetected = false;
        private static bool _PrincipiaDetectTried = false;
        private static bool FindPrincipia()
        {
            _PrincipiaDetectTried = true;
            _PrincipiaDetected = AssemblyLoader.loadedAssemblies.Any(a => a.name.Equals("ksp_plugin_adapter", StringComparison.OrdinalIgnoreCase));
            return _PrincipiaDetected;
        }

        public static double PrincipiaCorrectInclination(Orbit o)
        {
            if (PrincipiaDetected && o.referenceBody != (FlightGlobals.currentMainBody ?? Planetarium.fetch.Home))
            {
                Vector3d polarAxis = o.referenceBody.BodyFrame.Z;

                double hSqrMag = o.h.sqrMagnitude;
                if (hSqrMag == 0d)
                {
                    return Math.Acos(Vector3d.Dot(polarAxis, o.pos) / o.pos.magnitude) * (180.0 / Math.PI);
                }
                else
                {
                    Vector3d orbitZ = o.h / Math.Sqrt(hSqrMag);
                    return Math.Atan2((orbitZ - polarAxis).magnitude, (orbitZ + polarAxis).magnitude) * (2d * (180.0 / Math.PI));
                }
            }
            else
            {
                return o.inclination;
            }
        }
    }
}
