using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RaceResult
{
    public string trackName;
    public float time;
    public int score;
    public string date;
}

[Serializable]
public class RaceResultList
{
    public List<RaceResult> results = new List<RaceResult>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private string filePath;
    [Header("Server Config")]
    [SerializeField] private string saveScoreUrl = "https://fictionsearch.net/api/save_score.php";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            filePath = Path.Combine(Application.persistentDataPath, "scores.json");
        }
        else
        {
            Destroy(gameObject);
        }
        
        DontDestroyOnLoad(gameObject);

    }

    public void SaveResult(string trackName, float time, int score)
    {
        var list = LoadAllResultsInternal();
        var result = new RaceResult
        {
            trackName = trackName,
            time = time,
            score = score,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        list.results.Add(result);
        try
        {
            string json = JsonUtility.ToJson(list, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Saved result to {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save results: " + e);
        }
        
        //DB Stuff
        int usuarioId = PlayerPrefs.GetInt("UsuarioID", 0);
        if (usuarioId != 0)
        {
            StartCoroutine(SendResultToServer(usuarioId, result));
        }
        else
        {
            Debug.LogWarning("No se encontró UsuarioID en PlayerPrefs. No se envió score a la BD.");
        }
    }

    public List<RaceResult> LoadAllResults()
    {
        return LoadAllResultsInternal().results;
    }

    private RaceResultList LoadAllResultsInternal()
    {
        if (!File.Exists(filePath))
        {
            return new RaceResultList();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(json))
                return new RaceResultList();

            return JsonUtility.FromJson<RaceResultList>(json) ?? new RaceResultList();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load results: " + e);
            return new RaceResultList();
        }
    }

    public void ClearAllResults()
    {
        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            Debug.Log("Cleared saved scores.");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to clear results: " + e);
        }
    }
    private System.Collections.IEnumerator SendResultToServer(int usuarioId, RaceResult result)
    {
        if (string.IsNullOrEmpty(saveScoreUrl))
        {
            Debug.LogError("SaveManager: saveScoreUrl no está configurada.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("usuario_id", usuarioId);
        form.AddField("track_name", result.trackName);
        form.AddField("time", result.time.ToString(System.Globalization.CultureInfo.InvariantCulture));
        form.AddField("score", result.score);
        form.AddField("date", result.date);

        using (UnityWebRequest www = UnityWebRequest.Post(saveScoreUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error enviando score a la BD: " + www.error);
            }
            else
            {
                Debug.Log("Score enviado a la BD. Respuesta: " + www.downloadHandler.text);
            }
        }
    }

}
