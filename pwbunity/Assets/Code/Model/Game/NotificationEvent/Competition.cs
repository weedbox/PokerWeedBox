using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Code.Model.Game.NotificationEvent
{
    public class Competition
    {
        [JsonProperty("update_serial")] public long UpdateSerial; // 更新序列號 (數字越大越晚發生)
        [JsonProperty("id")] public string ID; // 賽事 Unique ID
        [JsonProperty("meta")] public CompetitionMeta Meta; // 賽事固定資料
        [JsonProperty("state")] [CanBeNull] public CompetitionState State; // 賽事動態資料
        [JsonProperty("update_at")] public long UpdateAt; // 更新時間 (Seconds)
    }

    public class CompetitionMeta
    {
        [JsonProperty("blind")] public Blind Blind; // 盲注資訊
        [JsonProperty("max_duration")] public int MaxDuration; // 比賽時間總長 (Seconds)
        [JsonProperty("min_player_count")] public int MinPlayerCount; // 最小參賽人數
        [JsonProperty("max_player_count")] public int MaxPlayerCount; // 最大參賽人數
        [JsonProperty("table_max_seat_count")] public int TableMaxSeatCount; // 每桌人數上限
        [JsonProperty("table_min_player_count")] public int TableMinPlayerCount; // 每桌最小開打數
        [JsonProperty("rule")] public string Rule; // 德州撲克規則, 常牌(default), 短牌(short_deck), 奧瑪哈(omaha)
        [JsonProperty("mode")] public string Mode; // 賽事模式 (CT, MTT, Cash)
        [JsonProperty("re_buy_setting")] public ReBuySetting ReBuySetting; // 補碼設定
        [JsonProperty("addon_setting")] public AddonSetting AddonSetting; // 增購設定
        [JsonProperty("advance_setting")] public AdvanceSetting AdvanceSetting; // 晉級設定
        [JsonProperty("action_time")] public int ActionTime; // 思考時間 (Seconds)
        [JsonProperty("min_chip_unit")] public long MinChipUnit; // 最小單位籌碼量
    }
    
    public class Blind
    {
        [JsonProperty("id")] public string ID; // ID
        [JsonProperty("initial_level")] public int InitialLevel; // 起始盲注級別
        [JsonProperty("final_buy_in_level_idx")] public int FinalBuyInLevelIdx; // 最後買入盲注等級索引值
        [JsonProperty("dealer_blind_time")] public int DealerBlindTime; // Dealer 位置要收取的前注倍數 (短牌用)
        [JsonProperty("levels")] [CanBeNull] public List<BlindLevel> Levels; // 級別資訊列表

        public class BlindLevel
        {
            [JsonProperty("level")] public int Level; // 盲注等級(-1 表示中場休息)
            [JsonProperty("sb")] public long Sb; // 小盲籌碼量
            [JsonProperty("bb")] public long Bb; // 大盲籌碼量
            [JsonProperty("ante")] public long Ante; // 前注籌碼量
            [JsonProperty("duration")] public int Duration; // 等級持續時間 (Seconds)
            [JsonProperty("allow_addon")] public bool AllowAddon; // 是否允許增購
        }
    }

    public class ReBuySetting
    {
        [JsonProperty("max_time")] public int MaxTime; // 最大次數
        [JsonProperty("waiting_time")] public int WaitingTime; // 玩家可補碼時間 (Seconds)
    }

    public class AddonSetting
    {
        [JsonProperty("is_break_only")] public bool IsBreakOnly; // 是否中場休息限定
        [JsonProperty("redeem_chips")] [CanBeNull] public List<long> RedeemChips; // 可兌換籌碼數
        [JsonProperty("max_time")] public int MaxTime; // 最大次數
    }

    public class AdvanceSetting
    {
        [JsonProperty("rule")] public string Rule; // 晉級方式
        [JsonProperty("player_count")] public int PlayerCount; // 晉級人數
        [JsonProperty("blind_level")] public int BlindLevel; // 晉級盲注級別
    }

    public class CompetitionState
    {
        [JsonProperty("open_at")] public long OpenAt; // 賽事建立時間 (可報名、尚未開打)
        [JsonProperty("disable_at")] public long DisableAt; // 賽事未開打前，賽局可見時間 (Seconds)
        [JsonProperty("start_at")] public long StartAt; // 賽事開打時間 (可報名、開打) (Seconds)
        [JsonProperty("end_at")] public long EndAt; // 賽事結束時間 (Seconds)
        [JsonProperty("blind_state")] [CanBeNull] public BlindState BlindState; // 盲注狀態
        [JsonProperty("players")] [CanBeNull] public List<CompetitionPlayer> Players; // 參與過賽事玩家陣列
        [JsonProperty("status")] public string Status; // 賽事狀態
        // [JsonProperty("tables")] public List<Table> tables; // 多桌
        [JsonProperty("rankings")] [CanBeNull] public List<CompetitionRank> Rankings; // 停止買入後玩家排名 (陣列 Index 即是排名 rank - 1, ex: index 0 -> 第一名, index 1 -> 第二名...)
        [JsonProperty("advance_state")] [CanBeNull] public AdvanceState AdvanceState; // 晉級狀態
        [JsonProperty("statistic")] [CanBeNull] public Statistic Statistic; // 賽事統計資料
    }

    public class BlindState
    {
        [JsonProperty("final_buy_in_level_idx")] public int FinalBuyInLevelIdx; // 最後買入盲注等級索引值
        [JsonProperty("current_level_index")] public int CurrentLevelIndex; // 現在盲注等級級別索引值
        [JsonProperty("end_ats")] [CanBeNull] public List<long> EndAts; // 每個等級結束時間 (Seconds)
    }

    public class CompetitionPlayer
    {
        [JsonProperty("player_id")] public string PlayerID; // 玩家 ID
        [JsonProperty("table_id")] public string TableID; // 當前桌次 ID
        [JsonProperty("seat")] public int Seat; // 當前座位
        [JsonProperty("join_at")] public long JoinAt; // 加入時間 (Seconds)

        // current info
        [JsonProperty("status")] public string Status; // 參與玩家狀態
        [JsonProperty("rank")] public int Rank; // 當前桌次排名
        [JsonProperty("chips")] public long Chips; // 當前籌碼
        [JsonProperty("is_re_buying")] public bool IsReBuying; // 是否正在補碼
        [JsonProperty("re_buy_end_at")] public long ReBuyEndAt; // 最後補碼時間 (Seconds)
        [JsonProperty("re_buy_times")] public int ReBuyTimes; // 補碼次數
        [JsonProperty("addon_times")] public int AddonTimes; // 增購次數

        // statistics info
        // best
        [JsonProperty("best_winning_pot_chips")] public long BestWinningPotChips; // 贏得最大底池籌碼數
        [JsonProperty("best_winning_combo")] [CanBeNull] public List<string> BestWinningCombo; // 身為贏家時最大的牌型組合
        [JsonProperty("best_winning_type")] public string BestWinningType; // 身為贏家時最大的牌型類型
        [JsonProperty("best_winning_power")] public int BestWinningPower; // 身為贏家時最大的牌型牌力

        // accumulated info
        // competition/table
        [JsonProperty("total_redeem_chips")] public long TotalRedeemChips; // 累積兌換籌碼
        [JsonProperty("total_game_counts")] public long TotalGameCounts; // 總共玩幾手牌

        // game: round & actions
        [JsonProperty("total_walk_times")] public long TotalWalkTimes; // Preflop 除了大盲以外的人全部 Fold，而贏得籌碼的次數
        [JsonProperty("total_vpip_times")] public int TotalVpipTimes; // 入池總次數
        [JsonProperty("total_fold_times")] public int TotalFoldTimes; // 棄牌總次數
        [JsonProperty("total_preflop_fold_times")] public int TotalPreflopFoldTimes; // Preflop 棄牌總次數
        [JsonProperty("total_flop_fold_times")] public int TotalFlopFoldTimes; // Flop 棄牌總次數
        [JsonProperty("total_turn_fold_times")] public int TotalTurnFoldTimes; // Turn 棄牌總次數
        [JsonProperty("total_river_fold_times")] public int TotalRiverFoldTimes; // River 棄牌總次數
        [JsonProperty("total_action_times")] public int TotalActionTimes; // 下注動作總次數
        [JsonProperty("total_raise_times")] public int TotalRaiseTimes; // 加注/入池總次數(AllIn&Raise、Raise、Bet)
        [JsonProperty("total_call_times")] public int TotalCallTimes; // 跟注總次數
        [JsonProperty("total_check_times")] public int TotalCheckTimes; // 過牌總次數
        [JsonProperty("total_profit_times")] public int TotalProfitTimes; // 總共贏得籌碼次數
    }

    public class CompetitionRank
    {
        [JsonProperty("player_id")] public string PlayerID; // 玩家 ID
        [JsonProperty("final_chips")] public long FinalChips; // 玩家最後籌碼數
    }

    public class AdvanceState
    {
        [JsonProperty("status")] public string Status; // 晉級狀態
        [JsonProperty("total_tables")] public int TotalTables; // 總桌數
        [JsonProperty("updated_tables")] public int UpdatedTables; // 已更新桌數
        [JsonProperty("updated_table_ids")] [CanBeNull] public List<string> UpdatedTableIds; // 已更新桌次 ID
    }

    public class Statistic
    {
        [JsonProperty("total_buy_in_count")] public int TotalBuyInCount; // 總買入次數
    }
}