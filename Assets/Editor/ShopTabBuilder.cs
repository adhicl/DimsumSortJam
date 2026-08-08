using System.Collections.Generic;
using System.Linq;
using Commons;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTools
{
    /// <summary>
    /// Generates the Home scene's shop tab from <see cref="IAPCatalog"/>.
    ///
    /// Two stages, both run by <b>Tools ▸ Shop ▸ Rebuild Shop Tab</b>:
    ///
    /// 1. <see cref="RebuildPrefabs"/> writes the row templates to
    ///    <c>Assets/Prefabs/UI/Generated/Shop/</c> — a section header, a bundle row, a gold tile
    ///    and the gold grid. They are assembled out of CuteKawaiiGUIPack parts so the shop reads
    ///    as part of the kit rather than as something bolted on: every card is the kit's
    ///    five-layer <c>Rectangle-Outline-Shadow</c> frame, every reward well is a
    ///    <c>Rectangle-Outline</c>, and the colours are read out of the kit's own colour variants
    ///    at build time rather than being guessed here. Same construction as the kit's own row
    ///    prefabs (<c>Messages-Item</c>, <c>Shop-Item</c>, <c>Rewards-Item</c>).
    ///
    /// 2. <see cref="Rebuild"/> instantiates those prefabs into Page 1 and fills them in. Every
    ///    amount printed on a card is read from <see cref="IAPCatalog.Rewards"/>, so the shelf
    ///    can never advertise something different from what the purchase grants. Prices are
    ///    never generated: <see cref="IAPBuyButton"/> fills them in from the store at runtime.
    ///
    /// The build is idempotent — it deletes the previously generated root and makes a new one.
    /// Edit the prefabs to restyle every row at once; edit this file to change what rows exist.
    /// </summary>
    public static class ShopTabBuilder
    {
        private const string ScenePath = "Assets/Scenes/Home.unity";

        /// <summary>Everything this tool creates in the scene lives here, so a rebuild is a clean sweep.</summary>
        private const string GeneratedRootName = "Shop-Generated";

        private const string PrefabDir = "Assets/Prefabs/UI/Generated/Shop";
        private const string SectionHeaderPrefab = PrefabDir + "/Shop-Section-Header.prefab";
        private const string BundleRowPrefab = PrefabDir + "/Shop-Bundle-Row.prefab";
        private const string GoldTilePrefab = PrefabDir + "/Shop-Gold-Tile.prefab";
        private const string GoldPanelPrefab = PrefabDir + "/Shop-Gold-Panel.prefab";

        // The shop page sits between the Home scene's shared top bar and the tab strip.
        private const float TopBarHeight = 190f;
        private const float TabBarHeight = 200f;

        private const float SideMargin = 30f;
        private const float SectionHeaderHeight = 110f;
        private const float BundleRowHeight = 420f;
        private const float BundleFooterHeight = 128f;
        private const float ContentWidth = 1020f;
        private const float GoldCellWidth = 320f;
        private const float GoldCellHeight = 400f;
        private const float GoldSpacing = 30f;
        private const int GoldColumns = 3;

        // --- Kit assets -------------------------------------------------------------------

        private const string KitRoot = "Assets/CuteKawaiiGUIPack/Demo/Prefabs/Common/";
        private const string FramePath = KitRoot + "1-Foundations/Shapes/Rectangle-Outline-Shadow/Rectangle-Outline-Shadow-{0}.prefab";
        private const string WellPath = KitRoot + "1-Foundations/Shapes/Rectangle-Outline/Rectangle-Outline-{0}.prefab";
        private const string HeadlinePath = KitRoot + "3-Layouts/Headline/Headline-{0}.prefab";
        private const string BuyButtonPath = KitRoot + "2-Components/Buttons/Buttons/Button-Green-Label-Only.prefab";

        private const string BadgePath = KitRoot + "3-Layouts/Badges/Sign-Sale.prefab";
        private const string FontPath = "Assets/CuteKawaiiGUIPack/Demo/Fonts/Dosis-ExtraBold SDF.asset";

        /// <summary>
        /// The scrolling tile pattern every other tab uses, in the shop's own colour. Home is
        /// Blue and Missions is Violet, so Green keeps the tabs telling themselves apart — it is
        /// also what the original shop template used.
        /// </summary>
        private const string BackgroundPath =
            "Assets/CuteKawaiiGUIPack/Demo/Prefabs/Common/1-Foundations/Background-Tiles/Backgrund-Tiles-Green.prefab";

        // The kit numbers its coin art from biggest hoard to smallest, so these read backwards
        // on purpose: Coin1 is the smallest pile a tier can show, Coin6 the treasure chest.
        private const string CoinsDir = "Assets/CuteKawaiiGUIPack/Demo/Sprites/Panels/Credit-Shop-Popups/Coins/";
        private const string Coin1 = CoinsDir + "Coins-6.png";
        private const string Coin2 = CoinsDir + "Coins-5.png";
        private const string Coin3 = CoinsDir + "Coins-4.png";
        private const string Coin4 = CoinsDir + "Coins-3.png";
        private const string Coin5 = CoinsDir + "Coins-2-Bag.png";
        private const string Coin6 = CoinsDir + "Coins-1-Chest.png";

        private const string HeartPath = "Assets/CuteKawaiiGUIPack/Icons/Icons/Hearts/Heart-Red-128.png";
        private const string NoAdsIconPath = "Assets/CuteKawaiiGUIPack/Icons/Icons/Media/Display-128.png";

        /// <summary>
        /// Hand-made animated illustrations lifted from the kit's other panels. Unlike everything
        /// else here these are authored, not generated — the builder places them and nothing more.
        /// </summary>
        private const string IllustrationDir = PrefabDir + "/Illustrations";
        private const string SealbearIllustration = IllustrationDir + "/Shop-Illustration-Sealbear.prefab";
        private const string BunnyMailIllustration = IllustrationDir + "/Shop-Illustration-Bunny-Mail.prefab";

        /// <summary>The game's four power-up slots, in the order the in-game top bar shows them.</summary>
        private static readonly string[] PowerupIconPaths =
        {
            "Assets/CuteKawaiiGUIPack/Demo/Sprites/Panels/Crafting/Menu/Package.png",
            "Assets/CuteKawaiiGUIPack/Icons/Icons/Basics/Lens-128.png",
            "Assets/CuteKawaiiGUIPack/Icons/Icons/Arrows/Arrow-Random-Blue-Violet-64.png",
            "Assets/CuteKawaiiGUIPack/Icons/Icons/Time/Hourglass-128.png",
        };

        // --- Card model ------------------------------------------------------------------

        /// <summary>
        /// One shelf item. <see cref="Palette"/> names a CuteKawaiiGUIPack colour family — the
        /// frame, well and label colours are all lifted from that family's kit prefabs, so a
        /// card can only ever be a colour the kit already ships.
        /// </summary>
        private class CardSpec
        {
            public string ProductId;
            public string DisplayName;
            public string Palette;
            public string ArtPath;

            /// <summary>
            /// True when <see cref="ArtPath"/> is a pile of coins, so the gold amount can be
            /// printed under it. Cards whose art sells something else — the No Ads bundle's
            /// screen, say — show their gold as a chip instead, where it cannot be misread as
            /// a caption for the icon.
            /// </summary>
            public bool ArtIsGold;

            /// <summary>
            /// An animated illustration to stand in the art slot instead of a flat icon, e.g.
            /// the kit's Sealbear or Bunny scenes. These are hand-made, so they live as their
            /// own prefabs under <c>Illustrations/</c> and this tool only places them — it never
            /// generates or overwrites them. Setting one hides <see cref="ArtPath"/>'s image.
            /// </summary>
            public string IllustrationPath;

            /// <summary>Shown in place of the reward wells, for cards that sell an entitlement.</summary>
            public string Description;

            /// <summary>Optional corner flash, e.g. "BEST VALUE".</summary>
            public string Badge;
        }

        private static readonly CardSpec SpecialPack = new CardSpec
        {
            ProductId = IAPCatalog.StarterPack,
            DisplayName = "Starter Pack",
            Palette = "Orange",
            ArtPath = Coin3,
            ArtIsGold = true,
            Badge = "BEST VALUE",
        };

        private static readonly CardSpec NoAds = new CardSpec
        {
            ProductId = IAPCatalog.RemoveAds,
            DisplayName = "No Ads",
            Palette = "Berry",
            ArtPath = NoAdsIconPath,
            IllustrationPath = SealbearIllustration,
            Description = "Remove banner ads",
        };

        private static readonly CardSpec[] Bundles =
        {
            new CardSpec
            {
                ProductId = IAPCatalog.BundleNoAds, DisplayName = "No Ads Bundle",
                Palette = "Berry", ArtPath = NoAdsIconPath,
                IllustrationPath = BunnyMailIllustration,
            },
            new CardSpec
            {
                ProductId = IAPCatalog.PowerupBundle, DisplayName = "Power-Up Bundle",
                Palette = "Teal", ArtPath = null,
            },
            new CardSpec
            {
                ProductId = IAPCatalog.BundleBig, DisplayName = "Big Bundle",
                Palette = "Violet", ArtPath = Coin2, ArtIsGold = true,
            },
            new CardSpec
            {
                ProductId = IAPCatalog.BundleGreat, DisplayName = "Great Bundle",
                Palette = "Yellow", ArtPath = Coin3, ArtIsGold = true,
            },
            new CardSpec
            {
                ProductId = IAPCatalog.BundleUltra, DisplayName = "Ultra Bundle",
                Palette = "Purple", ArtPath = Coin4, ArtIsGold = true,
            },
            new CardSpec
            {
                ProductId = IAPCatalog.BundleSuperior, DisplayName = "Superior Bundle",
                Palette = "Red", ArtPath = Coin5, ArtIsGold = true,
            },
            new CardSpec
            {
                ProductId = IAPCatalog.BundleLegendary, DisplayName = "Legendary Bundle",
                Palette = "Cyan", ArtPath = Coin6, ArtIsGold = true,
            },
        };

        /// <summary>Gold tiers, cheapest first — the 3x2 grid at the bottom of the shop.</summary>
        private static readonly string[] GoldTiers =
        {
            IAPCatalog.Coins500, IAPCatalog.Coins1200, IAPCatalog.Coins3000,
            IAPCatalog.Coins8000, IAPCatalog.Coins20000, IAPCatalog.Coins50000,
        };

        /// <summary>Coin art per gold tier, smallest pile to biggest.</summary>
        private static readonly string[] GoldTierArt = { Coin1, Coin2, Coin3, Coin4, Coin5, Coin6 };

        private const string GoldPalette = "Blue";

        // --- Entry points ----------------------------------------------------------------

        [MenuItem("Tools/Shop/Rebuild Shop Tab")]
        public static void Rebuild()
        {
            // Deliberately does not regenerate the prefabs: they are meant to be edited by hand
            // once generated, and rewriting them on every tab rebuild would throw that away.
            // Use "Rebuild Shop Prefabs Only" to go back to the generated styling.
            if (Load<GameObject>(BundleRowPrefab) == null) RebuildPrefabs();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var panel = FindPanelShop(scene);
            if (panel == null)
            {
                Debug.LogError("[ShopTabBuilder] Could not find 'Panel-Shop' under Page 1 in " + ScenePath);
                return;
            }

            var old = panel.Find(GeneratedRootName);
            if (old != null)
            {
                if (!ConfirmHandEditsMayBeLost(old)) return;
                Object.DestroyImmediate(old.gameObject);
            }

            RetireTemplate(panel);

            BuildScene(panel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShopTabBuilder] Rebuilt the shop tab: " +
                      (2 + Bundles.Length + GoldTiers.Length) + " cards.");
        }

        [MenuItem("Tools/Shop/Rebuild Shop Prefabs Only")]
        public static void RebuildPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir)) CreateFolders(PrefabDir);

            SavePrefab(BuildSectionHeaderTemplate(), SectionHeaderPrefab);
            SavePrefab(BuildBundleRowTemplate(), BundleRowPrefab);
            SavePrefab(BuildGoldTileTemplate(), GoldTilePrefab);
            SavePrefab(BuildGoldPanelTemplate(), GoldPanelPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log("[ShopTabBuilder] Wrote the shop row prefabs to " + PrefabDir);
        }

        /// <summary>
        /// A rebuild throws away the whole generated subtree, and anything dropped into a card
        /// by hand goes with it — the animated illustrations on the No Ads cards, for instance.
        /// Losing hand work to a menu click should never be a surprise, so name what would go
        /// and let the user back out.
        /// </summary>
        private static bool ConfirmHandEditsMayBeLost(Transform generated)
        {
            var added = new List<string>();
            foreach (var t in generated.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;

                foreach (var a in PrefabUtility.GetAddedGameObjects(t.gameObject))
                {
                    added.Add(t.name + "  →  " + a.instanceGameObject.name);
                }
            }

            if (added.Count == 0) return true;

            return EditorUtility.DisplayDialog(
                "Rebuild the shop tab?",
                "These were added to the generated cards by hand and will be destroyed:\n\n" +
                string.Join("\n", added.Select(a => "    " + a).ToArray()) +
                "\n\nTo keep them across rebuilds, move them into the prefabs in\n" + PrefabDir,
                "Rebuild anyway", "Cancel");
        }

        /// <summary>
        /// The shop tab shipped as an untouched UI-kit sample. It is switched off rather than
        /// deleted so the original art is still there to copy from; delete it by hand once the
        /// generated shop is signed off.
        /// </summary>
        private static void RetireTemplate(Transform panel)
        {
            foreach (var name in new[] { "Header", "Content" })
            {
                var child = panel.Find(name);
                if (child == null) continue;

                child.gameObject.SetActive(false);
                child.name = name + " (unused template)";
            }
        }

        private static Transform FindPanelShop(UnityEngine.SceneManagement.Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "Panel-Shop");
        }

        // --- Prefab templates -------------------------------------------------------------

        /// <summary>
        /// A full-width section bar. The kit's Headline hugs its text and carries a subtitle;
        /// a long shelf reads better with a bar that spans the list, so the subtitle goes and
        /// the fitter is switched off — the sprite and the family's title colours stay.
        /// </summary>
        private static GameObject BuildSectionHeaderTemplate()
        {
            var root = NewRect("Shop-Section-Header", null);
            root.sizeDelta = new Vector2(ContentWidth, SectionHeaderHeight);
            AddFixedHeight(root, SectionHeaderHeight);

            var headline = CloneKit(string.Format(HeadlinePath, "Green"), root);
            var headlineRect = (RectTransform)headline.transform;
            Stretch(headlineRect);

            var subtitle = headline.transform.Find("Subtitle-Background");
            if (subtitle != null) Object.DestroyImmediate(subtitle.gameObject);

            // The kit clips the pill to its text with a fitter, a layout group and a Mask. All
            // three go: the bar spans the list now. The Mask especially — left on a full-width
            // bar it does nothing, and saving the prefab with it throws inside TMP's sub-mesh
            // material update when the stencil state changes.
            var bar = (RectTransform)headline.transform.Find("Title-Background");
            var fitter = bar.GetComponent<ContentSizeFitter>();
            if (fitter != null) Object.DestroyImmediate(fitter);
            var group = bar.GetComponent<HorizontalLayoutGroup>();
            if (group != null) Object.DestroyImmediate(group);
            var mask = bar.GetComponent<Mask>();
            if (mask != null) Object.DestroyImmediate(mask);
            Stretch(bar);

            var label = (RectTransform)bar.Find("Label-Title");
            Stretch(label);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.text = "SECTION";
            text.fontSize = 52f;
            text.alignment = TextAlignmentOptions.Center;

            return root.gameObject;
        }

        /// <summary>
        /// One bundle row: the kit's five-layer card frame, a hero image, a grid of power-up
        /// chips, a column of extras (gold / unlimited lives) and a footer with the buy button.
        /// Every optional piece exists in the prefab and is switched off per card, so filling a
        /// row in never adds or removes objects — instances carry value overrides only.
        /// </summary>
        private static GameObject BuildBundleRowTemplate()
        {
            var root = NewRect("Shop-Bundle-Row", null);
            root.sizeDelta = new Vector2(ContentWidth, BundleRowHeight);
            AddFixedHeight(root, BundleRowHeight);

            AddFrame(root, "Violet");

            // --- top row: art, power-ups, extras ---
            var top = NewRect("Top", root);
            Stretch(top);
            top.offsetMin = new Vector2(20f, BundleFooterHeight + 6f);
            top.offsetMax = new Vector2(-20f, -20f);

            var row = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            row.childAlignment = TextAnchor.MiddleLeft;

            var art = NewRect("Art", top);
            AddFlexibleWidth(art, 320f, 0f);
            var artImage = NewImage("Image", art, null);
            Stretch(artImage);
            artImage.offsetMin = new Vector2(10f, 52f);
            artImage.offsetMax = new Vector2(-10f, -10f);
            artImage.GetComponent<Image>().preserveAspect = true;
            var goldLabel = NewLabel("Label-Gold", art, "0", 54f, TextAlignmentOptions.Center);
            goldLabel.anchorMin = new Vector2(0f, 0f);
            goldLabel.anchorMax = new Vector2(1f, 0f);
            goldLabel.pivot = new Vector2(0.5f, 0f);
            goldLabel.sizeDelta = new Vector2(0f, 60f);
            goldLabel.anchoredPosition = Vector2.zero;

            var powerups = NewWell("Powerups", top, "Violet");
            AddFlexibleWidth(powerups, 0f, 1f);
            var grid = NewRect("Grid", powerups);
            Stretch(grid);
            grid.offsetMin = new Vector2(14f, 14f);
            grid.offsetMax = new Vector2(-14f, -14f);
            var chipGrid = grid.gameObject.AddComponent<GridLayoutGroup>();
            chipGrid.spacing = new Vector2(8f, 8f);
            chipGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            chipGrid.constraintCount = 2;
            chipGrid.cellSize = new Vector2(110f, 110f);
            chipGrid.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < PowerupIconPaths.Length; i++) NewChip("Powerup" + (i + 1), grid);

            var extras = NewWell("Extras", top, "Violet");
            AddFlexibleWidth(extras, 230f, 0f);
            var column = NewRect("Column", extras);
            Stretch(column);
            column.offsetMin = new Vector2(16f, 16f);
            column.offsetMax = new Vector2(-16f, -16f);
            var stack = column.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 8f;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = true;
            stack.childAlignment = TextAnchor.MiddleCenter;
            NewChip("Gold", column);
            NewChip("Life", column);

            // Shown instead of the wells on cards that sell an entitlement rather than goods.
            var description = NewWell("Description", top, "Violet");
            AddFlexibleWidth(description, 0f, 1f);
            var descriptionText = NewLabel("Label", description, "", 46f, TextAlignmentOptions.Center);
            Stretch(descriptionText);
            descriptionText.offsetMin = new Vector2(24f, 16f);
            descriptionText.offsetMax = new Vector2(-24f, -16f);
            description.gameObject.SetActive(false);

            // --- footer: name and price ---
            var footer = NewRect("Footer", root);
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.sizeDelta = new Vector2(-40f, BundleFooterHeight);
            footer.anchoredPosition = new Vector2(0f, 14f);

            var name = NewLabel("Label-Name", footer, "Bundle", 50f, TextAlignmentOptions.Left);
            Stretch(name);
            name.offsetMin = new Vector2(16f, 0f);
            name.offsetMax = new Vector2(-400f, 0f);

            NewBuyButton(footer, new Vector2(0f, 0f));

            var badge = NewBadge(root);
            badge.gameObject.SetActive(false);

            return root.gameObject;
        }

        private static GameObject BuildGoldTileTemplate()
        {
            var root = NewRect("Shop-Gold-Tile", null);
            root.sizeDelta = new Vector2(GoldCellWidth, GoldCellHeight);

            AddFrame(root, GoldPalette);

            var header = NewWell("Header", root, GoldPalette);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(-28f, 86f);
            header.anchoredPosition = new Vector2(0f, -16f);

            var amount = NewLabel("Label-Amount", header, "0", 48f, TextAlignmentOptions.Center);
            Stretch(amount);

            var art = NewImage("Art", root, null);
            Stretch(art);
            art.offsetMin = new Vector2(26f, 132f);
            art.offsetMax = new Vector2(-26f, -116f);
            art.GetComponent<Image>().preserveAspect = true;

            var buy = NewBuyButton(root, Vector2.zero);
            buy.anchorMin = new Vector2(0.5f, 0f);
            buy.anchorMax = new Vector2(0.5f, 0f);
            buy.pivot = new Vector2(0.5f, 0f);
            buy.sizeDelta = new Vector2(GoldCellWidth - 52f, 104f);
            buy.anchoredPosition = new Vector2(0f, 24f);

            return root.gameObject;
        }

        private static GameObject BuildGoldPanelTemplate()
        {
            int rows = Mathf.CeilToInt(GoldTiers.Length / (float)GoldColumns);
            float height = rows * GoldCellHeight + (rows - 1) * GoldSpacing;

            var root = NewRect("Shop-Gold-Panel", null);
            root.sizeDelta = new Vector2(ContentWidth, height);
            AddFixedHeight(root, height);

            var grid = root.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GoldColumns;
            grid.cellSize = new Vector2(GoldCellWidth, GoldCellHeight);
            grid.spacing = new Vector2(GoldSpacing, GoldSpacing);
            grid.childAlignment = TextAnchor.UpperCenter;

            // The cell size above is only the authoring default. A 20:9 phone gives the canvas
            // about 966 units of width, where three 320-wide cells plus spacing do not fit, so
            // the real size is divided out of the width at runtime.
            var responsive = root.gameObject.AddComponent<ResponsiveGrid>();
            SetPrivate(responsive, "cellAspect", GoldCellHeight / GoldCellWidth);

            return root.gameObject;
        }

        // --- Scene assembly ----------------------------------------------------------------

        private static void BuildScene(Transform panel)
        {
            var root = NewRect(GeneratedRootName, panel);
            Stretch(root);

            // Only the prefab root is stretched. Its "Tiles" child is deliberately oversized and
            // animated, so squaring it up to the panel would stop the pattern drifting.
            var background = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(BackgroundPath), root);
            background.name = "Background";
            Stretch((RectTransform)background.transform);

            var scroll = NewRect("Shop-ScrollRect", root);
            Stretch(scroll);
            scroll.offsetMin = new Vector2(0f, TabBarHeight);
            scroll.offsetMax = new Vector2(0f, -TopBarHeight);

            var viewport = NewRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            // An invisible raycast target over the whole list. Without it a ScrollRect can only
            // be dragged where some other graphic happens to be — here that was the buy buttons
            // and the section bars, so a swipe on a card body fell through to the pager behind
            // and turned into a page change instead of a scroll. Alpha is irrelevant to
            // raycasting, so a fully clear Image costs nothing visually.
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            catcher.raycastTarget = true;

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero; // width follows the viewport; the fitter sets height

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)SideMargin, (int)SideMargin, 24, 60);
            layout.spacing = 26f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // NestedScrollRect rather than ScrollRect: this list lives inside the Home scene's
            // horizontal pager, and a plain ScrollRect would eat the swipe that leaves the tab.
            var rect = scroll.gameObject.AddComponent<NestedScrollRect>();
            rect.content = content;
            rect.viewport = viewport;
            rect.horizontal = false;
            rect.vertical = true;
            rect.movementType = ScrollRect.MovementType.Elastic;
            rect.elasticity = 0.1f;
            rect.inertia = true;
            rect.decelerationRate = 0.135f;
            rect.scrollSensitivity = 40f;

            SectionHeader(content, "SPECIAL PACK", "Orange");
            BundleRow(content, SpecialPack);

            SectionHeader(content, "NO ADS", "Berry");
            BundleRow(content, NoAds);

            SectionHeader(content, "BUNDLES", "Blue");
            foreach (var spec in Bundles) BundleRow(content, spec);

            // Not Green: the page's own tiles are Green, so a Green bar disappears into them.
            SectionHeader(content, "GOLD", "Yellow");
            GoldPanel(content);
        }

        private static void SectionHeader(Transform parent, string title, string palette)
        {
            var instance = Instantiate(SectionHeaderPrefab, parent);
            instance.name = "Header-" + title;

            var bar = instance.transform.Find("Headline-Green/Title-Background");
            var label = bar.Find("Label-Title").GetComponent<TextMeshProUGUI>();

            var source = Load<GameObject>(string.Format(HeadlinePath, palette));
            var sourceBar = source.transform.Find("Title-Background");
            bar.GetComponent<Image>().color = sourceBar.GetComponent<Image>().color;
            label.color = sourceBar.Find("Label-Title").GetComponent<TextMeshProUGUI>().color;
            label.text = title;
        }

        private static void BundleRow(Transform parent, CardSpec spec)
        {
            var reward = IAPCatalog.Rewards[spec.ProductId];
            var instance = Instantiate(BundleRowPrefab, parent);
            instance.name = "Card-" + spec.ProductId;

            ApplyPalette(instance.transform, spec.Palette);

            // Only the one-time products can ever be "owned"; a consumable card never hides.
            if (IAPCatalog.IsNonConsumable(spec.ProductId))
            {
                SetPrivate(instance.AddComponent<ShopCard>(), "productId", spec.ProductId);
            }

            bool goldOnArt = spec.ArtIsGold && reward.gold > 0;

            var art = instance.transform.Find("Top/Art");
            art.gameObject.SetActive(spec.ArtPath != null || spec.IllustrationPath != null);
            if (art.gameObject.activeSelf)
            {
                // An animated illustration replaces the flat icon rather than sitting on top of
                // it. Placement lives in the illustration prefab, so instantiating reproduces it.
                var image = art.Find("Image").gameObject;
                image.SetActive(spec.IllustrationPath == null);
                if (image.activeSelf)
                {
                    var sprite = image.GetComponent<Image>();
                    sprite.sprite = Load<Sprite>(spec.ArtPath);
                    ((RectTransform)sprite.transform).offsetMin = new Vector2(10f, goldOnArt ? 52f : 10f);
                }

                if (spec.IllustrationPath != null)
                {
                    var illustration = Instantiate(spec.IllustrationPath, art);
                    illustration.name = "Illustration";
                    illustration.transform.SetAsFirstSibling();
                }

                var label = art.Find("Label-Gold").gameObject;
                label.SetActive(goldOnArt);
                if (goldOnArt) label.GetComponent<TextMeshProUGUI>().text = reward.gold.ToString("N0");
            }

            var description = instance.transform.Find("Top/Description");
            description.gameObject.SetActive(spec.Description != null);
            if (spec.Description != null)
            {
                description.Find("Label").GetComponent<TextMeshProUGUI>().text = spec.Description;
            }

            var powerups = instance.transform.Find("Top/Powerups");
            powerups.gameObject.SetActive(spec.Description == null && reward.powerupEach > 0);
            for (int i = 0; i < PowerupIconPaths.Length; i++)
            {
                FillChip(powerups.Find("Grid/Powerup" + (i + 1)), PowerupIconPaths[i],
                    reward.powerupEach.ToString("N0"), null);
            }

            // Everything that did not fit on the art or in the power-up grid ends up here.
            var extras = instance.transform.Find("Top/Extras");
            var goldChip = extras.Find("Column/Gold");
            var lifeChip = extras.Find("Column/Life");

            bool showGold = reward.gold > 0 && !goldOnArt;
            goldChip.gameObject.SetActive(showGold);
            if (showGold) FillChip(goldChip, Coin2, reward.gold.ToString("N0"), null);

            bool showLife = reward.unlimitedLifeHours > 0 || reward.lives > 0;
            lifeChip.gameObject.SetActive(showLife);
            if (showLife)
            {
                // An infinity heart plus how long it lasts, matching the top bar's ∞ heart.
                FillChip(lifeChip, HeartPath,
                    reward.unlimitedLifeHours > 0
                        ? Settings.GetLifeWindowLabel(reward.unlimitedLifeHours)
                        : reward.lives.ToString("N0"),
                    reward.unlimitedLifeHours > 0 ? "∞" : null);
            }

            extras.gameObject.SetActive(spec.Description == null && (showGold || showLife));

            instance.transform.Find("Footer/Label-Name").GetComponent<TextMeshProUGUI>().text = spec.DisplayName;
            SetPrivate(instance.transform.Find("Footer/Button-Buy").GetComponent<IAPBuyButton>(),
                "productId", spec.ProductId);

            var badge = instance.transform.Find("Badge");
            badge.gameObject.SetActive(spec.Badge != null);
            if (spec.Badge != null) badge.Find("Label-Title").GetComponent<TextMeshProUGUI>().text = spec.Badge;
        }

        private static void GoldPanel(Transform parent)
        {
            var panel = Instantiate(GoldPanelPrefab, parent);

            for (int i = 0; i < GoldTiers.Length; i++)
            {
                string productId = GoldTiers[i];
                var reward = IAPCatalog.Rewards[productId];
                var tile = Instantiate(GoldTilePrefab, panel.transform);
                tile.name = "Tile-" + productId;
                ApplyPalette(tile.transform, GoldPalette);

                tile.transform.Find("Header/Label-Amount").GetComponent<TextMeshProUGUI>().text =
                    reward.gold.ToString("N0");
                tile.transform.Find("Art").GetComponent<Image>().sprite = Load<Sprite>(GoldTierArt[i]);
                SetPrivate(tile.transform.Find("Button-Buy").GetComponent<IAPBuyButton>(),
                    "productId", productId);
            }
        }

        // --- Kit palette --------------------------------------------------------------------

        /// <summary>
        /// Recolours a generated row into one of the kit's colour families, by copying the
        /// colours straight off that family's own prefabs. Nothing here invents a colour, so a
        /// card can only look like something CuteKawaiiGUIPack already ships.
        /// </summary>
        private static void ApplyPalette(Transform row, string palette)
        {
            var frameSource = Load<GameObject>(string.Format(FramePath, palette));
            CopyImageColours(frameSource.transform, row.Find("Frame"));

            var wellSource = Load<GameObject>(string.Format(WellPath, palette));
            foreach (var name in new[] { "Top/Powerups", "Top/Extras", "Top/Description", "Header" })
            {
                var well = row.Find(name);
                // The colour layers live on the well's Frame child, not on the well itself.
                if (well != null) CopyImageColours(wellSource.transform, well.Find("Frame"));
            }

            ApplyInk(row, InkOf(palette));
        }

        /// <summary>
        /// The family's text colour, taken from its Headline — the kit already picked a dark
        /// tone that reads on that family's pale card fill, so there is nothing to invent.
        /// </summary>
        private static Color InkOf(string palette)
        {
            var headline = Load<GameObject>(string.Format(HeadlinePath, palette));
            return headline.transform.Find("Title-Background/Label-Title")
                .GetComponent<TextMeshProUGUI>().color;
        }

        /// <summary>
        /// Darkens every label on a card. White survives only where it sits on saturated art —
        /// the ∞ on the heart, the badge flash, and the green buy button — because everywhere
        /// else the kit's card fills are pale enough that white text disappears.
        /// </summary>
        private static void ApplyInk(Transform row, Color ink)
        {
            foreach (var label in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name == "Overlay") continue;
                if (IsUnder(label.transform, row, "Badge", "Button-Buy")) continue;

                label.color = ink;
            }
        }

        private static bool IsUnder(Transform node, Transform stopAt, params string[] ancestors)
        {
            for (var cur = node; cur != null && cur != stopAt; cur = cur.parent)
            {
                if (ancestors.Contains(cur.name)) return true;
            }

            return false;
        }

        /// <summary>Copies each source child's Image colour onto the same-named child of the target.</summary>
        private static void CopyImageColours(Transform source, Transform target)
        {
            foreach (Transform child in source)
            {
                var image = child.GetComponent<Image>();
                var match = target.Find(child.name);
                if (image == null || match == null) continue;

                var targetImage = match.GetComponent<Image>();
                if (targetImage != null) targetImage.color = image.color;
            }
        }

        // --- Building blocks ------------------------------------------------------------------

        /// <summary>A plain copy of a kit prefab — not a nested instance, so the saved row stands alone.</summary>
        private static GameObject CloneKit(string path, Transform parent)
        {
            var clone = Object.Instantiate(Load<GameObject>(path), parent, false);
            clone.name = clone.name.Replace("(Clone)", string.Empty);
            return clone;
        }

        /// <summary>
        /// The kit's five-layer card: drop shadow, outline, shading, fill and a curved highlight.
        /// Every row prefab in CuteKawaiiGUIPack is built on one, which is most of why they all
        /// look like they belong together.
        /// </summary>
        private static void AddFrame(Transform card, string palette)
        {
            var frame = CloneKit(string.Format(FramePath, palette), card);
            frame.name = "Frame";
            Stretch((RectTransform)frame.transform);

            // Decoration only; the buy button underneath must still receive the clicks.
            foreach (var image in frame.GetComponentsInChildren<Image>(true)) image.raycastTarget = false;
        }

        /// <summary>The kit's two-layer inset panel — an outline with a pale fill.</summary>
        private static RectTransform NewWell(string name, Transform parent, string palette)
        {
            var well = NewRect(name, parent);
            var frame = CloneKit(string.Format(WellPath, palette), well);
            frame.name = "Frame";
            Stretch((RectTransform)frame.transform);

            // The frame is decoration; chips and labels sit above it and must take the clicks.
            foreach (var image in frame.GetComponentsInChildren<Image>(true)) image.raycastTarget = false;
            return well;
        }

        private static RectTransform NewChip(string name, Transform parent)
        {
            var chip = NewRect(name, parent);
            chip.sizeDelta = new Vector2(110f, 110f);

            var icon = NewImage("Icon", chip, null);
            Stretch(icon);
            icon.offsetMin = new Vector2(6f, 6f);
            icon.offsetMax = new Vector2(-6f, -6f);
            icon.GetComponent<Image>().preserveAspect = true;

            // Sits slightly high: the heart's point drags its visual centre down, so a glyph
            // centred on the rect reads as sitting too low inside it.
            var overlay = NewLabel("Overlay", icon, "∞", 64f, TextAlignmentOptions.Center);
            Stretch(overlay);
            overlay.offsetMin = new Vector2(0f, 12f);
            overlay.gameObject.SetActive(false);

            Stretch(NewLabel("Label-Count", chip, "0", 36f, TextAlignmentOptions.BottomRight));
            return chip;
        }

        private static void FillChip(Transform chip, string iconPath, string caption, string overlay)
        {
            var icon = chip.Find("Icon");
            icon.GetComponent<Image>().sprite = Load<Sprite>(iconPath);

            var mark = icon.Find("Overlay");
            mark.gameObject.SetActive(overlay != null);
            if (overlay != null) mark.GetComponent<TextMeshProUGUI>().text = overlay;

            chip.Find("Label-Count").GetComponent<TextMeshProUGUI>().text = caption;
        }

        /// <summary>
        /// The kit's corner flash (as used for "New" / "Sale" on <c>Shop-Item</c>), grown to fit
        /// a longer word and forced to the Red family so it stands off whatever colour the card
        /// underneath happens to be.
        /// </summary>
        private static RectTransform NewBadge(Transform parent)
        {
            var badge = (RectTransform)CloneKit(BadgePath, parent).transform;
            badge.name = "Badge";
            badge.anchorMin = new Vector2(1f, 1f);
            badge.anchorMax = new Vector2(1f, 1f);
            badge.pivot = new Vector2(1f, 1f);
            badge.sizeDelta = new Vector2(250f, 80f);
            badge.anchoredPosition = new Vector2(-18f, -10f);

            var outline = badge.GetChild(0);
            outline.name = "Frame";
            CopyImageColours(Load<GameObject>(string.Format(WellPath, "Red")).transform, outline);

            var label = badge.Find("Label-Title").GetComponent<TextMeshProUGUI>();
            label.text = "BADGE";
            label.fontSize = 34f;
            label.color = InkOf("Red");
            return badge;
        }

        /// <summary>
        /// The kit's green button, turned into an <see cref="IAPBuyButton"/>. The price is left
        /// as the component's placeholder — only the store knows it.
        /// </summary>
        private static RectTransform NewBuyButton(Transform parent, Vector2 rightInset)
        {
            var instance = CloneKit(BuyButtonPath, parent);
            instance.name = "Button-Buy";

            var rect = (RectTransform)instance.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(380f, 130f);
            rect.anchoredPosition = rightInset;
            rect.localScale = Vector3.one;

            // The kit prefab ships with its own name on the label. Overwrite it with the same
            // placeholder IAPBuyButton uses, so a store that never answers reads as "loading"
            // rather than as the word "Green".
            var label = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            label.text = "…";

            SetPrivate(instance.AddComponent<IAPBuyButton>(), "priceText", label);
            return rect;
        }

        // --- Small helpers ---------------------------------------------------------------

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform NewImage(string name, Transform parent, Sprite sprite)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;

            // Only the panel shapes carry a 9-slice border; icons and coin art must stay Simple
            // or Unity stretches them from a border of zero and warns about it every frame.
            image.type = sprite != null && sprite.border != Vector4.zero
                ? Image.Type.Sliced
                : Image.Type.Simple;

            return rect;
        }

        private static RectTransform NewLabel(string name, Transform parent, string text,
            float size, TextAlignmentOptions alignment)
        {
            var rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Load<TMP_FontAsset>(FontPath);
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = alignment;
            label.raycastTarget = false;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Pins a row of the vertical layout to an exact height.</summary>
        private static void AddFixedHeight(RectTransform rect, float height)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
        }

        /// <summary>Sizes a column of a row's horizontal layout.</summary>
        private static void AddFlexibleWidth(RectTransform rect, float preferredWidth, float flexibleWidth)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth > 0f) element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
        }

        private static GameObject Instantiate(string prefabPath, Transform parent)
        {
            var prefab = Load<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateFolders(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogError("[ShopTabBuilder] Missing asset: " + path);
            return asset;
        }

        /// <summary>
        /// Fills in a [SerializeField] on a generated component. The shop UI is built by this
        /// tool, so these would otherwise have to be dragged in by hand for every card.
        /// </summary>
        private static void SetPrivate(Component target, string field, object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[ShopTabBuilder] " + target.GetType().Name + " has no field '" + field + "'");
                return;
            }

            if (value is string s) property.stringValue = s;
            else if (value is float f) property.floatValue = f;
            else property.objectReferenceValue = (Object)value;

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
