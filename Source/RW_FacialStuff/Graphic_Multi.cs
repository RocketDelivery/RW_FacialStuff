﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace RW_FacialStuff
{

    //  public class GraphicModded: Graphic
    //  {
    //      public virtual Material MatFrontNarrow { get; }
    //      public virtual Material MatSideNarrow { get; }
    //  }

    public class Graphic_Multi_Head : Graphic
    {
        private Material[] mats = new Material[5];

        public string GraphicPath
        {
            get
            {
                return path;
            }
        }

        public override Material MatSingle
        {
            get
            {
                return mats[2];
            }
        }

        public override Material MatFront
        {
            get
            {
                return mats[2];
            }
        }

        public override Material MatSide
        {
            get
            {
                return mats[1];
            }
        }

        public override Material MatBack
        {
            get
            {
                return mats[0];
            }
        }

        public override bool ShouldDrawRotated
        {
            get
            {
                return MatSide == MatBack;
            }
        }

        private static Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();


        public static Texture2D BlankTexture()
        {

          Texture2D blankTexture = null;
          if (textureCache.TryGetValue("blankBack", out blankTexture)) return blankTexture;


             blankTexture = new Texture2D(128, 128);
            int startX = 0;
            int startY = 0;

            for (int x = startX; x < blankTexture.width; x++)
            {
                for (int y = startY; y < blankTexture.height; y++)
                {
                    blankTexture.SetPixel(x, y, Color.clear);
                }
            }
                    textureCache.Add("blankBack", blankTexture);

            return blankTexture;
        }

        public static Texture2D LoadTexture(string texturePath)
        {
            Texture2D texture = null;

            texture = new Texture2D(1, 1);
            texture.LoadImage(File.ReadAllBytes(GraphicDatabaseHeadRecordsModded.modpath + texturePath + ".png"));
            texture.anisoLevel = 8;

            return texture;
        }

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;
            Texture2D[] array = new Texture2D[3];

            if (File.Exists(GraphicDatabaseHeadRecordsModded.modpath + req.path + "_back.png"))
                array[0] = LoadTexture(req.path + "_back");
            else
                array[0] = BlankTexture();

            array[1] = LoadTexture(req.path + "_side");
            array[2] = LoadTexture(req.path + "_front");



            //          array[0] = ContentFinder<Texture2D>.Get(req.path + "_back", false);
            //          if (array[0] == null)
            //          {
            //              Log.Error("RW_FacialStuff: Failed to find any texture while constructing " + this.ToString());
            //              return;
            //          }
            //          array[1] = ContentFinder<Texture2D>.Get(req.path + "_side", false);
            //          if (array[1] == null)
            //          {
            //              array[1] = array[0];
            //          }
            //          array[2] = ContentFinder<Texture2D>.Get(req.path + "_front", false);
            //          if (array[2] == null)
            //          {
            //              array[2] = array[0];
            //          }
            //      Texture2D[] array2 = new Texture2D[3];
            //      if (req.shader.SupportsMaskTex())
            //      {
            //          array2[0] = ContentFinder<Texture2D>.Get(req.path + "_backm", false);
            //          if (array2[0] != null)
            //          {
            //              array2[1] = ContentFinder<Texture2D>.Get(req.path + "_sidem", false);
            //              if (array2[1] == null)
            //              {
            //                  array2[1] = array2[0];
            //              }
            //              array2[2] = ContentFinder<Texture2D>.Get(req.path + "_frontm", false);
            //              if (array2[2] == null)
            //              {
            //                  array2[2] = array2[0];
            //              }
            //          }
            //      }
            for (int i = 0; i < 3; i++)
            {
                MaterialRequest req2 = default(MaterialRequest);
                req2.mainTex = array[i];
                req2.shader = req.shader;
                req2.color = this.color;
                req2.colorTwo = this.colorTwo;
                //           req2.maskTex = array2[i];
                mats[i] = MaterialPool.MatFrom(req2);
            }
        }

        //   public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        //   {
        //       return GraphicDatabase.Get<Graphic_Multi>(this.path, newShader, this.drawSize, newColor, newColorTwo, this.data);
        //   }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "Multi(initPath=",
                path,
                ", color=",
                this.color,
                ", colorTwo=",
                this.colorTwo,
                ")"
            });
        }

        public override int GetHashCode()
        {
            int seed = 0;
            seed = Gen.HashCombine<string>(seed, this.path);
            seed = Gen.HashCombineStruct<Color>(seed, this.color);
            return Gen.HashCombineStruct<Color>(seed, this.colorTwo);
        }
    }
}