using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Code.Model.Game.NotificationEvent
{
    public class Table
    {
        [JsonProperty("update_serial")] public long UpdateSerial; // 更新序列號 (數字越大越晚發生)
        [JsonProperty("id")] public string ID; // 桌次 Unique ID
        [JsonProperty("meta")] public TableMeta Meta; // 桌次固定資料
        [JsonProperty("state")] [CanBeNull] public TableState State; // 桌次動態資料
        [JsonProperty("update_at")] public long UpdateAt; // 更新時間 (Seconds)
    }

    public class TableMeta
    {
        [JsonProperty("competition_id")] public string CompetitionID; // 賽事 ID
        [JsonProperty("rule")] public string Rule; // 德州撲克規則, 常牌(default), 短牌(short_deck), 奧瑪哈(omaha)
        [JsonProperty("mode")] public string Mode; // 賽事模式 (CT, MTT, Cash)
        [JsonProperty("max_duration")] public int MaxDuration; // 比賽時間總長 (Seconds)
        [JsonProperty("table_max_seat_count")] public int TableMaxSeatCount; // 每桌人數上限
        [JsonProperty("table_min_player_count")] public int TableMinPlayerCount; // 每桌最小開打數
        [JsonProperty("min_chip_unit")] public long MinChipUnit; // 最小單位籌碼量
        [JsonProperty("action_time")] public int ActionTime; // 玩家動作思考時間 (Seconds)
    }

    public class TableState
    {
        [JsonProperty("status")] public string Status; // 當前桌次狀態
        [JsonProperty("start_at")] public long StartAt; // 開打時間 (Seconds)
        [JsonProperty("seat_map")] [CanBeNull] public List<int> SeatMap; // 座位入座狀況，index: seat index (0-8), value: TablePlayerState index (-1 by default)
        [JsonProperty("blind_state")] [CanBeNull] public TableBlindState BlindState; // 盲注狀態
        [JsonProperty("current_dealer_seat")] public int CurrentDealerSeat; // 當前 Dealer 座位編號
        [JsonProperty("current_bb_seat")] public int CurrentBbSeat; // 當前 BB 座位編號
        [JsonProperty("player_states")] [CanBeNull] public List<TablePlayerState> PlayerStates; // 賽局桌上玩家狀態
        [JsonProperty("game_count")] public int GameCount; // 執行牌局遊戲次數 (遊戲跑幾輪)
        [JsonProperty("game_player_indexes")] [CanBeNull] public List<int> GamePlayerIndexes; // 本手正在玩的 PlayerIndex 陣列 (陣列 index 為從 Dealer 位置開始的 PlayerIndex)，GameEngine 用
        [JsonProperty("game_state")] [CanBeNull] public GameState GameState; // 本手狀態
        [JsonProperty("seat_changes")] [CanBeNull] public TableGameSeatChanges SeatChanges; // 新的一手座位狀況
        [JsonProperty("last_player_game_action")] [CanBeNull] public TablePlayerGameAction LastPlayerGameAction; // 最新一筆玩家牌局動作
    }

    public class TableBlindState
    {
        [JsonProperty("level")] public int Level; // 盲注等級(-1 表示中場休息)
        [JsonProperty("ante")] public long Ante; // 前注籌碼量
        [JsonProperty("dealer")] public long Dealer; // 庄位籌碼量
        [JsonProperty("sb")] public long Sb; // 大盲籌碼量
        [JsonProperty("bb")] public long Bb; // 小盲籌碼量
    }

    public class TablePlayerState
    {
        [JsonProperty("player_id")] public string PlayerID; // 玩家 ID
        [JsonProperty("seat")] public int Seat; // 座位編號 0 ~ 8
        [JsonProperty("positions")] [CanBeNull] public List<string> Positions; // 場上位置
        [JsonProperty("is_participated")] public bool IsParticipated; // 玩家是否參戰
        [JsonProperty("is_between_dealer_bb")] public bool IsBetweenDealerBb; // 玩家入場時是否在 Dealer & BB 之間
        [JsonProperty("bankroll")] public long Bankroll; // 玩家身上籌碼
        [JsonProperty("is_in")] public bool IsIn; // 玩家是否入座
        [JsonProperty("game_statistics")] public GameStatistics GameStatistics; // 玩家每手遊戲統計
    }
    
    public class GameStatistics
    {
        [JsonProperty("action_times")] public int ActionTimes; // 下注動作總次數
        [JsonProperty("raise_times")] public int RaiseTimes; // 加注總次數
        [JsonProperty("call_times")] public int CallTimes; // 跟注總次數
        [JsonProperty("check_times")] public int CheckTimes; // 過牌總次數
        [JsonProperty("is_fold")] public bool IsFold; // 是否蓋牌
        [JsonProperty("fold_round")] public string FoldRound; // 蓋牌回合
    }

    public class TableGameSeatChanges
    {
        [JsonProperty("new_dealer")] public int NewDealer; // 下一手 Dealer 座位編號
        [JsonProperty("new_sb")] public int NewSb; // 下一手 SB 座位編號
        [JsonProperty("new_bb")] public int NewBb; // 下一手 BB 座位編號
    }

    public class TablePlayerGameAction
    {
        [JsonProperty("table_id")] public string TableID; // 桌次 ID
        [JsonProperty("game_id")] public string GameID; // 遊戲 ID
        [JsonProperty("game_count")] public int GameCount; // 執行牌局遊戲次數 (遊戲跑幾輪)
        [JsonProperty("round")] public string Round; // 哪回合
        [JsonProperty("update_at")] public long UpdateAt; // 更新時間 (Seconds)
        [JsonProperty("player_id")] public string PlayerID; // 玩家 ID
        [JsonProperty("seat")] public int Seat; // 座位編號 0 ~ 8
        [JsonProperty("positions")] [CanBeNull] public List<string> Positions; // 場上位置
        [JsonProperty("action")] public string Action; // 動作
        [JsonProperty("chips")] public long Chips; // 下注籌碼量
    }
}