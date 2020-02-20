﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp.Example.Handlers;
using CefSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Diagnostics;
using cefsharptest.Widgets;
using MaterialSkin.Controls;
using MaterialSkin;

namespace cefsharptest
{

    public partial class SettingsWidget : MaterialForm
    {

        //JObject rss = null;
        List<object> uiElement = new List<object>();
        const int uiElementWidth = 200;  
        const int uiElementHeight = 40;
        const int uiElementMargin = 10;

        readonly string propertyFilePath = Path.Combine(Path.GetDirectoryName(Form1.htmlPath), "LivelyProperties.json");
        public SettingsWidget(string arg)
        {
            //ipc message: "lively-config display_device_name"
            string[] words = arg.Split(' ');
            if (words.Length < 2) //unlikely.
            {
                return;
            }
            InitializeComponent();
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Grey800, Primary.Grey700, Primary.Grey900, Accent.Lime700, TextShade.WHITE);

            this.Icon = Properties.Icons.icons8_pencil_48;
      
            Screen screen = null;
            foreach (var item in Screen.AllScreens)
            {
                if (item.DeviceName.Equals(words[1], StringComparison.OrdinalIgnoreCase))
                {
                    screen = item;
                }
            }
            if (screen == null)
            {
                //fallback.
                screen = Screen.PrimaryScreen;
            }
            //right-most location of the display in which wallpaper is running.
            this.StartPosition = FormStartPosition.Manual;
            this.Height = (int)(screen.WorkingArea.Height / 1.2f);
            this.Location = new Point(screen.Bounds.Right - this.Width, screen.WorkingArea.Bottom - this.Height);
            this.MaximumSize = new Size(320, int.MaxValue);


        }

