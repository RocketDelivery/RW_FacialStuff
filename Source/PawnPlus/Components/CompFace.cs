﻿using FacialStuff.Animator;
using FacialStuff.DefOfs;
using FacialStuff.Defs;
using FacialStuff.GraphicsFS;
using FacialStuff.Utilities;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using FacialStuff.Harmony;
using UnityEngine;
using Verse;
using static FacialStuff.Offsets;
using FacialStuff.AI;

namespace FacialStuff
{
    public class CompFace : ThingComp
    {
        public FaceGraphic PawnFaceGraphic;

        private Faction _originFactionInt;
        private FaceData _faceData;
        private RenderParam[,] _eyeRenderParams;
        private RenderParam[] _mouthRenderParams;
        private Rot4 _cachedHeadFacing;
        private IMouthBehavior.Params _cachedMouthParam = new IMouthBehavior.Params();
        private List<IEyeBehavior.Result> _cachedEyeParam;
        private PawnState _pawnState;
        private IHeadBehavior _headBehavior;
        private IMouthBehavior _mouthBehavior;
        private IEyeBehavior _eyeBehavior;
        // Used for distance culling of face details
        private GameComponent_FacialStuff _fsGameComp;
        
        public bool Initialized { get; private set; }

        public IHeadBehavior HeadBehavior => _headBehavior;

        public IMouthBehavior MouthBehavior => _mouthBehavior;

        public IEyeBehavior EyeBehavior => _eyeBehavior;

        public FaceMaterial FaceMaterial { get; set; }
        
        public FullHead FullHeadType { get; set; } = FullHead.Undefined;
        
        public PartStatusTracker PartStatusTracker { get; private set; }
                                
        public Faction OriginFaction => _originFactionInt;
        
        public virtual CrownType PawnCrownType => Pawn?.story.crownType ?? CrownType.Average;

        public FaceData FaceData
        {
            get
			{
                return _faceData;
            }

            set
			{
                _faceData = value;
			}
        }

        public HeadType PawnHeadType
        {
            get
            {
                if(Pawn.story?.HeadGraphicPath != null)
                {
                    if(Pawn.story.HeadGraphicPath.Contains("Pointy"))
                    {
                        return HeadType.Pointy;
                    }

                    if(Pawn.story.HeadGraphicPath.Contains("Wide"))
                    {
                        return HeadType.Wide;
                    }
                }

                return HeadType.Normal;
            }
        }

        public CompProperties_Face Props { get; private set; }
        
        public Pawn Pawn { get; private set; }
        
        public HeadCoverage CurrentHeadCoverage { get; set; }
                        
