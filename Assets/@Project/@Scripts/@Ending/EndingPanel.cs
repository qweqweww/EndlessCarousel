using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 엔딩 결과 화면 (현재는 테스트용 텍스트 표시). 최종 디자인은 Prefab을 교체한다.
public class EndingPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button closeButton;

    [Tooltip("{0}=이름, {1}=결과")]
    [SerializeField] private string lineFormat = "{0} : {1}";

    [Tooltip("{0}=AllReturn / Mixed / AllStay")]
    [SerializeField] private string groupFormat = "Result : {0}";

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(EndingResult result, EndingConfig config)
    {
        var sb = new StringBuilder();
        foreach (CharacterResult c in result.Characters)
            sb.AppendLine(string.Format(lineFormat, c.DisplayName, config.GetLabel(c.Outcome)));
        sb.AppendLine(string.Format(lineFormat, config.XDisplayName, config.GetLabel(result.X)));
        sb.AppendLine();
        sb.Append(string.Format(groupFormat, result.Group));

        resultText.text = sb.ToString();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
