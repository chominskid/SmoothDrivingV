using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.UI;
using GTA.Native;

namespace SmoothDrivingV
{
    static class UI
    {
        public static bool isLoaded = false;

        private static float aspectRatio = 0.0f;
        private static float margin = 0.025f;
        private static float masterScale = 0.1224609375f;
        private static float masterScaleRace = 1.1961722488f;
        private static float masterAlpha = 0.0f;
        private static float fadeAlpha = 0.0f;

        private static bool fpsDisableUI = false;
        public static string speedUnit = "MPH";
        public static float speedUnitMultiplier = 2.236936f;
        public static bool useWheelSpeed = true;
        public static float cruiseSpeedIncrement = 6.705601f;
        private static float speedSmoothing = 0.2f;

        private static float fontSpacing = 0.32f;
        private static Dictionary<char, int> characterSet = new Dictionary<char, int>();

        private static string[] gearText = new string[]
        {
            "Reverse",
            "1st Gear",
            "2nd Gear",
            "3rd Gear",
            "4th Gear",
            "5th Gear",
            "6th Gear",
            "7th Gear",
            "8th Gear",
            "9th Gear",
            "10th Gear",
            "11th Gear",
            "12th Gear",
            "13th Gear",
            "14th Gear",
            "15th Gear",
            "16th Gear",
            "17th Gear",
            "18th Gear",
            "19th Gear",
            "20th Gear",
        };

        private static int background = 0;
        private static int overlay = 0;
        private static int bar = 0;
        private static int raceHighlight = 0;
        private static int indicatorLight = 0;
        private static int headlightsLight = 0;
        private static int highbeamsLight = 0;

        public static float displaySpeed = 0.0f;

        private static float centerX = 0.0f;
        private static float centerY = 0.0f;

        private static float rpmBarX = 0.0f;
        private static float rpmBarY = 0.0f;
        private static float rpmBarSizeX = 0.0f;
        private static float rpmBarSizeY = 0.0f;

        private static float largeTextSizeY = 0.0f;
        private static float smallTextSizeY = 0.0f;

        private static float fuelBarX = 0.0f;

        private static float cruiseSpeedY = 0.0f;
        private static float speedValueY = 0.0f;
        private static float speedUnitY = 0.0f;
        private static float gearY = 0.0f;

        private static Dictionary<int, int> instances = new Dictionary<int, int>();
        private static Dictionary<int, int> drawTimes = new Dictionary<int, int>();

        private static int CreateTexture(string fileName)
        {
            int id = Memory.CreateTexture(fileName);
            instances.Add(id, 0);
            drawTimes.Add(id, 0);
            return id;
        }

        private static void DrawTexture(int id, bool fast, int level, float a, float r, float g, float b, float posX, float posY, float sizeX, float sizeY, float rotation, float centerX, float centerY)
        {
            int frameCount = Game.FrameCount;

            if (frameCount != drawTimes[id])
            {
                drawTimes[id] = frameCount;
                instances[id] = 0;
            }

            float timeScale = Math.Max(0.1f, Game.TimeScale);
            int time = fast ? (int)((1000.0f * Time.maxDeltaTime / timeScale) + 12.0f) : (int)((1000.0f * Time.maxDeltaTime / timeScale) + 100.0f);

            Memory.DrawTexture(id, instances[id]++, level, time, sizeX, sizeY, centerX, centerY, posX, posY, rotation, aspectRatio, r, g, b, a);
        }

        private static void DrawText(string text, bool fast, int level, float startX, float startY, float centerX, float centerY, float a, float r, float g, float b, float scale, bool center)
        {
            float offset = center ? (-0.5f * (text.Length - 1) * fontSpacing) : 0.0f;

            foreach (char character in text)
            {
                if (character != ' ' && characterSet.ContainsKey(character))
                {
                    int id = characterSet[character];

                    if (id != 0)
                    {
                        DrawTexture(id, fast, level, a, r, g, b, startX + offset * 0.1f * scale, startY, 0.1f * scale, 0.1f * scale, 0.0f, centerX, centerY);
                    }
                }

                offset += fontSpacing;
            }
        }

