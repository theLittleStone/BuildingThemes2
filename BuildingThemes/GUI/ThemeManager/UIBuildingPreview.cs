using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BuildingThemes.GUI
{
    public class UIBuildingPreview : UIPanel
    {
        private BuildingItem m_item;
        private BuildingInfo m_renderPrefab;

        private UITextureSprite m_preview;
        private UISprite m_noPreview;
        private PreviewRenderer m_previewRenderer;

        private UILabel m_buildingName;
        private UISprite m_categoryIcon;

        private UILabel m_level;
        private UILabel m_height;
        private UILabel m_size;
        private UILabel m_origin;
        private UILabel m_wallToWall;
        private UILabel m_corner;

        private static readonly Color32 OriginTextColor = new Color32(180, 180, 180, 255);
        private static readonly Color32 WallToWallTextColor = new Color32(140, 190, 230, 255);
        private static readonly Color32 CornerTextColor = new Color32(235, 180, 110, 255);

        public override void Start()
        {
            base.Start();

            backgroundSprite = "GenericPanel";

            // Preview
            m_preview = AddUIComponent<UITextureSprite>();
            m_preview.size = size;
            m_preview.relativePosition = Vector3.zero;

            m_noPreview = AddUIComponent<UISprite>();
            m_noPreview.spriteName = "Niet";
            m_noPreview.relativePosition = new Vector3((width - m_noPreview.spriteInfo.width) / 2, (height - m_noPreview.spriteInfo.height) / 2);

            m_previewRenderer = gameObject.AddComponent<PreviewRenderer>();
            m_previewRenderer.size = m_preview.size * 2; // Twice the size for anti-aliasing

            eventMouseDown += (c, p) =>
            {
                eventMouseMove += RotateCamera;
            };

            eventMouseUp += (c, p) =>
            {
                eventMouseMove -= RotateCamera;
            };

            eventMouseWheel += (c, p) =>
            {
                m_previewRenderer.zoom -= Mathf.Sign(p.wheelDelta) * 0.25f;
                RenderPreview();
            };

            // Name
            m_buildingName = AddUIComponent<UILabel>();
            m_buildingName.textScale = 0.8f;
            m_buildingName.useDropShadow = true;
            m_buildingName.dropShadowColor = new Color32(80, 80, 80, 255);
            m_buildingName.dropShadowOffset = new Vector2(2, -2);
            m_buildingName.text = "Name";
            m_buildingName.isVisible = false;
            m_buildingName.relativePosition = new Vector3(5, 10);

            // Origin label — DLC / workshop source, shown below the building name
            m_origin = AddUIComponent<UILabel>();
            m_origin.textScale = 0.65f;
            m_origin.textColor = OriginTextColor;
            m_origin.useDropShadow = true;
            m_origin.dropShadowColor = new Color32(0, 0, 0, 200);
            m_origin.dropShadowOffset = new Vector2(1, -1);
            m_origin.autoSize = false;
            m_origin.height = 14f;
            m_origin.isVisible = false;

            // Wall-to-wall label — BT2's own classification, shown just below the origin line
            m_wallToWall = AddUIComponent<UILabel>();
            m_wallToWall.textScale = 0.65f;
            m_wallToWall.textColor = WallToWallTextColor;
            m_wallToWall.useDropShadow = true;
            m_wallToWall.dropShadowColor = new Color32(0, 0, 0, 200);
            m_wallToWall.dropShadowOffset = new Vector2(1, -1);
            m_wallToWall.autoSize = false;
            m_wallToWall.height = 14f;
            m_wallToWall.isVisible = false;

            // Corner label — the building's actual zoning mode (CornerLeft/CornerRight). Only
            // buildings authored with corner zoning can fill corner lots; this makes that visible.
            m_corner = AddUIComponent<UILabel>();
            m_corner.textScale = 0.65f;
            m_corner.textColor = CornerTextColor;
            m_corner.useDropShadow = true;
            m_corner.dropShadowColor = new Color32(0, 0, 0, 200);
            m_corner.dropShadowOffset = new Vector2(1, -1);
            m_corner.autoSize = false;
            m_corner.height = 14f;
            m_corner.isVisible = false;

            // Category icon
            m_categoryIcon = AddUIComponent<UISprite>();
            m_categoryIcon.size = new Vector2(35, 35);
            m_categoryIcon.isVisible = false;
            m_categoryIcon.relativePosition = new Vector3(width - 37, 2);

            // Level
            m_level = AddUIComponent<UILabel>();
            m_level.textScale = 0.8f;
            m_level.useDropShadow = true;
            m_level.dropShadowColor = new Color32(80, 80, 80, 255);
            m_level.dropShadowOffset = new Vector2(2, -2);
            m_level.text = "Level";
            m_level.isVisible = false;
            m_level.relativePosition = new Vector3(5, height - 20);

            // Height
            m_height = AddUIComponent<UILabel>();
            m_height.textScale = 0.8f;
            m_height.useDropShadow = true;
            m_height.dropShadowColor = new Color32(80, 80, 80, 255);
            m_height.dropShadowOffset = new Vector2(2, -2);
            m_height.text = "Height";
            m_height.isVisible = false;
            m_height.relativePosition = new Vector3(5, height - 20);

            // Size
            m_size = AddUIComponent<UILabel>();
            m_size.textScale = 0.8f;
            m_size.useDropShadow = true;
            m_size.dropShadowColor = new Color32(80, 80, 80, 255);
            m_size.dropShadowOffset = new Vector2(2, -2);
            m_size.text = "Size";
            m_size.isVisible = false;
            m_size.relativePosition = new Vector3(width - 50, height - 20);
        }

        public void Show(BuildingItem item)
        {
            if (m_item == item) return;

            m_item = item;
            m_renderPrefab = (m_item == null) ? null : m_item.prefab;

            // Preview
            if (m_renderPrefab != null && m_renderPrefab.m_mesh != null)
            {
                m_previewRenderer.cameraRotation = 210f;
                m_previewRenderer.zoom = 4f;
                m_previewRenderer.mesh = m_renderPrefab.m_mesh;
                m_previewRenderer.material = m_renderPrefab.m_material;

                RenderPreview();

                m_preview.texture = m_previewRenderer.texture;

                m_noPreview.isVisible = false;
            }
            else
            {
                m_preview.texture = null;
                m_noPreview.isVisible = true;
            }

            m_buildingName.isVisible = false;
            m_origin.isVisible = false;
            m_wallToWall.isVisible = false;
            m_corner.isVisible = false;
            m_categoryIcon.isVisible = false;
            m_level.isVisible = false;
            m_height.isVisible = false;
            m_size.isVisible = false;

            if(item == null) return;

            // Name
            m_buildingName.isVisible = true;
            m_buildingName.text = m_item.displayName;
            UIUtils.TruncateLabel(m_buildingName, width - 45);
            m_buildingName.autoHeight = true;

            // Origin label (DLC / workshop source) — positioned just below the building name
            float infoBottom = m_buildingName.relativePosition.y + m_buildingName.height;
            string originText = BuildingItem.GetOriginTextForName(m_item.name);
            if (!string.IsNullOrEmpty(originText))
            {
                m_origin.text = originText;
                m_origin.width = width - 10;
                m_origin.isVisible = true;
                m_origin.relativePosition = new Vector3(5, infoBottom + 2);
                infoBottom = m_origin.relativePosition.y + m_origin.height;
            }

            // Wall-to-wall classification (BT2's own mesh-based catalog) — just below the origin
            if (m_item.isWallToWall)
            {
                m_wallToWall.text = "Wall to wall";
                m_wallToWall.width = width - 10;
                m_wallToWall.isVisible = true;
                m_wallToWall.relativePosition = new Vector3(5, infoBottom + 2);
                infoBottom = m_wallToWall.relativePosition.y + m_wallToWall.height;
            }

            // Corner zoning — only true CornerLeft/CornerRight assets can fill corner lots.
            // Shown so users can tell a real corner building from a straight one that merely
            // looks like a corner piece. (ZoningMode also has a NotZoning=3 value, which is NOT
            // a corner, so we check the two corner values explicitly rather than "!= Straight".)
            BuildingInfo.ZoningMode zm = m_item.prefab != null
                ? m_item.prefab.m_zoningMode : BuildingInfo.ZoningMode.Straight;
            if (zm == BuildingInfo.ZoningMode.CornerLeft || zm == BuildingInfo.ZoningMode.CornerRight)
            {
                m_corner.text = zm == BuildingInfo.ZoningMode.CornerLeft
                    ? "Corner (left)" : "Corner (right)";
                m_corner.width = width - 10;
                m_corner.isVisible = true;
                m_corner.relativePosition = new Vector3(5, infoBottom + 2);
            }

            // Category icon
            Category category = m_item.category;
            if (category != Category.None)
            {
                m_categoryIcon.atlas = UIUtils.GetAtlas(CategoryIcons.atlases[(int)category]);
                m_categoryIcon.spriteName = CategoryIcons.spriteNames[(int)category];
                m_categoryIcon.tooltip = CategoryIcons.tooltips[(int)category];
                m_categoryIcon.isVisible = true;
            }

            // Level and Height — stacked at bottom-left
            float bottomY = height - 20;
            float h = m_item.height;
            if (h >= 0)
            {
                m_height.text = Mathf.RoundToInt(h) + "m";
                m_height.isVisible = true;
                m_height.relativePosition = new Vector3(5, bottomY);
                bottomY -= 18;
            }
            if (m_item.level != 0)
            {
                m_level.text = "Level " + m_item.level;
                m_level.isVisible = true;
                m_level.relativePosition = new Vector3(5, bottomY);
            }

            // Size
            if (m_item.size != Vector2.zero)
            {
                m_size.text = m_item.sizeAsString;
                m_size.isVisible = true;

                m_size.autoSize = true;
                m_size.relativePosition = new Vector3(width - m_size.width - 7, height - 20);
            }
        }

        private void RenderPreview()
        {
            if (m_renderPrefab == null) return;

            if (m_renderPrefab.m_useColorVariations)
            {
                Color materialColor = m_renderPrefab.m_material.color;
                m_renderPrefab.m_material.color = m_renderPrefab.m_color0;
                m_previewRenderer.Render();
                m_renderPrefab.m_material.color = materialColor;
            }
            else
            {
                m_previewRenderer.Render();
            }
        }

        private void RotateCamera(UIComponent c, UIMouseEventParameter p)
        {
            m_previewRenderer.cameraRotation -= p.moveDelta.x / m_preview.width * 360f;
            RenderPreview();
        }
    }
}
