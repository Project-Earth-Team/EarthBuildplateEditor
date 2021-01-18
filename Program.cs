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
            String targetFilePath = @"C:\Workspace\Programming\c#\EarthBuildplateEditor\plates\test_c.plate";
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
            Raylib.InitWindow(800, 600, "Earth Buildplate Editor");
            SetCameraMode(camera, CAMERA_FIRST_PERSON);
            SetTargetFPS(60);


            //Render Data
            Dictionary<int, int> chunkAirValues = new Dictionary<int, int>();
            Dictionary<int, List<Texture2D>> chunkTextures = new Dictionary<int, List<Texture2D>>();

            //editor data
            int currentSubchunk = 0;
            int maxSubChunk = plate.sub_chunks.Count - 1;
            String selectedBlock = "beacon";
            bool cursorActive = false;


            //Texture  load
            Texture2D entityTex = LoadTexture(@"C:\Workspace\Programming\c#\EarthBuildplateEditor\earth_res\textures\entity\entitydummy.png");
            for (int subchunk = 0; subchunk < plate.sub_chunks.Count; subchunk++)
            {
                List<Texture2D> textures = new List<Texture2D>() { };
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
                  
                }
                chunkTextures.Add(subchunk, textures);
            }


            while (!Raylib.WindowShouldClose())
            {
                //Render file
                UpdateCamera(ref camera);
                camera.position.Y = camY;
                BeginDrawing();
                ClearBackground(WHITE);
                BeginMode3D(camera);


                //Positioning offsets for this plate's blocks.
                //int xOffset = plate.sub_chunks[currentSubchunk].position.x;
                //int yOffset = plate.sub_chunks[currentSubchunk].position.y;
                //int zOffset = plate.sub_chunks[currentSubchunk].position.z;


                int x = 0;
                int y = 0;
                int z = 0;

                //Draw Buildplate blocks
                for (int currentBlock = 0; currentBlock < 4096; currentBlock++)
                {
                    x++;
                    if (x == 16) { x = 0; y += 1; }
                    if (y == 16) { y = 0; z += 1; }

                    if (plate.sub_chunks[currentSubchunk].blocks[currentBlock] != chunkAirValues[currentSubchunk])
                    {
                        int textureIndex = plate.sub_chunks[currentSubchunk].blocks[currentBlock]; //index

                        var textures = chunkTextures[currentSubchunk];

                        DrawCubeTexture(textures[textureIndex], new Vector3(x, y, z), 1.0f, 1.0f, 1.0f, WHITE);
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
                    DrawCubeTexture(LoadTexture(textureBasePath + selectedBlock + ".png"), camera.target, 0.2f, 0.2f, 0.2f, GRAY);
                }
                EndMode3D();

                DrawText("Current Subchunk: " + currentSubchunk, 10, 10, 10, BLACK);
                DrawText("Left/Right arrow to change subchunk", 10, 30, 10, BLACK);
                DrawText("Current air val: " + chunkAirValues[currentSubchunk], 10, 50, 10, BLACK);
                DrawText("Selected Block: " + selectedBlock, 10, 70, 10, BLACK);
                DrawText("Press E to export to plate64", 10, 90, 10, BLACK);

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
              
                //Other controls
                if (IsKeyPressed(KeyboardKey.KEY_LEFT))
                {
                    currentSubchunk -= 1;
                    if (currentSubchunk < 0) { currentSubchunk = 0; }
                }
                if (IsKeyPressed(KeyboardKey.KEY_RIGHT))
                {
                    currentSubchunk += 1;
                    if (currentSubchunk > maxSubChunk) { currentSubchunk = maxSubChunk; }
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

            }
            CloseWindow();
        }
    }
}
