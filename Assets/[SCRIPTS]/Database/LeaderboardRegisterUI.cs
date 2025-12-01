using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class LeaderboardRegisterUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField;       // Campo donde el jugador escribe su nombre
    public TextMeshProUGUI feedbackText;        // Texto para mostrar mensajes al jugador

    [Header("Server Config")]
    [Tooltip("URL del script en tu servidor que registra al usuario")]
    public string registerUrl = "https://mydomain.com/api/register_user.php";

    // Llamar este método desde el botón "Registrar"
    public void OnClickRegister()
    {
        string playerName = nameInputField.text.Trim();

        // Validaciones básicas
        if (string.IsNullOrEmpty(playerName))
        {
            feedbackText.text = "Por favor escribe un nombre.";
            return;
        }

        if (playerName.Length > 20)
        {
            feedbackText.text = "El nombre debe tener máximo 20 caracteres.";
            return;
        }

        // Lanzar la corrutina para enviar al servidor
        StartCoroutine(RegisterUserCoroutine(playerName));
    }

    private IEnumerator RegisterUserCoroutine(string playerName)
    {
        feedbackText.text = "Registrando...";

        // Preparar el formulario POST
        WWWForm form = new WWWForm();
        form.AddField("nombre", playerName);

        using (UnityWebRequest www = UnityWebRequest.Post(registerUrl, form))
        {
            // Enviar petición
            yield return www.SendWebRequest();

            // Revisar errores de red/HTTP
            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error al registrar usuario: " + www.error);
                feedbackText.text = "Error de conexión.";
            }
            else
            {
                // Leer respuesta del servidor (asumiremos JSON)
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Respuesta servidor: " + jsonResponse);

                // Intentar parsear como JSON simple
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(jsonResponse);

                if (response != null && response.success)
                {
                    feedbackText.text = "Registro exitoso. ¡Bienvenida/o, " + playerName + "!";
                    // Aquí podrías guardar el usuario_id para usarlo después:
                    int usuarioId = response.usuario_id;
                    
                    yield return new WaitForSeconds(2.5f);
                    
                    if (UIController.Instance != null)
                    {
                        // Esto hace lo que antes hacía el botón Play:
                        UIController.Instance.OnPlayFromMainMenu();
                    }
                }
                else
                {
                    // Si el PHP manda success=false o la respuesta no es correcta
                    feedbackText.text = string.IsNullOrEmpty(response?.message)
                        ? "No se pudo registrar."
                        : response.message;
                }
            }
        }
    }

    [System.Serializable]
    private class ServerResponse
    {
        public bool success;
        public int usuario_id;
        public string message;
    }
}