        public static void Initialize()
        {
            string[] configLines = File.ReadAllLines(Environment.CurrentDirectory + "/Scripts/SmoothDrivingV/UI.ini");

            if (Config.ReadBool(configLines, "EnableUI", true))
            {
                string directory = Environment.CurrentDirectory + "/Scripts/SmoothDrivingV/UI/";

                fpsDisableUI = Config.ReadBool(configLines, "DisableUIInFirstPerson", false);
                speedUnit = Config.ReadString(configLines, "SpeedUnit", "MPH");
                speedUnitMultiplier = Config.ReadFloat(configLines, "SpeedUnitMultiplier", 2.236936f);
                cruiseSpeedIncrement = Config.ReadFloat(configLines, "CruiseSpeedIncrement", 15.0f) / speedUnitMultiplier;
                useWheelSpeed = Config.ReadBool(configLines, "UseWheelSpeed", false);
                speedSmoothing = Config.ReadFloat(configLines, "SpeedSmoothing", 0.2f);
                background = CreateTexture(directory + "background.png");
                overlay = CreateTexture(directory + "overlay.png");
                bar = CreateTexture(directory + "bar.png");
                raceHighlight = CreateTexture(directory + "race.png");
                indicatorLight = CreateTexture(directory + "indicator.png");
                headlightsLight = CreateTexture(directory + "headlights.png");
                highbeamsLight = CreateTexture(directory + "highbeams.png");

                string[] files = Directory.GetFiles(directory + "/Font/");
                foreach (string file in files)
                {
                    string name = file.Split('/').Last();
                    if (name.EndsWith(".png"))
                    {
                        characterSet.Add((char)int.Parse(name.Replace(".png", "")), CreateTexture(file));
                    }
                }

                aspectRatio = Screen.AspectRatio;
                string anchor = Config.ReadString(configLines, "Anchor", "BR").Trim();
                if (anchor.Length < 2) anchor = "BR";
                margin = Config.ReadFloat(configLines, "Margin", 0.025f);
                masterScale = Config.ReadFloat(configLines, "MasterScale", 0.1224609375f);
                masterScaleRace = masterScale * 1.1961722488f;
                float halfScale = masterScale / 2.0f;

                switch (anchor[1])
                {
                    case 'L': centerX = 0.0f; break;
                    case 'C': centerX = 0.5f; break;
                    case 'R': centerX = 1.0f; break;
                    default: centerX = 1.0f; break;
                }
                switch (anchor[0])
                {
                    case 'T': centerY = 0.0f; break;
                    case 'C': centerY = 0.5f; break;
                    case 'B': centerY = 1.0f; break;
                    default: centerY = 1.0f; break;
                }

                if (anchor[1] == 'L') centerX += halfScale + margin;
                else if (anchor[1] == 'R') centerX -= halfScale + margin;

                if (anchor[0] == 'T') centerY += (halfScale + margin) * aspectRatio;
                else if (anchor[0] == 'B') centerY -= (halfScale + margin) * aspectRatio;

                centerX += Config.ReadFloat(configLines, "CenterXOffset", 0.0f);
                centerY += Config.ReadFloat(configLines, "CenterYOffset", 0.0f) * aspectRatio;

                fontSpacing = Config.ReadFloat(configLines, "FontSpacing", 0.32f);
                rpmBarX = centerX + Config.ReadFloat(configLines, "RPMBarXOffset", 0.346889952153f) * masterScale;
                rpmBarY = centerY + Config.ReadFloat(configLines, "RPMBarYOffset", 0.253588516746f) * masterScale * aspectRatio;
                fuelBarX = centerX + Config.ReadFloat(configLines, "FuelBarXOffset", -0.346889952153f) * masterScale;
                rpmBarSizeX = Config.ReadFloat(configLines, "RPMBarSizeX", 0.129186602871f) * masterScale;
                rpmBarSizeY = Config.ReadFloat(configLines, "RPMBarSizeY", 0.730861244019f) * masterScale;
                largeTextSizeY = Config.ReadFloat(configLines, "LargeTextSize", 5.4f) * masterScale;
                smallTextSizeY = Config.ReadFloat(configLines, "SmallTextSize", 1.44f) * masterScale;

                speedValueY = centerY + masterScale * Config.ReadFloat(configLines, "SpeedValueYOffset", -0.18f) * aspectRatio;
                cruiseSpeedY = speedValueY + masterScale * Config.ReadFloat(configLines, "CruiseSpeedYOffset", -0.2f) * aspectRatio;
                speedUnitY = speedValueY + masterScale * Config.ReadFloat(configLines, "SpeedUnitYOffset", 0.2f) * aspectRatio;
                gearY = speedUnitY + masterScale * Config.ReadFloat(configLines, "GearYOffset", 0.18f) * aspectRatio;
                isLoaded = true;
            }
        }

        public static void Update(bool engine, float speed, bool cruise, bool raceMode, float cruiseSpeed, float rpm, float fuel, int gear, int indicatorState, bool indicatorFlash, bool headlights, bool highbeams)
        {
            float speedSmoothingMultiplier = MathExt.Clamp(1.0f - (float)Math.Pow(speedSmoothing, Time.deltaTime * 5.0f), 0.0f, 1.0f);
            float fadeSmoothingMultiplier = MathExt.Clamp(1.0f - (float)Math.Pow(0.2f, Time.deltaTime * 5.0f), 0.0f, 1.0f);
            displaySpeed += (speed - displaySpeed) * speedSmoothingMultiplier;

            bool enableUI = !fpsDisableUI || (Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE) != 4 && !Function.Call<bool>(Hash.IS_CINEMATIC_CAM_RENDERING));
            masterAlpha += ((enableUI ? 1.0f : 0.0f) - masterAlpha) * fadeSmoothingMultiplier;
            if (masterAlpha <= 0.00392156862745f) masterAlpha = 0.0f;
            else if (masterAlpha >= 0.996078431373f) masterAlpha = 1.0f;