		// Return true if head was drawn. False if not.
		public bool DrawHead(
            PawnGraphicSet graphicSet,
            RotDrawMode bodyDrawType, 
            bool portrait, 
            bool headStump,
            Rot4 bodyFacing, 
            Rot4 headFacing,
            Vector3 headPos,
            Quaternion headQuat)
        {
            if(Pawn.IsChild())
			{
                return false;
			}
            bool headDrawn = false;
            // Draw head
            headFacing = portrait ? HeadBehavior.GetRotationForPortrait() : headFacing;
            Material headMaterial = graphicSet.HeadMatAt_NewTemp(headFacing, bodyDrawType, headStump);
            if(headMaterial != null)
            {
                GenDraw.DrawMeshNowOrLater(
                    MeshPool.humanlikeHeadSet.MeshAt(headFacing),
                    headPos,
                    headQuat,
                    headMaterial,
                    portrait);
                headDrawn = true;
                if(bodyDrawType != RotDrawMode.Dessicated && !headStump)
                {
                    if(portrait || _fsGameComp.ShouldRenderFaceDetails)
				    {
                        if(EyeBehavior.NumEyes > 0)
                        {
                            Vector3 eyeLoc = headPos;
                            eyeLoc.y += 0.002f;
                            // Draw natural eyes
                            DrawEyes(eyeLoc, headFacing, headQuat, portrait);
                        }
                        if(Props.hasMouth &&
                            FaceData.BeardDef.drawMouth &&
                            Controller.settings.UseMouth)
                        {
                            Vector3 mouthLoc = headPos;
                            mouthLoc.y += YOffset_Mouth;
                            DrawMouth(mouthLoc, headFacing, headQuat, portrait);
                        }
                        if(headFacing != Rot4.North)
						{
                            if(Props.hasWrinkles)
                            {
                                Vector3 wrinkleLoc = headPos;
                                wrinkleLoc.y += YOffset_Wrinkles;
                                // Draw wrinkles
                                DrawWrinkles(wrinkleLoc, headFacing, headQuat, bodyDrawType, portrait);
                            }
                            if(EyeBehavior.NumEyes > 0)
                            {
                                Vector3 browLoc = headPos;
                                browLoc.y += YOffset_Brows;
                                // Draw brows above eyes
                                DrawBrows(browLoc, headFacing, headQuat, portrait);
                            }
                            if(Props.hasBeard)
                            {
                                Vector3 beardLoc = headPos;
                                Vector3 tacheLoc = headPos;
                                beardLoc.y += YOffset_Beard;
                                tacheLoc.y += YOffset_Tache;
                                // Draw beard and mustache
                                DrawBeardAndTache(graphicSet, beardLoc, tacheLoc, headFacing, headQuat, portrait);
                            }
                        }
                    }
                    // When CurrentHeadCoverage == HeadCoverage.None, let the vanilla routine draw the hair
                    if(CurrentHeadCoverage != HeadCoverage.None)
				    {
                        DrawHairBasedOnCoverage(headFacing, headPos, headQuat, portrait);
                    }
                }
            }
            return headDrawn;
        }

        // Draw hair using different cutout masks based on head coverage
        public void DrawHairBasedOnCoverage(
            Rot4 headFacing, 
            Vector3 headDrawLoc, 
            Quaternion headQuat, 
            bool portrait)
		{
            PawnGraphicSet graphics = Pawn.Drawer.renderer.graphics;
            Vector3 hairDrawLoc = headDrawLoc;
            // Constant is "YOffsetIntervalClothes". Adding this will ensure that hair is above the head 
            // and apparel regardless of the head's orientation, but also ensure that it remains below headwear.
            // Kind of arbitrary, but at least it works.
            hairDrawLoc.y += 0.00306122447f;
            Graphic_Hair hairGraphic = graphics.hairGraphic as Graphic_Hair;
            if(hairGraphic != null)
            {
                // Copied from PawnGraphicSet.HairMatAt_NewTemp
                Material hairBasemat = hairGraphic.MatAt(headFacing, CurrentHeadCoverage);
                if(!portrait && Pawn.IsInvisible())
                {
                    // Invisibility shader ignores the mask texture used in this mod's custom hair shader, which
                    // cuts out the parts of hair that are poking through headwear.
                    // However, decompiling vanilla invisibility shader and writing an equivalent shader for this mod's
                    // custom shader is rather difficult. The only downside of using the vanilla invisibility shader
                    // is the graphical artifacts, so fixing it will be a low priority task.
                    hairBasemat = InvisibilityMatPool.GetInvisibleMat(hairBasemat);
                }
                // Similar to the invisibility shader, a separate damaged mat shader needs to be written for this
                // mod's custom hair shader. However, the effect is so subtle that taking time to decompile the vanilla
                // shader and writing a custom shader isn't worth the time.
                // graphics.flasher.GetDamagedMat(baseMat);
                var maskTex = hairBasemat.GetMaskTexture();
                GenDraw.DrawMeshNowOrLater(
                    graphics.HairMeshSet.MeshAt(headFacing),
                    mat: hairBasemat,
                    loc: hairDrawLoc,
                    quat: headQuat,
                    drawNow: portrait);
            } else
            {
                Log.ErrorOnce(
                    "Facial Stuff: " + Pawn.Name + " has CompFace but doesn't have valid hair graphic of Graphic_Hair class",
                    "FacialStuff_CompFaceNoValidHair".GetHashCode());
            }
        }

