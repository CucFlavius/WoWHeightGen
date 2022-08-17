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

        public struct Chunk
        {
            public float minHeight;
            public float maxHeight;
            public float[] vertexHeights;
            public float positionX;
            public float positionY;
            public float positionZ;

            public Chunk(BinaryReader br, int mcnkSize)
            {
                long save = br.BaseStream.Position;

                br.BaseStream.Position += 104;
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

                    if (chunkID == 0x4d435654)
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