using UnityEngine;
using UnityEngine.UI;

//Single chat message UI element
public class ChatMessage : MonoBehaviour
{
    [SerializeField] private Color _nameColor;
    [SerializeField] private Color _ownNameColor;
    [SerializeField] private Color _messageColor;
    [SerializeField] private Color _ownMessageColor;

    [SerializeField] private Text _name;
    [SerializeField] private Text _message;

    public void Initialize(string nameText, string message, bool isOwn)
    {
        _name.text = nameText;
        _name.color = isOwn ? _ownNameColor : _nameColor;
        _message.text = message;
        _message.color = isOwn ? _ownMessageColor : _messageColor;
    }
}