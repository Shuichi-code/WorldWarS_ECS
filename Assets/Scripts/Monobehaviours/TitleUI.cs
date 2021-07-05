using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Assets.Scripts.Monobehaviours
{
    public class TitleUI : VisualElement
    {
        [SerializeField] private GameObject titleUI;
        VisualElement m_TitleScreen;
        VisualElement m_OptionsScreen;
        VisualElement m_AboutScreen;

        string m_SceneName = "Main";

        public new class UxmlFactory : UxmlFactory<TitleUI, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_StartScene = new UxmlStringAttributeDescription { name = "start-scene", defaultValue = "Main" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var sceneName = m_StartScene.GetValueFromBag(bag, cc);

                ((TitleUI)ve).Init(sceneName);
            }
        }

        public TitleUI()
        {
            this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
        }

        void OnGeometryChange(GeometryChangedEvent evt)
        {
            m_TitleScreen = this.Q("Title");
            m_OptionsScreen = this.Q("OptionsScreen");
            m_AboutScreen = this.Q("AboutScreen");
            //m_AboutScreenOperator = m_AboutScreen.Q<AboutScreenOperator>();

            m_TitleScreen?.Q("startBtn")?.RegisterCallback<ClickEvent>(ev => StartGame());
            m_TitleScreen?.Q("optionBtn")?.RegisterCallback<ClickEvent>(ev => EnableOptionsScreen());
            m_TitleScreen?.Q("exitBtn")?.RegisterCallback<ClickEvent>(ev => EnableAboutScreen());

            m_OptionsScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());
            m_AboutScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());

            this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
        }

        void EnableTitleScreen()
        {
            m_TitleScreen.style.display = DisplayStyle.Flex;
            m_OptionsScreen.style.display = DisplayStyle.None;
            m_AboutScreen.style.display = DisplayStyle.None;
        }

        void EnableOptionsScreen()
        {
            m_TitleScreen.style.display = DisplayStyle.None;
            m_OptionsScreen.style.display = DisplayStyle.Flex;
            m_AboutScreen.style.display = DisplayStyle.None;
        }

        void EnableAboutScreen()
        {
            m_TitleScreen.style.display = DisplayStyle.None;
            m_OptionsScreen.style.display = DisplayStyle.None;
            m_AboutScreen.style.display = DisplayStyle.Flex;
            //m_AboutScreenOperator.Animate();
        }

        void StartGame()
        {
            //Debug.Log("Pressed the start button!");
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                SceneManager.LoadSceneAsync(m_SceneName);
#if UNITY_EDITOR
            else
                Debug.Log("Loading: " + m_SceneName);
#endif
        }

        void Init(string sceneName)
        {
            m_SceneName = sceneName;
        }
    }
}