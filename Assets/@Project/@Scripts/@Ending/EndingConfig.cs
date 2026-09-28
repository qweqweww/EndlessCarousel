using System.Collections.Generic;
using UnityEngine;

public enum CharacterOutcome { Park, Reality }
public enum XOutcome { Stay, Search }
public enum EndingGroup { AllReturn, Mixed, AllStay }

// 엔딩 판정 조건. 캐릭터와 플래그 이름은 모두 이 에셋에서 입력한다.
[CreateAssetMenu(fileName = "EndingConfig", menuName = "EndlessCarousel/Ending Config")]
public class EndingConfig : ScriptableObject
{
    [Tooltip("현실 귀환 여부를 판정할 캐릭터들")]
    [SerializeField] private List<CharacterEndingRule> characters = new List<CharacterEndingRule>();

    [Header("결과 문구")]
    [SerializeField] private string realityLabel = "현실";
    [SerializeField] private string parkLabel = "놀이공원";

    [Header("X의 마지막 선택")]
    [SerializeField] private string xDisplayName = "X";

    [Tooltip("판정 전에 재생할 대화. 선택지의 Choice Id + Record As Flag로 아래 Search Flag를 기록한다. 비워두면 바로 판정")]
    [SerializeField] private DialogueData xFinalChoiceDialogue;

    [Tooltip("이 플래그가 켜져 있으면 Search, 없으면 Stay")]
    [SerializeField] private string xSearchFlag = "X_Search";

    [SerializeField] private string xStayLabel = "놀이공원에 남는다";
    [SerializeField] private string xSearchLabel = "다른 가능성을 찾는다";

    public IReadOnlyList<CharacterEndingRule> Characters => characters;
    public DialogueData XFinalChoiceDialogue => xFinalChoiceDialogue;
    public string XDisplayName => xDisplayName;
    public string XSearchFlag => xSearchFlag;

    public string GetLabel(CharacterOutcome outcome) => outcome == CharacterOutcome.Reality ? realityLabel : parkLabel;
    public string GetLabel(XOutcome outcome) => outcome == XOutcome.Search ? xSearchLabel : xStayLabel;

    // 현재 GameFlags를 읽어 결과를 만든다. X의 선택은 캐릭터 판정에 영향을 주지 않는다.
    public EndingResult Evaluate()
    {
        var result = new EndingResult();
        int realityCount = 0;

        foreach (CharacterEndingRule rule in characters)
        {
            CharacterOutcome outcome = rule.IsRealityMet() ? CharacterOutcome.Reality : CharacterOutcome.Park;
            if (outcome == CharacterOutcome.Reality)
                realityCount++;
            result.Characters.Add(new CharacterResult(rule.characterId, rule.displayName, outcome));
        }

        result.X = GameFlags.Has(xSearchFlag) ? XOutcome.Search : XOutcome.Stay;

        if (characters.Count > 0 && realityCount == characters.Count)
            result.Group = EndingGroup.AllReturn;
        else if (realityCount == 0)
            result.Group = EndingGroup.AllStay;
        else
            result.Group = EndingGroup.Mixed;

        return result;
    }
}

[System.Serializable]
public class CharacterEndingRule
{
    [Tooltip("코드/데이터에서 캐릭터를 구분하는 ID (예: A)")]
    public string characterId;

    [Tooltip("결과 화면에 표시할 이름")]
    public string displayName;

    [Tooltip("모두 켜져 있으면 Reality, 하나라도 없으면 Park. 비워두면 항상 Reality")]
    public List<string> requiredFlags = new List<string>();

    public bool IsRealityMet()
    {
        foreach (string flag in requiredFlags)
        {
            if (!GameFlags.Has(flag))
                return false;
        }
        return true;
    }
}

public class EndingResult
{
    public readonly List<CharacterResult> Characters = new List<CharacterResult>();
    public XOutcome X;
    public EndingGroup Group;

    public CharacterOutcome? GetOutcome(string characterId)
    {
        foreach (CharacterResult c in Characters)
        {
            if (c.CharacterId == characterId)
                return c.Outcome;
        }
        return null;
    }
}

public readonly struct CharacterResult
{
    public readonly string CharacterId;
    public readonly string DisplayName;
    public readonly CharacterOutcome Outcome;

    public CharacterResult(string characterId, string displayName, CharacterOutcome outcome)
    {
        CharacterId = characterId;
        DisplayName = displayName;
        Outcome = outcome;
    }
}
