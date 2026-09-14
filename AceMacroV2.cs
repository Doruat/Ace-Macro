global using BTD_Mod_Helper.Extensions;
using MelonLoader;
using BTD_Mod_Helper;
using AceMacroV2;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using System;
using System.Linq;
using Il2CppAssets.Scripts.Models.Map;
using System.Collections.Immutable;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Models.Profile;
using UnityEngine.UI;
using UnityEngine.InputSystem.Utilities;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Data.Behaviors.Attacks;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu.TowerSelectionMenuThemes;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Data.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Simulation.SMath;
using Il2CppAssets.Scripts.Data.Behaviors.Events.Triggers;
using NAudio.Wave.SampleProviders;
using UnityEngine.Playables;
using Math = Il2CppAssets.Scripts.Simulation.SMath.Math;
using Il2CppAssets.Scripts.Data.MapSets;
using UnityEngine.UIElements;
using Il2CppSystem.Collections.Generic;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Il2CppSystem.Linq.Expressions;
using Il2CppSystem.Linq;
using UnityEngine.UIElements.UIR;
using BTD_Mod_Helper.Api.ModOptions;
using System.ComponentModel;
using Il2Cpp;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts;
using System.Text.RegularExpressions;
using Il2CppAssets.Scripts.Utils;
using Il2CppAssets.Scripts.Simulation.Artifacts.Behaviors;
using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using UnityEngine.Rendering.Universal;
using Il2CppNinjaKiwi.GUTS.Utils.ElasticSearch;
using UnityEngine.InputSystem;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.Utils;
using Il2CppAssets.Scripts.Simulation.Objects;
using System.IO;
using Il2CppAssets.Scripts.Simulation.Track;
using Il2CppAssets.Scripts.Data.Behaviors.Abilities;
using System.Diagnostics.Tracing;
using Il2CppAssets.Scripts.Simulation.Bloons.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Mods;
using Il2CppTMPro;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using System.Xml;
using Il2CppAssets.Scripts.Unity.Feats.List;
using UnityEngine.Experimental.GlobalIllumination;
using Il2CppAssets.Scripts.Utils.Messaging;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Data.Legends;
using Il2CppAssets.Scripts.Unity.GameEditor.UI.PopupPanels;

