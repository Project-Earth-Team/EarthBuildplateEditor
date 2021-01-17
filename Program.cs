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
          
            Console.WriteLine("Minecraft Earth Buildplate File Format Editor \n Version " + version +"\n Enter path to input file:");
            // String targetFilePath = Console.ReadLine();           
            String targetFilePath = @"C:\Workspace\Programming\c#\EarthBuildplateEditor\plates\test.plate";
            if (!File.Exists(targetFilePath))
            {
                Console.WriteLine("Error: File does not exist");
                return;
            }
            String fileData = File.ReadAllText(targetFilePath);
            //Deserialize
            Buildplate plate = JsonConvert.DeserializeObject<Buildplate>(fileData);
            Console.WriteLine("Version: "+plate.format_version+" Subchunk Count: "+plate.sub_chunks.Count+" Entity Count: "+plate.entities.Count);
            Console.WriteLine("Opening Editor");
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
            List<Texture2D> textures = new List<Texture2D> { };
            Dictionary<int, int> airVals = new Dictionary<int, int>();
            for (int subchunk = 0; subchunk < plate.sub_chunks.Count; subchunk++) {
                for(int paletteIndex = 0; paletteIndex < plate.sub_chunks[subchunk].block_palette.Count; paletteIndex++)
                {
                    Buildplate.PaletteBlock paletteBlock = plate.sub_chunks[subchunk].block_palette[paletteIndex];
                    String blockName = paletteBlock.name.Split(":")[1]; //gives us a clean texture name like dirt or grass_block
                    if (blockName != "air")
                    {
                        textures.Add(LoadTexture(textureBasePath + blockName + ".png"));
                    }
                    else
                    {
                        airVals.Add(subchunk, paletteIndex);
                    }
                }
            }


            while (!Raylib.WindowShouldClose())
            {
                //Render file
                UpdateCamera(ref camera);
                camera.position.Y = camY;
                BeginDrawing();
                ClearBackground(WHITE);
                BeginMode3D(camera);

                for (int subchunk = 0; subchunk < plate.sub_chunks.Count; subchunk++)
                {
                   
                    int xOffset = plate.sub_chunks[subchunk].position.x;
                    int yOffset = plate.sub_chunks[subchunk].position.y;
                    int zOffset = plate.sub_chunks[subchunk].position.z;


                    int x = 0;
                    int y = 0;
                    int z = 0;

                    for (int currentBlock = 0; currentBlock < 4096; currentBlock++)
                    {
                        x++;
                        if (x == 16) { x = 0; y += 1; }
                        if (y == 16) { y = 0; z += 1; }

                        if (plate.sub_chunks[subchunk].blocks[currentBlock] != airVals[subchunk])
                        {
                            DrawCubeTexture(textures[plate.sub_chunks[0].blocks[currentBlock]], new Vector3(x + xOffset, y + yOffset, z + zOffset), 1.0f, 1.0f, 1.0f, WHITE);
                            //DrawCube(new Vector3(x, y, z), 1.0f, 1.0f, 1.0f, GREEN);
                            //DrawCubeWires(new Vector3(x, y, z), 1.0f, 1.0f, 1.0f, BLACK);
                        }
                    }
                }

                EndMode3D();
                EndDrawing();


                //Movement for player in file
                if (IsKeyDown(KeyboardKey.KEY_SPACE))
                {
                    camY += 0.1f;
                }
                if (IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
                {
                    camY -= 0.1f;
                }

                //Other controls

            }
            CloseWindow();
        }
    }
}
