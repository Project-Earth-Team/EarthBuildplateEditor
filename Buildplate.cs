using System;
using System.Collections.Generic;
using System.Text;

namespace EarthBuildplateEditor.model
{
    class Buildplate
    {
       
        public List<Entity> entities;
        public int format_version;
        public List<SubChunk> sub_chunks;

        

        public class SubChunk
        {
            public List<PaletteBlock> block_palette;
            public List<int> blocks;
            public PositionInt position;
        }

        public class PaletteBlock
        {
            public int data;
            public String name;
        }


        public class Entity
        {
            public int changeColor;
            public int multiplicitiveTintChangeColor;
            public String name;
            public PositionDouble position;
            public PositionDouble rotation;
            public PositionDouble shadowPosition;
            public double shadowSize;
        }
        public class PositionDouble
        {
            public double x;
            public double y;
            public double z;
        }
        public class PositionInt
        {
            public int x;
            public int y;
            public int z;
        }
    }
}
