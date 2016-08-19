﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;


namespace FaceStyling
{
    class FaceStyler : Building
    {

        public override void SpawnSetup()
        {
            base.SpawnSetup();

        }

        public void FaceStyling(Pawn pawn)
        {
            Find.WindowStack.Add((Window)new FaceStyling.Dialog_FaceStyling(pawn));
        }


        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            {
                if (!myPawn.CanReserve(this))
                {

                    FloatMenuOption item = new FloatMenuOption("CannotUseReserved".Translate(), null);
                    return new List<FloatMenuOption>
                {
                    item
                };
                }
                if (!myPawn.CanReach(this, PathEndMode.Touch, Danger.Some))
                {
                    FloatMenuOption item2 = new FloatMenuOption("CannotUseNoPath".Translate(), null);
                    return new List<FloatMenuOption>
                {
                    item2
                };

                }

                Action action2 = delegate
                {
                    // IntVec3 InteractionSquare = (this.Position + new IntVec3(0, 0, 1)).RotatedBy(this.Rotation);
                    Job ShallWalkTheValleyOfSquierrls = new Job(DefDatabase<JobDef>.GetNamed("FaceStyleChanger"), this, InteractionCell);
                    if (myPawn.drafter.CanTakeOrderedJob())//This is used to force go job, it will work even when drafted
                    {
                        myPawn.drafter.TakeOrderedJob(ShallWalkTheValleyOfSquierrls);
                    }
                    else
                    {
                        myPawn.QueueJob(ShallWalkTheValleyOfSquierrls);
                        myPawn.jobs.StopAll();
                    }

                };
                list.Add(new FloatMenuOption("ClutterStylizeHair".Translate(), action2));

                if (myPawn.apparel.WornApparelCount <= 0)
                {
                    FloatMenuOption item3 = new FloatMenuOption("ClutterImNaked".Translate(), null);
                    return new List<FloatMenuOption>
                {
                    item3
                };

                }

                Action action1 = delegate
                {
                    // IntVec3 InteractionSquare = (this.Position + new IntVec3(0, 0, 1)).RotatedBy(this.Rotation);
                    Job ShallWalkTheValleyOfSquierrls = new Job(DefDatabase<JobDef>.GetNamed("ClutterColorChanger"), this, InteractionCell);
                    if (myPawn.drafter.CanTakeOrderedJob())//This is used to force go job, it will work even when drafted
                    {
                        myPawn.drafter.TakeOrderedJob(ShallWalkTheValleyOfSquierrls);
                    }
                    else
                    {
                        myPawn.QueueJob(ShallWalkTheValleyOfSquierrls);
                        myPawn.jobs.StopAll();
                    }

                };
           //     list.Add(new FloatMenuOption("ClutterColorCloths".Translate(), action1));



            }
            return list;
        }

    }
}