using System.Collections.Generic;

namespace MCFight.BalanceLab
{
    public enum LabPhase { Idle, Running, Paused, Stopped, Completed }

    public struct LabLineupEntry
    {
        public string MonsterId;
        public int Count;
    }

    public struct LabTestCase
    {
        public string Id;
        public string Label;
        public LabLineupEntry[] TeamBlue;
        public LabLineupEntry[] TeamRed;
        public int RepeatCount;
    }

    public struct LabMatchResult
    {
        public string CaseId;
        public int RunIndex;
        public int Winner;
        public float Duration;
        public Dictionary<int, UnitBattleStats> UnitStats;
    }

    public struct LabTestCaseResult
    {
        public string CaseId;
        public string Label;
        public List<LabMatchResult> Matches;
        public int BlueWins;
        public int RedWins;
        public int Draws;
    }
}
