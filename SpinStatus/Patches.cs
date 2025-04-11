using HarmonyLib;
using UnityEngine;
using SimpleJSON;

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
        private static void Postfix(this PlayState playState, int noteIndex, TempNoteState __state)
        {
            ScoreState scoreState = playState.scoreState;
            ref NoteState noteState = ref scoreState.GetNoteState(noteIndex);

            // if (playState.playStateStatus <= PlayStateStatus.Preparing) { return; }

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
                    // Plugin.Logger.LogInfo($"{currentTime}: Score Updated");

                    var scoreEventJSON = new JSONObject();

                    scoreEventJSON["type"] = "scoreEvent";
                    var scoreJSON = scoreEventJSON["status"].AsObject;
                    scoreJSON["score"] = scoreState.TotalScore;
                    scoreJSON["combo"] = scoreState.combo;
                    scoreJSON["maxCombo"] = scoreState.maxCombo;

                    Server.ServerBehavior.SendMessage(scoreEventJSON);
                }
            }

            if (!becameDoneWith && !becameSustained) { return; }

            if (note.NoteType == NoteType.SectionContinuationOrEnd ||
                note.NoteType == NoteType.Checkpoint ||
                note.NoteType == NoteType.TutorialStart) { return; }

            var noteEventJSON = new JSONObject();

            noteEventJSON["type"] = "noteEvent";
            var noteJSON = noteEventJSON["status"].AsObject;
            noteJSON["index"] = noteIndex;
            noteJSON["accuracy"] = noteState.timingAccuracy.ToString();
            noteJSON["type"] = note.NoteType.ToString();
            noteJSON["color"] = (int)note.colorIndex;
            noteJSON["lane"] = (int)note.column;

            Plugin.Logger.LogInfo($"{noteIndex}: Color={note.colorIndex}");
            Server.ServerBehavior.SendMessage(noteEventJSON);
        }
    }

    [HarmonyPatch]
    internal static class SceneEventHandler {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
        private static void StartTrack()
        {
            TrackInfo trackInfo = Track.PlayHandle.loadedData.segments[0].trackInfo;
            TrackInfoMetadata trackMeta = new TrackInfoMetadata(trackInfo);

            Texture2D image = trackInfo.albumArtReference.GetLoadedAsset();

            var eventJSON = new JSONObject();
            eventJSON["type"] = "trackStart";
            var trackJSON = eventJSON["status"].AsObject;
            trackJSON["title"] = trackMeta.title;
            trackJSON["artist"] = trackMeta.artistName;
            trackJSON["feat"] = trackMeta.featArtists;
            // trackJSON["cover"] = System.Convert.ToBase64String(image.EncodeToPNG());

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
    }
}
