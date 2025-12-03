using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using Google;

public class FirebaseAuthSystem : MonoBehaviour
{
    [Header("Google Settings")]
    public string WebClientID ="";

    private GoogleSignInConfiguration googleConfig;
    private bool googleInit = false;

    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("UI Panels")]
    public GameObject LoginPanel;
    public GameObject UserPanel;
    public GameObject LoadingSpinner;

    [Header("Profile UI")]
    public TextMeshProUGUI Username;
    public TextMeshProUGUI UserEmail;
    public Image ProfileImage;
    public Sprite DefaultProfilePic;

    [Header("Message")]
    public TextMeshProUGUI MessageText;

    void Start()
    {
        InitFirebase();
        InitUI();
        TrySilentLogin();
    }

    void InitFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    void InitGoogle()
    {
        if (googleInit) return;

        googleConfig = new GoogleSignInConfiguration
        {
            WebClientId = WebClientID,
            RequestEmail = true,
            RequestIdToken = true
        };

        GoogleSignIn.Configuration = googleConfig;
        googleInit = true;
    }

    public void GoogleLogin()
    {
        InitGoogle();
        ShowLoading(true);

        GoogleSignIn.DefaultInstance.SignIn()
            .ContinueWithOnMainThread(task => HandleGoogleLogin(task));
    }

    public void TrySilentLogin()
    {
        InitGoogle();

        GoogleSignIn.DefaultInstance.SignInSilently()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                    return;

                HandleGoogleLogin(task);
            });
    }

    void HandleGoogleLogin(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            ShowError("Google login failed.");
            ShowLoading(false);
            return;
        }

        GoogleSignInUser gUser = task.Result;
        Credential credential = GoogleAuthProvider.GetCredential(gUser.IdToken, null);

        auth.SignInWithCredentialAsync(credential)
            .ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsFaulted)
                {
                    ShowError("Firebase login failed.");
                    ShowLoading(false);
                    return;
                }

                user = auth.CurrentUser;
                LoadUserData();
            });
    }

    void LoadUserData()
    {
        Username.text = user.DisplayName;
        UserEmail.text = user.Email;

        if (user.PhotoUrl != null)
            StartCoroutine(LoadImage(user.PhotoUrl.ToString()));
        else
            ProfileImage.sprite = DefaultProfilePic;

        LoginPanel.SetActive(false);
        UserPanel.SetActive(true);

        ShowLoading(false);
    }

    IEnumerator LoadImage(string url)
    {
        UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            ProfileImage.sprite =
                Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
        }
        else
        {
            ProfileImage.sprite = DefaultProfilePic;
        }
    }

    public void Logout()
    {
        GoogleSignIn.DefaultInstance.SignOut();
        auth.SignOut();
        InitUI();
    }

    void InitUI()
    {
        LoginPanel.SetActive(true);
        UserPanel.SetActive(false);

        Username.text = "";
        UserEmail.text = "";
        ProfileImage.sprite = DefaultProfilePic;
    }

    void ShowLoading(bool state)
    {
        if (LoadingSpinner)
            LoadingSpinner.SetActive(state);
    }

    void ShowError(string msg)
    {
        if (MessageText)
            MessageText.text = msg;
    }
}