        #region ui_generation
        private void SettingsWidget_Load(object sender, EventArgs e)
        {
            try
            {
                WidgetData.LoadLivelyProperties(Path.Combine(Path.GetDirectoryName(Form1.htmlPath), "LivelyProperties.json"));
                GenerateLivelyWidgetUIElements();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,"Something went wrong..", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void GenerateLivelyWidgetUIElements()
        {
            if (WidgetData.liveyPropertiesData.Count == 0)
            {
                //nothing here
                AddUIElement(new MaterialLabel()
                {
                    Text = "1+1=1",
                    TextAlign = ContentAlignment.BottomLeft,
                    AutoSize = true,
                    ForeColor = Color.FromArgb(200, 200, 200),
                    Font = new Font("Segoe UI", 10, FontStyle.Regular)
                });
                return;
            }

            dynamic obj = null;
            foreach (var item in WidgetData.liveyPropertiesData)
            {
                string uiElementType = item.Value["type"].ToString();
                if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
                {
                    var tb = new TrackBar
                    {
                        Name = item.Key,
                        Minimum = (int)item.Value["min"],
                        Maximum = (int)item.Value["max"],
                        TickFrequency = (int)item.Value["tick"],
                        Value = (int)item.Value["value"]
                    };
                    tb.Scroll += Trackbar_Scroll;
                    obj = tb;
                }
                else if (uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                {
                    var tb = new TextBox
                    {
                        Name = item.Key,
                        Text = item.Value["value"].ToString(),
                        AutoSize = true
                    };
                    tb.TextChanged += Textbox_TextChanged;
                    obj = tb;
                }
                else if (uiElementType.Equals("button", StringComparison.OrdinalIgnoreCase))
                {
                    var btn = new Button
                    {
                        BackColor = Color.FromArgb(65, 65, 65),
                        ForeColor = Color.FromArgb(200, 200, 200),
                        Name = item.Key,
                        Text = item.Value["value"].ToString()
                    };
                    btn.Click += Button_Click;
                    obj = btn;
                }
                else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase))
                {
                    var pb = new PictureBox
                    {
                        Name = item.Key,
                        BackColor = ColorTranslator.FromHtml(item.Value["value"].ToString())
                    };
                    pb.Click += PictureBox_Clicked;
                    obj = pb;
                }
                else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                {
                    var chk = new MaterialCheckBox
                    {
                        Name = item.Key,
                        Text = item.Value["text"].ToString(),
                        Checked = (bool)item.Value["value"],
                        BackColor = Color.FromArgb(37, 37, 37),
                        ForeColor = Color.FromArgb(200, 200, 200),
                        Font = new Font("Segoe UI", 10, FontStyle.Regular)
                    };
                    //chk.Text.
                    chk.CheckedChanged += Checkbox_CheckedChanged;
                    obj = chk;
                }
                else if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                {
                    var cmbBox = new ComboBox
                    {
                        Name = item.Key,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Font = new Font("Segoe UI", 10, FontStyle.Regular),

                    };
                    //JSON Array
                    foreach (var dropItem in item.Value["items"])
                    {
                        cmbBox.Items.Add(dropItem);
                    }
                    cmbBox.SelectedIndex = (int)item.Value["value"];
                    //cmbBox.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                    cmbBox.SelectedValueChanged += CmbBox_SelectedValueChanged;
                    obj = cmbBox;
                }
                else if (uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                {
                    var label = new MaterialLabel
                    {
                        Name = item.Key,
                        Text = item.Value["value"].ToString(),
                        TextAlign = ContentAlignment.MiddleLeft,
                        //AutoSize = true,
                        ForeColor = Color.FromArgb(200, 200, 200),
                        Font = new Font("Segoe UI", 10, FontStyle.Regular)
                    };
                    obj = label;
                }
                else
                {
                    continue;
                }

                //Title
                if (item.Value["text"] != null &&
                    !uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase) &&
                    !uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                {
                    
                    AddUIElement(new MaterialLabel() { Text = item.Value["text"].ToString(), 
                        TextAlign = ContentAlignment.BottomLeft,
                        //AutoSize = true, 
                        ForeColor = Color.FromArgb(200,200,200),
                        Font = new Font("Segoe UI", 10, FontStyle.Regular)
                    });
                    
                }
                AddUIElement(obj);
                //label with trackbar value
                if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
                {
                    AddUIElement(new MaterialLabel
                    {
                        Text = item.Value["value"].ToString(),
                        TextAlign = ContentAlignment.MiddleRight,
                        //AutoSize = true, 
                        ForeColor = Color.FromArgb(200, 200, 200),
                        Font = new Font("Segoe UI", 10, FontStyle.Regular)
                    });
                }
            }
        }

        private void AddUIElement(dynamic obj)
        {
            
            obj.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            if (uiElement.Count == 0)
            {
                obj.Top = uiElementMargin;
            }
            else
            {
                obj.Top = ((uiElementHeight + uiElementMargin) * uiElement.Count) + uiElementMargin;
            }

            obj.Left = uiElementMargin;
            obj.Height = uiElementHeight;
            obj.Width = uiElementWidth;
            
            uiElement.Add(obj);
            //this.panel1.Controls.Add(obj);
            this.flowLayoutPanel1.Controls.Add(obj);
            this.flowLayoutPanel1.SetFlowBreak(obj, true);
        }
        #endregion

        #region menu_events
        private void CmbBox_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                var item = (ComboBox)sender;
                //Debug.WriteLine(item.Name + item.SelectedIndex);
                if (Form1.chromeBrowser.CanExecuteJavascriptInMainFrame)
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, item.SelectedIndex);
                    WidgetData.liveyPropertiesData[item.Name]["value"] = item.SelectedIndex;
                    WidgetData.SaveLivelyProperties(propertyFilePath, WidgetData.liveyPropertiesData);
                }
            }
            catch(Exception ex) //saving error
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                var item = (CheckBox)sender;
                //Debug.WriteLine(item.Name +item.Checked);
                if (Form1.chromeBrowser.CanExecuteJavascriptInMainFrame)
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, item.Checked);
                    WidgetData.liveyPropertiesData[item.Name]["value"] = item.Checked;
                    WidgetData.SaveLivelyProperties(propertyFilePath, WidgetData.liveyPropertiesData);
                }
            }
            catch (Exception ex) //saving error
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PictureBox_Clicked(object sender, EventArgs e)
        {
            try
            {
                var item = (PictureBox)sender;
                ColorDialog colorDialog = new ColorDialog() { AllowFullOpen = true };
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    item.BackColor = colorDialog.Color;
                    if (Form1.chromeBrowser.CanExecuteJavascriptInMainFrame)
                    {
                        Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, ToHexValue(colorDialog.Color));
                        WidgetData.liveyPropertiesData[item.Name]["value"] = ToHexValue(colorDialog.Color);
                        WidgetData.SaveLivelyProperties(propertyFilePath, WidgetData.liveyPropertiesData);
                    }
                }
            }
            catch (Exception ex) //saving error
            {
                MessageBox.Show(ex.Message);
            }

        }

        public static string ToHexValue(Color color)
        {
            return "#" + color.R.ToString("X2") +
                         color.G.ToString("X2") +
                         color.B.ToString("X2");
        }

        private void Button_Click(object sender, EventArgs e)
        {
            try
            {
                var item = (Button)sender;
                //Debug.WriteLine(item.Name);
                if (Form1.chromeBrowser.CanExecuteJavascriptInMainFrame)
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, true);
                    WidgetData.liveyPropertiesData[item.Name]["value"] = true;
                    WidgetData.SaveLivelyProperties(propertyFilePath, WidgetData.liveyPropertiesData);
                }
            }
            catch (Exception ex) //saving error
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Textbox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var item = (TextBox)sender;
                //Debug.WriteLine(item.Name + " "+item.Text);
                if (Form1.chromeBrowser.CanExecuteJavascriptInMainFrame)
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, item.Text);
                    WidgetData.liveyPropertiesData[item.Name]["value"] = item.Text;
                    WidgetData.SaveLivelyProperties(propertyFilePath, WidgetData.liveyPropertiesData);
                }
            }
            catch (Exception ex) //saving error
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Trackbar_Scroll(object sender, EventArgs e)
        {
            try
            {
                var item = (TrackBar)sender;
                //label with trackbar value.
                var lbl = (Label)uiElement[uiElement.FindIndex(x => x.Equals(sender)) + 1];
                lbl.Text = item.Value.ToString();

                if (Form1.chromeBrowser.CanExecuteJavascriptInMainFrame)
                {
                    Form1.chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", item.Name, item.Value);
                    WidgetData.liveyPropertiesData[item.Name]["value"] = item.Value;
                    WidgetData.SaveLivelyProperties(propertyFilePath, WidgetData.liveyPropertiesData);
                }
            }
            catch (Exception ex) //saving error
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void flowLayoutPanel1_Layout(object sender, LayoutEventArgs e)
        {

        }

        private void flowLayoutPanel1_SizeChanged(object sender, EventArgs e)
        {

        }
    }
}
