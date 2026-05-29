using HarmonyLib;
using SpinStatus.Model;
using System;
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

            if (becameDoneWith || isSustained)
            {
                if (currentTime < previousScoreEventTime ||
                    currentTime - previousScoreEventTime > scoreEventInterval)
                {
                    previousScoreEventTime = currentTime;

                    var scoreEvent = new Model.Event
                    {
                        Type = Model.EventType.ScoreEvent,
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

                    Server.ServerBehavior.SendMessage(scoreEvent);
                }
            }

            if (!becameDoneWith && !becameSustained) { return; }

            if (note.NoteType == NoteType.SectionContinuationOrEnd ||
                note.NoteType == NoteType.Checkpoint ||
                note.NoteType == NoteType.TutorialStart) { return; }

            string noteType;

            if (becameDoneWith && isSustained) noteType = ((NoteEndType)note.NoteType).ToString();
            else if (!isSustained && note.NoteType == NoteType.DrumStart) noteType = "Drum";
            else noteType = note.NoteType.ToString();

            var noteEvent = new Model.Event
            {
                Type = Model.EventType.NoteEvent,
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

            Server.ServerBehavior.SendMessage(noteEvent);
        }
    }

    [HarmonyPatch]
    internal static class SceneEventHandler
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
        private static void TrackStart()
        {
            PlayState playState = Track.PlayStates[0];
            PlayableTrackData trackData = playState.trackData;

            TrackDataSegment trackDataSegment = trackData.GetFirstSegment();
            TrackInfoMetadata trackMeta = trackDataSegment.GetTrackInfoMetadata();
            TrackInfoAssetReference assetInfo = trackDataSegment.metadata.TrackInfoRef;
            TrackDataMetadata mapData = trackDataSegment.GetTrackDataMetadata();

            playState.trackData.GetFirstSegment();

            var trackEvent = new Model.Event
            {
                Type = Model.EventType.TrackStart,
                Status = new TrackStatus
                {
                    Title = trackMeta.title,
                    SubTitle = trackMeta.subtitle,
                    Artist = trackMeta.artistName,
                    Feat = trackMeta.featArtists,
                    Charter = trackMeta.charter,
                    Difficulty = playState.CurrentDifficulty.ToString(),
                    Rating = mapData.DifficultyRating,
                    IsCustom = trackMeta.isCustom,
                    StartTime = playState.startTrackTime,
                    EndTime = trackData.GameplayEndTick.ToSecondsFloat(),
                    Filename = trackMeta.isCustom ? assetInfo.customFile.FileNameNoExtension : "",
                    MaxScore = mapData.MaxScore,

                    Palette = []
                }
            };

            var trackStatus = (TrackStatus)trackEvent.Status;

            ColorSystem colorSystem = GameSystemSingleton<ColorSystem, ColorSystemSettings>.Instance;
            ColorSystem.NoteColorProfile colorProfile = colorSystem.GetNoteColorProfile(playState.GetNoteColorProfile());

            for (var i = 1; i < 8; i++)
            {
                Color color = colorProfile.GetBlender((NoteColorType)i).colorBlend.baseColor;
                trackStatus.Palette.Add((NoteColorType)i, "#" + ColorUtility.ToHtmlStringRGB(color));
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

            Server.ServerBehavior.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.StopTrack))]
        private static void TrackEnd()
        {
            var trackEvent = new Model.Event { Type = Model.EventType.TrackEnd };
            Server.ServerBehavior.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.HandlePauseGame))]
        private static void TrackPause()
        {
            var trackEvent = new Model.Event { Type = Model.EventType.TrackPause };
            Server.ServerBehavior.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.HandleUnpauseGame))]
        private static void TrackResume()
        {
            var trackEvent = new Model.Event { Type = Model.EventType.TrackResume };
            Server.ServerBehavior.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong))]
        private static void TrackComplete()
        {
            var trackEvent = new Model.Event { Type = Model.EventType.TrackComplete };
            Server.ServerBehavior.SendMessage(trackEvent);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.FailSong))]
        private static void TrackFail()
        {
            var trackEvent = new Model.Event { Type = Model.EventType.TrackFail };
            Server.ServerBehavior.SendMessage(trackEvent);
        }
    }
}
