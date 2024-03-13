using System.Diagnostics.CodeAnalysis;

namespace Code.Helper
{
    public abstract class Constant
    {
        public static readonly string[] LoginPhones = {
            "+886912000371",
            "+886912000372",
            "+886912000373",
            "+886912000374",
            "+886912000375",
            "+886912000376",
            "+886912000377",
            "+886912000378",
            "+886912000379"
        };

        public static readonly string[] LoginNames = {
            "Anya",
            "Celeste",
            "Evelyn",
            "Fiona",
            "Isabella",
            "Ethan",
            "James",
            "Michael",
            "William"
        };

        public static readonly string[] LogonAvatars = 
        {
            "PlayerAvatar_01",
            "PlayerAvatar_02",
            "PlayerAvatar_06",
            "PlayerAvatar_07",
            "PlayerAvatar_09",
            "PlayerAvatar_04",
            "PlayerAvatar_03",
            "PlayerAvatar_09",
            "PlayerAvatar_05"
        };
        
        // dev
        public const string ServerURL = "https://dev.cyberpoker.online/api/v1";
        public const string SocketURL = "wss://dev.cyberpoker.online/v1/client-agent";
        
        // staging
        // public const string ServerURL = "https://staging.cyberpoker.online/api/v1";
        // public const string SocketURL = "wss://staging.cyberpoker.online/v1/client-agent";
        
        public const string PfKeyUserToken = "UserToken";
        public const string PfKeyUserID = "UserPlayerID";

        public const string PfKeyCompetitionID = "CompetitionID";
        public const string PfKeyCompetitionName = "CompetitionName";
        public const string PfKeyTableID = "TableID";
        public const string PfKeyTableName = "TableName";
        
        public const string PfKeyBGMVolume = "BGMVolume";
        public const string PfKeySoundEffectVolume = "SoundEffectVolume";

        public const string SocketOnCompetitionUpdated = "competition_updated";
        public const string SocketOnTableUpdated = "table_updated";
        public const string SocketOnAutoModeUpdated = "game_player_auto_mode_updated";

        public const int TimeoutJitterDelayValue = 9999;

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public abstract class PlayerStatus
        {
            public const string WaitingTableBalancing = "waiting_table_balancing";  // 等待拆併桌中
            public const string Playing = "playing";                                // 比賽中
            public const string ReBuyWaiting = "re_buy_waiting";                    // 等待補碼中 (已不再桌次內)
            public const string Knockout = "knockout";                              // 已淘汰
            public const string CashLeaving = "cash_leaving";                       // 現金桌離開中 (結算時就會離開)
        }
        
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public abstract class CompetitionMode
        {
            public const string CompetitionModeCT = "ct";       // 倒數錦標賽
            public const string CompetitionModeMTT = "mtt";     // 大型錦標賽
            public const string CompetitionModeCash = "cash";   // 現金桌
        }
            
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public abstract class CompetitionStatus
        {
            public const string PreRegistering = "pre-registering"; // 賽事已建立 (但不可報名)
            public const string Registering = "registering";        // 賽事已建立 (可報名參賽)
            public const string DelayedBuyIn = "delayed_buy_in";    // 賽事已建立 (延遲買入)
            public const string StoppedBuyIn = "stopped_buy_in";    // 賽事已建立 (停止買入)
            public const string End = "end";                        // 賽事已結束 (正常結束)
            public const string AutoEnd = "auto_end";               // 賽事已結束 (開賽未成功自動關閉)
            public const string ForceEnd = "force_end";             // 賽事已結束 (其他原因強制關閉)
            public const string Restoring = "restoring";            // 賽事資料轉移中 (Graceful Shutdown Use)
        }
        
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public abstract class TableStatus
        {
            // TableStateStatus: Table
            public const string TableCreated = "table_created";     // 桌次已建立
            public const string TablePausing = "table_pausing";     // 桌次暫停中
            public const string TableRestoring = "table_restoring"; // 桌次轉移中 (Graceful Shutdown)
            public const string TableBalancing = "table_balancing"; // 桌次拆併桌中
            public const string TableClosed = "table_closed";       // 桌次已結束
            
            // TableStateStatus: Game
            public const string TableGameOpened = "table_game_opened";      // 桌次內遊戲已開局
            public const string TableGamePlaying = "table_game_playing";    // 桌次內遊戲開打中
            public const string TableGameSettled = "table_game_settled";    // 桌次內遊戲已結算
        }
        
        public abstract class GameStatusCurrentEvent
        {
            public const string ReadyRequested = "ReadyRequested";
            public const string BlindsRequested = "BlindsRequested";
            public const string RoundStarted = "RoundStarted";
            public const string RoundClosed = "RoundClosed";
            public const string GameClosed = "GameClosed";
        }
        
        public const string GameStatusPlayerActionReady = "ready";

        public abstract class GameStatusPlayerAction
        {
            public const string Fold = "fold";
            public const string Call = "call";
            public const string Check = "check";
            public const string Bet = "bet";
            public const string Raise = "raise";
            public const string Allin = "allin";
        }
    }
}