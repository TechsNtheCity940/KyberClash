using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KyberKlash.EditorTools
{
    public static class KyberKlashPresentationSkinInstaller
    {
        private const string UiRoot = "Assets/Resources/KyberKlash/UI";
        private const string AudioRoot = "Assets/Resources/KyberKlash/Audio";
        private const int SampleRate = 44100;

        [MenuItem("Tools/Kyber Clash/Presentation/Install Themed Menu Skin")]
        public static void InstallThemedMenuSkin()
        {
            EnsureFolders();

            WriteButton($"{UiRoot}/menu_button_normal.png", new Color32(7, 17, 32, 230), new Color32(108, 226, 255, 255), new Color32(28, 88, 136, 170));
            WriteButton($"{UiRoot}/menu_button_hover.png", new Color32(12, 31, 55, 240), new Color32(170, 245, 255, 255), new Color32(83, 190, 255, 210));
            WriteButton($"{UiRoot}/menu_button_pressed.png", new Color32(36, 9, 24, 245), new Color32(255, 83, 157, 255), new Color32(145, 28, 90, 210));
            WritePanel($"{UiRoot}/menu_panel_frame.png", 720, 360, new Color32(4, 13, 26, 218), new Color32(80, 214, 255, 240));
            WritePanel($"{UiRoot}/character_card_frame.png", 256, 256, new Color32(5, 12, 23, 225), new Color32(72, 184, 240, 230));
            WritePanel($"{UiRoot}/character_card_selected.png", 256, 256, new Color32(18, 24, 42, 235), new Color32(255, 74, 148, 245));
            WriteLoadingBar($"{UiRoot}/loading_bar_frame.png", false);
            WriteLoadingBar($"{UiRoot}/loading_bar_fill.png", true);

            for (int i = 0; i < 8; i++)
            {
                WriteSpinnerFrame($"{UiRoot}/loading_spinner_{i}.png", i);
            }

            WriteMusicIfMissing($"{AudioRoot}/music_title.wav", MusicMood.Title);
            WriteMusicIfMissing($"{AudioRoot}/music_character_select.wav", MusicMood.CharacterSelect);
            WriteMusicIfMissing($"{AudioRoot}/music_gameplay_battle.wav", MusicMood.Gameplay);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[KyberKlash][PresentationSkin] Installed themed menu sprites, loading animation frames, and loopable music.");
        }

        public static void InstallThemedMenuSkinBatch()
        {
            InstallThemedMenuSkin();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void WriteButton(string path, Color32 baseColor, Color32 edgeColor, Color32 glowColor)
        {
            const int width = 512;
            const int height = 128;
            Texture2D texture = CreateClearTexture(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool outer = InsideBeveledRect(x, y, width, height, 30, 0);
                    if (!outer)
                    {
                        float glow = EdgeGlow(x, y, width, height, 30, 8);
                        if (glow > 0f)
                        {
                            Color c = glowColor;
                            c.a *= glow * 0.55f;
                            texture.SetPixel(x, y, c);
                        }
                        continue;
                    }

                    bool inner = InsideBeveledRect(x, y, width, height, 24, 8);
                    Color color = inner ? baseColor : edgeColor;
                    float v = y / (float)(height - 1);
                    float centerLine = Mathf.Exp(-Mathf.Pow((v - 0.52f) * 8.5f, 2f));
                    color = Color.Lerp(color, glowColor, inner ? centerLine * 0.32f : 0.12f);

                    if (inner && (Mathf.Abs(y - height * 0.5f) < 1.8f || Mathf.Abs(y - height * 0.18f) < 1.2f))
                    {
                        color = Color.Lerp(color, edgeColor, 0.55f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            WritePngAndConfigure(path, texture, new Vector4(44, 36, 44, 36));
        }

        private static void WritePanel(string path, int width, int height, Color32 baseColor, Color32 edgeColor)
        {
            Texture2D texture = CreateClearTexture(width, height);
            Color glowColor = new Color32(125, 228, 255, 180);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool outer = InsideBeveledRect(x, y, width, height, 28, 0);
                    if (!outer)
                    {
                        float glow = EdgeGlow(x, y, width, height, 28, 10);
                        if (glow > 0f)
                        {
                            Color c = glowColor;
                            c.a *= glow * 0.45f;
                            texture.SetPixel(x, y, c);
                        }
                        continue;
                    }

                    bool inner = InsideBeveledRect(x, y, width, height, 20, 10);
                    Color color = inner ? baseColor : edgeColor;
                    float scan = Mathf.Sin((y + x * 0.08f) * 0.22f) * 0.5f + 0.5f;
                    if (inner)
                    {
                        color = Color.Lerp(color, new Color32(25, 64, 96, 230), scan * 0.07f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            WritePngAndConfigure(path, texture, new Vector4(48, 48, 48, 48));
        }

        private static void WriteLoadingBar(string path, bool fill)
        {
            const int width = 512;
            const int height = 64;
            Texture2D texture = CreateClearTexture(width, height);
            Color32 edge = fill ? new Color32(166, 242, 255, 255) : new Color32(80, 200, 245, 230);
            Color32 body = fill ? new Color32(38, 170, 255, 220) : new Color32(6, 14, 28, 190);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool outer = InsideBeveledRect(x, y, width, height, 16, 0);
                    if (!outer) continue;

                    bool inner = InsideBeveledRect(x, y, width, height, 12, fill ? 3 : 6);
                    Color color = inner ? body : edge;
                    if (fill && inner)
                    {
                        float pulse = Mathf.Sin(x * 0.045f) * 0.5f + 0.5f;
                        color = Color.Lerp(color, edge, pulse * 0.35f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            WritePngAndConfigure(path, texture, new Vector4(24, 20, 24, 20));
        }

        private static void WriteSpinnerFrame(string path, int frame)
        {
            const int size = 256;
            Texture2D texture = CreateClearTexture(size, size);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float radius = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x);
                    if (angle < 0f) angle += Mathf.PI * 2f;

                    float rotating = Mathf.Repeat(angle / (Mathf.PI * 2f) + frame / 8f, 1f);
                    bool ring = radius > 78f && radius < 94f;
                    bool tick = ring && rotating < 0.62f && Mathf.Repeat(rotating * 8f, 1f) < 0.42f;
                    bool crystal = Mathf.Abs(p.x) + Mathf.Abs(p.y * 0.62f) < 34f;

                    if (!tick && !crystal) continue;

                    Color color = tick
                        ? Color.Lerp(new Color32(44, 155, 255, 160), new Color32(245, 90, 170, 255), rotating)
                        : new Color32(190, 245, 255, 220);
                    texture.SetPixel(x, y, color);
                }
            }

            WritePngAndConfigure(path, texture, Vector4.zero);
        }

        private static Texture2D CreateClearTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }
            return texture;
        }

        private static bool InsideBeveledRect(int x, int y, int width, int height, int bevel, int inset)
        {
            int left = inset;
            int right = width - inset - 1;
            int bottom = inset;
            int top = height - inset - 1;
            if (x < left || x > right || y < bottom || y > top) return false;

            int localX = x - left;
            int localY = y - bottom;
            int localRight = right - x;
            int localTop = top - y;
            int b = Mathf.Max(0, bevel - inset);

            if (localX < b && localY < b && localX + localY < b) return false;
            if (localX < b && localTop < b && localX + localTop < b) return false;
            if (localRight < b && localY < b && localRight + localY < b) return false;
            if (localRight < b && localTop < b && localRight + localTop < b) return false;
            return true;
        }

        private static float EdgeGlow(int x, int y, int width, int height, int bevel, int range)
        {
            for (int i = 1; i <= range; i++)
            {
                if (InsideBeveledRect(x, y, width, height, bevel, -i))
                {
                    return 1f - (i - 1f) / range;
                }
            }
            return 0f;
        }

        private static void WritePngAndConfigure(string path, Texture2D texture, Vector4 border)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static void WriteMusicIfMissing(string path, MusicMood mood)
        {
            if (File.Exists(path))
            {
                return;
            }

            float duration = mood == MusicMood.Gameplay ? 18f : 14f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            short[] samples = new short[sampleCount * 2];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float value = BuildMusicSample(t, mood);
                short pcm = (short)Mathf.Clamp(value * short.MaxValue, short.MinValue, short.MaxValue);
                samples[i * 2] = pcm;
                samples[i * 2 + 1] = pcm;
            }

            WriteWav(path, samples, 2, SampleRate);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static float BuildMusicSample(float t, MusicMood mood)
        {
            float tempo = mood == MusicMood.Gameplay ? 142f : mood == MusicMood.CharacterSelect ? 126f : 96f;
            float beat = t * tempo / 60f;
            int step = Mathf.FloorToInt(beat * 2f);
            float[] scale = { 0f, 3f, 5f, 7f, 10f, 12f, 15f, 17f };
            float root = mood == MusicMood.Gameplay ? 73.42f : mood == MusicMood.CharacterSelect ? 82.41f : 65.41f;
            float note = root * Mathf.Pow(2f, scale[Mathf.Abs(step) % scale.Length] / 12f);

            float lead = Mathf.Sin(Mathf.PI * 2f * note * t) * 0.12f;
            float fifth = Mathf.Sin(Mathf.PI * 2f * note * 1.5f * t) * 0.06f;
            float drone = Mathf.Sin(Mathf.PI * 2f * root * 0.5f * t) * 0.12f;
            float shimmer = Mathf.Sin(Mathf.PI * 2f * (note * 4f + Mathf.Sin(t * 2f) * 9f) * t) * 0.035f;
            float kick = Mathf.Exp(-Mathf.Repeat(beat, 1f) * 18f) * 0.18f * Mathf.Sin(Mathf.PI * 2f * 58f * t);
            float hat = (Mathf.Repeat(beat * 4f, 1f) < 0.08f ? 1f : 0f) * Mathf.Sin(Mathf.PI * 2f * 4200f * t) * 0.025f;

            float intensity = mood == MusicMood.Title ? 0.7f : mood == MusicMood.CharacterSelect ? 0.85f : 1f;
            return (lead + fifth + drone + shimmer + kick + hat) * intensity;
        }

        private static void WriteWav(string path, short[] samples, short channels, int sampleRate)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                int dataLength = samples.Length * sizeof(short);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * sizeof(short));
                writer.Write((short)(channels * sizeof(short)));
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);

                for (int i = 0; i < samples.Length; i++)
                {
                    writer.Write(samples[i]);
                }
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/KyberKlash");
            EnsureFolder(UiRoot);
            EnsureFolder(AudioRoot);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private enum MusicMood
        {
            Title,
            CharacterSelect,
            Gameplay
        }
    }
}
