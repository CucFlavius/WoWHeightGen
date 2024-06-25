using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace WoWHeightGen
{
    public class Adt
    {
        public Chunk[]? chunks;
        public float minHeight;
        public float maxHeight;
        public float[,] heightmap;
        public uint[,] areaIDmap;

        public Adt(byte[] data)
        {
            if (data == null)
                return;

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    Read(br);
                    CalcHeightMap();
                }
            }
        }

        public Adt(Stream str)
        {
            if (str == null)
                return;

            using (BinaryReader br = new BinaryReader(str))
            {
                Read(br);
                CalcHeightMap();
                CalcAreaIDMap();
            }
        }


        void Read(BinaryReader br)
        {
            this.chunks = new Chunk[256];
            int mcnkIndex = 0;
            long streamPos = 0;
            this.minHeight = float.MaxValue;
            this.maxHeight = float.MinValue;

            while (streamPos < br.BaseStream.Length)
            {
                br.BaseStream.Position = streamPos;
                uint chunkID = br.ReadUInt32();
                int chunkSize = br.ReadInt32();

                streamPos = br.BaseStream.Position + chunkSize;

                if (chunkID == 0x4d434e4b)
                {
                    this.chunks[mcnkIndex] = new Chunk(br, chunkSize);
                    if (this.chunks[mcnkIndex].minHeight < this.minHeight)
                        this.minHeight = this.chunks[mcnkIndex].minHeight;
                    if (this.chunks[mcnkIndex].maxHeight > this.maxHeight)
                        this.maxHeight = this.chunks[mcnkIndex].maxHeight;
                    mcnkIndex++;
                }
            }
        }

        void CalcHeightMap()
        {
            if (this.chunks != null)
            {
                this.heightmap = new float[128, 128];
                int cIndex = 0;
                for (int cx = 0; cx < 16; cx++)
                {
                    for (int cy = 0; cy < 16; cy++)
                    {
                        var chunk = this.chunks[cIndex];

                        int currentVertex = 0;

                        for (int i = 0; i < 17; i++)
                        {
                            // Selecting squares only
                            if (i % 2 == 0)
                            {
                                var vx = i / 2;
                                for (int vy = 0; vy < 9; vy++)
                                {
                                    if (vx < 8 && vy < 8)
                                        this.heightmap[cx * 8 + vx, cy * 8 + vy] = chunk.vertexHeights[currentVertex];
                                    currentVertex++;
                                }
                            }
                            if (i % 2 == 1)
                            {
                                for (int j1 = 0; j1 < 8; j1++)
                                {
                                    currentVertex++;
                                }
                            }
                        }

                        cIndex++;
                    }
                }
            }
        }

        void CalcAreaIDMap()
        {
            if (this.chunks != null)
            {
                this.areaIDmap = new uint[128, 128];
                int cIndex = 0;
                for (int cx = 0; cx < 16; cx++)
                {
                    for (int cy = 0; cy < 16; cy++)
                    {
                        var chunk = this.chunks[cIndex];

                        int currentVertex = 0;

                        for (int i = 0; i < 17; i++)
                        {
                            // Selecting squares only
                            if (i % 2 == 0)
                            {
                                var vx = i / 2;
                                for (int vy = 0; vy < 9; vy++)
                                {
                                    if (vx < 8 && vy < 8)
                                        this.areaIDmap[cx * 8 + vx, cy * 8 + vy] = chunk.areaID;
                                    currentVertex++;
                                }
                            }
                            if (i % 2 == 1)
                            {
                                for (int j1 = 0; j1 < 8; j1++)
                                {
                                    currentVertex++;
                                }
                            }
                        }

                        cIndex++;
                    }
                }
            }
        }

        public struct Chunk
        {
            public float minHeight;
            public float maxHeight;
            public float[] vertexHeights;
            public float positionX;
            public float positionY;
            public float positionZ;
            public uint areaID;

            public Chunk(BinaryReader br, int mcnkSize)
            {
                long save = br.BaseStream.Position;

                var flags = br.ReadUInt32();
                var indexX = br.ReadUInt32();
                var indexY = br.ReadUInt32();
                var nlayers = br.ReadUInt32();
                var nDoodadRefs = br.ReadUInt32();
                var holesHigh = br.ReadUInt64();
                var ofslayers = br.ReadUInt32();
                var ofsRefs = br.ReadUInt32();
                var ofsAlpha = br.ReadUInt32();
                var sizeAlpha = br.ReadUInt32();
                var ofsShadow = br.ReadUInt32();
                var sizeShadow = br.ReadUInt32();
                this.areaID = br.ReadUInt32();
                var nMapObjRefs = br.ReadUInt32();
                var holesLow = br.ReadUInt16();
                var unk = br.ReadUInt16();
                var lowQualityTextureMap = br.ReadBytes(16); // uint2_t[8][8]
                var noEffectDoodad = br.ReadUInt64(); // uint1_t[8][8]
                var ofsSndEmitters = br.ReadUInt32();
                var nSndEmitters = br.ReadUInt32();
                var ofsLiquid = br.ReadUInt32();
                var sizeLiquid = br.ReadUInt32();

                this.positionX = br.ReadSingle();
                this.positionY = br.ReadSingle();
                this.positionZ = br.ReadSingle();
                br.BaseStream.Position += 12;

                this.minHeight = float.MaxValue;
                this.maxHeight = float.MinValue;
                this.vertexHeights = new float[145];

                long streamPos = br.BaseStream.Position;
                while (streamPos < save + mcnkSize)
                {
                    br.BaseStream.Position = streamPos;
                    uint chunkID = br.ReadUInt32();
                    int chunkSize = br.ReadInt32();

                    streamPos = br.BaseStream.Position + chunkSize;

                    if (chunkID == 0x4d435654)  // MCVT
                    {
                        for (int i = 0; i < 145; i++)
                        {
                            float h = br.ReadSingle() + this.positionZ;

                            // Calc minmax
                            if (h < this.minHeight)
                                this.minHeight = h;
                            if (h > this.maxHeight)
                                this.maxHeight = h;

                            this.vertexHeights[i] = h;
                        }
                    }
                }
            }
        }
    }
}