        public void DrawWrinkles(Vector3 drawPos, Rot4 headFacing, Quaternion headQuat, RotDrawMode bodyDrawType, bool portrait)
		{
            if(!Controller.settings.UseWrinkles)
            {
                return;
            }
            Material wrinkleMat = FaceMaterial.WrinkleMatAt(headFacing, bodyDrawType);
            if(wrinkleMat == null)
            {
                return;
            }
            Mesh headMesh = MeshPool.humanlikeHeadSet.MeshAt(headFacing);
            GenDraw.DrawMeshNowOrLater(headMesh, drawPos, headQuat, wrinkleMat, portrait);
        }

        public void DrawEyes(Vector3 drawPos, Rot4 headFacing, Quaternion headQuat, bool portrait)
		{
            if(PawnFaceGraphic == null || _eyeRenderParams == null)
            {
                return;
            }
            for(int partIdx = 0; partIdx < _eyeRenderParams.GetLength(0); ++partIdx)
            {
                if(_eyeRenderParams[partIdx, headFacing.AsInt].render || portrait)
                {
                    Mesh eyeMesh = _eyeRenderParams[partIdx, headFacing.AsInt].mesh;
                    Material eyeMat = PawnFaceGraphic.EyeMatAt(
                        partIdx,
                        headFacing,
                        portrait,
                        _cachedEyeParam[partIdx].eyeAction,
                        PartStatusTracker.GetEyePartLevel(partIdx));
                    if(eyeMat != null)
                    {
                        Vector3 offset = new Vector3(
                            _eyeRenderParams[partIdx, headFacing.AsInt].offset.x,
                            0,
                            _eyeRenderParams[partIdx, headFacing.AsInt].offset.y);
                        offset = headQuat * offset;
                        GenDraw.DrawMeshNowOrLater(
                            eyeMesh,
                            drawPos + offset,
                            headQuat,
                            eyeMat,
                            portrait);
                    }
                }
            }
        }

        public void DrawBrows(Vector3 drawPos, Rot4 headFacing, Quaternion headQuat, bool portrait)
		{
            Material browMat = FaceMaterial.BrowMatAt(headFacing);
            if(browMat == null)
            {
                return;
            }
            Mesh eyeMesh = MeshPoolFS.GetFaceMesh(PawnCrownType, headFacing, false);
            GenDraw.DrawMeshNowOrLater(
                eyeMesh,
                drawPos,
                headQuat,
                browMat,
                portrait);
        }

        public void DrawMouth(Vector3 drawPos, Rot4 headFacing, Quaternion headQuat, bool portrait)
		{
            if(PawnFaceGraphic == null || _mouthRenderParams == null || !_mouthRenderParams[headFacing.AsInt].render)
			{
                return;
			}
            Mesh mouthMesh = _mouthRenderParams[headFacing.AsInt].mesh;
            int mouthTextureIdx = portrait ?
                MouthBehavior.GetTextureIndexForPortrait() :
                _cachedMouthParam.mouthTextureIdx;
            Material mouthMat = PawnFaceGraphic.MouthMatAt(headFacing, mouthTextureIdx);
            if(mouthMat != null)
            {
                Vector3 offset = new Vector3(
                    _mouthRenderParams[headFacing.AsInt].offset.x,
                    0,
                    _mouthRenderParams[headFacing.AsInt].offset.y);
                offset = headQuat * offset;
                GenDraw.DrawMeshNowOrLater(
                    mouthMesh,
                    drawPos + offset,
                    headQuat,
                    mouthMat,
                    portrait);
            }
        }

