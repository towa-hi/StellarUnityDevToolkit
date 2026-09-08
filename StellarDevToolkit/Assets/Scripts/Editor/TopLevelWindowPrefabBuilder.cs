using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Regenerates the Main/Market/Game window prefabs as variants of Window.prefab and
// re-instantiates them under "New Network UI" next to Connection Window.
public static class TopLevelWindowPrefabBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string WindowPath = "Assets/UI Prefabs/Window.prefab";
    const string ButtonPath = "Assets/UI Prefabs/Button Template.prefab";
    const string VlgTemplatePath = "Assets/UI Prefabs/VLG Template.prefab";
    const string VlgEntryPath = "Assets/UI Prefabs/VLG Entry.prefab";
    const string BodyTextPath = "Assets/UI Prefabs/Body Text.prefab";
    const string AssetCardPath = "Assets/UI Prefabs/AssetCard.prefab";
    const string MainWindowPath = "Assets/UI Prefabs/Main Window.prefab";
    const string MarketWindowPath = "Assets/UI Prefabs/Market Window.prefab";
    const string GameWindowPath = "Assets/UI Prefabs/Game Window.prefab";

    static GameObject windowPrefab;
    static GameObject buttonPrefab;
    static GameObject vlgTemplatePrefab;
    static GameObject vlgEntryPrefab;
    static GameObject bodyTextPrefab;
    static GameObject assetCardPrefab;

    [MenuItem("Tools/Stellar/Build Top Level Window Prefabs")]
    public static void BuildAndPlace()
    {
        windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath);
        buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
        vlgTemplatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VlgTemplatePath);
        vlgEntryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VlgEntryPath);
        bodyTextPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BodyTextPath);
        assetCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetCardPath);
        if (windowPrefab == null || buttonPrefab == null || vlgTemplatePrefab == null
            || vlgEntryPrefab == null || bodyTextPrefab == null || assetCardPrefab == null)
        {
            Debug.LogError("TopLevelWindowPrefabBuilder: missing one or more UI Prefabs templates.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError($"TopLevelWindowPrefabBuilder: expected {ScenePath} to be the active scene, found '{scene.path}'.");
            return;
        }

        GameObject mainPrefab = SavePrefab(BuildMainWindow(), MainWindowPath);
        GameObject marketPrefab = SavePrefab(BuildMarketWindow(), MarketWindowPath);
        GameObject gamePrefab = SavePrefab(BuildGameWindow(), GameWindowPath);

        Transform parent = FindInScene(scene, "New Network UI");
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        ConnectionWindow connectionWindow = Object.FindFirstObjectByType<ConnectionWindow>(FindObjectsInactive.Include);
        if (parent == null || gameManager == null)
        {
            Debug.LogError("TopLevelWindowPrefabBuilder: 'New Network UI' or GameManager not found in the scene.");
            return;
        }

        DestroyExisting<MainWindow>();
        DestroyExisting<MarketWindow>();
        DestroyExisting<GameWindow>();

        GameObject main = PlaceInScene(mainPrefab, parent, connectionWindow);
        GameObject market = PlaceInScene(marketPrefab, parent, connectionWindow);
        GameObject game = PlaceInScene(gamePrefab, parent, connectionWindow);

        gameManager.connectionWindow = connectionWindow;
        gameManager.mainWindow = main.GetComponent<MainWindow>();
        gameManager.marketWindow = market.GetComponent<MarketWindow>();
        gameManager.gameWindow = game.GetComponent<GameWindow>();
        EditorUtility.SetDirty(gameManager);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("TopLevelWindowPrefabBuilder: built Main/Market/Game window prefabs and wired them to GameManager.");
    }

    static GameObject SavePrefab(GameObject source, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        return prefab;
    }

    static GameObject PlaceInScene(GameObject prefab, Transform parent, ConnectionWindow connectionWindow)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        if (connectionWindow != null)
        {
            RectTransform source = connectionWindow.GetComponent<RectTransform>();
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition = source.anchoredPosition;
        }

        instance.SetActive(false);
        return instance;
    }

    static void DestroyExisting<T>() where T : MonoBehaviour
    {
        T existing = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
    }

    static GameObject BuildMainWindow()
    {
        GameObject window = CreateShell("Main Window", "MAIN");
        Transform stack = CreateBodyStack(window);
        MainWindow mainWindow = window.AddComponent<MainWindow>();
        mainWindow.startGameButton = CreateButton(stack, "Button Start Game", "START GAME");
        mainWindow.runTestsButton = CreateButton(stack, "Button Run Tests", "RUN TESTS");
        mainWindow.marketButton = CreateButton(stack, "Button Market", "MARKET");
        AddSpacer(stack);
        mainWindow.backToConnectionButton = CreateButton(stack, "Button Back", "BACK TO CONNECTION");
        return window;
    }

    static GameObject BuildMarketWindow()
    {
        GameObject window = CreateShell("Market Window", "MARKET");
        Transform stack = CreateBodyStack(window);
        MarketWindow marketWindow = window.AddComponent<MarketWindow>();

        TextMeshProUGUI addressText = CreateEntry(stack, "VLG Entry Asset Address", "ASSET ADDRESS");
        addressText.enableAutoSizing = true;
        addressText.fontSizeMin = 8f;
        addressText.fontSizeMax = 14f;
        addressText.overflowMode = TextOverflowModes.Ellipsis;
        addressText.text = string.Empty;

        GameObject statusObject = (GameObject)PrefabUtility.InstantiatePrefab(bodyTextPrefab, stack);
        statusObject.name = "Status Text";
        AddLayoutElement(statusObject, 20f, -1f);
        TextMeshProUGUI statusText = statusObject.GetComponent<TextMeshProUGUI>();
        statusText.text = string.Empty;
        statusText.fontSize = 14f;

        // The grid reports its full height to the parent stack, so it has to live inside a
        // clipped scroll view or a long list pushes the Back button out of the window.
        GameObject assetList = new GameObject("Asset List", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        assetList.layer = 5;
        assetList.transform.SetParent(stack, false);
        AddLayoutElement(assetList, -1f, 1f);
        Image listBackground = assetList.GetComponent<Image>();
        listBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        listBackground.type = Image.Type.Sliced;
        listBackground.color = new Color(1f, 1f, 1f, 0.392f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewport.layer = 5;
        viewport.transform.SetParent(assetList.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.layer = 5;
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150f, 170f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Scrollbar scrollbar = CreateVerticalScrollbar(assetList.transform);

        ScrollRect scrollRect = assetList.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarSpacing = -3f;

        marketWindow.backButton = CreateButton(stack, "Button Back", "BACK");
        marketWindow.assetAddressText = addressText;
        marketWindow.statusText = statusText;
        marketWindow.root = content;
        marketWindow.assetCardPrefab = assetCardPrefab;
        return window;
    }

    static GameObject BuildGameWindow()
    {
        GameObject window = CreateShell("Game Window", "GAME");
        window.AddComponent<GameWindow>();
        Transform stack = CreateBodyStack(window);
        GameObject placeholder = (GameObject)PrefabUtility.InstantiatePrefab(bodyTextPrefab, stack);
        placeholder.name = "Placeholder Text";
        AddLayoutElement(placeholder, 24f, -1f);
        placeholder.GetComponent<TextMeshProUGUI>().text = "In-game UI goes here.";
        AddSpacer(stack);
        return window;
    }

    static GameObject CreateShell(string name, string header)
    {
        GameObject window = (GameObject)PrefabUtility.InstantiatePrefab(windowPrefab);
        window.name = name;
        RectTransform rect = window.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, 600f);
        rect.anchoredPosition = Vector2.zero;

        Transform headerText = window.transform.Find("VLG/Header/Header Text");
        if (headerText != null)
        {
            headerText.GetComponent<TextMeshProUGUI>().text = header;
        }

        return window;
    }

    static Transform CreateBodyStack(GameObject window)
    {
        Transform body = window.transform.Find("VLG/Body");
        GameObject stack = (GameObject)PrefabUtility.InstantiatePrefab(vlgTemplatePrefab, body);
        stack.name = "VLG Template";
        Stretch(stack.GetComponent<RectTransform>());
        VerticalLayoutGroup layout = stack.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        return stack.transform;
    }

    static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject button = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent);
        button.name = name;
        AddLayoutElement(button, 40f, -1f);
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = label;
        }

        return button.GetComponent<Button>();
    }

    // VLG Entry is a label/value row: "HLG > Panel (label text), Panel (1) (value slot)".
    static TextMeshProUGUI CreateEntry(Transform parent, string name, string label)
    {
        GameObject entry = (GameObject)PrefabUtility.InstantiatePrefab(vlgEntryPrefab, parent);
        entry.name = name;
        TextMeshProUGUI labelText = entry.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }

        Transform hlg = entry.transform.Find("HLG");
        Transform valuePanel = hlg != null && hlg.childCount > 1 ? hlg.GetChild(1) : entry.transform;
        GameObject valueObject = (GameObject)PrefabUtility.InstantiatePrefab(bodyTextPrefab, valuePanel);
        valueObject.name = "Value Text";
        Stretch(valueObject.GetComponent<RectTransform>());
        return valueObject.GetComponent<TextMeshProUGUI>();
    }

    static void AddSpacer(Transform parent)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.layer = 5;
        spacer.transform.SetParent(parent, false);
        AddLayoutElement(spacer, -1f, 1f);
    }

    static void AddLayoutElement(GameObject target, float preferredHeight, float flexibleHeight)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = flexibleHeight;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Scrollbar CreateVerticalScrollbar(Transform parent)
    {
        Sprite background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject scrollbarObject = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarObject.layer = 5;
        scrollbarObject.transform.SetParent(parent, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = Vector2.one;
        scrollbarRect.pivot = Vector2.one;
        scrollbarRect.sizeDelta = new Vector2(20f, 0f);
        scrollbarRect.anchoredPosition = Vector2.zero;

        Image track = scrollbarObject.GetComponent<Image>();
        track.sprite = background;
        track.type = Image.Type.Sliced;
        track.color = Color.white;

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.layer = 5;
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.offsetMin = new Vector2(10f, 10f);
        slidingRect.offsetMax = new Vector2(-10f, -10f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.layer = 5;
        handle.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = new Vector2(20f, 20f);
        handleRect.anchoredPosition = Vector2.zero;
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = knob;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = Color.white;

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        scrollbar.size = 0.2f;
        scrollbar.numberOfSteps = 0;
        return scrollbar;
    }

    static Transform FindInScene(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name == name)
                {
                    return children[j];
                }
            }
        }

        return null;
    }
}
