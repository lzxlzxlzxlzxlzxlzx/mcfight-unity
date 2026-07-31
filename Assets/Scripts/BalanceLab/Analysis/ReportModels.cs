using System;
using System.Collections.Generic;

namespace MCFight.BalanceLab
{
    // ===== P4 分析层数据模型 =====

    [Serializable]
    public class SessionReport
    {
        public string SessionId;
        public string PlanTitle;
        public DateTime StartTime;
        public DateTime EndTime;
        public float TotalDuration;
        public int TotalMatches;
        public int CompletedMatches;
        public int SkippedMatches;
        public List<UnitRanking> Rankings = new();
        public List<CounterRelation> Counters = new();
        public List<BalanceSuggestion> Suggestions = new();
        public List<string> KeyFindings = new();
        public string Summary;
    }

    [Serializable]
    public class UnitRanking
    {
        public string MonsterId;
        public string DisplayName;
        public int Price;
        public int Wins;
        public int Losses;
        public int Draws;
        public int TotalMatches;
        public float WinRate;
        public float AvgDamageDealt;
        public float AvgDamageTaken;
        public float AvgKills;
        public float AvgSurvivalTime;
        public float PowerScore;
        public float PowerDelta;
        public string BalanceStatus; // "Balanced", "Overpowered", "Underpowered"
        public float Confidence;
    }

    [Serializable]
    public class CounterRelation
    {
        public string AttackerId;
        public string AttackerName;
        public string TargetId;
        public string TargetName;
        public float WinRate;       // attacker vs target
        public int SampleSize;
    }

    [Serializable]
    public class BalanceSuggestion
    {
        public string MonsterId;
        public string DisplayName;
        public string Field;         // "hp", "attack", "price"
        public float CurrentValue;
        public float SuggestedValue;
        public float ChangePercent;
        public string Reason;
        public float Confidence;
        public int SampleSize;
    }

    // ===== 可序列化包装（用于 JsonUtility） =====

    [Serializable]
    public class SessionReportFile
    {
        public SessionReportData data = new();
    }

    [Serializable]
    public class SessionReportData
    {
        public string session_id;
        public string plan_title;
        public string start_time;
        public string end_time;
        public int total_matches;
        public int completed_matches;
        public int skipped_matches;
        public string summary;
        public List<string> key_findings = new();
        public List<RankingEntry> rankings = new();
        public List<CounterEntry> counters = new();
        public List<SuggestionEntry> suggestions = new();
    }

    [Serializable]
    public class RankingEntry
    {
        public string monster_id;
        public string display_name;
        public int price;
        public int wins, losses, draws, total;
        public float win_rate;
        public float avg_damage_dealt;
        public float avg_kills;
        public float power_score;
        public float power_delta;
        public string balance_status;
        public float confidence;
    }

    [Serializable]
    public class CounterEntry
    {
        public string attacker_id;
        public string attacker_name;
        public string target_id;
        public string target_name;
        public float win_rate;
        public int sample_size;
    }

    [Serializable]
    public class SuggestionEntry
    {
        public string monster_id;
        public string display_name;
        public string field;
        public float current_value;
        public float suggested_value;
        public float change_percent;
        public string reason;
        public float confidence;
    }

    // ===== Archive Index =====

    [Serializable]
    public class ArchiveIndex
    {
        public List<ArchiveEntry> sessions = new();
    }

    [Serializable]
    public class ArchiveEntry
    {
        public string session_id;
        public string title;
        public string created_at;
        public string status;
        public int total_matches;
        public int completed_matches;
        public string key_finding;
    }
}