            fadeAlpha += ((engine ? 1.0f : 0.0f) - fadeAlpha) * fadeSmoothingMultiplier;
            if (fadeAlpha <= 0.00392156862745f) fadeAlpha = 0.0f;
            else if (fadeAlpha >= 0.996078431373f) fadeAlpha = 1.0f;

            if (masterAlpha > 0.0f)
            {
                if (raceMode) DrawTexture(raceHighlight, false, 0, masterAlpha, 1.0f, 0.0f, 0.0f, centerX, centerY, masterScaleRace, masterScaleRace, 0.0f, 0.5f, 0.5f);
                DrawTexture(background, false, 1, masterAlpha * 0.7f, 1.0f, 1.0f, 1.0f, centerX, centerY, masterScale, masterScale, 0.0f, 0.5f, 0.5f);
                DrawTexture(bar, false, 2, masterAlpha * fadeAlpha, 1.0f, 0.0f, 0.0f, rpmBarX, rpmBarY, rpmBarSizeX, rpmBarSizeY * rpm * fadeAlpha, 0.0f, 0.0f, 1.0f);
                DrawTexture(bar, false, 2, masterAlpha * fadeAlpha, fuel < 0.1f ? 1.0f : 0.0f, fuel < 0.1f ? 0.5f : 0.0f, fuel < 0.1f ? 0.0f : 1.0f, fuelBarX, rpmBarY, rpmBarSizeX, rpmBarSizeY * fuel * fadeAlpha, 0.0f, 1.0f, 1.0f);
                DrawTexture(overlay, false, 3, masterAlpha, 1.0f, 1.0f, 1.0f, centerX, centerY, masterScale, masterScale, 0.0f, 0.5f, 0.5f);

                if (engine)
                {
                    if (indicatorFlash)
                    {
                        if (indicatorState == 1 || indicatorState == 3)
                        {
                            DrawTexture(indicatorLight, true, 4, masterAlpha, 1.0f, 1.0f, 1.0f, centerX, centerY, masterScale, masterScale, 0.0f, 0.5f, 0.5f);
                        }
                        if (indicatorState == 2 || indicatorState == 3)
                        {
                            DrawTexture(indicatorLight, true, 4, masterAlpha, 1.0f, 1.0f, 1.0f, centerX, centerY, -masterScale, masterScale, 0.0f, 0.5f, 0.5f);
                        }
                    }

                    if (headlights) DrawTexture(headlightsLight, false, 4, masterAlpha, 1.0f, 1.0f, 1.0f, centerX, centerY, masterScale, masterScale, 0.0f, 0.5f, 0.5f);
                    if (highbeams) DrawTexture(highbeamsLight, false, 4, masterAlpha, 1.0f, 1.0f, 1.0f, centerX, centerY, masterScale, masterScale, 0.0f, 0.5f, 0.5f);
                }

                if (fadeAlpha > 0.0f)
                {
                    if (cruise) DrawText("~ " + (cruiseSpeed * speedUnitMultiplier).ToString("0"), true, 5, centerX, cruiseSpeedY, 0.5f, 0.5f, masterAlpha * fadeAlpha, 0.0f, 1.0f, 0.0f, smallTextSizeY, true);
                    bool nearSpeedLimit = Main.speedLimiter > 0 && displaySpeed / Main.speedLimiter >= 0.95f;
                    DrawText((displaySpeed * speedUnitMultiplier).ToString("0"), true, 5, centerX, speedValueY, 0.5f, 0.5f, masterAlpha * fadeAlpha, 1.0f, nearSpeedLimit ? 0.0f : 1.0f, nearSpeedLimit ? 0.0f : 1.0f, largeTextSizeY, true);
                    DrawText(speedUnit, false, 5, centerX, speedUnitY, 0.5f, 0.5f, masterAlpha * fadeAlpha, 1.0f, 1.0f, 1.0f, smallTextSizeY, true);
                    DrawText(Main.isElectric ? (gear == 0 ? "Reverse" : "Forward") : gearText[gear], true, 5, centerX, gearY, 0.5f, 0.7f, masterAlpha * fadeAlpha, 1.0f, 1.0f, 1.0f, smallTextSizeY, true);
                }
            }
        }

        public static void Reset()
        {
            displaySpeed = 0.0f;
            fadeAlpha = 0.0f;
            masterAlpha = 0.0f;
        }
    }
}
