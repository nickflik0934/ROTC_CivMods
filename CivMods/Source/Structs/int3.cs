namespace CivMods
{

    public struct int3
    {
        public int x;
        public int y;
        public int z;

        public int3(int x = 0, int y = 0, int z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public int3 Clone()
        {
            return new int3(x, y, z);
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}", x, y, z);
        }
    }
}