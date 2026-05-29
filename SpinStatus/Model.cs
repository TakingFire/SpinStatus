#nullable enable

using System.Collections.Generic;

namespace SpinStatus.Model
{
    internal struct Event
    {
        public EventType Type { get; set; }
        public object? Status { get; set; }
        public int? Player { get; set; }
    }

    internal enum EventType
    {
        Hello,
        NoteEvent,
        ScoreEvent,
        TrackStart,
        TrackEnd,
        TrackComplete,
        TrackFail,
        TrackPause,
        TrackResume
    }

    internal struct NoteStatus
    {
        public int Index { get; set; }
        public int Lane { get; set; }
        public string Type { get; set; }
        public int Color { get; set; }

        public string Accuracy { get; set; }
        public float Timing { get; set; }
    }

    internal struct ScoreStatus
    {
        public int Score { get; set; }
        public int Combo { get; set; }
        public int MaxCombo { get; set; }
        public string FullCombo { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Multiplier { get; set; }
        public int BaseScore { get; set; }
        public int BaseScoreLost { get; set; }
    }

    internal struct TrackStatus
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Artist { get; set; }
        public string Feat { get; set; }
        public string Charter { get; set; }
        public string AlbumArt { get; set; }

        public float StartTime { get; set; }
        public float EndTime { get; set; }

        public bool IsCustom { get; set; }
        public string Filename { get; set; }

        public List<PlayerStatus> Players { get; set; }
    }

    internal struct PlayerStatus
    {
        public int TotalWins { get; set; }
        public string DisplayName { get; set; }
        public Dictionary<NoteColorType, string> Palette { get; set; }

        public string Difficulty { get; set; }
        public int Rating { get; set; }
        public int MaxScore { get; set; }
    }
}
