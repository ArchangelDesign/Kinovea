#region License
/*
Copyright � Joan Charmant 2011.
jcharmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.
*/
#endregion
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using Kinovea.Root.Languages;
using Kinovea.Root.Properties;
using Kinovea.ScreenManager;
using Kinovea.Services;
using System.Collections.Generic;
using Kinovea.ScreenManager.Languages;

namespace Kinovea.Root
{
    /// <summary>
    /// Description of PreferencePanelDrawings.
    /// </summary>
    public partial class PreferencePanelDrawings : UserControl, IPreferencePanel
    {
        #region IPreferencePanel properties
        public string Description
        {
            get { return description;}
        }
        public Bitmap Icon
        {
            get { return icon;}
        }
        public List<PreferenceTab> Tabs
        {
            get { return tabs; }
        }
        #endregion
        
        #region Members
        private string description;
        private Bitmap icon;
        private List<PreferenceTab> tabs = new List<PreferenceTab> { PreferenceTab.Drawings_General, PreferenceTab.Drawings_Persistence, PreferenceTab.Drawings_Tracking };
        private InfosFading defaultFading;
        private bool drawOnPlay;
        private bool enableFiltering;
        private bool enableCustomToolsDebug;
        private TrackingProfile trackingProfile;
        #endregion
        
        #region Construction & Initialization
        public PreferencePanelDrawings()
        {
            InitializeComponent();
            this.BackColor = Color.White;
            
            description = RootLang.dlgPreferences_tabDrawings;
            icon = Resources.drawings;
            
            ImportPreferences();
            InitPage();
        }

        public void OpenTab(PreferenceTab tab)
        {
            int index = tabs.IndexOf(tab);
            if (index < 0)
                return;

            tabSubPages.SelectedIndex = index;
        }

        public void Close()
        {
        }

        private void ImportPreferences()
        {
            drawOnPlay = PreferencesManager.PlayerPreferences.DrawOnPlay;
            enableFiltering = PreferencesManager.PlayerPreferences.EnableFiltering;
            enableCustomToolsDebug = PreferencesManager.PlayerPreferences.EnableCustomToolsDebugMode;
            defaultFading = new InfosFading(0, 0);
            trackingProfile = PreferencesManager.PlayerPreferences.TrackingProfile;
        }
        private void InitPage()
        {
            InitPageGeneral();
            InitPageOpacity();
            InitPageTracking();
        }
        private void InitPageGeneral()
        {
            tabGeneral.Text = RootLang.dlgPreferences_tabGeneral;
            chkDrawOnPlay.Text = RootLang.dlgPreferences_Drawings_chkDrawOnPlay;
            chkEnableFiltering.Text = RootLang.dlgPreferences_Drawings_chkEnableFiltering;
            chkCustomToolsDebug.Text = RootLang.dlgPreferences_Drawings_chkCustomToolsDebugMode;

            chkDrawOnPlay.Checked = drawOnPlay;
            chkEnableFiltering.Checked = enableFiltering;
            chkCustomToolsDebug.Checked = enableCustomToolsDebug;
        }
        private void InitPageOpacity()
        {
            tabPersistence.Text = ScreenManagerLang.Generic_Opacity;
            lblDefaultOpacity.Text = RootLang.dlgPreferences_Drawings_lblDefaultOpacity;
            chkAlwaysVisible.Text = RootLang.dlgPreferences_Drawings_rbAlwaysVisible;
            lblMax.Text = "Maximum opacity (%):";
            lblOpaque.Text = "Opaque duration (frames):";
            lblFading.Text = "Fading duration (frames):";

            chkAlwaysVisible.Checked = defaultFading.AlwaysVisible;
            nudMax.Value = (decimal)(defaultFading.MasterFactor * 100);
            nudOpaque.Value = (decimal)defaultFading.OpaqueFrames;
            nudFading.Value = (decimal)defaultFading.FadingFrames;
        }
        private void InitPageTracking()
        {
            tabTracking.Text = RootLang.dlgPreferences_Player_Tracking;
            lblDescription.Text = RootLang.dlgPreferences_Player_TrackingDescription;
            lblObjectWindow.Text = RootLang.dlgPreferences_Player_TrackingObjectWindow;
            lblSearchWindow.Text = RootLang.dlgPreferences_Player_TrackingSearchWindow;
            cmbBlockWindowUnit.Items.Add(RootLang.dlgPreferences_Player_TrackingPercentage);
            cmbBlockWindowUnit.Items.Add(RootLang.dlgPreferences_Player_TrackingPixels);
            cmbSearchWindowUnit.Items.Add(RootLang.dlgPreferences_Player_TrackingPercentage);
            cmbSearchWindowUnit.Items.Add(RootLang.dlgPreferences_Player_TrackingPixels);

            int blockWindowUnit = (int)trackingProfile.BlockWindowUnit;
            cmbBlockWindowUnit.SelectedIndex = blockWindowUnit < cmbBlockWindowUnit.Items.Count ? blockWindowUnit : 0;
            int searchWindowUnit = (int)trackingProfile.SearchWindowUnit;
            cmbSearchWindowUnit.SelectedIndex = searchWindowUnit < cmbSearchWindowUnit.Items.Count ? searchWindowUnit : 0;
            tbBlockWidth.Text = trackingProfile.BlockWindow.Width.ToString();
            tbBlockHeight.Text = trackingProfile.BlockWindow.Height.ToString();
            tbSearchWidth.Text = trackingProfile.SearchWindow.Width.ToString();
            tbSearchHeight.Text = trackingProfile.SearchWindow.Height.ToString();
        }
        #endregion

