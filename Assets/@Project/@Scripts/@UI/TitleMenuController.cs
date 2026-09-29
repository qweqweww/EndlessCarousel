using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 화면의 버튼 동작을 한곳에서 관리한다. 버튼 디자인과 배경은 씬/Prefab에서 교체한다.
public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Tooltip("시작하기를 눌렀을 때 열 씬 이름 (Build Settings에 등록되어 있어야 한다)")]
    [SerializeField] private string gameSceneName = "Main";

    private void Awake()
    {
        startButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(QuitGame);
    }

    public void StartGame()
    {
        // 씬이 로드되는 동안 중복 클릭 방지
        startButton.interactable = false;
        quitButton.interactable = false;
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[TitleMenu] 게임 종료 (에디터에서는 종료되지 않습니다)");
#else
        Application.Quit();
#endif
    }
}
