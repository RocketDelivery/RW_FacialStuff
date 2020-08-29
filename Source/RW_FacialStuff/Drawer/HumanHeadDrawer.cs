﻿using FacialStuff.AnimatorWindows;
using FacialStuff.GraphicsFS;
using FacialStuff.Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using FacialStuff.Animator;
using UnityEngine;
using Verse;
using FacialStuff.Defs;

namespace FacialStuff
{
    public class HumanHeadDrawer : PawnHeadDrawer
    {
        #region Public Methods

        public override void DrawBasicHead(
        Vector3 drawLoc,
            Quaternion headQuat,
            RotDrawMode bodyDrawType,
            bool headStump,
            bool portrait,
            out bool headDrawn)
        {
            Material headMaterial = this.Graphics.HeadMatAt_NewTemp(this.HeadFacing, bodyDrawType, headStump);
            if (headMaterial != null)
            {
                GenDraw.DrawMeshNowOrLater(this.GetPawnMesh(false, portrait),
                                           drawLoc,
                                           headQuat,
                                           headMaterial,
                                           portrait);
                headDrawn = true;
            }
            else
            {
                headDrawn = false;
            }
        }

        public override void DrawBeardAndTache(Vector3 beardLoc, Vector3 tacheLoc, Quaternion headQuat, bool portrait)
        {
            Mesh headMesh = this.GetPawnMesh(false, portrait);
            if (this.CompFace.FaceData.BeardDef.IsBeardNotHair())
            {
                headMesh = this.GetPawnHairMesh(portrait);
            }
            Material beardMat = this.CompFace.FaceMaterial.BeardMatAt(this.HeadFacing);
            Material moustacheMatAt = this.CompFace.FaceMaterial.MoustacheMatAt(this.HeadFacing);

            if (beardMat != null)
            {
                GenDraw.DrawMeshNowOrLater(headMesh, beardLoc, headQuat, beardMat, portrait);
            }

            if (moustacheMatAt != null)
            {
                GenDraw.DrawMeshNowOrLater(headMesh, tacheLoc, headQuat, moustacheMatAt, portrait);
            }
        }

        public override void DrawBrows(Vector3 drawLoc, Quaternion headQuat, bool portrait)
        {
            Material browMat = this.CompFace.FaceMaterial.BrowMatAt(this.HeadFacing);
            if (browMat == null)
            {
                return;
            }

            Mesh eyeMesh = MeshPoolFS.GetFaceMesh(CompFace.PawnCrownType, HeadFacing, false);
            GenDraw.DrawMeshNowOrLater(
                eyeMesh,
                drawLoc,
                headQuat,
                browMat,
                portrait);
        }

        public override void DrawNaturalEyes(Vector3 drawLoc, Quaternion headQuat, bool portrait)
        {
            FaceGraphic faceGraphic = CompFace.PawnFaceGraphic;
            if(faceGraphic == null)
            {
                return;
            }
            for(int partIdx = 0; partIdx < CompFace.PartStatusTracker.EyeCount; ++partIdx)
			{
                PerEyeBehavior perEyeBehavior = CompFace.Props.perEyeBehaviors[partIdx];
                bool shouldDraw = (perEyeBehavior.drawDirBitFlag & (1 << HeadFacing.AsInt)) != 0;
                if(shouldDraw)
				{
                    Mesh eyeMesh = MeshPoolFS.GetFaceMesh(CompFace.PawnCrownType, HeadFacing, perEyeBehavior.drawMirrored);
                    Material eyeMat = faceGraphic.EyeMatAt(
                        partIdx, 
                        HeadFacing, 
                        portrait, 
                        CompFace.FacialExpressionAI.IsEyeOpen(partIdx),
                        CompFace.PartStatusTracker.GetEyePartLevel(partIdx));
                    if(eyeMat != null)
                    {
                        drawLoc.y += Offsets.YOffset_LeftPart;
                        GenDraw.DrawMeshNowOrLater(
                            eyeMesh,
                            drawLoc,
                            headQuat,
                            eyeMat,
                            portrait);
                    }
                }
			}
        }

