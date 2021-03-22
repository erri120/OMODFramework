using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using OblivionModManager.Scripting;

//TODO: cleanup
//ReSharper disable all
#pragma warning disable 414
#nullable disable

namespace OMODFramework.Scripting.ScriptHandlers.CSharp.InlinedScripts
{
    internal class DarkUIdDarN : IScript
    {
        internal const uint CRC = 0xF39B4EF8;
        
        
	public static IScriptFunctions sf;

	// 	--------------------- Start Install Config Section ------------------------------
	//
	//	These are the settings used to select the defaults in the lists.
	//	Setting these properly will enable you to pre-select what you want,
	//	and simply press "Ok" when the page comes up.
	//
	//	You'll find a detailed walkthrough here: http://darnified.net/forums/index.php?topic=14.0
	//	Look for the section called "Automating the omod install".
	//
	//	These lists are zero based, which means in a list of 6 items,
	//	nr. 1 = 0, and nr. 6 = 5.

	int defFonts		=	0;		// Default font size
	int defFont1		= 	0;		// Default Font 1 selection

	// Select which menus/options to pre-select (true = selected, false = not selected)

	bool[] defMenus = {		false,	// Breathmeter
							false,	// Info Menu
							false,	// Subtitles
							false,	// Inventory
							false,	// Dialog Menu
							false,	// Magic Menu
							false,	// Map Menu
							false,	// Spell Purchase Menu
							false,	// Container Menu
							false,	// Repair Menu
							false,	// Alchemy Menu
							false,	// Persuasion Menu
							false,	// Lockpick Menu
							false,	// Recharge Menu
							false,	// Training Menu
							false,	// Spellmaking Menu
							false,	// Enchantment Menu
							false,	// System Menus
							false,	// Quest Added Menu
							false,	// Barter Pack
							false,	// SleepWait Menu
							false,	// LevelUp Menu
							false,	// Chargen Pack
							false,	// TextEdit Menu
							false,	// Sigilstone Menu
							false,	// Skill Perk Menu
							false,	// Enchantment Setting Menu
							false,	// Message Menu
							false};	// Loading Menu

	bool[] defOptions = {			false,	// Custom Font 1
							false,	// KCAS-AF Menus
							false,	// Trollf Loading Screens
							false,	// Trollf Loading Screens - DarkUI Version
							false,	// DarkUI'd DarN Loading Screens
							false,	// Atmospheric Loading Screens
							false,  // Lighter Main Menu Text
							false,	// Classic Inventory
							false,	// Documentation
							false,	// Colored Local Map
							false};	// No Quest Added popup

	// Other settings:

	string pn			=	"";		// Player name.
	bool showReminder	=	true;	// Show the confirmation reminder at the end of install?

	// Setting skipdetection to true will allow installation of compatability options regardless of the mods you have installed.

	bool skipdetection	=	false;

	// Setting unattended to true will install everything silently using the settings in
	// the config section. I recommend you set all values and go through each page to verify
	// that all selections are ok before you do this.

	bool unattended 	=	false;

	// Example:
	//
	//	int defFonts		= 0;		(Normal)
	//	int defFont1		= 14;		(LaBrit_28)
	//	string pn			= "DarN";	(C'est moi :))
	//	bool showReminder	= false;	(We know to push 'yes' already...)
	//	bool skipdetection	= false;	(Filter out items I shouldn't be installing)
	//	bool unattended		= true; 	(Silent install)
	//
	//	------------------------ End Install Config Section -----------------------------

	bool updating = false, insstats = false, inslevelup = false, insinventory = false, insloading = false;
	string levelmode = "", installmode = "", duiVersion = "";

	string[] menuselections = new string[] {	"Breathmeter",
												"Info Menu",
												"Subtitles",
												"Inventory",
												"Dialog Menu",
												"Magic Menu",
												"Map Menu",
												"Spell Purchase Menu",
												"Container Menu",
												"Repair Menu",
												"Alchemy Menu",
												"Persuasion Menu",
												"Lockpick Menu",
												"Recharge Menu",
												"Training Menu",
												"Spellmaking Menu",
												"Enchantment Menu",
												"System Menus",
												"Quest Added Menu",
												"Barter Pack",
												"SleepWait Menu",
												"LevelUp Menu",
												"Chargen Pack",
												"TextEdit Menu",
												"Sigilstone Menu",
												"Skill Perk Menu",
												"Enchantment Setting Menu",
												"Message Menu",
												"Loading Menu"};

	string[] customoptions = new string[] {					"Custom Font 1",
												"KCAS-AF Menus",
												"Trollf Loading Screens",
												"Trollf Loading Screens - DarkUI Version",
												"DarkUI'd DarN Loading Screens",
												"Atmospheric Loading Screens",
												"Lighter Main Menu Text",
												"Classic Inventory",
												"Documentation",
												"Colored Local Map",
												"No Quest Added popup"};

