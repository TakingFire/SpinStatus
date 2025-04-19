using HarmonyLib;
using SimpleJSON;
using UnityEngine;
using System.Linq;
using System;

namespace SpinStatus.Patches
{
    [HarmonyPatch]
    internal static class NoteEventHandler
    {
        private struct TempNoteState {
            public bool isDoneWith { get; set; }
            public bool isSustained { get; set; }
        }

        private static float previousScoreEventTime = 0;
        private const float scoreEventInterval = 0.125F;

        private enum NoteEndType {
            DrumEnd = 2,
            SpinRightEnd = 4,
            SpinLeftEnd = 8,
            HoldEnd = 16,
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState))]
        private static void Prefix(this PlayState playState, int noteIndex, ref TempNoteState __state)
        {
            ScoreState scoreState = playState.scoreState;
            ref NoteState noteState = ref scoreState.GetNoteState(noteIndex);
            __state = new TempNoteState();
            __state.isDoneWith = noteState.IsDoneWith;
            __state.isSustained = scoreState.GetSustainState(noteIndex).isSustained;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState))]
        private static void Postfix(this PlayState playState, int noteIndex, ref TempNoteState __state)
        {
            ScoreState scoreState = playState.scoreState;
            ref NoteState noteState = ref scoreState.GetNoteState(noteIndex);

            if (playState.playStateStatus <= PlayStateStatus.Preparing) { return; }

            PlayableNoteData nodeData = playState.noteData;
            Note note = nodeData.GetNote(noteIndex);

            bool isSustained = scoreState.GetSustainState(noteIndex).isSustained;
            bool isDoneWith = noteState.IsDoneWith;
            bool becameSustained = (!__state.isSustained && isSustained);
            bool becameDoneWith = (!__state.isDoneWith && isDoneWith);

            float currentTime = playState.currentTrackTime;

            if (becameDoneWith || isSustained) {
                if (currentTime < previousScoreEventTime ||
                    currentTime - previousScoreEventTime > scoreEventInterval)
                {
                    previousScoreEventTime = currentTime;

                    var scoreEventJSON = new JSONObject();

                    scoreEventJSON["type"] = "scoreEvent";
                    var scoreJSON = scoreEventJSON["status"].AsObject;
                    scoreJSON["score"] = scoreState.TotalScore;
                    scoreJSON["combo"] = scoreState.combo;
                    scoreJSON["maxCombo"] = scoreState.maxCombo;
                    scoreJSON["fullCombo"] = scoreState.fullComboState.ToString();
                    scoreJSON["health"] = playState.health;
                    scoreJSON["maxHealth"] = playState.MaxHealth;
                    scoreJSON["multiplier"] = playState.multiplier;

                    Server.ServerBehavior.SendMessage(scoreEventJSON);
                }
            }

            if (!becameDoneWith && !becameSustained) { return; }

            if (note.NoteType == NoteType.SectionContinuationOrEnd ||
                note.NoteType == NoteType.Checkpoint ||
                note.NoteType == NoteType.TutorialStart) { return; }

            string noteType;

            if (becameDoneWith && isSustained) noteType = ((NoteEndType)note.NoteType).ToString();
            else if ((!isSustained && note.NoteType == NoteType.DrumStart)) noteType = "Drum";
            else noteType = note.NoteType.ToString();

            var noteEventJSON = new JSONObject();

            noteEventJSON["type"] = "noteEvent";
            var noteJSON = noteEventJSON["status"].AsObject;
            noteJSON["index"] = noteIndex;
            noteJSON["accuracy"] = noteState.timingAccuracy.ToString();
            noteJSON["timing"] = noteState.timingOffset ?? 0;
            noteJSON["type"] = noteType;
            noteJSON["color"] = (int)note.colorIndex;
            noteJSON["lane"] = (int)note.column;

            Server.ServerBehavior.SendMessage(noteEventJSON);
        }
    }

    [HarmonyPatch]
    internal static class SceneEventHandler
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
        private static void StartTrack()
        {
            PlayState playState = Track.PlayStates[0];
            PlayableTrackData trackData = playState.trackData;

            TrackDataSegment trackDataSegment = trackData.GetFirstSegment();
            TrackInfoMetadata trackMeta = trackDataSegment.GetTrackInfoMetadata();

            playState.trackData.GetFirstSegment();

            var eventJSON = new JSONObject();
            eventJSON["type"] = "trackStart";
            var trackJSON = eventJSON["status"].AsObject;
            trackJSON["title"] = trackMeta.title;
            trackJSON["subTitle"] = trackMeta.subtitle;
            trackJSON["artist"] = trackMeta.artistName;
            trackJSON["feat"] = trackMeta.featArtists;
            trackJSON["charter"] = trackMeta.charter;
            trackJSON["difficulty"] = playState.CurrentDifficulty.ToString();
            trackJSON["isCustom"] = trackMeta.isCustom;
            trackJSON["startTime"] = playState.startTrackTime;
            trackJSON["endTime"] = trackData.GameplayEndTick.ToSecondsFloat();

            var colorJSON = trackJSON["palette"].AsObject;

            ColorSystem colorSystem = GameSystemSingleton<ColorSystem, ColorSystemSettings>.Instance;
            ColorSystem.NoteColorProfile colorProfile = colorSystem.GetNoteColorProfile(playState.GetNoteColorProfile());

            for (var i = 1; i < 8; i++) {
                Color color = colorProfile.GetBlender((NoteColorType)i).colorBlend.baseColor;
                colorJSON[((NoteColorType)i).ToString()] = "#" + ColorUtility.ToHtmlStringRGB(color);
            }

            AssetLoadingSystem instance = GameSystemSingletonNoSetting<AssetLoadingSystem>.Instance;
            Texture2DAssetReference assetReference = trackDataSegment.metadata.AlbumArtReferenceCopy();

            AssetBundle[] assetBundles = AssetBundle.GetAllLoadedAssetBundles().ToArray();
            AssetBundle assetBundle = assetBundles.FirstOrDefault(b => b.name == assetReference?.Bundle);
            Texture2D asset = assetBundle?.LoadAsset<Texture2D>(assetReference?.Guid);

            if (asset) {
                RenderTexture tmp = RenderTexture.GetTemporary(
                    Math.Min(asset.width, 256),
                    Math.Min(asset.height, 256),
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB
                );
                Graphics.Blit(asset, tmp);
                Texture2D image = new Texture2D(tmp.width, tmp.height);
                image.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                image.Apply();
                RenderTexture.ReleaseTemporary(tmp);

                trackJSON["albumArt"] = Convert.ToBase64String(image.EncodeToPNG());
            }

            Server.ServerBehavior.SendMessage(eventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.StopTrack))]
        private static void StopTrack()
        {
            var eventJSON = new JSONObject();
            eventJSON["type"] = "trackEnd";
            var trackJSON = eventJSON["status"].AsObject;
            Server.ServerBehavior.SendMessage(eventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.HandlePauseGame))]
        private static void PauseTrack()
        {
            var eventJSON = new JSONObject();
            eventJSON["type"] = "trackPause";
            Server.ServerBehavior.SendMessage(eventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.HandleUnpauseGame))]
        private static void ResumeTrack()
        {
            var eventJSON = new JSONObject();
            eventJSON["type"] = "trackResume";
            Server.ServerBehavior.SendMessage(eventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong))]
        private static void CompleteTrack()
        {
            var eventJSON = new JSONObject();
            eventJSON["type"] = "trackComplete";
            Server.ServerBehavior.SendMessage(eventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.FailSong))]
        private static void FailTrack()
        {
            var eventJSON = new JSONObject();
            eventJSON["type"] = "trackFail";
            Server.ServerBehavior.SendMessage(eventJSON);
        }
    }
}
