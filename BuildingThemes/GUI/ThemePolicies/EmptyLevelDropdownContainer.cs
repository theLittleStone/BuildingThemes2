using ColossalFramework.UI;

namespace BuildingThemes.GUI
{
    // Updates the "Empty level behavior" dropdown state every game tick so it reflects
    // the current district's setting and is only interactive when theme management is on.
    public class EmptyLevelDropdownContainer : ToolsModifierControl
    {
        private UIDropDown m_Dropdown;

        private void Start()
        {
            m_Dropdown = base.GetComponent<UIDropDown>();
        }

        private void Update()
        {
            if (m_Dropdown == null || !base.component.isVisible) return;

            var districtId = ToolsModifierControl.policiesPanel.targetDistrict;
            bool managed = BuildingThemesManager.instance.IsThemeManagementEnabled(districtId);

            m_Dropdown.isEnabled = managed;
            m_Dropdown.opacity = managed ? 1f : 0.5f;

            if (managed)
            {
                int val = (int)BuildingThemesManager.instance.GetDistrictEmptyLevelBehavior(districtId);
                if (m_Dropdown.selectedIndex != val)
                    m_Dropdown.selectedIndex = val;
            }
        }
    }
}