	string[] font1selections = new string[] {	"Default",
												"!Sketchy_Times_36",
												"Dundalk_28",
												"Endor_20",
												"FantaisieArtistique_28",
												"Immortal_28",
												"Kingthings_Exeter_28",
												"Knights_Quest_36",
												"Morris_Roman_28",
												"Ringbearer_22",
												"Roosevelt_28",
												"Walshes_36",
												"Yataghan_24",
												"Kingthings_Calligraphica_36",
												"LaBrit_28",
												"Gushing_Meadow_28"};


	string[] fonts = new string[] { 			"Normal", "Large" };

	string[] initialdescs = new string[] {		"\n\n\n\nSelect the menus you wish to install in the list to the left.",
												"\n\n\n\nSelect the custom options you wish to install in the list to the left.",
												"\n\n\n\nSelect the font you want for font 1 in the list to the left.",
												"\n\n\n\nSelect the font size you wish to install in the list to the left."};

	// These arrays contain the esp/esm names for the mod detection function.
	// Edit these if there are omissions/errors, or just set skipdetection to true
	// to force the selections to be visible regardless.
	
	string[] trollf = {		"LoadingScreens.esp",
						"LoadingScreens-OOO.esp",
						"LoadingScreensSI.esp",
						"LoadingScreensAddOn.esp"};

	string[] trollfdark = {		"LoadingScreens.esp",
						"LoadingScreens-OOO.esp",
						"LoadingScreensSI.esp",
						"LoadingScreensAddOn.esp"};

	string[] kcas = {			"RealisticLeveling.esp",
						"Kobu's Character Advancement System.esp",
						"AFLevelMod.esp"};

	string[] als = {			"Atmospheric Loading Screens - No Text.esp",
						"Atmospheric Loading Screens - Original Text.esp",
						"Atmospheric Loading Screens - Random Quotes.esp"};
	
	string[] oxp = {	"Oblivion XP.esp"};

	string[] esps;
	
	string[] fontArr = { "Palatino Linotype", "Times New Roman", "Georgia" };


	Form spFrm, elFrm;
	ListBox lb;
	PictureBox pb, bgpb, epb, spb, apb;
	RichTextBox rtb;
	ToolStrip ts;
	Button bok, babort;
	Label versionLabel;
	Bitmap bm1, bm2, bm3, bm4, bm5, bm6, bm7, bm8;
	Page currentPage;

	enum Page { menus, options, font1select, fonts }


