
#if VCONTAINER_AVAILABLE

using UnityEngine;
using UnityEngine.UI;

namespace UIFeture.Core
{
    public class UIPopup : WindowBinder
    {
        [SerializeField] private Button _btnClose;
        [SerializeField] private Button _btnAlternativeClose;


        private void OnEnable()
        {
            _btnClose?.onClick.AddListener(OnCloseClicked);
            _btnAlternativeClose?.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _btnClose?.onClick.RemoveListener(OnCloseClicked);
            _btnAlternativeClose?.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            Close();
        }

        public override void Close()
        {
            base.Close();
        }

        public override void Open()
        {
            base.Open();
        }
    }
}
#endif