[assembly: MelonInfo(typeof(AceMacroV2.AceMacroV2), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6-Epic")]

namespace AceMacroV2;

public class AceMacroV2 : BloonsTD6Mod
{
    public override void OnApplicationStart()
    {
        ModHelper.Msg<AceMacroV2>("AceMacroV2 loaded!");
    }

    public static readonly ModSettingBool active = new(false);
    public static readonly ModSettingInt startDelay = new ModSettingInt(-1);
    public static readonly ModSettingInt oneOverRate = new ModSettingInt(60);
    public static Vector2 acePos = new();
    public static bool reverse = false, stayOnRight = true;
    public static AirUnit airUnit;
    public static Vector3 startingPos;
    public static PathMovement pathMovement;
    public static int elapsed, lastElapsed, lastrev;
    public static Bloon? targetBloon;
    public static float rate, direction;
    public static int rndm;
    public static Vector2 farPoint;

    public static float distance(Vector2 acepos, Vector2 bloonpos, Vector2 zomgpos)
    {
        Vector2 direction = bloonpos - acepos, temp = zomgpos - acepos;
        float t = (direction.x * temp.x + direction.y * temp.y) / direction.MagnitudeSquared;
        t = t < 0 ? 1 : t > 1 ? 1 : t;
        Vector2 point = new(acepos.x + t * direction.x, acepos.y + t * direction.y);
        return (zomgpos - point).Magnitude;
    }
    public static void Rev(int elapsed)
    {
        foreach (var ps in pathMovement.pathSuppliers)
        {
            ps.reverse = !ps.reverse;
        }
        pathMovement.t += pathMovement.currPathSupplier.reverse ? -0.1f : 0.1f;
        pathMovement.onPath = false;
        reverse = !reverse;
        lastrev = elapsed;
    }

    public static Vector3 CalcPath(Vector3 acepos, Vector2 targetPos)
    {
        //duration=6;
        var apos = new Vector2(acepos.x, acepos.y);
        var dir = acepos.z;
        var speed = pathMovement.pathMovementModel.speed / 60;
        var targetvec = targetPos - apos;
        var targetrot = targetvec.Rotation;
        var targetangle = UnityEngine.Mathf.DeltaAngle(dir, targetrot);
        if (targetangle < -6 * 60 / oneOverRate)
            targetangle = -6 * 60 / oneOverRate;
        if (6 * 60 / oneOverRate < targetangle)
            targetangle = 6 * 60 / oneOverRate;
        targetangle += dir;
        var directionvec = Vector2.forward;
        directionvec.Rotate(targetangle);
        directionvec *= speed * 1.2f * 60 / oneOverRate;
        var finalpos = apos + directionvec;
        dir = (float)System.Math.Truncate(targetangle * 1000) / 1000;
        apos = Math.TruncateVector2(finalpos, 3);
        dir = (dir + 360) % 360;

        if (DistCalc(apos.ToVector3(), targetPos.ToVector3()) < 1)
        {
            ;//apos = targetPos;
        }

        return new Vector3(apos.x, apos.y, dir);
    }

    public static float DistCalc(Vector3 v1, Vector3 v2)
    {
        return Math.Sqrt(Math.Pow(v1.x - v2.x, 2) + Math.Pow(v1.y - v2.y, 2));
    }
    public static System.Collections.Generic.KeyValuePair<float, string[]> NextPath(Vector3 acePos, float t, bool reverse, int revlast, string pattern, int elapsed, int depth, string[] moves, string prev)
    {
        Vector3 targetPos = new();
        if (targetBloon == null)
            targetPos = farPoint.ToVector3();
        else
            targetPos = targetBloon.path.DistanceToPoint(targetBloon.distanceTraveled);
        var dist = (acePos.ToVector2() - targetPos.ToVector2()).Magnitude;
        if (targetBloon != null && targetBloon.bloonModel.isMoab == false)
            dist = targetBloon == null ? DistCalc(acePos, targetPos) : distance(targetBloon.path.DistanceToPoint(targetBloon.distanceTraveled - targetBloon.bloonModel.speed / 2).ToVector2(), targetBloon.path.DistanceToPoint(targetBloon.distanceTraveled + targetBloon.bloonModel.speed / 2).ToVector2(), new Vector2(acePos.x, acePos.y));
        if (targetPos.x > 90 && targetPos.y > 0)
            targetPos.x = 90;
        if (targetPos.y > 50)
            targetPos.y = 50;
        targetPos = ((targetPos.ToVector2() - startingPos.ToVector2()) * 1.2f + startingPos.ToVector2()).ToVector3();
        Vector2 posCircle = new(), posInfinite = new(), posEight = new(), posRev = new(), posRevC = new(), posRevE = new(), posRevI = new();
        Vector3 pathCircle = new(), pathEight = new(), pathInfinite = new(), pathRev = new();
        //targetPos = (targetBloon?.distanceTraveled ?? 5) / (targetBloon?.path.Length ?? 10) > 0.2 && (targetBloon?.distanceTraveled ?? 5) / (targetBloon?.path.Length ?? 10) < 0.8 ? new Vector3(targetPos.x < -90 ? -90 : targetPos.x > 90 ? 90 : targetPos.x, targetPos.y < -40 ? -40 : targetPos.x > 50 ? 50 : targetPos.y, 0) : targetPos;

        circleT?.transform.position = new UnityEngine.Vector3(targetPos.x, targetPos.y * -1.7f, 0);

        if (depth >= 1)
        {
            return new System.Collections.Generic.KeyValuePair<float, string[]>(dist, moves);
        }

        try
        {
            foreach (var path in pathMovement.pathSuppliers.ToList())
                switch (path.GetName())
                {
                    case "Circle":
                        posCircle = startingPos.ToVector2() + path.GetPathPosition(t + (reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate);
                        posRevC = startingPos.ToVector2() + path.GetPathPosition(t + (reverse ? 0.1f : -0.1f) + (!pathMovement.currPathSupplier.reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate);
                        break;
                    case "FigureEight":
                        posEight = startingPos.ToVector2() + path.GetPathPosition(t + (reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate);
                        posRevE = startingPos.ToVector2() + path.GetPathPosition(t + (reverse ? 0.1f : -0.1f) + (!pathMovement.currPathSupplier.reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate);
                        break;
                    case "FigureInfinite":
                        posInfinite = startingPos.ToVector2() + path.GetPathPosition(t + (reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate);
                        posRevI = startingPos.ToVector2() + path.GetPathPosition(t + (reverse ? 0.1f : -0.1f) + (!pathMovement.currPathSupplier.reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate);
                        break;
                }
        }
        catch (Exception e) { }

        switch (pattern)
        {
            case "Circle":
                posRev = posRevC;
                break;
            case "FigureEight":
                posRev = posRevE;
                break;
            case "FigureInfinite":
                posRev = posRevI;
                break;
            case "Reverse":
                posRev = new Vector2(1000, 1000);
                break;
        }

        pathRev = CalcPath(acePos, posRev);
        pathCircle = CalcPath(acePos, posCircle);
        pathEight = CalcPath(acePos, posEight);
        pathInfinite = CalcPath(acePos, posInfinite);

        circle8?.transform.localScale = circleI.transform.localScale = circleO.transform.localScale = new UnityEngine.Vector3(0.5f, 0.5f, 0.5f);
        circle8?.transform.position = new UnityEngine.Vector3(posEight.x, posEight.y * -1.7f + 5, 0);
        circleI?.transform.position = new UnityEngine.Vector3(posInfinite.x, posInfinite.y * -1.7f + 5, 0);
        circleO?.transform.position = new UnityEngine.Vector3(posCircle.x, posCircle.y * -1.7f + 5, 0);


        var retCircle = NextPath(pathCircle, t + (reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate, reverse, revlast, "Circle", elapsed + 1, depth + 1, moves.AddToArray("Circle"), "Circle");
        var retEight = NextPath(pathEight, t + (reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate, reverse, revlast, "FigureEight", elapsed + 1, depth + 1, moves.AddToArray("FigureEight"), "FigureEight");
        var retInfinite = NextPath(pathInfinite, t + (reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate, reverse, revlast, "FigureInfinite", elapsed + 1, depth + 1, moves.AddToArray("FigureInfinite"), "FigureInfinite");
        reverse = !reverse;
        var retRev = NextPath(pathRev, t + (reverse ? -1 : 1) * 0.1f + (reverse ? -1 : 1) * pathMovement.tpf * 60 / oneOverRate, reverse, elapsed, "Reverse", elapsed + 1, depth + 1, moves.AddToArray("Reverse"), pattern);


        if (elapsed - revlast < 300)
        {
            retRev = new System.Collections.Generic.KeyValuePair<float, string[]>(1000, []);
        }

        if (Math.Abs(Math.DeltaAngle((posRev - acePos.ToVector2()).Rotation, direction)) < 6 * 60 / oneOverRate)//((t < 0.875 && t > 0.375) != stayOnRight)
        {
            retRev = new System.Collections.Generic.KeyValuePair<float, string[]>(1000, []);
        }

        retRev = new System.Collections.Generic.KeyValuePair<float, string[]>(retRev.Key + 0.1f, retRev.Value);

        if (retCircle.Key <= retEight.Key && retCircle.Key <= retInfinite.Key && retCircle.Key <= retRev.Key)
        {
            return retCircle;
        }
        else if (retEight.Key <= retCircle.Key && retEight.Key <= retInfinite.Key && retEight.Key <= retRev.Key)
        {
            return retEight;
        }
        else if (retInfinite.Key <= retEight.Key && retInfinite.Key <= retCircle.Key && retInfinite.Key <= retRev.Key)
        {
            return retInfinite;
        }
        else
        {
            return retRev;
        }
    }

    [HarmonyPatch(typeof(AirUnit), "Initialise")]
    public class AirUnitInit
    {
        [HarmonyPostfix]
        public static void Postfix(AirUnit __instance)
        {
            if (!active)
                return;
            airUnit = __instance;
            startingPos = airUnit.Position.ToVector3();
            reverse = false;
        }
    }

    [HarmonyPatch(typeof(PathMovement), "Initialise")]
    public class PathMovementInit
    {
        [HarmonyPostfix]
        public static void Postfix(PathMovement __instance)
        {
            if (!active)
                return;
            pathMovement = __instance;
        }
    }

    [HarmonyPatch(typeof(PathManager), "Process")]
    public class PathManagerProc
    {
        [HarmonyPostfix]
        public static void Postfix(int elapsed, PathManager __instance)
        {
        }
    }

    public static void PathMovementProcessPre(int elapsed)
    {

        if (elapsed - lastElapsed < ((int)(60 / oneOverRate)))
            return;
        if (!InGame.Bridge.simulation.AreRoundsActive())
            return;

        InGame.instance.GetMap().pathManager.UpdateActivePaths();
        var path = InGame.instance.GetMap().pathManager.paths[0];
        var bloonToSimulations = InGame.Bridge.GetAllBloons().ToIl2CppList().ToList();
        Il2CppSystem.Collections.Generic.List<Bloon> bloons = new();
        foreach (var bloonsim in bloonToSimulations)
        {
            bloons.Add(bloonsim.GetBloon());
        }
        float leak = 0;
        System.Collections.Generic.List<Bloon> moabs = new(), bfbs = new(), fbfbs = new(), zomgs = new(), cerams = new(), ddts = new(), others = new();
        foreach (var bloon in bloons)
        {
            leak += bloon.GetModifiedTotalLeakDamage() - bloon.bloonModel.maxHealth + bloon.health;
            if (bloon.bloonModel.name.ToLower().StartsWith("moab"))
                moabs.Add(bloon);
            else if (bloon.bloonModel.name.ToLower().StartsWith("bfb"))
                bfbs.Add(bloon);
            else if (bloon.bloonModel.name.ToLower().StartsWith("zomg") || bloon.bloonModel.name.ToLower().StartsWith("bad"))
                zomgs.Add(bloon);
            else if (bloon.bloonModel.name.ToLower().StartsWith("ceram"))
            { cerams.Add(bloon); others.Add(bloon); }
            else if (bloon.bloonModel.name.ToLower().StartsWith("ddt"))
                ddts.Add(bloon);
            else if (bloon.bloonModel.layerNumber > 3)
                cerams.Add(bloon);
            else
                others.Add(bloon);
            if (bloon.bloonModel.name.ToLower().StartsWith("bfbfortified"))
                fbfbs.Add(bloon);
        }
        UnityEngine.GameObject.Find("UpgradeTreeButton").GetComponentInChildren<TextMeshProUGUI>().text = leak.ToString();

        var bestZomg = zomgs.Count > 0 ? zomgs.First() : null;
        var worstZomg = zomgs.Count > 0 ? zomgs.Last() : null;
        int firstind = 0;
        float dist = 0;
        Bloon? strongCeram = null;
        foreach (var ceram in cerams)
        {
            if (ceram.distanceTraveled > dist)
            {
                dist = ceram.distanceTraveled;
                firstind = cerams.IndexOf(ceram);
            }
            if (ceram.health >= 60 && ceram.distanceTraveled > (strongCeram?.distanceTraveled ?? 0))
            {
                strongCeram = ceram;
            }
        }

        var bestCeram = cerams.Count > 0 ? cerams[firstind] : null;

        dist = 0;
        firstind = -1;
        foreach (var moab in moabs)
        {
            if (moab.distanceTraveled > dist)
            {
                dist = moab.distanceTraveled;
                firstind = moabs.IndexOf(moab);
            }
        }
        var bestMoab = firstind != -1 ? moabs[firstind] : null;

        Bloon? worstBfb = null;
        dist = 0;
        foreach (var bfb in bfbs)
        {
            if (bfb.distanceTraveled > dist)
            {
                dist = bfb.distanceTraveled;
                firstind = bfbs.IndexOf(bfb);
            }
            if (bfb.distanceTraveled < ((worstZomg?.distanceTraveled ?? 0) - 75) && (worstBfb?.distanceTraveled ?? 0) < bfb.distanceTraveled)
            {
                worstBfb = bfb;
            }
        }
        var bestBfb = bfbs.Count > 0 ? bfbs[firstind] : null;

        dist = 0;
        foreach (var ddt in ddts)
        {
            if (ddt.distanceTraveled > dist)
            {
                dist = ddt.distanceTraveled;
                firstind = ddts.IndexOf(ddt);
            }
        }
        var bestDdt = ddts.Count > 0 ? ddts[firstind] : null;

        dist = 0;
        foreach (var other in others)
        {
            if (other.distanceTraveled > dist)
            {
                dist = other.distanceTraveled;
                firstind = others.IndexOf(other);
            }
        }
        var bestOther = others.Count > 0 ? others[firstind] : null;

        dist = 0;
        firstind = 0;
        bool regrow = false;
        foreach (var other in others)
        {
            if (other.distanceTraveled > dist && other.bloonModel.IsRegrowBloon())
            {
                regrow = true;
                dist = other.distanceTraveled;
                firstind = others.IndexOf(other);
            }
        }
        var bestRegrow = regrow ? others[firstind] : null;

        if (bestZomg != null && worstZomg != null)
        {
            if (worstZomg.distanceTraveled / path.Length < 0.3)
            {
                if (worstZomg.distanceTraveled / path.Length < 0.2)
                {
                    if (worstZomg.Position.Y < -20)
                    {
                        if (bestOther != null && bestOther.distanceTraveled / path.Length > 0.85f)
                        {
                            targetBloon = bestOther;
                        }
                        else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.72)
                        {
                            targetBloon = bestCeram;
                        }
                        else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.58)
                        {
                            targetBloon = bestMoab;
                        }/*
                        else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.58)
                        {
                            targetBloon = bestCeram;
                        }*/
                        else if (moabs.Count >= 10)
                        {
                            targetBloon = bestMoab;
                        }
                        else if (bestMoab != null && zomgs.Count == 0 && bfbs.Count == 0)
                        {
                            targetBloon = bestMoab;
                        }
                        else if (bestMoab != null && (bestMoab.Position - bestZomg.Position).ToVector2().Magnitude > 40 && bestMoab.distanceTraveled / path.Length > 0.35f)
                        {
                            targetBloon = bestMoab;
                        }
                        else if (moabs.Count < 5 && bestBfb != null && bestBfb.distanceTraveled - (bestZomg?.distanceTraveled ?? 0) > 15)
                        {
                            targetBloon = bestBfb;
                        }
                        else if (bestMoab != null)
                        {
                            targetBloon = bestMoab;
                        }
                        else if (bestBfb != null)
                        {
                            targetBloon = bestBfb;
                        }
                        else if (bestCeram != null)
                        {
                            targetBloon = bestCeram;
                        }
                    }
                    else
                    {
                        if (bestOther != null && bestOther.distanceTraveled / path.Length > 0.85f)
                        {
                            targetBloon = bestOther;
                        }
                        else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.76)
                        {
                            targetBloon = bestCeram;
                        }
                        else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.5)
                        {
                            targetBloon = bestMoab;
                        }
                        else if (worstBfb != null && fbfbs.Count > 20 && (float)worstBfb.health / worstBfb.bloonModel.maxHealth > 0.4)
                        {
                            targetBloon = worstBfb;
                        }
                        else if (bestBfb != null && bestBfb.distanceTraveled > bestZomg.distanceTraveled + 20 && moabs.Count < 13)
                        {
                            targetBloon = bestBfb;
                        }
                        else if (bestMoab != null && bestMoab.distanceTraveled > (bestBfb?.distanceTraveled ?? 0))
                        {
                            targetBloon = bestMoab;
                        }
                        else if (bestCeram != null)
                        {
                            targetBloon = bestCeram;
                        }
                        else
                        {
                            targetBloon = null;
                            farPoint = new Vector2(80, 0);
                        }
                    }
                }
                else
                {
                    if (bestOther != null && bestOther.distanceTraveled / path.Length > 0.85f)
                    {
                        targetBloon = bestOther;
                    }
                    else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.72)
                    {
                        targetBloon = bestCeram;
                    }/*
                    else if (strongCeram != null && strongCeram.distanceTraveled / path.Length > 0.6)
                    {
                        targetBloon = strongCeram;
                    }*/
                    else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.5)
                    {
                        targetBloon = bestMoab;
                    }
                    else if (bestMoab != null && bestMoab.distanceTraveled > (bestBfb?.distanceTraveled ?? 0))
                    {
                        targetBloon = bestMoab;
                        //farPoint = new Vector2(50, bestMoab.Position.Y < 50 ? bestMoab.Position.Y : 50);
                    }
                    else if (bestCeram != null)
                    {
                        targetBloon = bestCeram;
                    }
                    else
                    {
                        targetBloon = null;
                        farPoint = new Vector2(80, 0);
                    }
                }
            }
            else
            {
                if (worstZomg.distanceTraveled / path.Length < 0.325)
                {
                    if (bestOther != null && bestOther.distanceTraveled / path.Length > 0.835f)
                    {
                        targetBloon = bestOther;
                    }
                    else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.7)
                    {
                        targetBloon = bestCeram;
                    }
                    else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.6)
                    {
                        targetBloon = bestMoab;
                    }
                    else if (strongCeram != null && strongCeram.distanceTraveled / path.Length > 0.6)
                    {
                        targetBloon = strongCeram;
                    }
                    else if (bestMoab != null && bestMoab.distanceTraveled > (bestBfb?.distanceTraveled ?? 0))
                    {
                        targetBloon = bestMoab;
                    }
                    else if (bestCeram != null)
                    {
                        targetBloon = bestCeram;
                    }
                    else
                    {
                        targetBloon = null;
                        farPoint = new Vector2(-50, 30);
                    }
                }
                else if (bestZomg.distanceTraveled / path.Length < 0.35)
                {
                    if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.835)
                    {
                        targetBloon = bestCeram;
                    }
                    else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.73)
                    {
                        if (strongCeram != null && strongCeram.distanceTraveled / path.Length > 0.6 && strongCeram.distanceTraveled / path.Length < 0.7)
                        {
                            targetBloon = strongCeram;
                        }
                        else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.6 && bestMoab.distanceTraveled / path.Length < 0.7)
                        {
                            targetBloon = bestMoab;
                        }
                        else
                        {
                            targetBloon = null;
                            farPoint = new Vector2(-50, 30);
                        }
                    }
                    else if (strongCeram != null && strongCeram.distanceTraveled / path.Length > 0.6)
                    {
                        targetBloon = strongCeram;
                    }
                    else if (bestMoab != null && bestMoab.distanceTraveled > (bestBfb?.distanceTraveled ?? 0))
                    {
                        targetBloon = bestMoab;
                    }
                    else if (bestCeram != null)
                    {
                        targetBloon = bestCeram;
                    }
                    else
                    {
                        targetBloon = null;
                        farPoint = new Vector2(-50, 30);
                    }
                }
                else
                {
                    if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.835)
                    {
                        targetBloon = bestCeram;
                    }
                    else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.7)
                    {
                        if (strongCeram != null && strongCeram.distanceTraveled / path.Length > 0.6 && strongCeram.distanceTraveled / path.Length < 0.7)
                        {
                            targetBloon = strongCeram;
                        }
                        else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.6 && bestMoab.distanceTraveled / path.Length < 0.7)
                        {
                            targetBloon = bestMoab;
                        }
                        else
                        {
                            targetBloon = null;
                            farPoint = new Vector2(-50, 30);
                        }
                    }
                    else if (strongCeram != null && strongCeram.distanceTraveled / path.Length > 0.6)
                    {
                        targetBloon = strongCeram;
                    }
                    else if (bestMoab != null && bestMoab.distanceTraveled > (bestBfb?.distanceTraveled ?? 0))
                    {
                        targetBloon = bestMoab;
                    }
                    else if (bestCeram != null)
                    {
                        targetBloon = bestCeram;
                    }
                    else
                    {
                        targetBloon = null;
                        farPoint = new Vector2(-50, 30);
                    }
                }
            }
        }
        else
        {

            if (bestOther != null && bestOther.distanceTraveled / path.Length > 0.85f)
            {
                targetBloon = bestOther;
            }
            else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.835)
            {
                targetBloon = bestCeram;
            }
            else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.7)
            {
                if (strongCeram != null && strongCeram.distanceTraveled / path.Length > 0.6 && strongCeram.distanceTraveled / path.Length < 0.7)
                {
                    targetBloon = strongCeram;
                }
                else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.6 && bestMoab.distanceTraveled / path.Length < 0.7)
                {
                    targetBloon = bestMoab;
                }
                else
                {
                    targetBloon = null;
                    farPoint = new Vector2(-50, 30);
                }
            }
            else if (bestCeram != null && bestCeram.distanceTraveled / path.Length > 0.58)
            {
                targetBloon = bestCeram;
            }
            else if (bestMoab != null && bestMoab.distanceTraveled / path.Length > 0.58)
            {
                targetBloon = bestMoab;
            }
            else if (ddts.Count > 0)
            {
                targetBloon = bestDdt;
            }
            else if (regrow)
            {
                targetBloon = bestRegrow;
            }
            else if (moabs.Count >= 10)
            {
                targetBloon = bestMoab;
            }
            else if (bestMoab != null && bestZomg == null && bestBfb == null)
            {
                targetBloon = bestMoab;
            }
            else if (bestMoab != null && bestZomg == null && bestMoab.distanceTraveled > bestBfb.distanceTraveled)
            {
                targetBloon = bestMoab;
            }
            else if (bestMoab != null && (bestMoab.Position - bestZomg.Position).ToVector2().Magnitude > 40 && bestMoab.distanceTraveled / path.Length > 0.35f)
            {
                targetBloon = bestMoab;
            }
            else if (moabs.Count < 5 && bestBfb != null && bestBfb.distanceTraveled - (bestZomg?.distanceTraveled ?? 0) > 40)
            {
                targetBloon = bestBfb;
            }
            else if (bestMoab != null)
            {
                targetBloon = bestMoab;
            }
            else if (bestBfb != null)
            {
                targetBloon = bestBfb;
            }
            else if (bestCeram != null)
            {
                targetBloon = bestCeram;
            }
            else if (bestZomg != null)
            {
                targetBloon = bestZomg;
            }
            else
            {
                targetBloon = bestOther;
            }
        }
        lastElapsed = elapsed;
        foreach (var ace in InGame.instance.GetTowers().ToList())
        {
            if (!ace.namedMonkeyKey.StartsWith("MonkeyAce"))
                continue;

            string move = NextPath(new Vector3(airUnit.Position.X, airUnit.Position.Y, airUnit.entity.transformBehaviorCache.rotation.value), pathMovement.t + (pathMovement.currPathSupplier.reverse ? -1 : 1) * pathMovement.tpf, pathMovement.currPathSupplier.reverse, lastrev, ace.TargetType.id, elapsed, 0, [], ace.TargetType.id).Value[0];
            //Console.WriteLine(move);
            if (move != ace.TargetType.id && InGame.Bridge.AreRoundsActive())
            {
                switch (move)
                {
                    case "Circle":
                        ace.SetNextTargetType(ace.TargetType.id == "FigureInfinite");
                        break;
                    case "FigureEight":
                        ace.SetNextTargetType(ace.TargetType.id == "Circle");
                        break;
                    case "FigureInfinite":
                        ace.SetNextTargetType(ace.TargetType.id == "FigureEight");
                        break;
                    case "Reverse":
                        Rev(elapsed);
                        break;

                }
            }
        }
    }

    public static Vector2 targetPos = new();
    public static bool onpath = true;

    [HarmonyPatch(typeof(PathMovement), "Process")]
    public class PathMovementProcess
    {
        public static float oldt, oldd, llb;
        [HarmonyPrefix]
        public static void Prefix(int elapsed, PathMovement __instance)
        {
            if (!active)
                return;
            if (pathMovement.takingOff)
                return;
            if (InGame.Bridge.simulation.roundTime.elapsed % (60 / oneOverRate) == 0)
                PathMovementProcessPre(elapsed);
            foreach (var ace in InGame.instance.GetTowers().ToList())
            {
                if (!ace.namedMonkeyKey.StartsWith("MonkeyAce"))
                    return;

                var a = ace.TargetType.id; // Circle, FigureInfinite, FigureEight
                Vector2 posCircle = new();// = new Vector2(startingPos.x + 80 * Math.Cos(deg * Math.Deg2Rad), startingPos.y + 80 * Math.Sin(deg * Math.Deg2Rad));
                Vector2 posInfinite = new();// = new Vector2(startingPos.x + (deg < 180 ? 1 : -1) * (40 - 40 * Math.Cos(deg * 2 * Math.Deg2Rad)), startingPos.y - 40 * Math.Sin(deg * 2 * Math.Deg2Rad));
                Vector2 posEight = new();// = new Vector2(startingPos.x - 40 * Math.Sin(deg * 2 * Math.Deg2Rad), startingPos.y - (deg < 180 ? 1 : -1) * (40 - 40 * Math.Cos(deg * 2 * Math.Deg2Rad)));

                try
                {
                    foreach (var path in pathMovement.pathSuppliers.ToList())
                        switch (path.GetName())
                        {
                            case "Circle":
                                posCircle = startingPos.ToVector2() + path.GetPathPosition(pathMovement.t + (pathMovement.currPathSupplier.reverse ? -1f : 1f) * pathMovement.tpf);
                                break;
                            case "FigureEight":
                                posEight = startingPos.ToVector2() + path.GetPathPosition(pathMovement.t + (pathMovement.currPathSupplier.reverse ? -1f : 1f) * pathMovement.tpf);
                                break;
                            case "FigureInfinite":
                                posInfinite = startingPos.ToVector2() + path.GetPathPosition(pathMovement.t + (pathMovement.currPathSupplier.reverse ? -1f : 1f) * pathMovement.tpf);
                                break;
                        }
                }
                catch (Exception e) { }

                targetPos = a == "Circle" ? posCircle : a == "FigureEight" ? posEight : posInfinite;
                float speed = pathMovement.pathMovementModel.speed / 60f;// acetype?.GetValue().ToString()?.ToLower() == "bace" ? 1 : (acetype?.GetValue().ToString()?.ToLower() == "shredder" ? (float)1.25 : (float)2 / (float)3);

                if (pathMovement.onPath)
                {
                    acePos = airUnit.Position.ToVector2();
                    direction = airUnit.entity.transformBehaviorCache.rotation.value;
                    onpath = true;
                }
                else
                {
                    if (onpath)
                    {
                        acePos = airUnit.Position.ToVector2();
                        direction = airUnit.entity.transformBehaviorCache.rotation.value;
                        onpath = false;
                    }
                    var targetvec = targetPos - acePos;
                    var targetrot = targetvec.ToVector3().Rotation;
                    var targetangle = UnityEngine.Mathf.DeltaAngle(direction, targetrot);
                    if (targetangle < -6)
                        targetangle = -6;
                    if (6 < targetangle)
                        targetangle = 6;
                    targetangle += direction;
                    var directionvec = Vector2.forward;
                    directionvec.Rotate(targetangle);
                    directionvec *= speed * 1.2f;
                    var finalpos = acePos + directionvec;
                    direction = (float)System.Math.Truncate(targetangle * 1000) / 1000;
                    acePos = Math.TruncateVector2(finalpos, 3);
                    direction %= 360;
                    /*
                    var rdir=(-airUnit.Rotation.value % 360 + 450) % 360;
                    var calcdir=(direction - Math.Atan2(targetPos.y - acePos.y, targetPos.x - acePos.x) * Math.Rad2Deg + 360)%360;
                    var realdir=( rdir - Math.Atan2(targetPos.y - airUnit.Position.Y, targetPos.x - airUnit.Position.X) * Math.Rad2Deg+360)%360;
                    if ((calcdir<180)!=(realdir<180))
                        Console.WriteLine("fuck");//fuck*/
                    //direction = (-airUnit.Rotation.value % 360 + 450) % 360;

                }
                if (DistCalc(acePos.ToVector3(), targetPos.ToVector3()) < 1)
                {
                    ;//acePos=targetPos;
                }

            }
            AceMacroV2.elapsed = elapsed;
        }
        [HarmonyPostfix]
        public static void Postfix(int elapsed)
        {
        }
    }
    public override void OnRoundStart()
    {
        base.OnRoundStart();
        acePos = airUnit.Position.ToVector2();
        direction = (-airUnit.Rotation.value % 360 + 450) % 360;
    }

    public static UnityEngine.GameObject circleT, circle8, circleO, circleI, tButton, rtButton, reverseButton;
    public override void OnGUI()
    {
        base.OnGUI();
        if (!active)
            return;
        if (!InGame.instance.Exists())
            return;
        try
        {
            if (InGame.Bridge.simulation.AreRoundsActive())
            {
                if (circleT == null)
                {
                    var gos = UnityEngine.Object.FindObjectsOfType<UnityEngine.GameObject>(true);
                    foreach (var go in gos)
                    {
                        if (go.name.StartsWith("MortarTarget") && circleT == null)
                        {
                            circleT = UnityEngine.GameObject.Instantiate(go);/*
                            circle8 = UnityEngine.GameObject.Instantiate(go);
                            circleO = UnityEngine.GameObject.Instantiate(go);
                            circleI = UnityEngine.GameObject.Instantiate(go);*/
                        }
                    }
                }
            }
        }
        catch (NullReferenceException e) { }
    }
    public static string leak = "1000000";
    public override void OnDefeat()
    {
        base.OnDefeat();
        if (!active)
            return;
        InGame.Bridge.RetryLastRound(0, false);
        lastElapsed = InGame.Bridge.simulation.time.elapsed;
        elapsed = 0;
        lastrev = 0;
        if (InGame.Bridge.simulation.GetCurrentRound() == 97 && Int32.Parse(leak) > Int32.Parse(UnityEngine.GameObject.Find("UpgradeTreeButton").GetComponentInChildren<TextMeshProUGUI>().text))
        {
            leak = UnityEngine.GameObject.Find("UpgradeTreeButton").GetComponentInChildren<TextMeshProUGUI>().text;
            Console.WriteLine(leak + "-" + rndm);
            Console.WriteLine(System.DateTime.Now);
        }
        rndm = startDelay == -1 ? new System.Random().Next(1000) : startDelay;

    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (!active)
            return;
        rate = 1f / (float)oneOverRate;
        if (InGame.instance.Exists())
        {
            try
            {
                if (!InGame.Bridge.AreRoundsActive())
                    foreach (var ace in InGame.instance.GetTowers().ToList())
                    {
                        if (!ace.namedMonkeyKey.StartsWith("MonkeyAce"))
                            continue;
                        var a = ace.TargetType.id;
                        string[] moves = ["Circle", "FigureEight", "FigureInfinite"];
                        var move = moves[rndm % 3];
                        if (move != a)
                        {
                            switch (move)
                            {

                                case "Circle":
                                    ace.SetNextTargetType(a == "FigureInfinite");
                                    break;
                                case "FigureEight":
                                    ace.SetNextTargetType(a == "Circle");
                                    break;
                                case "FigureInfinite":
                                    ace.SetNextTargetType(a == "FigureEight");
                                    break;
                            }
                            if (rndm % 6 > 2)
                            {
                                Rev(lastElapsed);
                            }
                        }
                    }
                if (InGame.instance.GetTowers().Count == 0)
                {
                    TowerModel ace = null;
                    foreach (var tow in InGame.instance.GetGameModel().towers)
                    {
                        if (tow.name == "MonkeyAce")
                        {
                            ace = tow;
                            break;
                        }
                    }
                    InGame.instance.GetTowerManager().CreateTower(ace, new Vector3(-3.1696632f, 34.068512f), InGame.Bridge.GetInputId(), InGame.instance.GetMap().GetAreaAtPoint(new Vector2(-3.1696632f, 34.068512f)).GetAreaID(), ObjectId.Create(0));

                }
                if (Math.Abs((int)(pathMovement.t * 1000) - rndm) >= 3)
                    return;
                if (!InGame.Bridge.AreRoundsActive() && InGame.instance.GetTowers().Count > 0 && pathMovement.onPath && !pathMovement.takingOff)
                {
                    onpath = true;
                    InGame.Bridge.StartRound();
                    InGame.Bridge.SetFastForward(true);
                }

            }
            catch (NullReferenceException e) { }
        }
    }

}