	void IScript.Execute(IScriptFunctions sf)
	{
		DarkUIdDarN.sf = sf;

		Version requiredver = new Version(1, 1, 12, 0);

		if (sf.GetOBMMVersion() < requiredver) {
			MessageBox.Show("This mod must be installed with Oblivion Mod Manager version " + requiredver.ToString() + " or higher to prevent script errors.",
							"Old OBMM version detected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			sf.FatalError();
			return;
		}
		sf.DontInstallAnyDataFiles();
		sf.DontInstallAnyPlugins();

		esps = sf.GetActiveEspNames();
		GetPlayerName();

		if (unattended) {
			SilentInstall();
			goto exitmsg;
		}
		CreateInitialDialog();

		switch (elFrm.ShowDialog()) {
			case System.Windows.Forms.DialogResult.OK:
				InstallEverything();
				CreateSelectPlentyDialog(Page.options);
				if (spFrm.ShowDialog() == System.Windows.Forms.DialogResult.Abort)
					goto abort;
				break;
			case System.Windows.Forms.DialogResult.Retry:
				CreateSelectPlentyDialog(Page.menus);
				if (spFrm.ShowDialog() == System.Windows.Forms.DialogResult.Abort)
					goto abort;
				break;
			default:				// Abort or Alt-F4
abort:			sf.FatalError();
				return;
		}

		// Final Message
exitmsg:	if (showReminder)
				MessageBox.Show("OBMM will now ask for confirmation as Oblivion.ini is modified to implement your choices.\nBe sure to click 'Yes' when asked. Hold down CTRL while pressing the button to choose yes/no to all.", "Modifying the ini", MessageBoxButtons.OK, MessageBoxIcon.Information);

	}//IScript.Execute


	// Initial form

	void CreateInitialDialog()
	{
		elFrm = sf.CreateCustomDialog();

		bgpb = new PictureBox();
		epb = new PictureBox();
		spb = new PictureBox();
		apb = new PictureBox();
		versionLabel = new Label();
		((System.ComponentModel.ISupportInitialize)(bgpb)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(epb)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(spb)).BeginInit();
		((System.ComponentModel.ISupportInitialize)(apb)).BeginInit();
		elFrm.SuspendLayout();
		//
		// bgpb
		//
		bgpb.Location = new Point(0, 0);
		bgpb.Name = "bgpb";
		bgpb.Size = new Size(370, 366);
		//
		// epb
		//
		epb.BackColor = Color.Transparent;
		epb.Location = new Point(50, 141);
		epb.Name = "epb";
		epb.Size = new Size(180, 30);
		epb.Click += new EventHandler(this.epb_Click);
		epb.MouseEnter += new EventHandler(this.epb_MouseEnter);
		epb.MouseLeave += new EventHandler(this.epb_MouseLeave);
		epb.Cursor = Cursors.Hand;
		//
		// spb
		//
		spb.BackColor = Color.Transparent;
		spb.Location = new Point(50, 177);
		spb.Name = "spb";
		spb.Size = new Size(180, 30);
		spb.Click += new EventHandler(this.spb_Click);
		spb.MouseEnter += new EventHandler(this.spb_MouseEnter);
		spb.MouseLeave += new EventHandler(this.spb_MouseLeave);
		spb.Cursor = Cursors.Hand;
		//
		// apb
		//
		apb.BackColor = Color.Transparent;
		apb.Location = new Point(50, 213);
		apb.Name = "apb";
		apb.Size = new Size(180, 30);
		apb.Click += new EventHandler(this.apb_Click);
		apb.MouseEnter += new EventHandler(this.apb_MouseEnter);
		apb.MouseLeave += new EventHandler(this.apb_MouseLeave);
		apb.Cursor = Cursors.Hand;
		//
		// versionLabel
		//
		versionLabel.AutoSize = true;
		versionLabel.BackColor = System.Drawing.Color.Transparent;
		versionLabel.Font = SetFont(8.25F, FontStyle.Bold);
		versionLabel.ForeColor = System.Drawing.Color.SaddleBrown;
		versionLabel.Location = new System.Drawing.Point(14, 336);
		versionLabel.Name = "versionLabel";
		versionLabel.TabIndex = 4;
		//
		// elForm
		//
		elFrm.AutoScaleDimensions = new SizeF(6F, 13F);
		elFrm.AutoScaleMode = AutoScaleMode.Font;
		elFrm.ClientSize = new Size(370, 366);
		elFrm.Controls.Add(versionLabel);
		elFrm.Controls.Add(apb);
		elFrm.Controls.Add(spb);
		elFrm.Controls.Add(epb);
		elFrm.Controls.Add(bgpb);
		elFrm.FormBorderStyle = FormBorderStyle.None;
		elFrm.Name = "elForm";
		elFrm.Text = "DarNified UI";
		elFrm.StartPosition = FormStartPosition.CenterScreen;
		elFrm.FormClosed += new FormClosedEventHandler(this.elFrm_FormClosed);
		((System.ComponentModel.ISupportInitialize)(bgpb)).EndInit();
		((System.ComponentModel.ISupportInitialize)(epb)).EndInit();
		((System.ComponentModel.ISupportInitialize)(spb)).EndInit();
		((System.ComponentModel.ISupportInitialize)(apb)).EndInit();
		elFrm.ResumeLayout(false);
		elFrm.PerformLayout();

		bm1 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\everything1.png")));
		bm2 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\everything2.png")));
		bm3 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\select1.png")));
		bm4 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\select2.png")));
		bm5 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\abort1.png")));
		bm6 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\abort2.png")));
		bm7 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\DUI_Install1bg.jpg")));

		epb.Parent = bgpb;
		spb.Parent = bgpb;
		apb.Parent = bgpb;
		versionLabel.Parent = bgpb;
		bgpb.Image = bm7;
		epb.Image = bm1;
		spb.Image = bm3;
		apb.Image = bm5;

		// Get version nr from xml
		byte[] ba = sf.ReadDataFile("menus\\options\\main_menu.xml");

		MemoryStream ms = new MemoryStream(ba);
		XmlTextReader xtr = new XmlTextReader(ms);

		xtr.MoveToContent();
		xtr.ReadToFollowing("_duiv");
		duiVersion = xtr.ReadString();
		xtr.Close();
		ms.Close();

		versionLabel.Text = duiVersion.Substring(14);
	}

	// Event handlers for initial form

	private void epb_MouseEnter(object sender, EventArgs e)
	{
		epb.Image = bm2;
	}

	private void epb_MouseLeave(object sender, EventArgs e)
	{
		epb.Image = bm1;
	}

	private void spb_MouseEnter(object sender, EventArgs e)
	{
		spb.Image = bm4;
	}

	private void spb_MouseLeave(object sender, EventArgs e)
	{
		spb.Image = bm3;
	}

	private void apb_MouseEnter(object sender, EventArgs e)
	{
		apb.Image = bm6;
	}

	private void apb_MouseLeave(object sender, EventArgs e)
	{
		apb.Image = bm5;
	}

	private void apb_Click(object sender, EventArgs e)
	{
		elFrm.Close();
	}

	private void epb_Click(object sender, EventArgs e)
	{
		elFrm.DialogResult = System.Windows.Forms.DialogResult.OK;
		elFrm.Close();
	}

	private void spb_Click(object sender, EventArgs e)
	{
		elFrm.DialogResult = System.Windows.Forms.DialogResult.Retry;
		elFrm.Close();
	}

	private void elFrm_FormClosed(object sender, FormClosedEventArgs e)
	{
		bm1.Dispose();
		bm2.Dispose();
		bm3.Dispose();
		bm4.Dispose();
		bm5.Dispose();
		bm6.Dispose();
		bm7.Dispose();
	}

	// Select form

	void CreateSelectPlentyDialog(Page mode)
	{
		spFrm = sf.CreateCustomDialog();

		bm8 = new Bitmap(new MemoryStream(sf.ReadDataFile("installfiles\\ui\\dui_logo.png")));

		lb = new ListBox();
		bok = new Button();
		babort = new Button();
		pb = new PictureBox();
		rtb = new RichTextBox();
		ts = new ToolStrip();
		ToolStripButton tsbSelectAll = new ToolStripButton();
		ToolStripButton tsbSelectNone = new ToolStripButton();
		ToolStripButton tsbInvert = new ToolStripButton();
		((System.ComponentModel.ISupportInitialize)(pb)).BeginInit();
		ts.SuspendLayout();
		spFrm.SuspendLayout();
		//
		// lbComponents
		//
		lb.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left)));
		lb.Font = SetFont(8.25F, FontStyle.Bold);
		lb.ItemHeight = 16;
		lb.Location = new System.Drawing.Point(9, 9);
		lb.Margin = new Padding(0);
		lb.Name = "lb";
		lb.Size = new Size(272, 500);
		lb.TabIndex = 0;
		if (!unattended) {
			lb.SelectedIndexChanged += new EventHandler(this.lb_SelectedIndexChanged);
			lb.SizeChanged += new EventHandler(this.lb_SizeChanged);
		}
		//
		// btnOk
		//
		bok.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
		bok.Location = new Point(598, 522);
		bok.Margin = new Padding(3, 4, 3, 4);
		bok.Name = "bok";
		bok.Size = new Size(87, 30);
		bok.TabIndex = 1;
		bok.Text = "&Ok";
		bok.UseVisualStyleBackColor = true;
		bok.Click += new EventHandler(this.bok_Click);
		bok.Enabled = false;
		//
		// btnAbort
		//
		babort.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
		babort.DialogResult = System.Windows.Forms.DialogResult.Abort;
		babort.Location = new Point(691, 522);
		babort.Margin = new Padding(3, 4, 3, 4);
		babort.Name = "babort";
		babort.Size = new Size(87, 30);
		babort.TabIndex = 2;
		babort.Text = "&Abort";
		babort.UseVisualStyleBackColor = true;
		//
		// pb
		//
		pb.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
		pb.Location = new Point(284, 9);
		pb.Margin = new Padding(3, 4, 3, 4);
		pb.Name = "pb";
		pb.Size = new Size(496, 311);
		pb.SizeMode = PictureBoxSizeMode.Zoom;
		pb.Click += new EventHandler(this.pb_Click);
		pb.Cursor = Cursors.Hand;
		//
		// rtbDesription
		//
		rtb.Anchor = ((AnchorStyles)((((AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
		rtb.BackColor = SystemColors.Window;
		rtb.Cursor = Cursors.Default;
		rtb.Location = new Point(284, 323);
		rtb.Name = "rtb";
		rtb.ReadOnly = true;
		rtb.Size = new Size(496, 186);
		rtb.TabStop = false;
		rtb.SelectionAlignment = HorizontalAlignment.Center;
		rtb.WordWrap = true;
		//
		// ts
		//
		ts.LayoutStyle = ToolStripLayoutStyle.Flow;
		ts.Font = SetFont(8.25F, FontStyle.Regular);
		ts.Dock = DockStyle.None;
		ts.Items.AddRange(new ToolStripItem[] { tsbSelectAll, tsbSelectNone, tsbInvert });
		ts.Location = new Point(9, 509);
		ts.Name = "ts";
		ts.Size = new Size(211, 25);
		ts.Visible = true;
		//
		// tsbSelectAll
		//
		tsbSelectAll.Name = "tsbSelectAll";
		tsbSelectAll.Size = new Size(54, 22);
		tsbSelectAll.Text = "A&ll";
		tsbSelectAll.ToolTipText = "Select all entries";
		tsbSelectAll.Click += new EventHandler(this.tsbSelect_Click);
		//
		// tsbSelectNone
		//
		tsbSelectNone.Name = "tsbSelectNone";
		tsbSelectNone.Size = new Size(68, 22);
		tsbSelectNone.Text = "&None";
		tsbSelectNone.ToolTipText = "Deselect all entries";
		tsbSelectNone.Click += new EventHandler(this.tsbSelect_Click);
		//
		// tsbInvert
		//
		tsbInvert.Name = "tsbInvert";
		tsbInvert.Size = new Size(86, 22);
		tsbInvert.Text = "&Invert";
		tsbInvert.ToolTipText = "Select all unselected entries and deselect all selected ones";
		tsbInvert.Click += new EventHandler(this.tsbSelect_Click);
		//
		// SelectPlentyDialog
		//
		spFrm.AutoScaleDimensions = new SizeF(7F, 17F);
		spFrm.AutoScaleMode = AutoScaleMode.Font;
		spFrm.ClientSize = new Size(792, 566);
		spFrm.MinimumSize = new Size(800, 600);
		spFrm.Controls.Add(rtb);
		spFrm.Controls.Add(pb);
		spFrm.Controls.Add(babort);
		spFrm.Controls.Add(bok);
		spFrm.Controls.Add(lb);
		spFrm.Controls.Add(ts);
		spFrm.ControlBox = false;
		spFrm.Font = SetFont(9F, FontStyle.Regular);
		spFrm.Margin = new Padding(3, 4, 3, 4);
		spFrm.StartPosition = FormStartPosition.CenterScreen;
		spFrm.Name = "SelectPlentyDialog";
		spFrm.Text = duiVersion + " Install";
		spFrm.FormClosed += new FormClosedEventHandler(this.spFrm_FormClosed);

		InitPage(mode);
		ResetInfo();

		((System.ComponentModel.ISupportInitialize)(pb)).EndInit();
		ts.BringToFront(); //helps flicker
		ts.ResumeLayout(false);
		ts.PerformLayout();
		spFrm.ResumeLayout(false);
		spFrm.PerformLayout();
	}

	// Event Handlers for Select form

	private void pb_Click(object sender, EventArgs e)
	{
		if ((string) pb.Tag != "")
			sf.DisplayImage((string)pb.Tag, lb.Text);
	}

	private void bok_Click(object sender, EventArgs e)
	{
		switch (currentPage) {
			case Page.menus:
				InstallSelected(lb.SelectedItems);
				InitPage(Page.options);
			break;
			case Page.options:
				InstallCustomOptions(lb.SelectedItems);

				if (lb.SelectedItems.Contains("Custom Font 1"))
					InitPage(Page.font1select);
				else InitPage(Page.fonts);
			break;
			case Page.font1select:
				InstallFont1(lb.Text);
				InitPage(Page.fonts);
			break;
			case Page.fonts:
				InstallFonts(lb.Text);
				spFrm.Close();
			break;
		}
		ResetInfo();
		ts.Visible = (lb.SelectionMode != SelectionMode.One);
	}

	private void lb_SelectedIndexChanged(object sender, EventArgs e)
	{
		if ((lb.SelectedItems.Count > 0) && !updating)
        {
        	string[] descpath = { "" }, previewpath = { "" };

			switch (currentPage) {
				case Page.font1select:
					previewpath[0] = "installfiles\\previews\\custom font 1.png";
					descpath[0] = "installfiles\\descriptions\\custom font 1_2.rtf";
				break;
				default:
					previewpath = sf.GetDataFiles("installfiles\\previews", lb.Text + ".*", false);
					descpath = sf.GetDataFiles("installfiles\\descriptions", lb.Text + ".rtf", false);
				break;
			}
			if (descpath.Length > 0) {
				byte[] data = sf.ReadDataFile(descpath[0]);
				char[] str = new char[data.Length];

				for (int i = 0; i < data.Length; i++)
					str[i] = (char)data[i];

				StringWriter sw = new StringWriter();
				sw.Write(str);
				rtb.Rtf = sw.ToString();
				sw.Close();
				rtb.SelectionStart = 0;
				rtb.ScrollToCaret();
			}
			else {
				rtb.Text = "\n\n\n\nDescription not implemented";
			}
			if (previewpath.Length > 0) {
				MemoryStream ms = new MemoryStream(sf.ReadDataFile(previewpath[0]));
				pb.Image = Image.FromStream(ms);
				pb.Tag = previewpath[0];
				ms.Close();
			}
			else {
				pb.Image = bm8;
				pb.Tag = "";
			}
		}
		else if (lb.SelectedItems.Count == 0) {
			ResetInfo();
		}
		bok.Enabled = (lb.SelectedItems.Count > 0) || (currentPage == Page.options);
	}

	private void lb_SizeChanged(object sender, EventArgs e)
	{
		ts.Top = lb.Bottom;
	}

	private void tsbSelect_Click(object sender, EventArgs e)
	{
		updating = true;
		lb.BeginUpdate();
		int currind = lb.TopIndex;

		switch (((ToolStripButton)sender).Name) {
			case "tsbSelectAll":
				for (int i = 0; i < lb.Items.Count; i++)
					lb.SetSelected(i, true);
				break;
			case "tsbSelectNone":
				lb.ClearSelected();
				break;
			case "tsbInvert":
				for (int i = 0; i < lb.Items.Count; i++)
					lb.SetSelected(i, !lb.GetSelected(i));
				break;
		}
		lb.TopIndex = currind;
		lb.EndUpdate();
		updating = false;
	}

	private void spFrm_FormClosed(object sender, FormClosedEventArgs e)
	{
		bm8.Dispose();
	}

	// Install functions

	private void InstallEverything()
	{
		sf.InstallDataFolder("textures", true);
		sf.InstallDataFolder("menus", true);
		if (ModActive(oxp)) {
			sf.CancelDataFileCopy("menus\\main\\stats_menu.xml");
			sf.CancelDataFileCopy("menus\\prefabs\\darn\\stats_config.xml");
			sf.CancelDataFileCopy("menus\\levelup_menu.xml");
		}
		sf.InstallDataFolder("meshes", true);
		insstats = true;
		inslevelup = true;
		insinventory = true;
		insloading = true;
	}

	private void InstallSelected(ListBox.SelectedObjectCollection lbItems)
	{
		// shared components
		sf.InstallDataFolder("menus\\prefabs\\darn", true);
		if (ModActive(oxp))
			sf.CancelDataFileCopy("menus\\prefabs\\darn\\stats_config.xml");
		sf.InstallDataFolder("textures\\menus\\darn", true);
		sf.InstallDataFolder("meshes\\Menus\\darn", true);
		sf.InstallDataFile("menus\\main\\quickkeys_menu.xml");
		sf.InstallDataFile("menus\\prefabs\\darn\\fill_bar.xml");
		sf.InstallDataFile("menus\\options\\credits_menu.xml");
		sf.InstallDataFolder("textures\\menus\\stats", true);
		sf.InstallDataFolder("textures\\menus50\\stats", true);
		sf.InstallDataFolder("textures\\menus80\\stats", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\hud", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\icons", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\stats", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\book", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\focus", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\genericbackground", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\icons", true);
		sf.InstallDataFolder("textures\\darkui\\menus\\shared", true);
		sf.InstallDataFile("textures\\darkui\\menus\\loading\\loading_save_center_folddui.dds");
		sf.InstallDataFile("textures\\darkui\\menus\\loading\\loading_save_wide_framedui.dds");
		sf.InstallDataFile("textures\\darkui\\menus\\loading\\loading_save_linesdui.dds");
		sf.InstallDataFile("menus\\main\\hud_main_menu.xml");			// hud
		sf.InstallDataFile("menus\\main\\hud_reticle.xml");
		sf.InstallDataFile("menus\\book_menu.xml");

		if (!ModActive(oxp))
			sf.InstallDataFile("menus\\main\\stats_menu.xml");			// Oblivion XP Levelling

		insstats = true;

		foreach (Object opt in lbItems) {
			switch (opt.ToString()) {
				case "Breathmeter":
					sf.InstallDataFile("menus\\breath_meter_menu.xml");
					break;
				case "Info Menu":
					sf.InstallDataFile("menus\\main\\hud_info_menu.xml");
					break;
				case "Subtitles":
					sf.InstallDataFile("menus\\main\\hud_subtitle_menu.xml");
					break;
				case "Inventory":
					sf.InstallDataFile("menus\\main\\inventory_menu.xml");
					sf.InstallDataFile("menus\\main\\magic_popup_menu.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\inventory", true);
					insinventory = true;
					break;
				case "Dialog Menu":
					sf.InstallDataFile("menus\\dialog\\dialog_menu.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\dialog", true);
					break;
				case "Magic Menu":
					sf.InstallDataFile("menus\\main\\magic_menu.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\magic", true);
					break;
				case "Map Menu":
					sf.InstallDataFile("menus\\main\\map_menu.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\map", true);
					break;
				case "Spell Purchase Menu":
					sf.InstallDataFile("menus\\dialog\\spell_purchase.xml");
					break;
				case "Container Menu":
					sf.InstallDataFile("menus\\container_menu.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\container", true);
					break;
				case "Repair Menu":
					sf.InstallDataFile("menus\\repair_menu.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\armorrepair", true);
					break;
				case "Alchemy Menu":
					sf.InstallDataFile("menus\\dialog\\alchemy.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\alchemy", true);
					break;
				case "Persuasion Menu":
					sf.InstallDataFile("menus\\dialog\\persuasion_menu.xml");
					break;
				case "Lockpick Menu":
					sf.InstallDataFile("menus\\lockpick_menu.xml");
					break;
				case "Recharge Menu":
					sf.InstallDataFile("menus\\recharge_menu.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\recharge", true);
					break;
				case "Training Menu":
					sf.InstallDataFile("menus\\training_menu.xml");
					break;
				case "Spellmaking Menu":
					sf.InstallDataFile("menus\\dialog\\spellmaking.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\spellmaking", true);
					break;
				case "Enchantment Menu":
					sf.InstallDataFile("menus\\dialog\\enchantment.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\enchanting", true);
					break;
				case "System Menus":
					sf.InstallDataFolder("menus\\options", true);
					break;
				case "Quest Added Menu":
					sf.InstallDataFile("menus\\generic\\quest_added.xml");
					break;
				case "Barter Pack":
					sf.InstallDataFile("menus\\negotiate_menu.xml");
					sf.InstallDataFile("menus\\quantity_menu.xml");
					break;
				case "SleepWait Menu":
					sf.InstallDataFile("menus\\sleep_wait_menu.xml");
					break;
				case "LevelUp Menu":
					sf.InstallDataFile("menus\\levelup_menu.xml");
					inslevelup = true;
					break;
				case "Chargen Pack":
					sf.InstallDataFolder("menus\\chargen", true);
					break;
				case "TextEdit Menu":
					sf.InstallDataFile("menus\\dialog\\texteditmenu.xml");
					break;
				case "Sigilstone Menu":
					sf.InstallDataFile("menus\\dialog\\sigilstone.xml");
					sf.InstallDataFolder("textures\\darkui\\menus\\enchanting", true);
					break;
				case "Skill Perk Menu":
					sf.InstallDataFile("menus\\generic\\skill_perk.xml");
					break;
				case "Enchantment Setting Menu":
					sf.InstallDataFile("menus\\dialog\\enchantmentsetting_menu.xml");
					break;
				case "Message Menu":
					sf.InstallDataFile("menus\\message_menu.xml");
					break;
				case "Loading Menu":
					sf.InstallDataFile("menus\\loading_menu.xml");
					insloading = true;
					break;
			}//switch
		}//foreach
	}

	private void InstallCustomOptions(ListBox.SelectedObjectCollection lbOptions)
	{
		foreach (Object opt in lbOptions) {
			switch (opt.ToString()) {
				case "Custom Font 1":
					// handled elsewhere
					break;
				case "KCAS-AF Menus":
					if (insstats && !ModActive(oxp))
						sf.EditXMLReplace("menus\\prefabs\\darn\\stats_config.xml", "<_KCAS> &false; </_KCAS>", "<_KCAS> &true; </_KCAS>");
					if (inslevelup && !ModActive(oxp))
						sf.CopyDataFile("custom_files\\KCAS_levelup_menu.xml", "menus\\levelup_menu.xml");
					break;
				case "Trollf Loading Screens":
					sf.CopyDataFile("custom_files\\trollf_loading_menu.xml", "menus\\loading_menu.xml");
					break;
				case "Trollf Loading Screens - DarkUI Version":
					sf.CopyDataFile("custom_files\\trollf_dark_loading_menu.xml", "menus\\loading_menu.xml");
					break;
				case "DarkUI'd DarN Loading Screens":
					sf.CopyDataFile("custom_files\\dark_loading_menu.xml", "menus\\loading_menu.xml");
					sf.CopyDataFolder("custom_files\\darkuid_loading_screens\\", "", true);
					break;
				case "Atmospheric Loading Screens":
					sf.CopyDataFile("custom_files\\atmo_loading_menu.xml", "menus\\loading_menu.xml");
					break;
				case "Lighter Main Menu Text":
					sf.CopyDataFile("custom_files\\light_system_config.xml", "menus\\prefabs\\darn\\system_config.xml");
					break;
				case "Classic Inventory":
					sf.CopyDataFolder("custom_files\\classic_inventory\\", "", true);
					break;
				case "Colored Local Map":
					sf.EditINI("[Display]", "bLocalMapShader", "0");
					break;
				case "Documentation":
					sf.InstallDataFolder("Docs", true);
					break;
				case "No Quest Added popup":
					sf.CopyDataFile("custom_files\\empty.xml", "menus\\generic\\quest_added.xml");
					break;
			}
		}
	}

	private void InstallFonts(string font)
	{
		sf.InstallDataFolder("fonts", true);
		sf.EditINI("[Fonts]", "SFontFile_4", "Data\\Fonts\\DarN_Oblivion_28.fnt");

		if (font == "Large") {
			sf.EditINI("[Fonts]", "SFontFile_2", "Data\\Fonts\\DarN_LG_Kingthings_Petrock_14.fnt");
			sf.EditINI("[Fonts]", "SFontFile_3", "Data\\Fonts\\DarN_LG_Kingthings_Petrock_18.fnt");
		}
		else {
			sf.EditINI("[Fonts]", "SFontFile_2", "Data\\Fonts\\DarN_Kingthings_Petrock_14.fnt");
			sf.EditINI("[Fonts]", "SFontFile_3", "Data\\Fonts\\DarN_Kingthings_Petrock_16.fnt");
		}
		// Old DarNified/Phinix cleanup
		string def_font5 = "Data\\Fonts\\Handwritten.fnt";
		string ini_font5 = sf.ReadINI("[Fonts]", "SFontFile_5");

		if (def_font5 != ini_font5)
			sf.EditINI("[Fonts]", "SFontFile_5", def_font5);
	}

	private void InstallFont1(string choice)
	{
		if (choice != "Default") {
			sf.CopyDataFile("custom_files\\fonts\\DarN_" + choice + ".fnt", "Fonts\\DarN_" + choice + ".fnt");
			sf.CopyDataFile("custom_files\\fonts\\DarN_" + choice + ".tex", "Fonts\\DarN_" + choice + ".tex");
			sf.EditINI("[Fonts]", "SFontFile_1", "Data\\Fonts\\DarN_" + choice + ".fnt");
		}
	}

	private void SilentInstall()
	{
		CreateSelectPlentyDialog(Page.menus); // Init menus called here
		InstallSelected(lb.SelectedItems);

		InitPage(Page.options);

		if (lb.SelectedItems.Count > 0) {
			InstallCustomOptions(lb.SelectedItems);

			if (lb.SelectedItems.Contains("Custom Font 1")) {
				InitPage(Page.font1select);
				InstallFont1(lb.Text);
			}
			InitPage(Page.fonts);
			InstallFonts(lb.Text);
		}
	}

	private void ResetInfo()
	{
		if (!unattended) {
			pb.Image = bm8;
			pb.Tag = "";

			rtb.ResetFont();
			rtb.Text = initialdescs[(int)currentPage];
		}
	}

	private void InitPage(Page page)
	{
		try {
				currentPage = page;
				updating = true;

				lb.Items.Clear();

				switch (page) {
					case Page.menus:
						lb.Items.AddRange(menuselections);
						lb.SelectionMode = SelectionMode.MultiExtended;

						for (int i = 0; i < defMenus.Length; i++)
							lb.SetSelected(i, defMenus[i]);

						// Filter out redundant items
		                if (ModActive(oxp))
		                    lb.Items.RemoveAt(lb.FindStringExact("LevelUp Menu"));
					break;
					case Page.options:
						lb.Items.AddRange(customoptions);
						lb.SelectionMode = SelectionMode.MultiExtended;
						bok.Enabled = true;

						for (int i = 0; i < defOptions.Length; i++)
							lb.SetSelected(i, defOptions[i]);

						// Filter out redundant items
		                if (!ModInstalled(trollf) || !insloading) {
		                    lb.Items.RemoveAt(lb.FindStringExact("Trollf Loading Screens"));
		                }
		                if (!ModActive(kcas) || ModActive(oxp)) {
		                    lb.Items.RemoveAt(lb.FindStringExact("KCAS-AF Menus"));
		                }
		                if (!insinventory) {
		                	lb.Items.RemoveAt(lb.FindStringExact("Classic Inventory"));
		                }
					break;
					case Page.font1select:
						lb.Items.AddRange(font1selections);
						lb.SelectionMode = SelectionMode.One;
						lb.SetSelected(defFont1, true);
					break;
					case Page.fonts:
						lb.Items.AddRange(fonts);
						lb.SelectionMode = SelectionMode.One;
						lb.SetSelected(defFonts, true);
					break;
				}
				updating = false;
			}
		catch (Exception e)
		{
			MessageBox.Show("Exception: " + e.Message, "DUI Install - Error in InitPage()", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	private bool ModInstalled(string[] modnameArr) // for Bashed mods (trollf)
	{
		for (int i = 0; i < modnameArr.Length; i++) {
			if (sf.DataFileExists(modnameArr[i]))
				return true;
		}
		return skipdetection;
	}

	private bool ModActive(string[] modnameArr)
	{
		for (int i = 0; i < modnameArr.Length; i++) {
			for (int x = 0; x < esps.Length; x++) {
				if (esps[x] == modnameArr[i])
					return true;
			}
		}
		return skipdetection;
	}

	private Font SetFont(float fontSize, FontStyle style)
	{
		for (int i = 0; i < fontArr.Length; i++) {
			foreach (FontFamily font in FontFamily.Families) {
				if (font.Name == fontArr[i] && font.IsStyleAvailable(style)) {
				    return new Font(font.Name, fontSize, style, GraphicsUnit.Point);
				}
			}
		}
		return new Font("Arial", fontSize, FontStyle.Regular, GraphicsUnit.Point);
	}

	private void GetPlayerName()
	{
		// Get players name in the credits
		//pn = sf.InputString("What's your name?"); //needs bugfix
		if (pn == "")
			pn = ShowInputDialog();

		if (pn != "")
			sf.EditXMLReplace("menus\\options\\credits_menu.xml", "<string>You</string>", "<string>" + pn + "</string>");
	}

	// Stop gap input dialog...
	Button btnOk;
	TextBox textBox1;
	Form ipFrm;

	private string ShowInputDialog()
	{
		ipFrm = sf.CreateCustomDialog();

		btnOk = new Button();
		textBox1 = new TextBox();
		ipFrm.SuspendLayout();
		//
		// btnOk
		//
		btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
		btnOk.Location = new Point(86, 49);
		btnOk.Name = "btnOk";
		btnOk.Size = new Size(75, 23);
		btnOk.TabIndex = 1;
		btnOk.Text = "&Ok";
		btnOk.UseVisualStyleBackColor = true;
		btnOk.Click += new EventHandler(btnOk_Click);
		//
		// textBox1
		//
		textBox1.Location = new Point(12, 12);
		textBox1.MaxLength = 50;
		textBox1.Name = "textBox1";
		textBox1.Size = new Size(236, 20);
		textBox1.TabIndex = 0;
		//
		// InputDialog
		//
		ipFrm.AutoScaleDimensions = new SizeF(6F, 13F);
		ipFrm.AutoScaleMode = AutoScaleMode.Font;
		ipFrm.ClientSize = new Size(259, 89);
		ipFrm.Controls.Add(textBox1);
		ipFrm.Controls.Add(btnOk);
		ipFrm.AcceptButton = btnOk;
		ipFrm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
		ipFrm.Name = "InputDialog";
		ipFrm.StartPosition = FormStartPosition.CenterParent;
		ipFrm.Text = "What\'s your name?";
		ipFrm.ResumeLayout(false);
		ipFrm.PerformLayout();

		if (ipFrm.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
			if ((string) ipFrm.Tag != "")
				return (string)ipFrm.Tag;
			else
				return "";
		}
		else return "";
	}

	private void btnOk_Click(object sender, EventArgs e)
	{
		ipFrm.Tag = textBox1.Text;
	}

    }
}
