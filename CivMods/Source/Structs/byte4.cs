using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace CivMods
{
    public struct byte4
    {
        private uint val;

        public byte4(byte x, byte y, byte z, byte w) : this()
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public byte4(uint value)
        {
            val = value;
        }

        public byte4(string hex)
        {
            hex = hex.StartsWith("0x") ? hex.Substring(2) : hex;
            hex = hex.StartsWith("#") ? hex.Substring(1) : hex;
            if (hex.Length < 8)
            {
                int dl = 8 - hex.Length;
                for (int i = 8 - dl; i < 8; i++)
                {
                    hex += i > 5 ? 'F' : '0';
                }
            }

            uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val);
        }

        public byte4(float x, float y, float z, float w) : this()
        {
            this.x = (byte)Math.Round(x * 255);
            this.y = (byte)Math.Round(y * 255);
            this.z = (byte)Math.Round(z * 255);
            this.w = (byte)Math.Round(w * 255);
        }

        public byte[] ToBytes()
        {
            int size = Marshal.SizeOf(this);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);

            byte[] bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);

            return bytes;
        }

        public static byte[] ToBytes(byte4[] structs)
        {
            int subSize = Marshal.SizeOf<byte4>();
            int size = subSize * structs.Length;
            byte[] bytes = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
           
            for (int i = 0; i < structs.Length; i++)
            {
                Marshal.WriteInt32(ptr, subSize * i, (int)structs[i].val);
            }
            
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);

            return bytes;
        }

        public static byte4 operator +(byte4 a, byte4 b)
        {
            byte x = (byte)GameMath.Clamp(a.x + b.x, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y + b.y, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z + b.z, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w + b.w, 0, 255);
            return new byte4(x, y, z, w);
        }

        public static byte4 operator -(byte4 a, byte4 b)
        {
            byte x = (byte)GameMath.Clamp(a.x - b.x, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y - b.y, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z - b.z, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w - b.w, 0, 255);
            return new byte4(x, y, z, w);
        }

        public static byte4 operator *(byte4 a, byte4 b)
        {
            byte x = (byte)GameMath.Clamp(a.x * b.x, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y * b.y, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z * b.z, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w * b.w, 0, 255);
            return new byte4(x, y, z, w);
        }

        public static byte4 operator /(byte4 a, byte4 b)
        {
            byte x = (byte)GameMath.Clamp(a.x / b.x, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y / b.y, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z / b.z, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w / b.w, 0, 255);
            return new byte4(x, y, z, w);
        }

        public static byte4 operator +(byte4 a, byte b)
        {
            byte x = (byte)GameMath.Clamp(a.x + b, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y + b, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z + b, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w + b, 0, 255);
            return new byte4(x, y, z, w);
        }

        public static byte4 operator -(byte4 a, byte b)
        {
            byte x = (byte)GameMath.Clamp(a.x - b, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y - b, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z - b, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w - b, 0, 255);
            return new byte4(x, y, z, w);
        }

        public static byte4 operator *(byte4 a, byte b)
        {
            byte x = (byte)GameMath.Clamp(a.x * b, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y * b, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z * b, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w * b, 0, 255);
            return new byte4(x, y, z, w);
        }

        public static byte4 operator /(byte4 a, byte b)
        {
            byte x = (byte)GameMath.Clamp(a.x / b, 0, 255);
            byte y = (byte)GameMath.Clamp(a.y / b, 0, 255);
            byte z = (byte)GameMath.Clamp(a.z / b, 0, 255);
            byte w = (byte)GameMath.Clamp(a.w / b, 0, 255);
            return new byte4(x, y, z, w);
        }

        public byte r { get => x; set => x = value; }
        public byte g { get => y; set => y = value; }
        public byte b { get => z; set => z = value; }
        public byte a { get => w; set => w = value; }

        public byte x { 
            get => (byte)((val & 0xFF000000) >> 24);
            set
            {
                val &= 0x00FFFFFF;
                val |= (uint)value << 24;
            }
        }
        public byte y { 
            get => (byte)((val & 0x00FF0000) >> 16);
            set
            {
                val &= 0xFF00FFFF;
                val |= (uint)value << 16;
            }
        }
        public byte z { 
            get => (byte)((val & 0x0000FF00) >> 08);
            set
            {
                val &= 0xFFFF00FF;
                val |= (uint)value << 08;
            }
        }
        public byte w { 
            get => (byte)((val & 0x000000FF) >> 00);
            set
            {
                val &= 0xFFFFFF00;
                val |= (uint)value << 00;
            }
        }
    }
}