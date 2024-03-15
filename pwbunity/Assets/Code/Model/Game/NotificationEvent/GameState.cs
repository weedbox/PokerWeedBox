using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Code.Model.Game.NotificationEvent
{
    public class GameState
    {
        [JsonProperty("game_id")] public string GameID;
        [JsonProperty("created_at")] public long CreatedAt;
        [JsonProperty("updated_at")] public long UpdatedAt;
        [JsonProperty("meta")] public Meta Meta;
        [JsonProperty("status")] public Status Status;
        [JsonProperty("players")] [CanBeNull] public List<PlayerState> Players;
        [JsonProperty("result")] [CanBeNull] public Result Result;
    }

    public class Meta
    {
        [JsonProperty("ante")] public long Ante;
        [JsonProperty("blind")] public BlindSetting Blind;
        [JsonProperty("limit")] public string Limit;
        [JsonProperty("hole_cards_count")] public int HoleCardsCount;
        [JsonProperty("required_hole_cards_count")] public int RequiredHoleCardsCount;
        [JsonProperty("combination_powers")] [CanBeNull] public List<string> CombinationPowers;
        [JsonProperty("deck")] [CanBeNull] public List<string> Deck;
        [JsonProperty("burn_count")] public int BurnCount;
    }
    
    public class BlindSetting
    {
        [JsonProperty("dealer")] public long Dealer;
        [JsonProperty("sb")] public long Sb;
        [JsonProperty("bb")] public long Bb;
    }

    public class Status
    {
        [JsonProperty("mini_bet")] public long MiniBet;
        [JsonProperty("pots")] [CanBeNull] public List<Pot> Pots;
        [JsonProperty("round")] [CanBeNull] public string Round;
        [JsonProperty("burned")] [CanBeNull] public List<string> Burned;
        [JsonProperty("board")] [CanBeNull] public List<string> Board;
        [JsonProperty("previous_raise_size")] public long PreviousRaiseSize;
        [JsonProperty("current_deck_position")] public int CurrentDeckPosition;
        [JsonProperty("current_round_pot")] public long CurrentRoundPot;
        [JsonProperty("current_wager")] public long CurrentWager;
        [JsonProperty("current_raiser")] public int CurrentRaiser;
        [JsonProperty("current_player")] public int CurrentPlayer;
        [JsonProperty("current_event")] public string CurrentEvent;
        [JsonProperty("last_action")] [CanBeNull] public Action LastAction;
    }

    public class Pot
    {
        [JsonProperty("level")] public long Level;
        [JsonProperty("wager")] public long Wager;
        [JsonProperty("total")] public long Total;
        [JsonProperty("contributors")] public Dictionary<string, long> Contributors;
    }

    public class Action
    {
        [JsonProperty("source")] public int Source;
        [JsonProperty("type")] public string Type;
        [JsonProperty("value")] public long? Value;
    }
    
    public class PlayerState
    {
        [JsonProperty("idx")] public int Idx;

        [JsonProperty("positions")] [CanBeNull] public List<string> Positions;

        // Status
        [JsonProperty("acted")] public bool Acted;
        [JsonProperty("did_action")] [CanBeNull] public string DidAction;
        [JsonProperty("fold")] public bool Fold;
        [JsonProperty("vpip")] public bool Vpip; // Voluntarily Put In Pot
        [JsonProperty("allowed_actions")] [CanBeNull] public List<string> AllowedActions;

        // Stack and wager
        [JsonProperty("bankroll")] public long Bankroll;
        [JsonProperty("initial_stack_size")] public long InitialStackSize; // bankroll - pot
        [JsonProperty("stack_size")] public long StackSize; // initial_stack_size - wager
        [JsonProperty("pot")] public long Pot;
        [JsonProperty("wager")] public long Wager; // Set 0 at RoundClosed

        // Hole cards information
        [JsonProperty("hole_cards")] [CanBeNull] public List<string> HoleCards;
        [JsonProperty("combination")] [CanBeNull] public CombinationInfo Combination;
    }
    
    public class CombinationInfo
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("cards")] public List<string> Cards;
        [JsonProperty("power")] public int Power;
    }

    public class Result
    {
        [JsonProperty("players")] public List<PlayerResult> Players;
        [JsonProperty("pots")] public List<PotResult> Pots;
    }
    public class PlayerResult
    {
        [JsonProperty("idx")] public int Idx;
        [JsonProperty("final")] public long Final;
        [JsonProperty("changed")] public long Changed;
    }

    public class PotResult
    {
        [JsonProperty("total")] public long Total;
        [JsonProperty("winners")] public List<Winner> Winners;
    }
    
    public class Winner
    {
        [JsonProperty("idx")] public int Idx;
        [JsonProperty("chips")] public long Chips;
    }
}