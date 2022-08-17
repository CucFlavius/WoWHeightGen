using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace WoWHeightGen
{
    public class Wdt
    {
        public FileInfo[,]? fileInfo;

        public Wdt(byte[] data)
        {
            if (data == null)
                return;

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    Read(br);
                }
            }
        }

        public Wdt(Stream str)
        {
            if (str == null)
                return;

            using (BinaryReader br = new BinaryReader(str))
            {
                Read(br);
            }
        }

        void Read(BinaryReader br)
        {
            long streamPos = 0;
            while (streamPos < br.BaseStream.Length)
            {
                br.BaseStream.Position = streamPos;
                uint chunkID = br.ReadUInt32();
                int chunkSize = br.ReadInt32();
                streamPos = br.BaseStream.Position + chunkSize;

                if (chunkID == 0x4d414944)
                {
                    this.fileInfo = new FileInfo[64, 64];
                    for (var y = 0; y < 64; y++)
                    {
                        for (var x = 0; x < 64; x++)
                        {
                            this.fileInfo[x, y] = new FileInfo(br);
                        }
                    }
                }
            }
        }

        public struct FileInfo
        {
            public uint rootADT;            // reference to fdid of mapname_xx_yy.adt
            public uint obj0ADT;            // reference to fdid of mapname_xx_yy_obj0.adt
            public uint obj1ADT;            // reference to fdid of mapname_xx_yy_obj1.adt
            public uint tex0ADT;            // reference to fdid of mapname_xx_yy_tex0.adt
            public uint lodADT;             // reference to fdid of mapname_xx_yy_lod.adt
            public uint mapTexture;         // reference to fdid of mapname_xx_yy.blp
            public uint mapTextureN;        // reference to fdid of mapname_xx_yy_n.blp
            public uint minimapTexture;     // reference to fdid of mapxx_yy.blp

            public FileInfo(BinaryReader br)
            {
                this.rootADT = br.ReadUInt32();
                this.obj0ADT = br.ReadUInt32();
                this.obj1ADT = br.ReadUInt32();
                this.tex0ADT = br.ReadUInt32();
                this.lodADT = br.ReadUInt32();
                this.mapTexture = br.ReadUInt32();
                this.mapTextureN = br.ReadUInt32();
                this.minimapTexture = br.ReadUInt32();
            }
        }
    }
}