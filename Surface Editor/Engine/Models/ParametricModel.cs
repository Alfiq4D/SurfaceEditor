using System.Collections.Generic;

namespace Engine.Models
{
    public class ParametricModel
    {
        public List<CylindricalBezierPatchC2> Parts { get; set; }

        public ParametricModel(List<_3DObject> parts)
        {
            Parts = new List<CylindricalBezierPatchC2>();
            foreach (var part in parts)
            {
                if (part is CylindricalBezierPatchC2)
                {
                    Parts.Add(part as CylindricalBezierPatchC2);
                }
            }
        }

        public CylindricalBezierPatchC2 this[string name]
        {
            get
            {
                foreach (var part in Parts)
                    if (part.Name == name)
                        return part;
                return null;
            }
        }
    }
}
