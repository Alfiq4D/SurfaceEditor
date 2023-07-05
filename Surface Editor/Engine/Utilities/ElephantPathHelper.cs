namespace Engine.Utilities
{
    public class ElephantPathHelper
    {
        private ElephantPathHelper()
        {
            Torso = new Torso();
            Tail = new Tail();
            Trumpet = new Trumpet();
            Head = new Head();
            FrontLegs = new FrontLegs();
            BackLegs = new BackLegs();
            Ear = new Ear();
        }

        public static ElephantPathHelper Instance { get; } = new ElephantPathHelper();

        public static Tail Tail;
        public static Torso Torso;
        public static Trumpet Trumpet;
        public static Head Head;
        public static FrontLegs FrontLegs;
        public static BackLegs BackLegs;
        public static Ear Ear;

        public static Part GetPart(string name)
        {
            switch (name)
            {
                case "Tail":
                    return Tail;
                case "Torso":
                    return Torso;
                case "Trumpet":
                    return Trumpet;
                case "Head":
                    return Head;
                case "FrontLegs":
                    return FrontLegs;
                case "BackLegs":
                    return BackLegs;
                case "Ear1":
                    return Ear;
                case "Ear2":
                    return Ear;
                default:
                    return null;
            }
        }
    }

    public abstract class Part
    {
        public IntersectionPoints IntersectionPoints { get; set; }
        public int Samples { get; set; }
        public string Name { get; set; }
    }

    public class IntersectionPoints
    {
        public Vector4 Top;
        public Vector4 Bottom;
        public Vector4 Back;
        public Vector4 Front;
    }

    public class Torso : Part
    {
        public Torso()
        {
            IntersectionPoints = new IntersectionPoints()
            {
                Top = new Vector4(-4, 11, 0, 0),
                Bottom = new Vector4(0, 5, 0, 0),
                Front = new Vector4(3, 6, 0)
            };
            Samples = 140;
            Name = "Torso";
        }
    }

    public class Tail : Part
    {
        public Tail()
        {
            IntersectionPoints = new IntersectionPoints()
            {
                Top = new Vector4(-5.07f, 9.21f, 0, 0),
                Bottom = new Vector4(-4.08f, 6.74f, 0, 0)
            };
            Samples = 75;
            Name = "Tail";
        }
    }

    public class Trumpet : Part
    {
        public Trumpet()
        {
            IntersectionPoints = new IntersectionPoints()
            {
                Front = new Vector4(8, 6, 0, 0),
                Back = new Vector4(4, 5.7f, 0)
            };
            Samples = 75;
            Name = "Trumpet";
        }
    }

    public class Head : Part
    {
        public Head()
        {
            IntersectionPoints = new IntersectionPoints()
            {
                Top = new Vector4(4.19074f, 11.26311f, 0, 1),
                Bottom = new Vector4(4, 5.7f, 0)
            };
            Samples = 90;
            Name = "Head";
        }
    }

    public class FrontLegs : Part
    {
        public FrontLegs()
        {
            IntersectionPoints = new IntersectionPoints()
            {
                Front = new Vector4(3.5f, 5, 0, 0),
                Back = new Vector4(1, 5, 0, 0)
            };
            Samples = 80;
            Name = "FrontLegs";
        }
    }

    public class BackLegs : Part
    {
        public BackLegs()
        {
            IntersectionPoints = new IntersectionPoints()
            {
                Front = new Vector4(-1.97f, 4.89f, -1.22f, 0), // -1.22f
                Back = new Vector4(-3.49f, 5.3f, 0, 0) // -2.10f
            };
            Samples = 80;
            Name = "BackLegs";
        }
    }

    public class Ear : Part
    {
        public Ear()
        {
            Samples = 70;
            Name = "Ear2";
        }
    }
}
