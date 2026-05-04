using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.Graphics.Builder;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        const float k_SliderMidpoint = 50f;
        const float k_SliderMax = 100f;
        const float k_SliderMin = 0f;
        const float k_MinHeightRatio = 0.2f;
        const float k_MaxHeightRatio = 0.8f;
        const float k_MaxZoom = 100f;
        const float k_ZoomDivisor = 2f;
        const int k_FrequentUpdateMs = 10;
        const int k_ImageUpdateMs = 250;
        const int k_ImageInvalidateMs = 2000;
        const int k_ScrollDelayMs = 25;

        VisualElement m_ImageGroup;
        Image m_RefImageContainer;
        Image m_ActImageContainer;
        Image m_DiffImageContainer;
        Image m_HeatmapContainer;

        Button m_AcceptResultButton;
        Button m_ClearResultButton;

        Button m_QuickSwitchButton;
        Button m_ResetViewButton;
        Button m_PrevVariantButton;
        Button m_NextVariantButton;
        Label m_VariantLabel;
        VisualElement m_VariantNavigator;

        Toggle m_OverlayDiffToggle;
        Toggle m_OverlayHeatmapToggle;

        Slider m_ComparisonSlider;
        Slider m_ZoomSlider;
        VisualElement m_MaskLeft;
        VisualElement m_MaskRight;

        GroupBox m_ReferenceImageBar;
        GroupBox m_ActualImageBar;
        Label m_ReferenceImageLabel;
        Label m_ActualImageLabel;
        VisualElement m_ImagePanel;
        VisualElement m_ImageToolbar;
        ToolbarBreadcrumbs m_ReferenceImageBreadcrumbs;
        ToolbarBreadcrumbs m_ActualImageBreadcrumbs;
        ToolbarMenu m_ToolbarMenu;
        bool m_Resizing;
        float m_StartMousePosition;
        float m_StartSize;
        Texture2D m_ReferenceImage;
        Texture2D m_ActualImage;
        Texture2D m_DiffImage;
        Texture2D m_HeatmapImage;
        int m_CurrentVariantIndex;
        List<ReferenceImage> m_CachedVariants = new();

        Label m_NoReferenceImageLabel;
        Label m_NoActualImageLabel;

        Image m_NoRefImageImage;
        Image m_NoActImageImage;

        bool m_HasGeneratedImageMetrics;
        List<int> m_PreviousSelectedIds = new();

        TabView m_ComparisonTabView;
        Button m_NewComparisonButton;
        int m_TabCount;
        Dictionary<string, ImageComparisonTab> m_ImageComparisonTabs = new();

        void SetupImageComparisonView()
        {
            m_ImageToolbar = m_Root.Q<VisualElement>("ImageToolbar");

            m_QuickSwitchButton = m_Root.Q<Button>("QuickSwitchButton");
            m_PrevVariantButton = m_Root.Q<Button>("PrevVariantButton");
            m_NextVariantButton = m_Root.Q<Button>("NextVariantButton");
            m_VariantLabel = m_Root.Q<Label>("VariantLabel");
            m_VariantNavigator = m_Root.Q<VisualElement>("VariantNavigator");

            m_OverlayDiffToggle = m_Root.Q<Toggle>("OverlayDiffToggle");
            m_OverlayHeatmapToggle = m_Root.Q<Toggle>("OverlayHeatmapToggle");

            m_AcceptResultButton = m_Root.Q<Button>("AcceptResultButton");
            m_ClearResultButton = m_Root.Q<Button>("ClearResultButton");

            m_ResetViewButton = m_Root.Q<Button>("ResetViewButton");

            m_ToolbarMenu = m_Root.Q<ToolbarMenu>("ImageMenu");
            m_ImagePanel = m_Root.Q<VisualElement>("ImagePanel");
            m_ImageGroup = m_Root.Q<VisualElement>("ImageGroup");
            m_RefImageContainer = m_Root.Q<Image>("ReferenceImage");
            m_ActImageContainer = m_Root.Q<Image>("ActualImage");
            m_DiffImageContainer = m_Root.Q<Image>("DiffImage");
            m_HeatmapContainer = m_Root.Q<Image>("HeatmapImage");
            m_ComparisonSlider = m_Root.Q<Slider>("RevealSlider");
            m_ZoomSlider = m_Root.Q<Slider>("ZoomSlider");

            m_MaskLeft = m_Root.Q<VisualElement>("MaskLeft");
            m_MaskRight = m_Root.Q<VisualElement>("MaskRight");

            m_ReferenceImageBar = m_Root.Q<GroupBox>("ReferenceImageBar");
            m_ActualImageBar = m_Root.Q<GroupBox>("ActualImageBar");
            m_ReferenceImageLabel = m_Root.Q<Label>("ReferenceImageLabel");
            m_ActualImageLabel = m_Root.Q<Label>("ActualImageLabel");

            m_NoActualImageLabel = m_Root.Q<Label>("NoActualImageLabel");
            m_NoReferenceImageLabel = m_Root.Q<Label>("NoReferenceImageLabel");

            m_ComparisonTabView = m_Root.Q<TabView>("ComparisonTabView");
            m_NewComparisonButton = m_Root.Q<Button>("NewComparisonButton");

            var noImageTex = AssetService.LoadAssetAtPath<Texture2D>(
                "Packages/com.unity.testframework.graphics/Editor/UI/Assets/gtf-unity-chan-question.png"
            );
            m_NoRefImageImage = m_Root.Q<Image>("NoRefImageImage");
            m_NoRefImageImage.image = noImageTex;

            m_NoActImageImage = m_Root.Q<Image>("NoActualImageImage");
            m_NoActImageImage.image = noImageTex;

            if (!EditorGUIUtility.isProSkin)
            {
                m_MaskLeft.style.backgroundColor = new Color(0.7843137f, 0.7843137f, 0.7843137f);
                m_MaskRight.style.backgroundColor = new Color(0.7843137f, 0.7843137f, 0.7843137f);
                m_Root.Q<Label>("ReferenceImageLabel").style.color = new Color(0.7843137f, 0.32f, 0.7843137f);
                m_Root.Q<Label>("ActualImageLabel").style.color = new Color(0.32f, 0.55f, 0.7843137f);
            }

            m_NewComparisonButton.clickable.clicked += () =>
            {
                PopupWindow.Show(m_NewComparisonButton.worldBound, new GraphicsTestNewComparisonWindow());
            };

            m_QuickSwitchButton.clickable.clicked += () =>
            {
                m_ComparisonSlider.value = m_ComparisonSlider.value >= k_SliderMidpoint ? k_SliderMin : k_SliderMax;
            };

            m_PrevVariantButton.clickable.clicked += () =>
            {
                if (m_CurrentVariantIndex > 0)
                {
                    m_CurrentVariantIndex--;
                    m_ShouldUpdateImageComparisonView = true;
                    UpdateImageComparisonView();
                }
            };

            m_NextVariantButton.clickable.clicked += () =>
            {
                if (m_CurrentVariantIndex < m_CachedVariants.Count - 1)
                {
                    m_CurrentVariantIndex++;
                    m_ShouldUpdateImageComparisonView = true;
                    UpdateImageComparisonView();
                }
            };

            m_AcceptResultButton.clickable.clicked += UpdateReference;
            m_ClearResultButton.clickable.clicked += DeleteActualImage;
            m_ResetViewButton.clickable.clicked += ResetView;

            m_OverlayDiffToggle.RegisterValueChangedCallback(evt =>
            {
                m_DiffImageContainer.visible = evt.newValue;
            });

            m_OverlayHeatmapToggle.RegisterValueChangedCallback(evt =>
            {
                m_HeatmapContainer.visible = evt.newValue;
            });

            m_ComparisonSlider.RegisterValueChangedCallback(evt =>
            {
                AdjustSlider(evt.newValue);
            });

            SetupResizeCallbacks();
            SetupZoomCallbacks();

            m_ImageGroup.AddManipulator(new GrabbableManipulator(m_ImageGroup));
            m_ImageGroup.AddToClassList("grabbable");

            m_ImagePanel.schedule.Execute(UpdateFrequent).Every(k_FrequentUpdateMs);
            m_ImagePanel.schedule.Execute(UpdateImageComparisonView).Every(k_ImageUpdateMs);
            m_ImagePanel.schedule.Execute(UpdateToolbarMenu).Every(k_ImageUpdateMs);
            m_ImagePanel.schedule.Execute(() => m_ShouldUpdateImageComparisonView = true).Every(k_ImageInvalidateMs);

            m_TreeView.selectedIndicesChanged += (_) =>
            {
                UpdateImageComparisonView();

                var idList = new List<int>(m_TreeView.selectedIds);
                if (idList.Count == 0)
                    return;

                bool selectionChanged = idList.Count != m_PreviousSelectedIds.Count;
                if (!selectionChanged)
                {
                    for (int i = 0; i < idList.Count; i++)
                    {
                        if (idList[i] != m_PreviousSelectedIds[i])
                        {
                            selectionChanged = true;
                            break;
                        }
                    }
                }

                m_PreviousSelectedIds = idList;

                if (!selectionChanged)
                    return;

                var lastId = idList[idList.Count - 1];
                m_TreeView
                    .schedule.Execute(() =>
                    {
                        m_TreeView.ScrollToItemById(lastId);
                        ResetView();
                    })
                    .ExecuteLater(k_ScrollDelayMs);
            };

            m_ImagePanel.SendToBack();
            m_ImageGroup.SendToBack();
            m_ImagePanel.style.overflow = Overflow.Hidden;
            m_ImageGroup.style.overflow = Overflow.Hidden;

            m_OverlayHeatmapToggle.RegisterValueChangedCallback((_) => m_ShouldUpdateImageComparisonView = true);

            var schemata = GraphicsTestBuildSettings.LoadOrDefault().PlatformSchemata;
            var schemaList = new List<PlatformSchema>(schemata);
            schemaList.Sort((a, b) => a.Types.Count.CompareTo(b.Types.Count));
            foreach (var schema in schemaList)
            {
                var platform = GraphicsTestPlatform.GetCurrent(schema);
                if (m_ImageComparisonTabs.ContainsKey(platform.Name))
                    continue;

                var tab = new Tab(string.IsNullOrWhiteSpace(platform.Name) ? "Base" : platform.Name)
                {
                    closeable = !string.IsNullOrWhiteSpace(platform.Name),
                    tabIndex = m_TabCount++,
                    name = platform.Name,
                };
                m_ImageComparisonTabs.Add(platform.Name, new ImageComparisonTab(platform));
                m_ComparisonTabView.Add(tab);
                tab.closed += tab1 =>
                {
                    m_ImageComparisonTabs.Remove(tab1.name);
                };
            }

            m_ComparisonTabView.activeTabChanged += (_, _) =>
            {
                m_ShouldUpdateImageComparisonView = true;
                UpdateSliderLabels();
                UpdateImageComparisonView();
            };

            GraphicsTestNewComparisonWindow.s_OnComparisonCreated += OnComparisonCreated;

            GraphicsTestNewComparisonWindow.s_OnAdhocComparisonCreated += OnAdhocComparisonCreated;

            GraphicsTestBuilder.OnTestBuilderFinished += OnTestBuilderFinished;
        }

        void SetupResizeCallbacks()
        {
            m_ImageToolbar.RegisterCallback<MouseDownEvent>(evt =>
            {
                m_Resizing = true;
                m_StartMousePosition = evt.mousePosition.y;
                m_StartSize = m_ImagePanel.resolvedStyle.height;
                evt.StopPropagation();
            });

            m_Root.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (m_Resizing)
                {
                    var delta = evt.mousePosition.y - m_StartMousePosition;
                    var minHeight = m_Root.resolvedStyle.height * k_MinHeightRatio;
                    var maxHeight = m_Root.resolvedStyle.height * k_MaxHeightRatio;
                    m_ImagePanel.style.minHeight = new StyleLength(
                        Mathf.Max(minHeight, Mathf.Min(maxHeight, m_StartSize - delta))
                    );
                    m_ImagePanel.style.maxHeight = new StyleLength(
                        Mathf.Min(maxHeight, Mathf.Max(minHeight, m_StartSize - delta))
                    );
                    evt.StopPropagation();
                }
            });

            m_Root.RegisterCallback<MouseUpEvent>(evt =>
            {
                m_Resizing = false;
                evt.StopPropagation();
            });
        }

        void SetupZoomCallbacks()
        {
            m_ZoomSlider.RegisterValueChangedCallback(evt =>
            {
                var t = (evt.newValue - m_ZoomSlider.lowValue) / (m_ZoomSlider.highValue - m_ZoomSlider.lowValue);
                var zoomAmount = Mathf.Lerp(1f, k_MaxZoom, Mathf.Pow(t, 2f));
                m_ImageGroup.style.scale = new StyleScale(new Vector2(zoomAmount, zoomAmount));

                m_ComparisonSlider.visible = Mathf.Approximately(zoomAmount, 1f);
                m_ZoomSlider.visible = !Mathf.Approximately(zoomAmount, 1f);
                m_TreeView.SetEnabled(Mathf.Approximately(zoomAmount, 1f));

                if (Mathf.Approximately(zoomAmount, 1f))
                    ResetImageGroupPosition();

                evt.StopPropagation();
            });

            m_ImageGroup.RegisterCallback<WheelEvent>(evt =>
            {
                var zoomBefore = m_ZoomSlider.value;
                var zoomDelta = -evt.delta.y / k_ZoomDivisor;
                var zoomAfter = Mathf.Clamp(zoomBefore + zoomDelta, k_SliderMin, k_SliderMax);

                var pointerPosition = evt.localMousePosition;
                var worldBefore = m_ImageGroup.LocalToWorld(pointerPosition);
                var imageWorldRect = GetWorldRect(m_ImageGroup);
                var parentWorldRect = GetWorldRect(m_ImagePanel);

                if (imageWorldRect.width == 0 || imageWorldRect.height == 0)
                    return;

                var visibleRect = Rect.MinMaxRect(
                    Mathf.Max(imageWorldRect.xMin, parentWorldRect.xMin),
                    Mathf.Max(imageWorldRect.yMin, parentWorldRect.yMin),
                    Mathf.Min(imageWorldRect.xMax, parentWorldRect.xMax),
                    Mathf.Min(imageWorldRect.yMax, parentWorldRect.yMax)
                );

                if (visibleRect.width <= 0 || visibleRect.height <= 0)
                    return;

                var xStartPercent = (visibleRect.xMin - imageWorldRect.xMin) / imageWorldRect.width * k_SliderMax;
                var xEndPercent = (visibleRect.xMax - imageWorldRect.xMin) / imageWorldRect.width * k_SliderMax;
                var yStartPercent = (visibleRect.yMin - imageWorldRect.yMin) / imageWorldRect.height * k_SliderMax;
                var yEndPercent = (visibleRect.yMax - imageWorldRect.yMin) / imageWorldRect.height * k_SliderMax;

                var pointerPercentX =
                    xStartPercent
                    + (pointerPosition.x / m_ImageGroup.resolvedStyle.width) * (xEndPercent - xStartPercent);
                var pointerPercentY =
                    yStartPercent
                    + (pointerPosition.y / m_ImageGroup.resolvedStyle.height) * (yEndPercent - yStartPercent);

                var newTransformOrigin = new TransformOrigin(
                    new Length(pointerPercentX, LengthUnit.Percent),
                    new Length(pointerPercentY, LengthUnit.Percent)
                );
                m_ImageGroup.style.transformOrigin = new StyleTransformOrigin(newTransformOrigin);

                m_ZoomSlider.value = zoomAfter;

                var worldAfter = m_ImageGroup.LocalToWorld(pointerPosition);
                var delta = worldBefore - worldAfter;

                var currentTranslate = m_ImageGroup.resolvedStyle.translate;
                var offset = new Vector2(currentTranslate.x, currentTranslate.y) + delta;
                m_ImageGroup.style.translate = new StyleTranslate(new Translate(offset.x, offset.y, 0));

                evt.StopPropagation();
            });
        }

        void OnComparisonCreated(ImageComparisonTab comparisonTab, string label)
        {
            if (m_ImageComparisonTabs.TryGetValue(label, out _))
            {
                Tab existing = null;
                foreach (var c in m_ComparisonTabView.Children())
                {
                    if (c.name == label)
                    {
                        existing = (Tab)c;
                        break;
                    }
                }
                if (existing != null)
                    m_ComparisonTabView.selectedTabIndex = existing.tabIndex;

                return;
            }

            var tab = new Tab(label)
            {
                closeable = true,
                tabIndex = m_TabCount++,
                name = label,
            };
            m_ImageComparisonTabs.Add(label, comparisonTab);
            m_ComparisonTabView.Add(tab);
            m_ComparisonTabView.selectedTabIndex = tab.tabIndex;
            tab.closed += closedTab =>
            {
                m_ImageComparisonTabs.Remove(closedTab.name);
            };
            m_ShouldUpdateImageComparisonView = true;
            UpdateSliderLabels();
        }

        void OnAdhocComparisonCreated(
            Texture2D imageA,
            Texture2D imageB,
            string label,
            string imageALabel,
            string imageBLabel
        )
        {
            var adhocTab = new ImageComparisonTab(imageA, imageB, imageALabel, imageBLabel);

            if (m_ImageComparisonTabs.TryGetValue(label, out _))
            {
                m_ImageComparisonTabs[label] = adhocTab;
                Tab existing = null;
                foreach (var c in m_ComparisonTabView.Children())
                {
                    if (c.name == label)
                    {
                        existing = (Tab)c;
                        break;
                    }
                }
                if (existing != null)
                    m_ComparisonTabView.selectedTabIndex = existing.tabIndex;
            }
            else
            {
                var tab = new Tab(label)
                {
                    closeable = true,
                    tabIndex = m_TabCount++,
                    name = label,
                };
                m_ImageComparisonTabs.Add(label, adhocTab);
                m_ComparisonTabView.Add(tab);
                m_ComparisonTabView.selectedTabIndex = tab.tabIndex;
                tab.closed += closedTab =>
                {
                    m_ImageComparisonTabs.Remove(closedTab.name);
                };
            }

            m_ShouldUpdateImageComparisonView = true;
        }

        void OnTestBuilderFinished(GraphicsTestBuilder builder)
        {
            foreach (var platform in builder.Platforms)
            {
                if (platform.Schema.rootPath == PlatformSchema.k_DefaultSchemaBase.rootPath)
                    continue;

                Tab tab = null;
                foreach (var c in m_ComparisonTabView.Children())
                {
                    if (c.name == platform.Name)
                    {
                        tab = (Tab)c;
                        break;
                    }
                }
                if (tab == null)
                {
                    tab = new Tab(platform.Name)
                    {
                        closeable = true,
                        tabIndex = m_TabCount++,
                        name = platform.Name,
                    };
                    m_ComparisonTabView.Add(tab);
                }

                m_ComparisonTabView.selectedTabIndex = tab.tabIndex;
                m_ImageComparisonTabs.TryAdd(platform.Name, new ImageComparisonTab(platform));
            }
        }

        void TearDownImageComparisonView()
        {
            GraphicsTestNewComparisonWindow.s_OnComparisonCreated -= OnComparisonCreated;
            GraphicsTestNewComparisonWindow.s_OnAdhocComparisonCreated -= OnAdhocComparisonCreated;
            GraphicsTestBuilder.OnTestBuilderFinished -= OnTestBuilderFinished;
        }

        void UpdateSliderLabels()
        {
            var selectedTab = m_ComparisonTabView[m_ComparisonTabView.selectedTabIndex];
            if (m_ImageComparisonTabs.TryGetValue(selectedTab.name, out var tab))
            {
                m_ReferenceImageLabel.text = tab.ImageALabel;
                m_ActualImageLabel.text = tab.ImageBLabel;
            }
        }

        void AdjustSlider(float value)
        {
            var percent = value / k_SliderMax;
            var visibleWidth = m_ImageGroup.layout.width * percent;
            m_MaskLeft.style.width = visibleWidth;
            m_ReferenceImageBar.style.width = visibleWidth;
            m_ActualImageBar.style.width = m_ImageGroup.layout.width - visibleWidth;
        }

        void UpdateFrequent()
        {
            m_RefImageContainer.style.width = m_ImageGroup.layout.width;
            m_ActImageContainer.style.width = m_ImageGroup.layout.width;
            m_DiffImageContainer.style.width = m_ImageGroup.layout.width;
            AdjustSlider(m_ComparisonSlider.value);

            m_OverlayDiffToggle.SetEnabled(m_DiffImage != null);

            var isAdhocTab =
                m_ImageComparisonTabs.TryGetValue(
                    m_ComparisonTabView[m_ComparisonTabView.selectedTabIndex].name,
                    out var activeTab
                ) && activeTab.IsAdhoc;
            m_AcceptResultButton.SetEnabled(m_ActualImage != null && !isAdhocTab);
            m_ClearResultButton.SetEnabled(m_ActualImage != null && !isAdhocTab);
            m_QuickSwitchButton.SetEnabled(m_ActualImage != null);

            m_VariantNavigator.SetEnabled(m_CachedVariants.Count > 1);
            if (m_CachedVariants.Count > 1)
            {
                m_PrevVariantButton.visible = true;
                m_PrevVariantButton.SetEnabled(m_CurrentVariantIndex > 0);

                m_NextVariantButton.visible = true;
                m_NextVariantButton.SetEnabled(m_CurrentVariantIndex < m_CachedVariants.Count - 1);

                m_VariantLabel.text = $"Variant {m_CurrentVariantIndex + 1} / {m_CachedVariants.Count}";
            }
            else
            {
                m_VariantLabel.text = "No Variants";
                m_PrevVariantButton.visible = false;
                m_NextVariantButton.visible = false;
            }

            m_NoActualImageLabel.visible = m_ActualImage is null;
            m_NoReferenceImageLabel.visible = m_ReferenceImage is null;

            m_NoActImageImage.visible = m_ActualImage is null;
            m_NoRefImageImage.visible = m_ReferenceImage is null;

            m_HasGeneratedImageMetrics = !m_ReferenceImageMetrics?.IsEmpty ?? false;
            m_OverlayHeatmapToggle.SetEnabled(m_HasGeneratedImageMetrics);
        }

        void UpdateToolbarMenu()
        {
            m_ToolbarMenu.menu.ClearItems();
            m_ToolbarMenu.menu.AppendAction(
                "Delete Reference Image",
                _ => DeleteReferenceImage(),
                m_ReferenceImage == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal
            );
            m_ToolbarMenu.menu.AppendAction(
                "Delete All Result Images",
                _ => ClearResults(),
                Directory.Exists(GraphicsTestBuildSettings.LoadOrDefault().ActualImagesPath)
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled
            );
            var selectedFullName = m_SelectedTestCase?.FullName;
            m_ToolbarMenu.menu.AppendAction(
                "Clear Test Result",
                _ =>
                {
                    if (selectedFullName == null)
                        return;
                    m_TestResults.TryRemove(selectedFullName, out var _);
                    BuildFullTreeModel();
                },
                selectedFullName != null && m_TestResults.ContainsKey(selectedFullName)
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled
            );
        }

        bool m_ShouldUpdateImageComparisonView { get; set; }

        void UpdateImageComparisonView()
        {
            GraphicsTestCase newSelected = null;
            foreach (var i in m_TreeView.selectedItems)
            {
                var g = i as GraphicsTestCaseGroup;
                if (g?.m_TestCase == null)
                    continue;
                newSelected = g.m_TestCase;
                break;
            }

            var isNewTestCase = newSelected != m_SelectedTestCase;

            if (newSelected == m_SelectedTestCase && !m_ShouldUpdateImageComparisonView)
                return;

            var selectedTabName = m_ComparisonTabView[m_ComparisonTabView.selectedTabIndex].name;
            if (!m_ImageComparisonTabs.TryGetValue(selectedTabName, out var currentComparison))
                return;

            // Ad-hoc tabs display pre-loaded textures and don't depend on the tree selection
            if (currentComparison.IsAdhoc)
            {
                m_ReferenceImage = currentComparison.AdhocImageA;
                m_ActualImage = currentComparison.AdhocImageB;
                m_DiffImage = null;
                m_HeatmapImage = null;

                m_RefImageContainer.image = m_ReferenceImage;
                m_ActImageContainer.image = m_ActualImage;
                m_DiffImageContainer.image = null;
                m_HeatmapContainer.image = null;

                m_ImageToolbar.style.display = DisplayStyle.Flex;
                m_ImagePanel.style.display = DisplayStyle.Flex;
                m_ComparisonSlider.style.display = DisplayStyle.Flex;

                m_VariantNavigator.style.display = DisplayStyle.None;

                UpdateSliderLabels();
                m_ShouldUpdateImageComparisonView = false;
                return;
            }

            if (newSelected == null)
            {
                m_ImageToolbar.style.display = DisplayStyle.None;
                m_ImagePanel.style.display = DisplayStyle.None;

                m_SelectedTestCase = null;
                m_ReferenceImage = null;
                m_ActualImage = null;
                m_DiffImage = null;
                m_HeatmapImage = null;
                m_CurrentVariantIndex = 0;
                m_CachedVariants.Clear();

                return;
            }

            if (isNewTestCase)
            {
                m_CurrentVariantIndex = 0;
            }

            m_SelectedTestCase = newSelected;
            m_RefImageContainer.image = null;
            m_ActImageContainer.image = null;
            m_DiffImageContainer.image = null;
            m_HeatmapContainer.image = null;

            // Cache variants on test case change, or rebuild if empty (e.g. after returning from an ad-hoc tab)
            if (isNewTestCase || m_CachedVariants.Count == 0)
            {
                m_CachedVariants.Clear();
                if (m_SelectedTestCase.ReferenceImage != null)
                    m_CachedVariants.Add(m_SelectedTestCase.ReferenceImage);

                foreach (var variant in m_SelectedTestCase.AdditionalReferenceImages)
                {
                    if (variant != null && !m_CachedVariants.Contains(variant))
                        m_CachedVariants.Add(variant);
                }
            }

            if (m_CachedVariants.Count == 0)
            {
                m_ReferenceImage = null;
                m_ActualImage = null;
                m_DiffImage = null;
                m_HeatmapImage = null;

                m_RefImageContainer.image = null;
                m_ActImageContainer.image = null;
                m_DiffImageContainer.image = null;
                m_HeatmapContainer.image = null;

                m_ImageToolbar.style.display = DisplayStyle.Flex;
                m_ImagePanel.style.display = DisplayStyle.Flex;
                m_ComparisonSlider.style.display = DisplayStyle.Flex;
                m_VariantNavigator.style.display = DisplayStyle.Flex;

                m_ShouldUpdateImageComparisonView = false;
                return;
            }

            if (m_CurrentVariantIndex >= m_CachedVariants.Count)
            {
                m_CurrentVariantIndex = m_CachedVariants.Count - 1;
            }

            var currentVariant = m_CachedVariants[m_CurrentVariantIndex];
            m_ReferenceImage = currentVariant.Image;
            m_RefImageContainer.image = m_ReferenceImage;

            var variantExt = currentVariant.ImageExtension.ToLowerCase();
            var variantName = currentVariant.Name;
            var actualPath = $"{currentComparison.ImageBPath}/{variantName}.{variantExt}";
            m_ActualImage = AssetService.LoadAssetAtPath<Texture2D>(actualPath);
            m_ActImageContainer.image = m_ActualImage;

            var diffPath = $"{currentComparison.ImageBPath}/{variantName}.diff.{variantExt}";
            m_DiffImage = AssetService.LoadAssetAtPath<Texture2D>(diffPath);
            m_DiffImageContainer.image = m_DiffImage;

            m_ImageToolbar.style.display = DisplayStyle.Flex;
            m_ImagePanel.style.display = DisplayStyle.Flex;
            m_ComparisonSlider.style.display = DisplayStyle.Flex;
            m_VariantNavigator.style.display = DisplayStyle.Flex;

            if (m_ReferenceImage != null && m_HasGeneratedImageMetrics && m_OverlayHeatmapToggle.value)
            {
                if (m_HeatmapImage != null)
                    DestroyImmediate(m_HeatmapImage);

                var colorScheme = GraphicsTestBuildSettings.LoadOrDefault().HeatmapColorScheme;
                m_HeatmapImage =
                    m_HeatmapManager.LoadHeatmap(m_SelectedTestCase.FileName, colorScheme)
                    ?? m_HeatmapManager.EmptyTexture(m_ReferenceImage?.width, m_ReferenceImage?.height, colorScheme);

                m_HeatmapContainer.image = m_HeatmapImage;
            }

            m_ShouldUpdateImageComparisonView = false;
        }

        void UpdateReference()
        {
            if (m_ActualImage == null)
            {
                GraphicsTestLogger.LogError("Did not find any result image to update.");
                return;
            }

            var startingPath = AssetService.GetAssetPath(m_ActualImage);

            if (string.IsNullOrEmpty(startingPath))
                return;

            var destinationPath = startingPath.Replace(
                GraphicsTestBuildSettings.LoadOrDefault().ActualImagesPath,
                PlatformSchema.k_DefaultReferenceImagesRoot
            );

            if (File.Exists(destinationPath))
            {
                AssetService.DeleteAsset(destinationPath);
            }

            if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);
                AssetService.Refresh();
            }

            AssetService.MoveAsset(startingPath, destinationPath);
            AssetService.SaveAssets();
            AssetService.Refresh();

            GraphicsTestLogger.Log($"Reference Image at path {destinationPath} was successfully updated.");
            EditorGUIUtility.PingObject(AssetService.LoadAssetAtPath<Texture2D>(destinationPath));
        }

        void ResetView()
        {
            if (m_ReferenceImage != null && m_ActualImage != null)
            {
                m_MaskLeft.style.width = new Length(k_SliderMidpoint, LengthUnit.Percent);
                m_ReferenceImageBar.style.width = new Length(k_SliderMidpoint, LengthUnit.Percent);
                m_ComparisonSlider.value = k_SliderMidpoint;
            }
            else if (m_ActualImage == null)
            {
                m_MaskLeft.style.width = new Length(k_SliderMax, LengthUnit.Percent);
                m_ReferenceImageBar.style.width = new Length(k_SliderMax, LengthUnit.Percent);
                m_ComparisonSlider.value = k_SliderMax;
            }
            else
            {
                m_MaskLeft.style.width = new Length(k_SliderMin, LengthUnit.Percent);
                m_ReferenceImageBar.style.width = new Length(k_SliderMin, LengthUnit.Pixel);
                m_ComparisonSlider.value = k_SliderMin;
            }

            m_ZoomSlider.value = 0;
            ResetImageGroupPosition();
        }

        void ResetImageGroupPosition()
        {
            m_ImageGroup.style.top = 0;
            m_ImageGroup.style.right = 0;
            m_ImageGroup.style.bottom = 0;
            m_ImageGroup.style.left = 0;
            m_ImageGroup.style.translate = new StyleTranslate(Translate.None());
        }

        void DeleteReferenceImage()
        {
            if (File.Exists(AssetService.GetAssetPath(m_ReferenceImage)))
            {
                AssetService.DeleteAsset(AssetService.GetAssetPath(m_ReferenceImage));
            }

            m_ReferenceImage = null;
        }

        void DeleteActualImage()
        {
            if (File.Exists(AssetService.GetAssetPath(m_ActualImage)))
            {
                AssetService.DeleteAsset(AssetService.GetAssetPath(m_ActualImage));
            }

            if (File.Exists(AssetService.GetAssetPath(m_DiffImage)))
            {
                AssetService.DeleteAsset(AssetService.GetAssetPath(m_DiffImage));
            }

            m_ActualImage = null;
            m_DiffImage = null;
        }

        void ClearResults()
        {
            AssetService.DeleteAsset(GraphicsTestBuildSettings.LoadOrDefault().ActualImagesPath);
        }

        void OpenScene(string testName)
        {
            if (!m_TreeViewModel.TestNames.TryGetValue(testName, out var id))
                return;

            var scenePath = (
                m_TreeView.GetItemDataForId<GraphicsTestCaseGroup>(id).m_TestCase as SceneGraphicsTestCase
            )?.ScenePath;
            EditorSceneManager.OpenScene(scenePath);
        }

        static Rect GetWorldRect(VisualElement ve)
        {
            // Top-left corner in world space
            var topLeft = ve.LocalToWorld(Vector2.zero);

            // Bottom-right corner in world space
            var bottomRight = ve.LocalToWorld(new Vector2(ve.resolvedStyle.width, ve.resolvedStyle.height));

            return new Rect(topLeft, bottomRight - topLeft);
        }
    }

    class GrabbableManipulator : PointerManipulator
    {
        Vector2 m_StartPointerPosition;
        Vector2 m_StartElementPosition;
        bool m_IsDragging;

        internal GrabbableManipulator(VisualElement target)
        {
            this.target = target;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0) // Left mouse button
            {
                m_StartPointerPosition = evt.position;
                m_StartElementPosition = target.layout.position;
                m_IsDragging = true;
                target.CapturePointer(evt.pointerId);
                target.AddToClassList("grabbing"); // Switch to grabbing cursor
                evt.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (m_IsDragging && target.HasPointerCapture(evt.pointerId))
            {
                var delta = new Vector2(evt.position.x, evt.position.y) - m_StartPointerPosition;
                target.style.left = m_StartElementPosition.x + delta.x;
                target.style.top = m_StartElementPosition.y + delta.y;
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (m_IsDragging && target.HasPointerCapture(evt.pointerId))
            {
                m_IsDragging = false;
                target.ReleasePointer(evt.pointerId);
                target.RemoveFromClassList("grabbing");
            }
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (m_IsDragging)
            {
                m_IsDragging = false;
                target.RemoveFromClassList("grabbing");
            }
        }
    }
}
