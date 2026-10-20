using Blocktrotter.AppShell;
using Blocktrotter.Data;
using Blocktrotter.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Blocktrotter.Screens
{
    /// <summary>Scrollable grid of one world's levels: number, best stars, locked state.</summary>
    public sealed class LevelSelectScreen : UiScreen
    {
        private const int Columns = 5;
        private const float CellWidth = 172f;
        private const float CellHeight = 196f;
        private const float Spacing = 22f;
        private static readonly Vector2 ViewportSize = new Vector2(980, 1500);
        private const float ViewportY = -40f;

        private readonly int _world;

        /// <summary>Creates the grid for a 0-based world index.</summary>
        public LevelSelectScreen(int world) => _world = world;

        /// <inheritdoc/>
        public override ThemeDefinition Theme => App.ThemeOfWorld(_world);

        /// <inheritdoc/>
        protected override void Build()
        {
            BackButton(App.ShowWorlds);
            RectTransform title = UiFactory.Rect("Title", Root, new Vector2(0, 790), new Vector2(760, 120));
            UiFactory.Label(title, T(Theme.NameKey), 68, Theme.PanelColor);

            int start = App.WorldStart(_world), count = App.WorldSize(_world);
            int rows = Mathf.CeilToInt((float)count / Columns);

            // ScrollRect: viewport (clips) -> content (tall, anchored top).
            RectTransform viewport = UiFactory.Rect("Viewport", Root, new Vector2(0, ViewportY), ViewportSize);
            viewport.gameObject.AddComponent<RectMask2D>();
            UiFactory.Image(viewport, new Color(0, 0, 0, 0), raycast: true);   // catches drags between cells
            RectTransform content = UiFactory.Rect("Content", viewport, Vector2.zero,
                new Vector2(ViewportSize.x, rows * (CellHeight + Spacing) + Spacing));
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            float gridWidth = Columns * CellWidth + (Columns - 1) * Spacing;
            for (int i = 0; i < count; i++)
            {
                int row = i / Columns, col = i % Columns;
                var pos = new Vector2(-gridWidth / 2f + CellWidth / 2f + col * (CellWidth + Spacing),
                    -Spacing - CellHeight / 2f - row * (CellHeight + Spacing));
                BuildCell(content, start + i, pos);
            }

            // Show the frontier row near the top of the viewport.
            int frontierRow = Mathf.Clamp(App.Progress.FrontierLevel - start, 0, count - 1) / Columns;
            float maxScroll = Mathf.Max(0f, content.sizeDelta.y - ViewportSize.y);
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp((frontierRow - 1) * (CellHeight + Spacing), 0f, maxScroll));
        }

        private void BuildCell(RectTransform parent, int levelIndex, Vector2 position)
        {
            bool unlocked = App.Progress.IsUnlocked(levelIndex);
            bool frontier = levelIndex == App.Progress.FrontierLevel && App.Progress.GetStars(levelIndex) == 0;
            Color bg = frontier ? Theme.TargetColor : Theme.ButtonColor;

            RectTransform cell = UiFactory.Rect($"Level{levelIndex + 1}", parent, position, new Vector2(CellWidth, CellHeight));
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
            Image image = UiFactory.Image(cell, bg, raycast: true);
            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = unlocked;
            button.onClick.AddListener(() => App.PlayLevel(levelIndex));

            RectTransform number = UiFactory.Rect("Number", cell, new Vector2(0, 26), new Vector2(CellWidth, 100));
            UiFactory.Label(number, (levelIndex + 1).ToString(), 64, Theme.TextColor);

            Image[] stars = UiFactory.StarRow(cell, new Vector2(0, -58), 40f, 4f);
            UiFactory.SetStars(stars, App.Progress.GetStars(levelIndex), Theme.ExitColor, new Color(0f, 0f, 0f, 0.25f));

            if (!unlocked) cell.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
        }
    }
}
