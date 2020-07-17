﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace BDArmory.Competition
{

    public class BDAScoreClient
    {
        private BDAScoreService service;

        private string baseUrl = "https://bdascores.herokuapp.com";

        private string vesselPath = "";

        private string competitionHash = "";

        private bool pendingRequest = false;

        public CompetitionModel competition = null;

        public HeatModel activeHeat = null;

        public Dictionary<int, HeatModel> heats = new Dictionary<int, HeatModel>();

        public Dictionary<int, VesselModel> vessels = new Dictionary<int, VesselModel>();

        public Dictionary<int, PlayerModel> players = new Dictionary<int, PlayerModel>();


        public BDAScoreClient(BDAScoreService service, string vesselPath, string hash)
        {
            this.service = service;
            this.vesselPath = vesselPath;
            this.competitionHash = hash;
        }

        public IEnumerator GetCompetition(string hash)
        {
            if (pendingRequest)
            {
                Debug.Log("[BDAScoreClient] Request already pending");
                yield break;
            }
            pendingRequest = true;

            string uri = string.Format("{0}/competitions/{1}.json", baseUrl, hash);
            Debug.Log(string.Format("[BDAScoreClient] GET {0}", uri));
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                yield return webRequest.SendWebRequest();
                if (!webRequest.isHttpError)
                {
                    ReceiveCompetition(webRequest.downloadHandler.text);
                }
                else
                {
                    Debug.Log(string.Format("[BDAScoreClient] Failed to get competition {0}: {1}", hash, webRequest.error));
                }
            }

            pendingRequest = false;
        }

        private void ReceiveCompetition(string response)
        {
            if( response == null || "".Equals(response) )
            {
                Debug.Log(string.Format("[BDAScoreClient] Received empty competition response"));
                return;
            }
            CompetitionModel competition = ParseJson<CompetitionModel>(response);
            if( competition == null )
            {
                Debug.Log(string.Format("[BDAScoreClient] Failed to parse competition: {0}", response));
            }
            else
            {
                this.competition = competition;
                Debug.Log(string.Format("[BDAScoreClient] Competition: {0}", competition.ToString()));
            }
        }

        public IEnumerator GetHeats(string hash)
        {
            if (pendingRequest)
            {
                Debug.Log("[BDAScoreClient] Request already pending");
                yield break;
            }
            pendingRequest = true;

            string uri = string.Format("{0}/competitions/{1}/heats.json", baseUrl, hash);
            Debug.Log(string.Format("[BDAScoreClient] GET {0}", uri));
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return webRequest.SendWebRequest();
                if (!webRequest.isHttpError)
                {
                    string response = Encoding.UTF8.GetString(webRequest.downloadHandler.data);
                    ReceiveHeats(response);
                }
                else
                {
                    Debug.Log(string.Format("[BDAScoreClient] Failed to get heats for {0}: {1}", hash, webRequest.error));
                }
            }

            pendingRequest = false;
        }

        private void ReceiveHeats(string response)
        {
            if (response == null || "".Equals(response))
            {
                Debug.Log(string.Format("[BDAScoreClient] Received empty heat collection response"));
                return;
            }
            string wrapped = "{\"heats\":" + response + "}";
            HeatCollection collection = ParseJson<HeatCollection>(wrapped);
            heats.Clear();
            if (collection == null || collection.heats == null)
            {
                Debug.Log(string.Format("[BDAScoreClient] Failed to parse heat collection: {0}", response));
                return;
            }
            foreach (HeatModel heatModel in collection.heats)
            {
                Debug.Log(string.Format("[BDAScoreClient] Heat: {0}", heatModel.ToString()));
                heats.Add(heatModel.id, heatModel);
            }
            Debug.Log(string.Format("[BDAScoreClient] Heats: {0}", heats.Count));
        }

        public IEnumerator GetPlayers(string hash)
        {
            if (pendingRequest)
            {
                Debug.Log("[BDAScoreClient] Request already pending");
                yield break;
            }
            pendingRequest = true;

            string uri = string.Format("{0}/players.json", baseUrl);
            Debug.Log(string.Format("[BDAScoreClient] GET {0}", uri));
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                yield return webRequest.SendWebRequest();
                if (!webRequest.isHttpError)
                {
                    ReceivePlayers(webRequest.downloadHandler.text);
                }
                else
                {
                    Debug.Log(string.Format("[BDAScoreClient] Failed to get players for {0}: {1}", hash, webRequest.error));
                }
            }

            pendingRequest = false;
        }

        private void ReceivePlayers(string response)
        {
            if (response == null || "".Equals(response))
            {
                Debug.Log(string.Format("[BDAScoreClient] Received empty player collection response"));
                return;
            }
            string wrapper = "{\"players\":" + response + "}";
            PlayerCollection collection = ParseJson<PlayerCollection>(wrapper);
            players.Clear();
            if( collection == null || collection.players == null )
            {
                Debug.Log(string.Format("[BDAScoreClient] Failed to parse player collection: {0}", wrapper));
                return;
            }
            foreach (PlayerModel playerModel in collection.players)
            {
                Debug.Log(string.Format("[BDAScoreClient] Player {0}", playerModel.ToString()));
                players.Add(playerModel.id, playerModel);
            }
            Debug.Log(string.Format("[BDAScoreClient] Players: {0}", players.Count));
        }

        public IEnumerator GetVessels(string hash, HeatModel heat)
        {
            if (pendingRequest)
            {
                Debug.Log("[BDAScoreClient] Request already pending");
                yield break;
            }
            pendingRequest = true;

            string uri = string.Format("{0}/competitions/{1}/heats/{2}/vessels.json", baseUrl, hash, heat.id);
            Debug.Log(string.Format("[BDAScoreClient] GET {0}", uri));
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                yield return webRequest.SendWebRequest();
                if (!webRequest.isHttpError)
                {
                    ReceiveVessels(webRequest.downloadHandler.text);
                }
                else
                {
                    Debug.Log(string.Format("[BDAScoreClient] Failed to get vessels {0}/{1}: {2}", hash, heat, webRequest.error));
                }
            }

            pendingRequest = false;
        }

        private void ReceiveVessels(string response)
        {
            if (response == null || "".Equals(response))
            {
                Debug.Log(string.Format("[BDAScoreClient] Received empty vessel collection response"));
                return;
            }
            string wrapper = "{\"vessels\":" + response + "}";
            VesselCollection collection = ParseJson<VesselCollection>(wrapper);
            vessels.Clear();
            if( collection == null || collection.vessels == null )
            {
                Debug.Log(string.Format("[BDAScoreClient] Failed to parse vessel collection: {0}", wrapper));
                return;
            }
            foreach (VesselModel vesselModel in collection.vessels)
            {
                Debug.Log(string.Format("[BDAScoreClient] Vessel {0}", vesselModel.ToString()));
                vessels.Add(vesselModel.id, vesselModel);
            }
            Debug.Log(string.Format("[BDAScoreClient] Vessels: {0}", vessels.Count));
        }

        public IEnumerator PostRecords(string hash, int heat, List<RecordModel> records)
        {
            IEnumerable<string> recordsJson = records.Select(e => JsonUtility.ToJson(e));
            string recordsJsonStr = string.Join(",", recordsJson);
            string requestBody = string.Format("{{\"records\":[{0}]}}", recordsJsonStr);

            byte[] rawBody = Encoding.UTF8.GetBytes(requestBody);
            string uri = string.Format("{0}/competitions/{1}/heats/{2}/records/batch.json", baseUrl, hash, heat);
            Debug.Log(string.Format("[BDAScoreClient] POST {0}", uri));
            using (UnityWebRequest webRequest = new UnityWebRequest(uri))
            {
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.uploadHandler = new UploadHandlerRaw(rawBody);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.method = UnityWebRequest.kHttpVerbPOST;

                yield return webRequest.SendWebRequest();

                if( webRequest.isHttpError )
                {
                    Debug.Log(string.Format("[BDAScoreClient] Failed to post records: {0}", webRequest.error));
                }
            }
        }

        public IEnumerator GetCraftFiles(string hash, HeatModel model)
        {
            // already have the vessels in memory; just need to fetch the files
            foreach (VesselModel v in vessels.Values)
            {
                Debug.Log(string.Format("[BDAScoreClient] GET {0}", v.craft_url));
                using (UnityWebRequest webRequest = UnityWebRequest.Get(v.craft_url))
                {
                    yield return webRequest.SendWebRequest();
                    if (!webRequest.isHttpError)
                    {
                        byte[] rawBytes = webRequest.downloadHandler.data;
                        SaveCraftFile(v, rawBytes);
                    }
                    else
                    {
                        Debug.Log(string.Format("[BDAScoreClient] Failed to get craft for {0}: {1}", v.id, webRequest.error));
                    }
                }
            }
        }

        private void SaveCraftFile(VesselModel vessel, byte[] bytes)
        {
            PlayerModel p = players[vessel.player_id];
            if (p == null)
            {
                Debug.Log(string.Format("[BDAScoreClient] Failed to save craft for vessel {0}, player {1}", vessel.id, vessel.player_id));
                return;
            }
            string filename = string.Format("{0}/{1}.craft", vesselPath, p.name);
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("[BDAScoreClient] Saved craft for player {0}", p.name));
        }

        public IEnumerator StartHeat(string hash, HeatModel heat)
        {
            if (pendingRequest)
            {
                Debug.Log("[BDAScoreClient] Request already pending");
                yield break;
            }
            pendingRequest = true;

            string uri = string.Format("{0}/competitions/{1}/heats/{2}/start", baseUrl, hash, heat.id);
            using (UnityWebRequest webRequest = new UnityWebRequest(uri))
            {
                yield return webRequest.SendWebRequest();
                if (!webRequest.isHttpError)
                {
                    Debug.Log(string.Format("[BDAScoreClient] Started heat {1} in {0}", hash, heat.order));
                }
                else
                {
                    Debug.Log(string.Format("[BDAScoreClient] Failed to start heat {1} in {0}: {2}", hash, heat.order, webRequest.error));
                }
            }

            pendingRequest = false;
        }

        public IEnumerator StopHeat(string hash, HeatModel heat)
        {
            if (pendingRequest)
            {
                Debug.Log("[BDAScoreClient] Request already pending");
                yield break;
            }
            pendingRequest = true;

            string uri = string.Format("{0}/competitions/{1}/heats/{2}/stop", baseUrl, hash, heat.id);
            using (UnityWebRequest webRequest = new UnityWebRequest(uri))
            {
                yield return webRequest.SendWebRequest();
                if (!webRequest.isHttpError)
                {
                    Debug.Log(string.Format("[BDAScoreClient] Stopped heat {1} in {0}", hash, heat.order));
                }
                else
                {
                    Debug.Log(string.Format("[BDAScoreClient] Failed to stop heat {1} in {0}: {2}", hash, heat.order, webRequest.error));
                }
            }

            pendingRequest = false;
        }

        private T ParseJson<T>(string source)
        {
            T result;
            try
            {
                result = JsonConvert.DeserializeObject<T>(source);
            }
            catch(Exception e)
            {
                Debug.Log("[BDAScoreClient] error: " + e);
                result = default(T);
            }
            return result;
        }

    }
}
