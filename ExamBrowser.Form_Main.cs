// ExamBrowser, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// ExamBrowser.Form_Main
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using ExamBrowser;
using ExamBrowser.Arthur_WorkstationService;
using ExamBrowser.Properties;
using Gma.UserActivityMonitor;

public class Form_Main : Form
{
	private struct fkey
	{
		public string keys;

		public bool found;
	}

	private struct fkeys
	{
		public fkey[] keys;

		public bool found;
	}

	private struct Blocked_Keys
	{
		public bool blocked;

		public string keys;
	}

	private class ForceCloseParam
	{
		public string blockKeys { get; set; }
	}

	private Form_Settings.AppConfig AppConfig;

	private List<string> PressedKeys;

	private fkeys[] FilteredKeys;

	private fkey[] SpecialKeys;

	private bool SpecialKeysPressed;

	private bool Flag_Do_Block;

	private bool CtrlShiftKeyPress;

	private string wsIp;

	private string ThisIP;

	private int Process_Chrome;

	private bool cableunplug;

	private IContainer components;

	private StatusStrip statusStrip1;

	private ToolStripStatusLabel tSSLbl_Message;

	private MenuStrip menuStrip1;

	private ToolStripMenuItem TSMItm_Close;

	private ToolStripMenuItem TSMItm_Settings;

	private Button btn_Run;

	private System.Windows.Forms.Timer tmr_Clean;

	private System.Windows.Forms.Timer tmr_Interlocks;

	private System.Windows.Forms.Timer timer_WatchDog;

	private TextBox tB_WatchDog;

	private ToolStripProgressBar toolStripProgressBar1;

	private ToolStripMenuItem helpToolStripMenuItem;

	private ToolStripMenuItem tsmi_version;

	private System.Windows.Forms.Timer timer_mouseleftdoubleclick;

	private System.Windows.Forms.Timer timer_heartbeep;

	private ToolStripStatusLabel tssl_heartbeep;

	private System.Windows.Forms.Timer timer_heartbeepanim;

	private System.Windows.Forms.Timer timer_chklanplug;

	private System.Windows.Forms.Timer timer_chkinternet;

	private System.Windows.Forms.Timer timer_chkgma;

	private BackgroundWorker bw_chkinternet;

	private BackgroundWorker bw_chkgma;

	private BackgroundWorker bw_heartbeep;

	private System.Windows.Forms.Timer timer_ProcessChromeCount;

	private BackgroundWorker bw_ForceClose;

	public Form_Main()
	{
		InitializeComponent();
	}

	private void Initialization()
	{
		base.Height = 234;
		System.Windows.Forms.Timer timer = timer_WatchDog;
		bool enabled = (tB_WatchDog.Visible = false);
		timer.Enabled = enabled;
		tSSLbl_Message.Text = "";
		PressedKeys = new List<string>();
		toolStripProgressBar1.Visible = false;
	}

	public void SetMessage(string str)
	{
		tSSLbl_Message.Text = str;
		tmr_Clean.Enabled = true;
	}

	private Form_Password.IsPasswordValid_result RunValidation()
	{
		Form_Password.IsPasswordValid_result result = default(Form_Password.IsPasswordValid_result);
		Form_Password form_Password = new Form_Password();
		form_Password.Opacity = 1.0;
		form_Password.ShowDialog();
		if (form_Password.DialogResult == DialogResult.OK)
		{
			return form_Password.IsPasswordValid();
		}
		return result;
	}

	private void ReadAppConfig()
	{
		Form_Settings form_Settings = new Form_Settings();
		AppConfig = default(Form_Settings.AppConfig);
		AppConfig = form_Settings.ReadConfig(Load_List: true);
		form_Settings.Close();
		form_Settings.Dispose();
		try
		{
			if (AppConfig.DebugMode == "1")
			{
				base.Height = 450;
				System.Windows.Forms.Timer timer = timer_WatchDog;
				TextBox textBox = tB_WatchDog;
				bool flag2 = (base.TopMost = true);
				flag2 = (textBox.Visible = flag2);
				timer.Enabled = flag2;
				BringToFront();
			}
			else
			{
				base.Height = 234;
				System.Windows.Forms.Timer timer2 = timer_WatchDog;
				TextBox textBox2 = tB_WatchDog;
				bool flag2 = (base.TopMost = false);
				flag2 = (textBox2.Visible = flag2);
				timer2.Enabled = flag2;
			}
			tmr_Interlocks.Interval = Convert.ToInt32(AppConfig.TimerInterval);
			timer_heartbeep.Interval = AppConfig.HeartInterval;
		}
		catch (Exception ex)
		{
			SetMessage(ex.Message);
			tmr_Interlocks.Interval = 1000;
		}
		LoadSpecialKeys();
		LoadFilterKeys();
	}