        #region Handlers
        #region General
        private void chkDrawOnPlay_CheckedChanged(object sender, EventArgs e)
        {
            drawOnPlay = chkDrawOnPlay.Checked;
        }
        private void chkEnableFiltering_CheckedChanged(object sender, EventArgs e)
        {
            enableFiltering = chkEnableFiltering.Checked;
        }
        private void chkCustomToolsDebug_CheckedChanged(object sender, EventArgs e)
        {
            enableCustomToolsDebug = chkCustomToolsDebug.Checked;
        }
        #endregion

        #region Opacity
        private void chkAlwaysVisible_CheckedChanged(object sender, EventArgs e)
        {
            defaultFading.AlwaysVisible = chkAlwaysVisible.Checked;
        }

        private void nudMax_ValueChanged(object sender, EventArgs e)
        {
            defaultFading.MasterFactor = (float)nudMax.Value / 100;
        }

        private void nudOpaque_ValueChanged(object sender, EventArgs e)
        {
            defaultFading.OpaqueFrames = (int)nudOpaque.Value;
        }

        private void nudFading_ValueChanged(object sender, EventArgs e)
        {
            defaultFading.FadingFrames = (int)nudFading.Value;
        }
        #endregion

        #region Tracking
        private void tbBlockWidth_TextChanged(object sender, EventArgs e)
        {
            int width;
            bool parsed = ExtractTrackerParameter(tbBlockWidth, out width);
            if (!parsed)
                return;

            trackingProfile.BlockWindow = new Size(width, trackingProfile.BlockWindow.Height);
        }

        private void tbBlockHeight_TextChanged(object sender, EventArgs e)
        {
            int height;
            bool parsed = ExtractTrackerParameter(tbBlockHeight, out height);
            if (!parsed)
                return;

            trackingProfile.BlockWindow = new Size(trackingProfile.BlockWindow.Width, height);
        }

        private void tbSearchWidth_TextChanged(object sender, EventArgs e)
        {
            int width;
            bool parsed = ExtractTrackerParameter(tbSearchWidth, out width);
            if (!parsed)
                return;

            trackingProfile.SearchWindow = new Size(width, trackingProfile.SearchWindow.Height);
        }

        private void tbSearchHeight_TextChanged(object sender, EventArgs e)
        {
            int height;
            bool parsed = ExtractTrackerParameter(tbSearchHeight, out height);
            if (!parsed)
                return;

            trackingProfile.SearchWindow = new Size(trackingProfile.SearchWindow.Width, height);
        }

        private bool ExtractTrackerParameter(TextBox tb, out int value)
        {
            int v;
            bool parsed = int.TryParse(tb.Text, out v);
            tbBlockWidth.ForeColor = parsed ? Color.Black : Color.Red;
            value = parsed ? v : 10;
            return parsed;
        }

        private void cmbBlockWindowUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            trackingProfile.BlockWindowUnit = (TrackerParameterUnit)cmbBlockWindowUnit.SelectedIndex;
        }

        private void cmbSearchWindowUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            trackingProfile.SearchWindowUnit = (TrackerParameterUnit)cmbSearchWindowUnit.SelectedIndex;
        }
        #endregion
        #endregion
        
        public void CommitChanges()
        {
            PreferencesManager.PlayerPreferences.DrawOnPlay = drawOnPlay;
            PreferencesManager.PlayerPreferences.EnableFiltering = enableFiltering;
            PreferencesManager.PlayerPreferences.EnableCustomToolsDebugMode = enableCustomToolsDebug;
            PreferencesManager.PlayerPreferences.DefaultFading.FromInfosFading(defaultFading);
            PreferencesManager.PlayerPreferences.TrackingProfile = trackingProfile;
        }
    }
}
