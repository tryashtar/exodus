using System.Security.Principal;
using System.Text;
using System.Security.Cryptography;

namespace Exodus;

// ported from https://github.com/DanysysTeam/PS-SFTA/blob/master/SFTA.ps1
public static class StupidFileExtensionRegistryHack
{
    public static string GetHash(string id, string extension)
    {
        string user = new NTAccount(Environment.UserName).Translate(typeof(SecurityIdentifier)).ToString();
        string stupid = "User Choice set via Windows User Experience {D18B6DD5-6124-4341-9318-804003BAFA0B}";
        var time = DateTime.Now;
        time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        long ltime = time.ToFileTime();
        long hi = ltime >> 32;
        long low = ltime & 0xFFFFFFFFL;
        string hextime = hi.ToString("X8") + low.ToString("X8");
        string hashable = $"{extension}{user}{id}{hextime}{stupid}".ToLower();
        return Hash(hashable);
    }

    private static string Hash(string baseInfo)
    {
        var bytelist = Encoding.Unicode.GetBytes(baseInfo).ToList();
        bytelist.Add(0x00);
        bytelist.Add(0x00);
        byte[] bytesBaseInfo = bytelist.ToArray();
        var MD5 = new MD5CryptoServiceProvider();
        byte[] bytesMD5 = MD5.ComputeHash(bytesBaseInfo);

        long shift_right(long value, int count)
        {
            if ((value & 0x80000000) > 0)
                return (value >> count) ^ 0xFFFF0000;
            else
                return value >> count;
        }

        var lengthBase = (baseInfo.Length * 2) + 2;
        var length = ((lengthBase & 4) < 1 ? 1 : 0) + shift_right(lengthBase, 2) - 1;
        var base64Hash = "";

        if (length > 1)
        {
            var map = new MapData();
            map.MD51 = ((BitConverter.ToInt32(bytesMD5)) | 1) + 0x69FB0000L;
            map.MD52 = ((BitConverter.ToInt32(bytesMD5, 4)) | 1) + 0x13DB0000L;
            map.INDEX = shift_right(length - 2, 1);
            map.COUNTER = map.INDEX + 1;

            while (map.COUNTER > 0)
            {
                map.R0 = BitConverter.ToInt32(BitConverter.GetBytes(BitConverter.ToInt32(bytesBaseInfo, map.PDATA) + (long)map.OUTHASH1));
                map.R1_0 = BitConverter.ToInt32(BitConverter.GetBytes(BitConverter.ToInt32(bytesBaseInfo, map.PDATA + 4)));
                map.PDATA += 8;
                map.R2_0 = BitConverter.ToInt32(BitConverter.GetBytes((map.R0 * ((long)map.MD51)) - (0x10FA9605L * shift_right(map.R0, 16))));
                map.R2_1 = BitConverter.ToInt32(BitConverter.GetBytes((0x79F8A395L * ((long)map.R2_0)) + (0x689B6B9FL * shift_right(map.R2_0, 16))));
                map.R3 = BitConverter.ToInt32(BitConverter.GetBytes((0xEA970001L * map.R2_1) - (0x3C101569L * shift_right(map.R2_1, 16))));
                map.R4_0 = BitConverter.ToInt32(BitConverter.GetBytes(map.R3 + map.R1_0));
                map.R5_0 = BitConverter.ToInt32(BitConverter.GetBytes(map.CACHE + map.R3));
                map.R6_0 = BitConverter.ToInt32(BitConverter.GetBytes((map.R4_0 * (long)map.MD52) - (0x3CE8EC25L * shift_right(map.R4_0, 16))));
                map.R6_1 = BitConverter.ToInt32(BitConverter.GetBytes((0x59C3AF2DL * map.R6_0) - (0x2232E0F1L * shift_right(map.R6_0, 16))));
                map.OUTHASH1 = BitConverter.ToInt32(BitConverter.GetBytes((0x1EC90001L * map.R6_1) + (0x35BD1EC9L * shift_right(map.R6_1, 16))));
                map.OUTHASH2 = BitConverter.ToInt32(BitConverter.GetBytes((long)map.R5_0 + (long)map.OUTHASH1));
                map.CACHE = (long)map.OUTHASH2;
                map.COUNTER--;
            }

            var outHash = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var buffer = BitConverter.GetBytes(map.OUTHASH1);
            buffer.CopyTo(outHash, 0);
            buffer = BitConverter.GetBytes(map.OUTHASH2);
            buffer.CopyTo(outHash, 4);

            map = new MapData();

            map.CACHE = 0;
            map.OUTHASH1 = 0;
            map.PDATA = 0;
            map.MD51 = (BitConverter.ToInt32(bytesMD5)) | 1;
            map.MD52 = (BitConverter.ToInt32(bytesMD5, 4)) | 1;
            map.INDEX = shift_right(length - 2, 1);
            map.COUNTER = map.INDEX + 1;

            while (map.COUNTER > 0)
            {
                map.R0 = BitConverter.ToInt32(BitConverter.GetBytes(BitConverter.ToInt32(bytesBaseInfo, map.PDATA) + ((long)map.OUTHASH1)));
                map.PDATA += 8;
                map.R1_0 = BitConverter.ToInt32(BitConverter.GetBytes(map.R0 * (long)map.MD51));
                map.R1_1 = BitConverter.ToInt32(BitConverter.GetBytes((0xB1110000L * map.R1_0) - (0x30674EEFL * shift_right(map.R1_0, 16))));
                map.R2_0 = BitConverter.ToInt32(BitConverter.GetBytes((0x5B9F0000L * map.R1_1) - (0x78F7A461L * shift_right(map.R1_1, 16))));
                map.R2_1 = BitConverter.ToInt32(BitConverter.GetBytes((0x12CEB96DL * shift_right(map.R2_0, 16)) - (0x46930000L * map.R2_0)));
                map.R3 = BitConverter.ToInt32(BitConverter.GetBytes((0x1D830000L * map.R2_1) + (0x257E1D83L * shift_right(map.R2_1, 16))));
                map.R4_0 = BitConverter.ToInt32(BitConverter.GetBytes((long)map.MD52 * ((long)map.R3 + (BitConverter.ToInt32(bytesBaseInfo, map.PDATA - 4)))));
                map.R4_1 = BitConverter.ToInt32(BitConverter.GetBytes((0x16F50000L * map.R4_0) - (0x5D8BE90BL * shift_right(map.R4_0, 16))));
                map.R5_0 = BitConverter.ToInt32(BitConverter.GetBytes((0x96FF0000L * map.R4_1) - (0x2C7C6901L * shift_right(map.R4_1, 16))));
                map.R5_1 = BitConverter.ToInt32(BitConverter.GetBytes((0x2B890000L * map.R5_0) + (0x7C932B89L * shift_right(map.R5_0, 16))));
                map.OUTHASH1 = BitConverter.ToInt32(BitConverter.GetBytes((0x9F690000L * map.R5_1) - (0x405B6097L * shift_right(map.R5_1, 16))));
                map.OUTHASH2 = BitConverter.ToInt32(BitConverter.GetBytes((long)map.OUTHASH1 + map.CACHE + map.R3));
                map.CACHE = (long)map.OUTHASH2;
                map.COUNTER--;
            }

            buffer = BitConverter.GetBytes(map.OUTHASH1);
            buffer.CopyTo(outHash, 8);
            buffer = BitConverter.GetBytes(map.OUTHASH2);
            buffer.CopyTo(outHash, 12);

            var outHashBase = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var hashValue1 = (BitConverter.ToInt32(outHash, 8)) ^ (BitConverter.ToInt32(outHash));
            var hashValue2 = (BitConverter.ToInt32(outHash, 12)) ^ (BitConverter.ToInt32(outHash, 4));

            buffer = BitConverter.GetBytes(hashValue1);
            buffer.CopyTo(outHashBase, 0);
            buffer = BitConverter.GetBytes(hashValue2);
            buffer.CopyTo(outHashBase, 4);
            base64Hash = Convert.ToBase64String(outHashBase);
        }
        return base64Hash;
    }

    private class MapData
    {
        public int PDATA = 0;
        public long CACHE = 0;
        public long COUNTER = 0;
        public long INDEX = 0;
        public long MD51 = 0;
        public long MD52 = 0;
        public int OUTHASH1 = 0;
        public int OUTHASH2 = 0;
        public int R0 = 0;
        public int R1_0 = 0;
        public int R1_1 = 0;
        public int R2_0 = 0;
        public int R2_1 = 0;
        public int R3 = 0;
        public int R4_0 = 0;
        public int R4_1 = 0;
        public int R5_0 = 0;
        public int R5_1 = 0;
        public int R6_0 = 0;
        public int R6_1 = 0;
        public int R7_0 = 0;
        public int R7_1 = 0;
    }
}