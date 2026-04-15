using ColossalFramework.UI;

namespace BuildingThemes.GUI
{
    // Enables the "District Options" button only when theme management is on for the current district.
    public class DistrictOptionsButtonContainer : ToolsModifierControl
    {
        private UIButton m_Button;

        private void Start()
        {
            m_Button = base.GetComponent<UIButton>();
        }

        private void Update()
        {
            if (m_Button == null || !base.component.isVisible) return;

            var districtId = ToolsModifierControl.policiesPanel.targetDistrict;
            bool managed = BuildingThemesManager.instance.IsThemeManagementEnabled(districtId);

            m_Button.isEnabled = managed;
            m_Button.opacity = managed ? 1f : 0.5f;
        }
    }
}
