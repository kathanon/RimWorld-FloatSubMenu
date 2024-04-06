﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TextureTools {
    public static class TextureExtensions {
        public static Texture2D Overlay(this Texture2D orig, Texture2D overlay, float alpha = 1f) {
            return MakeTexture(orig.width, orig.height, Draw);

            void Draw(RenderTexture render) {
                // Copy original
                Graphics.Blit(orig, render);

                // Add overlay texture
                Material mat = MaterialPool.MatFrom(overlay, ShaderDatabase.MetaOverlay, new Color(1f, 1f, 1f, alpha));
                Graphics.Blit(overlay, render, mat);
            }
        }

        public static Texture2D Transform(this Texture2D orig, Rect pos, IntVec2 imageSize = default) {
            if (imageSize == default) { 
                imageSize = new IntVec2(orig.width, orig.height);
            }
            return MakeTexture(imageSize.x, imageSize.z, Draw);

            void Draw(RenderTexture render) {
                var origSize = new Vector2(orig.width, orig.height);
                var scale = imageSize.ToVector2() / origSize / pos.size;
                var offset = new Vector2(-pos.x, -pos.yMax) * scale;
                Graphics.Blit(orig, render, scale, offset);
            }
        }

        private static Texture2D MakeTexture(int width, int height, Action<RenderTexture> draw) {
            var render = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            // Apply drawing function
            draw(render);

            // Create texture
            RenderTexture.active = render;
            var res = new Texture2D(width, height);
            res.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            res.Apply();

            // Cleanup
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(render);
            return res;
        }
    }
}
