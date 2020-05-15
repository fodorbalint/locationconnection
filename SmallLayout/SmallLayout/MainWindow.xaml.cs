using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SmallLayout
{
	//map buttons have to be reverted

	public partial class MainWindow : Window
	{
		List<string> layoutFiles;
		List<string> drawableFiles;
		string layoutDir = @"c:\Users\fodor\LocationConnection Android\LocationConnection\Resources\layout\";
		string drawableDir = @"c:\Users\fodor\LocationConnection Android\LocationConnection\Resources\drawable\";
		int fileCount;
		int counter;
		float mulitplyBy;
		float divideBy;

		public MainWindow()
		{
			InitializeComponent();

			LoadButton.Click += LoadButton_Click;
			ReplaceButton.Click += ReplaceButton_Click;
		}

		private void LoadButton_Click(object sender, RoutedEventArgs e)
		{

			layoutFiles = new List<string>(Directory.GetFiles(layoutDir, @"*_normal.xml"));
			drawableFiles = new List<string>(Directory.GetFiles(drawableDir, @"*_normal.xml"));

			FileList.ItemsSource = new List<string>(layoutFiles.Concat(drawableFiles));
			fileCount = layoutFiles.Count + drawableFiles.Count;
			StatusText.Text = fileCount + " files loaded.";
		}

		private void ReplaceButton_Click(object sender, RoutedEventArgs e)
		{
			if (layoutFiles is null || drawableFiles is null)
			{
				return;
			}
			counter = 0;
			mulitplyBy = float.Parse(MulitplyBy.Text);
			divideBy = float.Parse(DivideBy.Text);

			foreach (string file in layoutFiles)
			{
				counter++;
				ConvertToSmall(file);
			}

			foreach (string file in drawableFiles)
			{
				counter++;
				ConvertToSmall(file);
			}
			StatusText.Text = fileCount + " files converted.";
		}

		private void ConvertToSmall(string file)
		{
			int offset = 0;

			StatusText.Text = "Processing " + counter + " / " + fileCount;
			DoEvents(); //updates layout
			string text = File.ReadAllText(file);
			string newFile = file.Replace("_normal", "_small");
			string newText = text.Replace(@"_normal""", @"_small""");
			newText = newText.Replace(@"Normal""", @"Small""");
			Regex regex = new Regex(@"""([^""]+)dp""");
			MatchCollection matches = regex.Matches(newText);

			Console.Text += file + " " + matches.Count + " matches" + Environment.NewLine;

			foreach (Match match in matches)
			{
				GroupCollection groups = match.Groups;
				int index = groups[1].Index;
				float value = float.Parse(groups[1].Value,CultureInfo.InvariantCulture);
				double newValue = Math.Round(value * mulitplyBy / divideBy, 2);
				
				var builder = new StringBuilder(newText);
				builder.Remove(index + offset, groups[1].Length);
				builder.Insert(index + offset, newValue.ToString(CultureInfo.InvariantCulture));
				newText = builder.ToString();

				offset += newValue.ToString(CultureInfo.InvariantCulture).Length - value.ToString(CultureInfo.InvariantCulture).Length;

				Console.Text += value.ToString(CultureInfo.InvariantCulture) + " > " + newValue.ToString(CultureInfo.InvariantCulture) + " at " + (index + offset) + Environment.NewLine;
			}
			File.WriteAllText(newFile, newText);
		}

		public void DoEvents()
		{
			DispatcherFrame frame = new DispatcherFrame(true);
			Dispatcher.CurrentDispatcher.BeginInvoke
			(
			DispatcherPriority.Background,
			(SendOrPostCallback)delegate (object arg)
			{
				var f = arg as DispatcherFrame;
				f.Continue = false;
			},
			frame
			);
			Dispatcher.PushFrame(frame);
		}
	}
}
