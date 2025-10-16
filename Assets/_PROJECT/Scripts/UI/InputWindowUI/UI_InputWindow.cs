using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_InputWindow : MonoBehaviour
{
    private static UI_InputWindow instance;

    [SerializeField] private Button okBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_InputField inputField;

    private void Awake()
    {
        instance = this;

        Hide();
    }

    private void Show(string titleString, string inputString, string validCharacters, int characterLimit,
        Action onCancel, Action<string> onOk)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        titleText.text = titleString;

        inputField.characterLimit = characterLimit;
        inputField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            return ValidateChar(validCharacters, addedChar);
        };

        inputField.text = inputString;
        inputField.Select();

        okBtn.onClick.AddListener(() => {
            Hide();
            onOk(inputField.text);
        });
        
        cancelBtn.onClick.AddListener(() => {
            Hide();
            onCancel();
        });
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private char ValidateChar(string validCharacters, char addedChar)
    {
        if (validCharacters.IndexOf(addedChar) != -1)
        {
            // Valid
            return addedChar;
        }
        else
        {
            // Invalid
            return '\0';
        }
    }

    public static void Show_Static(string titleString, string inputString, string validCharacters, int characterLimit,
        Action onCancel, Action<string> onOk)
    {
        instance.Show(titleString, inputString, validCharacters, characterLimit, onCancel, onOk);
    }

    public static void Show_Static(string titleString, int defaultInt, Action onCancel, Action<int> onOk)
    {
        instance.Show(titleString, defaultInt.ToString(), "0123456789-", 20, onCancel,
            (string inputText) =>
            {
                // Try to Parse input string
                if (int.TryParse(inputText, out int _i))
                {
                    onOk(_i);
                }
                else
                {
                    onOk(defaultInt);
                }
            }
        );
    }
}