        public override void DrawNaturalMouth(Vector3 drawLoc, Quaternion headQuat, bool portrait)
        {
            float mouthMood = 0;
            MouthState mouthState = CompFace.FacialExpressionAI.GetMouthState(ref mouthMood);
            Material mouthMat = CompFace.PawnFaceGraphic.MouthMatAt(HeadFacing, portrait, mouthState, mouthMood);
            if (mouthMat == null)
            {
                return;
            }
            Mesh meshMouth = MeshPoolFS.GetFaceMesh(CompFace.PawnCrownType, HeadFacing, false);
            Vector3 mouthOffset = MeshPoolFS.mouthOffsetsHeadType[(int)CompFace.FullHeadType];
            switch(HeadFacing.AsInt)
            {
                case 1: 
                    mouthOffset = new Vector3(mouthOffset.x, 0f, mouthOffset.y);
                    break;
                case 2: 
                    mouthOffset = new Vector3(0, 0f, mouthOffset.y);
                    break;
                case 3: 
                    mouthOffset = new Vector3(-mouthOffset.x, 0f, mouthOffset.y);
                    break;
                default: 
                    mouthOffset = Vector3.zero;
                    break;
            }
            Vector3 mouthLoc = drawLoc + headQuat * mouthOffset;
            GenDraw.DrawMeshNowOrLater(meshMouth, mouthLoc, headQuat, mouthMat, portrait);
        }

        public override void DrawUnnaturalEyeParts(Vector3 drawLoc, Quaternion headQuat, bool portrait)
        {
            Mesh headMesh = this.GetPawnMesh(false, portrait);
            if (this.CompFace.BodyStat.EyeLeft == PartStatus.Artificial)
            {
                Material leftBionicMat = this.CompFace.FaceMaterial.EyeLeftPatchMatAt(this.HeadFacing);
                if (leftBionicMat != null)
                {
                    Vector3 left = drawLoc;
                    left.y += Offsets.YOffset_LeftPart;
                    GenDraw.DrawMeshNowOrLater(
                                               headMesh,
                                               left,
                                               headQuat,
                                               leftBionicMat,
                                               portrait);
                }
            }

            if (this.CompFace.BodyStat.EyeRight == PartStatus.Artificial)
            {
                Material rightBionicMat = this.CompFace.FaceMaterial.EyeRightPatchMatAt(this.HeadFacing);

                if (rightBionicMat != null)
                {
                    Vector3 right = drawLoc;
                    right.y += Offsets.YOffset_RightPart;
                    GenDraw.DrawMeshNowOrLater(
                                               headMesh,
                                               right,
                                               headQuat,
                                               rightBionicMat,
                                               portrait);
                }
            }
        }
        
        public override void DrawWrinkles(
         Vector3 drawLoc,
            RotDrawMode bodyDrawType,
            Quaternion headQuat,
            bool portrait)
        {
            if (!Controller.settings.UseWrinkles)
            {
                return;
            }

            Material wrinkleMat = this.CompFace.FaceMaterial.WrinkleMatAt(this.HeadFacing, bodyDrawType);

            if (wrinkleMat == null)
            {
                return;
            }

            Mesh headMesh = this.GetPawnMesh(false, portrait);
            GenDraw.DrawMeshNowOrLater(headMesh, drawLoc, headQuat, wrinkleMat, portrait);
        }
        
        public override void Initialize()
        {
            base.Initialize();
            this.CompAnimator = this.Pawn.GetComp<CompBodyAnimator>();
        }

        public override void Tick(Rot4 bodyFacing, Rot4 headFacing, PawnGraphicSet graphics)
        {
            // Do I need this? Seems pretty emtpy by now

            base.Tick(bodyFacing, headFacing, graphics);

            CompBodyAnimator animator = this.CompAnimator;
            if (animator == null)
            {
            }

            // var curve = bodyFacing.IsHorizontal ? this.walkCycle.BodyOffsetZ : this.walkCycle.BodyOffsetVerticalZ;
        }

        #endregion Public Methods
    }
}