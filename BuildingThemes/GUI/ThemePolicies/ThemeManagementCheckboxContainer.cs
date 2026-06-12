using ColossalFramework;
using ColossalFramework.UI;

namespace BuildingThemes.GUI
{
    // This helper component updates the "Enable Theme Management" checkbox state in the policy panel every game tick.
    public class ThemeManagementCheckboxContainer : ToolsModifierControl
    {
        private UICheckBox m_Check;

        private static string TEXT_CITY => Localization.Get("POLICY_ENABLE_MANAGEMENT_CITY");
        private static string TEXT_DISTRICT => Localization.Get("POLICY_ENABLE_MANAGEMENT_DISTRICT");

        private void Start()
        {
            this.m_Check = base.GetComponent<UICheckBox>();
        }

        private void Update()
        {
            if (base.component.isVisible)
            {
                lock (m_Check)
                {
                    var districtId = ToolsModifierControl.policiesPanel.targetDistrict;

                    bool managed = BuildingThemesManager.instance.IsThemeManagementEnabled(districtId);

                    m_Check.text = districtId == 0 ? TEXT_CITY : TEXT_DISTRICT;

                    if (managed != this.m_Check.isChecked)
                    {
                        this.m_Check.isChecked = managed;
                    }
                }
            }
        }
    }
}