using AMS.Profile;
using FontAwesome.WPF;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Screen = System.Windows.Forms.Screen;
using Timer = System.Windows.Forms.Timer;

namespace Snipper_v2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private int previewSpeed = 0;
		private Ini config = null;

		[DllImport("user32.dll")]
		private static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		private static extern int SendMessage(IntPtr hwnd, int msg, int wp, int lp);

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				ReleaseCapture();
				SendMessage(new WindowInteropHelper(this).Handle, 161, 2, 0);
			}
		}

		private void CloseWindow(object sender, RoutedEventArgs e)
		{
			SystemCommands.CloseWindow(this);
		}

		private void MaximizeWindow(object sender, RoutedEventArgs e)
		{
			if (WindowState == WindowState.Normal)
			{
				SystemCommands.MaximizeWindow(this);
				((ImageAwesome)Maximize.Content).Icon = FontAwesomeIcon.WindowMaximize;
				((ImageAwesome)Maximize.Content).Margin = new Thickness(8);
			}
			else if (WindowState == WindowState.Maximized)
			{
				SystemCommands.RestoreWindow(this);
				((ImageAwesome)Maximize.Content).Icon = FontAwesomeIcon.WindowRestore;
				((ImageAwesome)Maximize.Content).Margin = new Thickness(7);
			}
		}

		private void MinimizeWindow(object sender, RoutedEventArgs e)
		{
			SystemCommands.MinimizeWindow(this);
		}

		private void ToggleFileSettings(object sender, RoutedEventArgs e)
		{
			if (ImageSettings.Visibility == Visibility.Visible)
			{
				ImageSettings.Visibility = Visibility.Hidden;
				((TranslateTransform)ImageRegion.RenderTransform).Y = 10;

				((ToolTip)((Button)sender).ToolTip).Content = "Open Screenshot File Settings";
			}
			else if (ImageSettings.Visibility == Visibility.Hidden)
			{
				ImageSettings.Visibility = Visibility.Visible;
				((TranslateTransform)ImageRegion.RenderTransform).Y = 85;

				((ToolTip)((Button)sender).ToolTip).Content = "Close Screenshot File Settings";
			}
		}

		private void ToggleRegionSettings(object sender, RoutedEventArgs e)
		{
			if (ImageRegion.Visibility == Visibility.Visible)
			{
				ImageRegion.Visibility = Visibility.Hidden;

				((ToolTip)((Button)sender).ToolTip).Content = "Open Screenshot Region Settings";
			}
			else if (ImageRegion.Visibility == Visibility.Hidden)
			{
				ImageRegion.Visibility = Visibility.Visible;

				((ToolTip)((Button)sender).ToolTip).Content = "Close Screenshot Region Settings";
			}
		}

		private static readonly Regex regex = new Regex("[0-9.-]+");

		private static bool IsNumeric(string Text)
		{
			return regex.IsMatch(Text) == false;
		}

		private void ForceNumericInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = string.IsNullOrEmpty(e.Text) == false && IsNumeric(e.Text);

			if (e.Handled == true)
			{
				((TextBox)sender).Text = int.Parse(((TextBox)sender).Text).ToString();
			}
		}

		private void InputPasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string)))
			{
				string text = e.DataObject.GetData(typeof(string)).ToString();
				if (IsNumeric(text) == false)
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}

			((TextBox)sender).Text = int.Parse(((TextBox)sender).Text).ToString();
		}

		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);

			if (WindowState == WindowState.Maximized)
			{
				ContentWindow.Padding = new Thickness(8);
				((ImageAwesome)Maximize.Content).Icon = FontAwesomeIcon.WindowRestore;
				((ImageAwesome)Maximize.Content).Margin = new Thickness(7);
			}
			else if (WindowState == WindowState.Normal)
			{
				ContentWindow.Padding = new Thickness(0);
				((ImageAwesome)Maximize.Content).Icon = FontAwesomeIcon.WindowMaximize;
				((ImageAwesome)Maximize.Content).Margin = new Thickness(8);
			}
		}

		private bool previewRegion = true;
		private Timer timer = null;
		private ScreenshotRegionPreview regPrev = null;

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			timer = new Timer { Interval = 10 };
			timer.Tick += (s, ee) =>
			{
				PreviewScreenshotRegion();
			};
			timer.Enabled = true;

			regPrev = new ScreenshotRegionPreview();
			regPrev.Owner = this;
			regPrev.Left = int.Parse(RegionX.Text);
			regPrev.Top = int.Parse(RegionY.Text);
			regPrev.Width = int.Parse(RegionW.Text);
			regPrev.Height = int.Parse(RegionH.Text);
			regPrev.Show();

			Closing += (s, ee) =>
			{
				regPrev.Close();
			};

			screenshot = CreateScreenshot();

			string file = string.Format("{0}\\config.ini", Environment.CurrentDirectory);
			if (File.Exists(file) == false)
			{
				using (StreamWriter writer = new StreamWriter(file))
				{
					writer.Write("[screenshotting]\n" +
						"changeSpeed=2");
					writer.Close();
				}
			}

			config = new Ini(file);
			previewSpeed = int.Parse(config.GetValue("screenshotting", "changeSpeed").ToString());
		}

		private void TogglePreviewScreenshotRegion(object sender, RoutedEventArgs e)
		{
			previewRegion = !previewRegion;
			timer.Enabled = previewRegion;
			regPrev.Visibility = previewRegion ? Visibility.Visible : Visibility.Hidden;

			GC.Collect();
		}

		private void PreviewScreenshotRegion()
		{
			if (string.IsNullOrEmpty(RegionX.Text) == true)
				RegionX.Text = "0";

			if (string.IsNullOrEmpty(RegionY.Text) == true)
				RegionY.Text = "0";

			if (string.IsNullOrEmpty(RegionW.Text) == true || int.Parse(RegionW.Text) < 10)
				RegionW.Text = "10";

			if (string.IsNullOrEmpty(RegionH.Text) == true || int.Parse(RegionH.Text) < 10)
				RegionH.Text = "10";

			regPrev.Left = Math.Max(0, Math.Min(int.Parse(RegionX.Text), int.MaxValue));
			regPrev.Top = Math.Max(0, Math.Min(int.Parse(RegionY.Text), int.MaxValue));
			regPrev.Width = Math.Max(10, Math.Min(int.Parse(RegionW.Text), int.MaxValue));
			regPrev.Height = Math.Max(10, Math.Min(int.Parse(RegionH.Text), int.MaxValue));

			GC.Collect();
		}

		private Bitmap CreateScreenshot()
		{
			Bitmap bmp = new Bitmap(int.Parse(RegionW.Text), int.Parse(RegionH.Text));

			using (Graphics ScreenGraphics = Graphics.FromImage(bmp))
			{
				ScreenGraphics.CopyFromScreen(
					int.Parse(RegionX.Text), int.Parse(RegionY.Text),
					0, 0, bmp.Size, CopyPixelOperation.SourceCopy
					);
			}

			return bmp;

			GC.Collect();
		}

		private Bitmap screenshot = new Bitmap(1, 1);

		private void TakeScreenshot(object sender, RoutedEventArgs e)
		{
			Visibility prevVis = regPrev.Visibility;
			regPrev.Visibility = Visibility.Hidden;

			Timer t = new Timer { Interval = 1 };
			t.Tick += (s, ee) =>
			{
				t.Enabled = false;

				if (screenshot != null) screenshot.Dispose();
				Bitmap bmp = CreateScreenshot();
				screenshot = bmp;
				IntPtr handle = IntPtr.Zero;

				try
				{
					handle = bmp.GetHbitmap();
					Screenshot.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				}
				catch { return; }

				regPrev.Visibility = prevVis;
			};
			t.Start();

			GC.Collect();
		}

		private Bitmap ImgSrcToBitmap(BitmapSource Source)
		{
			int width = Source.PixelWidth;
			int height = Source.PixelHeight;
			int stride = width * ((Source.Format.BitsPerPixel + 7) / 8);
			IntPtr memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
			Source.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
			Bitmap bitmap = new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, memoryBlockPointer);
			return bitmap;
		}

		private void SaveScreenshot(object sender, RoutedEventArgs e)
		{
			if (screenshot != null)
			{
				Bitmap bmp = new Bitmap(1, 1);
				bmp = ImgSrcToBitmap((BitmapSource)Screenshot.Source);

				if (screenshot != bmp)
					screenshot = bmp;

				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Title = "Save your screenshot...";
				sfd.Filter = "Joint Photographic Experts Group (JPEG)|*.jpeg; *.jpg";

				bool? result = sfd.ShowDialog();

				if (result == true)
				{
					screenshot.Save(sfd.FileName, ImageFormat.Jpeg);
				}
			}
			else if (screenshot == null)
			{
				MessageBox.Show("Cannot save screenshot if one isn't taken.", "Screenshot Not Taken",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ModifyNumericInput(object sender, KeyEventArgs e)
		{
			try
			{
				int x = int.Parse(RegionX.Text);
				int y = int.Parse(RegionY.Text);
				int w = int.Parse(RegionW.Text);
				int h = int.Parse(RegionH.Text);

				TextBox txtbx = (TextBox)sender;

				if (e.Key == Key.Left)
				{
					if (txtbx == RegionX)
					{
						RegionX.Text = Math.Max(0, Math.Min(x - previewSpeed, int.MaxValue)).ToString();
						RegionW.Text = Math.Max(10, Math.Min(w + previewSpeed, int.MaxValue)).ToString();
					}
					else if (txtbx == RegionW)
					{
						RegionW.Text = Math.Max(10, Math.Min(w - previewSpeed, int.MaxValue)).ToString();
					}
				}
				else if (e.Key == Key.Right)
				{
					if (txtbx == RegionX)
					{
						RegionX.Text = Math.Max(0, Math.Min(x + previewSpeed, int.MaxValue)).ToString();
						RegionW.Text = Math.Max(10, Math.Min(w - previewSpeed, int.MaxValue)).ToString();
					}
					else if (txtbx == RegionW)
					{
						RegionW.Text = Math.Max(10, Math.Min(w + previewSpeed, int.MaxValue)).ToString();
					}
				}
				else if (e.Key == Key.Up)
				{
					if (txtbx == RegionY)
					{
						RegionY.Text = Math.Max(0, Math.Min(y - previewSpeed, int.MaxValue)).ToString();
						RegionH.Text = Math.Max(10, Math.Min(h + previewSpeed, int.MaxValue)).ToString();
					}
					else if (txtbx == RegionH)
					{
						RegionH.Text = Math.Max(10, Math.Min(h - previewSpeed, int.MaxValue)).ToString();
					}
				}
				else if (e.Key == Key.Down)
				{
					if (txtbx == RegionY)
					{
						RegionY.Text = Math.Max(0, Math.Min(y + previewSpeed, int.MaxValue)).ToString();
						RegionH.Text = Math.Max(10, Math.Min(h - previewSpeed, int.MaxValue)).ToString();
					}
					else if (txtbx == RegionH)
					{
						RegionH.Text = Math.Max(10, Math.Min(h + previewSpeed, int.MaxValue)).ToString();
					}
				}
			}
			catch { return; }
		}

		private void SelectCurrentScreen(object sender, RoutedEventArgs e)
		{
			Screen currentScreen = Screen.FromHandle(new WindowInteropHelper(this).Handle);
			int x = currentScreen.Bounds.X;
			int y = currentScreen.Bounds.Y;
			int w = currentScreen.Bounds.Width;
			int h = currentScreen.Bounds.Height;

			RegionX.Text = x.ToString();
			RegionY.Text = y.ToString();
			RegionW.Text = w.ToString();
			RegionH.Text = h.ToString();
		}
	}
}
