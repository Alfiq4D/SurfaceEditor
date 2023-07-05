using ObjParser;

namespace Engine.Interfaces
{
    public interface IObjectable
    {
        Obj ObjOriginalObject { get; set; }
        void SaveToObj(string fileName);
    }
}