	private bool OpenBrowser(string usragent)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo();
		processStartInfo.Arguments = AppConfig.BrowserArguments + " " + AppConfig.InitialUrl + " --user-agent=" + usragent + " --ignore-certificate-errors --disable-translate --user-data-dir=/tmp";
		processStartInfo.FileName = AppConfig.BrowserFilename;
		processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		processStartInfo.CreateNoWindow = true;
		bool result = true;
		try
		{
			Process.Start(processStartInfo);
			SetMessage("Open Browser.");
			if (AppConfig.SendF11Stroke == "1")
			{
				toolStripProgressBar1.Value = 0;
				toolStripProgressBar1.Visible = true;
				for (int i = 0; i < 100; i++)
				{
					toolStripProgressBar1.Value = i;
					Thread.Sleep(30);
				}
				toolStripProgressBar1.Visible = false;
				SendKeys.Send("{F11}");
			}
		}
		catch (Exception ex)
		{
			result = false;
			SetMessage("Fail Open Browser!");
			string arg_107_0 = ex.Message;
		}
		return result;
	}

	private void PressedKeysLogger(string FunctionCode, string key)
	{
		if (FunctionCode == "+")
		{
			if (!PressedKeys.Contains(key.ToLower()))
			{
				PressedKeys.Add(key.ToLower());
			}
		}
		else if (FunctionCode == "-" && PressedKeys.Contains(key.ToLower()))
		{
			PressedKeys.Remove(key.ToLower());
		}
	}

	private void LoadFilterKeys()
	{
		fkeys[] array = new fkeys[0];
		for (int i = 0; i < AppConfig.FilteredKeys.Length; i++)
		{
			string text = AppConfig.FilteredKeys[i];
			Array.Resize(ref array, array.Length + 1);
			array[i].keys = new fkey[0];
			if (text.Contains("+"))
			{
				string text2 = text;
				while (text2.Contains("+"))
				{
					int num = text2.IndexOf("+");
					text = text2.Substring(0, num);
					text2 = text2.Replace(text2.Substring(0, num + 1), "");
					Array.Resize(ref array[i].keys, array[i].keys.Length + 1);
					array[i].keys[array[i].keys.Length - 1].keys = text;
					array[i].keys[array[i].keys.Length - 1].found = false;
				}
				Array.Resize(ref array[i].keys, array[i].keys.Length + 1);
				array[i].keys[array[i].keys.Length - 1].keys = text2;
				array[i].keys[array[i].keys.Length - 1].found = false;
			}
			else
			{
				Array.Resize(ref array[i].keys, array[i].keys.Length + 1);
				array[i].keys[array[i].keys.Length - 1].keys = text;
				array[i].keys[array[i].keys.Length - 1].found = false;
			}
			array[i].found = false;
		}
		FilteredKeys = array;
	}

	private void LoadSpecialKeys()
	{
		fkey[] array = new fkey[0];
		for (int i = 0; i < AppConfig.SpecialKeys.Length; i++)
		{
			Array.Resize(ref array, array.Length + 1);
			array[array.Length - 1].keys = AppConfig.SpecialKeys[i].ToLower().Trim();
			array[array.Length - 1].found = false;
		}
		SpecialKeys = array;
	}

	private void CheckSpecialKeys()
	{
		SpecialKeysPressed = false;
		bool flag = true;
		for (int i = 0; i < SpecialKeys.Length; i++)
		{
			SpecialKeys[i].found = false;
			for (int j = 0; j < PressedKeys.Count; j++)
			{
				if (SpecialKeys[i].keys == PressedKeys[j])
				{
					SpecialKeys[i].found = true;
				}
			}
			flag = flag && SpecialKeys[i].found;
		}
		if (flag)
		{
			BringToFront();
			if (btn_Run.Tag.ToString() == "1")
			{
				SpecialKeysPressed = true;
				btn_Run_Click(btn_Run, null);
			}
		}
	}

	private Blocked_Keys BlockedKeys()
	{
		Blocked_Keys result = default(Blocked_Keys);
		result.keys = "";
		result.blocked = false;
		for (int i = 0; i < FilteredKeys.Length; i++)
		{
			bool flag = true;
			FilteredKeys[i].found = false;
			for (int j = 0; j < FilteredKeys[i].keys.Length; j++)
			{
				FilteredKeys[i].keys[j].found = false;
				for (int k = 0; k < PressedKeys.Count; k++)
				{
					if (PressedKeys[k].ToLower() == "LShiftKey".ToLower() || PressedKeys[k].ToLower() == "RShiftKey".ToLower() || PressedKeys[k].ToLower() == "LControlKey".ToLower() || PressedKeys[k].ToLower() == "RControlKey".ToLower())
					{
						CtrlShiftKeyPress = true;
					}
					else
					{
						CtrlShiftKeyPress = false;
					}
					if (FilteredKeys[i].keys[j].keys.ToLower() == PressedKeys[k].ToLower())
					{
						FilteredKeys[i].keys[j].found = true;
					}
				}
				flag = flag && FilteredKeys[i].keys[j].found;
			}
			FilteredKeys[i].found = flag;
			if (flag)
			{
				result.keys = "";
				for (int l = 0; l < FilteredKeys[i].keys.Length; l++)
				{
					result.keys += ((l == 0) ? "" : " + ");
					result.keys += FilteredKeys[i].keys[l].keys;
				}
				FilteredKeys[i].found = false;
				result.blocked = true;
				break;
			}
		}
		return result;
	}

	private void HookManager_KeyDown(object sender, KeyEventArgs e)
	{
		PressedKeysLogger("+", e.KeyCode.ToString());
		Blocked_Keys blocked_Keys = BlockedKeys();
		e.Handled = (Flag_Do_Block = blocked_Keys.blocked);
		if (e.Handled)
		{
			SetMessage(blocked_Keys.keys + " blocked");
			ForceCloseParam forceCloseParam = new ForceCloseParam();
			forceCloseParam.blockKeys = blocked_Keys.keys;
			ForceCloseParam argument = forceCloseParam;
			bw_ForceClose.RunWorkerAsync(argument);
		}
	}

	private void HookManager_KeyUp(object sender, KeyEventArgs e)
	{
		e.Handled = Flag_Do_Block;
		CheckSpecialKeys();
		PressedKeysLogger("-", e.KeyCode.ToString());
	}

	private void HookManager_MouseClickExt(object sender, MouseEventExtArgs e)
	{
		if (e.Button == MouseButtons.Right)
		{
			e.Handled = true;
			SetMessage("Mouse right click blocked.");
		}
		if (e.Button == MouseButtons.Left && CtrlShiftKeyPress)
		{
			e.Handled = true;
			SetMessage("Shift + Mouse left click blocked.");
			timer_mouseleftdoubleclick.Enabled = false;
			timer_mouseleftdoubleclick.Enabled = true;
		}
	}

	private void Hooks()
	{
		HookManager.KeyDown += HookManager_KeyDown;
		HookManager.KeyUp += HookManager_KeyUp;
		HookManager.MouseClickExt += HookManager_MouseClickExt;
	}

	public void Unhooks()
	{
		HookManager.KeyDown -= HookManager_KeyDown;
		HookManager.KeyUp -= HookManager_KeyUp;
		HookManager.MouseClickExt -= HookManager_MouseClickExt;
	}

	private void RunInterlocks()
	{
		if (AppConfig.LockKeys.ToString().Trim() == "1")
		{
			Hooks();
		}
		tmr_Interlocks.Enabled = true;
		SetMessage("Interlocks Run.");
	}

	private void StopInterlocks()
	{
		if (AppConfig.LockKeys.ToString().Trim() == "1")
		{
			Unhooks();
		}
		tmr_Interlocks.Enabled = false;
		SetMessage("Interlocks Stop.");
		PressedKeys.Clear();
	}

	public void KillAllBrowser()
	{
		Process[] processesByName = Process.GetProcessesByName("chrome");
		Process[] array = processesByName;
		foreach (Process process in array)
		{
			process.Kill();
		}
		processesByName = Process.GetProcessesByName("iexplore");
		Process[] array2 = processesByName;
		foreach (Process process2 in array2)
		{
			process2.Kill();
		}
		processesByName = Process.GetProcessesByName("firefox");
		Process[] array3 = processesByName;
		foreach (Process process3 in array3)
		{
			process3.Kill();
		}
	}

	public void UnregisterfromServer()
	{
		try
		{
			WorkstationService workstationService = new WorkstationService();
			CBTResponse cBTResponse = workstationService.UnSubmitWorkstation(wsIp, wsIp, "Fijar Love Feyy", "FF");
			if (!cBTResponse.Result)
			{
				SetMessage(cBTResponse.Message);
			}
		}
		catch (Exception ex)
		{
			string arg_40_0 = ex.Message;
		}
	}

	public static void Log(string logMessage, TextWriter w)
	{
		w.Write("\r\n{0}: {1}", DateTime.Now.ToString("dd/MM/yy HH:mm:ss"), logMessage);
	}

	public static void TulisLog(string logMessage)
	{
		using StreamWriter streamWriter = File.AppendText("log.txt");
		Log(logMessage, streamWriter);
	}

	public static bool CheckForInternetConnection()
	{
		try
		{
			using WebClient webClient = new WebClient();
			using (webClient.OpenRead("http://www.google.com"))
			{
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	private void Form_Main_Load(object sender, EventArgs e)
	{
		CtrlShiftKeyPress = false;
		Initialization();
	}

	private void Form_Main_FormClosing(object sender, FormClosingEventArgs e)
	{
		if (btn_Run.Tag.ToString() == "1")
		{
			SetMessage("Please Stop application first!");
			e.Cancel = true;
		}
		else
		{
			timer_heartbeep.Enabled = false;
			UnregisterfromServer();
		}
	}

	private void TSMItm_Close_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void TSMItm_Settings_Click(object sender, EventArgs e)
	{
		Form_Password.IsPasswordValid_result isPasswordValid_result = RunValidation();
		if (isPasswordValid_result.valid)
		{
			Form_Settings form_Settings = new Form_Settings();
			if (isPasswordValid_result.isadmin)
			{
				form_Settings.setFormTag("admin");
			}
			form_Settings.ShowDialog();
		}
	}

	private bool ChkGmaFile()
	{
		string currentDirectory = Directory.GetCurrentDirectory();
		string text = currentDirectory + "\\Gma.UserActivityMonitor.dll";
		long num = 0L;
		try
		{
			FileInfo fileInfo = new FileInfo(text);
			num = fileInfo.Length;
		}
		catch
		{
			num = 0L;
		}
		bool flag = File.Exists(text) && Math.Abs(28672 - num) <= 0;
		if (!flag)
		{
			SetMessage("Ada file aplikasi yang hilang!");
		}
		return flag;
	}

	private bool ChkXmlSerializersFile()
	{
		string currentDirectory = Directory.GetCurrentDirectory();
		string path = currentDirectory + "\\ExamBrowser.XmlSerializers.dll";
		bool flag = File.Exists(path);
		if (!flag)
		{
			SetMessage("Ada file aplikasi yang hilang!");
		}
		return flag;
	}

	private void btn_Run_Click(object sender, EventArgs e)
	{
		string usragent = "";
		if (btn_Run.Tag.ToString() == "0")
		{
			if (!ChkGmaFile() || !ChkXmlSerializersFile())
			{
				return;
			}
			TulisLog("Run ExamBrowser...");
			KillAllBrowser();
			try
			{
				ReadAppConfig();
				if (!OpenBrowser(usragent))
				{
					string text4 = "Aplikasi web browser tidak ditemukan!";
					SetMessage(text4);
					MessageBox.Show(text4, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
					return;
				}
				btn_Run.Tag = "1";
				btn_Run.Text = "STOP";
				btn_Run.ForeColor = Color.Green;
				TSMItm_Settings.Enabled = false;
				base.Opacity = 0.75;
				RunInterlocks();
				if (AppConfig.DebugMode != "1")
				{
					SendToBack();
				}
				timer_ProcessChromeCount.Enabled = true;
				return;
			}
			catch (Exception ex2)
			{
				SetMessage(ex2.Message);
				return;
			}
		}
		Form_Password.IsPasswordValid_result isPasswordValid_result = default(Form_Password.IsPasswordValid_result);
		if (SpecialKeysPressed || RunValidation().valid)
		{
			btn_Run.Tag = "0";
			btn_Run.Text = "RUN";
			try
			{
				btn_Run.ForeColor = Color.CornflowerBlue;
			}
			catch (Exception)
			{
			}
			TSMItm_Settings.Enabled = true;
			base.Opacity = 1.0;
			StopInterlocks();
			BringToFront();
			KillAllBrowser();
			timer_heartbeep.Enabled = false;
			UnregisterfromServer();
			TulisLog("Stop ExamBrowser...");
		}
	}

	private void tmr_Clean_Tick(object sender, EventArgs e)
	{
		tSSLbl_Message.Text = "";
		tmr_Clean.Enabled = false;
	}

	private void tmr_Interlocks_Tick(object sender, EventArgs e)
	{
		try
		{
			Process[] processes = Process.GetProcesses();
			string b = AppConfig.BrowserFilename.ToLower().Replace(".exe", "").ToLower()
				.Trim();
			Process[] array = processes;
			foreach (Process process in array)
			{
				string a = process.ProcessName.ToLower().Trim();
				if (!(a != b))
				{
					continue;
				}
				for (int j = 0; j < AppConfig.Application.Length; j++)
				{
					string text = AppConfig.Application[j];
					if (a == text && !string.IsNullOrEmpty(text))
					{
						process.Kill();
						SetMessage(process.ProcessName + " blocked.");
					}
				}
			}
		}
		catch
		{
		}
	}

	private void timer_WatchDog_Tick(object sender, EventArgs e)
	{
		try
		{
			tB_WatchDog.Text = "Pressed Keys" + Environment.NewLine;
			for (int i = 0; i < PressedKeys.Count; i++)
			{
				tB_WatchDog.AppendText(PressedKeys[i] + Environment.NewLine);
			}
			TextBox expr_5D = tB_WatchDog;
			expr_5D.Text = expr_5D.Text + Environment.NewLine + Environment.NewLine;
			TextBox expr_7D = tB_WatchDog;
			expr_7D.Text = expr_7D.Text + "Filtered Keys" + Environment.NewLine;
			for (int j = 0; j < FilteredKeys.Length; j++)
			{
				if (FilteredKeys[j].found)
				{
					string text = "";
					for (int k = 0; k < FilteredKeys[j].keys.Length; k++)
					{
						text += ((k == 0) ? "" : " + ");
						text += FilteredKeys[j].keys[k].keys;
					}
					TextBox expr_114 = tB_WatchDog;
					expr_114.Text = expr_114.Text + text + Environment.NewLine;
				}
			}
			TextBox expr_115 = tB_WatchDog;
			expr_115.Text = expr_115.Text + Environment.NewLine + Environment.NewLine;
			TextBox expr_116 = tB_WatchDog;
			expr_116.Text = expr_116.Text + "Flag_Do_Block: " + Flag_Do_Block + Environment.NewLine;
		}
		catch (Exception ex)
		{
			string arg_192_0 = ex.Message;
		}
	}

	private void tsmi_version_Click(object sender, EventArgs e)
	{
		MessageBox.Show(tsmi_version.Text);
	}

	private void timer_mouseleftdoubleclick_Tick(object sender, EventArgs e)
	{
		CtrlShiftKeyPress = false;
		timer_mouseleftdoubleclick.Enabled = false;
	}

	private void timer_heartbeepanim_Tick(object sender, EventArgs e)
	{
		try
		{
			tssl_heartbeep.Text = (tssl_heartbeep.Text + "-").Substring(1, 10);
		}
		catch
		{
		}
	}

	private void timer_heartbeep_Tick(object sender, EventArgs e)
	{
		bw_heartbeep.RunWorkerAsync();
	}

	private void bw_heartbeep_DoWork(object sender, DoWorkEventArgs e)
	{
		timer_heartbeep.Enabled = false;
		if (!string.IsNullOrEmpty(ThisIP))
		{
			Form_Settings form_Settings = new Form_Settings();
			string thisIP = ThisIP;
			string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			string puspendikdata = form_Settings.EncryptString(thisIP, form_Settings.passPhrase);
			string puspendikdata2 = form_Settings.EncryptString(text, form_Settings.passPhrase);
			CBTResponse cBTResponse = new CBTResponse();
			cBTResponse.Result = false;
			try
			{
				WorkstationService workstationService = new WorkstationService();
				cBTResponse = workstationService.PesertaTesBeat(puspendikdata, puspendikdata2, "Fijar Love Feyy", "FF");
				tssl_heartbeep.Text = (cBTResponse.Result ? (tssl_heartbeep.Text + "^").Substring(1, 10) : (tssl_heartbeep.Text + "_").Substring(1, 10));
				TulisLog((cBTResponse.Result ? "OOK" : "NOK") + " - (" + thisIP + ", " + text + ") - " + cBTResponse.Message);
			}
			catch (Exception ex)
			{
				SetMessage(ex.Message);
				tssl_heartbeep.Text = (tssl_heartbeep.Text + "e").Substring(1, 10);
				TulisLog("ERR - (" + thisIP + ", " + text + ") - " + ex.Message);
			}
		}
	}

	private void bw_heartbeep_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		timer_heartbeep.Enabled = true;
	}

	private void timer_chklanplug_Tick(object sender, EventArgs e)
	{
		timer_chklanplug.Enabled = false;
		if (!string.IsNullOrEmpty(ThisIP))
		{
			string hostName = Dns.GetHostName();
			IPAddress[] addressList = Dns.GetHostByName(hostName).AddressList;
			bool flag = false;
			for (int i = 0; i < addressList.Length; i++)
			{
				flag = addressList[i].ToString() == ThisIP;
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				SetMessage("Kabel LAN unplug!");
				if (btn_Run.Tag.ToString() == "1")
				{
					SpecialKeysPressed = true;
					btn_Run_Click(btn_Run, null);
					string text = "Kabel LAN unplug!";
					if (!cableunplug)
					{
						cableunplug = true;
						SetMessage(text);
						MessageBox.Show(text, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
						return;
					}
				}
			}
			else
			{
				cableunplug = false;
			}
		}
		timer_chklanplug.Enabled = true;
	}

	private void timer_chkgma_Tick(object sender, EventArgs e)
	{
		bw_chkgma.RunWorkerAsync();
	}

	private void bw_chkgma_DoWork(object sender, DoWorkEventArgs e)
	{
		timer_chkgma.Enabled = false;
		if (btn_Run.Tag.ToString() == "1")
		{
			if (!ChkGmaFile())
			{
				SpecialKeysPressed = true;
				btn_Run_Click(btn_Run, null);
				MessageBox.Show("Ada file aplikasi yang hilang!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
			}
			if (!ChkXmlSerializersFile())
			{
				SpecialKeysPressed = true;
				btn_Run_Click(btn_Run, null);
				MessageBox.Show("Ada file aplikasi yang hilang!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
			}
			if (Process.GetProcessesByName("chrome").Length <= 0)
			{
				SpecialKeysPressed = true;
				btn_Run_Click(btn_Run, null);
				MessageBox.Show("Browser yang running tidak ada!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
			}
		}
	}

	private void bw_chkgma_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		timer_chkgma.Enabled = true;
	}

	private void timer_chkinternet_Tick(object sender, EventArgs e)
	{
		bw_chkinternet.RunWorkerAsync();
	}

	private void bw_chkinternet_DoWork(object sender, DoWorkEventArgs e)
	{
		timer_chkinternet.Enabled = false;
		if (btn_Run.Tag.ToString() == "1" && CheckForInternetConnection())
		{
			SetMessage("Koneksi internet terdeteksi!");
			SpecialKeysPressed = true;
		}
	}

	private void bw_chkinternet_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		timer_chkinternet.Enabled = true;
	}

	private void timer_ProcessChromeCount_Tick(object sender, EventArgs e)
	{
		Process_Chrome = Process.GetProcessesByName("chrome").Length;
		timer_ProcessChromeCount.Enabled = false;
	}

	private void bw_ForceClose_DoWork(object sender, DoWorkEventArgs e)
	{
		ForceCloseParam forceCloseParam = e.Argument as ForceCloseParam;
		string blockKeys = forceCloseParam.blockKeys;
	}

	private void ForceStop()
	{
		SpecialKeysPressed = true;
		btn_Run_Click(btn_Run, null);
		MessageBox.Show("Special Key ditekan!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager componentResourceManager = new System.ComponentModel.ComponentResourceManager(typeof(ExamBrowser.Form_Main));
		this.statusStrip1 = new System.Windows.Forms.StatusStrip();
		this.tssl_heartbeep = new System.Windows.Forms.ToolStripStatusLabel();
		this.tSSLbl_Message = new System.Windows.Forms.ToolStripStatusLabel();
		this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
		this.menuStrip1 = new System.Windows.Forms.MenuStrip();
		this.TSMItm_Close = new System.Windows.Forms.ToolStripMenuItem();
		this.TSMItm_Settings = new System.Windows.Forms.ToolStripMenuItem();
		this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.tsmi_version = new System.Windows.Forms.ToolStripMenuItem();
		this.tmr_Clean = new System.Windows.Forms.Timer(this.components);
		this.tmr_Interlocks = new System.Windows.Forms.Timer(this.components);
		this.timer_WatchDog = new System.Windows.Forms.Timer(this.components);
		this.tB_WatchDog = new System.Windows.Forms.TextBox();
		this.timer_mouseleftdoubleclick = new System.Windows.Forms.Timer(this.components);
		this.timer_heartbeep = new System.Windows.Forms.Timer(this.components);
		this.timer_heartbeepanim = new System.Windows.Forms.Timer(this.components);
		this.timer_chklanplug = new System.Windows.Forms.Timer(this.components);
		this.btn_Run = new System.Windows.Forms.Button();
		this.timer_chkinternet = new System.Windows.Forms.Timer(this.components);
		this.timer_chkgma = new System.Windows.Forms.Timer(this.components);
		this.bw_chkinternet = new System.ComponentModel.BackgroundWorker();
		this.bw_chkgma = new System.ComponentModel.BackgroundWorker();
		this.bw_heartbeep = new System.ComponentModel.BackgroundWorker();
		this.timer_ProcessChromeCount = new System.Windows.Forms.Timer(this.components);
		this.bw_ForceClose = new System.ComponentModel.BackgroundWorker();
		this.statusStrip1.SuspendLayout();
		this.menuStrip1.SuspendLayout();
		base.SuspendLayout();
		this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.tssl_heartbeep, this.tSSLbl_Message, this.toolStripProgressBar1 });
		this.statusStrip1.Location = new System.Drawing.Point(0, 390);
		this.statusStrip1.Name = "statusStrip1";
		this.statusStrip1.Size = new System.Drawing.Size(284, 22);
		this.statusStrip1.TabIndex = 0;
		this.statusStrip1.Text = "statusStrip1";
		this.tssl_heartbeep.Name = "tssl_heartbeep";
		this.tssl_heartbeep.Size = new System.Drawing.Size(57, 17);
		this.tssl_heartbeep.Text = "---------s";
		this.tSSLbl_Message.Name = "tSSLbl_Message";
		this.tSSLbl_Message.Size = new System.Drawing.Size(30, 17);
		this.tSSLbl_Message.Text = "msg";
		this.toolStripProgressBar1.Name = "toolStripProgressBar1";
		this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
		this.toolStripProgressBar1.Visible = false;
		this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.TSMItm_Close, this.TSMItm_Settings, this.helpToolStripMenuItem });
		this.menuStrip1.Location = new System.Drawing.Point(0, 0);
		this.menuStrip1.Name = "menuStrip1";
		this.menuStrip1.Size = new System.Drawing.Size(284, 24);
		this.menuStrip1.TabIndex = 1;
		this.menuStrip1.Text = "menuStrip1";
		this.TSMItm_Close.Name = "TSMItm_Close";
		this.TSMItm_Close.Size = new System.Drawing.Size(48, 20);
		this.TSMItm_Close.Text = "Close";
		this.TSMItm_Close.Click += new System.EventHandler(TSMItm_Close_Click);
		this.TSMItm_Settings.Name = "TSMItm_Settings";
		this.TSMItm_Settings.Size = new System.Drawing.Size(61, 20);
		this.TSMItm_Settings.Text = "Settings";
		this.TSMItm_Settings.Click += new System.EventHandler(TSMItm_Settings_Click);
		this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.tsmi_version });
		this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
		this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
		this.helpToolStripMenuItem.Text = "Help";
		this.tsmi_version.Name = "tsmi_version";
		this.tsmi_version.Size = new System.Drawing.Size(282, 22);
		this.tsmi_version.Text = "Version 16.1030 REL © 2015 PUSPENDIK";
		this.tsmi_version.Click += new System.EventHandler(tsmi_version_Click);
		this.tmr_Clean.Interval = 10000;
		this.tmr_Clean.Tick += new System.EventHandler(tmr_Clean_Tick);
		this.tmr_Interlocks.Interval = 500;
		this.tmr_Interlocks.Tick += new System.EventHandler(tmr_Interlocks_Tick);
		this.timer_WatchDog.Enabled = true;
		this.timer_WatchDog.Tick += new System.EventHandler(timer_WatchDog_Tick);
		this.tB_WatchDog.Location = new System.Drawing.Point(12, 189);
		this.tB_WatchDog.Multiline = true;
		this.tB_WatchDog.Name = "tB_WatchDog";
		this.tB_WatchDog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
		this.tB_WatchDog.Size = new System.Drawing.Size(260, 198);
		this.tB_WatchDog.TabIndex = 3;
		this.timer_mouseleftdoubleclick.Enabled = true;
		this.timer_mouseleftdoubleclick.Interval = 2000;
		this.timer_mouseleftdoubleclick.Tick += new System.EventHandler(timer_mouseleftdoubleclick_Tick);
		this.timer_heartbeep.Tick += new System.EventHandler(timer_heartbeep_Tick);
		this.timer_heartbeepanim.Interval = 6000;
		this.timer_heartbeepanim.Tick += new System.EventHandler(timer_heartbeepanim_Tick);
		this.timer_chklanplug.Enabled = true;
		this.timer_chklanplug.Interval = 1000;
		this.timer_chklanplug.Tick += new System.EventHandler(timer_chklanplug_Tick);
		this.btn_Run.Font = new System.Drawing.Font("Verdana", 48f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.btn_Run.ForeColor = System.Drawing.Color.CornflowerBlue;
		this.btn_Run.Image = ExamBrowser.Properties.Resources.test2;
		this.btn_Run.Location = new System.Drawing.Point(12, 27);
		this.btn_Run.Name = "btn_Run";
		this.btn_Run.Size = new System.Drawing.Size(260, 135);
		this.btn_Run.TabIndex = 2;
		this.btn_Run.Tag = "0";
		this.btn_Run.Text = "RUN";
		this.btn_Run.UseVisualStyleBackColor = true;
		this.btn_Run.Click += new System.EventHandler(btn_Run_Click);
		this.timer_chkinternet.Enabled = true;
		this.timer_chkinternet.Interval = 1000;
		this.timer_chkinternet.Tick += new System.EventHandler(timer_chkinternet_Tick);
		this.timer_chkgma.Enabled = true;
		this.timer_chkgma.Interval = 1000;
		this.timer_chkgma.Tick += new System.EventHandler(timer_chkgma_Tick);
		this.bw_chkinternet.DoWork += new System.ComponentModel.DoWorkEventHandler(bw_chkinternet_DoWork);
		this.bw_chkinternet.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(bw_chkinternet_RunWorkerCompleted);
		this.bw_chkgma.DoWork += new System.ComponentModel.DoWorkEventHandler(bw_chkgma_DoWork);
		this.bw_chkgma.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(bw_chkgma_RunWorkerCompleted);
		this.bw_heartbeep.DoWork += new System.ComponentModel.DoWorkEventHandler(bw_heartbeep_DoWork);
		this.bw_heartbeep.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(bw_heartbeep_RunWorkerCompleted);
		this.timer_ProcessChromeCount.Interval = 5000;
		this.timer_ProcessChromeCount.Tick += new System.EventHandler(timer_ProcessChromeCount_Tick);
		this.bw_ForceClose.DoWork += new System.ComponentModel.DoWorkEventHandler(bw_ForceClose_DoWork);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(284, 412);
		base.Controls.Add(this.tB_WatchDog);
		base.Controls.Add(this.btn_Run);
		base.Controls.Add(this.statusStrip1);
		base.Controls.Add(this.menuStrip1);
		base.Icon = (System.Drawing.Icon)componentResourceManager.GetObject("$this.Icon");
		base.MainMenuStrip = this.menuStrip1;
		base.MaximizeBox = false;
		this.MaximumSize = new System.Drawing.Size(300, 450);
		base.Name = "Form_Main";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Exam Browser";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(Form_Main_FormClosing);
		base.Load += new System.EventHandler(Form_Main_Load);
		this.statusStrip1.ResumeLayout(false);
		this.statusStrip1.PerformLayout();
		this.menuStrip1.ResumeLayout(false);
		this.menuStrip1.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
