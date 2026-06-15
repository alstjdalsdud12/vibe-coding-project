using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public class ApiClient : MonoBehaviour
{
    private const string BASE_URL = "http://localhost:3000/api";

    [Serializable]
    private class CreateRequest
    {
        public string name;
        public string appearance;
        public string weapon;
        public string concept;
        public string worldview;
    }

    public IEnumerator CreateCharacter(
        string name, string appearance, string weapon, string concept, string worldview,
        Action<CharacterData> onSuccess, Action<string> onError)
    {
        var body = new CreateRequest { name = name, appearance = appearance, weapon = weapon, concept = concept, worldview = worldview };
        string json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest(BASE_URL + "/characters", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

        var res = JsonUtility.FromJson<CharacterResponse>(req.downloadHandler.text);
        if (res.success) onSuccess?.Invoke(res.data);
        else onError?.Invoke("캐릭터 생성 실패");
    }

    public IEnumerator GetAllCharacters(Action<CharacterListItem[]> onSuccess, Action<string> onError)
    {
        using var req = UnityWebRequest.Get(BASE_URL + "/characters");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

        var res = JsonUtility.FromJson<CharacterListResponse>(req.downloadHandler.text);
        if (res.success) onSuccess?.Invoke(res.data);
        else onError?.Invoke("목록 조회 실패");
    }

    public IEnumerator GetCharacterById(string id, Action<CharacterData> onSuccess, Action<string> onError)
    {
        using var req = UnityWebRequest.Get(BASE_URL + "/characters/" + id);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

        Debug.Log("[API] 캐릭터 응답: " + req.downloadHandler.text);
        var res = JsonUtility.FromJson<CharacterResponse>(req.downloadHandler.text);
        if (res.success) onSuccess?.Invoke(res.data);
        else onError?.Invoke("캐릭터 조회 실패");
    }

    public IEnumerator DeleteCharacter(string id, Action onSuccess, Action<string> onError)
    {
        using var req = UnityWebRequest.Delete(BASE_URL + "/characters/" + id);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
        else onSuccess?.Invoke();
    }

    [Serializable]
    private class StateRequest
    {
        public InventoryItem[]   inventory;
        public int               gold;
        public QuestProgress     questProgress;
        public string            lastAttendanceDate;
        public ExpeditionState[] expeditions;
        public string[]          storyLog;
        public int               xp;
        public int               level;
        public SkillData[]       learnedSkills;
    }

    public IEnumerator UpdateCharacterState(CharacterData ch, Action onSuccess, Action<string> onError)
    {
        var body = new StateRequest
        {
            inventory          = ch.inventory      != null ? ch.inventory.ToArray()      : new InventoryItem[0],
            gold               = ch.gold,
            questProgress      = ch.questProgress         ?? new QuestProgress(),
            lastAttendanceDate = ch.lastAttendanceDate    ?? "",
            expeditions        = ch.expeditions    != null ? ch.expeditions.ToArray()    : new ExpeditionState[0],
            storyLog           = ch.storyLog       != null ? ch.storyLog.ToArray()       : new string[0],
            xp                 = ch.xp,
            level              = ch.level,
            learnedSkills      = ch.learnedSkills  != null ? ch.learnedSkills.ToArray()  : new SkillData[0],
        };
        string json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest(BASE_URL + "/characters/" + ch.id + "/state", "PATCH");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
        else onSuccess?.Invoke();
    }

    [Serializable]
    private class NovelResponse
    {
        public bool   success;
        public string data;
    }

    public IEnumerator GenerateNovel(CharacterData ch, Action<string> onSuccess, Action<string> onError)
    {
        using var req = new UnityWebRequest(BASE_URL + "/characters/" + ch.id + "/novel", "POST");
        req.uploadHandler   = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

        var res = JsonUtility.FromJson<NovelResponse>(req.downloadHandler.text);
        if (res.success) onSuccess?.Invoke(res.data);
        else onError?.Invoke("소설 생성 실패");
    }
}
