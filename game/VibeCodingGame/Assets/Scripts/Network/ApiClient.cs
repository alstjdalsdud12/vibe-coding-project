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
        public string appearance;
        public string weapon;
        public string concept;
        public string worldview;
    }

    public IEnumerator CreateCharacter(
        string appearance, string weapon, string concept, string worldview,
        Action<CharacterData> onSuccess, Action<string> onError)
    {
        var body = new CreateRequest { appearance = appearance, weapon = weapon, concept = concept, worldview = worldview };
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
}
