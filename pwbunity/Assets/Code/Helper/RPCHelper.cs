using System.Collections.Generic;
using Code.Base;
using Code.Model;
using Code.Model.Game;
using Code.Model.Match;
using Code.Model.PlayerInfo;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Code.Helper
{
    public class RPCHelper : BaseRPC
    {
        #region Auth

        public void SendAuthenticate(string token, UnityAction<RPCResponse<object>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Auth.Authenticate",
                Parameters = new List<object> { token }
            };

            SendMessage(rpc, callback);
        }

        #endregion

        #region System

        public void SendReady(UnityAction<RPCResponse<object>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "System.Ready",
                Parameters = new List<object>()
            };

            SendMessage(rpc, callback);

            SendDeepPing();
        }

        #endregion

        #region PlayerInfo

        public void SendGetCurrentPlayer(UnityAction<RPCResponse<RespPlayer>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "PlayerInfo.GetCurrentPlayer",
                Parameters = new List<object>()
            };

            SendMessage(rpc, callback);
        }

        public void SendGetPlayerLatestData(UnityAction<RPCResponse<RespGetPlayerLatest>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "PlayerInfo.GetPlayerLatestData",
                Parameters = new List<object>()
            };

            SendMessage(rpc, callback);
        }

        public void SendGetPlayer([CanBeNull] string id, [CanBeNull] string uid,
            UnityAction<RPCResponse<RespPlayer>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "PlayerInfo.GetPlayer",
                Parameters = new List<object> { id ?? "", uid ?? "" }
            };

            SendMessage(rpc, callback);
        }

        #endregion

        #region Game

        public void SendListGames(UnityAction<RPCResponse<RespListGame>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Game.ListGames",
                Parameters = new List<object>()
            };

            SendMessage(rpc, callback);
        }

        public void SendListGameLevels(int page, int limit, string gameCategoryId,
            UnityAction<RPCResponse<RespListGameLevels>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Game.ListGameLevels",
                Parameters = new List<object> { page, limit, gameCategoryId }
            };

            SendMessage(rpc, callback);
        }

        public void SendListCompetitions(string req, UnityAction<RPCResponse<RespListCompetitions>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Game.ListCompetitions",
                Parameters = new List<object> { req }
            };

            SendMessage(rpc, callback);
        }

        public void SendListPlayerActiveCompetitions(UnityAction<RPCResponse<ListPlayerActiveCompetitions>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Game.ListPlayerActiveCompetitions",
                Parameters = new List<object>()
            };

            SendMessage(rpc, callback);
        }

        #endregion

        #region Match

        public void SendCompetitionCashBuyIn(string competitionId, string cpp,
            UnityAction<RPCResponse<object>> callback)
        {
            const string latitude = "";
            const string longitude = "";
            var rpc = new RPCRequest
            {
                Method = "Match.CompetitionCashBuyIn",
                Parameters = new List<object> { competitionId, cpp, latitude, longitude }
            };

            SendMessage(rpc, callback);
        }

        public void SendCompetitionCashOut(string competitionId, string tableId,
            UnityAction<RPCResponse<object>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Match.CompetitionCashOut",
                Parameters = new List<object> { competitionId, tableId }
            };

            SendMessage(rpc, callback);
        }

        public void SendUpdateCompetitionEventSubscribeStates(string competitionId, bool isEventSubscribed,
            UnityAction<RPCResponse<object>> callback)
        {
            var req = new ReqUpdateCompetitionEventSubscribeStates();
            req.EventSubscribeStates.Add(new UpdateCompetitionEventSubscribeStates(competitionId, isEventSubscribed));

            var rpc = new RPCRequest
            {
                Method = "Match.UpdateCompetitionEventSubscribeStates",
                Parameters = new List<object> { JsonConvert.SerializeObject(req) }
            };

            SendMessage(rpc, callback);
        }

        public void SendCompetitionGetLatest(string competitionId,
            UnityAction<RPCResponse<RespCompetitionGetLatest>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Match.CompetitionGetLatest",
                Parameters = new List<object> { competitionId }
            };

            SendMessage(rpc, callback);
        }

        public void SendTableGetLatest(string competitionId, string tableId,
            UnityAction<RPCResponse<RespTableGetLatest>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Match.TableGetLatest",
                Parameters = new List<object> { competitionId, tableId }
            };

            SendMessage(rpc, callback);
        }

        public void SendTableJoin(string competitionId, string tableId, UnityAction<RPCResponse<object>> callback)
        {
            const string latitude = "";
            const string longitude = "";
            var rpc = new RPCRequest
            {
                Method = "Match.TableJoin",
                Parameters = new List<object> { competitionId, tableId, latitude, longitude }
            };

            SendMessage(rpc, callback);
        }

        public void SendTableLeave(string competitionId, string tableId, UnityAction<RPCResponse<object>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Match.TableLeave",
                Parameters = new List<object> { competitionId, tableId }
            };

            SendMessage(rpc, callback);
        }

        public void SendGamePlayerReady(string competitionId, string tableId, UnityAction<RPCResponse<object>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Match.GamePlayerReady",
                Parameters = new List<object> { competitionId, tableId }
            };

            SendMessage(rpc, callback);
        }

        public void SendGamePlayerWager(string competitionId, string tableId, string action, long chips,
            UnityAction<RPCResponse<object>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Match.GamePlayerWager",
                Parameters = new List<object> { competitionId, tableId, action, chips }
            };

            SendMessage(rpc, callback);
        }

        public void SendGamePlayerAutoMode(string competitionId, string tableId, bool isOn,
            UnityAction<RPCResponse<object>> callback)
        {
            var rpc = new RPCRequest
            {
                Method = "Match.GamePlayerAutoMode",
                Parameters = new List<object> { competitionId, tableId, isOn }
            };

            SendMessage(rpc, callback);
        }

        #endregion
    }
}