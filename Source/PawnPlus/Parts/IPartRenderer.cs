﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PawnPlus.Parts
{	
	public interface IPartRenderer : ICloneable
	{
		public void Initialize(
			Pawn pawn, 
			BodyDef bodyDef, 
			string defaultTexPath, 
			Dictionary<string, string> namedTexPaths,
			BodyPartSignals bodyPartSignals,
			ref TickDelegate tickDelegate);
		
		public void Render(
			Vector3 rootPos,
			Quaternion rootQuat,
			Rot4 rootRot4,
			Vector3 renderNodeOffset,
			Mesh renderNodeMesh,
			bool portrait);
	}
}
