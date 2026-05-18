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

    [Serializable]
    private class ApiResponse
    {
        public bool success;
        public CharacterData data;
        public ErrorData error;
    }

    [Serializable]
    private class ErrorData
    {
        public string code;
        public string message;
    }

    public IEnumerator CreateCharacter(
        string appearance, string weapon, string concept, string worldview,
        Action<CharacterData> onSuccess, Action<string> onError)
    {
        var body = new CreateRequest
        {
            appearance = appearance,
            weapon = weapon,
            concept = concept,
            worldview = worldview,
        };

        string json = JsonUtility.ToJson(body);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(BASE_URL + "/characters", "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        var response = JsonUtility.FromJson<ApiResponse>(req.downloadHandler.text);
        if (response.success)
            onSuccess?.Invoke(response.data);
        else
            onError?.Invoke(response.error?.message ?? "알 수 없는 오류");
    }
}
