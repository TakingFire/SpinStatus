using GameSystems.LocalMultiplayer;
using HarmonyLib;
using SpinStatus.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpinStatus.Patches
{
    [HarmonyPatch]
    internal static class NoteEventHandler
    {
        private struct TempNoteState
        {
            public bool isDoneWith { get; set; }
            public bool isSustained { get; set; }
        }

        private static float previousScoreEventTime = 0f;
        private const float scoreEventInterval = 0.125f;

        private enum NoteEndType
        {
            DrumEnd = 2,
            SpinRightEnd = 4,
            SpinLeftEnd = 8,
            HoldEnd = 16,
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState))]
        private static void ScoreEventPre(this PlayState playState, int noteIndex, ref TempNoteState __state)
        {
            ScoreState scoreState = playState.scoreState;
            ref NoteState noteState = ref scoreState.GetNoteState(noteIndex);
            __state = new TempNoteState
            {
                isDoneWith = noteState.IsDoneWith,
                isSustained = scoreState.GetSustainState(noteIndex).isSustained
            };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState))]
        private static void ScoreEventPost(this PlayState playState, int noteIndex, ref TempNoteState __state)
        {
            if (!Config.EventNote.Value && !Config.EventScore.Value) { return; }

            ScoreState scoreState = playState.scoreState;
            ref NoteState noteState = ref scoreState.GetNoteState(noteIndex);

            if (playState.playStateStatus <= PlayStateStatus.Preparing) { return; }

            PlayableNoteData nodeData = playState.noteData;
            Note note = nodeData.GetNote(noteIndex);

            bool isSustained = scoreState.GetSustainState(noteIndex).isSustained;
            bool isDoneWith = noteState.IsDoneWith;
            bool becameSustained = !__state.isSustained && isSustained;
            bool becameDoneWith = !__state.isDoneWith && isDoneWith;

            float currentTime = playState.currentTrackTime;

            if (Config.EventScore.Value && (becameDoneWith || isSustained))
            {
                if (currentTime < previousScoreEventTime ||
                    currentTime - previousScoreEventTime > scoreEventInterval)
                {
                    previousScoreEventTime = currentTime;

                    Model.Event scoreEvent = new()
                    {
                        Type = Model.EventType.ScoreEvent,
                        Player = playState.playerIndex,
                        Status = new ScoreStatus
                        {
                            Score = scoreState.TotalScore,
                            Combo = scoreState.combo,
                            MaxCombo = scoreState.maxCombo,
                            FullCombo = scoreState.fullComboState.ToString(),
                            Health = playState.health,
                            MaxHealth = playState.MaxHealth,
                            Multiplier = playState.multiplier,
                            BaseScore = scoreState.CurrentTotals.baseScore,
                            BaseScoreLost = scoreState.CurrentTotals.baseScoreLost
                        }
                    };

                    Server.SendMessage(scoreEvent);
                }
            }

            if (!Config.EventNote.Value) { return; }
            if (!becameDoneWith && !becameSustained) { return; }

            if (note.NoteType == NoteType.SectionContinuationOrEnd ||
                note.NoteType == NoteType.Checkpoint ||
                note.NoteType == NoteType.TutorialStart) { return; }

            string noteType;

            if (becameDoneWith && isSustained) noteType = ((NoteEndType)note.NoteType).ToString();
            else if (!isSustained && note.NoteType == NoteType.DrumStart) noteType = "Drum";
            else noteType = note.NoteType.ToString();

            Model.Event noteEvent = new()
            {
                Type = Model.EventType.NoteEvent,
                Player = playState.playerIndex,
                Status = new NoteStatus
                {
                    Index = noteIndex,
                    Accuracy = noteState.timingAccuracy.ToString(),
                    Timing = noteState.timingOffset ?? 0f,
                    Type = noteType,
                    Color = note.colorIndex,
                    Lane = note.column
                }
            };

            Server.SendMessage(noteEvent);
        }
    }

    [HarmonyPatch]
    internal static class SceneEventHandler
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
        private static void TrackStart()
        {
            if (!Config.EventTrackStart.Value) { return; }

            TrackDataSegment trackDataSegment = Track.PlayStates[0].trackData.GetFirstSegment();
            TrackInfoMetadata trackMeta = trackDataSegment.GetTrackInfoMetadata();
            TrackInfoAssetReference assetInfo = trackDataSegment.metadata.TrackInfoRef;

            Model.Event trackEvent = new()
            {
                Type = Model.EventType.TrackStart,
                Status = new TrackStatus
                {
                    Title = trackMeta.title,
                    SubTitle = trackMeta.subtitle,
                    Artist = trackMeta.artistName,
                    Feat = trackMeta.featArtists,
                    Charter = trackMeta.charter,
                    AlbumArt = string.Empty,

                    StartTime = Track.PlayStates[0].startTrackTime,
                    EndTime = Track.PlayStates[0].trackData.GameplayEndTick.ToSecondsFloat(),

                    IsCustom = trackMeta.isCustom,
                    Filename = trackMeta.isCustom ? assetInfo.customFile.FileNameNoExtension : "",

                    Players = [],
                }
            };

            TrackStatus trackStatus = (TrackStatus)trackEvent.Status;

            List<ActivePlayer> players = GameSystemSingletonNoSetting<LocalMultiplayerSystem>.Instance.ActivePlayers;

            for (int i = 0; i < players.Count; i++)
            {
                PlayState playState = Track.PlayStates[i];
                PlayableTrackData trackData = playState.Handle.GetDataForPlayerIndex(i);
                TrackDataMetadata mapData = trackData.GetFirstSegment().metadata?.TrackDataMetadata?.GetMetadataForDifficulty(playState.CurrentDifficulty);

                PlayerStatus playerStatus = new()
                {
                    TotalWins = players[i].totalWins,
                    DisplayName = players[i].displayName,
                    Palette = [],

                    Difficulty = playState.CurrentDifficulty.ToString(),
                    Rating = mapData.DifficultyRating,
                    MaxScore = mapData.MaxScore,
                };

                ColorSystem colorSystem = GameSystemSingleton<ColorSystem, ColorSystemSettings>.Instance;
                ColorSystem.NoteColorProfile colorProfile = colorSystem.GetNoteColorProfile(playState.GetNoteColorProfile());

                for (int j = 1; j < 8; j++)
                {
                    Color color = colorProfile.GetBlender((NoteColorType)j).colorBlend.baseColor;
                    playerStatus.Palette.Add((NoteColorType)j, "#" + ColorUtility.ToHtmlStringRGB(color));
                }

                trackStatus.Players.Add(playerStatus);
            }

            if (!Config.SendImageData.Value)
            {
                Server.SendMessage(trackEvent);
                return;
            }

            AssetLoadingSystem instance = GameSystemSingletonNoSetting<AssetLoadingSystem>.Instance;
            Texture2DAssetReference assetReference = trackDataSegment.metadata.AlbumArtReferenceCopy();

            Texture2D asset = null;

            if (!string.IsNullOrEmpty(assetReference.Bundle))
            {
                AssetBundle[] assetBundles = AssetBundle.GetAllLoadedAssetBundles().ToArray();
                AssetBundle assetBundle = assetBundles.FirstOrDefault(b => b.name == assetReference?.Bundle);
                asset = assetBundle?.LoadAsset<Texture2D>(assetReference?.Guid);
            }
            else
            {
                string customArtDir = Path.Combine(Application.persistentDataPath, "Custom", "AlbumArt");
                string customArtPath = Path.Combine(customArtDir, assetReference?.AssetName + ".png");
                if (File.Exists(customArtPath))
                {
                    asset = new Texture2D(2, 2);
                    asset.LoadImage(File.ReadAllBytes(customArtPath));
                }
            }

            if (asset)
            {
                RenderTexture tmp = RenderTexture.GetTemporary(
                    Math.Min(asset.width, 256),
                    Math.Min(asset.height, 256),
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB
                );
                Graphics.Blit(asset, tmp);

                Texture2D image = new(tmp.width, tmp.height);
                image.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                image.Apply();
                RenderTexture.ReleaseTemporary(tmp);

                trackStatus.AlbumArt = Convert.ToBase64String(image.EncodeToPNG());
                trackEvent.Status = trackStatus;
            }

            Server.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.StopTrack))]
        private static void TrackEnd()
        {
            if (!Config.EventTrackEnd.Value) { return; }

            Model.Event trackEvent = new() { Type = Model.EventType.TrackEnd };
            Server.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.HandlePauseGame))]
        private static void TrackPause()
        {
            if (!Config.EventTrackPause.Value) { return; }

            Model.Event trackEvent = new() { Type = Model.EventType.TrackPause };
            Server.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.HandleUnpauseGame))]
        private static void TrackResume()
        {
            if (!Config.EventTrackResume.Value) { return; }

            Model.Event trackEvent = new() { Type = Model.EventType.TrackResume };
            Server.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong))]
        private static void TrackComplete()
        {
            if (!Config.EventTrackComplete.Value) { return; }

            Model.Event trackEvent = new() { Type = Model.EventType.TrackComplete };
            Server.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.FailSong))]
        private static void TrackFail()
        {
            if (!Config.EventTrackFail.Value) { return; }

            Model.Event trackEvent = new() { Type = Model.EventType.TrackFail };
            Server.SendMessage(trackEvent);
        }
    }
}
