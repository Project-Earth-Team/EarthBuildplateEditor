using System;
using EarthBuildplateEditor.model;
using System.IO;
using Newtonsoft.Json;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.CameraType;
using static Raylib_cs.CameraMode;
using static Raylib_cs.Color;
using System.Numerics;
using System.Collections.Generic;
namespace EarthBuildplateEditor
{
    class Program
    {
        public static String version = "1.0.0";
        public static String textureBasePath = @"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\blocks\";
        static void Main(string[] args)
        {

            Console.WriteLine("Minecraft Earth Buildplate File Format Editor \n Version " + version + "\n Enter path to input file:");
            // String targetFilePath = Console.ReadLine();           
            String targetFilePath = @"C:\Workspace\Programming\c#\EarthBuildplateEditor\plates\test_32.plate";
            if (!File.Exists(targetFilePath))
            {
                Console.WriteLine("Error: File does not exist");
                return;
            }
            String fileData = File.ReadAllText(targetFilePath);
            String fileName = targetFilePath.Split(@"\")[targetFilePath.Split(@"\").Length - 1]; //get the filename with .plate
            int fileRev = 1; //used for saving so we dont save over a prior rev. useful when preparing multiple tests.
            fileName = fileName.Split(".")[0]; //remove the .plate
            //Deserialize
            Buildplate plate = JsonConvert.DeserializeObject<Buildplate>(fileData);
            //null checks
            if (plate.entities == null) { plate.entities = new List<Buildplate.Entity>() { }; }

            Console.WriteLine("Version: " + plate.format_version + " Subchunk Count: " + plate.sub_chunks.Count + " Entity Count: " + plate.entities.Count);
            Console.WriteLine("Opening Editor");

            //Prepare editor
            Camera3D camera = new Camera3D();
            camera.position = new Vector3(4.0f, 2.0f, 4.0f);
            camera.target = new Vector3(0.0f, 1.8f, 0.0f);
            camera.up = new Vector3(0.0f, 1.0f, 0.0f);
            camera.fovy = 60.0f;
            camera.type = (int)CAMERA_PERSPECTIVE;

            float camY = 2.0f;
            Raylib.InitWindow(1200, 800, "Earth Buildplate Editor");
            SetCameraMode(camera, CAMERA_FIRST_PERSON);
            SetTargetFPS(60);


            //===Render Data
            Dictionary<int, int> chunkAirValues = new Dictionary<int, int>();
            Dictionary<int, List<Texture2D>> chunkTextures = new Dictionary<int, List<Texture2D>>();
            Dictionary<int, List<int>> chunkConstraintValues = new Dictionary<int, List<int>>();
            //Model Type lists
            Dictionary<int, List<int>> chunkModelTypeTorch = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> chunkModelTypeStair = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> chunkModelTypeSlab = new Dictionary<int, List<int>>();

            //===editor data
            int maxSubChunk = plate.sub_chunks.Count - 1;
            String selectedBlock = "portal";
            bool cursorActive = false;
            bool showConstraints = true;
            int layers = 16;
            int slices = 16;

            Vector3 cursorPos;

            Texture2D cursorTexture = LoadTexture(@"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\custom\cursor.png");
            Texture2D layerIcon = LoadTexture(@"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\custom\icon\layer.png");
            Texture2D selectedBlockIcon = LoadTexture(@"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\custom\icon\selected_block.png");
            Texture2D sliceIcon = LoadTexture(@"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\custom\icon\slice.png");
            Texture2D subchunkIcon = LoadTexture(@"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\custom\icon\subchunk.png");


            //Texture  load
            Texture2D entityTex = LoadTexture(@"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\entity\entitydummy.png");
            for (int subchunk = 0; subchunk < plate.sub_chunks.Count; subchunk++)
            {
                List<Texture2D> textures = new List<Texture2D>() { };
                List<int> constraints = new List<int>() { };
                List<int> modelTypeTorch = new List<int>() { };

                //Create the textures
                for (int paletteIndex = 0; paletteIndex < plate.sub_chunks[subchunk].block_palette.Count; paletteIndex++)
                {
                    Buildplate.PaletteBlock paletteBlock = plate.sub_chunks[subchunk].block_palette[paletteIndex];
                    String blockName = paletteBlock.name.Split(":")[1]; //gives us a clean texture name like dirt or grass_block
                    textures.Add(LoadTexture(textureBasePath + blockName + ".png")); // we assign the texture to this subchunks part of the texture dict
                    if (blockName == "air")
                    {
                        chunkAirValues.Add(subchunk, paletteIndex);
                    }
                    if (blockName.Contains("constraint"))
                    {
                        constraints.Add(paletteIndex);
                    }
                    if (blockName.Contains("torch"))
                    {
                        modelTypeTorch.Add(paletteIndex);
                    }
                  
                }
                chunkTextures.Add(subchunk, textures);
                chunkConstraintValues.Add(subchunk, constraints);
                chunkModelTypeTorch.Add(subchunk, modelTypeTorch);
            }


            while (!Raylib.WindowShouldClose())
            {
                //Render file
                UpdateCamera(ref camera);
                camera.position.Y = camY;
                BeginDrawing();
                ClearBackground(WHITE);
                BeginMode3D(camera);

                //Take care of cursor position
                cursorPos.X = (int) Math.Floor(camera.target.X);
                cursorPos.Y = (int) Math.Floor(camera.target.Y-1);
                cursorPos.Z = (int) Math.Floor(camera.target.Z);




                for (int currentSubchunk = 0; currentSubchunk < maxSubChunk+1; currentSubchunk++)
                {

                    int x = 0;
                    int y = 0;
                    int z = 0;
                    int origx;
                    int origy;
                    int origz;
                    int xOffset = plate.sub_chunks[currentSubchunk].position.x*16;
                    int yOffset = plate.sub_chunks[currentSubchunk].position.y*16;
                    int zOffset = plate.sub_chunks[currentSubchunk].position.z*16;



                    //Draw Buildplate blocks, and place
                    for (int currentBlock = 0; currentBlock < 4096; currentBlock++)
                    {

                       


                        z++;
                        if (z == 16) { z = 0; y += 1; }
                        if (y == 16) { y = 0; x += 1; }

                       
                        bool shouldRender = true;
                        if (plate.sub_chunks[currentSubchunk].blocks[currentBlock] == chunkAirValues[currentSubchunk]) { shouldRender = false; }
                        if (!showConstraints && chunkConstraintValues[currentSubchunk].Contains(plate.sub_chunks[currentSubchunk].blocks[currentBlock])) { shouldRender = false; }
                        if (y > layers) { shouldRender = false; }
                        if (x > slices) { shouldRender = false; }


                        if (shouldRender)
                        {
                            int textureIndex = plate.sub_chunks[currentSubchunk].blocks[currentBlock]; //index

                            var textures = chunkTextures[currentSubchunk];

                            // Console.WriteLine("subchunk x/y/z offset: " + xOffset + "," + yOffset + "," + zOffset);
                            //Console.WriteLine("Block x/y/z: " + x + "," + y + "," + z);
                            origz = z;
                            origy = y;

                            if (z == 0)
                            {
                                z = 16;
                                y -= 1;
                            }

                            if (y == -1)
                            {
                                y = 16;
                            }


                            if (IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON) && cursorPos == new Vector3(x + xOffset, y + yOffset, z + zOffset))
                            {
                                plate.sub_chunks[currentSubchunk].blocks[currentBlock] = chunkAirValues[currentSubchunk];
                            }
                            if (IsMouseButtonPressed(MouseButton.MOUSE_RIGHT_BUTTON) && cursorPos == new Vector3(x + xOffset, y + yOffset, z + zOffset))
                            {
                                //Check the palette!
                                bool doesPaletteContainBlock = false;
                                foreach (Buildplate.PaletteBlock paletteBlock in plate.sub_chunks[currentSubchunk].block_palette)
                                {
                                    if(paletteBlock.name.Split(":")[1] == selectedBlock) { doesPaletteContainBlock = true; }
                                }
                                //if the palette does not contain the block, add it both to the palette and  to the texture index
                                if (!doesPaletteContainBlock)
                                {
                                    plate.sub_chunks[currentSubchunk].block_palette.Add(new Buildplate.PaletteBlock { data = 0, name = "minecraft:" + selectedBlock });
                                    chunkTextures[currentSubchunk].Add(LoadTexture(textureBasePath + selectedBlock + ".png"));
                                }

                                //TODO: Build function to query a palette for the index of a given blocktype 
                                plate.sub_chunks[currentSubchunk].blocks[currentBlock] = chunkAirValues[currentSubchunk];
                            }


                            DrawCubeTexture(textures[textureIndex],new Vector3(x + xOffset, y + yOffset, z + zOffset), 1.0f, 1.0f, 1.0f, WHITE);
                            z = origz;
                            y = origy;
                        }
                    }
                }

                //Draw entities
                foreach (Buildplate.Entity entity in plate.entities)
                {
                    DrawCubeTexture(entityTex, new Vector3((float)entity.position.x, (float)entity.position.y, (float)entity.position.z), 0.5f, 0.5f, 0.5f, WHITE); ;
                }

                //Draw Selection Cursor
                if (cursorActive)
                {
                    DrawCubeWires(cursorPos, 1.1f, 1.1f, 1.1f, YELLOW);
                }
                EndMode3D();

                DrawTexture(subchunkIcon, 10, 10, WHITE);
                DrawText(plate.sub_chunks.Count +" subchunks ", 39, 20, 10, BLACK);
                // DrawText("Left/Right arrow to change slice count, Up/Down to change Layer count", 10, 30, 10, BLACK);
                DrawTexture(sliceIcon, 10, 50, WHITE);
                DrawText("Slices: " +slices, 39, 60, 10, BLACK);

                DrawTexture(layerIcon, 10, 80, WHITE);
                DrawText("Layers: " + layers, 39, 90, 10, BLACK);

                DrawText("Cx/Cy/Cz: " + cursorPos, 10, 110, 10, BLACK);



                EndDrawing();


                //Player up/down movement
                if (IsKeyDown(KeyboardKey.KEY_SPACE))
                {
                    camY += 0.1f;
                }
                if (IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
                {
                    camY -= 0.1f;
                }
              
             
                if (IsKeyPressed(KeyboardKey.KEY_E))
                {

                    string exportPath = @"./exports/" + fileName + "_"+ fileRev+ ".plate64";
                    fileRev++;
                    string exportData = Util.Base64Encode(JsonConvert.SerializeObject(plate));
                    System.IO.Directory.CreateDirectory("./exports/");
                    System.IO.File.WriteAllText(exportPath, exportData);
                }
                if (IsKeyPressed(KeyboardKey.KEY_C))
                {
                    cursorActive = !cursorActive;
                }
                if (IsKeyPressed(KeyboardKey.KEY_V))
                {
                    showConstraints = !showConstraints;
                }
                if (IsKeyPressed(KeyboardKey.KEY_UP))
                {
                    layers++;
                    if (layers > 16) { layers = 16; }
                }
                if (IsKeyPressed(KeyboardKey.KEY_DOWN))
                {
                    layers--;
                    if (layers < 0) { layers = 0; }
                }
                if (IsKeyPressed(KeyboardKey.KEY_LEFT))
                {
                    slices--;
                    if (slices < 0) { slices = 0; }
                }
                if (IsKeyPressed(KeyboardKey.KEY_RIGHT))
                {
                    slices++;
                    if (slices > 16) { slices = 16; }
                }

            }
            CloseWindow();
        }
      
    }
}