        public void DrawBeardAndTache(PawnGraphicSet graphicSet, Vector3 beardLoc, Vector3 tacheLoc, Rot4 headFacing, Quaternion headQuat, bool portrait)
        {
            Mesh headMesh = MeshPool.humanlikeHeadSet.MeshAt(headFacing);
            if(FaceData.BeardDef.IsBeardNotHair())
            {
                headMesh = graphicSet.HairMeshSet.MeshAt(headFacing);
            }
            Material beardMat = FaceMaterial.BeardMatAt(headFacing);
            Material moustacheMatAt = FaceMaterial.MoustacheMatAt(headFacing);
            if(beardMat != null)
            {
                GenDraw.DrawMeshNowOrLater(headMesh, beardLoc, headQuat, beardMat, portrait);
            }
            if(moustacheMatAt != null)
            {
                GenDraw.DrawMeshNowOrLater(headMesh, tacheLoc, headQuat, moustacheMatAt, portrait);
            }
        }

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
            _fsGameComp = Current.Game.GetComponent<GameComponent_FacialStuff>();
            Props = (CompProperties_Face)props;
            Pawn = (Pawn)parent;
            if(PartStatusTracker == null)
            {
                PartStatusTracker = new PartStatusTracker(Pawn.RaceProps.body);
            }
            if(HeadBehavior == null)
            {
                _headBehavior = (IHeadBehavior)Props.headBehavior.Clone();
                HeadBehavior.Initialize(Pawn);
            }
            if(MouthBehavior == null)
            {
                _mouthBehavior = (IMouthBehavior)Props.mouthBehavior.Clone();
            }
            if(EyeBehavior == null)
			{
                _eyeBehavior = (IEyeBehavior)Props.eyeBehavior.Clone();
            }
            _pawnState = new PawnState(Pawn);
        }

        // Graphics and faction data aren't available in ThingComp.Initialize(). Initialize the members related to those in this method,
        // which is called by a postfix to ResolveAllGraphics() method.
        public void InitializeFace()
        {
            if(_originFactionInt == null)
            {
                _originFactionInt = Pawn.Faction ?? Faction.OfPlayer;
            }
            if(FaceData == null)
            {
                FaceData = new FaceData(this, OriginFaction?.def);
            }
            HeadRenderDef.GetCachedHeadRenderParams(Pawn.story.HeadGraphicPath, out _eyeRenderParams, out _mouthRenderParams);
            
            MouthBehavior.InitializeTextureIndex(FaceData.MouthSetDef.texNames.AsReadOnly());
            _cachedEyeParam = new List<IEyeBehavior.Result>(EyeBehavior.NumEyes);
            for(int i = 0; i < EyeBehavior.NumEyes; ++i)
			{
                _cachedEyeParam.Add(new IEyeBehavior.Result()); 
			}

            FullHeadType = MeshPoolFS.GetFullHeadType(Pawn.gender, PawnCrownType, PawnHeadType);
            PawnFaceGraphic = new FaceGraphic(this, EyeBehavior.NumEyes);
            FaceMaterial = new FaceMaterial(this, PawnFaceGraphic);
            Initialized = true;
        }

		public override void CompTick()
		{
			base.CompTick();
            if(Initialized)
            {
                bool canUpdatePawn =
                    Pawn.Map != null &&
                    !Pawn.InContainerEnclosed &&
                    Pawn.Spawned &&
                    !Find.TickManager.Paused;
                if(canUpdatePawn)
                {
                    _pawnState.UpdateState();
                    _cachedMouthParam.Reset();
                    foreach(var eyeParam in _cachedEyeParam)
                    {
                        eyeParam.Reset();
                    }
                    HeadBehavior.Update(Pawn, _pawnState, out _cachedHeadFacing);
                    MouthBehavior.Update(Pawn, _cachedHeadFacing, _pawnState, _cachedMouthParam);
                    EyeBehavior.Update(Pawn, _cachedHeadFacing, _pawnState, _cachedEyeParam);
                }
            }
        }
        
        public Rot4 GetHeadFacing()
		{
            return _cachedHeadFacing;
		}
        
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_References.Look(ref this._originFactionInt, "pawnFaction");
            Scribe_Deep.Look(ref this._faceData, "pawnFace");
            Scribe_Deep.Look(ref _headBehavior, "headBehavior");
            Scribe_Deep.Look(ref _mouthBehavior, "mouthBehavior");
            Scribe_Deep.Look(ref _eyeBehavior, "eyeBehavior");
        }
    }
}